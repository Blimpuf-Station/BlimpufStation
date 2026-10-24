using Robust.Shared.Enums;
using Robust.Shared.GameStates;

namespace Content.Shared._Blimpuf.IdentityManagement;

/// <summary>
/// Carries the secondary pronoun set on the identity entity used by examine localization.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlimpufIdentityPronounsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Gender SecondaryGender;
}
