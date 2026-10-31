using Content.Server._Blimpuf.Traitor.Systems;
using Content.Shared.Store;

namespace Content.Server._Blimpuf.Store.Conditions;

public sealed partial class SyndicateResearchCondition : ListingCondition
{
    public override bool Condition(ListingConditionArgs args)
    {
        var research = args.EntityManager.System<SyndicateResearchSystem>();

        return research.IsResearched(args.Listing.ID);
    }
}
