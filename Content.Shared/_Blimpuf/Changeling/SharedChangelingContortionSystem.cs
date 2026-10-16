using Content.Shared.Examine;

namespace Content.Shared._Blimpuf.Changeling;

public sealed class SharedChangelingContortionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingContortionComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, ChangelingContortionComponent comp, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("changeling-contorted"));
    }
}
