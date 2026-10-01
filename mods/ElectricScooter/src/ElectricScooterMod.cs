using MelonLoader;
using S1Shared;
#if DEV
using S1Shared.Dev;
#endif

[assembly: MelonInfo(typeof(ElectricScooter.ElectricScooterMod), ElectricScooter.ModInfo.Name, ElectricScooter.ModInfo.Version, ElectricScooter.ModInfo.Author, ElectricScooter.ModInfo.DownloadLink)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: HarmonyDontPatchAll]

namespace ElectricScooter;

public sealed class ElectricScooterMod : MelonMod
{
    private CoRunner _runner = null!;

    public override void OnInitializeMelon()
    {
        SafePatcher.Apply(HarmonyInstance, LoggerInstance);
        Config.Init();
        _runner = new CoRunner(ex => LoggerInstance.Error(ex));
#if DEV
        if (DevSmoke.TryInit(ModInfo.Name))
            _runner.Start(Dev.ScooterSmoke.Run());
#endif
        LoggerInstance.Msg($"{ModInfo.Name} {ModInfo.Version} loaded");
    }

    public override void OnUpdate()
    {
        _runner.Tick();
        ScooterItem.EnsureRegistered();
        ScooterShop.Tick();
        ScooterRide.Tick();
        DressHeldScooter();
#if DEV
        DevSmoke.CheckTimeout();
#endif
    }

    private int _dressedHeld;

    /// <summary>The scooter in the hands is the golden board's view model until it gets its own parts.</summary>
    private void DressHeldScooter()
    {
        var inventory = S1.PlayerScripts.PlayerInventory.Instance;
        if (inventory == null)
            return;
        var equippable = inventory.Equippable;
        if (equippable == null || equippable.GetInstanceID() == _dressedHeld || !ScooterItem.Is(inventory.EquippedItem))
            return;
        _dressedHeld = equippable.GetInstanceID();
        if (UnityQuery.TryCastTo<S1.Skating.Skateboard_Equippable>(equippable, out var board) && !ScooterModel.IsDressed(board.transform))
            ScooterModel.DressHeld(board);
    }
}
