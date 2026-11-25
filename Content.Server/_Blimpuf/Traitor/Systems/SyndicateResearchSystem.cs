using System.Linq;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.StoreDiscount.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Blimpuf.Traitor.Systems;

public sealed partial class SyndicateResearchSystem : EntitySystem
{
    [Dependency] private readonly StoreSystem _store = default!;

    public int RequiredResearch { get; private set; }
    private readonly HashSet<(EntityUid Store, string ListingId)> _freeResearch = new();
    private const string FreeResearchKey = "SyndicateResearchFree";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnBuyFinished);
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
    public void Research(string id, EntityUid researcher)
    {
        _completedResearch.Add(id);
        GrantFreeListing(researcher, id);
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
        _freeResearch.Clear();
    }

    /// <summary>
    /// Makes the listing free (one purchase) on every store belonging to the researcher.
    /// </summary>
    private void GrantFreeListing(EntityUid researcher, string listingId)
    {
        var query = EntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var storeUid, out var store))
        {
            if (!BelongsTo(storeUid, store, researcher))
                continue;

            var listing = store.FullListingsCatalog.FirstOrDefault(l => l.ID == listingId);
            if (listing == null)
                continue;

            if (!_freeResearch.Add((storeUid, listingId)))
                continue;

            var modifier = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>();
            foreach (var (currency, cost) in listing.OriginalCost)
            {
                modifier[currency] = -cost;
            }

            listing.AddCostModifier(FreeResearchKey, modifier);
            _store.UpdateUserInterface(researcher, storeUid, store);
        }
    }

    /// <summary>
    /// Removes the free modifier after the first purchase so it's a one-time reward.
    /// </summary>
    private void OnBuyFinished(ref StoreBuyFinishedEvent ev)
    {
        var (storeUid, purchased) = ev;

        if (!_freeResearch.Remove((storeUid, purchased.ID)))
            return;

        purchased.RemoveCostModifier(FreeResearchKey);
    }

    private bool BelongsTo(EntityUid storeUid, StoreComponent store, EntityUid researcher)
    {
        if (store.AccountOwner == researcher)
            return true;

        var current = storeUid;
        while (current.IsValid())
        {
            if (current == researcher)
                return true;

            var parent = Transform(current).ParentUid;
            if (!parent.IsValid() || parent == current)
                break;

            current = parent;
        }

        return false;
    }
}
