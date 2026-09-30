using System;
using System.Collections;
using System.IO;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace S1Shared.Dev;

/// <summary>
/// In-game smoke-test harness (Dev builds only). Activated by command-line arguments that
/// tools/run-smoke.ps1 passes to the game:
///   --s1dev-mod &lt;ModName&gt; --s1dev-scenario &lt;name&gt; --s1dev-out &lt;dir&gt; [--s1dev-timeout &lt;sec&gt;]
/// Writes smoke.log, screenshots and result.txt (PASS/FAIL line) into the output directory,
/// then quits the game. Saves go to a disposable copy of StreamingAssets/DefaultSave inside
/// the output directory, never to the player's save slots.
/// </summary>
internal static class DevSmoke
{
    public static bool Active { get; private set; }
    public static string Scenario { get; private set; } = "";
    public static string OutDir { get; private set; } = "";
    /// <summary>Free-form scenario parameter (--s1dev-arg), e.g. a language code.</summary>
    public static string Arg { get; private set; } = "";

    private static float _deadline;
    private static bool _finished;
    private static StreamWriter? _log;

    public static bool TryInit(string modName)
    {
        var args = Environment.GetCommandLineArgs();
        string? mod = null, scenario = null, outDir = null;
        var timeout = 240f;
        for (var i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--s1dev-mod": mod = args[++i]; break;
                case "--s1dev-scenario": scenario = args[++i]; break;
                case "--s1dev-out": outDir = args[++i]; break;
                case "--s1dev-timeout": timeout = float.Parse(args[++i]); break;
                case "--s1dev-arg": Arg = args[++i]; break;
            }
        }
        if (!string.Equals(mod, modName, StringComparison.OrdinalIgnoreCase) || scenario == null || outDir == null)
            return false;

        Active = true;
        Scenario = scenario;
        OutDir = Path.GetFullPath(outDir);
        Directory.CreateDirectory(OutDir);
        _log = new StreamWriter(Path.Combine(OutDir, "smoke.log"), append: false) { AutoFlush = true };
        _deadline = Time.realtimeSinceStartup + timeout;
        Log($"Smoke start: mod={modName} scenario={scenario} unity={Application.unityVersion} game={Application.version}");
        return true;
    }

    public static void Log(string message)
    {
        var line = $"[{Time.realtimeSinceStartup,8:0.00}] {message}";
        _log?.WriteLine(line);
        MelonLogger.Msg("[Smoke] " + message);
    }

    /// <summary>Call every frame; fails the run when the deadline passes.</summary>
    public static void CheckTimeout()
    {
        if (Active && !_finished && Time.realtimeSinceStartup > _deadline)
            Finish(false, $"Timeout in scene '{SceneManager.GetActiveScene().name}'");
    }

    public static void Finish(bool ok, string summary)
    {
        if (_finished)
            return;
        _finished = true;
        Log((ok ? "PASS: " : "FAIL: ") + summary);
        File.WriteAllText(Path.Combine(OutDir, "result.txt"), (ok ? "PASS" : "FAIL") + " " + summary + Environment.NewLine);
        _log?.Dispose();
        _log = null;
        Application.Quit();
    }

    public static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds, string what)
    {
        var end = Time.realtimeSinceStartup + timeoutSeconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > end)
                throw new TimeoutException("Timed out waiting for " + what);
            yield return null;
        }
    }

    public static IEnumerator Screenshot(string name)
    {
        var path = Path.Combine(OutDir, name + ".png");
        ScreenCapture.CaptureScreenshot(path);
        // The capture is written at the end of the frame; give it a moment.
        yield return 0.6f;
        Log($"Screenshot {name}.png");
    }

    /// <summary>Starts a fresh game from the built-in default save, stored inside OutDir.</summary>
    public static IEnumerator LoadDisposableSave(bool tutorial = false)
    {
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == "Menu" && S1.Persistence.LoadManager.Instance != null,
            60f, "main menu");
        yield return 2f;

        var loadManager = S1.Persistence.LoadManager.Instance;
        var savePath = Path.Combine(OutDir, tutorial ? "SaveGame_SmokeTutorial" : "SaveGame_Smoke");
        CreateSaveFolder(savePath, tutorial);
        EnableLoadDebugMode(loadManager);

        var metadata = new S1.Persistence.Datas.MetaData(null, null, Application.version, Application.version, tutorial);
        var info = new S1.Persistence.SaveInfo(savePath, -1, "Smoke Test", Now(), Now(), 0f, Application.version, metadata);
        Log("Starting disposable save: " + savePath);
        loadManager.StartGame(info, false, false);

        var scene = tutorial ? "Tutorial" : "Main";
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == scene && !loadManager.IsLoading && loadManager.IsGameLoaded,
            120f, "game load");
        yield return WaitUntil(() => S1.PlayerScripts.Player.Local != null, 30f, "local player");
        yield return 3f;
        Log("Game loaded");
    }

    private static void CreateSaveFolder(string savePath, bool tutorial)
    {
        var defaultSave = Path.Combine(Application.streamingAssetsPath, tutorial ? "DefaultTutorialSave" : "DefaultSave");
        if (Directory.Exists(savePath))
            Directory.Delete(savePath, true);
        CopyDirectory(defaultSave, savePath);
        File.WriteAllText(Path.Combine(savePath, "Game.json"),
            "{\"DataType\":\"GameData\",\"DataVersion\":0,\"GameVersion\":\"" + Application.version +
            "\",\"OrganisationName\":\"Smoke Test\",\"Seed\":7337,\"Settings\":{\"ConsoleEnabled\":true}}");
        File.WriteAllText(Path.Combine(savePath, "Metadata.json"),
            "{\"DataType\":\"MetaData\",\"DataVersion\":0,\"GameVersion\":\"" + Application.version +
            "\",\"CreationDate\":null,\"LastPlayedDate\":null,\"CreationVersion\":\"" + Application.version +
            "\",\"LastSaveVersion\":\"" + Application.version + "\",\"PlayTutorial\":" + (tutorial ? "true" : "false") + "}");
    }

    private static void EnableLoadDebugMode(S1.Persistence.LoadManager loadManager)
    {
        // DebugMode skips the new-character intro cutscene.
        var setter = typeof(S1.Persistence.LoadManager).GetProperty("DebugMode")!.GetSetMethod(true)!;
        setter.Invoke(loadManager, new object[] { true });
    }

#if IL2CPP
    private static Il2CppSystem.DateTime Now() => new(DateTime.Now.Ticks);
#else
    private static DateTime Now() => DateTime.Now;
#endif

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(source, target));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(source, target), true);
    }
}
