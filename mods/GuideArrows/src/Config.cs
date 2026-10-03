using System.Collections.Generic;
using GuideArrows.Targets;
using S1Shared.UI;
using UnityEngine;

namespace GuideArrows;

internal enum ArrowMode
{
    PerKind = 0,
    Nearest = 1,
    NearestFew = 2,
}

internal static class Config
{
    public static PrefsFile Prefs = null!;

    public static Setting<bool> Enabled = null!;
    public static Setting<KeyCode> ToggleKey = null!;
    public static Setting<int> Mode = null!;
    public static Setting<int> MaxArrows = null!;

    public static Setting<float> PosX = null!;
    public static Setting<float> PosY = null!;
    public static Setting<float> Scale = null!;
    public static Setting<int> Size = null!;
    public static Setting<int> Spacing = null!;
    public static Setting<int> ViewAngle = null!;
    public static Setting<bool> PitchToTarget = null!;
    public static Setting<float> TurnSpeed = null!;
    public static Setting<int> Opacity = null!;
    public static Setting<bool> ShowDistance = null!;
    public static Setting<bool> ShowLabels = null!;
    public static Setting<int> HideWithin = null!;
    public static Setting<int> MaxDistance = null!;
    public static Setting<bool> Outline = null!;

    public static Setting<bool> QuestsTrackedOnly = null!;
    public static Setting<int> SettingsVersion = null!;
    public static Setting<int> HomeMode = null!;

    private static readonly Dictionary<TargetKind, Setting<bool>> KindOn = new();
    private static readonly Dictionary<TargetKind, Setting<int>> KindColor = new();

    public static readonly Vector2 DefaultPos = new(0.5f, 0.87f);

    public static void Init()
    {
        Prefs = new PrefsFile(ModInfo.Name);
        Enabled = Prefs.Add("Enabled", true, "Show the arrows.");
        ToggleKey = Prefs.Add("ToggleKey", KeyCode.F7, "Key that shows/hides the arrows. None disables it.");
        Mode = Prefs.Add("Mode", (int)ArrowMode.PerKind,
            "0 = one arrow per kind (the nearest of each), 1 = a single arrow to the nearest target, 2 = arrows to the nearest few targets.");
        MaxArrows = Prefs.Add("MaxArrows", 5, "Most arrows shown at once.");

        PosX = Prefs.Add("PositionX", DefaultPos.x, "Horizontal position of the arrows (0 = left, 1 = right).");
        PosY = Prefs.Add("PositionY", DefaultPos.y, "Vertical position of the arrows (0 = bottom, 1 = top). Default: under the compass.");
        Scale = Prefs.Add("Scale", 1f, "Overall size of the arrow row.");
        Size = Prefs.Add("ArrowSize", 76, "Size of one arrow.");
        Spacing = Prefs.Add("Spacing", 18, "Space between arrows.");
        ViewAngle = Prefs.Add("ViewAngle", 52, "How steeply the arrows are seen from above (degrees).");
        PitchToTarget = Prefs.Add("PitchToTarget", true, "Tilt the arrow up or down towards targets above or below you.");
        TurnSpeed = Prefs.Add("TurnSpeed", 7f, "How quickly arrows turn to a new direction.");
        Opacity = Prefs.Add("Opacity", 95, "Arrow opacity in percent.");
        ShowDistance = Prefs.Add("ShowDistance", true, "Show the distance under each arrow.");
        ShowLabels = Prefs.Add("ShowLabels", true, "Show what the arrow points at (customer, quest, stash...).");
        HideWithin = Prefs.Add("HideWithin", 0, "Hide an arrow once you are closer than this (meters). 0 = never hide.");
        MaxDistance = Prefs.Add("MaxDistance", 0, "Ignore targets further away than this (meters). 0 = no limit.");
        Outline = Prefs.Add("Outline", true, "The people and stashes the arrows point at glow in the arrow's color, even through walls.");

        QuestsTrackedOnly = Prefs.Add("QuestsTrackedOnly", true, "Quest arrows only for quests you track in the journal.");
        HomeMode = Prefs.Add("HomeMode", (int)Targets.HomeMode.Auto,
            "Home base: 0 = where you last slept (else the most equipped property), 1 = only where you last slept, 2 = the property with the most valuable equipment.");

        foreach (var kind in TargetKinds.All)
        {
            KindOn[kind] = Prefs.Add("Show" + kind, true, "Arrows to " + TargetKinds.Name(kind).ToLowerInvariant() + ".");
            KindColor[kind] = Prefs.Add("Color" + kind, TargetKinds.DefaultColor(kind),
                "Color: 0 green, 1 red, 2 orange, 3 yellow, 4 purple, 5 blue, 6 cyan, 7 pink, 8 white.");
        }

        SettingsVersion = Prefs.Add("SettingsVersion", 0, "Which defaults the settings above were last updated to (do not change).");
        UpdateDefaults();
    }

    /// <summary>
    /// Settings saved by 1.0.x still hold that version's defaults: arrows hid within 6 m and had no labels.
    /// Those move to the new defaults; values the player chose stay.
    /// </summary>
    private static void UpdateDefaults()
    {
        if (SettingsVersion.Value >= 2)
            return;
        if (HideWithin.Value == 6)
            HideWithin.Value = HideWithin.Default;
        if (!ShowLabels.Value)
            ShowLabels.Value = ShowLabels.Default;
        SettingsVersion.Value = 2;
    }

    public static ArrowMode ArrowMode => (ArrowMode)Mathf.Clamp(Mode.Value, 0, 2);

    public static bool KindEnabled(TargetKind kind) => KindOn[kind].Value;

    public static Setting<bool> KindSetting(TargetKind kind) => KindOn[kind];

    public static Setting<int> ColorSetting(TargetKind kind) => KindColor[kind];

    public static Color ColorOf(TargetKind kind) => Palette.Get(KindColor[kind].Value);
}
