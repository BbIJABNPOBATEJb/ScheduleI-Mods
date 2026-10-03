using System;
using DamageIndicator.Combat;
using DamageIndicator.UI;
using MelonLoader;
using S1Shared;
using S1Shared.UI;
using UnityEngine;
#if DEV
using S1Shared.Dev;
#endif

[assembly: MelonInfo(typeof(DamageIndicator.DamageIndicatorMod), DamageIndicator.ModInfo.Name, DamageIndicator.ModInfo.Version, DamageIndicator.ModInfo.Author, DamageIndicator.ModInfo.DownloadLink)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: HarmonyDontPatchAll]

namespace DamageIndicator;

public sealed class DamageIndicatorMod : MelonMod
{
    private static readonly Color YourHitColor = Color.white;
    private static readonly Color OtherHitColor = new(0.82f, 0.82f, 0.82f, 1f);
    private static readonly Color KillColor = new(1f, 0.32f, 0.28f, 1f);

    private CoRunner _runner = null!;
    private string? _lastError;

    internal static HudCanvas Canvas { get; private set; } = null!;
    internal static DamageNumbers Numbers { get; private set; } = null!;
    internal static NpcBars NpcBars { get; private set; } = null!;
    internal static PlayerBar PlayerBar { get; private set; } = null!;
    internal static SettingsWindow Window { get; private set; } = null!;
    internal static HudEditor Editor { get; private set; } = null!;

    public override void OnInitializeMelon()
    {
        SafePatcher.Apply(HarmonyInstance, LoggerInstance);
        Config.Init();
        Canvas = new HudCanvas("DamageIndicator HUD");
        Numbers = new DamageNumbers(Canvas);
        NpcBars = new NpcBars(Canvas);
        PlayerBar = new PlayerBar(Canvas, Numbers);
        Window = SettingsUi.Create(() => Editor!.Begin(), () =>
        {
            Window!.Refresh();
            PlayerBar.Element.Apply();
        });
        Editor = new HudEditor(Canvas, Window, new[] { PlayerBar.Element });
        Editor.Started += () => PlayerBar.Preview = true;
        Editor.Finished += _ => PlayerBar.Preview = false;
        CombatHooks.NpcHit += OnNpcHit;

        _runner = new CoRunner(ex => LoggerInstance.Error(ex));
#if DEV
        if (DevSmoke.TryInit(ModInfo.Name))
        {
            Config.Prefs.SavingDisabled = true;
            _runner.Start(Dev.DamageSmoke.Run());
        }
#endif
        LoggerInstance.Msg($"{ModInfo.Name} {ModInfo.Version} loaded");
    }

    public override void OnUpdate()
    {
        _runner.Tick();
        Window.Tick();
        Editor.Tick();
        Config.Prefs.Tick();
#if DEV
        DevSmoke.CheckTimeout();
#endif
    }

    public override void OnLateUpdate()
    {
        try
        {
            if (!Canvas.Tick() && !Canvas.ShowWhilePaused)
                return;
            var camera = MainCamera();
            PlayerBar.Tick();
            NpcBars.Tick(camera);
            Numbers.Tick(camera);
        }
        catch (Exception ex)
        {
            if (_lastError != ex.Message)
                LoggerInstance.Warning($"HUD update failed: {ex}");
            _lastError = ex.Message;
        }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        NpcBars.Clear();
        Numbers.Clear();
        PlayerBar.Reset();
    }

    public override void OnApplicationQuit()
    {
        if (Config.Prefs.SavingDisabled)
            Config.Prefs.RestoreLoadedValues(); // smoke test: leave the player's settings as they were
        else
            Config.Prefs.SaveNow();
    }

    private static void OnNpcHit(NpcHit hit)
    {
        // Your own hits always show. Someone else's only when you saw it: nearby and not behind a wall.
        if (!hit.ByLocalPlayer && !Sight.Witnessed(MainCamera(), hit, Config.OthersRange.Value))
            return;
        NpcBars.OnHit(hit);
        var mode = Config.NumbersFor;
        if (!Config.Enabled.Value || mode == NumbersFor.Off || (mode == NumbersFor.Yours && !hit.ByLocalPlayer))
            return;

        var finalBlow = hit.HealthBefore > 0f && hit.HealthAfter <= 0f;
        var size = 30f * Config.NumberScale.Value;
        var max = 100f;
        try
        {
            max = Mathf.Max(1f, hit.Npc.Health.MaxHealth);
        }
        catch (Exception)
        {
            // NPC data not ready: assume the default.
        }
        if (hit.Damage >= max * 0.35f)
            size *= 1.25f;

        var text = Mathf.Max(1, Mathf.RoundToInt(hit.Damage)).ToString();
        Color color;
        if (finalBlow && hit.Lethal)
        {
            color = KillColor;
            size *= 1.15f;
        }
        else if (finalBlow)
        {
            color = HealthBarView.StunColor;
            text += " KO";
            size *= 1.15f;
        }
        else
        {
            color = hit.ByLocalPlayer ? YourHitColor : OtherHitColor;
        }
        var point = hit.Point;
        if (point == Vector3.zero)
            point = hit.Npc.transform.position + Vector3.up * 1.5f;
        Numbers.SpawnWorld(point, text, color, size);
    }

    internal static Camera? MainCamera()
    {
        var playerCamera = S1.PlayerScripts.PlayerCamera.Instance;
        return playerCamera != null ? playerCamera.Camera : Camera.main;
    }
}
