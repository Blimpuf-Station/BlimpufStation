using Content.Client.UserInterface.RichText;
using Content.Shared.Antag;
using Robust.Client.Player;
using Robust.Client.Replays.Playback;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Client.Antag;

public sealed class AntagBriefingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private IReplayPlaybackManager _replayPlayback = default!;

    private readonly Queue<SoundSpecifier> _pendingSounds = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AntagBriefingSoundEvent>(OnBriefingSound);
    }

    private void OnBriefingSound(AntagBriefingSoundEvent ev)
    {
        _pendingSounds.Enqueue(ev.Sound);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pendingSounds.Count == 0)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_playerManager.LocalEntity is not { } entity)
            return;

        while (_pendingSounds.Count > 0)
        {
            var sound = _pendingSounds.Dequeue();

            if (_replayPlayback.Replay != null)
                return;

            _audio.PlayLocal(sound, entity, entity);
        }
    }
}
