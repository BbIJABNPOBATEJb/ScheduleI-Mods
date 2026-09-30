#if DEV
using System.Collections;
using System.Linq;
using GuideArrows.Targets;
using S1Shared;
using S1Shared.Dev;
using UnityEngine;

namespace GuideArrows.Dev;

/// <summary>
/// Smoke scenarios for tools/run-smoke.ps1 -Mod GuideArrows -Scenario &lt;name&gt; [-Save &lt;save folder&gt;].
/// With -Save, a copy of a progressed save shows deals, stashes and several properties.
/// </summary>
internal static class ArrowSmoke
{
    public static IEnumerator Run() => DevSmoke.RunScenario(name => name switch
    {
        "arrows" => Arrows(),
        _ => null,
    });

    private static void Face(S1.PlayerScripts.Player player, Vector3 point, float extraYaw)
    {
        var to = point - player.transform.position;
        to.y = 0f;
        player.transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up) * Quaternion.Euler(0f, extraYaw, 0f);
    }

    private static void LogAngles(string when)
    {
        var cam = S1.PlayerScripts.PlayerCamera.Instance;
        var compass = S1.UI.Compass.CompassManager.Instance;
        foreach (var t in TargetScanner.Targets.Take(4))
        {
            var f = cam.transform.forward; f.y = 0f;
            var to = t.Position - cam.transform.position; to.y = 0f;
            var mine = Vector3.SignedAngle(f, to, Vector3.up);
            var cf = cam.Camera.transform.forward; cf.y = 0f;
            var viaCamera = Vector3.SignedAngle(cf, to, Vector3.up);
            var compassX = 0f;
            if (compass != null)
                compass.GetCompassData(t.Position, out compassX, out _);
            DevSmoke.Log($"[{when}] {t.Kind} '{t.Label}': yaw(PlayerCamera)={mine:0} yaw(Camera)={viaCamera:0} compassX={compassX:0}");
        }
    }

    private static IEnumerator Arrows()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return 3f;
        TargetScanner.Tick(Time.unscaledTime, force: true);
        foreach (var kind in TargetKinds.All)
        {
            var list = TargetScanner.Targets.Where(t => t.Kind == kind).ToList();
            DevSmoke.Log($"{kind}: {list.Count} targets" + (list.Count > 0 ? " e.g. " + string.Join(", ", list.Take(4).Select(t => $"'{t.Label}'")) : ""));
        }
        DevSmoke.Log("Home: " + HomeTracker.Describe());
        DevSmoke.Log($"3D arrows: {Render.ArrowRenderer.Supported}");
        DevSmoke.Check(TargetScanner.Targets.Count > 0, "no targets found");

        yield return 1.5f;
        DevSmoke.Log($"Visible arrows: {GuideArrowsMod.Strip.VisibleCount}");
        yield return DevSmoke.Screenshot("arrows");

        // Orientation check: 0 = away (up on screen), 90 = right, pitch 30 = nose up.
        foreach (var (yaw, pitch) in new[] { (0f, 0f), (90f, 0f), (0f, 30f) })
        {
            UI.ArrowStrip.DebugYaw = yaw;
            UI.ArrowStrip.DebugPitch = pitch;
            yield return 0.5f;
            yield return DevSmoke.Screenshot($"orient_yaw{yaw}_pitch{pitch}");
        }
        UI.ArrowStrip.DebugYaw = null;

        // Turn towards the nearest target: its arrow should point forward (compare with the compass).
        var player = S1.PlayerScripts.Player.Local;
        var nearest = TargetScanner.Targets.OrderBy(t => Vector3.Distance(t.Position, player.transform.position)).First();
        LogAngles("before turning");
        DevSmoke.Log($"Turning to {nearest.Kind} '{nearest.Label}'");
        Face(player, nearest.Position, 0f);
        yield return 2f;
        LogAngles("facing nearest");
        yield return DevSmoke.Screenshot("look_at_nearest");
        Face(player, nearest.Position, 90f);
        yield return 2f;
        LogAngles("nearest on the left");
        yield return DevSmoke.Screenshot("look_right_of_nearest");

        // Modes.
        Config.Mode.Value = (int)ArrowMode.NearestFew;
        yield return 1.5f;
        yield return DevSmoke.Screenshot("mode_nearest_few");
        Config.Mode.Value = (int)ArrowMode.Nearest;
        Config.ShowLabels.Value = true;
        yield return 1.5f;
        yield return DevSmoke.Screenshot("mode_nearest_labels");
        Config.Mode.Value = (int)ArrowMode.PerKind;
        Config.ShowLabels.Value = false;

        // Sleeping remembers the home base.
        HomeTracker.OnSleepStart();
        DevSmoke.Log("Home after sleep: " + HomeTracker.Describe());

        // Settings screen and the layout editor preview.
        yield return DevSmoke.WaitUntil(() => UnityQuery.FindInScenes<S1.UI.MainMenu.SettingsScreen>().Any(s => s.name == "GuideArrowsModScreen"),
            20f, "settings screen");
        GuideArrowsMod.Window.Toggle();
        yield return 2f;
        DevSmoke.Check(GuideArrowsMod.Window.IsOpen, "settings did not open");
        var screen = UnityQuery.FindInScenes<S1.UI.MainMenu.SettingsScreen>().First(s => s.name == "GuideArrowsModScreen");
        for (var i = 0; i < 3; i++)
        {
            screen.Categories[i].Toggle.isOn = true;
            yield return 0.8f;
            yield return DevSmoke.Screenshot("settings_tab" + i);
        }
        GuideArrowsMod.Editor.Begin();
        yield return 1.5f;
        yield return DevSmoke.Screenshot("editor");
        GuideArrowsMod.Editor.End();
        screen.Close();
        yield return 0.5f;
        S1.UI.PauseMenu.Instance.Resume();
        yield return 1f;
        DevSmoke.Finish(true, "arrows done");
    }
}
#endif
