using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using WorldRates.Rates;

namespace WorldRates.Patches;

/// <summary>
/// How often customers order: from you by text message, or from the dealer they are assigned to
/// (which is what decides how fast a dealer sells his stock).
///
/// The game decides this every in-game minute in Customer.OnMinPass: the customer orders on a few
/// days of the week (1–7, from addiction and relationship), inside a two-hour window, and not sooner
/// than 10–13 hours after the previous deal. The order size is the weekly budget divided by the
/// number of order days.
///
/// OnMinPass is the one reliably hookable point (virtual, called through a delegate; the private
/// helpers may be inlined on IL2CPP). So for the duration of that call the customer's schedule data is
/// swapped for values that make the game's own logic say "order now" or "not now" according to the
/// scaled schedule, and everything is put back right after. The order size is kept as in vanilla, so
/// ×2 means twice as many orders of the usual size.
/// </summary>
internal static class DemandPatches
{
    private const int WindowMinutes = 120;
    private const int MinutesPerDay = 1440;
    private const int MaxWindowsPerDay = MinutesPerDay / WindowMinutes;

    /// <summary>Above the game's longest cooldown (780), below the 1440 its other checks look at.</summary>
    private const int CooldownPassed = 800;

    private static readonly MethodInfo? SetCompleted =
        AccessTools.PropertySetter(typeof(S1.Economy.Customer), nameof(S1.Economy.Customer.TimeSinceLastDealCompleted));
    private static readonly MethodInfo? SetOffered =
        AccessTools.PropertySetter(typeof(S1.Economy.Customer), nameof(S1.Economy.Customer.TimeSinceLastDealOffered));

    /// <summary>What was swapped for one OnMinPass call.</summary>
    private sealed class Swap
    {
        public S1.Economy.CustomerData Data = null!;
        public int OrderTime, MinOrders, MaxOrders;
        public float MinSpend, MaxSpend;
        public bool CountersRaised;
        public int Completed, Offered;
    }

    /// <summary>The number of days a week the game lets a customer order (CustomerData.GetOrderDays).</summary>
    internal static int VanillaOrdersPerWeek(int minOrders, int maxOrders, float dependence, float relationship)
    {
        var perWeek = Mathf.RoundToInt(Mathf.Lerp(minOrders, maxOrders, Mathf.Max(dependence, relationship)));
        var interval = Mathf.Max(Mathf.RoundToInt(7f / perWeek), 1);
        return 6 / interval + 1;
    }

    /// <summary>
    /// Two-hour order windows a customer with <paramref name="ordersPerWeek"/> vanilla order days gets on
    /// a day under the rate. The scaled weekly number is spread evenly: below one a day some days get
    /// none (for low rates the gaps span weeks), above it a day gets several.
    /// </summary>
    internal static int WindowsOn(int ordersPerWeek, float rate, int day)
    {
        var perDay = ordersPerWeek * Math.Round(rate, 2) / 7.0;
        if (perDay <= 0.0)
            return 0;
        var windows = (int)(Math.Ceiling((day + 1) * perDay - 1e-6) - Math.Ceiling(day * perDay - 1e-6));
        return Mathf.Clamp(windows, 0, MaxWindowsPerDay);
    }

    /// <summary>Whether this minute is inside one of the day's order windows (the first is at the customer's order time).</summary>
    internal static bool IsOrderSlot(int ordersPerWeek, float rate, int day, int minuteOfDay, int orderMinuteOfDay)
    {
        var windows = WindowsOn(ordersPerWeek, rate, day);
        if (windows == 0)
            return false;
        var spacing = MinutesPerDay / windows;
        var sinceFirst = (minuteOfDay - orderMinuteOfDay + MinutesPerDay) % MinutesPerDay;
        return sinceFirst % spacing <= WindowMinutes && sinceFirst / spacing < windows;
    }

    /// <summary>Minutes a customer waits after a deal before ordering again (600–780 in the game).</summary>
    internal static int Cooldown(char firstLetter, float rate) =>
        Mathf.Max(Mathf.RoundToInt((600 + firstLetter % 10 * 20) / rate), WindowMinutes + 5);

    [HarmonyPatch(typeof(S1.Economy.Customer), "OnMinPass")]
    private static class MinPass
    {
        private static void Prefix(S1.Economy.Customer __instance, out Swap? __state)
        {
            __state = null;
            try
            {
                __state = Apply(__instance);
            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }

        private static void Finalizer(S1.Economy.Customer __instance, Swap? __state)
        {
            if (__state == null)
                return;
            try
            {
                Restore(__instance, __state);
            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }
    }

    private static Swap? Apply(S1.Economy.Customer customer)
    {
        if (!RatesState.InWorld || !RatesState.IsAuthority)
            return null;
        var rate = RatesState.Current.Get(customer.AssignedDealer != null ? RateCatalog.DealsDealers : RateCatalog.DealsRequests);
        if (Mathf.Approximately(rate, 1f))
            return null;
        var npc = customer.NPC;
        var data = customer.CustomerData;
        // Locked customers only buy from the cartel; pending offers and running deals are left alone.
        if (npc == null || data == null || !npc.RelationData.Unlocked || customer.CurrentContract != null
            || customer.OfferedContractInfo != null)
            return null;
        var time = S1.GameTime.TimeManager.Instance;
        if (time == null)
            return null;

        var allow = false;
        var perWeek = 1;
        if (rate > 0f)
        {
            perWeek = VanillaOrdersPerWeek(data.MinOrdersPerWeek, data.MaxOrdersPerWeek, customer.CurrentAddiction,
                npc.RelationData.RelationDelta / 5f);
            // Day 0 of the customer's own calendar falls on his preferred order day, as in the game.
            var day = time.ElapsedDays + (7 - (int)data.PreferredOrderDay) % 7;
            var cooldown = Cooldown(npc.FirstName[0], rate);
            allow = customer.TimeSinceLastDealCompleted >= cooldown && customer.TimeSinceLastDealOffered >= cooldown
                && IsOrderSlot(perWeek, rate, day, S1.GameTime.TimeManager.GetMinSumFrom24HourTime(time.CurrentTime),
                    S1.GameTime.TimeManager.GetMinSumFrom24HourTime(data.OrderTime));
        }

        var swap = new Swap
        {
            Data = data,
            OrderTime = data.OrderTime,
            MinOrders = data.MinOrdersPerWeek,
            MaxOrders = data.MaxOrdersPerWeek,
            MinSpend = data.MinWeeklySpend,
            MaxSpend = data.MaxWeeklySpend,
        };
        if (!allow)
        {
            // Order window twelve hours away: the game's own check says "not now".
            data.OrderTime = S1.GameTime.TimeManager.AddMinutesTo24HourTime(time.CurrentTime, 720);
            return swap;
        }

        // Order window around "now" on every day of the week. The weekly budget grows with the number
        // of order days, so that budget / days — the order size — stays what it was.
        data.OrderTime = S1.GameTime.TimeManager.AddMinutesTo24HourTime(time.CurrentTime, -1);
        data.MinOrdersPerWeek = 7;
        data.MaxOrdersPerWeek = 7;
        data.MinWeeklySpend = swap.MinSpend * 7f / perWeek;
        data.MaxWeeklySpend = swap.MaxSpend * 7f / perWeek;

        swap.Completed = customer.TimeSinceLastDealCompleted;
        swap.Offered = customer.TimeSinceLastDealOffered;
        if (swap.Completed < CooldownPassed || swap.Offered < CooldownPassed)
        {
            swap.CountersRaised = true;
            SetCounter(SetCompleted, customer, Mathf.Max(swap.Completed, CooldownPassed));
            SetCounter(SetOffered, customer, Mathf.Max(swap.Offered, CooldownPassed));
        }
        return swap;
    }

    private static void Restore(S1.Economy.Customer customer, Swap swap)
    {
        var data = swap.Data;
        data.OrderTime = swap.OrderTime;
        data.MinOrdersPerWeek = swap.MinOrders;
        data.MaxOrdersPerWeek = swap.MaxOrders;
        data.MinWeeklySpend = swap.MinSpend;
        data.MaxWeeklySpend = swap.MaxSpend;
        if (!swap.CountersRaised)
            return;
        // OnMinPass added a minute to both counters; an offer made during the call reset "offered" to 0.
        if (customer.TimeSinceLastDealCompleted == Mathf.Max(swap.Completed, CooldownPassed) + 1)
            SetCounter(SetCompleted, customer, swap.Completed + 1);
        if (customer.TimeSinceLastDealOffered == Mathf.Max(swap.Offered, CooldownPassed) + 1)
            SetCounter(SetOffered, customer, swap.Offered + 1);
    }

    private static void SetCounter(MethodInfo? setter, S1.Economy.Customer customer, int value) =>
        setter?.Invoke(customer, new object[] { value });

    private static bool _warned;

    /// <summary>True once applying the rate threw (it is then reported once in the log).</summary>
    internal static bool Failed => _warned;

    private static void Warn(Exception ex)
    {
        if (_warned)
            return;
        _warned = true;
        MelonLogger.Warning($"Deal frequency rate could not be applied: {ex}");
    }
}
