using System.Collections.Generic;
using System.Linq;

namespace WorldRates.Rates;

internal enum RateGroup
{
    Experience,
    Income,
    Storage,
}

/// <summary>One adjustable multiplier. Ids are persisted in world files — never rename them.</summary>
internal sealed class RateDef
{
    public string Id { get; }
    public RateGroup Group { get; }
    public string Label { get; }
    public string Hint { get; }
    public float Min { get; }
    public float Max { get; }
    public float Step { get; }
    public bool IsGroupTotal { get; }

    public RateDef(string id, RateGroup group, string label, string hint, float min = 0f, float max = 10f,
        float step = 0.1f, bool isGroupTotal = false)
    {
        Id = id;
        Group = group;
        Label = label;
        Hint = hint;
        Min = min;
        Max = max;
        Step = step;
        IsGroupTotal = isGroupTotal;
    }
}

internal static class RateCatalog
{
    // Experience
    public const string XpAll = "xp.all";
    public const string XpDealPlayer = "xp.deal_player";
    public const string XpDealDealer = "xp.deal_dealer";
    public const string XpSample = "xp.sample";
    public const string XpCounterOffer = "xp.counter_offer";
    public const string XpHarvest = "xp.harvest";
    public const string XpMix = "xp.mix";
    public const string XpQuest = "xp.quest";
    public const string XpPolice = "xp.police";
    public const string XpGraffiti = "xp.graffiti";
    public const string XpPickpocket = "xp.pickpocket";
    public const string XpOther = "xp.other";

    // Income
    public const string IncomeAll = "income.all";
    public const string IncomeDealPlayer = "income.deal_player";
    public const string IncomeDealDealer = "income.deal_dealer";
    public const string IncomeLaundering = "income.laundering";
    public const string IncomePawnShop = "income.pawn_shop";
    public const string IncomeRecycling = "income.recycling";

    // Storage
    public const string StorageFurniture = "storage.furniture";
    public const string StorageVehicles = "storage.vehicles";

    public static readonly IReadOnlyList<RateDef> All = new[]
    {
        new RateDef(XpAll, RateGroup.Experience, "All experience", "Multiplies every XP gain (on top of the per-source rates).", isGroupTotal: true),
        new RateDef(XpDealPlayer, RateGroup.Experience, "Your deals", "Completing a deal in person (20 XP)."),
        new RateDef(XpDealDealer, RateGroup.Experience, "Dealer deals", "A hired dealer completes a deal (10 XP)."),
        new RateDef(XpSample, RateGroup.Experience, "Free samples", "A customer likes your free sample (50 XP)."),
        new RateDef(XpCounterOffer, RateGroup.Experience, "Counter-offers", "A customer accepts your counter-offer (5 XP)."),
        new RateDef(XpHarvest, RateGroup.Experience, "Harvesting", "Fully harvesting a plant (5 XP)."),
        new RateDef(XpMix, RateGroup.Experience, "New mixes", "Discovering and naming a new mix (80 XP)."),
        new RateDef(XpQuest, RateGroup.Experience, "Quests", "Completing a quest."),
        new RateDef(XpPolice, RateGroup.Experience, "Escaping police", "Losing the police (20-60 XP)."),
        new RateDef(XpGraffiti, RateGroup.Experience, "Graffiti", "Spraying or cleaning graffiti (25-50 XP)."),
        new RateDef(XpPickpocket, RateGroup.Experience, "Pickpocketing", "Successful pickpocket (2 XP)."),
        new RateDef(XpOther, RateGroup.Experience, "Other", "Any other XP source."),

        new RateDef(IncomeAll, RateGroup.Income, "All income", "Multiplies every income below (on top of its own rate).", isGroupTotal: true),
        new RateDef(IncomeDealPlayer, RateGroup.Income, "Your deals", "Cash a customer pays you in person, bonuses included."),
        new RateDef(IncomeDealDealer, RateGroup.Income, "Dealer sales", "Cash your dealers collect from customers."),
        new RateDef(IncomeLaundering, RateGroup.Income, "Laundering", "Money returned by business laundering."),
        new RateDef(IncomePawnShop, RateGroup.Income, "Pawn shop", "Selling items at the pawn shop."),
        new RateDef(IncomeRecycling, RateGroup.Income, "Recycling", "Cash from the recycler."),

        new RateDef(StorageFurniture, RateGroup.Storage, "Placed storage", "Racks, shelves, safes and other placeable storage. Max 20 slots.", 1f, 4f, 0.25f),
        new RateDef(StorageVehicles, RateGroup.Storage, "Vehicle trunks", "Trunk capacity of vehicles. Max 20 slots.", 1f, 4f, 0.25f),
    };

    private static readonly Dictionary<string, RateDef> ById = All.ToDictionary(d => d.Id);

    public static RateDef? Find(string id) => ById.TryGetValue(id, out var def) ? def : null;

    public static IEnumerable<RateDef> InGroup(RateGroup group) => All.Where(d => d.Group == group);
}
