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
            "sms" => Sms(),
            "effects" => Effects(),
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

    /// <summary>
    /// Product effects with the longest translations in every panel that lists them: the product
    /// manager, a customer's preferences in contacts and the item tooltip. Logs each label's box and lines.
    /// </summary>
    private static IEnumerator Effects()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return 2f;
        LanguageSwitcher.Apply(DevSmoke.Arg.Length > 0 ? DevSmoke.Arg : "ru", save: false);
        yield return 1f;

        var effects = UnityQuery.FindAllIncludingAssets<S1.Effects.Effect>()
            .Where(e => e != null && !string.IsNullOrEmpty(e.Name))
            .GroupBy(e => e.Name).Select(g => g.First())
            .OrderByDescending(e => Translator.Translate(e.Name).Length).ToList();
        DevSmoke.Log("Longest effects: " + string.Join(", ", effects.Take(8).Select(e => $"{e.Name} = {Translator.Translate(e.Name)}")));
        var product = UnityQuery.ToManaged(S1.Product.ProductManager.DiscoveredProducts).First(p => p != null);
        product.Properties.Clear();
        foreach (var effect in effects.Take(8))
            product.Properties.Add(effect);

        var menu = S1.UI.GameplayMenu.Instance;
        menu.SetScreen(S1.UI.GameplayMenu.EGameplayScreen.Phone);
        menu.Open();
        yield return 2f;
        var products = S1.UI.Phone.ProductManagerApp.ProductManagerApp.Instance;
        products.SetOpen(true);
        yield return 1f;
        products.DetailPanel.SetActiveProduct(product);
        yield return 1f;
        foreach (var label in products.DetailPanel.PropertyLabels)
            LogLabel("product app", label);
        yield return DevSmoke.Screenshot("effects_products");
        products.SetOpen(false);
        yield return 0.5f;

        var customer = UnityQuery.ToManaged(S1.Economy.Customer.UnlockedCustomers).FirstOrDefault(c => c != null)
            ?? UnityQuery.ToManaged(S1.Economy.Customer.LockedCustomers).First(c => c != null);
        customer.CustomerData.PreferredProperties.Clear();
        foreach (var effect in effects.Take(3))
            customer.CustomerData.PreferredProperties.Add(effect);
        var contacts = S1.UI.Phone.ContactsApp.ContactsApp.Instance;
        contacts.SetOpen(true);
        yield return 1f;
        contacts.DetailPanel.Open(customer.NPC);
        yield return 1f;
        LogLabel("contacts", contacts.DetailPanel.PropertiesLabel);
        yield return DevSmoke.Screenshot("effects_contacts");
        contacts.SetOpen(false);
        menu.Close();
        yield return 1f;

        // The item tooltip's own content, on a canvas of its own (3x): the game closes its tooltip
        // panel whenever no inventory slot is hovered.
        var canvasObject = new GameObject("Smoke Tooltip Canvas");
        var tooltipCanvas = canvasObject.AddComponent<Canvas>();
        tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tooltipCanvas.sortingOrder = 5000;
        tooltipCanvas.scaleFactor = 3f;
        var box = new GameObject("Tooltip").AddComponent<RectTransform>();
        box.SetParent(canvasObject.transform, false);
        var content = UnityEngine.Object.Instantiate(product.CustomInfoContent, box);
        var background = box.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(0.22f, 0.22f, 0.22f, 1f);
        content.Initialize(product.GetDefaultInstance(1));
        // Like the game's panel: sized from the content's height after Initialize.
        box.sizeDelta = new Vector2(180f, content.Height);
        DevSmoke.Log($"tooltip height {content.Height:0}");
        yield return 1f;
        Write("tooltip_layout.txt", DumpRects(box));
        foreach (var label in UnityQuery.GetComponentsInChildren<TextMeshProUGUI>(box, false))
            LogLabel("tooltip", label);
        yield return DevSmoke.Screenshot("effects_tooltip");
        UnityEngine.Object.Destroy(canvasObject);
        LanguageSwitcher.Apply("en", save: false);
        yield return 1f;
        DevSmoke.Finish(true, "effects done");
    }

    private static string DumpRects(Transform root)
    {
        var sb = new StringBuilder();
        void Walk(Transform t, int depth)
        {
            var r = UiKit.Get<RectTransform>(t);
            sb.Append(' ', depth * 2).Append(t.name).Append(t.gameObject.activeSelf ? "" : " [inactive]");
            if (r != null)
                sb.Append($" pos={r.anchoredPosition} size={r.sizeDelta} rect={r.rect.size} min={r.anchorMin} max={r.anchorMax} pivot={r.pivot}");
            sb.Append("  {");
            foreach (var c in t.GetComponents<Component>())
            {
                if (c != null)
                    sb.Append(UnityQuery.TypeName(c)).Append(' ');
            }
            sb.Append("}\n");
            for (var i = 0; i < t.childCount; i++)
                Walk(t.GetChild(i), depth + 1);
        }
        Walk(root, 0);
        return sb.ToString();
    }

    private static void LogLabel(string where, UnityEngine.UI.Text? text)
    {
        if (text == null || !text.gameObject.activeInHierarchy)
            return;
        var rect = text.rectTransform.rect;
        var shown = Translator.Translate(text.text);
        var settings = text.GetGenerationSettings(rect.size);
        var lines = text.cachedTextGenerator.lineCount;
        var height = text.cachedTextGeneratorForLayout.GetPreferredHeight(shown, settings) / text.pixelsPerUnit;
        DevSmoke.Log($"{where} [Text] box {rect.width:0}x{rect.height:0} font {text.fontSize} wrap={text.horizontalOverflow} bestFit={text.resizeTextForBestFit} "
            + $"lines={lines} needs height {height:0}: \"{shown.Replace("\n", " | ")}\"");
    }

    private static void LogLabel(string where, TextMeshProUGUI? text)
    {
        if (text == null || !text.isActiveAndEnabled || string.IsNullOrEmpty(text.text))
            return;
        var rect = text.rectTransform.rect;
        DevSmoke.Log($"{where} [TMP] {text.name} box {rect.width:0}x{rect.height:0} font {text.fontSize:0.#} wrap={text.textWrappingMode} auto={text.enableAutoSizing} "
            + $"lines={text.textInfo.lineCount} overflow={text.isTextOverflowing}: \"{Translator.Translate(text.text).Replace("\n", " | ")}\"");
    }

    /// <summary>Text messages whose translation is much longer than the English: every bubble holds its text, none overlap.</summary>
    private static IEnumerator Sms()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return 2f;
        LanguageSwitcher.Apply("ru", save: false);
        yield return 1f;

        var npc = UnityQuery.ToManaged(S1.NPCs.NPCManager.NPCRegistry).First(n => n != null && n.MSGConversation != null);
        var conversation = npc.MSGConversation;
        DevSmoke.Log($"Conversation with {npc.FirstName}");
        var texts = new[]
        {
            ("Click a product to list/unlist it. Customers will only buy listed products.", false),
            ("Used to mix product with ingredients to create unique new products.", true),
            ("A big jug of ethically sourced horse semen.", false),
            ("Open the map app to see your potential customers", false),
            ("Place the soil bag and seed vial in the motel room trash can", true),
        };
        foreach (var (text, mine) in texts)
        {
            DevSmoke.Check(Translator.Translate(text) != text, "not translated: " + text);
            var sender = mine ? S1.Messaging.Message.ESenderType.Player : S1.Messaging.Message.ESenderType.Other;
            conversation.SendMessage(new S1.Messaging.Message(text, sender, true), false, false);
        }

        var menu = S1.UI.GameplayMenu.Instance;
        menu.SetScreen(S1.UI.GameplayMenu.EGameplayScreen.Phone);
        menu.Open();
        yield return 2f;
        S1.UI.Phone.Messages.MessagesApp.Instance.SetOpen(true);
        yield return 1f;
        conversation.SetOpen(true);
        yield return 1.5f;
        yield return DevSmoke.Screenshot("sms_ru");
        CheckBubbles(conversation, texts.Length);

        // Another language while the conversation is open, then opened again.
        LanguageSwitcher.Apply("de", save: false);
        yield return 1f;
        conversation.SetOpen(false);
        yield return 0.5f;
        conversation.SetOpen(true);
        yield return 1.5f;
        yield return DevSmoke.Screenshot("sms_de");
        CheckBubbles(conversation, texts.Length);

        conversation.SetOpen(false);
        menu.Close();
        LanguageSwitcher.Apply("en", save: false);
        yield return 1f;
        DevSmoke.Finish(true, "sms done");
    }

    private static void CheckBubbles(S1.Messaging.MSGConversation conversation, int last)
    {
        var bubbles = Text.MessageBubbleFit.Bubbles(conversation);
        DevSmoke.Check(bubbles.Count >= last, $"only {bubbles.Count} bubbles");
        var checkedFrom = bubbles.Count - last;
        for (var i = checkedFrom; i < bubbles.Count; i++)
        {
            var bubble = bubbles[i];
            var content = Text.MessageBubbleFit.Content(bubble)!;
            var shown = Translator.Translate(content.text);
            var settings = content.GetGenerationSettings(new Vector2(content.GetPixelAdjustedRect().size.x, 0f));
            settings.resizeTextForBestFit = false;
            var needed = content.cachedTextGeneratorForLayout.GetPreferredHeight(shown, settings) / content.pixelsPerUnit;
            var room = content.GetPixelAdjustedRect().size.y;
            DevSmoke.Log($"bubble {i}: height {bubble.Height:0}, text needs {needed:0} of {room:0}: \"{shown}\"");
            DevSmoke.Check(needed <= room + 2f, $"bubble {i}: the text does not fit ({needed:0} > {room:0})");
            if (i > checkedFrom)
            {
                var above = bubbles[i - 1];
                var aboveBottom = above.Container.anchoredPosition.y - above.Height / 2f;
                var top = bubble.Container.anchoredPosition.y + bubble.Height / 2f;
                DevSmoke.Check(top <= aboveBottom + 1f, $"bubble {i} overlaps the one above ({top:0} > {aboveBottom:0})");
            }
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
                     // Game 0.4.7: reworded item descriptions and the special customer groups' announcements.
                     "Single OG Kush marijuana seed.",
                     "The <color=#FF5B5B>Bikers</color> have arrived.",
                     "The <color=#51F409FF>Hippies</color> are coming to town...",
                     "Deal completed for the <color=#FF5B5B>Bikers</color>",
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
