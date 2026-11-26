using Content.Shared.EntityTable;
using Content.Shared.EntityTable.Conditions;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using Content.Shared._Starlight.EntityTable;

namespace Content.Shared._Blimpuf.EntityTable;

/// <summary>
/// Condition that succeeds only when the specified gamerules have already run.
/// </summary>
public sealed partial class RequiresOtherRuleCondition : EntityTableCondition
{
    /// <summary>
    /// All previously-run rules in this list that are required to pass.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Rules = new();

    protected override bool EvaluateImplementation(EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        if (Rules.Count == 0)
            return false;

        if (ctx.TryGetData<GameRuleTableContext>(out var gameRuleContext))
        {
            foreach (var ruleId in Rules)
            {
                if (!gameRuleContext.Contains(ruleId))
                    return false;
            }

            return true;
        }

        var gameTicker = entMan.System<SharedGameTicker>();

        foreach (var ruleId in Rules)
        {
            var exists = false;

            foreach (var previousRule in gameTicker.AllPreviousGameRules)
            {
                if (previousRule.Item2 == ruleId.Id)
                    exists = true;
            }

            if (!exists)
                return false;
        }

        return false;
    }
}
