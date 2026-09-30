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
