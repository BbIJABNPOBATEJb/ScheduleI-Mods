using System;
using System.Globalization;
using GuideArrows.Targets;
using S1Shared.UI;
using UnityEngine;

namespace GuideArrows.UI;

/// <summary>The "Guide Arrows" settings screen (pause menu button).</summary>
internal static class SettingsUi
{
    public static SettingsWindow Create(Action editLayout, Action afterReset)
    {
        var window = new SettingsWindow("GuideArrows", "Guide Arrows", "Guide Arrows");

        window.Tab("General")
            .Toggle("Show arrows", "Also toggled with the " + Config.ToggleKey.Value + " key (see UserData/MelonPreferences.cfg).",
                () => Config.Enabled.Value, v => Config.Enabled.Value = v)
            .Choice("Arrows", "One arrow per kind points at the nearest target of each kind; or a single arrow to the nearest target of any kind; or arrows to the nearest few.",
                new[] { "One per kind", "Nearest only", "Nearest few" },
                () => Config.Mode.Value, v => Config.Mode.Value = v)
            .Slider("Max arrows", null, 1f, 8f, 1f,
                () => Config.MaxArrows.Value, v => Config.MaxArrows.Value = Mathf.RoundToInt(v), Whole)
            .Slider("Hide when closer than", "An arrow disappears once you are this close to its target.", 0f, 50f, 1f,
                () => Config.HideWithin.Value, v => Config.HideWithin.Value = Mathf.RoundToInt(v), Meters)
            .Slider("Ignore further than", "Targets further away get no arrow. Off = no limit.", 0f, 2000f, 50f,
                () => Config.MaxDistance.Value, v => Config.MaxDistance.Value = Mathf.RoundToInt(v), v => v <= 0f ? "Off" : Meters(v))
            .Button("Move and resize the arrows", editLayout)
            .Button("Reset all settings", () =>
            {
                Config.Prefs.ResetAll();
                afterReset();
            });

        var kinds = window.Tab("Targets");
        foreach (var kind in TargetKinds.All)
        {
            var k = kind;
            kinds.Toggle(TargetKinds.Name(k), TargetKinds.Hint(k),
                    () => Config.KindSetting(k).Value, v => Config.KindSetting(k).Value = v)
                .Choice(TargetKinds.Name(k) + " color", null, Palette.Names,
                    () => Config.ColorSetting(k).Value, v => Config.ColorSetting(k).Value = v);
        }
        kinds.Toggle("Tracked quests only", "Quest arrows only for quests you track in the journal.",
                () => Config.QuestsTrackedOnly.Value, v => Config.QuestsTrackedOnly.Value = v)
            .Choice("Home base", "Which of your properties counts as home.",
                new[] { "Last slept (else best equipped)", "Last slept", "Best equipped" },
                () => Config.HomeMode.Value, v => Config.HomeMode.Value = v);

        window.Tab("Look")
            .Slider("Arrow size", null, 40f, 160f, 4f,
                () => Config.Size.Value, v => Config.Size.Value = Mathf.RoundToInt(v), Whole)
            .Slider("Spacing", null, 0f, 80f, 2f,
                () => Config.Spacing.Value, v => Config.Spacing.Value = Mathf.RoundToInt(v), Whole)
            .Slider("View angle", "How steeply the arrows are seen from above.", 15f, 85f, 1f,
                () => Config.ViewAngle.Value, v => Config.ViewAngle.Value = Mathf.RoundToInt(v), v => Mathf.RoundToInt(v) + "°")
            .Toggle("Tilt towards height", "The arrow's nose points up or down at targets above or below you.",
                () => Config.PitchToTarget.Value, v => Config.PitchToTarget.Value = v)
            .Slider("Turn speed", "How quickly arrows swing to a new direction.", 1f, 20f, 0.5f,
                () => Config.TurnSpeed.Value, v => Config.TurnSpeed.Value = v, v => v.ToString("0.#", CultureInfo.InvariantCulture))
            .Slider("Opacity", null, 30f, 100f, 5f,
                () => Config.Opacity.Value, v => Config.Opacity.Value = Mathf.RoundToInt(v), v => Mathf.RoundToInt(v) + "%")
            .Toggle("Distance", "The distance under each arrow.",
                () => Config.ShowDistance.Value, v => Config.ShowDistance.Value = v)
            .Toggle("Labels", "What the arrow points at: the customer's name, the quest step, the stash...",
                () => Config.ShowLabels.Value, v => Config.ShowLabels.Value = v);

        window.SetSubtitle("v" + ModInfo.Version);
        return window;
    }

    private static string Whole(float v) => Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);

    private static string Meters(float v) => Mathf.RoundToInt(v) + " m";
}
