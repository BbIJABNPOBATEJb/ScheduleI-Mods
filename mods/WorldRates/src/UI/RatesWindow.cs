using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MelonLoader;
using S1Shared;
using UnityEngine;
using UnityEngine.UI;
using WorldRates.Rates;

namespace WorldRates.UI;

/// <summary>
/// The "World Rates" screen. Built by cloning the game's in-game Settings screen, so tabs, sliders,
/// tooltips and navigation look and behave like the rest of the game. Opened from the pause menu
/// (a "World Rates" button is added under Settings) or with the hotkey. Changes apply immediately.
/// </summary>
internal static class RatesWindow
{
    private const string ScreenName = "WorldRatesScreen";
    private const string ButtonName = "WorldRatesButton";

    private sealed class Row
    {
        public RateDef Def = null!;
        public Slider Slider = null!;
        public TMP_Text Value = null!;
        public TMP_Text Label = null!;
        public RectTransform? Hint;
    }

    private static S1.UI.MainMenu.SettingsScreen? _screen;
    private static readonly List<Row> Rows = new();
    private static readonly List<GameObject> Blockers = new();
    private static TMP_Text? _worldLabel;
    private static bool _openWhenPaused;
    private static bool _buildFailed;
    private static bool _syncing;
    private static int _placeHintsIn;

    public static bool IsOpen => _screen != null && _screen.IsOpen;

    public static void Init()
    {
        RatesState.Current.Changed += _ => Sync();
        RatesState.WorldChanged += () =>
        {
            _buildFailed = false;
            Sync();
        };
    }

    public static void Tick()
    {
        var pause = S1.UI.PauseMenu.Instance;
        if (_screen == null)
        {
            Rows.Clear();
            Blockers.Clear();
            if (pause != null && RatesState.InWorld && !_buildFailed)
                TryBuild(pause);
            return;
        }
        if (_openWhenPaused && pause != null && pause.IsPaused)
        {
            _openWhenPaused = false;
            Open();
        }
        if (_placeHintsIn > 0 && --_placeHintsIn == 0)
            PlaceHints();
    }

    public static void Toggle()
    {
        var pause = S1.UI.PauseMenu.Instance;
        if (_screen == null || pause == null)
            return;
        if (_screen.IsOpen)
        {
            _screen.Close();
        }
        else if (!pause.IsPaused)
        {
            pause.Pause();
            _openWhenPaused = true;
        }
        else
        {
            Open();
        }
    }

    public static void Open()
    {
        if (_screen == null)
            return;
        Sync();
        foreach (var scroll in UnityQuery.GetComponentsInChildren<ScrollRect>(_screen, true))
            scroll.verticalNormalizedPosition = 1f;
        // Labels may be shown translated (e.g. by a translation mod): re-place hints once rendered.
        _placeHintsIn = 2;
        _screen.Open();
    }

    // --- building ----------------------------------------------------------------------------------

    private static void TryBuild(S1.UI.PauseMenu pause)
    {
        try
        {
            Build(pause);
        }
        catch (Exception ex)
        {
            _buildFailed = true;
            _screen = null;
            MelonLogger.Error($"World Rates window could not be built: {ex}");
        }
    }

    private static void Build(S1.UI.PauseMenu pause)
    {
        var source = UnityQuery.GetComponentsInChildren<S1.UI.MainMenu.SettingsScreen>(pause, true)
            .FirstOrDefault(s => s.name != ScreenName) ?? throw new InvalidOperationException("in-game Settings screen not found");

        // Work on a clone parked under an inactive holder: nothing in it wakes up until it is ready.
        var holder = new GameObject("WorldRates Holder");
        holder.SetActive(false);
        var root = UnityEngine.Object.Instantiate(source.gameObject, holder.transform);
        root.name = ScreenName;
        var screen = UiKit.Get<S1.UI.MainMenu.SettingsScreen>(root)!;

        StripSettingsBindings(root.transform);

        var unused = new GameObject("Unused");
        unused.transform.SetParent(root.transform, false);
        unused.SetActive(false);

        var sliderTemplate = FindRowTemplate(root.transform, "Master") ?? throw new InvalidOperationException("slider template not found");
        var hintTemplate = UiKit.Find(root.transform, "Hint");
        var buttonTemplate = UiKit.Find(root.transform, "ViewCommands");
        var blockerTemplate = UiKit.Find(root.transform, "Blocker");
        foreach (var t in new[] { sliderTemplate, hintTemplate, buttonTemplate, blockerTemplate })
            t?.SetParent(unused.transform, false);

        // Park every original row (some are still referenced by SettingsScreen) out of sight.
        var categories = screen.Categories;
        for (var i = 0; i < categories.Length; i++)
        {
            var layout = UiKit.Find(categories[i].Panel.transform, "Vertical Layout");
            if (layout == null)
                continue;
            foreach (var child in UiKit.Children(layout))
                child.SetParent(unused.transform, false);
        }
        screen.HostOnlyGameObjects = new GameObject[0];

        SetText(root.transform, "Title", "World Rates");
        _worldLabel = UiKit.Find(root.transform, "Version") is { } version ? UiKit.Get<TMP_Text>(version) : null;

        var groups = new[] { RateGroup.Experience, RateGroup.Income, RateGroup.Storage };
        var tabTitles = new[] { "Experience", "Income", "Storage", "Presets" };
        for (var i = 0; i < categories.Length; i++)
        {
            var toggle = categories[i].Toggle;
            if (i >= tabTitles.Length)
            {
                toggle.gameObject.SetActive(false);
                continue;
            }
            var label = UiKit.InChildren<TMP_Text>(toggle);
            if (label != null)
                label.text = tabTitles[i];
            UiKit.AddListener(toggle, on =>
            {
                if (on)
                    _placeHintsIn = 2;
            });

            var layout = UiKit.Find(categories[i].Panel.transform, "Vertical Layout")!;
            if (i < groups.Length)
            {
                foreach (var def in RateCatalog.InGroup(groups[i]))
                    AddSlider(layout, sliderTemplate, hintTemplate, def);
                if (blockerTemplate != null)
                    AddBlocker(layout, blockerTemplate);
            }
            else
            {
                BuildPresets(layout, sliderTemplate, buttonTemplate);
            }
        }

        // Move into the pause menu next to the original and let Awake register the screen.
        root.transform.SetParent(source.transform.parent, false);
        root.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
        UnityEngine.Object.Destroy(holder);
        root.SetActive(true);
        _screen = screen;

        AddPauseButton(pause);
        Sync();
        MelonLogger.Msg("World Rates window ready");
    }

    /// <summary>Removes components that bind rows to real game settings (volume, resolution...).</summary>
    private static void StripSettingsBindings(Transform root)
    {
        foreach (var behaviour in UnityQuery.GetComponentsInChildren<MonoBehaviour>(root, true))
        {
            if (behaviour == null)
                continue;
            var ns = UiKit.Namespace(behaviour);
            var name = UnityQuery.TypeName(behaviour);
            if (ns.EndsWith("ScheduleOne.UI.Settings", StringComparison.Ordinal) || name == "GameSettingsWindow" || name == "VersionText")
                UnityEngine.Object.DestroyImmediate(behaviour);
        }
    }

    private static Transform? FindRowTemplate(Transform root, string name)
    {
        var row = UiKit.Find(root, name);
        return row != null && row.parent != null && row.parent.name == "Vertical Layout" ? row : null;
    }

    private static void AddSlider(Transform layout, Transform template, Transform? hintTemplate, RateDef def)
    {
        var row = UnityEngine.Object.Instantiate(template.gameObject, layout, false);
        row.name = def.Id;
        row.SetActive(true);

        var option = UiKit.Get<S1.UISlider>(row);
        if (option != null)
            UiKit.SetMember(option, "optionName", def.Label);
        var label = UiKit.Get<TMP_Text>(row.transform.Find("Label"))!;
        label.text = def.Label;
        if (def.IsGroupTotal)
            label.fontStyle |= FontStyles.Bold;
        var slider = UiKit.InChildren<Slider>(row.transform)!;
        FitLabelColumn(row.transform, label, slider);

        var value = UiKit.Get<TMP_Text>(slider.transform.Find("Value"))!;
        slider.wholeNumbers = true;
        slider.minValue = Mathf.Round(def.Min / def.Step);
        slider.maxValue = Mathf.Round(def.Max / def.Step);
        UiKit.OnValueChanged(slider, v =>
        {
            if (_syncing)
                return;
            RatesState.Current.Set(def.Id, v * def.Step);
        });

        RectTransform? hintRect = null;
        if (hintTemplate != null)
        {
            var hint = UnityEngine.Object.Instantiate(hintTemplate.gameObject, label.transform, false);
            hint.SetActive(true);
            var tooltip = UiKit.Get<S1.UI.Tooltips.Tooltip>(hint);
            if (tooltip != null)
                tooltip.text = def.Hint;
            var rect = UiKit.Get<RectTransform>(hint)!;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(label.preferredWidth + 8f, 0f);
            hintRect = rect;
        }
        Rows.Add(new Row { Def = def, Slider = slider, Value = value, Label = label, Hint = hintRect });
    }

    /// <summary>
    /// Limits the label to the space left of the slider (minus room for the hint icon) and lets it
    /// shrink there, so long translated labels never run into the slider.
    /// </summary>
    private static void FitLabelColumn(Transform row, TMP_Text label, Slider slider)
    {
        var rowRect = UiKit.Get<RectTransform>(row)!;
        var labelRect = label.rectTransform;
        var sliderRect = UiKit.Get<RectTransform>(slider.transform)!;
        var left = labelRect.localPosition.x + labelRect.rect.xMin;
        var right = sliderRect.localPosition.x + sliderRect.rect.xMin - HintSpace;
        var height = labelRect.rect.height;
        var y = labelRect.localPosition.y + labelRect.rect.center.y;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(Mathf.Max(60f, right - left), height);
        labelRect.anchoredPosition = new Vector2(left - rowRect.rect.xMin, y - rowRect.rect.center.y);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.fontSizeMax = label.fontSize;
        label.fontSizeMin = label.fontSize * 0.6f;
        label.enableAutoSizing = true;
    }

    private const float HintSpace = 30f;

    /// <summary>Places each hint icon right after its label's rendered text (needs an active, rendered screen).</summary>
    private static void PlaceHints()
    {
        foreach (var row in Rows)
        {
            if (row.Hint == null || row.Label == null || !row.Label.isActiveAndEnabled)
                continue;
            row.Label.ForceMeshUpdate();
            var width = Mathf.Min(row.Label.textBounds.size.x, row.Label.rectTransform.rect.width);
            row.Hint.anchoredPosition = new Vector2(width + 8f, 0f);
        }
    }

    private static void AddBlocker(Transform layout, Transform template)
    {
        // The original sits next to the scroll view and covers the panel for non-hosts.
        var panel = layout.parent?.parent?.parent;
        if (panel == null)
            return;
        var blocker = UnityEngine.Object.Instantiate(template.gameObject, panel, false);
        blocker.name = "WorldRates Blocker";
        var text = UiKit.InChildren<TMP_Text>(blocker.transform);
        if (text != null)
            text.text = "World rates are set by the host";
        Blockers.Add(blocker);
    }

    private static void BuildPresets(Transform layout, Transform sliderTemplate, Transform? buttonTemplate)
    {
        AddText(layout, sliderTemplate, "Quick presets set every experience and income rate (per-source rates back to ×1).");
        if (buttonTemplate == null)
            return;
        foreach (var n in new[] { 1f, 2f, 3f, 5f, 10f })
        {
            var title = n == 1f ? "Vanilla ×1" : "All rates " + Format(n);
            AddButton(layout, buttonTemplate, title, () => RatesState.Current.ApplyPreset(n));
        }
        AddText(layout, sliderTemplate, "Rates are saved per world and apply instantly.");
        AddButton(layout, buttonTemplate, "Use these rates for new worlds", () =>
        {
            RatesState.SaveAsDefaults();
            Notify("Saved as default for new worlds");
        });
    }

    private static void AddText(Transform layout, Transform template, string text)
    {
        var row = UnityEngine.Object.Instantiate(template.gameObject, layout, false);
        row.name = "Text";
        row.SetActive(true);
        var option = UiKit.Get<S1.UISlider>(row);
        if (option != null)
            UnityEngine.Object.DestroyImmediate(option);
        var slider = UiKit.InChildren<Slider>(row.transform);
        if (slider != null)
            UnityEngine.Object.DestroyImmediate(slider.gameObject);
        var label = UiKit.Get<TMP_Text>(row.transform.Find("Label"))!;
        var rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
        rect.anchorMax = new Vector2(1f, rect.anchorMax.y);
        rect.offsetMax = new Vector2(-10f, rect.offsetMax.y);
        label.textWrappingMode = TextWrappingModes.Normal;
        label.fontStyle |= FontStyles.Italic;
        label.text = text;
    }

    private static void AddButton(Transform layout, Transform template, string title, Action onClick)
    {
        var go = UnityEngine.Object.Instantiate(template.gameObject, layout, false);
        go.name = "Button " + title;
        go.SetActive(true);
        var label = UiKit.InChildren<TMP_Text>(go.transform);
        if (label != null)
            label.text = title;
        var button = UiKit.Get<Button>(go)!;
        UiKit.OnClick(button, onClick);
    }

    private static void AddPauseButton(S1.UI.PauseMenu pause)
    {
        var panel = UiKit.Find(pause.Container, "Panel");
        var settings = panel?.Find("Settings");
        if (panel == null || settings == null || panel.Find(ButtonName) != null)
            return;
        var go = UnityEngine.Object.Instantiate(settings.gameObject, panel, false);
        go.name = ButtonName;
        go.transform.SetSiblingIndex(settings.GetSiblingIndex() + 1);
        var text = UiKit.Get<TMP_Text>(go.transform.Find("ButtonText"));
        if (text != null)
            text.text = "World Rates";
        UiKit.OnClick(UiKit.Get<Button>(go)!, Open);
    }

    // --- state -------------------------------------------------------------------------------------

    private static void Sync()
    {
        if (_screen == null)
            return;
        _syncing = true;
        try
        {
            Rows.RemoveAll(r => r.Slider == null);
            foreach (var row in Rows)
            {
                var v = RatesState.Current.Get(row.Def.Id);
                row.Slider.value = Mathf.Round(v / row.Def.Step);
                row.Value.text = Format(v) + EffectiveSuffix(row.Def, v);
            }
        }
        finally
        {
            _syncing = false;
        }
        var readOnly = !RatesState.IsAuthority;
        foreach (var blocker in Blockers)
            if (blocker != null)
                blocker.SetActive(readOnly);
        if (_worldLabel != null)
            _worldLabel.text = RatesState.WorldName ?? "";
    }

    /// <summary>For per-source rows: the rate after the group's "all" multiplier, e.g. "×2 (×6)".</summary>
    private static string EffectiveSuffix(RateDef def, float v)
    {
        if (def.IsGroupTotal || def.Group == RateGroup.Storage)
            return "";
        var total = RatesState.Current.Get(def.Group == RateGroup.Experience ? RateCatalog.XpAll : RateCatalog.IncomeAll);
        return Mathf.Approximately(total, 1f) ? "" : $" <color=#9A9A9A>({Format(v * total)})</color>";
    }

    private static string Format(float v) =>
        v <= 0f ? "Off" : "×" + v.ToString("0.##", CultureInfo.InvariantCulture);

    private static void SetText(Transform root, string name, string text)
    {
        var t = UiKit.Find(root, name);
        var tmp = t != null ? UiKit.Get<TMP_Text>(t) : null;
        if (tmp != null)
            tmp.text = text;
    }

    private static void Notify(string text)
    {
        var notifications = S1.UI.NotificationsManager.Instance;
        if (notifications != null)
            notifications.SendNotification("World Rates", text, null, 3f, true);
    }
}
