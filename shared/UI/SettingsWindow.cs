using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
#if IL2CPP
using Il2CppInterop.Runtime;
using UnityEngine.Events;
#endif

namespace S1Shared.UI;

/// <summary>
/// A mod settings screen built by cloning the game's in-game Settings screen, so tabs, sliders,
/// toggles, dropdowns, tooltips and controller navigation look and behave like the game's own.
/// Rows are declared once (<see cref="Tab"/>); the screen is (re)built whenever a pause menu exists,
/// gets a button in the pause menu and can be toggled with a hotkey. Values apply immediately.
/// Labels are plain English, so a translation mod can translate them at render time.
/// </summary>
internal sealed class SettingsWindow
{
    private const int MaxTabs = 6;
    /// <summary>Names of cloned screens end with this, so other mods never clone a clone.</summary>
    private const string CloneSuffix = "ModScreen";

    private readonly string _screenName;
    private readonly string _buttonName;
    private readonly string _title;
    private readonly string? _pauseButtonTitle;
    private readonly List<SettingsTab> _tabs = new();

    private S1.UI.MainMenu.SettingsScreen? _screen;
    private RectTransform? _screenRect;
    private bool _buildFailed;
    private bool _openWhenPaused;
    private bool _wasOpen;
    private int _fitIn;
    private string? _subtitle;
    private TMP_Text? _subtitleLabel;

    public SettingsWindow(string id, string title, string? pauseButtonTitle)
    {
        _screenName = id + CloneSuffix;
        _buttonName = id + "Button";
        _title = title;
        _pauseButtonTitle = pauseButtonTitle;
    }

    public bool IsOpen => _screen != null && _screen.IsOpen;

    /// <summary>Fired when the screen closes (Back, Esc, or leaving the world).</summary>
    public event Action? Closed;

    /// <summary>Fired whenever a row changed a value.</summary>
    public event Action? Changed;

    internal bool Syncing { get; private set; }

    public SettingsTab Tab(string title)
    {
        if (_tabs.Count >= MaxTabs)
            throw new InvalidOperationException("The settings screen has room for " + MaxTabs + " tabs");
        var tab = new SettingsTab(this, title);
        _tabs.Add(tab);
        return tab;
    }

    /// <summary>Small text in the corner of the screen (the game shows its version there).</summary>
    public void SetSubtitle(string? text)
    {
        _subtitle = text;
        if (_subtitleLabel != null)
            _subtitleLabel.text = text ?? "";
    }

    /// <summary>Call every frame.</summary>
    public void Tick()
    {
        var pause = S1.UI.PauseMenu.Instance;
        if (_screen == null)
        {
            if (_wasOpen)
            {
                _wasOpen = false;
                Closed?.Invoke();
            }
            if (pause != null && !_buildFailed)
                TryBuild(pause);
            if (pause == null)
                _buildFailed = false;
            return;
        }
        if (_openWhenPaused && pause != null && pause.IsPaused)
        {
            _openWhenPaused = false;
            Open();
        }
        var open = _screen.IsOpen;
        if (_wasOpen && !open)
            Closed?.Invoke();
        _wasOpen = open;
        if (_fitIn > 0 && --_fitIn == 0)
            PlaceHints();
    }

    public void Toggle()
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

    public void Open()
    {
        if (_screen == null)
            return;
        Refresh();
        foreach (var scroll in UnityQuery.GetComponentsInChildren<ScrollRect>(_screen, true))
            scroll.verticalNormalizedPosition = 1f;
        _fitIn = 2;
        _screen.Open();
    }

    /// <summary>
    /// Moves the (open) screen out of view without closing it, e.g. while the player drags HUD elements
    /// around. The pause menu stays active, so the cursor stays free.
    /// </summary>
    public void SetHidden(bool hidden)
    {
        if (_screenRect == null)
            return;
        _screenRect.anchoredPosition = hidden ? new Vector2(0f, 100000f) : Vector2.zero;
    }

    /// <summary>Re-reads every row's value (call after changing settings from code).</summary>
    public void Refresh()
    {
        if (_screen == null)
            return;
        Syncing = true;
        try
        {
            foreach (var tab in _tabs)
                foreach (var row in tab.Rows)
                    row.Sync();
        }
        finally
        {
            Syncing = false;
        }
    }

    internal void NotifyChanged()
    {
        Refresh();
        Changed?.Invoke();
    }

    // --- building ----------------------------------------------------------------------------------

    private void TryBuild(S1.UI.PauseMenu pause)
    {
        try
        {
            Build(pause);
        }
        catch (Exception ex)
        {
            _buildFailed = true;
            _screen = null;
            MelonLogger.Error($"{_title} window could not be built: {ex}");
        }
    }

    private void Build(S1.UI.PauseMenu pause)
    {
        var source = UnityQuery.GetComponentsInChildren<S1.UI.MainMenu.SettingsScreen>(pause, true)
            .FirstOrDefault(s => !s.name.EndsWith(CloneSuffix, StringComparison.Ordinal) && s.name != "WorldRatesScreen")
            ?? throw new InvalidOperationException("in-game Settings screen not found");

        // Work on a clone parked under an inactive holder: nothing in it wakes up until it is ready.
        var holder = new GameObject(_title + " Holder");
        holder.SetActive(false);
        var root = UnityEngine.Object.Instantiate(source.gameObject, holder.transform);
        root.name = _screenName;
        var screen = UiKit.Get<S1.UI.MainMenu.SettingsScreen>(root)!;

        var unused = new GameObject("Unused");
        unused.transform.SetParent(root.transform, false);
        unused.SetActive(false);

        var templates = new RowTemplates
        {
            Slider = FindRow(root.transform, "Master"),
            Toggle = FindRow(root.transform, "VSync") ?? FindRow(root.transform, "PauseOnLoseFocus"),
            Choice = FindRow(root.transform, "Units"),
            Button = UiKit.Find(root.transform, "ViewCommands"),
            Hint = UiKit.Find(root.transform, "Hint"),
        };
        if (templates.Slider == null)
            throw new InvalidOperationException("slider template not found");

        StripSettingsBindings(root.transform);
        foreach (var t in new[] { templates.Slider, templates.Toggle, templates.Choice, templates.Button, templates.Hint })
            t?.SetParent(unused.transform, false);
        var blocker = UiKit.Find(root.transform, "Blocker");
        if (blocker != null)
            UnityEngine.Object.DestroyImmediate(blocker.gameObject);

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

        SetText(root.transform, "Title", _title);
        _subtitleLabel = UiKit.Find(root.transform, "Version") is { } version ? UiKit.Get<TMP_Text>(version) : null;
        if (_subtitleLabel != null)
            _subtitleLabel.text = _subtitle ?? "";

        for (var i = 0; i < categories.Length; i++)
        {
            var toggle = categories[i].Toggle;
            if (i >= _tabs.Count)
            {
                toggle.gameObject.SetActive(false);
                continue;
            }
            var label = UiKit.InChildren<TMP_Text>(toggle);
            if (label != null)
                label.text = _tabs[i].Title;
            UiKit.AddListener(toggle, on =>
            {
                if (on)
                    _fitIn = 2;
            });
            var layout = UiKit.Find(categories[i].Panel.transform, "Vertical Layout");
            if (layout == null)
                continue;
            foreach (var row in _tabs[i].Rows)
                row.Build(layout, templates);
        }

        // Move into the pause menu next to the original and let Awake register the screen.
        root.transform.SetParent(source.transform.parent, false);
        root.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
        UnityEngine.Object.Destroy(holder);
        root.SetActive(true);
        _screen = screen;
        _screenRect = UiKit.Get<RectTransform>(root);
        if (_pauseButtonTitle != null)
            AddPauseButton(pause);
        Refresh();
        MelonLogger.Msg($"{_title} window ready");
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

    private static Transform? FindRow(Transform root, string name)
    {
        var row = UiKit.Find(root, name);
        return row != null && row.parent != null && row.parent.name == "Vertical Layout" ? row : null;
    }

    private void AddPauseButton(S1.UI.PauseMenu pause)
    {
        var panel = UiKit.Find(pause.Container, "Panel");
        var settings = panel?.Find("Settings");
        if (panel == null || settings == null || panel.Find(_buttonName) != null)
            return;
        var go = UnityEngine.Object.Instantiate(settings.gameObject, panel, false);
        go.name = _buttonName;
        go.transform.SetSiblingIndex(settings.GetSiblingIndex() + 1);
        var text = UiKit.Get<TMP_Text>(go.transform.Find("ButtonText"));
        if (text != null)
            text.text = _pauseButtonTitle;
        UiKit.OnClick(UiKit.Get<Button>(go)!, Open);
    }

    /// <summary>Places each hint icon right after its label's rendered text (needs a rendered screen).</summary>
    private void PlaceHints()
    {
        foreach (var tab in _tabs)
            foreach (var row in tab.Rows)
                row.PlaceHint();
    }

    private static void SetText(Transform root, string name, string text)
    {
        var t = UiKit.Find(root, name);
        var tmp = t != null ? UiKit.Get<TMP_Text>(t) : null;
        if (tmp != null)
            tmp.text = text;
    }

    internal sealed class RowTemplates
    {
        public Transform? Slider, Toggle, Choice, Button, Hint;
    }
}

/// <summary>One tab of a <see cref="SettingsWindow"/>. Methods return the tab for chaining.</summary>
internal sealed class SettingsTab
{
    private readonly SettingsWindow _window;
    internal readonly List<SettingsRow> Rows = new();

    internal SettingsTab(SettingsWindow window, string title)
    {
        _window = window;
        Title = title;
    }

    public string Title { get; }

    public SettingsTab Slider(string label, string? hint, float min, float max, float step,
        Func<float> get, Action<float> set, Func<float, string> format)
    {
        Rows.Add(new SliderRow(_window, label, hint, min, max, step, get, set, format));
        return this;
    }

    public SettingsTab Toggle(string label, string? hint, Func<bool> get, Action<bool> set)
    {
        Rows.Add(new ToggleRow(_window, label, hint, get, set));
        return this;
    }

    public SettingsTab Choice(string label, string? hint, string[] options, Func<int> get, Action<int> set)
    {
        Rows.Add(new ChoiceRow(_window, label, hint, options, get, set));
        return this;
    }

    public SettingsTab Button(string title, Action onClick)
    {
        Rows.Add(new ButtonRow(title, onClick));
        return this;
    }

    public SettingsTab Text(string text, bool header = false)
    {
        Rows.Add(new TextRow(text, header));
        return this;
    }
}

internal abstract class SettingsRow
{
    protected TMP_Text? LabelText;
    protected RectTransform? HintRect;

    public abstract void Build(Transform layout, SettingsWindow.RowTemplates templates);

    public virtual void Sync()
    {
    }

    public void PlaceHint()
    {
        if (HintRect == null || LabelText == null || !LabelText.isActiveAndEnabled)
            return;
        LabelText.ForceMeshUpdate();
        var width = Mathf.Min(LabelText.textBounds.size.x, LabelText.rectTransform.rect.width);
        HintRect.anchoredPosition = new Vector2(width + 8f, 0f);
    }

    protected static GameObject Clone(Transform template, Transform layout, string name)
    {
        var row = UnityEngine.Object.Instantiate(template.gameObject, layout, false);
        row.name = name;
        row.SetActive(true);
        return row;
    }

    protected void AddHint(Transform? template, TMP_Text label, string? hint)
    {
        if (template == null || string.IsNullOrEmpty(hint))
            return;
        var go = UnityEngine.Object.Instantiate(template.gameObject, label.transform, false);
        go.SetActive(true);
        var tooltip = UiKit.Get<S1.UI.Tooltips.Tooltip>(go);
        if (tooltip != null)
            tooltip.text = hint;
        var rect = UiKit.Get<RectTransform>(go)!;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(label.preferredWidth + 8f, 0f);
        HintRect = rect;
    }

    /// <summary>
    /// Limits the label to the space left of the control (minus room for the hint icon) and lets it
    /// shrink there, so long translated labels never run into the control.
    /// </summary>
    /// <summary>
    /// The control's left edge in the row's space, through local transforms only: menus are often
    /// scaled to zero while closed, which makes world-space conversions useless.
    /// </summary>
    private static float LeftEdgeIn(Transform row, RectTransform control)
    {
        var x = control.rect.xMin;
        for (Transform? t = control; t != null && t != row; t = t.parent)
            x = t.localPosition.x + t.localScale.x * x;
        return x;
    }

    internal static void FitLabelColumn(Transform row, TMP_Text label, RectTransform control, bool roomForHint = true)
    {
        var rowRect = UiKit.Get<RectTransform>(row)!;
        var labelRect = label.rectTransform;
        var left = labelRect.localPosition.x + labelRect.rect.xMin;
        var controlLeft = LeftEdgeIn(row, control);
        var right = controlLeft - (roomForHint ? 30f : 12f);
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
}

internal sealed class SliderRow : SettingsRow
{
    private readonly SettingsWindow _window;
    private readonly string _label;
    private readonly string? _hint;
    private readonly float _min, _max, _step;
    private readonly Func<float> _get;
    private readonly Action<float> _set;
    private readonly Func<float, string> _format;
    private Slider? _slider;
    private TMP_Text? _value;

    public SliderRow(SettingsWindow window, string label, string? hint, float min, float max, float step,
        Func<float> get, Action<float> set, Func<float, string> format)
    {
        _window = window;
        _label = label;
        _hint = hint;
        _min = min;
        _max = max;
        _step = step;
        _get = get;
        _set = set;
        _format = format;
    }

    public override void Build(Transform layout, SettingsWindow.RowTemplates templates)
    {
        var row = Clone(templates.Slider!, layout, _label);
        var option = UiKit.Get<S1.UISlider>(row);
        if (option != null)
        {
            UiKit.SetMember(option, "optionName", _label);
            UiKit.SetMember(option, "canUpdateValueText", false);
        }
        var label = UiKit.Get<TMP_Text>(row.transform.Find("Label"))!;
        label.text = _label;
        var slider = UiKit.InChildren<Slider>(row.transform)!;
        FitLabelColumn(row.transform, label, UiKit.Get<RectTransform>(slider.transform)!);
        _value = UiKit.Get<TMP_Text>(slider.transform.Find("Value"));
        slider.wholeNumbers = true;
        slider.minValue = Mathf.Round(_min / _step);
        slider.maxValue = Mathf.Round(_max / _step);
        UiKit.OnValueChanged(slider, v =>
        {
            if (_window.Syncing)
                return;
            _set(v * _step);
            _window.NotifyChanged();
        });
        _slider = slider;
        LabelText = label;
        AddHint(templates.Hint, label, _hint);
    }

    public override void Sync()
    {
        if (_slider == null)
            return;
        var v = _get();
        _slider.value = Mathf.Round(v / _step);
        if (_value != null)
            _value.text = _format(v);
    }
}

internal sealed class ToggleRow : SettingsRow
{
    private readonly SettingsWindow _window;
    private readonly string _label;
    private readonly string? _hint;
    private readonly Func<bool> _get;
    private readonly Action<bool> _set;
    private S1.UIToggle? _toggle;

    public ToggleRow(SettingsWindow window, string label, string? hint, Func<bool> get, Action<bool> set)
    {
        _window = window;
        _label = label;
        _hint = hint;
        _get = get;
        _set = set;
    }

    public override void Build(Transform layout, SettingsWindow.RowTemplates templates)
    {
        if (templates.Toggle == null)
            return;
        var row = Clone(templates.Toggle, layout, _label);
        var toggle = UiKit.Get<S1.UIToggle>(row)!;
        UiKit.SetMember(toggle, "optionName", _label);
        var label = UnityQuery.GetComponentsInChildren<TMP_Text>(row.transform, true)
            .First(t => t.name.StartsWith("Option Name", StringComparison.Ordinal));
        label.text = _label;
        var control = row.transform.Find("Toggle (OffOn)");
        if (control != null)
            FitLabelColumn(row.transform, label, UiKit.Get<RectTransform>(control)!);
#if IL2CPP
        toggle.OnChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<bool>>(new Action<bool>(OnChanged)));
#else
        toggle.OnChanged.AddListener(OnChanged);
#endif
        _toggle = toggle;
        LabelText = label;
        AddHint(templates.Hint, label, _hint);
    }

    private void OnChanged(bool value)
    {
        if (_window.Syncing)
            return;
        _set(value);
        _window.NotifyChanged();
    }

    public override void Sync() => _toggle?.SetStateWithoutNotify(_get());
}

internal sealed class ChoiceRow : SettingsRow
{
    private readonly SettingsWindow _window;
    private readonly string _label;
    private readonly string? _hint;
    private readonly string[] _options;
    private readonly Func<int> _get;
    private readonly Action<int> _set;
    private TMP_Dropdown? _dropdown;

    public ChoiceRow(SettingsWindow window, string label, string? hint, string[] options, Func<int> get, Action<int> set)
    {
        _window = window;
        _label = label;
        _hint = hint;
        _options = options;
        _get = get;
        _set = set;
    }

    public override void Build(Transform layout, SettingsWindow.RowTemplates templates)
    {
        if (templates.Choice == null)
            return;
        var row = Clone(templates.Choice, layout, _label);
        var dropdown = UnityQuery.GetComponentsInChildren<TMP_Dropdown>(row.transform, true).First();
        var label = UnityQuery.GetComponentsInChildren<TMP_Text>(row.transform, true)
            .First(t => t.name.StartsWith("Option Name", StringComparison.Ordinal));
        label.text = _label;
        FitLabelColumn(row.transform, label, UiKit.Get<RectTransform>(dropdown.transform)!);
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();
        foreach (var option in _options)
            dropdown.options.Add(new TMP_Dropdown.OptionData(option));
#if IL2CPP
        dropdown.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<int>>(new Action<int>(OnSelected)));
#else
        dropdown.onValueChanged.AddListener(OnSelected);
#endif
        _dropdown = dropdown;
        LabelText = label;
        AddHint(templates.Hint, label, _hint);
    }

    private void OnSelected(int index)
    {
        if (_window.Syncing || index < 0 || index >= _options.Length)
            return;
        _set(index);
        _window.NotifyChanged();
    }

    public override void Sync()
    {
        if (_dropdown == null)
            return;
        _dropdown.SetValueWithoutNotify(Mathf.Clamp(_get(), 0, _options.Length - 1));
        _dropdown.RefreshShownValue();
    }
}

internal sealed class ButtonRow : SettingsRow
{
    private const float ButtonWidth = 460f;

    private readonly string _title;
    private readonly Action _onClick;

    public ButtonRow(string title, Action onClick)
    {
        _title = title;
        _onClick = onClick;
    }

    public override void Build(Transform layout, SettingsWindow.RowTemplates templates)
    {
        if (templates.Button == null)
            return;
        var go = Clone(templates.Button, layout, "Button " + _title);
        var label = UiKit.InChildren<TMP_Text>(go.transform);
        if (label != null)
        {
            label.text = _title;
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = label.fontSize * 0.65f;
        }
        // The game's button is sized for short English words; translated labels need room.
        var rect = UiKit.Get<RectTransform>(go)!;
        rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, ButtonWidth), rect.sizeDelta.y);
        var element = UiKit.Get<LayoutElement>(go) ?? go.AddComponent<LayoutElement>();
        element.minWidth = element.preferredWidth = ButtonWidth;
        UiKit.OnClick(UiKit.Get<Button>(go)!, _onClick);
    }
}

internal sealed class TextRow : SettingsRow
{
    private readonly string _text;
    private readonly bool _header;

    public TextRow(string text, bool header)
    {
        _text = text;
        _header = header;
    }

    public override void Build(Transform layout, SettingsWindow.RowTemplates templates)
    {
        var row = Clone(templates.Slider!, layout, _header ? "Header" : "Text");
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
        if (_header)
            label.fontStyle |= FontStyles.Bold;
        else
            label.fontStyle |= FontStyles.Italic;
        label.text = _text;
    }
}
