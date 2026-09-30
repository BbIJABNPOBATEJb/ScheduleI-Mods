using System;
using MelonLoader;
using Polyglot.Fonts;
using Polyglot.Localization;
using Polyglot.Text;
using Polyglot.UI;
using S1Shared;
using UnityEngine;
#if DEV
using S1Shared.Dev;
#endif

[assembly: MelonInfo(typeof(Polyglot.PolyglotMod), Polyglot.ModInfo.Name, Polyglot.ModInfo.Version, Polyglot.ModInfo.Author, Polyglot.ModInfo.DownloadLink)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: HarmonyDontPatchAll]

namespace Polyglot;

public sealed class PolyglotMod : MelonMod
{
    private const float MissFlushInterval = 10f;

    private CoRunner _runner = null!;
    private float _nextFlush;
    private bool _inputBroken;

    public override void OnInitializeMelon()
    {
        SafePatcher.Apply(HarmonyInstance, LoggerInstance);
        PolyglotConfig.Init();
        MissLog.Enabled = PolyglotConfig.LogUntranslated.Value;
        LanguageCatalog.Discover();
        TextHooks.Init();
        LanguageSettingsRow.Init();
        _runner = new CoRunner(ex => LoggerInstance.Error(ex));
#if DEV
        if (DevSmoke.TryInit(ModInfo.Name))
        {
            MissLog.Enabled = true;
            _runner.Start(Dev.PolyglotSmoke.Run());
        }
#endif
        LanguageSwitcher.Apply(PolyglotConfig.Language.Value, save: false);
        LoggerInstance.Msg($"{ModInfo.Name} {ModInfo.Version}: {LanguageCatalog.Languages.Count} languages installed");
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (!Translator.Active)
            return;
        FontManager.EnsureFallbacks(Translator.Current);
        TextHooks.RefreshAll();
    }

    public override void OnUpdate()
    {
        _runner.Tick();
        TextFitter.Tick();
#if DEV
        DevSmoke.CheckTimeout();
#endif
        if (CyclePressed())
            LanguageSwitcher.CycleNext();

        if (Time.realtimeSinceStartup >= _nextFlush)
        {
            _nextFlush = Time.realtimeSinceStartup + MissFlushInterval;
            MissLog.Flush();
        }
    }

    public override void OnApplicationQuit() => MissLog.Flush();

    private bool CyclePressed()
    {
        var key = PolyglotConfig.CycleHotkey.Value;
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
