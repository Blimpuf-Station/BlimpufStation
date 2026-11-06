using Content.Shared._Blimpuf.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Blimpuf.Chemistry;

public sealed partial class MultiSolutionRegenerationSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultiSolutionRegenerationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MultiSolutionRegenerationComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnMapInit(Entity<MultiSolutionRegenerationComponent> ent, ref MapInitEvent args)
    {
        foreach (var solution in ent.Comp.Solutions)
            solution.NextRegenTime = _timing.CurTime + solution.Duration;
    }

    private void OnEntRemoved(Entity<MultiSolutionRegenerationComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        foreach (var solution in ent.Comp.Solutions)
        {
            if (args.Entity == solution.SolutionRef?.Owner)
                solution.SolutionRef = null;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<MultiSolutionRegenerationComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var regen, out var manager))
        {
            foreach (var entry in regen.Solutions)
            {
                if (entry.PauseWhenDead && _mobState.IsDead(uid))
                    continue;

                if (time < entry.NextRegenTime)
                    continue;

                entry.NextRegenTime = time + entry.Duration;
                if (!_solutionContainer.ResolveSolution((uid, manager), entry.Solution, ref entry.SolutionRef,
                        out var solution))
                    continue;

                var amount = FixedPoint2.Min(solution.AvailableVolume, entry.Generated.Volume);
                if (amount <= FixedPoint2.Zero)
                    continue;

                var generated = amount == entry.Generated.Volume
                    ? entry.Generated
                    : entry.Generated.Clone().SplitSolution(amount);

                _solutionContainer.TryAddSolution(entry.SolutionRef.Value, generated);
            }
        }
    }
}
