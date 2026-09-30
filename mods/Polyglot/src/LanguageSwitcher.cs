using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using Polyglot.Fonts;
using Polyglot.Localization;

namespace Polyglot;

internal static class LanguageSwitcher
{
    public static void Apply(string code, bool save)
    {
        var language = LanguageCatalog.Find(code) ?? LanguageCatalog.Source;
        FontManager.EnsureFallbacks(language);
        Translator.SetLanguage(language);
#if DEV
        save &= !S1Shared.Dev.DevSmoke.Active; // smoke runs must not touch the player's settings
#endif
        if (save && PolyglotConfig.Language.Value != language.Code)
        {
            PolyglotConfig.Language.Value = language.Code;
            MelonPreferences.Save();
        }
    }

    public static void CycleNext()
    {
        var cycle = CycleList();
        if (cycle.Count == 0)
            return;
        var index = cycle.FindIndex(l => l == Translator.Current);
        var next = cycle[(index + 1) % cycle.Count];
        Apply(next.Code, save: true);
        ShowToast(next);
    }

    private static List<LanguageInfo> CycleList()
    {
        var configured = PolyglotConfig.CycleLanguages.Value
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(LanguageCatalog.Find)
            .Where(l => l != null)
            .Select(l => l!)
            .Distinct()
            .ToList();
        return configured.Count > 0 ? configured : LanguageCatalog.Languages.ToList();
    }

    private static void ShowToast(LanguageInfo language)
    {
        var notifications = S1.UI.NotificationsManager.Instance;
        if (notifications != null)
            notifications.SendNotification(language.NativeName, "Language", null, 2.5f, false);
    }
}
