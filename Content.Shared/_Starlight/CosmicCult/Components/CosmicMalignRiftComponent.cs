using Content.Shared.DoAfter;

namespace Content.Shared._Starlight.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicMalignRiftComponent : Component
{
    public DoAfterId? DoAfterId = null;

    [DataField] public bool Used;

    [DataField] public bool Occupied;

    [DataField] public TimeSpan AbsorbTime = TimeSpan.FromSeconds(30);
    [DataField] public float DistanceThreshold = 1.5f;

    [DataField] public float MovementThreshold = 0.5f;
}
