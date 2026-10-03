using S1Shared.UI;
using UnityEngine;

namespace DamageIndicator;

internal enum PlayerBarMode
{
    WhileHurt = 0,
    AfterDamage = 1,
    Always = 2,
    Never = 3,
}

internal enum NpcBarMode
{
    AfterDamage = 0,
    InCombat = 1,
    Never = 2,
}

internal enum NumbersFor
{
    All = 0,
    Yours = 1,
    Off = 2,
}

internal static class Config
{
    public static PrefsFile Prefs = null!;

    public static Setting<bool> Enabled = null!;

    public static Setting<int> PlayerBar = null!;
    public static Setting<float> PlayerBarX = null!;
    public static Setting<float> PlayerBarY = null!;
    public static Setting<float> PlayerBarScale = null!;
    public static Setting<int> PlayerBarWidth = null!;
    public static Setting<bool> PlayerValueText = null!;
    public static Setting<bool> PlayerNumbers = null!;
    public static Setting<bool> PlayerHealNumbers = null!;
    public static Setting<float> PlayerHideDelay = null!;

    public static Setting<int> NpcBars = null!;
    public static Setting<float> NpcBarScale = null!;
    public static Setting<float> NpcHideDelay = null!;
    public static Setting<int> NpcMaxDistance = null!;
    public static Setting<bool> NpcNames = null!;
    public static Setting<bool> NpcValueText = null!;

    public static Setting<bool> Stun = null!;

    public static Setting<int> Numbers = null!;
    public static Setting<float> NumberScale = null!;
    public static Setting<float> NumberDuration = null!;
    public static Setting<int> OthersRange = null!;

    public static readonly Vector2 DefaultPlayerBarPos = new(0.5f, 0.155f);

    public static void Init()
    {
        Prefs = new PrefsFile(ModInfo.Name);
        Enabled = Prefs.Add("Enabled", true, "Master switch.");

        PlayerBar = Prefs.Add("PlayerBar", (int)PlayerBarMode.WhileHurt,
            "Your health bar: 0 = while hurt (hidden at full health), 1 = for a few seconds after damage, 2 = always, 3 = never.");
        PlayerBarX = Prefs.Add("PlayerBarX", DefaultPlayerBarPos.x, "Horizontal position of your health bar (0 = left edge, 1 = right edge).");
        PlayerBarY = Prefs.Add("PlayerBarY", DefaultPlayerBarPos.y, "Vertical position of your health bar (0 = bottom, 1 = top).");
        PlayerBarScale = Prefs.Add("PlayerBarScale", 1f, "Size of your health bar.");
        PlayerBarWidth = Prefs.Add("PlayerBarWidth", 300, "Width of your health bar.");
        PlayerValueText = Prefs.Add("PlayerValueText", true, "Show your health as a number on the bar.");
        PlayerNumbers = Prefs.Add("PlayerDamageNumbers", true, "Show damage you take as numbers next to your bar.");
        PlayerHealNumbers = Prefs.Add("PlayerHealNumbers", true, "Show healing (not slow regeneration) as green numbers.");
        PlayerHideDelay = Prefs.Add("PlayerHideDelay", 4f, "Seconds your bar stays after damage / after healing to full.");

        NpcBars = Prefs.Add("NpcBars", (int)NpcBarMode.AfterDamage,
            "Health bars over characters: 0 = after they take damage, 1 = also while they fight you, 2 = never.");
        NpcBarScale = Prefs.Add("NpcBarScale", 1f, "Size of health bars over characters.");
        NpcHideDelay = Prefs.Add("NpcHideDelay", 6f, "Seconds a character's bar stays after the last damage.");
        NpcMaxDistance = Prefs.Add("NpcMaxDistance", 60, "Characters further away than this (meters) get no bar.");
        NpcNames = Prefs.Add("NpcNames", true, "Show the character's name above the bar.");
        NpcValueText = Prefs.Add("NpcValueText", false, "Show health as a number on characters' bars.");

        Stun = Prefs.Add("StunBars", true, "Yellow bar under the health bar while stunned (tased, staggered, knocked down) and a KO badge.");

        Numbers = Prefs.Add("DamageNumbers", (int)NumbersFor.All, "Floating damage numbers on characters: 0 = all hits, 1 = only your hits, 2 = off.");
        NumberScale = Prefs.Add("NumberScale", 1f, "Size of damage numbers.");
        NumberDuration = Prefs.Add("NumberDuration", 1.2f, "Seconds a damage number stays on screen.");
        OthersRange = Prefs.Add("OthersHitsRange", 30,
            "Hits by others (other players, fights between characters) only show a number and a bar within this distance (meters) and when nothing blocks your view. Your own hits always show.");
    }

    public static PlayerBarMode PlayerBarMode => (PlayerBarMode)Mathf.Clamp(PlayerBar.Value, 0, 3);
    public static NpcBarMode NpcBarMode => (NpcBarMode)Mathf.Clamp(NpcBars.Value, 0, 2);
    public static NumbersFor NumbersFor => (NumbersFor)Mathf.Clamp(Numbers.Value, 0, 2);
}
