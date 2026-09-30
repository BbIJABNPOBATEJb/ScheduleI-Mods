using System;
using System.Globalization;
using S1Shared.UI;
using UnityEngine;

namespace DamageIndicator.UI;

/// <summary>The "Damage Indicator" settings screen (pause menu button).</summary>
internal static class SettingsUi
{
    public static SettingsWindow Create(Action editLayout, Action afterReset)
    {
        var window = new SettingsWindow("DamageIndicator", "Damage Indicator", "Damage Indicator");

        window.Tab("General")
            .Toggle("Enabled", "Turns every health bar and damage number on or off.",
                () => Config.Enabled.Value, v => Config.Enabled.Value = v)
            .Toggle("Stun bars", "A yellow bar under the health bar while someone is stunned: tased, staggered by a heavy hit or knocked down. Knocked-out characters get a KO badge.",
                () => Config.Stun.Value, v => Config.Stun.Value = v)
            .Text("Nobody's health is shown until they take damage (unless you change it below).")
            .Button("Move and resize your health bar", editLayout)
            .Button("Reset all settings", () =>
            {
                Config.Prefs.ResetAll();
                afterReset();
            });

        window.Tab("Your health")
            .Choice("Health bar", "When your own health bar is visible.",
                new[] { "While hurt", "After damage", "Always", "Never" },
                () => Config.PlayerBar.Value, v => Config.PlayerBar.Value = v)
            .Slider("Width", null, 160f, 600f, 20f,
                () => Config.PlayerBarWidth.Value, v => Config.PlayerBarWidth.Value = Mathf.RoundToInt(v), v => v.ToString("0", CultureInfo.InvariantCulture))
            .Slider("Size", null, 0.5f, 2.5f, 0.1f,
                () => Config.PlayerBarScale.Value, v => Config.PlayerBarScale.Value = v, Percent)
            .Slider("Stays for", "Seconds the bar stays after damage, or after you healed back to full.", 1f, 15f, 1f,
                () => Config.PlayerHideDelay.Value, v => Config.PlayerHideDelay.Value = v, Seconds)
            .Toggle("Health number", "Your health as a number on the bar.",
                () => Config.PlayerValueText.Value, v => Config.PlayerValueText.Value = v)
            .Toggle("Damage numbers", "Damage you take pops up next to your bar.",
                () => Config.PlayerNumbers.Value, v => Config.PlayerNumbers.Value = v)
            .Toggle("Healing numbers", "Healing (food, medicine) pops up in green. Slow regeneration is not shown.",
                () => Config.PlayerHealNumbers.Value, v => Config.PlayerHealNumbers.Value = v);

        window.Tab("Characters")
            .Choice("Health bars", "Bars over the heads of other characters.",
                new[] { "After damage", "After damage and in fights", "Never" },
                () => Config.NpcBars.Value, v => Config.NpcBars.Value = v)
            .Slider("Size", null, 0.5f, 2f, 0.1f,
                () => Config.NpcBarScale.Value, v => Config.NpcBarScale.Value = v, Percent)
            .Slider("Stays for", "Seconds a bar stays after the character's last damage.", 2f, 30f, 1f,
                () => Config.NpcHideDelay.Value, v => Config.NpcHideDelay.Value = v, Seconds)
            .Slider("Max distance", "Characters further away get no bar.", 10f, 150f, 5f,
                () => Config.NpcMaxDistance.Value, v => Config.NpcMaxDistance.Value = Mathf.RoundToInt(v), Meters)
            .Toggle("Names", "The character's name above the bar.",
                () => Config.NpcNames.Value, v => Config.NpcNames.Value = v)
            .Toggle("Health number", "Health as a number on the bar.",
                () => Config.NpcValueText.Value, v => Config.NpcValueText.Value = v);

        window.Tab("Damage numbers")
            .Choice("Show", "Numbers that pop up where a character is hit.",
                new[] { "All hits", "Only your hits", "Off" },
                () => Config.Numbers.Value, v => Config.Numbers.Value = v)
            .Slider("Size", null, 0.5f, 2.5f, 0.1f,
                () => Config.NumberScale.Value, v => Config.NumberScale.Value = v, Percent)
            .Slider("Duration", "Seconds a number stays on screen.", 0.5f, 3f, 0.1f,
                () => Config.NumberDuration.Value, v => Config.NumberDuration.Value = v, Seconds);

        window.SetSubtitle("v" + ModInfo.Version);
        return window;
    }

    private static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";

    private static string Seconds(float v) => v.ToString("0.#", CultureInfo.InvariantCulture) + " s";

    private static string Meters(float v) => v.ToString("0", CultureInfo.InvariantCulture) + " m";
}
