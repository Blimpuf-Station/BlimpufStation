using Content.Shared.Chemistry.Components;

namespace Content.Shared._Blimpuf.Chemistry.Components;

[RegisterComponent]
public sealed partial class MultiSolutionRegenerationComponent : Component
{
    [DataField(required: true)] public List<MultiSolutionRegenerationEntry> Solutions = [];
}

[DataDefinition]
public sealed partial class MultiSolutionRegenerationEntry
{
    [DataField(required: true)] public string Solution = string.Empty;

    [DataField(required: true)] public Solution Generated = default!;

    [DataField] public TimeSpan Duration = TimeSpan.FromSeconds(1);

    [DataField] public bool PauseWhenDead = false;

    public Entity<SolutionComponent>? SolutionRef;

    public TimeSpan NextRegenTime;
}
