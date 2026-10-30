using Content.Shared._Blimpuf.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.Directions;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Shared._Blimpuf.Chemistry.EntitySystems;

public sealed partial class SqueezableSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SolutionTransferSystem _solutionTransfer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SqueezableComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SqueezableComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<SqueezableComponent, SqueezableTransferDoAfterEvent>(OnTransferDoAfter);
        SubscribeLocalEvent<SqueezableComponent, SqueezableSpillDoAfterEvent>(OnSpillDoAfter);
    }

    private void OnAfterInteract(Entity<SqueezableComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach
            || args.Target is not { } target
            || !_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var source)
            || source.Volume <= FixedPoint2.Zero
            || !_solution.TryGetRefillableSolution(target, out _, out var destination)
            || destination.AvailableVolume <= FixedPoint2.Zero)
        {
            return;
        }

        args.Handled = true;
        StartSqueeze(ent, args.User, new SqueezableTransferDoAfterEvent(), target);
    }

    private void OnGetVerbs(Entity<SqueezableComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || args.Using != ent.Owner)
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution)
            || solution.Volume <= FixedPoint2.Zero)
        {
            return;
        }

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(ent.Comp.SqueezeVerb),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/bubbles.svg.192dpi.png")),
            Act = () => StartSqueeze(ent, user, new SqueezableSpillDoAfterEvent(), ent.Owner)
        });
    }

    private void StartSqueeze<T>(Entity<SqueezableComponent> ent, EntityUid user, T doAfterEvent, EntityUid target)
        where T : SimpleDoAfterEvent
    {
        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            user,
            ent.Comp.SqueezeDuration,
            doAfterEvent,
            ent.Owner,
            target: target,
            used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        }))
        {
            return;
        }

        _popup.PopupPredicted(
            Loc.GetString(ent.Comp.SqueezeStartMessage, ("item", ent.Owner)),
            user,
            user);
    }

    private void OnTransferDoAfter(Entity<SqueezableComponent> ent, ref SqueezableTransferDoAfterEvent args)
    {
        if (args.Cancelled || args.Args.Target is not { } target
            || !_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out var source, out var sourceSolution)
            || sourceSolution.Volume <= FixedPoint2.Zero
            || !_solution.TryGetRefillableSolution(target, out var destination, out var destinationSolution)
            || destinationSolution.AvailableVolume <= FixedPoint2.Zero)
        {
            return;
        }

        var transferred = _solutionTransfer.Transfer(new SolutionTransferData(
            args.User,
            ent.Owner,
            source.Value,
            target,
            destination.Value,
            sourceSolution.Volume));

        if (transferred <= FixedPoint2.Zero)
            return;

        DamageSqueezedEntity(ent, args.User);
        _audio.PlayPredicted(ent.Comp.SqueezeSound, ent.Owner, args.User);
    }

    private void OnSpillDoAfter(Entity<SqueezableComponent> ent, ref SqueezableSpillDoAfterEvent args)
    {
        if (args.Cancelled
            || !_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out var source, out var solution)
            || solution.Volume <= FixedPoint2.Zero)
        {
            return;
        }

        var contents = _solution.SplitSolution(source.Value, solution.Volume);
        var userTransform = Transform(args.User);
        var spillCoordinates = userTransform.Coordinates.Offset(userTransform.LocalRotation.GetCardinalDir());
        _puddle.TrySpillAt(spillCoordinates, contents, out _);
        DamageSqueezedEntity(ent, args.User);
        _audio.PlayPredicted(ent.Comp.SqueezeSound, ent.Owner, args.User);
    }

    private void DamageSqueezedEntity(Entity<SqueezableComponent> ent, EntityUid user)
    {
        if (ent.Comp.SqueezeDamage is { } damage)
            _damageable.TryChangeDamage(ent.Owner, damage, origin: user);
    }
}
