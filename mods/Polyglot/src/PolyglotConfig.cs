using MelonLoader;
using UnityEngine;

namespace Polyglot;

/// <summary>Settings in &lt;game&gt;/UserData/MelonPreferences.cfg, section [Polyglot].</summary>
internal static class PolyglotConfig
{
    public static MelonPreferences_Entry<string> Language { get; private set; } = null!;
    public static MelonPreferences_Entry<KeyCode> CycleHotkey { get; private set; } = null!;
    public static MelonPreferences_Entry<string> CycleLanguages { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> LogUntranslated { get; private set; } = null!;
    public static MelonPreferences_Entry<string> CustomFontPath { get; private set; } = null!;

    public static void Init()
    {
        var category = MelonPreferences.CreateCategory(ModInfo.Name);
        Language = category.CreateEntry("Language", "en",
            description: "Active language code (en, ru, uk, de, fr, es, pt-BR, pl, tr, it, zh-CN, ja, ko, ...). Also selectable in Settings.");
        CycleHotkey = category.CreateEntry("CycleHotkey", KeyCode.F9,
            description: "Key that switches to the next language from CycleLanguages. None disables it.");
        CycleLanguages = category.CreateEntry("CycleLanguages", "",
            description: "Comma-separated language codes the hotkey cycles through, e.g. \"en,ru\". Empty = all installed languages.");
        LogUntranslated = category.CreateEntry("LogUntranslated", false,
            description: "Write every text without a translation to UserData/Polyglot/untranslated_<code>.txt (for translators).");
        CustomFontPath = category.CreateEntry("CustomFontPath", "",
            description: "Optional .ttf/.otf/.ttc used as the last-resort fallback font (e.g. for CJK on Linux/Proton).");
    }
}
