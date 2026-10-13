using Content.Shared._Blimpuf.Antags.Traitor;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Blimpuf.Traitor.Components;

[RegisterComponent]
public sealed partial class SyndicateResearchTargetComponent : Component
{
    [DataField] public Boolean ItemScanned;

    [DataField] public ProtoId<SyndicateResearchSpecifierPrototype>? ResearchItem1;

    [DataField] public ProtoId<SyndicateResearchSpecifierPrototype>? ResearchItem2;

    [DataField] public ProtoId<SyndicateResearchSpecifierPrototype>? ResearchItem3;

    [DataField] public ProtoId<SyndicateResearchSpecifierPrototype>? ResearchItem4;

    [DataField] public Boolean Task1Complete;

    [DataField] public Boolean Task2Complete;

    [DataField] public Boolean Task3Complete;

    [DataField] public Boolean Task4Complete;

    [DataField(required: true)] public List<ProtoId<SyndicateResearchSpecifierPrototype>> SyndicateResearchSpecifiers = new();

    [DataField] public Boolean ResearchComplete;

    [DataField] public int ActiveResearchNumber;

    [DataField] public SoundSpecifier ResearchSound = new SoundPathSpecifier("/Audio/_Blimpuf/Items/traitor-research.ogg");

    [DataField(required: true)] public String ResearchUnlockId = default!;

    [DataField(required: true)] public String ResearchAnnouncementString = default!;

    public EntityUid? ActiveSound;

    [DataDefinition]
    public sealed partial class ResearchTargetEntry
    {
        [DataField(required: true)] public string DisplayName = string.Empty;

        [DataField(required: true)] public List<EntProtoId> ValidPrototypes = new();
    }
}

