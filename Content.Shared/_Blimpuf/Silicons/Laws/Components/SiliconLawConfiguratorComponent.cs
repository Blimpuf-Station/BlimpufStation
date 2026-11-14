namespace Content.Shared._Blimpuf.Silicons.Laws.Components;

/// <summary>
/// Attached to the Silicon Law Configurator to allow it to update Cyborg Laws
/// </summary>
[RegisterComponent]
public sealed partial class SiliconLawConfiguratorComponent : Component
{
    /// <summary>
    /// The amount of time it takes to relaw
    /// </summary>
    [DataField]
    public TimeSpan UploadTime = TimeSpan.FromSeconds(15);
}
