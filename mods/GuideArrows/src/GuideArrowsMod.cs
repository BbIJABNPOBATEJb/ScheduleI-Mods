using System;
using System.Linq;
using GuideArrows.Render;
using GuideArrows.Targets;
using GuideArrows.UI;
using MelonLoader;
using S1Shared;
using S1Shared.UI;
using UnityEngine;
#if DEV
using S1Shared.Dev;
#endif

[assembly: MelonInfo(typeof(GuideArrows.GuideArrowsMod), GuideArrows.ModInfo.Name, GuideArrows.ModInfo.Version, GuideArrows.ModInfo.Author, GuideArrows.ModInfo.DownloadLink)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: HarmonyDontPatchAll]

namespace GuideArrows;

public sealed class GuideArrowsMod : MelonMod
{
    private CoRunner _runner = null!;
    private string? _lastError;

    internal static bool SmokeActive { get; private set; }
    internal static HudCanvas Canvas { get; private set; } = null!;
    internal static ArrowStrip Strip { get; private set; } = null!;
    internal static SettingsWindow Window { get; private set; } = null!;
    internal static HudEditor Editor { get; private set; } = null!;

    public override void OnInitializeMelon()
    {
        SafePatcher.Apply(HarmonyInstance, LoggerInstance);
        Config.Init();
        Canvas = new HudCanvas("GuideArrows HUD");
        Strip = new ArrowStrip(Canvas);
        Window = SettingsUi.Create(() => Editor!.Begin(), () =>
        {
            Window!.Refresh();
            Strip.Element.Apply();
        });
        Editor = new HudEditor(Canvas, Window, new[] { Strip.Element });
        Editor.Started += () => Strip.Preview = true;
        Editor.Finished += _ => Strip.Preview = false;

        _runner = new CoRunner(ex => LoggerInstance.Error(ex));
#if DEV
        if (DevSmoke.TryInit(ModInfo.Name))
        {
            SmokeActive = true;
            Config.Prefs.SavingDisabled = true;
            _runner.Start(Dev.ArrowSmoke.Run());
        }
#endif
        LoggerInstance.Msg($"{ModInfo.Name} {ModInfo.Version} loaded");
    }

    public override void OnUpdate()
    {
        _runner.Tick();
        HomeTracker.Tick();
        Window.Tick();
        Editor.Tick();
        Config.Prefs.Tick();
        if (Keys.Down(Config.ToggleKey.Value) && S1.PlayerScripts.Player.Local != null)
        {
            Config.Enabled.Value = !Config.Enabled.Value;
            Window.Refresh();
        }
#if DEV
        DevSmoke.CheckTimeout();
#endif
    }

    public override void OnLateUpdate()
    {
        try
        {
            if (!Canvas.Tick())
            {
                TargetOutlines.Tick(Enumerable.Empty<(GameObject, Color)>());
                return;
            }
            TargetScanner.Tick(Time.unscaledTime);
            Strip.Tick(MainCamera());
            TargetOutlines.Tick(Config.Outline.Value && WorldLoaded() ? Strip.Glowing : Enumerable.Empty<(GameObject, Color)>());
        }
        catch (Exception ex)
        {
            if (_lastError != ex.Message)
                LoggerInstance.Warning($"Arrow update failed: {ex}");
            _lastError = ex.Message;
        }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        Strip.Clear();
        TargetOutlines.Clear();
    }

    public override void OnApplicationQuit()
    {
        if (Config.Prefs.SavingDisabled)
            Config.Prefs.RestoreLoadedValues(); // smoke test: leave the player's settings as they were
        else
            Config.Prefs.SaveNow();
    }

    /// <summary>Outlines only once the world has finished loading: people are still being set up before that.</summary>
    private static bool WorldLoaded()
    {
        var loadManager = S1.Persistence.LoadManager.Instance;
        return loadManager != null && loadManager.IsGameLoaded && !loadManager.IsLoading;
    }

    internal static Camera? MainCamera()
    {
        var playerCamera = S1.PlayerScripts.PlayerCamera.Instance;
        return playerCamera != null ? playerCamera.Camera : Camera.main;
    }
}
