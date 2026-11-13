using Content.Server._Blimpuf.Objectives.Components;
using Content.Server.Objectives.Components;
using Content.Server._Blimpuf.Traitor.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Server._Blimpuf.Objectives.Systems;

public sealed class SyndicateResearchTotalSystem : EntitySystem
{
    [Dependency] private readonly SyndicateResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchTotalConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, ResearchTotalConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!TryComp<NumberObjectiveComponent>(uid, out var number))
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = MathF.Min(1f, (float)_research.ResearchCount / number.Target);
    }
}
