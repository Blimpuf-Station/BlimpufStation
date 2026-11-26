using System.Linq;
using Content.Server.Administration;
using Content.Server._Starlight.Medical.Body.Systems;
using Content.Server._Starlight.Medical.Limbs;
using Content.Shared.Administration;
using Content.Shared._Starlight;
using Content.Shared._Starlight.Medical.Body.Part;
using Content.Shared._Starlight.Medical.Surgery.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._Blimpuf.Administration.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class LimbCommand : ToolshedCommand
{
    private BodySystem? _body;
    private LimbSystem? _limbs;
    private StarlightEntitySystem? _entitySystem;

    [CommandImplementation("replace")]
    public EntityUid Replace([PipedArgument] EntityUid uid, string slot, EntProtoId prototype)
    {
        _body ??= GetSys<BodySystem>();
        _limbs ??= GetSys<LimbSystem>();
        _entitySystem ??= GetSys<StarlightEntitySystem>();

        var limb = _body.GetBodyChildren(uid)
            .FirstOrDefault(part => _body.GetParentPartAndSlotOrNull(part.Id)?.Slot == slot);
        if (limb == default
            || limb.Component.PartType is not (BodyPartType.Arm or BodyPartType.Hand or BodyPartType.Leg or BodyPartType.Foot)
            || HasComp<CustomLimbComponent>(limb.Id))
            return uid;

        if (_body.GetParentPartOrNull(limb.Id) is { } parent
            && _entitySystem.TryEntity<BodyPartComponent>(parent, out var parentPart)
            && _entitySystem.TryEntity<TransformComponent, HumanoidAppearanceComponent, BodyComponent>(uid, out var body)
            && _entitySystem.TryEntity<TransformComponent, MetaDataComponent, BodyPartComponent>(limb.Id, out var oldLimb))
        {
            var replacement = Spawn(prototype, body.Comp1.Coordinates);
            if (_entitySystem.TryEntity<BodyPartComponent>(replacement, out var newLimb, log: false)
                && newLimb.Comp.PartType == oldLimb.Comp3.PartType
                && newLimb.Comp.Symmetry == oldLimb.Comp3.Symmetry)
            {
                _limbs.Amputatate(body, oldLimb);
                _limbs.AttachLimb((body.Owner, body.Comp2), slot, parentPart, newLimb);
            }
            else
                Del(replacement);
        }

        return uid;
    }

    [CommandImplementation("replace")]
    public IEnumerable<EntityUid> Replace([PipedArgument] IEnumerable<EntityUid> entities, string slot,
        EntProtoId prototype) => entities.Select(uid => Replace(uid, slot, prototype));
}
