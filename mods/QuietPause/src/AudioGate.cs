using System;
using S1Shared;
using UnityEngine;

namespace QuietPause;

/// <summary>
/// Silences the game in the background (AudioListener.volume) and while the pause menu is open
/// (AudioListener.pause). The game declares pause/resume audio events but never raises them, and it
/// runs in the background (for multiplayer), so without this everything keeps playing.
/// </summary>
internal static class AudioGate
{
    private const float FadeInSeconds = 0.35f;

    private static float _volume = 1f;
    private static bool _ownsVolume;
    private static bool _pauseApplied;
    private static bool _appliedMusic;

    /// <summary>True while world sounds are paused by this mod.</summary>
    public static bool Paused => _pauseApplied;

    /// <summary>The listener volume this mod currently applies (1 = untouched).</summary>
    public static float Volume => _volume;

    public static void Tick()
    {
        try
        {
            UpdateVolume();
            UpdatePause();
        }
        catch (Exception ex)
        {
            MelonLoader.MelonLogger.Warning($"Audio update failed: {ex.Message}");
        }
    }

    /// <summary>Releases the pause (scene change, quit).</summary>
    public static void Reset()
    {
        if (_pauseApplied)
        {
            AudioListener.pause = false;
            _pauseApplied = false;
        }
    }

#if DEV
    /// <summary>Smoke tests can't take focus away from the window; they flip this instead.</summary>
    public static bool DebugBackground;
#endif

    private static void UpdateVolume()
    {
        var background = !Application.isFocused;
#if DEV
        background |= DebugBackground;
#endif
        var target = background && Config.MuteInBackground ? Config.BackgroundVolume / 100f : 1f;
        if (target < _volume)
            _volume = target; // mute at once, fade back in
        else if (target > _volume)
            _volume = Mathf.MoveTowards(_volume, target, Time.unscaledDeltaTime / FadeInSeconds);
        if (!_ownsVolume && _volume >= 1f)
            return; // never touched the listener: leave it to the game
        AudioListener.volume = _volume;
        _ownsVolume = _volume < 1f;
    }

    private static void UpdatePause()
    {
        var pause = S1.UI.PauseMenu.Instance;
        var want = Config.MuteWhenPaused && pause != null && pause.IsPaused;
        if (want && (!_pauseApplied || _appliedMusic != Config.MusicWhenPaused))
            KeepMenuSoundsAudible();
        if (want == _pauseApplied)
            return;
        AudioListener.pause = want;
        _pauseApplied = want;
    }

    /// <summary>Menu clicks (and optionally music) ignore the listener pause.</summary>
    private static void KeepMenuSoundsAudible()
    {
        _appliedMusic = Config.MusicWhenPaused;
        foreach (var controller in UnityQuery.FindInScenes<S1.Audio.AudioSourceController>())
        {
            try
            {
                var source = UiKit.Get<AudioSource>(controller);
                if (source == null)
                    continue;
                var type = controller.ExtractAudioSettings().AudioType;
                source.ignoreListenerPause = type == S1.Core.Audio.EAudioType.UI
                    || (type == S1.Core.Audio.EAudioType.Music && _appliedMusic);
            }
            catch (Exception)
            {
                // Controllers that never woke up have no AudioSource reference yet.
            }
        }
    }
}
