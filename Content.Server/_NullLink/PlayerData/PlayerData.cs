using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Robust.Shared.Player;
using Starlight.NullLink;
using Starlight.NullLink.Event;
using Color = Robust.Shared.Maths.Color;

namespace Content.Server._NullLink.PlayerData;

public sealed class PlayerData
{
    public string? Title { get; set; }
    public Color? AdminOocColor { get; set; }
    public required ICommonSession Session { get; init; }
    public ImmutableHashSet<ulong> Roles { get; set; } = [];
    public bool RolesLoaded { get; set; }
    public Dictionary<string, double> Resources { get; set; } = [];
    public Dictionary<string, Dictionary<string, TimeSpan>> RolePlayTimePerServer { get; set; } = [];
    public ulong DiscordId { get; set; }

    public void SyncRoles(PlayerRolesSyncEvent ev)
    {
        Roles = [.. ev.Roles];
        RolesLoaded = true; // Blimpuf: making sure player's Discord roles are actually loaded
    }

    public void UpdateRoles(RolesChangedEvent ev)
    {
        var roles = Roles.ToHashSet();
        roles.ExceptWith(ev.Remove);
        roles.UnionWith(ev.Add);
        Roles = [.. roles];
        RolesLoaded = true;
    }
}
