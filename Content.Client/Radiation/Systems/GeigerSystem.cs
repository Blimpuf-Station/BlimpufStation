using Content.Client.Items;
using Content.Client.Radiation.UI;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Client.Replays.Playback;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Client.Radiation.Systems;

public sealed class GeigerSystem : SharedGeigerSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private IReplayPlaybackManager _replayPlayback = default!;

    private readonly HashSet<EntityUid> _pendingSoundUpdates = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, AfterAutoHandleStateEvent>(OnHandleState);
        Subs.ItemStatus<GeigerComponent>(ent => ent.Comp.ShowControl ? new GeigerItemControl(ent) : null);
    }

    private void OnHandleState(EntityUid uid, GeigerComponent component, ref AfterAutoHandleStateEvent args)
    {
        component.UiUpdateNeeded = true;
        _pendingSoundUpdates.Add(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pendingSoundUpdates.Count == 0)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var uid in _pendingSoundUpdates)
        {
            if (!TryComp(uid, out GeigerComponent? component))
                continue;

            UpdateLocalSound(uid, component);
        }

        _pendingSoundUpdates.Clear();
    }

    private void UpdateLocalSound(EntityUid uid, GeigerComponent component)
    {
        component.Stream = _audio.Stop(component.Stream);

        if (!component.IsEnabled)
            return;

        if (component.User is not { } user)
            return;

        if (!component.Sounds.TryGetValue(component.DangerLevel, out var sounds))
            return;

        if (_replayPlayback.Replay != null)
            return;

        var param = sounds.Params.WithLoop(true).WithVolume(component.Volume);
        component.Stream = _audio.PlayLocal(sounds, uid, user, param)?.Entity;
    }
}
