using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using WorldRates.Rates;

namespace WorldRates.Patches;

/// <summary>
/// XP: the methods that award XP mark which source is active; LevelManager.AddXP scales the amount
/// by (all experience × that source). Unmarked XP counts as "Other".
/// </summary>
internal static class XpPatches
{
    private static string? _source;

    public static bool DebugLog { get; set; }

    private static void Begin(string source, out string? previous)
    {
        previous = _source;
        _source = source;
    }

    private static void End(string? previous) => _source = previous;

    private static int Scale(int xp)
    {
        if (xp <= 0 || !RatesState.InWorld || !RatesState.IsAuthority)
            return xp;
        var source = _source ?? RateCatalog.XpOther;
        var mult = RatesState.Multiplier(source, RateCatalog.XpAll);
        var scaled = mult <= 0f ? 0 : Math.Max(1, (int)Math.Round(xp * mult));
        if (DebugLog || scaled != xp)
            MelonLogger.Msg($"XP {xp} -> {scaled} ({source} x{mult:0.##})");
        return scaled;
    }

    // LevelManager.AddXP is a one-instruction stub that only forwards to its RPC writer. Hooking it
    // (together with the writer) corrupted the neighbouring native code on IL2CPP and crashed the game
    // with a stack overflow as soon as a dealer earned XP. Every XP award passes through the writer,
    // synchronously within the source context, so the writer is the one place to scale.
    [HarmonyPatch]
    private static class AddXpWriter
    {
        private static MethodBase TargetMethod() => AccessTools.GetDeclaredMethods(typeof(S1.Levelling.LevelManager))
            .First(m => m.Name.StartsWith("RpcWriter___Server_AddXP", StringComparison.Ordinal));

        private static void Prefix(ref int __0) => __0 = Scale(__0);
    }

    // --- sources ---------------------------------------------------------------------------------

    [HarmonyPatch]
    private static class HandoverServerSide
    {
        private static MethodBase TargetMethod() => AccessTools.GetDeclaredMethods(typeof(S1.Economy.Customer))
            .First(m => m.Name.StartsWith("RpcLogic___ProcessHandoverServerSide", StringComparison.Ordinal));

        private static void Prefix(bool handoverByPlayer, out string? __state) =>
            Begin(handoverByPlayer ? RateCatalog.XpDealPlayer : RateCatalog.XpDealDealer, out __state);

        private static void Finalizer(string? __state) => End(__state);
    }

    [HarmonyPatch]
    private static class CounterOffer
    {
        private static MethodBase TargetMethod() => AccessTools.GetDeclaredMethods(typeof(S1.Economy.Customer))
            .First(m => m.Name.StartsWith("RpcLogic___ProcessCounterOfferServerSide", StringComparison.Ordinal));

        private static void Prefix(out string? __state) => Begin(RateCatalog.XpCounterOffer, out __state);
        private static void Finalizer(string? __state) => End(__state);
    }

    [HarmonyPatch(typeof(S1.Economy.Customer), "SampleConsumed")]
    private static class Sample
    {
        private static void Prefix(out string? __state) => Begin(RateCatalog.XpSample, out __state);
        private static void Finalizer(string? __state) => End(__state);
    }

    [HarmonyPatch(typeof(S1.ObjectScripts.Pot), "OnPlantFullyHarvested")]
    private static class Harvest
    {
        private static void Prefix(out string? __state) => Begin(RateCatalog.XpHarvest, out __state);
        private static void Finalizer(string? __state) => End(__state);
    }

    [HarmonyPatch(typeof(S1.Product.ProductManager), nameof(S1.Product.ProductManager.FinishAndNameMix),
        new[] { typeof(string), typeof(string), typeof(string) })]
    private static class NewMix
    {
        private static void Prefix(out string? __state) => Begin(RateCatalog.XpMix, out __state);
        private static void Finalizer(string? __state) => End(__state);
    }

    [HarmonyPatch(typeof(S1.Quests.Quest), nameof(S1.Quests.Quest.Complete))]
    private static class QuestComplete
    {
        private static void Prefix(out string? __state) => Begin(RateCatalog.XpQuest, out __state);
        private static void Finalizer(string? __state) => End(__state);
    }

    [HarmonyPatch(typeof(S1.PlayerScripts.PlayerCrimeData), "TimeoutPursuit")]
    private static class PoliceEscape
    {
        private static void Prefix(out string? __state) => Begin(RateCatalog.XpPolice, out __state);
        private static void Finalizer(string? __state) => End(__state);
    }

    [HarmonyPatch(typeof(S1.Graffiti.WorldSpraySurface), nameof(S1.Graffiti.WorldSpraySurface.CleanGraffiti))]
    private static class GraffitiClean
    {
        private static void Prefix(out string? __state) => Begin(RateCatalog.XpGraffiti, out __state);
        private static void Finalizer(string? __state) => End(__state);
    }

    [HarmonyPatch(typeof(S1.Graffiti.WorldSpraySurface), "Reward")]
    private static class GraffitiReward
    {
        private static void Prefix(out string? __state) => Begin(RateCatalog.XpGraffiti, out __state);
        private static void Finalizer(string? __state) => End(__state);
    }

    [HarmonyPatch(typeof(S1.NPCs.Behaviour.GraffitiBehaviour), nameof(S1.NPCs.Behaviour.GraffitiBehaviour.Disable))]
    private static class GraffitiStopped
    {
        private static void Prefix(out string? __state) => Begin(RateCatalog.XpGraffiti, out __state);
        private static void Finalizer(string? __state) => End(__state);
    }

    [HarmonyPatch(typeof(S1.UI.PickpocketScreen), "StopArrow")]
    private static class Pickpocket
    {
        private static void Prefix(out string? __state) => Begin(RateCatalog.XpPickpocket, out __state);
        private static void Finalizer(string? __state) => End(__state);
    }
}
