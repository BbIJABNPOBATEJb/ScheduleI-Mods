using System;
using MelonLoader;
using S1Shared;
using UnityEngine;
using WorldRates.Patches;
using WorldRates.Rates;
using WorldRates.UI;
#if DEV
using S1Shared.Dev;
#endif

[assembly: MelonInfo(typeof(WorldRates.WorldRatesMod), WorldRates.ModInfo.Name, WorldRates.ModInfo.Version, WorldRates.ModInfo.Author)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: HarmonyDontPatchAll]

namespace WorldRates;

public sealed class WorldRatesMod : MelonMod
{
    private CoRunner _runner = null!;
    private MelonPreferences_Entry<KeyCode> _hotkey = null!;
    private bool _inputBroken;
    private float _nextStorageCheck;

    public override void OnInitializeMelon()
    {
        SafePatcher.Apply(HarmonyInstance, LoggerInstance);
        var prefs = MelonPreferences.CreateCategory(ModInfo.Name);
        _hotkey = prefs.CreateEntry("WindowHotkey", KeyCode.F10,
            description: "Opens the World Rates window (also in the pause menu). None disables the key.");
        var debug = prefs.CreateEntry("DebugLog", false, description: "Log every scaled XP gain.");
        XpPatches.DebugLog = debug.Value;

        RatesState.Init();
        RatesWindow.Init();
        RatesState.WorldChanged += StoragePatches.ApplyAll;
        RatesState.Current.Changed += id =>
        {
            if (id.StartsWith("storage.", StringComparison.Ordinal))
                StoragePatches.ApplyAll();
        };

        _runner = new CoRunner(ex => LoggerInstance.Error(ex));
#if DEV
        if (DevSmoke.TryInit(ModInfo.Name))
            _runner.Start(Dev.RatesSmoke.Run());
#endif
        LoggerInstance.Msg($"{ModInfo.Name} {ModInfo.Version} loaded");
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (sceneName == "Main")
            StoragePatches.ApplyAll();
    }

    public override void OnUpdate()
    {
        _runner.Tick();
        RatesState.Tick();
        RatesWindow.Tick();
#if DEV
        DevSmoke.CheckTimeout();
#endif
        if (HotkeyPressed())
            RatesWindow.Toggle();

        // Vehicles become player-owned after purchase; pick up such changes now and then.
        if (RatesState.InWorld && Time.realtimeSinceStartup >= _nextStorageCheck)
        {
            _nextStorageCheck = Time.realtimeSinceStartup + 15f;
            StoragePatches.ApplyAll();
        }
    }

    public override void OnApplicationQuit() => RatesState.Save();

    private bool HotkeyPressed()
    {
        var key = _hotkey.Value;
        if (key == KeyCode.None || _inputBroken)
            return false;
        try
        {
            return Input.GetKeyDown(key);
        }
        catch (Exception ex)
        {
            _inputBroken = true;
            LoggerInstance.Warning($"Hotkey disabled, legacy input unavailable: {ex.Message}");
            return false;
        }
    }
}
