using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Events;

/// <summary>
///     Event raised to check if a player is allowed/able to assume a role.
/// </summary>
/// <param name="player">The player.</param>
/// <param name="jobs">Optional list of job prototype IDs</param>
/// <param name="antags">Optional list of antag prototype IDs</param>
/// <param name="isSpawning">Are we checking this in the context of spawning a new entity (as opposed to assuming the place of an existing one)?</param> // Starlight
/// <param name="profile">The selected character, if checking a specific character's job eligibility.</param> // Blimpuf
[ByRefEvent]
public struct IsRoleAllowedEvent(
    ICommonSession player,
    List<ProtoId<JobPrototype>>? jobs,
    List<ProtoId<AntagPrototype>>? antags,
    bool cancelled = false,
    bool isSpawning = true, // Starlight - add isSpawning
    HumanoidCharacterProfile? profile = null) // Blimpuf
{
    public readonly ICommonSession Player = player;
    public readonly List<ProtoId<JobPrototype>>? Jobs = jobs;
    public readonly List<ProtoId<AntagPrototype>>? Antags = antags;
    public bool Cancelled = cancelled;
    public bool IsSpawning = isSpawning; // Starlight - add isSpawning
    public readonly HumanoidCharacterProfile? Profile = profile; // Blimpuf
}
