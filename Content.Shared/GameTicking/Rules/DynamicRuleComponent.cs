using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.GameTicking.Rules;

/// <summary>
/// Gamerule the spawns multiple antags at intervals based on a budget
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class DynamicRuleComponent : Component
{
    /// <summary>
    /// The total budget for antags.
    /// </summary>
    [DataField]
    public float Budget;

    /// <summary>
    /// The last time budget was updated.
    /// </summary>
    [DataField]
    public TimeSpan LastBudgetUpdate;

    /// <summary>
    /// The amount of budget accumulated every second.
    /// </summary>
    [DataField]
    public float BudgetPerSecond = 0.1f;

    #region Starlight
    /// <summary>
    /// The amount of budget accumulated every second after reaching <see cref="DynamicRuleComponent.VariantBudgetThreshold"/>.
    /// </summary>
    [DataField]
    public float VariantBudgetPerSecond = 0.05f;
    #endregion

    /// <summary>
    /// The minimum or lower bound for budgets to start at.
    /// </summary>
    [DataField]
    public int StartingBudgetMin = 200;

    /// <summary>
    /// The maximum or upper bound for budgets to start at.
    /// </summary>
    [DataField]
    public int StartingBudgetMax = 350;

    #region Starlight
    /// <summary>
    /// The budget threshold for the variant budget to take effect.
    /// </summary>
    [DataField]
    public int VariantBudgetThreshold = 300;
    #endregion

    #region Blimpuf
    /// <summary>
    /// Do we want the budget to scale with the number of players?
    /// </summary>
    [DataField]
    public bool UsePlayerScaling = false;

    /// <summary>
    /// Do we want to roll rules until we have a certain amount of the budget used?
    /// </summary>
    [DataField]
    public bool UsePercentageBudgetStart = false;

    /// <summary>
    /// The percentage of the budget to be spent roundstart.
    /// </summary>
    [DataField]
    public float PercentageBudget = 0.5f;

    /// <summary>
    /// How much to multiply the budget by per player.
    /// </summary>
    [DataField]
    public int BudgetScalingPerPlayer = 10;

    /// <summary>
    /// The minimum amount of budget regardless of player count.
    /// </summary>
    [DataField]
    public int HardMinBudget = 100;

    /// <summary>
    /// The percentage of the per-player budget that should be added whenever the budget is updated.
    /// This amount is multiplied by the change in players (if it's positive) and then added to the budget.
    /// Requires UsePlayerScaling.
    /// </summary>
    [DataField]
    public float BudgetGainOnLateJoin = 0.0f;

    /// <summary>
    /// The highest amount of in-round players this gamerule has seen. Used for BudgetGainOnLateJoin.
    /// </summary>
    [DataField]
    public int HighestPlayers = 0;
    #endregion

    /// <summary>
    /// The time at which the next rule will start
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRuleTime;

    /// <summary>
    /// Minimum delay between rules
    /// </summary>
    [DataField]
    public TimeSpan MinRuleInterval = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Maximum delay between rules
    /// </summary>
    [DataField]
    public TimeSpan MaxRuleInterval = TimeSpan.FromMinutes(30);

    /// <summary>
    /// A table of rules that are picked from.
    /// </summary>
    [DataField]
    public EntityTableSelector Table = new NoneSelector();

    /// <summary>
    /// The rules that have been spawned
    /// </summary>
    [DataField]
    public List<EntityUid> Rules = new();
}
