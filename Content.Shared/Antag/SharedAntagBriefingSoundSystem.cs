using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Antag;

[Serializable, NetSerializable]
public sealed class AntagBriefingSoundEvent : EntityEventArgs
{
    public SoundSpecifier Sound { get; }

    public AntagBriefingSoundEvent(SoundSpecifier sound)
    {
        Sound = sound;
    }
}
