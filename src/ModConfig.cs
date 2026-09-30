using MelonLoader;
using UnityEngine;

namespace S1StarterMod;

/// <summary>
/// Settings in &lt;game&gt;/UserData/MelonPreferences.cfg, section [S1StarterMod].
/// The file is created on first launch; edit it while the game is closed.
/// </summary>
internal static class ModConfig
{
    public static MelonPreferences_Entry<KeyCode> StatusHotkey { get; private set; } = null!;
    public static MelonPreferences_Entry<float> CashMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> DebugLogging { get; private set; } = null!;

    public static void Init()
    {
        var category = MelonPreferences.CreateCategory(ModInfo.Name);

        StatusHotkey = category.CreateEntry("StatusHotkey", KeyCode.F8,
            description: "Key that shows a notification with your cash and the in-game time.");
        CashMultiplier = category.CreateEntry("CashMultiplier", 1f,
            description: "Multiplier applied to every positive cash change. 1 = vanilla.");
        DebugLogging = category.CreateEntry("DebugLogging", false,
            description: "Write extra details to MelonLoader/Latest.log.");
    }
}
