using Robust.Shared.Serialization;

namespace Content.Shared._Blimpuf.Administration;

/// <summary>
/// Carries the character profile YAML selected by the admin.
/// </summary>
[Serializable, NetSerializable]
public sealed class LoadCharacterEvent(string yaml) : EntityEventArgs
{
    public string Yaml { get; } = yaml;
}
