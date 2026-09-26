namespace Content.Client._Blimpuf.Changeling;

using Content.Shared._Blimpuf.Changeling;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

public sealed class ChangelingContortionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingContortionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChangelingContortionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, ChangelingContortionComponent comp, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var shader = _proto.Index<ShaderPrototype>("Contortion").Instance().Duplicate();
        sprite.PostShader = shader;
        sprite.GetScreenTexture = true;
        sprite.RaiseShaderEvent = true;
    }

    private void OnShutdown(EntityUid uid, ChangelingContortionComponent comp, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.PostShader = null;
        sprite.GetScreenTexture = false;
    }
}
