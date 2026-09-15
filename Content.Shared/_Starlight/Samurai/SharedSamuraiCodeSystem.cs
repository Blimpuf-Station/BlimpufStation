using Robust.Shared.Prototypes;
using Content.Shared.Dataset;

namespace Content.Shared._Starlight.Samurai;

public abstract partial class SharedSamuraiCodeSystem : EntitySystem
{
    public static readonly ProtoId<DatasetPrototype> BaseDataset = "SamuraiCodesBase";
    public static readonly ProtoId<DatasetPrototype> ErraticDataset = "SamuraiCodesErratic";
    public static readonly ProtoId<DatasetPrototype> HostileDataset = "SamuraiCodesHostile";
}
