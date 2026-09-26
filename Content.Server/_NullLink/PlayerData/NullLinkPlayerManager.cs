using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Blimpuf.Discord;
using Content.Server._NullLink.Core;
using Content.Server._NullLink.Helpers;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared._NullLink;
using Content.Shared.NullLink.CCVar;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._NullLink.PlayerData;

public sealed partial class NullLinkPlayerManager : INullLinkPlayerManager
{
    [Dependency] private IActorRouter _actors = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IServerNetManager _netMgr = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private PlayTimeTrackingManager _playTimeTrackingManager = default!;
    [Dependency] private ISharedNullLinkPlayerResourcesManager _playerResourcesManager = default!;
    [Dependency] private IServerDbManager _dbManager = default!;
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private ITaskManager _taskManager = default!;
    [Dependency] private IBlimpufDiscordRoleProvider _blimpufDiscordRoles = default!;
    [Dependency] private IBlimpufDiscordLinkService _blimpufDiscordLink = default!;

    private readonly ConcurrentDictionary<Guid, PlayerData> _playerById = [];
    private readonly ConcurrentDictionary<Guid, ICommonSession> _mentors = [];
    private ISawmill _sawmill = default!;
    private ISawmill _blimpufDiscordSawmill = default!;
    private RoleRequirementPrototype? _mentorReq;
    private TitleBuilderPrototype? _builder;
    private ServerPlaytimeRecognitionPrototype? _serverPlaytimeRecognition;
    private string? _server;

    private bool _resourcesEnabled = false;

    public IEnumerable<ICommonSession> Mentors => _mentors.Values;
    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("NullLink player data");
        _blimpufDiscordSawmill = _logManager.GetSawmill("blimpuf.discord.roles");
        _netMgr.RegisterNetMessage<MsgUpdatePlayerRoles>();
        _netMgr.RegisterNetMessage<MsgUpdatePlayerPlayTime>();
        _netMgr.RegisterNetMessage<MsgUpdatePlayerResources>();
        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        _cfg.OnValueChanged(NullLinkCCVars.RoleReqMentors, UpdateMentors, true);
        _cfg.OnValueChanged(NullLinkCCVars.AdminRankBuilder, UpdateAdminBuilder, true);
        _cfg.OnValueChanged(NullLinkCCVars.TitleBuild, UpdateTitleBuilder, true);
        _cfg.OnValueChanged(NullLinkCCVars.Project, UpdateProject, true);
        _cfg.OnValueChanged(NullLinkCCVars.Server, UpdateServer, true);
        _cfg.OnValueChanged(NullLinkCCVars.ResourcesEnabled, UpdateResources, true);

        _actors.OnConnected += OnNullLinkConnected;
    }

    private void UpdateResources(bool obj) => _resourcesEnabled = obj;

    private void OnNullLinkConnected()
    {
        if (!_actors.TryGetServerGrain(out var serverGrain))
            return;

        foreach (var player in _playerById)
        {
            serverGrain.PlayerConnected(player.Key)
                .FireAndForget(err => _sawmill.Error($"PlayerConnected after reconnect failed for {player.Key}: {err}"));
        }
    }

    public void Shutdown()
    {
        _actors.OnConnected -= OnNullLinkConnected;
        _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
        _playerById.Clear();
    }

    public bool TryGetPlayerData(Guid userId, [NotNullWhen(true)] out PlayerData? playerData)
        => _playerById.TryGetValue(userId, out playerData);

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        switch (e.NewStatus)
        {
            case SessionStatus.Zombie:
            case SessionStatus.Connecting:
                break;
            case SessionStatus.Connected:
                var state = new PlayerData
                {
                    Session = e.Session,
                };
                _playerById[e.Session.UserId] = state;
                if (_actors.TryGetServerGrain(out var serverGrain))
                    serverGrain.PlayerConnected(e.Session.UserId)
                        .FireAndForget(err=> _sawmill.Error($"PlayerConnected dispatch failed: {err}"));
                SendPlayerRoles(e.Session, state.Roles);
                SyncBlimpufDiscordRoles(e.Session);
                break;
            case SessionStatus.InGame:
                if (_playerById.TryGetValue(e.Session.UserId, out var inGamePlayerData)
                    && inGamePlayerData.RolesLoaded) // Blimpuf: make sure Discord roles are actually loaded before assigning
                {
                    var userId = e.Session.UserId.UserId;
                    MentorCheck(userId, inGamePlayerData);
                    AdminCheck(userId, inGamePlayerData);
                    RebuildTitle(userId, inGamePlayerData);
                    SendPlayerRoles(e.Session, inGamePlayerData.Roles);
                }
                break;
            case SessionStatus.Disconnected:
                if (_actors.TryGetServerGrain(out var serverGrain2))
                    serverGrain2.PlayerDisconnected(e.Session.UserId)
                        .FireAndForget(err => _sawmill.Error($"PlayerDisconnected dispatch failed: {err}"));
                if (_playerById.TryGetValue(e.Session.UserId, out var playerData)
                    && playerData.Session == e.Session)
                {
                    _playerById.Remove(e.Session.UserId, out _);
                }
                _mentors.Remove(e.Session.UserId, out _);
                _discordPromptOpen.Remove(e.Session);
                break;
            default:
                break;
        }
    }

    private void UpdateMentors(string obj)
    {
        if(_mentorReq?.ID == obj)
            return;

        _mentors.Clear();
        if (!_proto.TryIndex<RoleRequirementPrototype>(obj, out var mentorReq))
            return;
        _mentorReq = mentorReq;

        Pipe.RunInBackground(async () =>
        {
            foreach (var player in _playerById)
            {
                if (_mentorReq?.Roles.Any(player.Value.Roles.Contains) != true)
                    continue;
                _mentors.TryAdd(player.Key, player.Value.Session);
            }
        });
    }

    private void MentorCheck(Guid player, PlayerData playerData)
    {
        if (_mentorReq?.Roles.Any(playerData.Roles.Contains) == true)
            _mentors.TryAdd(player, playerData.Session);
        else
            _mentors.Remove(player, out _);
    }

    // Blimpuf Start
    private void SyncBlimpufDiscordRoles(ICommonSession session)
    {
        Pipe.RunInBackground(async () =>
        {
            DiscordRoleSnapshot? snapshot;

            try
            {
                snapshot = await _blimpufDiscordRoles.GetRolesAsync(session.UserId);
            }
            catch (Exception ex)
            {
                _blimpufDiscordSawmill.Error($"Blimpuf Discord role sync failed for {session.UserId}: {ex}");
                return;
            }

            if (snapshot == null)
            {
                var url = _blimpufDiscordLink.GetAuthUrl(session.UserId.ToString());
                if (!string.IsNullOrEmpty(url))
                    _taskManager.RunOnMainThread(() => OpenDiscordPrompt(session, url));

                return;
            }

            _taskManager.RunOnMainThread(() =>
            {
                var userId = session.UserId.UserId;

                if (!_playerById.TryGetValue(userId, out var playerData)
                    || playerData.Session != session)
                    return;

                playerData.Roles = snapshot.Roles.ToImmutableHashSet();
                playerData.RolesLoaded = true;
                playerData.DiscordId = snapshot.DiscordId;

                MentorCheck(userId, playerData);
                AdminCheck(userId, playerData);
                RebuildTitle(userId, playerData);
                SendPlayerRoles(playerData.Session, playerData.Roles);
            });
        });
    }
    // Blimpuf end
}
