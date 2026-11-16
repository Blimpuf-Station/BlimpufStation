using Content.Server._Blimpuf.Objectives.Components;
using Content.Server._Blimpuf.Traitor.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Blimpuf.Objectives.Systems;

public sealed class ResearchSpecificObjectiveSystem : EntitySystem
{
    [Dependency] private readonly SyndicateResearchSystem _research = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchSpecificConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<ResearchSpecificConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnGetProgress(EntityUid uid, ResearchSpecificConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (comp.ResearchRequired == null)
        {
            args.Progress = 0f;
            return;
        }

        var check = _research.IsResearched(comp.ResearchRequired);

        if (check)
            args.Progress = 1f;
        else
            args.Progress = 0f;
    }

    private void OnAfterAssign(Entity<ResearchSpecificConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        var localizedName = Loc.GetString(condition.Comp.ItemName);

        var title = condition.Comp.OwnerText == null
            ? Loc.GetString(condition.Comp.ObjectiveNoOwnerText, ("itemName", localizedName))
            : Loc.GetString(condition.Comp.ObjectiveText, ("owner", Loc.GetString(condition.Comp.OwnerText)), ("itemName", localizedName));

        var description = Loc.GetString(condition.Comp.DescriptionText, ("itemName", localizedName));

        _metaData.SetEntityName(condition.Owner, title, args.Meta);
        _metaData.SetEntityDescription(condition.Owner, description, args.Meta);
        _objectives.SetIcon(condition.Owner, condition.Comp.Sprite, args.Objective);
    }
}
