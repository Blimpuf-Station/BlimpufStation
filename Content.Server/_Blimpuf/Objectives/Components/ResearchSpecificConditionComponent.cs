using Robust.Shared.Utility;

namespace Content.Server._Blimpuf.Objectives.Components;

[RegisterComponent]
public sealed partial class ResearchSpecificConditionComponent : Component
{
    /// <summary>
    /// The item that needs to be researched
    /// </summary>
    [DataField(required: true)] public string? ResearchRequired;

    /// <summary>
    /// Sprite that will show in the objective icon
    /// </summary>
    [DataField(required: true)] public SpriteSpecifier Sprite;

    /// <summary>
    /// Help newer players by saying e.g. "research the chief engineer's advanced magboots"
    /// instead of "research advanced magboots. Should be a loc string.
    /// </summary>
    [DataField("owner")]
    public string? OwnerText;

    // All this need to be loc string
    [DataField(required: true)]
    public LocId ObjectiveText;
    [DataField(required: true)]
    public LocId ObjectiveNoOwnerText;
    [DataField(required: true)]
    public LocId DescriptionText;
    [DataField(required: true)]
    public LocId ItemName;
}
