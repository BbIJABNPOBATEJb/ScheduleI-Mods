#if DEV
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using S1Shared;
using S1Shared.Dev;
using UnityEngine;
using WorldRates.Patches;
using WorldRates.Rates;
using WorldRates.UI;

namespace WorldRates.Dev;

/// <summary>Smoke scenarios for tools/run-smoke.ps1 -Mod WorldRates -Scenario &lt;name&gt;.</summary>
internal static class RatesSmoke
{
    public static IEnumerator Run()
    {
        IEnumerator? body = DevSmoke.Scenario switch
        {
            "inspect" => Inspect(),
            "ui" => Ui(),
            "income" => Income(),
            "storage-save" => StorageSave(),
            "complete-contract" => CompleteContract(),
            "idle" => Idle(),
            "deals" => Deals(),
            "perf" => Perf(),
            _ => null,
        };
        if (body == null)
            DevSmoke.Finish(false, "Unknown scenario " + DevSmoke.Scenario);
        var runner = new CoRunner(ex => DevSmoke.Finish(false, ex.ToString()));
        if (body != null)
            runner.Start(body);
        while (true)
        {
            runner.Tick();
            yield return null;
        }
    }

    /// <summary>How long the periodic storage scan takes (it runs every 15 seconds while playing).</summary>
    private static IEnumerator Perf()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return 5f;
        var watch = new System.Diagnostics.Stopwatch();
        for (var i = 0; i < 4; i++)
        {
            watch.Restart();
            var found = UnityQuery.FindInScenes<S1.Storage.StorageEntity>().Count;
            var find = watch.Elapsed.TotalMilliseconds;
            watch.Restart();
            StoragePatches.ApplyAll();
            var apply = watch.Elapsed.TotalMilliseconds;
            watch.Restart();
            var described = StoragePatches.Describe().Length;
            var describe = watch.Elapsed.TotalMilliseconds;
            DevSmoke.Log($"scan {i}: {found} storages found in {find:0.0} ms, ApplyAll {apply:0.0} ms, Describe {describe:0.0} ms ({described} chars)");
            DevSmoke.Check(apply < 250, $"the storage scan takes {apply:0} ms");
            yield return 2f;
        }
        DevSmoke.Finish(true, "perf done");
    }

    /// <summary>Just lets the (copied) world run for a while: dealers deal, employees work.</summary>
    private static IEnumerator Idle()
    {
        yield return DevSmoke.LoadDisposableSave();
        for (var i = 0; i < 9; i++)
        {
            yield return 5f;
            DevSmoke.Log($"alive {i * 5 + 5}s");
        }
        DevSmoke.Finish(true, "idle done");
    }

    // --- deal frequency ------------------------------------------------------------------------------

    private static readonly System.Reflection.MethodInfo MinPass =
        HarmonyLib.AccessTools.Method(typeof(S1.Economy.Customer), "OnMinPass");
    private static readonly System.Reflection.MethodInfo SetCompleted =
        HarmonyLib.AccessTools.PropertySetter(typeof(S1.Economy.Customer), "TimeSinceLastDealCompleted");
    private static readonly System.Reflection.MethodInfo SetOffered =
        HarmonyLib.AccessTools.PropertySetter(typeof(S1.Economy.Customer), "TimeSinceLastDealOffered");

    private static string Schedule(S1.Economy.CustomerData d) =>
        $"{d.OrderTime}/{d.MinOrdersPerWeek}/{d.MaxOrdersPerWeek}/{d.MinWeeklySpend:0.##}/{d.MaxWeeklySpend:0.##}";

    private static bool HasDeal(S1.Economy.Customer c) => c.OfferedContractInfo != null || c.CurrentContract != null;

    private static void SetCounters(S1.Economy.Customer c, int minutes)
    {
        SetCompleted.Invoke(c, new object[] { minutes });
        SetOffered.Invoke(c, new object[] { minutes });
    }

    /// <summary>One game minute for one customer, the way the game's clock calls it. True when a deal was offered.</summary>
    private static bool Minute(S1.Economy.Customer c)
    {
        // The game offers at most one deal per five minutes over all customers.
        foreach (var other in UnityQuery.ToManaged(S1.Economy.Customer.UnlockedCustomers))
            if (other.TimeSinceLastDealOffered < 5)
                SetOffered.Invoke(other, new object[] { 100 });
        var schedule = Schedule(c.CustomerData);
        MinPass.Invoke(c, null);
        DevSmoke.Check(Schedule(c.CustomerData) == schedule, $"{c.NPC.FirstName}: schedule not restored, {schedule} -> {Schedule(c.CustomerData)}");
        return HasDeal(c);
    }

    private static int OrdersPerWeek(S1.Economy.Customer c) => DemandPatches.VanillaOrdersPerWeek(c.CustomerData.MinOrdersPerWeek,
        c.CustomerData.MaxOrdersPerWeek, c.CurrentAddiction, c.NPC.RelationData.RelationDelta / 5f);

    /// <summary>Sets the clock; the method was renamed between game versions (SetTimeAndSync in 0.4.6, SetTime_Server in 0.4.7).</summary>
    private static void SetClock(S1.GameTime.TimeManager time, int hhmm)
    {
        var method = typeof(S1.GameTime.TimeManager).GetMethod("SetTimeAndSync") ?? typeof(S1.GameTime.TimeManager).GetMethod("SetTime_Server");
        DevSmoke.Check(method != null, "no method to set the game clock");
        method!.Invoke(time, new object[] { hhmm });
    }

    private static void SetRates(float requests, float dealers)
    {
        RatesState.Current.Set(RateCatalog.DealsRequests, requests);
        RatesState.Current.Set(RateCatalog.DealsDealers, dealers);
    }

    /// <summary>With -Save of a progressed save: order windows, and the game's own minute logic under different rates.</summary>
    private static IEnumerator Deals()
    {
        // Order windows over four weeks match the scaled weekly number.
        foreach (var perWeek in new[] { 1, 2, 4, 7 })
        {
            foreach (var rate in new[] { 0f, 0.3f, 0.5f, 1f, 2f, 3f, 10f })
            {
                var windows = 0;
                var minutes = 0;
                for (var day = 0; day < 28; day++)
                {
                    windows += DemandPatches.WindowsOn(perWeek, rate, day);
                    for (var m = 0; m < 1440; m++)
                        if (DemandPatches.IsOrderSlot(perWeek, rate, day, m, 1200))
                            minutes++;
                }
                var expected = Math.Min(perWeek * rate * 4f, 28 * 12);
                DevSmoke.Log($"{perWeek}/week x{rate}: {windows} windows in 4 weeks (expected {expected:0.#}), {minutes} open minutes");
                DevSmoke.Check(Math.Abs(windows - expected) < 1f, $"{perWeek}/week x{rate}: {windows} windows, expected {expected}");
                DevSmoke.Check(minutes == windows * 121 || windows == 28 * 12, $"{perWeek}/week x{rate}: {minutes} open minutes for {windows} windows");
            }
        }
        DevSmoke.Check(DemandPatches.Cooldown('A', 1f) == 700 && DemandPatches.Cooldown('A', 2f) == 350
            && DemandPatches.Cooldown('A', 10f) == 125 && DemandPatches.Cooldown('A', 0.5f) == 1400, "cooldown scaling");

        yield return DevSmoke.LoadDisposableSave();
        yield return 2f;
        var time = S1.GameTime.TimeManager.Instance;
        // Freeze the clock: from here minutes pass only when the test says so.
        time.SetTimeSpeedMultiplier(0f);
        yield return 3f;

        var customers = UnityQuery.ToManaged(S1.Economy.Customer.UnlockedCustomers);
        var free = customers.Where(c => !HasDeal(c) && c.NPC.IsConscious).ToList();
        var own = free.Where(c => c.AssignedDealer == null).ToList();
        var dealt = free.Where(c => c.AssignedDealer != null).ToList();
        DevSmoke.Log($"{customers.Count} unlocked customers: {own.Count} free of yours, {dealt.Count} free with dealers; day {time.ElapsedDays} ({time.CurrentDay}), {time.CurrentTime}");
        var before = customers.ToDictionary(c => c.NPC.ID, c => Schedule(c.CustomerData));

        // 1. In the customer's vanilla window: "Off" blocks the request, ×1 lets the game make it.
        var used = new System.Collections.Generic.HashSet<string>();
        var vanillaOk = false;
        foreach (var c in own)
        {
            var data = c.CustomerData;
            var perWeek = OrdersPerWeek(c);
            var interval = Mathf.Max(Mathf.RoundToInt(7f / Mathf.RoundToInt(Mathf.Lerp(data.MinOrdersPerWeek, data.MaxOrdersPerWeek,
                Mathf.Max(c.CurrentAddiction, c.NPC.RelationData.RelationDelta / 5f)))), 1);
            if (((int)time.CurrentDay - (int)data.PreferredOrderDay + 7) % 7 % interval != 0)
                continue;
            used.Add(c.NPC.ID);
            SetClock(time, S1.GameTime.TimeManager.AddMinutesTo24HourTime(data.OrderTime, 10));
            SetCounters(c, 5000);
            SetRates(0f, 10f);
            DevSmoke.Check(!Minute(c), $"{c.NPC.FirstName}: a request was made with requests Off");
            SetRates(1f, 1f);
            if (!Minute(c))
            {
                DevSmoke.Log($"{c.NPC.FirstName}: no vanilla request either ({perWeek}/week), trying another customer");
                continue;
            }
            DevSmoke.Log($"{c.NPC.FirstName}: Off blocks, x1 requests at {time.CurrentTime} ({perWeek}/week, ${c.OfferedContractInfo.Payment})");
            vanillaOk = true;
            break;
        }
        DevSmoke.Check(vanillaOk, "no customer made a vanilla request in his order window");
        yield return 1f;

        // 2. In an extra window, 200 minutes after the last deal: nothing at ×1, a request of the usual size at ×10.
        var extraOk = false;
        foreach (var c in own.Where(c => !used.Contains(c.NPC.ID)))
        {
            var data = c.CustomerData;
            var perWeek = OrdersPerWeek(c);
            var day = time.ElapsedDays + (7 - (int)data.PreferredOrderDay) % 7;
            var windows = DemandPatches.WindowsOn(perWeek, 10f, day);
            if (windows < 2)
                continue;
            SetClock(time, S1.GameTime.TimeManager.AddMinutesTo24HourTime(data.OrderTime, 1440 / windows + 10));
            SetCounters(c, 200);
            SetRates(1f, 1f);
            DevSmoke.Check(!Minute(c), $"{c.NPC.FirstName}: vanilla request outside the order window");
            SetRates(10f, 0f);
            if (!Minute(c))
            {
                DevSmoke.Log($"{c.NPC.FirstName}: no request in the extra window ({perWeek}/week, {windows} windows today), trying another customer");
                SetCounters(c, 5000);
                continue;
            }
            var info = c.OfferedContractInfo;
            var budget = data.GetAdjustedWeeklySpend(c.NPC.RelationData.RelationDelta / 5f) / perWeek;
            var quantity = info.Products.entries[0].Quantity;
            DevSmoke.Log($"{c.NPC.FirstName}: x10 requests at {time.CurrentTime} ({perWeek}/week, {windows} windows today): {quantity} pcs for ${info.Payment}, vanilla order budget ${budget:0}");
            DevSmoke.Check(c.TimeSinceLastDealCompleted == 202 && c.TimeSinceLastDealOffered == 0,
                $"{c.NPC.FirstName}: counters {c.TimeSinceLastDealCompleted}/{c.TimeSinceLastDealOffered}, expected 202/0");
            // Payment is about budget × enjoyment² (0.44–2.25); a budget cut to one seventh would fall below.
            if (quantity >= 4)
                DevSmoke.Check(info.Payment > budget * 0.3f && info.Payment < budget * 2.6f, $"{c.NPC.FirstName}: order size changed (${info.Payment} for a ${budget:0} budget)");
            extraOk = true;
            break;
        }
        DevSmoke.Check(extraOk, "no customer made a request in an extra x10 window");
        yield return 1f;

        // 3. The same through a dealer: his customers order in extra windows and he takes the deal.
        var dealerOk = false;
        foreach (var c in dealt)
        {
            var data = c.CustomerData;
            var perWeek = OrdersPerWeek(c);
            var day = time.ElapsedDays + (7 - (int)data.PreferredOrderDay) % 7;
            var windows = DemandPatches.WindowsOn(perWeek, 10f, day);
            if (windows < 2)
                continue;
            var dealer = c.AssignedDealer;
            SetClock(time, S1.GameTime.TimeManager.AddMinutesTo24HourTime(data.OrderTime, 1440 / windows + 10));
            SetCounters(c, 200);
            SetRates(1f, 1f);
            DevSmoke.Check(!Minute(c), $"{c.NPC.FirstName}: vanilla dealer order outside the order window");
            SetRates(0f, 10f);
            var ordered = Minute(c);
            DevSmoke.Log($"{c.NPC.FirstName} -> dealer {dealer.FirstName} ({dealer.ActiveContracts.Count} deals): {(ordered ? "ordered" : "no order")} at x10");
            if (!ordered)
            {
                SetCounters(c, 5000);
                continue;
            }
            DevSmoke.Check(c.CurrentContract != null && c.CurrentContract.Dealer != null, $"{c.NPC.FirstName}: the dealer did not take the deal");
            dealerOk = true;
            break;
        }
        if (dealt.Count == 0)
            DevSmoke.Log("WARN: the save has no free dealer customers, dealer orders not tested");
        else
            DevSmoke.Check(dealerOk, "no dealer customer ordered in an extra x10 window");

        // 4. Let the world run at ×10 for a while: nothing breaks, schedules stay intact.
        SetRates(10f, 10f);
        time.SetTimeSpeedMultiplier(1f);
        var deals = customers.Count(HasDeal);
        yield return 30f;
        DevSmoke.Log($"30 s at x10: deals {deals} -> {customers.Count(HasDeal)}, time {time.CurrentTime}");
        DevSmoke.Check(!DemandPatches.Failed, "the rate threw an exception (see the log)");
        foreach (var c in customers)
            DevSmoke.Check(Schedule(c.CustomerData) == before[c.NPC.ID], $"{c.NPC.FirstName}: schedule changed, {before[c.NPC.ID]} -> {Schedule(c.CustomerData)}");
        DevSmoke.Finish(true, "deals done");
    }

    /// <summary>With -Save of a progressed save: completes an active deal the way the game does.</summary>
    private static IEnumerator CompleteContract()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return 2f;
        var contracts = S1Shared.UnityQuery.ToManaged(S1.Quests.Contract.Contracts);
        DevSmoke.Log($"{contracts.Count} contracts");
        var contract = contracts.First(c => c.State == S1.Quests.EQuestState.Active);
        DevSmoke.Log($"Completing contract {contract.Title}");
        contract.Complete(true);
        DevSmoke.Log("Contract.Complete returned");
        yield return 2f;
        var quest = S1Shared.UnityQuery.ToManaged(S1.Quests.Quest.Quests).FirstOrDefault(q => q.State == S1.Quests.EQuestState.Active);
        if (quest != null)
        {
            DevSmoke.Log($"Completing quest {quest.Title}");
            quest.Complete(true);
            DevSmoke.Log("Quest.Complete returned");
        }
        yield return 2f;
        DevSmoke.Finish(true, "complete-contract done");
    }

    /// <summary>Loads a copied real save (-Save) and applies storage rates with a trace.</summary>
    private static IEnumerator StorageSave()
    {
        StoragePatches.Trace = DevSmoke.Log;
        yield return DevSmoke.LoadDisposableSave();
        yield return 2f;
        DevSmoke.Log("manual ApplyAll x1");
        StoragePatches.ApplyAll();
        yield return 1f;
        RatesState.Current.Set(RateCatalog.StorageFurniture, 2f);
        RatesState.Current.Set(RateCatalog.StorageVehicles, 2f);
        DevSmoke.Log("manual ApplyAll x2");
        StoragePatches.ApplyAll();
        yield return 20f;
        DevSmoke.Finish(true, "storage-save done");
    }

    private static IEnumerator Inspect()
    {
        yield return DevSmoke.LoadDisposableSave();
        Write("storages.txt", StoragePatches.Describe());

        var pause = S1.UI.PauseMenu.Instance;
        if (pause != null)
            Write("pause_hierarchy.txt", UnityQuery.DumpHierarchy(pause.transform, 8));
        var settings = UnityQuery.FindInScenes<S1.UI.MainMenu.SettingsScreen>();
        Write("settings_hierarchy.txt", string.Join("\n\n", settings.Select(s => UnityQuery.DumpHierarchy(s.transform, 14))));
        var storageMenu = S1.UI.StorageMenu.Instance;
        if (storageMenu != null)
            Write("storagemenu_hierarchy.txt", UnityQuery.DumpHierarchy(storageMenu.transform, 7));

        // XP: other-source multiplier applied through LevelManager.AddXP.
        var levels = S1.Levelling.LevelManager.Instance;
        var before = levels.TotalXP;
        RatesState.Current.Set(RateCatalog.XpOther, 3f);
        levels.AddXP(10);
        yield return 2f;
        var gained = levels.TotalXP - before;
        DevSmoke.Log($"XP: +10 with other x3 -> gained {gained}");
        if (gained != 30)
            throw new InvalidOperationException($"Expected 30 XP, got {gained}");
        RatesState.Current.Set(RateCatalog.XpOther, 1f);

        pause?.Pause();
        yield return 1f;
        yield return DevSmoke.Screenshot("pause");
        pause?.Resume();
        yield return 0.5f;

        DevSmoke.Finish(true, "inspect done");
    }

    private static IEnumerator Ui()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return DevSmoke.WaitUntil(() => UnityQuery.FindInScenes<S1.UI.MainMenu.SettingsScreen>().Any(s => s.name == "WorldRatesScreen"),
            20f, "World Rates screen");

        // Storage: a rack in the starting RV grows and shrinks live.
        var rack = UnityQuery.FindInScenes<S1.Storage.StorageEntity>()
            .First(e => UnityQuery.PathOf(e.transform).Contains("RV Contents Container/StorageRack_Medium"));
        var baseSlots = rack.ItemSlots.Count;
        RatesState.Current.Set(RateCatalog.StorageFurniture, 2f);
        yield return 0.5f;
        DevSmoke.Log($"Rack slots {baseSlots} -> {rack.ItemSlots.Count} (x2), rows={rack.DisplayRowCount}");
        if (rack.ItemSlots.Count != baseSlots * 2)
            throw new InvalidOperationException($"Rack expected {baseSlots * 2} slots, has {rack.ItemSlots.Count}");
        S1.UI.StorageMenu.Instance.Open(rack);
        yield return 1.5f;
        yield return DevSmoke.Screenshot("storage_x2");
        S1.UI.StorageMenu.Instance.Close();
        yield return 1f;
        RatesState.Current.Set(RateCatalog.StorageFurniture, 1f);
        yield return 0.5f;
        if (rack.ItemSlots.Count != baseSlots)
            throw new InvalidOperationException($"Rack should shrink back to {baseSlots}, has {rack.ItemSlots.Count}");

        // Window: open through the hotkey path, visit every tab.
        RatesWindow.Toggle();
        yield return 2f;
        if (!RatesWindow.IsOpen)
            throw new InvalidOperationException("World Rates window did not open");
        var screen = UnityQuery.FindInScenes<S1.UI.MainMenu.SettingsScreen>().First(s => s.name == "WorldRatesScreen");
        SetRates(2f, 3f);
        for (var i = 0; i < 5; i++)
        {
            screen.Categories[i].Toggle.isOn = true;
            yield return 0.8f;
            yield return DevSmoke.Screenshot("window_tab" + i);
        }
        SetRates(1f, 1f);

        // Slider → settings.
        var slider = UnityQuery.GetComponentsInChildren<UnityEngine.UI.Slider>(screen, true)
            .First(s => s.transform.parent.name == RateCatalog.XpDealPlayer);
        slider.value = 25; // 25 steps × 0.1 = ×2.5
        yield return 0.3f;
        var got = RatesState.Current.Get(RateCatalog.XpDealPlayer);
        DevSmoke.Log($"Slider set deal XP to x{got}");
        if (Math.Abs(got - 2.5f) > 0.01f)
            throw new InvalidOperationException($"Slider did not update rate, got {got}");

        // Preset → sliders follow.
        RatesState.Current.ApplyPreset(3f);
        yield return 0.5f;
        screen.Categories[0].Toggle.isOn = true;
        yield return 0.8f;
        yield return DevSmoke.Screenshot("window_after_x3");

        screen.Close();
        yield return 1.5f;
        yield return DevSmoke.Screenshot("pause_with_button");
        DevSmoke.Finish(true, "ui done");
    }

    /// <summary>Laundering through the game's own CompleteOperation with income multipliers applied.</summary>
    private static IEnumerator Income()
    {
        yield return DevSmoke.LoadDisposableSave();
        var money = S1.Money.MoneyManager.Instance;
        var business = UnityQuery.FindInScenes<S1.Property.Business>().First();
        var complete = HarmonyLib.AccessTools.Method(typeof(S1.Property.Business), "CompleteOperation");

        RatesState.Current.Set(RateCatalog.IncomeLaundering, 2f);
        RatesState.Current.Set(RateCatalog.IncomeAll, 1.5f);
        var before = money.onlineBalance;
        complete.Invoke(business, new object[] { new S1.Property.LaunderingOperation(business, 100f, 0) });
        yield return 2f;
        var gained = money.onlineBalance - before;
        DevSmoke.Log($"Laundering $100 at x2 (all x1.5) -> online balance +{gained}");
        if (Math.Abs(gained - 300f) > 0.01f)
            throw new InvalidOperationException($"Expected +300 online balance, got +{gained}");
        yield return DevSmoke.Screenshot("laundering_notification");

        RatesState.Current.Reset();
        before = money.onlineBalance;
        complete.Invoke(business, new object[] { new S1.Property.LaunderingOperation(business, 100f, 0) });
        yield return 2f;
        gained = money.onlineBalance - before;
        DevSmoke.Log($"Laundering $100 vanilla -> +{gained}");
        if (Math.Abs(gained - 100f) > 0.01f)
            throw new InvalidOperationException($"Expected +100 at vanilla, got +{gained}");
        DevSmoke.Finish(true, "income done");
    }

    private static void Write(string name, string content)
    {
        File.WriteAllText(Path.Combine(DevSmoke.OutDir, name), content, new UTF8Encoding(false));
        DevSmoke.Log($"Wrote {name} ({content.Length} chars)");
    }
}
#endif
