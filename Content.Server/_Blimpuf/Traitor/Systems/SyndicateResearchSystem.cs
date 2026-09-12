using Content.Shared.GameTicking;

namespace Content.Server._Blimpuf.Traitor.Systems;

public sealed partial class SyndicateResearchSystem : EntitySystem
{
    public int RequiredResearch { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    /// <summary>
    /// System for tracking researched items.
    /// </summary>
    private readonly HashSet<string> _completedResearch = new();

    /// <summary>
    /// Method for getting a count of research items completed
    /// </summary>
    public int ResearchCount => _completedResearch.Count;

    /// <summary>
    /// Adds a completed research unlock.
    /// </summary>
    public void Research(string id)
    {
        _completedResearch.Add(id);
    }

    /// <summary>
    /// Checks whether an unlock exists.
    /// </summary>
    public bool IsResearched(string id)
    {
        return _completedResearch.Contains(id);
    }

    /// <summary>
    /// Clears all Researched content every time the round restarts.
    /// </summary>
    /// <param name="ev"></param>
    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _completedResearch.Clear();
    }
}
