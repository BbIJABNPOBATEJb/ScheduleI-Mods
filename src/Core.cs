using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(S1StarterMod.Core), S1StarterMod.ModInfo.Name, S1StarterMod.ModInfo.Version, S1StarterMod.ModInfo.Author)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace S1StarterMod;

public sealed class Core : MelonMod
{
    internal static MelonLogger.Instance Log { get; private set; } = null!;

    private bool _inGameWorld;

    // Harmony-патчи из этой сборки MelonLoader применяет сам, до этого метода.
    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        ModConfig.Init();
        Log.Msg($"{ModInfo.Name} {ModInfo.Version} loaded");
    }

    // Сцены игры: "Menu", "Main" (игровой мир), "Tutorial".
    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        _inGameWorld = sceneName == "Main";
        if (ModConfig.DebugLogging.Value)
            Log.Msg($"Scene initialized: {sceneName} ({buildIndex})");
    }

    public override void OnUpdate()
    {
        if (_inGameWorld && Input.GetKeyDown(ModConfig.StatusHotkey.Value))
            ShowStatus();
    }

    private static void ShowStatus()
    {
        var money = S1.Money.MoneyManager.Instance;
        var time = S1.GameTime.TimeManager.Instance;
        var notifications = S1.UI.NotificationsManager.Instance;
        if (money == null || time == null || notifications == null)
            return;

        // CurrentTime хранится как HHMM, например 1930 = 19:30.
        var clock = $"{time.CurrentTime / 100:00}:{time.CurrentTime % 100:00}";
        notifications.SendNotification(ModInfo.Name, $"${money.cashBalance:N0} | {clock}", null, 5f, true);
    }
}
