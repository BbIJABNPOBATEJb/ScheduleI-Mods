#if DEV
using System.Collections;
using System.Linq;
using DamageIndicator.Combat;
using S1Shared;
using S1Shared.Dev;
using UnityEngine;

namespace DamageIndicator.Dev;

/// <summary>Smoke scenarios for tools/run-smoke.ps1 -Mod DamageIndicator -Scenario &lt;name&gt;.</summary>
internal static class DamageSmoke
{
    private static int _hits;

    public static IEnumerator Run() => DevSmoke.RunScenario(name => name switch
    {
        "combat" => Combat(),
        "witness" => Witness(),
        _ => null,
    });

    /// <summary>
    /// Others' hits (no player behind them) only show when seen up close; your own always show.
    /// Characters out in the town are hit where they stand: one in plain view, one behind a wall, one far away.
    /// </summary>
    private static IEnumerator Witness()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return 3f;
        var camera = DamageIndicatorMod.MainCamera()!;
        var eye = camera.transform.position;
        var range = Config.OthersRange.Value;
        var people = UnityQuery.ToManaged(S1.NPCs.NPCManager.NPCRegistry)
            .Where(n => n != null && n.gameObject.activeInHierarchy && n.IsConscious && !n.isInBuilding && !n.IsInVehicle)
            .ToList();
        DevSmoke.Log($"{people.Count} people outdoors, others' hits range {range} m");

        // In plain view: brought in front of the camera.
        var seen = people.OrderBy(n => Vector3.Distance(n.transform.position, eye)).First();
        seen.Movement.Warp(eye + Flat(camera.transform.forward) * 5f);
        seen.Movement.Stop();
        yield return 1f;
        people.Remove(seen);
        yield return Expect(seen, camera, byPlayer: false, shown: true, "in plain view, 5 m");

        // Behind something solid, within range.
        var hidden = people.FirstOrDefault(n => Distance(n, camera) < range - 3f && !Sight.Witnessed(camera, Probe(n), range));
        if (hidden != null)
        {
            people.Remove(hidden);
            yield return Expect(hidden, camera, byPlayer: false, shown: false, $"behind a wall, {Distance(hidden, camera):0} m");
            yield return Expect(hidden, camera, byPlayer: true, shown: true, "behind a wall, your own hit");
        }
        else
        {
            DevSmoke.Log("no one behind a wall within range here: that case is not checked");
        }

        // Far away.
        var far = people.Where(n => Distance(n, camera) > range + 10f).OrderBy(n => Distance(n, camera)).FirstOrDefault();
        if (far != null)
        {
            yield return Expect(far, camera, byPlayer: false, shown: false, $"far away, {Distance(far, camera):0} m");
            yield return Expect(far, camera, byPlayer: true, shown: true, "far away, your own hit");
        }
        yield return DevSmoke.Screenshot("witness");
        DevSmoke.Finish(true, "witness done");
    }

    private static IEnumerator Expect(S1.NPCs.NPC npc, Camera camera, bool byPlayer, bool shown, string what)
    {
        var before = DamageIndicatorMod.Numbers.Spawned;
        Hit(npc, camera, 5f, 20f, S1.Combat.EImpactType.Punch, byPlayer);
        yield return 0.3f;
        var spawned = DamageIndicatorMod.Numbers.Spawned > before;
        DevSmoke.Log($"{(byPlayer ? "your" : "other's")} hit on {npc.FirstName} ({what}): number {(spawned ? "shown" : "not shown")}");
        DevSmoke.Check(spawned == shown, $"{what}: the number should {(shown ? "" : "not ")}be shown");
    }

    private static NpcHit Probe(S1.NPCs.NPC npc) => new() { Npc = npc, Point = npc.transform.position + Vector3.up * 1.2f };

    private static float Distance(S1.NPCs.NPC npc, Camera camera) => Vector3.Distance(npc.transform.position, camera.transform.position);

    private static IEnumerator Combat()
    {
        CombatHooks.NpcHit += _ => _hits++;
        yield return DevSmoke.LoadDisposableSave();
        yield return 2f;
        yield return DevSmoke.Screenshot("hud_plain");

        var player = S1.PlayerScripts.Player.Local;
        var camera = DamageIndicatorMod.MainCamera()!;
        var npc = UnityQuery.ToManaged(S1.NPCs.NPCManager.NPCRegistry)
            .Where(n => n != null && n.gameObject.activeInHierarchy && n.IsConscious && !n.isInBuilding)
            .OrderBy(n => Vector3.Distance(n.transform.position, player.transform.position))
            .First();
        DevSmoke.Log($"Target NPC: {npc.FirstName} at {Vector3.Distance(npc.transform.position, player.transform.position):0.0} m");

        // Bring the NPC in front of the camera.
        var front = camera.transform.position + Flat(camera.transform.forward) * 4f;
        npc.Movement.Warp(front);
        npc.Movement.Stop();
        yield return 1f;

        Hit(npc, camera, 18f, 60f, S1.Combat.EImpactType.Punch);
        yield return 0.35f;
        yield return DevSmoke.Screenshot("npc_hit");
        DevSmoke.Check(_hits == 1, $"expected 1 hit event, got {_hits}");

        Hit(npc, camera, 22f, 120f, S1.Combat.EImpactType.BluntMetal); // heavy flinch → stun bar
        yield return 0.3f;
        yield return DevSmoke.Screenshot("npc_stun");

        Hit(npc, camera, 100f, 60f, S1.Combat.EImpactType.Punch); // non-lethal final blow → KO
        yield return 0.5f;
        yield return DevSmoke.Screenshot("npc_ko");
        DevSmoke.Check(npc.Health.IsKnockedOut, "NPC was not knocked out");

        // Your health: damage, stun, heal.
        player.Health.TakeDamage(35f, true, true);
        yield return 0.4f;
        yield return DevSmoke.Screenshot("player_hit");
        player.Health.RecoverHealth(20f);
        yield return 0.4f;
        yield return DevSmoke.Screenshot("player_heal");
        DevSmoke.Log($"Player health {player.Health.CurrentHealth}");

        // Settings screen, every tab.
        yield return DevSmoke.WaitUntil(() => UnityQuery.FindInScenes<S1.UI.MainMenu.SettingsScreen>().Any(s => s.name == "DamageIndicatorModScreen"),
            20f, "settings screen");
        DamageIndicatorMod.Window.Toggle();
        yield return 2f;
        DevSmoke.Check(DamageIndicatorMod.Window.IsOpen, "settings did not open");
        var screen = UnityQuery.FindInScenes<S1.UI.MainMenu.SettingsScreen>().First(s => s.name == "DamageIndicatorModScreen");
        for (var i = 0; i < 4; i++)
        {
            screen.Categories[i].Toggle.isOn = true;
            yield return 0.8f;
            yield return DevSmoke.Screenshot("settings_tab" + i);
        }

        // Layout editor with the preview bar, then a programmatic move.
        DamageIndicatorMod.Editor.Begin();
        yield return 1f;
        yield return DevSmoke.Screenshot("editor");
        var element = DamageIndicatorMod.PlayerBar.Element;
        element.Position = new Vector2(0.2f, 0.3f);
        element.Scale = 1.4f;
        element.Apply();
        yield return 0.5f;
        yield return DevSmoke.Screenshot("editor_moved");
        DamageIndicatorMod.Editor.End();
        yield return 0.5f;
        DevSmoke.Check(DamageIndicatorMod.Window.IsOpen, "settings closed after editing");
        screen.Close();
        yield return 0.5f;
        S1.UI.PauseMenu.Instance.Resume();
        yield return 1f;
        DevSmoke.Finish(true, $"combat done, {_hits} hit events");
    }

    private static void Hit(S1.NPCs.NPC npc, Camera camera, float damage, float force, S1.Combat.EImpactType type, bool byPlayer = true)
    {
        var head = npc.Avatar.LookController != null ? npc.Avatar.LookController.HeadBone : null;
        var point = head != null ? head.position - Vector3.up * 0.25f : npc.transform.position + Vector3.up * 1.4f;
        var impact = new S1.Combat.Impact(point, Flat(camera.transform.forward), force, damage, type,
            byPlayer ? S1.PlayerScripts.Player.Local.NetworkObject : null, Random.Range(1, int.MaxValue));
        npc.ReceiveImpact(impact);
        DevSmoke.Log($"Hit {npc.FirstName}: {damage} ({type}, force {force}) -> health {npc.Health.Health}");
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
    }
}
#endif
