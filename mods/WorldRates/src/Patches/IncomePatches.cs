using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using WorldRates.Rates;

namespace WorldRates.Patches;

/// <summary>
/// Income multipliers. Deals scale the contract payment before the game uses it, so bonuses, the deal
/// popup, the cash paid, dealer cuts and daily summaries all agree.
/// </summary>
internal static class IncomePatches
{
    private static readonly HashSet<int> ScaledContracts = new();
    private static string? _cashSource;

    private static float Multiplier(string id) =>
        RatesState.InWorld && RatesState.IsAuthority ? RatesState.Multiplier(id, RateCatalog.IncomeAll) : 1f;

    private static float Scale(float amount, string id)
    {
        var mult = Multiplier(id);
        if (amount <= 0f || mult == 1f)
            return amount;
        var scaled = amount * mult;
        MelonLogger.Msg($"Income {amount:0.##} -> {scaled:0.##} ({id} x{mult:0.##})");
        return scaled;
    }

    [HarmonyPatch(typeof(S1.Economy.Customer), nameof(S1.Economy.Customer.ProcessHandover))]
    private static class DealPayment
    {
        private static readonly System.Reflection.PropertyInfo PaymentProperty =
            AccessTools.Property(typeof(S1.Quests.Contract), nameof(S1.Quests.Contract.Payment));

        // Only the parameters both game versions have: 0.4.6 also passes an (unused) handover outcome first.
        private static void Prefix(S1.Quests.Contract contract, bool handoverByPlayer)
        {
            if (contract == null)
                return;
            if (!ScaledContracts.Add(contract.GetInstanceID()))
                return;
            // Payment has a protected setter on Mono.
            PaymentProperty.SetValue(contract, Scale(contract.Payment, handoverByPlayer ? RateCatalog.IncomeDealPlayer : RateCatalog.IncomeDealDealer));
        }
    }

    [HarmonyPatch(typeof(S1.Property.Business), "CompleteOperation")]
    private static class Laundering
    {
        private static void Prefix(S1.Property.LaunderingOperation op)
        {
            if (op != null)
                op.amount = Scale(op.amount, RateCatalog.IncomeLaundering);
        }
    }

    [HarmonyPatch(typeof(S1.UI.PawnShopInterface), "FinalizeDeal")]
    private static class PawnShop
    {
        private static void Prefix(ref float amount) => amount = Scale(amount, RateCatalog.IncomePawnShop);
    }

    [HarmonyPatch(typeof(S1.ObjectScripts.Recycler), nameof(S1.ObjectScripts.Recycler.CashInteracted))]
    private static class Recycling
    {
        private static void Prefix(out string? __state)
        {
            __state = _cashSource;
            _cashSource = RateCatalog.IncomeRecycling;
        }

        private static void Finalizer(string? __state) => _cashSource = __state;
    }

    [HarmonyPatch(typeof(S1.Money.MoneyManager), nameof(S1.Money.MoneyManager.ChangeCashBalance))]
    private static class CashChange
    {
        private static void Prefix(ref float change)
        {
            if (_cashSource != null)
                change = Scale(change, _cashSource);
        }
    }
}
