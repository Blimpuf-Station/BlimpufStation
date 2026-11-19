using Content.Shared._Blimpuf.Geras;
using System.Linq;
using Content.Client.Items.Systems;
using Content.Client.Clothing;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Robust.Client.GameObjects;

namespace Content.Client._Blimpuf.Geras;
public sealed class ClientGerasSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly ItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GerasColorComponent, AfterAutoHandleStateEvent>(OnUpdateColor);
        SubscribeLocalEvent<GerasColorComponent, GetInhandVisualsEvent>(OnGetInhandVisuals, after: [typeof(ItemSystem)]);
        SubscribeLocalEvent<GerasColorComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals, after: [typeof(ClientClothingSystem)]);
    }
    private void OnUpdateColor(EntityUid uid, GerasColorComponent comp, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            for (var layer = 0; layer < sprite.AllLayers.Count(); layer++)
                _sprite.LayerSetColor((uid, sprite), layer, comp.Color);

            _item.VisualsChanged(uid);
        }
    }

    private void OnGetInhandVisuals(EntityUid uid, GerasColorComponent comp, GetInhandVisualsEvent args)
    {
        foreach (var (_, layer) in args.Layers)
            layer.Color = comp.Color;
    }

    private void OnGetEquipmentVisuals(EntityUid uid, GerasColorComponent comp, GetEquipmentVisualsEvent args)
    {
        foreach (var (_, layer) in args.Layers)
            layer.Color = comp.Color;
    }
}
