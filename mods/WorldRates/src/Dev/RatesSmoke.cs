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
        for (var i = 0; i < 4; i++)
        {
            screen.Categories[i].Toggle.isOn = true;
            yield return 0.8f;
            yield return DevSmoke.Screenshot("window_tab" + i);
        }

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

    private static void Write(string name, string content)
    {
        File.WriteAllText(Path.Combine(DevSmoke.OutDir, name), content, new UTF8Encoding(false));
        DevSmoke.Log($"Wrote {name} ({content.Length} chars)");
    }
}
#endif
