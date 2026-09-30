using MelonLoader;
using S1Shared;
#if DEV
using S1Shared.Dev;
#endif

[assembly: MelonInfo(typeof(QuietPause.QuietPauseMod), QuietPause.ModInfo.Name, QuietPause.ModInfo.Version, QuietPause.ModInfo.Author, QuietPause.ModInfo.DownloadLink)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: HarmonyDontPatchAll]

namespace QuietPause;

public sealed class QuietPauseMod : MelonMod
{
    private CoRunner _runner = null!;

    public override void OnInitializeMelon()
    {
        SafePatcher.Apply(HarmonyInstance, LoggerInstance);
        Config.Init();
        _runner = new CoRunner(ex => LoggerInstance.Error(ex));
#if DEV
        if (DevSmoke.TryInit(ModInfo.Name))
        {
            Config.Prefs.SavingDisabled = true;
            _runner.Start(Dev.QuietSmoke.Run());
        }
#endif
        LoggerInstance.Msg($"{ModInfo.Name} {ModInfo.Version} loaded");
    }

    public override void OnUpdate()
    {
        _runner.Tick();
        AudioGate.Tick();
        Config.Prefs.Tick();
#if DEV
        DevSmoke.CheckTimeout();
#endif
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName) => AudioGate.Reset();

    public override void OnApplicationQuit()
    {
        AudioGate.Reset();
        Config.Prefs.SaveNow();
    }
}
