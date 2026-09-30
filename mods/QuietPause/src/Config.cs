using MelonLoader;
using S1Shared.UI;

namespace QuietPause;

internal static class Config
{
    public static PrefsFile Prefs = null!;
    private static MelonPreferences_Entry<bool> _muteInBackground = null!;
    private static MelonPreferences_Entry<int> _backgroundVolume = null!;
    private static MelonPreferences_Entry<bool> _muteWhenPaused = null!;
    private static MelonPreferences_Entry<bool> _musicWhenPaused = null!;

    public static void Init()
    {
        Prefs = new PrefsFile(ModInfo.Name);
        _muteInBackground = Prefs.Entry("MuteInBackground", true,
            "Mute the game while its window is minimized or another window has focus.");
        _backgroundVolume = Prefs.Entry("BackgroundVolume", 0,
            "Volume (0-100 %) while the game is in the background. 0 = silent.");
        _muteWhenPaused = Prefs.Entry("MuteWhenPaused", true,
            "Pause world sounds (ambience, footsteps, vehicles, voices...) while the pause menu is open. Menu clicks stay audible.");
        _musicWhenPaused = Prefs.Entry("MusicWhenPaused", false,
            "Keep the music playing in the pause menu.");
    }

    public static bool MuteInBackground
    {
        get => _muteInBackground.Value;
        set => Set(_muteInBackground, value);
    }

    public static int BackgroundVolume
    {
        get => UnityEngine.Mathf.Clamp(_backgroundVolume.Value, 0, 100);
        set => Set(_backgroundVolume, UnityEngine.Mathf.Clamp(value, 0, 100));
    }

    public static bool MuteWhenPaused
    {
        get => _muteWhenPaused.Value;
        set => Set(_muteWhenPaused, value);
    }

    public static bool MusicWhenPaused
    {
        get => _musicWhenPaused.Value;
        set => Set(_musicWhenPaused, value);
    }

    private static void Set<T>(MelonPreferences_Entry<T> entry, T value)
    {
        entry.Value = value;
        Prefs.MarkDirty();
    }
}
