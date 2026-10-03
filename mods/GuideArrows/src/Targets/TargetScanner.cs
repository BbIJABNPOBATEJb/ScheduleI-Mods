using System;
using System.Collections.Generic;
using MelonLoader;
using S1Shared;
using UnityEngine;
#if !IL2CPP
using System.Reflection;
using HarmonyLib;
#endif

namespace GuideArrows.Targets;

/// <summary>Collects every candidate target from the game's own lists (refreshed a few times a second).</summary>
internal static class TargetScanner
{
    private const float Interval = 0.4f;

    /// <summary>In-game minutes a customer will not buy again after a deal or a declined offer (Customer.OfferDealValid).</summary>
    private const int DealCooldownMinutes = 360;

    /// <summary>A deal's customer glows once they are this close to the meeting point (meters).</summary>
    private const float DealCustomerNear = 20f;

    private static readonly List<Target> Found = new();
    private static float _next;
    private static bool _warned;

    public static IReadOnlyList<Target> Targets => Found;

    public static void Tick(float now, bool force = false)
    {
        if (!force && now < _next)
            return;
        _next = now + Interval;
        Found.Clear();
        if (S1.PlayerScripts.Player.Local == null)
            return;
        Scan(TargetKind.Deal, ScanDeals);
        Scan(TargetKind.Buyer, ScanBuyers);
        Scan(TargetKind.Stash, ScanStashes);
        Scan(TargetKind.Quest, ScanQuests);
        Scan(TargetKind.Customer, ScanCustomers);
        Scan(TargetKind.Home, () =>
        {
            var home = HomeTracker.Current((HomeMode)Config.HomeMode.Value);
            if (home != null)
                Found.Add(home);
        });
    }

    private static void Scan(TargetKind kind, Action scan)
    {
        if (!Config.KindEnabled(kind))
            return;
        try
        {
            scan();
        }
        catch (Exception ex)
        {
            if (_warned)
                return;
            _warned = true;
            MelonLogger.Warning($"Scanning {kind} targets failed: {ex}");
        }
    }

    /// <summary>Deliveries you accepted yourself (dealers' deals are theirs).</summary>
    private static void ScanDeals()
    {
        foreach (var contract in UnityQuery.ToManaged(S1.Quests.Contract.Contracts))
        {
            if (contract == null || contract.State != S1.Quests.EQuestState.Active || contract.Dealer != null)
                continue;
            var location = contract.DeliveryLocation;
            if (location == null || location.CustomerStandPoint == null)
                continue;
            var label = "";
            GameObject? highlight = null;
            var customer = contract.Customer != null ? UiKit.Get<S1.NPCs.NPC>(contract.Customer) : null;
            if (customer != null)
            {
                label = customer.FirstName;
                // The customer glows once they are waiting there, not on their way across town.
                if (Vector3.Distance(customer.transform.position, location.CustomerStandPoint.position) < DealCustomerNear)
                    highlight = Body(customer);
            }
            Found.Add(new Target
            {
                Kind = TargetKind.Deal,
                Key = "deal:" + contract.GetInstanceID(),
                Label = label,
                Anchor = location.CustomerStandPoint,
                Highlight = highlight,
            });
        }
    }

    /// <summary>Dead drops with something in them.</summary>
    private static void ScanStashes()
    {
        foreach (var drop in UnityQuery.ToManaged(S1.Economy.DeadDrop.DeadDrops))
        {
            if (drop == null || drop.Storage == null || drop.Storage.ItemCount <= 0)
                continue;
            Found.Add(new Target
            {
                Kind = TargetKind.Stash,
                Key = "stash:" + drop.GetInstanceID(),
                Label = drop.DeadDropName,
                Anchor = drop.transform,
                Highlight = drop.gameObject,
            });
        }
    }

    /// <summary>Active objectives of story/side quests (deals and dead-drop pickups have their own kinds).</summary>
    private static void ScanQuests()
    {
        foreach (var quest in UnityQuery.ToManaged(S1.Quests.Quest.Quests))
        {
            if (quest == null || quest.State != S1.Quests.EQuestState.Active || IsDealOrDrop(quest))
                continue;
            if (Config.QuestsTrackedOnly.Value && !quest.IsTracked)
                continue;
            foreach (var entry in UnityQuery.ToManaged(quest.Entries))
            {
                if (entry == null || entry.State != S1.Quests.EQuestState.Active || entry.PoILocation == null)
                    continue;
                // An objective that follows a person (talk to someone) highlights them; places are not outlined.
                var person = UnityQuery.GetComponentInParent<S1.NPCs.NPC>(entry.PoILocation);
                Found.Add(new Target
                {
                    Kind = TargetKind.Quest,
                    Key = "quest:" + entry.GetInstanceID(),
                    Label = entry.Title,
                    Anchor = entry.PoILocation,
                    Highlight = person != null ? Body(person) : null,
                });
            }
        }
    }

    /// <summary>
    /// The people the map marks as potential customers, if a free sample can be offered to them now
    /// (Customer.ShowDirectApproachOption and SampleOptionValid): not those who turned you down today.
    /// </summary>
    private static void ScanCustomers()
    {
        foreach (var customer in UnityQuery.ToManaged(S1.Economy.Customer.LockedCustomers))
        {
            if (customer == null)
                continue;
            // Enabled for people who are not customers yet but know one of your customers.
            var poi = customer.potentialCustomerPoI;
            var npc = customer.NPC;
            if (poi == null || !poi.enabled || npc == null || !CanTalkTo(npc))
                continue;
            var data = customer.CustomerData;
            if (data == null || !data.CanBeDirectlyApproached || customer.IsAwaitingDelivery)
                continue;
            if (SampleOfferedToday(customer) || !RegionUnlocked(npc))
                continue;
            Found.Add(new Target
            {
                Kind = TargetKind.Customer,
                Key = "customer:" + npc.GetInstanceID(),
                Label = npc.FirstName,
                Anchor = npc.transform,
                Highlight = Body(npc),
            });
        }
    }

    /// <summary>
    /// Your own customers who would take a deal offered in person right now (Customer.ShowOfferDealOption
    /// and OfferDealValid), leaving out those your dealers serve.
    /// </summary>
    private static void ScanBuyers()
    {
        foreach (var customer in UnityQuery.ToManaged(S1.Economy.Customer.UnlockedCustomers))
        {
            if (customer == null)
                continue;
            var npc = customer.NPC;
            if (npc == null || npc.RelationData == null || !npc.RelationData.Unlocked || !CanTalkTo(npc))
                continue;
            if (customer.AssignedDealer != null || customer.CurrentContract != null || customer.OfferedContractInfo != null
                || customer.IsAwaitingDelivery)
                continue;
            if (customer.TimeSinceLastDealCompleted < DealCooldownMinutes)
                continue;
            if (customer.TimeSinceInstantDealOffered < DealCooldownMinutes && !PendingInstantDeal(customer))
                continue;
            Found.Add(new Target
            {
                Kind = TargetKind.Buyer,
                Key = "buyer:" + npc.GetInstanceID(),
                Label = npc.FirstName,
                Anchor = npc.transform,
                Highlight = Body(npc),
            });
        }
    }

    /// <summary>Someone you can walk up to: awake, outdoors and not driving.</summary>
    private static bool CanTalkTo(S1.NPCs.NPC npc) => npc.IsConscious && !npc.isInBuilding && !npc.IsInVehicle;

    private static GameObject Body(S1.NPCs.NPC npc) => npc.Avatar != null ? npc.Avatar.gameObject : npc.gameObject;

    /// <summary>Samples can only be offered in regions you have unlocked (not checked in the tutorial).</summary>
    private static bool RegionUnlocked(S1.NPCs.NPC npc)
    {
        if (S1.DevUtilities.GameManager.IS_TUTORIAL)
            return true;
        var map = S1.Map.Map.Instance;
        var region = map != null ? map.GetRegionData(npc.Region) : null;
        return region == null || region.IsUnlocked;
    }

#if IL2CPP
    private static bool SampleOfferedToday(S1.Economy.Customer customer) => customer.sampleOfferedToday;

    private static bool PendingInstantDeal(S1.Economy.Customer customer) => customer.pendingInstantDeal;
#else
    private static readonly FieldInfo? SampleOfferedTodayField = AccessTools.Field(typeof(S1.Economy.Customer), "sampleOfferedToday");
    private static readonly FieldInfo? PendingInstantDealField = AccessTools.Field(typeof(S1.Economy.Customer), "pendingInstantDeal");

    private static bool SampleOfferedToday(S1.Economy.Customer customer) =>
        SampleOfferedTodayField != null && (bool)SampleOfferedTodayField.GetValue(customer);

    private static bool PendingInstantDeal(S1.Economy.Customer customer) =>
        PendingInstantDealField != null && (bool)PendingInstantDealField.GetValue(customer);
#endif

    private static bool IsDealOrDrop(S1.Quests.Quest quest)
    {
#if IL2CPP
        return quest.TryCast<S1.Quests.Contract>() != null || quest.TryCast<S1.Quests.DeaddropQuest>() != null;
#else
        return quest is S1.Quests.Contract || quest is S1.Quests.DeaddropQuest;
#endif
    }
}
