using System;
using System.Linq;
using HarmonyLib;
using MelonLoader;
using S1Shared;
using UnityEngine;
using UnityEngine.UI;
#if IL2CPP
using Il2CppInterop.Runtime;
using UnityEngine.Events;
#endif

namespace QuietPause;

/// <summary>
/// Adds the mod's options at the end of Settings → Audio (main menu and pause menu), built from the
/// game's own toggle and slider rows.
/// </summary>
internal static class AudioSettingsRows
{
    private const string Marker = "QuietPause Mute In Background";

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
                MelonLogger.Error($"Audio settings rows failed: {ex}");
            }
        }
    }

    private static void Inject(S1.UI.MainMenu.SettingsScreen screen)
    {
        // Other mods clone this screen for their own settings: leave those alone.
        if (screen.name.EndsWith("ModScreen", StringComparison.Ordinal) || screen.name == "WorldRatesScreen")
            return;
        var master = UiKit.Find(screen.transform, "Master");
        var layout = master?.parent;
        if (master == null || layout == null || layout.name != "Vertical Layout" || layout.Find(Marker) != null)
            return;
        var toggleTemplate = UiKit.Find(screen.transform, "PauseOnLoseFocus") ?? UiKit.Find(screen.transform, "VSync");
        if (toggleTemplate == null)
            return;

        AddToggle(layout, toggleTemplate, Marker, "Mute in background",
            () => Config.MuteInBackground, v => Config.MuteInBackground = v);
        AddSlider(layout, master, "QuietPause Background Volume", "Background volume",
            () => Config.BackgroundVolume, v => Config.BackgroundVolume = v);
        AddToggle(layout, toggleTemplate, "QuietPause Mute When Paused", "Mute when paused",
            () => Config.MuteWhenPaused, v => Config.MuteWhenPaused = v);
        AddToggle(layout, toggleTemplate, "QuietPause Music When Paused", "Music in pause menu",
            () => Config.MusicWhenPaused, v => Config.MusicWhenPaused = v);
    }

    private static GameObject CloneStripped(Transform template, Transform layout, string name)
    {
        // Clone under an inactive holder so the cloned settings bindings never wake up.
        var holder = new GameObject("QuietPause Holder");
        holder.SetActive(false);
        var row = UnityEngine.Object.Instantiate(template.gameObject, holder.transform);
        row.name = name;
        foreach (var behaviour in UnityQuery.GetComponentsInChildren<MonoBehaviour>(row.transform, true))
        {
            if (behaviour != null && UiKit.Namespace(behaviour).EndsWith("ScheduleOne.UI.Settings", StringComparison.Ordinal))
                UnityEngine.Object.DestroyImmediate(behaviour);
        }
        return row;
    }

    private static void Activate(GameObject row, GameObject holder, Transform layout)
    {
        row.transform.SetParent(layout, false);
        row.transform.SetAsLastSibling();
        UnityEngine.Object.Destroy(holder);
    }

    private static void AddToggle(Transform layout, Transform template, string name, string label, Func<bool> get, Action<bool> set)
    {
        var row = CloneStripped(template, layout, name);
        var holder = row.transform.parent.gameObject;
        var toggle = UiKit.Get<S1.UIToggle>(row);
        if (toggle == null)
        {
            UnityEngine.Object.Destroy(holder);
            return;
        }
        UiKit.SetMember(toggle, "optionName", label);
        var nameText = UnityQuery.GetComponentsInChildren<TMP_Text>(row.transform, true)
            .FirstOrDefault(t => t.name.StartsWith("Option Name", StringComparison.Ordinal));
        var control = row.transform.Find("Toggle (OffOn)");
        Activate(row, holder, layout);
        // Measure once the row sits in the layout: under the holder its stretched rect has no size.
        if (nameText != null && control != null)
            S1Shared.UI.SettingsRow.FitLabelColumn(row.transform, nameText, UiKit.Get<RectTransform>(control)!, roomForHint: false);
        toggle.SetStateWithoutNotify(get());
        var handler = new Action<bool>(set);
#if IL2CPP
        toggle.OnChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<bool>>(handler));
#else
        toggle.OnChanged.AddListener(new UnityEngine.Events.UnityAction<bool>(handler));
#endif
    }

    private static void AddSlider(Transform layout, Transform template, string name, string label, Func<int> get, Action<int> set)
    {
        var row = CloneStripped(template, layout, name);
        var holder = row.transform.parent.gameObject;
        var option = UiKit.Get<S1.UISlider>(row);
        if (option != null)
            UiKit.SetMember(option, "optionName", label);
        var text = UiKit.Get<TMP_Text>(row.transform.Find("Label"));
        if (text != null)
            text.text = label;
        var slider = UiKit.InChildren<Slider>(row.transform);
        if (slider == null)
        {
            UnityEngine.Object.Destroy(holder);
            return;
        }
        slider.wholeNumbers = true;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.SetValueWithoutNotify(get());
        UiKit.OnValueChanged(slider, v => set(Mathf.RoundToInt(v)));
        Activate(row, holder, layout);
        if (text != null)
            S1Shared.UI.SettingsRow.FitLabelColumn(row.transform, text, UiKit.Get<RectTransform>(slider.transform)!, roomForHint: false);

    }
}
