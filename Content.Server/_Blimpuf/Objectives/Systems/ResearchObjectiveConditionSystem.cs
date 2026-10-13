using Content.Server._Blimpuf.Objectives.Components;
using Content.Server.Objectives.Components;
using Content.Server._Blimpuf.Traitor.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Server._Blimpuf.Objectives.Systems;

public sealed class SyndicateResearchObjectiveSystem : EntitySystem
{
    [Dependency] private readonly SyndicateResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchObjectiveConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, ResearchObjectiveConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!TryComp<NumberObjectiveComponent>(uid, out var number))
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = MathF.Min(1f, (float)_research.ResearchCount / number.Target);
    }
}
