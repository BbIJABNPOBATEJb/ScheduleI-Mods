using HarmonyLib;
using UnityEngine;

namespace S1StarterMod.Patches;

/// <summary>
/// Example Harmony patch: scales every positive cash change by <see cref="ModConfig.CashMultiplier"/>.
/// Prefix runs before the original method; `ref` lets it change the argument the original receives.
/// </summary>
[HarmonyPatch(typeof(S1.Money.MoneyManager), nameof(S1.Money.MoneyManager.ChangeCashBalance))]
internal static class CashMultiplierPatch
{
    private static void Prefix(ref float change)
    {
        var multiplier = ModConfig.CashMultiplier.Value;
        if (change <= 0f || Mathf.Approximately(multiplier, 1f))
            return;

        var original = change;
        change *= multiplier;

        if (ModConfig.DebugLogging.Value)
            Core.Log.Msg($"Cash change {original:0.##} -> {change:0.##} (x{multiplier})");
    }
}
