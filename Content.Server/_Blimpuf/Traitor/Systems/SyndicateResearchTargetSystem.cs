using System.Linq;
using Content.Server._Blimpuf.Traitor.Components;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared._Blimpuf.Antags.Traitor.Components;
using Content.Shared._Starlight.Antags.Traitor;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Blimpuf.Traitor.Systems;

public sealed partial class SyndicateResearchTargetSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SyndicateResearchSystem _completedResearch = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SyndicateResearchTargetComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SyndicateResearchTargetComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SyndicateResearchTargetComponent, SyndicateResearchDoAfterEvent>(OnDoAfter);
    }
    private void OnExamined(EntityUid uid, SyndicateResearchTargetComponent component, ExaminedEvent args)
    {
        if (!HasComp<TraitorComponent>(args.Examiner))
            return;

        if (component.ResearchComplete)
        {
            args.PushMarkup(Loc.GetString("traitor-research-complete"));
            return;
        }

        var message = Loc.GetString("traitor-research-incomplete") + "\n";

        if (!component.ItemScanned)
        {
            message += Loc.GetString("traitor-research-requires-scan");
            args.PushMarkup(message);
            return;
        }

        if (component.ResearchItem1 != null && !component.Task1Complete)
        {
            var item = _prototype.Index(component.ResearchItem1.Value).DisplayName;
            message += Loc.GetString("traitor-research-task-1-requirements", ("item1", item)) + "\n";
        }
        else if (component.ResearchItem1 != null && component.Task1Complete)
            message += Loc.GetString("traitor-research-task-1-complete") + "\n";

        if (component.ResearchItem2 != null && !component.Task2Complete)
        {
            var item = _prototype.Index(component.ResearchItem2.Value).DisplayName;
            message += Loc.GetString("traitor-research-task-2-requirements", ("item2", item)) + "\n";
        }
        else if (component.ResearchItem2 != null && component.Task2Complete)
            message += Loc.GetString("traitor-research-task-2-complete") + "\n";

        if (component.ResearchItem3 != null && !component.Task3Complete)
        {
            var item = _prototype.Index(component.ResearchItem3.Value).DisplayName;
            message += Loc.GetString("traitor-research-task-3-requirements", ("item3", item)) + "\n";
        }
        else if (component.ResearchItem3 != null && component.Task3Complete)
            message += Loc.GetString("traitor-research-task-3-complete") + "\n";

        if (component.ResearchItem4 != null && !component.Task4Complete)
        {
            var item = _prototype.Index(component.ResearchItem4.Value).DisplayName;
            message += Loc.GetString("traitor-research-task-4-requirements", ("item4", item)) + "\n";
        }
        else if (component.ResearchItem4 != null && component.Task4Complete)
            message += Loc.GetString("traitor-research-task-4-complete");

        args.PushMarkup(message);
    }

    private void OnInteractUsing(EntityUid uid, SyndicateResearchTargetComponent component, InteractUsingEvent args)
    {
        if (!TryComp(args.Used, out MetaDataComponent? meta))
            return;

        var proto = meta.EntityPrototype?.ID;

        if (proto == null)
            return;

        if (!HasComp<TraitorComponent>(args.User))
            return;

        if (HasComp<SyndicateResearchScannerComponent>(args.Used))
        {
            if (component.ItemScanned)
                return;

            if (component.SyndicateResearchSpecifiers.Count < 4)
            {
                _popup.PopupEntity(Loc.GetString("traitor-research-scan-failed"), uid);
                return;
            }

            var list = component.SyndicateResearchSpecifiers.ToList();

            _random.Shuffle(list);

            var selected = list.Take(4).ToList();

            component.ResearchItem1 = selected[0];
            component.ResearchItem2 = selected[1];
            component.ResearchItem3 = selected[2];
            component.ResearchItem4 = selected[3];

            var confirmSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
            _audio.PlayPvs(confirmSound, uid);

            _popup.PopupEntity(Loc.GetString("traitor-research-scan-success"), uid);

            component.ItemScanned = true;
        }

        if (component.ResearchItem1 != null && _prototype.Index(component.ResearchItem1.Value).ValidPrototypes.Contains(proto) && !component.Task1Complete)
            component.ActiveResearchNumber = 1;
        else if (component.ResearchItem2 != null && _prototype.Index(component.ResearchItem2.Value).ValidPrototypes.Contains(proto) && !component.Task2Complete)
            component.ActiveResearchNumber = 2;
        else if (component.ResearchItem3 != null && _prototype.Index(component.ResearchItem3.Value).ValidPrototypes.Contains(proto) && !component.Task3Complete)
            component.ActiveResearchNumber = 3;
        else if (component.ResearchItem4 != null && _prototype.Index(component.ResearchItem4.Value).ValidPrototypes.Contains(proto) && !component.Task4Complete)
            component.ActiveResearchNumber = 4;
        else
            return;

        component.ActiveSound = _audio.PlayPvs(component.ResearchSound, uid)?.Entity;

        var doAfterEvent = new SyndicateResearchDoAfterEvent
        {
            UserName = Name(args.User),
            ResearchingName = Name(uid),
        };

        var doAfter = new DoAfterArgs(EntityManager, args.User, 30f, doAfterEvent, uid)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
            BreakOnDropItem = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnWeightlessMove = true,
            BlockDuplicate = false,
            CancelDuplicate = false
        };

        if (!TryComp(args.User, out MetaDataComponent? userMeta))
            return;

        if (!TryComp(uid, out MetaDataComponent? researchingMeta))
            return;

        if (!TryComp(args.Used, out MetaDataComponent? usedMeta))
            return;

        _doAfter.TryStartDoAfter(doAfter);
        var popup = Loc.GetString("traitor-research-begin", ("user", userMeta.EntityName), ("researching", researchingMeta.EntityName), ("used", usedMeta.EntityName));
        _popup.PopupEntity(popup, uid, PopupType.LargeCaution);
    }

    private void OnDoAfter(EntityUid uid, SyndicateResearchTargetComponent component, SyndicateResearchDoAfterEvent args)
    {
        if (component.ActiveSound != null) _audio.Stop(component.ActiveSound.Value);

        if (args.Cancelled)
            return;

        if (component.ActiveResearchNumber == 1)
            component.Task1Complete = true;
        else if (component.ActiveResearchNumber == 2)
            component.Task2Complete = true;
        else if (component.ActiveResearchNumber == 3)
            component.Task3Complete = true;
        else if (component.ActiveResearchNumber == 4)
            component.Task4Complete = true;

        var popupOthers = Loc.GetString("traitor-research-finish", ("user", args.UserName), ("researching", args.ResearchingName));
        _popup.PopupEntity(popupOthers, uid, PopupType.LargeCaution);

        if (component.Task1Complete && component.Task2Complete && component.Task3Complete && component.Task4Complete)
        {
            component.ResearchComplete = true;

            _completedResearch.Research(component.ResearchUnlockId);
            var researchAnnouncementSound = new SoundPathSpecifier("/Audio/_Starlight/Announcements/attention.ogg");

            var delay = TimeSpan.FromSeconds(_random.Next(60, 301));

            Timer.Spawn(delay,
                () =>
                {
                    _chat.DispatchGlobalAnnouncement(Loc.GetString(component.ResearchAnnouncementString), playSound: true, announcementSound: researchAnnouncementSound , colorOverride: Color.Red);
                });
        }

    }
}
