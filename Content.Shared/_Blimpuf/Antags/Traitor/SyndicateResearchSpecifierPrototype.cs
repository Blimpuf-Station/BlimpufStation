using Robust.Shared.Prototypes;

namespace Content.Shared._Blimpuf.Antags.Traitor;

/// <summary>
/// Prototype which specifies the requirements for each possible task for Syndicate Research
/// </summary>
[Prototype("SyndicateResearchSpecifier")]
public sealed partial class SyndicateResearchSpecifierPrototype : IPrototype
{
    /// <summary>
    /// Try to keep this in the format SyndicateResearchSpecifierX please so it's easier lol
    /// </summary>
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Display name for the requirements for the research
    /// </summary>
    [DataField(required: true)]
    public string DisplayName = string.Empty;

    /// <summary>
    /// A list of all valid prototypes for the research
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> ValidPrototypes = new();
}
