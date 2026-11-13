using Content.Shared.CCVar;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Shared.SSDIndicator;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing; // Blimpuf

namespace Content.Client.SSDIndicator;

/// <summary>
///     Handles displaying SSD indicator as status icon
/// </summary>
public sealed partial class SSDIndicatorSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IGameTiming _timing = default!; // Blimpuf

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SSDIndicatorComponent, GetStatusIconsEvent>(OnGetStatusIcon);
    }

    private void OnGetStatusIcon(EntityUid uid, SSDIndicatorComponent component, ref GetStatusIconsEvent args)
    {
        if (component.IsSSD &&
            _cfg.GetCVar(CCVars.ICShowSSDIndicator) &&
            !_mobState.IsDead(uid) &&
            !HasComp<ActiveNPCComponent>(uid) &&
            HasComp<MindExaminableComponent>(uid))
        {
            // Blimpuf start
            var icon = component.Icon;
            if (component.SsdStartTime is { } startTime)
            {
                var elapsed = _timing.CurTime - startTime;
                if (elapsed >= TimeSpan.FromMinutes(10))
                    icon = component.RedIcon;
                else if (elapsed >= TimeSpan.FromMinutes(5))
                    icon = component.YellowIcon;
            }
            // Blimpuf end

            args.StatusIcons.Add(_prototype.Index(icon));
        }
    }
}
