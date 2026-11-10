using MelonLoader;
using UnityEngine;

namespace ElectricScooter;

/// <summary>Settings in UserData/MelonPreferences.cfg, section [ElectricScooter]. Read when a ride starts.</summary>
internal static class Config
{
    private static MelonPreferences_Entry<int> _price = null!;
    private static MelonPreferences_Entry<int> _speedPercent = null!;
    private static MelonPreferences_Entry<int> _stepHeightCm = null!;
    private static MelonPreferences_Entry<bool> _infiniteStamina = null!;
    private static MelonPreferences_Entry<bool> _offRoad = null!;

    public static float Price => Mathf.Clamp(_price.Value, 0, 1000000);

    /// <summary>Top speed and acceleration relative to the Golden Skateboard.</summary>
    public static float Speed => Mathf.Clamp(_speedPercent.Value, 50, 300) / 100f;

    /// <summary>Height of a curb the scooter rolls over, in metres.</summary>
    public static float StepHeight => Mathf.Clamp(_stepHeightCm.Value, 10, 40) / 100f;

    public static bool InfiniteStamina => _infiniteStamina.Value;

    /// <summary>Full speed on grass and gravel, where the skateboards slow down.</summary>
    public static bool OffRoad
    {
        get
        {
#if DEV
            if (OffRoadOverride.HasValue)
                return OffRoadOverride.Value;
#endif
            return _offRoad.Value;
        }
    }

#if DEV
    /// <summary>Set by the tests instead of the preference, so the player's file is left alone.</summary>
    public static bool? OffRoadOverride;
#endif

    public static void Init()
    {
        var category = MelonPreferences.CreateCategory(ModInfo.Name);
        _price = category.CreateEntry("Price", 5000, description: "Price at the skate shop, $.");
        _speedPercent = category.CreateEntry("SpeedPercent", 130,
            description: "Top speed and acceleration compared to the Golden Skateboard, % (130 = 30% faster). 50-300.");
        _stepHeightCm = category.CreateEntry("StepHeightCm", 20,
            description: "How high a curb the scooter rolls over without stopping, cm (10-40). The wheels grow with it.");
        _infiniteStamina = category.CreateEntry("InfiniteStamina", true,
            description: "Riding the scooter does not use stamina (it is electric, after all).");
        _offRoad = category.CreateEntry("OffRoad", false,
            description: "Ride over grass and gravel at full speed instead of slowing down there like the skateboards.");
    }
}
