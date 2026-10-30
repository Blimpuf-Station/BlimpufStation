using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Blimpuf.Chemistry.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SqueezableComponent : Component
{
    [DataField(required: true)] public string Solution = string.Empty;

    [DataField] public TimeSpan SqueezeDuration = TimeSpan.FromSeconds(2);

    [DataField] public LocId SqueezeVerb = "squeezable-verb";

    [DataField] public LocId SqueezeStartMessage = "squeezable-begin";

    [DataField] public DamageSpecifier? SqueezeDamage;

    [DataField] public SoundSpecifier SqueezeSound = new SoundPathSpecifier("/Audio/Voice/Slime/slime_squish.ogg");
}

[Serializable, NetSerializable]
public sealed partial class SqueezableTransferDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SqueezableSpillDoAfterEvent : SimpleDoAfterEvent;
