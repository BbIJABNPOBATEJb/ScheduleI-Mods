#if DEV
using System.Collections;
using System.Linq;
using S1Shared;
using S1Shared.Dev;
using UnityEngine;

namespace QuietPause.Dev;

/// <summary>Smoke scenarios for tools/run-smoke.ps1 -Mod QuietPause -Scenario &lt;name&gt;.</summary>
internal static class QuietSmoke
{
    public static IEnumerator Run() => DevSmoke.RunScenario(name => name switch
    {
        "audio" => Audio(),
        _ => null,
    });

    private static IEnumerator Audio()
    {
        yield return DevSmoke.LoadDisposableSave();
        DevSmoke.Check(!AudioListener.pause, "listener paused before pausing");

        // Background: muted at once, fades back in.
        AudioGate.DebugBackground = true;
        yield return 0.3f;
        DevSmoke.Log($"Background volume: {AudioListener.volume}");
        DevSmoke.Check(AudioListener.volume == 0f, "not muted in background");
        AudioGate.DebugBackground = false;
        yield return 1f;
        if (Application.isFocused)
            DevSmoke.Check(AudioListener.volume == 1f, "volume not restored after focus returned");
        else
            DevSmoke.Log("Game window is not focused: volume stays muted (expected), restore not checked");

        // Pause menu: world sounds pause, menu sounds stay.
        var pause = S1.UI.PauseMenu.Instance;
        pause.Pause();
        yield return 1f;
        DevSmoke.Check(AudioListener.pause, "listener not paused in pause menu");
        var controllers = UnityQuery.FindInScenes<S1.Audio.AudioSourceController>();
        var audible = controllers.Count(c => UiKit.Get<AudioSource>(c) is { ignoreListenerPause: true });
        DevSmoke.Log($"Paused: {audible}/{controllers.Count} sources stay audible (menu sounds)");
        DevSmoke.Check(audible > 0, "no menu sound kept audible");

        // Settings → Audio shows the options; switching one off unpauses at once.
        var settings = UnityQuery.GetComponentsInChildren<S1.UI.MainMenu.SettingsScreen>(pause, true)
            .First(s => !s.name.EndsWith("ModScreen") && s.name != "WorldRatesScreen");
        settings.Open();
        yield return 0.5f;
        var audioTab = settings.Categories.First(c => UiKit.Find(c.Panel.transform, "Master") != null);
        audioTab.Toggle.isOn = true;
        yield return 0.5f;
        foreach (var scroll in UnityQuery.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(audioTab.Panel.transform, true))
            scroll.verticalNormalizedPosition = 0f;
        yield return 0.5f;
        yield return DevSmoke.Screenshot("audio_settings");
        var row = UiKit.Find(settings.transform, "QuietPause Mute When Paused");
        DevSmoke.Check(row != null, "settings rows missing");
        UiKit.Get<S1.UIToggle>(row!)!.SetState(false);
        yield return 0.3f;
        DevSmoke.Check(!AudioListener.pause, "turning the option off did not unpause");
        UiKit.Get<S1.UIToggle>(row!)!.SetState(true);
        yield return 0.3f;
        DevSmoke.Check(AudioListener.pause, "turning the option on did not pause");
        settings.Close();
        yield return 0.5f;

        pause.Resume();
        yield return 1f;
        DevSmoke.Check(!AudioListener.pause, "listener still paused after resume");
        DevSmoke.Finish(true, "audio done");
    }
}
#endif
