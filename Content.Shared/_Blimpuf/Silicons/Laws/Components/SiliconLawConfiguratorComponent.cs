using Content.Shared.DoAfter;

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

    /// <summary>
    /// Tracks the doafter for the relawing process
    /// </summary>
    [ViewVariables]
    public DoAfterId? ActiveDoAfter;

    /// <summary>
    /// Tracks the borg being relawed
    /// </summary>
    [ViewVariables]
    public EntityUid? ActiveTarget;
}
