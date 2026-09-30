#if DEV
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Polyglot.Fonts;
using Polyglot.Localization;
using S1Shared;
using S1Shared.Dev;
using UnityEngine;

namespace Polyglot.Dev;

/// <summary>Smoke scenarios for tools/run-smoke.ps1 -Mod Polyglot -Scenario &lt;name&gt;.</summary>
internal static class PolyglotSmoke
{
    public static IEnumerator Run()
    {
        IEnumerator? body = DevSmoke.Scenario switch
        {
            "inspect" => Inspect(),
            "tour" => Tour(),
#if MONO
            "corpus" => Corpus(),
            "corpus-tutorial" => CorpusTutorial(),
#endif
            _ => null,
        };
        if (body == null)
            DevSmoke.Finish(false, "Unknown scenario " + DevSmoke.Scenario);
        return Guard(body ?? Enumerable.Empty<object>().GetEnumerator());
    }

    private static IEnumerator Guard(IEnumerator body)
    {
        // Exceptions inside the scenario end the run as FAIL instead of hanging until the timeout.
        var runner = new CoRunner(ex => DevSmoke.Finish(false, ex.ToString()));
        runner.Start(body);
        while (true)
        {
            runner.Tick();
            yield return null;
        }
    }

    private static IEnumerator Inspect()
    {
        yield return DevSmoke.WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Menu", 90f, "menu");
        yield return 4f;
        Write("fonts.txt", FontManager.Describe());
        Write("menu_texts.txt", DumpTexts());
        var settings = UnityQuery.FindAllIncludingAssets<S1.UI.MainMenu.SettingsScreen>();
        Write("settings_hierarchy.txt", string.Join("\n\n", settings.Select(s => UnityQuery.DumpHierarchy(s.transform))));
        yield return DevSmoke.Screenshot("menu_en");

        LanguageSwitcher.Apply("ru", save: false);
        yield return 1f;
        yield return DevSmoke.Screenshot("menu_ru");
        Write("fonts_after_ru.txt", FontManager.Describe());

        var screen = settings.FirstOrDefault(s => s.gameObject.scene.IsValid());
        if (screen == null)
            throw new InvalidOperationException("SettingsScreen not found");
        screen.Open();
        yield return 1.5f;
        yield return DevSmoke.Screenshot("settings_ru");
        var picker = UnityQuery.GetComponentsInChildren<TMP_Dropdown>(screen, true).FirstOrDefault(d => d.transform.parent.name == "Polyglot Language");
        if (picker == null)
            throw new InvalidOperationException("Language picker was not injected");
        picker.value = LanguageCatalog.Languages.ToList().FindIndex(l => l.Code == "ja");
        yield return 1f;
        if (Translator.Current.Code != "ja")
            throw new InvalidOperationException("Picker did not switch language, current=" + Translator.Current.Code);
        yield return DevSmoke.Screenshot("settings_ja_via_picker");
        picker.Show();
        yield return 1f;
        yield return DevSmoke.Screenshot("settings_picker_open");
        picker.Hide();
        LanguageSwitcher.Apply("ru", save: false);
        screen.Close();
        yield return 1f;

        yield return DevSmoke.LoadDisposableSave();
        yield return DevSmoke.Screenshot("game_ru");
        Write("game_texts.txt", DumpTexts());

        LanguageSwitcher.Apply("ja", save: false);
        yield return 1.5f;
        yield return DevSmoke.Screenshot("game_ja");

        LanguageSwitcher.Apply("en", save: false);
        yield return 1f;
        yield return DevSmoke.Screenshot("game_en");

        MissLog.Flush();
        DevSmoke.Finish(true, $"inspect done, misses={MissLog.Count}");
    }

#if MONO
    private static IEnumerator Corpus()
    {
        yield return DevSmoke.WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Menu", 90f, "menu");
        yield return 4f;
        CorpusDump.Write(Path.Combine(DevSmoke.OutDir, "corpus_menu.jsonl"));

        yield return DevSmoke.LoadDisposableSave();
        yield return 5f;
        CorpusDump.Write(Path.Combine(DevSmoke.OutDir, "corpus_main.jsonl"));

        var scriptables = Resources.LoadAll<ScriptableObject>("");
        DevSmoke.Log($"Loaded {scriptables.Length} ScriptableObjects from Resources");
        var prefabs = Resources.LoadAll<GameObject>("");
        DevSmoke.Log($"Loaded {prefabs.Length} GameObjects from Resources");
        yield return 2f;
        CorpusDump.Write(Path.Combine(DevSmoke.OutDir, "corpus_resources.jsonl"));
        DevSmoke.Finish(true, "corpus dumped");
    }

    private static IEnumerator CorpusTutorial()
    {
        yield return DevSmoke.LoadDisposableSave(tutorial: true);
        yield return 5f;
        CorpusDump.Write(Path.Combine(DevSmoke.OutDir, "corpus_tutorial.jsonl"));
        DevSmoke.Finish(true, "tutorial corpus dumped");
    }
#endif

    /// <summary>Walks through menus and phone apps in one language (DevSmoke.Arg, default ru), screenshotting each.</summary>
    private static IEnumerator Tour()
    {
        var code = string.IsNullOrEmpty(DevSmoke.Arg) ? "ru" : DevSmoke.Arg;
        File.Delete(Path.Combine(LanguageCatalog.UserRoot, $"untranslated_{code}.txt")); // count only this run's misses
        yield return DevSmoke.WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Menu", 90f, "menu");
        LanguageSwitcher.Apply(code, save: false);
        yield return 3f;
        yield return DevSmoke.Screenshot("01_menu");

        // Composed texts: bullets, upper-cased templates, highlight colors.
        foreach (var sample in new[]
                 {
                     "• Collect the stash near the fountain",
                     "'DOCKS' REGION MUST BE UNLOCKED",
                     "Deal for Kyle<color=#c0c0c0ff> (Begins in 5 min)</color>",
                     "1x OG Kush, Behind Thompson construction and demolition",
                     "At a <color=#88CBFF>mixing station</color>, you mix products with special <color=#88CBFF>ingredients</color> to create new mixes with <color=#88CBFF>unique effects</color>. Customers will pay more for these products.",
                 })
        {
            var tr = Translator.Translate(sample);
            DevSmoke.Log($"sample \"{sample}\" -> \"{tr}\"");
            DevSmoke.Check(tr != sample, "not translated: " + sample);
        }

        yield return DevSmoke.LoadDisposableSave();
        yield return DevSmoke.Screenshot("02_hud");

        var menu = S1.UI.GameplayMenu.Instance;
        menu.SetScreen(S1.UI.GameplayMenu.EGameplayScreen.Phone);
        menu.Open();
        yield return 2f;
        S1.UI.Phone.ProductManagerApp.ProductManagerApp.Instance.SetOpen(true);
        yield return 1.5f;
        Write("layout.txt", DumpLayout());
        S1.UI.Phone.ProductManagerApp.ProductManagerApp.Instance.SetOpen(false);
        yield return 0.5f;
        yield return DevSmoke.Screenshot("03_phone");

        var shot = 4;
        foreach (var (name, open) in new (string, Action<bool>)[]
                 {
                     ("journal", o => S1.UI.Phone.JournalApp.Instance.SetOpen(o)),
                     ("messages", o => S1.UI.Phone.Messages.MessagesApp.Instance.SetOpen(o)),
                     ("contacts", o => S1.UI.Phone.ContactsApp.ContactsApp.Instance.SetOpen(o)),
                     ("map", o => S1.UI.Phone.Map.MapApp.Instance.SetOpen(o)),
                     ("products", o => S1.UI.Phone.ProductManagerApp.ProductManagerApp.Instance.SetOpen(o)),
                     ("deliveries", o => S1.UI.Phone.Delivery.DeliveryApp.Instance.SetOpen(o)),
                     ("dealers", o => S1.UI.Phone.Messages.DealerManagementApp.Instance.SetOpen(o)),
                 })
        {
            open(true);
            yield return 1.5f;
            yield return DevSmoke.Screenshot($"{shot++:00}_{name}");
            open(false);
            yield return 0.5f;
        }
        menu.Close();
        yield return 1f;

        var pause = S1.UI.PauseMenu.Instance;
        pause.Pause();
        yield return 1.5f;
        yield return DevSmoke.Screenshot($"{shot++:00}_pause");

        // The WorldRates window, when that mod is installed too.
        var ratesWindow = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("WorldRates.UI.RatesWindow"))
            .FirstOrDefault(t => t != null);
        if (ratesWindow != null)
        {
            ratesWindow.GetMethod("Open")?.Invoke(null, null);
            yield return 1.5f;
            yield return DevSmoke.Screenshot($"{shot++:00}_worldrates");
            ratesWindow.GetMethod("Toggle")?.Invoke(null, null);
            yield return 1f;
        }
        // Settings screens of the other mods in this repository, when installed.
        foreach (var modType in new[] { "DamageIndicator.DamageIndicatorMod", "GuideArrows.GuideArrowsMod" })
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(modType)).FirstOrDefault(t => t != null);
            var window = type?.GetProperty("Window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.GetValue(null);
            if (window == null)
                continue;
            window.GetType().GetMethod("Open")?.Invoke(window, null);
            yield return 1.5f;
            yield return DevSmoke.Screenshot($"{shot++:00}_{modType.Split('.')[0]}");
            window.GetType().GetMethod("Toggle")?.Invoke(window, null);
            yield return 1f;
        }

        pause.Resume();
        yield return 1f;

        // A phone call with highlighted words (typewriter text).
        var calls = UnityQuery.FindAllIncludingAssets<S1.ScriptableObjects.PhoneCallData>();
        var callData = calls.FirstOrDefault(c => c.Stages != null && c.Stages.Length > 0 && c.Stages[0].Text.Contains("<h1>")) ?? calls.FirstOrDefault();
        var callUi = S1.UI.Phone.CallInterface.Instance;
        if (callData != null && callUi != null)
        {
            DevSmoke.Log($"Call: {callData.name}: {callData.Stages[0].Text}");
            callUi.StartCall(callData, callData.CallerID, 0);
            yield return 5f;
            DevSmoke.Log($"Call text: {callUi.MainText.text}");
            yield return DevSmoke.Screenshot($"{shot++:00}_call");
            callUi.CompleteCall();
            yield return 1f;
        }

        MissLog.Flush();
        File.Copy(Path.Combine(LanguageCatalog.UserRoot, $"untranslated_{code}.txt"), Path.Combine(DevSmoke.OutDir, $"untranslated_{code}.txt"), true);
        DevSmoke.Finish(true, $"tour {code} done, misses={MissLog.Count}, fitted={Polyglot.Text.TextFitter.FittedCount}");
    }

    /// <summary>Active texts whose translation is longer than the original, with their layout settings.</summary>
    private static string DumpLayout()
    {
        var sb = new StringBuilder();
        foreach (var t in UnityQuery.FindInScenes<TMP_Text>())
        {
            if (!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text))
                continue;
            var tr = Translator.Translate(t.text);
            var path = UnityQuery.PathOf(t.transform);
            if (tr == t.text)
                continue;
            sb.Append($"maxVis={t.maxVisibleCharacters} maxWords={t.maxVisibleWords} maxLines={t.maxVisibleLines} ")
              .Append($"parsed=\"{t.GetParsedText()}\" ");
            var r = t.rectTransform.rect;
            sb.Append(UnityQuery.PathOf(t.transform)).Append(" | \"").Append(tr.Replace("\n", "\\n")).Append("\" ")
              .Append($"overflow={t.overflowMode} wrap={t.textWrappingMode} auto={t.enableAutoSizing} size={t.fontSize:0.#} ")
              .Append($"rect={r.width:0}x{r.height:0} pref={t.preferredWidth:0}x{t.preferredHeight:0} ")
              .Append($"trunc={t.isTextTruncated} overflowing={t.isTextOverflowing}\n");
        }
        foreach (var t in UnityQuery.FindInScenes<UnityEngine.UI.Text>())
        {
            if (!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text) || Translator.Translate(t.text) == t.text)
                continue;
            var r = t.rectTransform.rect;
            sb.Append("[legacy] ").Append(UnityQuery.PathOf(t.transform)).Append(" | \"").Append(Translator.Translate(t.text)).Append("\" ")
              .Append($"h={t.horizontalOverflow} v={t.verticalOverflow} bestFit={t.resizeTextForBestFit} size={t.fontSize} rect={r.width:0}x{r.height:0}\n");
        }
        return sb.ToString();
    }

    private static string DumpTexts()
    {
        var sb = new StringBuilder();
        foreach (var t in UnityQuery.FindInScenes<TMP_Text>().OrderBy(t => UnityQuery.PathOf(t.transform)))
        {
            sb.Append(t.isActiveAndEnabled ? "+ " : "- ")
              .Append(UnityQuery.PathOf(t.transform)).Append(" [").Append(t.font != null ? t.font.name : "null").Append("] = ")
              .Append(t.text?.Replace("\n", "\\n")).Append('\n');
        }
        return sb.ToString();
    }

    private static void Write(string name, string content)
    {
        File.WriteAllText(Path.Combine(DevSmoke.OutDir, name), content, new UTF8Encoding(false));
        DevSmoke.Log($"Wrote {name} ({content.Length} chars)");
    }
}
#endif
