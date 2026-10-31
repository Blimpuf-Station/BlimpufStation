using Content.Server.Explosion.EntitySystems;
using Content.Server.Flash;
using Content.Shared._Blimpuf.Antags.Traitor.Components;
using Content.Shared.Emp;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server._Blimpuf.Traitor.Systems;

public sealed class UltimaHardsuitSystem : EntitySystem
{
    [Dependency] private readonly SharedEmpSystem _emp = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IEntityManager _entityManager = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UltimaHardsuitEmpEvent>(OnEmp);
        SubscribeLocalEvent<UltimaHardsuitFlashbangEvent>(OnFlashbang);
        SubscribeLocalEvent<UltimaHardsuitBlastEvent>(OnBlast);
        SubscribeLocalEvent<UltimaHardsuitComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<UltimaHardsuitComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<UltimaHardsuitComponent> ent, ref GotEquippedEvent args)
    {
        if (args.Slot != "outerClothing")
            return;

        _actions.AddAction(args.EquipTarget, ref ent.Comp.EmpActionEntity, ent.Comp.EmpAction);
        _actions.AddAction(args.EquipTarget, ref ent.Comp.FlashbangActionEntity, ent.Comp.FlashbangAction);
        _actions.AddAction(args.EquipTarget, ref ent.Comp.BlastActionEntity, ent.Comp.BlastAction);
    }

    private void OnUnequipped(Entity<UltimaHardsuitComponent> ent, ref GotUnequippedEvent args)
    {
        if (args.Slot != "outerClothing")
            return;

        var owner = args.EquipTarget;

        if (ent.Comp.EmpActionEntity is { } emp)
            _actions.RemoveAction(owner, emp);

        if (ent.Comp.FlashbangActionEntity is { } flash)
            _actions.RemoveAction(owner, flash);

        if (ent.Comp.BlastActionEntity is { } blast)
            _actions.RemoveAction(owner, blast);
    }

    private void OnEmp(UltimaHardsuitEmpEvent args)
    {
        var user = args.Performer;

        const float Cost = 180f;

        if (!_inventory.TryGetSlotEntity(user, "outerClothing", out var suitUid))
            return;

        if (suitUid is not { } suit)
            return;

        if (!HasComp<UltimaHardsuitComponent>(suit))
            return;

        if (!HasComp<PowerCellSlotComponent>(suit))
            return;

        if (!_powerCell.HasBattery(suit))
        {
            _popup.PopupEntity(Loc.GetString("ultima-hardsuit-no-cell"), suit, user);
            return;
        }

        if (!_powerCell.TryUseCharge(suit, Cost))
        {
            _popup.PopupEntity(Loc.GetString("ultima-hardsuit-insufficient-charge"), suit, user);
            return;
        }

        _emp.EmpPulse(Transform(user).Coordinates, 6, 100000f, TimeSpan.FromSeconds(60), user);
        args.Handled = true;

        var sound = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningbolt.ogg");
        _audio.PlayPvs(sound, suit);
    }

    private void OnFlashbang(UltimaHardsuitFlashbangEvent args)
    {
        var user = args.Performer;

        const float Cost = 180f;

        if (!_inventory.TryGetSlotEntity(user, "outerClothing", out var suitUid))
            return;

        if (suitUid is not { } suit)
            return;

        if (!HasComp<UltimaHardsuitComponent>(suit))
            return;

        if (!HasComp<PowerCellSlotComponent>(suit))
            return;

        if (!_powerCell.HasBattery(suit))
        {
            _popup.PopupEntity(Loc.GetString("ultima-hardsuit-no-cell"), suit, user);
            return;
        }

        if (!_powerCell.TryUseCharge(suit, Cost))
        {
            _popup.PopupEntity(Loc.GetString("ultima-hardsuit-insufficient-charge"), suit, user);
            return;
        }

        _flash.FlashArea(suit, suit, 6, TimeSpan.FromSeconds(8));
        args.Handled = true;

        var sound = new SoundPathSpecifier("/Audio/Effects/flash_bang.ogg");
        _audio.PlayPvs(sound, suit);
        var coords = Transform(suit).Coordinates;
        _entityManager.SpawnEntity("UltimaFlashEffect", coords);
    }

    private void OnBlast(UltimaHardsuitBlastEvent args)
    {
        var user = args.Performer;

        const float Cost = 180f;

        if (!_inventory.TryGetSlotEntity(user, "outerClothing", out var suitUid))
            return;

        if (suitUid is not { } suit)
            return;

        if (!HasComp<UltimaHardsuitComponent>(suit))
            return;

        if (!HasComp<PowerCellSlotComponent>(suit))
            return;

        if (!_powerCell.HasBattery(suit))
        {
            _popup.PopupEntity(Loc.GetString("ultima-hardsuit-no-cell"), suit, user);
            return;
        }

        if (!_powerCell.TryUseCharge(suit, Cost))
        {
            _popup.PopupEntity(Loc.GetString("ultima-hardsuit-insufficient-charge"), suit, user);
            return;
        }

        _explosionSystem.QueueExplosion(suit, "Default", 120f, 3f, 12f, 1f, 100);

        args.Handled = true;
    }
}
