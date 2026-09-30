using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MelonLoader;
using Polyglot.Localization;
using Polyglot.Text;
using S1Shared;
using UnityEngine;
#if IL2CPP
using Il2CppInterop.Runtime;
using UnityEngine.Events;
#endif

namespace Polyglot.UI;

/// <summary>
/// Adds a "Language" dropdown at the top of Settings → Display, in the game's own style: the
/// "Units" row is cloned and rewired. Works in the main menu and in the in-game pause menu.
/// </summary>
internal static class LanguageSettingsRow
{
    private const string RowName = "Polyglot Language";

    private static readonly List<(TMP_Dropdown Dropdown, TMP_Text Label)> Rows = new();

    public static void Init()
    {
        foreach (var lang in LanguageCatalog.Languages)
            Translator.Protect(OptionLabel(lang));
        Translator.LanguageChanged += SyncAll;
    }

    private static string OptionLabel(LanguageInfo lang) =>
        lang.NativeName == lang.EnglishName ? lang.NativeName : $"{lang.NativeName} ({lang.EnglishName})";

    private static void Inject(S1.UI.MainMenu.SettingsScreen screen)
    {
        var units = UnityQuery.GetComponentsInChildren<S1.UI.Settings.UnitsModeDropdown>(screen, includeInactive: true).FirstOrDefault();
        if (units == null)
            return; // not the game's settings screen (e.g. another mod's clone of it)
        var template = units.transform.parent;
        if (template.parent.Find(RowName) != null)
            return;

        // Clone under an inactive holder so the cloned UnitsModeDropdown never wakes up.
        var holder = new GameObject("Polyglot Holder");
        holder.SetActive(false);
        var row = UnityEngine.Object.Instantiate(template.gameObject, holder.transform);
        foreach (var c in UnityQuery.GetComponentsInChildren<S1.UI.Settings.UnitsModeDropdown>(row.transform, true))
            UnityEngine.Object.DestroyImmediate(c);
        row.name = RowName;
        row.transform.SetParent(template.parent, false);
        row.transform.SetSiblingIndex(0);
        UnityEngine.Object.Destroy(holder);

        var dropdown = UnityQuery.GetComponentsInChildren<TMP_Dropdown>(row.transform, true).First();
        var label = UnityQuery.GetComponentsInChildren<TMP_Text>(row.transform, true)
            .First(t => t.name.StartsWith("Option Name", StringComparison.Ordinal));

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();
        foreach (var lang in LanguageCatalog.Languages)
            dropdown.options.Add(new TMP_Dropdown.OptionData(OptionLabel(lang)));
#if IL2CPP
        dropdown.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<int>>(new Action<int>(OnSelected)));
#else
        dropdown.onValueChanged.AddListener(OnSelected);
#endif
        TextHooks.Exclude(label);
        Rows.Add((dropdown, label));
        Sync(dropdown, label);
        MelonLogger.Msg("Settings: language picker added");
    }

    private static void OnSelected(int index)
    {
        var languages = LanguageCatalog.Languages;
        if (index >= 0 && index < languages.Count && languages[index] != Translator.Current)
            LanguageSwitcher.Apply(languages[index].Code, save: true);
    }

    private static void SyncAll()
    {
        Rows.RemoveAll(r => r.Dropdown == null || r.Label == null);
        foreach (var (dropdown, label) in Rows)
            Sync(dropdown, label);
    }

    private static void Sync(TMP_Dropdown dropdown, TMP_Text label)
    {
        var index = LanguageCatalog.Languages.ToList().IndexOf(Translator.Current);
        dropdown.SetValueWithoutNotify(Math.Max(index, 0));
        dropdown.RefreshShownValue();
        // Keep "Language" readable for someone who switched to a language they can't read.
        var translated = Translator.Translate("Language");
        label.text = translated == "Language" ? "Language" : $"{translated} (Language)";
    }

    [HarmonyPatch(typeof(S1.UI.MainMenu.SettingsScreen), "Awake")]
    private static class SettingsAwakePatch
    {
        private static void Postfix(S1.UI.MainMenu.SettingsScreen __instance)
        {
            try
            {
                Inject(__instance);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Settings language picker failed: {ex}");
            }
        }
    }
}
