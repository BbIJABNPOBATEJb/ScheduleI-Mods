using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using S1Shared;
using UnityEngine;

namespace DamageIndicator.Combat;

/// <summary>A character took damage (runs on every machine: impacts are broadcast to all clients).</summary>
internal sealed class NpcHit
{
    public S1.NPCs.NPC Npc = null!;
    public float Damage;
    public float HealthBefore;
    public float HealthAfter;
    public bool Lethal;
    public bool ByLocalPlayer;
    public Vector3 Point;
    public float Force;
}

/// <summary>
/// NPC.ReceiveImpact (every hit: fists, weapons, bullets, cars, explosions) → NPCHealth.TakeDamage.
/// The impact is remembered while TakeDamage runs, so each hit knows where it landed and who dealt it.
/// </summary>
internal static class CombatHooks
{
    private static S1.NPCs.NPC? _impactNpc;
    private static S1.Combat.Impact? _impact;
    private static float _impactForce;

    public static event Action<NpcHit>? NpcHit;

    [HarmonyPatch]
    private static class ReceiveImpactPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.GetDeclaredMethods(typeof(S1.NPCs.NPC)).First(m => m.Name.StartsWith("RpcLogic___ReceiveImpact", StringComparison.Ordinal));

        private static void Prefix(S1.NPCs.NPC __instance, S1.Combat.Impact impact)
        {
            _impactNpc = __instance;
            _impact = impact;
            _impactForce = impact != null ? impact.ImpactForce : 0f;
        }

        private static void Postfix()
        {
            _impactNpc = null;
            _impact = null;
        }
    }

    [HarmonyPatch(typeof(S1.NPCs.NPCHealth), nameof(S1.NPCs.NPCHealth.TakeDamage))]
    private static class TakeDamagePatch
    {
        private static void Prefix(S1.NPCs.NPCHealth __instance, out float __state) => __state = __instance.Health;

        private static void Postfix(S1.NPCs.NPCHealth __instance, float damage, bool isLethal, float __state)
        {
            try
            {
                Raise(__instance, damage, isLethal, __state);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Damage event failed: {ex.Message}");
            }
        }
    }

    private static void Raise(S1.NPCs.NPCHealth health, float damage, bool lethal, float before)
    {
        if (damage <= 0f || NpcHit == null)
            return;
        var npc = _impactNpc != null && _impactNpc.Health == health
            ? _impactNpc
            : UnityQuery.GetComponentInParent<S1.NPCs.NPC>(health);
        if (npc == null)
            return;
        var impact = _impactNpc == npc ? _impact : null;
        var byLocal = false;
        var point = Vector3.zero;
        if (impact != null)
        {
            point = impact.HitPoint;
            var local = S1.PlayerScripts.Player.Local;
            byLocal = local != null && impact.IsPlayerImpact(out var player) && player == local;
        }
        NpcHit.Invoke(new NpcHit
        {
            Npc = npc,
            Damage = damage,
            HealthBefore = before,
            HealthAfter = health.Health,
            Lethal = lethal,
            ByLocalPlayer = byLocal,
            Point = point,
            Force = impact != null ? _impactForce : 0f,
        });
    }
}
