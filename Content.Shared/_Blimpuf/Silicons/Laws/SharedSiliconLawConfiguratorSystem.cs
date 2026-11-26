using Content.Shared._Blimpuf.Silicons.Laws.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._Blimpuf.Silicons.Laws;

public sealed partial class SharedSiliconLawConfiguratorSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SiliconLawConfiguratorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SiliconLawConfiguratorComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<BorgChassisComponent, PanelChangedEvent>(OnPanelChanged);
    }

    private void OnAfterInteract(EntityUid uid, SiliconLawConfiguratorComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<BorgChassisComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("laws-configurator-invalid-target"), uid, args.User);
            args.Handled = true;
            return;
        }

        if (TryComp<WiresPanelComponent>(target, out var panel) && !panel.Open)
        {
            _popup.PopupClient(Loc.GetString("borg-panel-not-open"), uid, args.User);
            args.Handled = true;
            return;
        }

        if (!_itemSlots.TryGetSlot(uid, "circuit_holder", out var slot) || slot.Item == null)
        {
            _popup.PopupClient(Loc.GetString("laws-configurator-no-board"), uid, args.User);
            args.Handled = true;
            return;
        }

        if (!HasComp<SiliconLawProviderComponent>(slot.Item))
        {
            _popup.PopupClient(Loc.GetString("laws-configurator-no-laws"), uid, args.User);
            args.Handled = true;
            return;
        }

        if (component.ActiveDoAfter != null)
        {
            _doAfter.Cancel(component.ActiveDoAfter);
            component.ActiveDoAfter = null;
            component.ActiveTarget = null;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.UploadTime, new LawConfiguratorDoAfterEvent(), eventTarget: uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            NeedHand = true,
            BreakOnHandChange = true,
            BreakOnWeightlessMove = true,
            CancelDuplicate = true,
            BlockDuplicate = true
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs, out var id))
        {
            component.ActiveDoAfter = id;
            component.ActiveTarget = target;
            _popup.PopupPredicted(Loc.GetString("laws-configurator-begin", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(target, EntityManager))), target, args.User, PopupType.Medium);
        }
        args.Handled = true;
    }

    private void OnEntRemoved(Entity<SiliconLawConfiguratorComponent> component, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != "circuit_holder")
            return;

        CancelActive(component);
    }

    private void OnPanelChanged(Entity<BorgChassisComponent> component, ref PanelChangedEvent args)
    {
        if (args.Open)
            return;

        var query = EntityQueryEnumerator<SiliconLawConfiguratorComponent>();
        while (query.MoveNext(out var uid, out var config))
        {
            if (config.ActiveTarget == component.Owner)
                CancelActive((uid, config));
        }
    }

    private void CancelActive(Entity<SiliconLawConfiguratorComponent> component)
    {
        if (component.Comp.ActiveDoAfter is { } id)
            _doAfter.Cancel(id);

        component.Comp.ActiveDoAfter = null;
        component.Comp.ActiveTarget = null;
    }
}

[Serializable, NetSerializable]
public sealed partial class LawConfiguratorDoAfterEvent : SimpleDoAfterEvent;
