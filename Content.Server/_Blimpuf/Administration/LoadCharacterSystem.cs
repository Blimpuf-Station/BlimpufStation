using System.IO;
using System.Text;
using Content.Server._Starlight.GameTicking;
using Content.Server._Starlight.Traits;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Humanoid;
using Content.Server.Station.Systems;
using Content.Shared._Blimpuf.Administration;
using Content.Shared._Starlight.Character.Info;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Tag;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Blimpuf.Administration;

/// <summary>
/// Imports and spawns characters for active admins with DEBUG permission
/// </summary>
public sealed class LoadCharacterSystem : EntitySystem
{
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IAdminLogManager _adminLogs = default!;
    [Dependency] private IConsoleHost _console = default!;
    [Dependency] private HumanoidAppearanceSystem _appearance = default!;
    [Dependency] private SLSharedCharacterInfoSystem _characterInfo = default!;
    [Dependency] private StationSpawningSystem _spawning = default!;
    [Dependency] private StationSystem _stations = default!;
    [Dependency] private TagSystem _tags = default!;
    [Dependency] private TraitSystem _traits = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly ProtoId<TagPrototype> AdminSpawnedPlayer = "AdminSpawnedPlayer";

    public override void Initialize()
    {
        SubscribeNetworkEvent<LoadCharacterEvent>(OnLoadCharacter);
    }

    private void OnLoadCharacter(LoadCharacterEvent message, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_admins.HasAdminFlag(session, AdminFlags.Debug))
            return;

        if (session.AttachedEntity is not { } admin || !_transform.TryGetMapOrGridCoordinates(admin, out var coordinates))
        {
            _console.WriteError(session, "You must be on a map to load a character.");
            return;
        }

        try
        {
            using var file = new MemoryStream(Encoding.UTF8.GetBytes(message.Yaml));
            var profile = _appearance.FromStream(file, session, ignoreSpawnRestrictions: true);
            var station = _stations.GetOwningStation(admin);
            var mob = _spawning.SpawnPlayerMob(coordinates.Value, null, profile, station);
            _traits.ApplyTraits(mob, profile, session, ignoreRestrictions: true);
            _characterInfo.ApplyCharacterInfo(mob, profile);
            RaiseLocalEvent(new PlayerAdminSpawnEvent(mob, session, null, station, profile));
            _tags.AddTag(mob, AdminSpawnedPlayer);
            _adminLogs.Add(LogType.AdminCommands, LogImpact.High, $"Admin {session:player} loaded character {mob:entity}");
            _console.WriteLine(session, $"Spawned {profile.Name} ({GetNetEntity(mob)}).");
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not load character for {session.Name}: {ex.GetType().Name}");
            _console.WriteError(session, $"Could not load character: {ex.Message}");
        }
    }
}
