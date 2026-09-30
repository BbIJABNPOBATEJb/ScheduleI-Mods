using System;
using System.Collections.Generic;
using MelonLoader;
using S1Shared;

namespace GuideArrows.Targets;

/// <summary>Collects every candidate target from the game's own lists (refreshed a few times a second).</summary>
internal static class TargetScanner
{
    private const float Interval = 0.4f;

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
            var customer = contract.Customer != null ? UiKit.Get<S1.NPCs.NPC>(contract.Customer) : null;
            if (customer != null)
                label = customer.FirstName;
            Found.Add(new Target
            {
                Kind = TargetKind.Deal,
                Key = "deal:" + contract.GetInstanceID(),
                Label = label,
                Anchor = location.CustomerStandPoint,
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
                Found.Add(new Target
                {
                    Kind = TargetKind.Quest,
                    Key = "quest:" + entry.GetInstanceID(),
                    Label = entry.Title,
                    Anchor = entry.PoILocation,
                });
            }
        }
    }

    /// <summary>The people the map marks as potential customers.</summary>
    private static void ScanCustomers()
    {
        foreach (var customer in UnityQuery.ToManaged(S1.Economy.Customer.LockedCustomers))
        {
            if (customer == null)
                continue;
            var poi = customer.potentialCustomerPoI;
            var npc = customer.NPC;
            if (poi == null || !poi.enabled || npc == null || !npc.IsConscious)
                continue;
            Found.Add(new Target
            {
                Kind = TargetKind.Customer,
                Key = "customer:" + npc.GetInstanceID(),
                Label = npc.FirstName,
                Anchor = npc.transform,
            });
        }
    }

    private static bool IsDealOrDrop(S1.Quests.Quest quest)
    {
#if IL2CPP
        return quest.TryCast<S1.Quests.Contract>() != null || quest.TryCast<S1.Quests.DeaddropQuest>() != null;
#else
        return quest is S1.Quests.Contract || quest is S1.Quests.DeaddropQuest;
#endif
    }
}
