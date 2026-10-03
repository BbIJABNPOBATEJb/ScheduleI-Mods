#if DEV
using System;
using System.Collections;
using System.Linq;
using GuideArrows.Targets;
using S1Shared;
using S1Shared.Dev;
using UnityEngine;

namespace GuideArrows.Dev;

/// <summary>
/// Smoke scenarios for tools/run-smoke.ps1 -Mod GuideArrows -Scenario &lt;name&gt; [-Save &lt;save folder&gt;].
/// With -Save, a copy of a progressed save shows deals, stashes and several properties.
/// </summary>
internal static class ArrowSmoke
{
    public static IEnumerator Run() => DevSmoke.RunScenario(name => name switch
    {
        "arrows" => Arrows(),
        "outline" => Outline(),
        "media" => Media(),
        _ => null,
    });

    /// <summary>
    /// Screenshots for the mod's pages (-Save with customers, English UI): a street where several buyers
    /// and potential customers glow, people glowing through a building, and a glowing stash.
    /// </summary>
    private static IEnumerator Media()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return 3f;
        SetTime(DevSmoke.Arg.Length > 0 ? int.Parse(DevSmoke.Arg) : 1630);
        Config.ShowLabels.Value = true;
        Config.HideWithin.Value = 0;
        Config.Outline.Value = true;
        Config.OutlineAll.Value = true;
        yield return 2f;

        var player = S1.PlayerScripts.Player.Local;
        var movement = S1.PlayerScripts.PlayerMovement.Instance;
        TargetScanner.Tick(Time.unscaledTime, force: true);
        var people = TargetScanner.Targets.Where(t => (t.Kind == TargetKind.Buyer || t.Kind == TargetKind.Customer) && t.Highlight != null).ToList();
        DevSmoke.Log($"{people.Count} buyers and potential customers");
        DevSmoke.Check(people.Count >= 2, "not enough people to show");

        // Where buyers and potential customers are close together (both colors in one picture).
        int Near(Vector3 at, TargetKind kind) => people.Count(o => o.Kind == kind && Vector3.Distance(o.Position, at) < 50f);
        var best = people.OrderByDescending(p => Math.Min(Near(p.Position, TargetKind.Buyer), Near(p.Position, TargetKind.Customer)) * 10
            + Near(p.Position, TargetKind.Buyer) + Near(p.Position, TargetKind.Customer)).First();
        var group = people.Where(o => Vector3.Distance(o.Position, best.Position) < 50f).ToList();
        var center = group.Aggregate(Vector3.zero, (sum, o) => sum + o.Position) / group.Count;
        DevSmoke.Log($"Busiest spot {center}: {Near(best.Position, TargetKind.Buyer)} buyers, {Near(best.Position, TargetKind.Customer)} potential customers within 50 m");

        var shot = 0;
        foreach (var (distance, blocked) in new[] { (9f, true), (11f, true), (13f, true), (16f, true), (10f, false), (16f, false) })
        {
            var spot = OpenSpot(center, distance, blocked);
            if (!spot.HasValue)
                continue;
            movement.Teleport(spot.Value + Vector3.up * 0.1f);
            yield return 0.5f;
            Face(player, center, 0f);
            yield return 2.5f;
            yield return DevSmoke.Screenshot($"media_people_{shot++}_{(blocked ? "wall" : "open")}");
        }

        // A glowing stash with its arrow.
        var stash = TargetScanner.Targets.Where(t => t.Kind == TargetKind.Stash && t.Highlight != null)
            .OrderBy(t => Vector3.Distance(t.Position, center)).FirstOrDefault();
        if (stash != null)
        {
            foreach (var distance in new[] { 6f, 10f })
            {
                var spot = OpenSpot(stash.Position, distance, blocked: false);
                if (!spot.HasValue)
                    continue;
                movement.Teleport(spot.Value + Vector3.up * 0.1f);
                yield return 0.5f;
                Face(player, stash.Position, 0f);
                yield return 2.5f;
                yield return DevSmoke.Screenshot($"media_stash_{distance:0}");
            }
        }
        else
        {
            DevSmoke.Log("no stash with items in this save");
        }
        DevSmoke.Finish(true, "media done");
    }

    /// <summary>Sets the clock (SetTimeAndSync in game 0.4.6, SetTime_Server in 0.4.7).</summary>
    private static void SetTime(int time)
    {
        var manager = S1.GameTime.TimeManager.Instance;
        var method = typeof(S1.GameTime.TimeManager).GetMethod("SetTimeAndSync") ?? typeof(S1.GameTime.TimeManager).GetMethod("SetTime_Server");
        method?.Invoke(manager, new object[] { time });
        DevSmoke.Log($"Time set to {time}");
    }

    /// <summary>
    /// Buyers and potential customers (who is left out and why), then the outline on the nearest one:
    /// in plain view and from behind a building. Needs a save with customers (-Save).
    /// </summary>
    private static IEnumerator Outline()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return 3f;
        TargetScanner.Tick(Time.unscaledTime, force: true);
        foreach (var kind in TargetKinds.All)
        {
            var list = TargetScanner.Targets.Where(t => t.Kind == kind).ToList();
            DevSmoke.Log($"{kind}: {list.Count} targets, {list.Count(t => t.Highlight != null)} to outline"
                + (list.Count > 0 ? " e.g. " + string.Join(", ", list.Take(5).Select(t => $"'{t.Label}'")) : ""));
        }
        LogCustomers();

        // Someone who turned you down today is no potential customer until tomorrow.
        var potential = TargetScanner.Targets.FirstOrDefault(t => t.Kind == TargetKind.Customer);
        if (potential != null)
        {
            var customer = UiKit.Get<S1.Economy.Customer>(potential.Anchor!)!;
            UiKit.SetMember(customer, "sampleOfferedToday", true);
            TargetScanner.Tick(Time.unscaledTime, force: true);
            DevSmoke.Check(TargetScanner.Targets.All(t => t.Key != potential.Key), "a customer who refused today still has an arrow");
            UiKit.SetMember(customer, "sampleOfferedToday", false);
            TargetScanner.Tick(Time.unscaledTime, force: true);
            DevSmoke.Check(TargetScanner.Targets.Any(t => t.Key == potential.Key), "the customer did not come back");
            DevSmoke.Log($"'{potential.Label}' left out after refusing a sample, back the next day");
        }

        var player = S1.PlayerScripts.Player.Local;
        var target = TargetScanner.Targets.Where(t => (t.Kind == TargetKind.Buyer || t.Kind == TargetKind.Customer) && t.Highlight != null)
            .OrderBy(t => Vector3.Distance(t.Position, player.transform.position)).FirstOrDefault();
        DevSmoke.Check(target != null, "no buyer or potential customer to outline");
        DevSmoke.Log($"Outline test on {target!.Kind} '{target.Label}'");
        var npc = UiKit.Get<S1.NPCs.NPC>(target.Anchor!)!;
        var movement = S1.PlayerScripts.PlayerMovement.Instance;

        // In plain view, 9 m away.
        npc.Movement.Stop();
        var open = OpenSpot(npc.transform.position, 9f, blocked: false);
        DevSmoke.Check(open.HasValue, "no open spot near the target");
        movement.Teleport(open!.Value + Vector3.up * 0.1f);
        yield return 0.5f;
        Face(player, npc.transform.position, 0f);
        yield return 2f;
        LogOutlines();
        DevSmoke.Check(Render.TargetOutlines.Active.Any(a => a.Root == target.Highlight && a.Renderers > 0), "the target has no outline");
        yield return DevSmoke.Screenshot("outline_open");

        // Every buyer and potential customer around glows, not only the one with the arrow.
        var people = TargetScanner.Targets.Count(t => (t.Kind == TargetKind.Buyer || t.Kind == TargetKind.Customer) && t.Highlight != null
            && Vector3.Distance(t.Position, player.transform.position) <= Config.OutlineRange.Value);
        var glowing = Render.TargetOutlines.Active.Count();
        DevSmoke.Log($"Buyers and potential customers within {Config.OutlineRange.Value} m: {people}, outlines: {glowing}");
        DevSmoke.Check(people < 3 || glowing >= 3, "only the arrows' targets glow");

        // Right next to the nearest one: its arrow hides instead of pointing at the next one; it keeps glowing.
        Config.HideWithin.Value = 6;
        {
            var front = player.transform.position + player.transform.forward * 2.5f;
            npc.Movement.Warp(front);
            npc.Movement.Stop();
            yield return 0.5f;
            TargetScanner.Tick(Time.unscaledTime, force: true);
            yield return 0.3f;
            var nearest = TargetScanner.Targets.Where(t => t.Kind == target.Kind)
                .OrderBy(t => Vector3.Distance(t.Position, player.transform.position)).First();
            var arrow = GuideArrowsMod.Strip.ArrowTargets.FirstOrDefault(t => t.Kind == target.Kind);
            DevSmoke.Log($"Next to '{nearest.Label}' ({Vector3.Distance(nearest.Position, player.transform.position):0.0} m): "
                + (arrow != null ? $"arrow to '{arrow.Label}'" : "no arrow"));
            var reached = Vector3.Distance(nearest.Position, player.transform.position) < Config.HideWithin.Value;
            DevSmoke.Check(reached, "could not bring the target close");
            DevSmoke.Check(arrow == null, "the arrow swung round to another target instead of hiding");
            DevSmoke.Check(Render.TargetOutlines.Active.Any(a => a.Root == nearest.Highlight), "the reached target stopped glowing");
            yield return DevSmoke.Screenshot("outline_reached");
        }

        // Behind a building: the outline shows through it.
        npc.Movement.Stop();
        var hidden = OpenSpot(npc.transform.position, 14f, blocked: true);
        if (hidden.HasValue)
        {
            movement.Teleport(hidden.Value + Vector3.up * 0.1f);
            yield return 0.5f;
            Face(player, npc.transform.position, 0f);
            yield return 2f;
            LogOutlines();
            yield return DevSmoke.Screenshot("outline_wall");
        }
        else
        {
            DevSmoke.Log("no spot with a wall in between found here");
        }

        // Off: outlines fade away.
        Config.Outline.Value = false;
        yield return 1f;
        DevSmoke.Check(!Render.TargetOutlines.Active.Any(), "outlines stayed after turning them off");
        yield return DevSmoke.Screenshot("outline_off");
        Config.Outline.Value = true;
        DevSmoke.Finish(true, "outline done");
    }

    private static void LogCustomers()
    {
        var locked = UnityQuery.ToManaged(S1.Economy.Customer.LockedCustomers).Where(c => c != null).ToList();
        var marked = locked.Where(c => c.potentialCustomerPoI != null && c.potentialCustomerPoI.enabled).ToList();
        DevSmoke.Log($"Locked customers: {locked.Count}, marked as potential: {marked.Count}");
        foreach (var c in marked.Take(12))
        {
            var npc = c.NPC;
            DevSmoke.Log($"    {npc.FirstName}: conscious={npc.IsConscious} inBuilding={npc.isInBuilding} inVehicle={npc.IsInVehicle} "
                + $"approachable={c.CustomerData.CanBeDirectlyApproached} awaitingDelivery={c.IsAwaitingDelivery}");
        }
        var unlocked = UnityQuery.ToManaged(S1.Economy.Customer.UnlockedCustomers).Where(c => c != null).ToList();
        DevSmoke.Log($"Unlocked customers: {unlocked.Count}");
        foreach (var c in unlocked.Take(12))
        {
            var npc = c.NPC;
            DevSmoke.Log($"    {npc.FirstName}: dealer={(c.AssignedDealer != null ? c.AssignedDealer.FirstName : "-")} contract={c.CurrentContract != null} "
                + $"offered={c.OfferedContractInfo != null} sinceDeal={c.TimeSinceLastDealCompleted} sinceOffer={c.TimeSinceInstantDealOffered} "
                + $"inBuilding={npc.isInBuilding} inVehicle={npc.IsInVehicle}");
        }
    }

    private static void LogOutlines()
    {
        foreach (var (root, renderers, alpha) in Render.TargetOutlines.Active)
            DevSmoke.Log($"    outline on '{root.name}': {renderers} meshes, alpha {alpha:0.00}");
    }

    /// <summary>A walkable spot at about this distance from the target, with or without something solid in between.</summary>
    private static Vector3? OpenSpot(Vector3 target, float distance, bool blocked)
    {
        var head = target + Vector3.up * 1.6f;
        for (var i = 0; i < 36; i++)
        {
            var dir = Quaternion.Euler(0f, i * 10f, 0f) * Vector3.forward;
            var above = target + dir * distance + Vector3.up * 30f;
            if (!Physics.Raycast(above, Vector3.down, out var ground, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                continue;
            if (Mathf.Abs(ground.point.y - target.y) > 1.5f)
                continue; // a roof or a ditch
            var eye = ground.point + Vector3.up * 1.6f;
            var wall = Physics.Linecast(eye, head, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && UnityQuery.GetComponentInParent<S1.NPCs.NPC>(hit.collider) == null
                && UnityQuery.GetComponentInParent<S1.PlayerScripts.Player>(hit.collider) == null;
            if (wall == blocked)
                return ground.point;
        }
        return null;
    }

    private static void Face(S1.PlayerScripts.Player player, Vector3 point, float extraYaw)
    {
        var to = point - player.transform.position;
        to.y = 0f;
        player.transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up) * Quaternion.Euler(0f, extraYaw, 0f);
    }

    private static void LogAngles(string when)
    {
        var cam = S1.PlayerScripts.PlayerCamera.Instance;
        var compass = S1.UI.Compass.CompassManager.Instance;
        foreach (var t in TargetScanner.Targets.Take(4))
        {
            var f = cam.transform.forward; f.y = 0f;
            var to = t.Position - cam.transform.position; to.y = 0f;
            var mine = Vector3.SignedAngle(f, to, Vector3.up);
            var cf = cam.Camera.transform.forward; cf.y = 0f;
            var viaCamera = Vector3.SignedAngle(cf, to, Vector3.up);
            var compassX = 0f;
            if (compass != null)
                compass.GetCompassData(t.Position, out compassX, out _);
            DevSmoke.Log($"[{when}] {t.Kind} '{t.Label}': yaw(PlayerCamera)={mine:0} yaw(Camera)={viaCamera:0} compassX={compassX:0}");
        }
    }

    private static IEnumerator Arrows()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return 3f;
        TargetScanner.Tick(Time.unscaledTime, force: true);
        foreach (var kind in TargetKinds.All)
        {
            var list = TargetScanner.Targets.Where(t => t.Kind == kind).ToList();
            DevSmoke.Log($"{kind}: {list.Count} targets" + (list.Count > 0 ? " e.g. " + string.Join(", ", list.Take(4).Select(t => $"'{t.Label}'")) : ""));
        }
        DevSmoke.Log("Home: " + HomeTracker.Describe());
        DevSmoke.Log($"3D arrows: {Render.ArrowRenderer.Supported}");
        DevSmoke.Check(TargetScanner.Targets.Count > 0, "no targets found");

        yield return 1.5f;
        DevSmoke.Log($"Visible arrows: {GuideArrowsMod.Strip.VisibleCount}");
        yield return DevSmoke.Screenshot("arrows");

        // Orientation check: 0 = away (up on screen), 90 = right, pitch 30 = nose up.
        foreach (var (yaw, pitch) in new[] { (0f, 0f), (90f, 0f), (0f, 30f) })
        {
            UI.ArrowStrip.DebugYaw = yaw;
            UI.ArrowStrip.DebugPitch = pitch;
            yield return 0.5f;
            yield return DevSmoke.Screenshot($"orient_yaw{yaw}_pitch{pitch}");
        }
        UI.ArrowStrip.DebugYaw = null;

        // Turn towards the nearest target: its arrow should point forward (compare with the compass).
        var player = S1.PlayerScripts.Player.Local;
        var nearest = TargetScanner.Targets.OrderBy(t => Vector3.Distance(t.Position, player.transform.position)).First();
        LogAngles("before turning");
        DevSmoke.Log($"Turning to {nearest.Kind} '{nearest.Label}'");
        Face(player, nearest.Position, 0f);
        yield return 2f;
        LogAngles("facing nearest");
        yield return DevSmoke.Screenshot("look_at_nearest");
        Face(player, nearest.Position, 90f);
        yield return 2f;
        LogAngles("nearest on the left");
        yield return DevSmoke.Screenshot("look_right_of_nearest");

        // Modes.
        Config.Mode.Value = (int)ArrowMode.NearestFew;
        yield return 1.5f;
        yield return DevSmoke.Screenshot("mode_nearest_few");
        Config.Mode.Value = (int)ArrowMode.Nearest;
        Config.ShowLabels.Value = true;
        yield return 1.5f;
        yield return DevSmoke.Screenshot("mode_nearest_labels");
        Config.Mode.Value = (int)ArrowMode.PerKind;
        Config.ShowLabels.Value = false;

        // A stash glows (stashes are part of the map's merged meshes: only their own parts are outlined).
        var stash = TargetScanner.Targets.Where(t => t.Kind == TargetKind.Stash && t.Highlight != null)
            .OrderBy(t => Vector3.Distance(t.Position, player.transform.position)).FirstOrDefault();
        if (stash != null)
        {
            var spot = OpenSpot(stash.Position, 7f, blocked: false);
            if (spot.HasValue)
            {
                S1.PlayerScripts.PlayerMovement.Instance.Teleport(spot.Value + Vector3.up * 0.1f);
                yield return 0.5f;
                Face(player, stash.Position, 0f);
                yield return 2.5f;
                LogOutlines();
                DevSmoke.Check(Render.TargetOutlines.Active.All(a => a.Renderers < 64), "an outline covers far too many meshes");
                yield return DevSmoke.Screenshot("stash_outline");
            }
        }

        // Sleeping remembers the home base.
        HomeTracker.OnSleepStart();
        DevSmoke.Log("Home after sleep: " + HomeTracker.Describe());

        // Settings screen and the layout editor preview.
        yield return DevSmoke.WaitUntil(() => UnityQuery.FindInScenes<S1.UI.MainMenu.SettingsScreen>().Any(s => s.name == "GuideArrowsModScreen"),
            20f, "settings screen");
        GuideArrowsMod.Window.Toggle();
        yield return 2f;
        DevSmoke.Check(GuideArrowsMod.Window.IsOpen, "settings did not open");
        var screen = UnityQuery.FindInScenes<S1.UI.MainMenu.SettingsScreen>().First(s => s.name == "GuideArrowsModScreen");
        for (var i = 0; i < 3; i++)
        {
            screen.Categories[i].Toggle.isOn = true;
            yield return 0.8f;
            yield return DevSmoke.Screenshot("settings_tab" + i);
        }
        GuideArrowsMod.Editor.Begin();
        yield return 1.5f;
        yield return DevSmoke.Screenshot("editor");
        GuideArrowsMod.Editor.End();
        screen.Close();
        yield return 0.5f;
        S1.UI.PauseMenu.Instance.Resume();
        yield return 1f;
        DevSmoke.Finish(true, "arrows done");
    }
}
#endif
