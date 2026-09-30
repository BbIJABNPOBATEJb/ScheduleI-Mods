using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace S1Shared;

/// <summary>
/// Applies every [HarmonyPatch] class of the mod one by one. MelonLoader's automatic PatchAll stops at
/// the first failing patch (e.g. after a game update renamed a method), silently skipping the rest;
/// here a broken patch is logged and the others still apply. Use with [assembly: HarmonyDontPatchAll].
/// </summary>
internal static class SafePatcher
{
    public static int Apply(HarmonyLib.Harmony harmony, MelonLogger.Instance log)
    {
        var failed = 0;
        foreach (var type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
        {
            if (!type.GetCustomAttributes(typeof(HarmonyPatch), false).Any())
                continue;
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception ex)
            {
                failed++;
                log.Warning($"Patch {type.FullName} not applied: {(ex.InnerException ?? ex).Message}");
            }
        }
        return failed;
    }
}
