#if DEV
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using HarmonyLib;
using S1Shared;
using S1Shared.Dev;
using UnityEngine;

namespace ElectricScooter.Dev;

/// <summary>Smoke scenarios for tools/run-smoke.ps1 -Mod ElectricScooter -Scenario &lt;name&gt;.</summary>
internal static class ScooterSmoke
{
    public static IEnumerator Run() => DevSmoke.RunScenario(name => name switch
    {
        "inspect" => Inspect(),
        "ride" => Ride(),
        "offroad" => OffRoad(),
        "shop" => Shop(),
        _ => null,
    });

    private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    private static string V(Vector3 v) => $"({F(v.x)}, {F(v.y)}, {F(v.z)})";

    private static string Curve(AnimationCurve? curve) => curve == null
        ? "null"
        : string.Join(" ", curve.keys.Select(k => $"{F(k.time)}:{F(k.value)}"));

    private static object? Member(object target, string name)
    {
        var type = target.GetType();
        var property = AccessTools.Property(type, name);
        if (property != null)
            return property.GetValue(target);
        return AccessTools.Field(type, name)?.GetValue(target);
    }

    private static void Settings(StringBuilder sb, S1.Experimental.SkateboardSettings s)
    {
        sb.AppendLine($"    top speed {F(s.TopSpeed_Kmh)} km/h, reverse {F(s.ReverseTopSpeed_Kmh)}, push x{F(s.PushForceMultiplier)} for {F(s.PushForceDuration)} s after {F(s.PushDelay)} s, brake {F(s.BrakeForce)}");
        sb.AppendLine($"    push curve {Curve(s.PushForceCurve)} | by speed {Curve(s.PushForceMultiplierMap)}");
        sb.AppendLine($"    hover: height {F(s.HoverHeight)}, ray {F(s.HoverRayLength)}, force {F(s.HoverForce)}, PID {F(s.Hover_P)}/{F(s.Hover_I)}/{F(s.Hover_D)}; gravity {F(s.Gravity)}");
        sb.AppendLine($"    turn: force {F(s.TurnForce)}, change {F(s.TurnChangeRate)}, rest {F(s.TurnReturnToRestRate)}, boost {F(s.TurnSpeedBoost)}, map {Curve(s.TurnForceMap)}, clamp {F(s.RotationClampForce)}");
        sb.AppendLine($"    friction: {s.FrictionEnabled}, long x{F(s.LongitudinalFrictionMultiplier)} curve {Curve(s.LongitudinalFrictionCurve)}, lateral {F(s.LateralFrictionForceMultiplier)}");
        sb.AppendLine($"    jump: force {F(s.JumpForce)}, {F(s.JumpDuration_Min)}-{F(s.JumpDuration_Max)} s, forward {F(s.JumpForwardBoost)}; air {s.AirMovementEnabled} {F(s.AirMovementForce)}");
    }

    private static void Colliders(StringBuilder sb, Transform root)
    {
        foreach (var c in UnityQuery.GetComponentsInChildren<Collider>(root, true))
        {
            var shape = "";
            if (UnityQuery.TryCastTo<BoxCollider>(c, out var box))
                shape = $"box center {V(box.center)} size {V(box.size)}";
            else if (UnityQuery.TryCastTo<CapsuleCollider>(c, out var capsule))
                shape = $"capsule center {V(capsule.center)} r {F(capsule.radius)} h {F(capsule.height)} dir {capsule.direction}";
            else if (UnityQuery.TryCastTo<SphereCollider>(c, out var sphere))
                shape = $"sphere center {V(sphere.center)} r {F(sphere.radius)}";
            else if (UnityQuery.TryCastTo<MeshCollider>(c, out var mesh))
                shape = $"mesh {(mesh.sharedMesh != null ? mesh.sharedMesh.name + " " + V(mesh.sharedMesh.bounds.size) : "none")} convex {mesh.convex}";
            var material = c.sharedMaterial != null ? $" material {c.sharedMaterial.name} friction {F(c.sharedMaterial.dynamicFriction)}/{F(c.sharedMaterial.staticFriction)}" : "";
            sb.AppendLine($"    collider {UnityQuery.PathOf(c.transform)} layer {c.gameObject.layer} trigger {c.isTrigger} enabled {c.enabled} scale {V(c.transform.lossyScale)}: {shape}{material}");
        }
    }

    private static void Renderers(StringBuilder sb, Transform root)
    {
        foreach (var r in UnityQuery.GetComponentsInChildren<Renderer>(root, true))
        {
            var mesh = "";
            var filter = UiKit.Get<MeshFilter>(r);
            if (filter != null && filter.sharedMesh != null)
                mesh = $" mesh {filter.sharedMesh.name} {V(filter.sharedMesh.bounds.size)} readable {filter.sharedMesh.isReadable}";
            var materials = string.Join(", ", r.sharedMaterials.Where(m => m != null).Select(m => $"{m.name} [{m.shader.name}]"));
            sb.AppendLine($"    renderer {UnityQuery.PathOf(r.transform)} ({UnityQuery.TypeName(r)}) layer {r.gameObject.layer} pos {V(r.transform.localPosition)} scale {V(r.transform.lossyScale)}{mesh}: {materials}");
        }
    }

    // --- shop and saves ------------------------------------------------------------------------------

    /// <summary>
    /// Opens the skate shop's dialogue. Done through reflection because the dialogue asset's type was
    /// renamed between game versions (DialogueContainer in 0.4.6, Conversation in 0.4.7): 0.4.7 keeps it
    /// in a field of the seller, 0.4.6 knows it by the asset's name.
    /// </summary>
    private static bool StartShopDialogue(S1.Dialogue.DialogueHandler handler, S1.Dialogue.DialogueController_SkateboardSeller seller)
    {
        var start = typeof(S1.Dialogue.DialogueHandler).GetMethods().FirstOrDefault(m =>
            m.Name == "StartDialogue" && m.GetParameters().Length == 3 && m.GetParameters()[0].ParameterType != typeof(string));
        if (start == null)
            return false;
        var type = start.GetParameters()[0].ParameterType;
        var conversation = Member(seller, "_sellConversation");
#if IL2CPP
        if (conversation == null)
        {
            var cast = typeof(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase).GetMethod("Cast")!.MakeGenericMethod(type);
            foreach (var asset in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.From(type)))
                if (asset != null && asset.name == "Skateboard_Sell")
                    conversation = cast.Invoke(asset, null);
        }
#else
        if (conversation == null)
            conversation = Resources.FindObjectsOfTypeAll(type).FirstOrDefault(asset => asset != null && asset.name == "Skateboard_Sell");
#endif
        if (conversation == null)
            return false;
        start.Invoke(handler, new[] { conversation, true, "ENTRY" });
        return true;
    }

    /// <summary>Buys the scooter through the seller's own dialogue, then saves, reloads and rides it.</summary>
    private static IEnumerator Shop()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return 4f;
        var seller = UnityQuery.FindInScenes<S1.Dialogue.DialogueController_SkateboardSeller>().First();
        var handler = UiKit.Get<S1.Dialogue.DialogueHandler>(seller);
        DevSmoke.Check(handler != null, "the seller has no dialogue handler");
        var money = S1.Money.MoneyManager.Instance;
        var movement = S1.PlayerScripts.PlayerMovement.Instance;
        var player = S1.PlayerScripts.Player.Local;
        var npc = handler!.NPC;
        DevSmoke.Log($"Seller {npc.FirstName} at {V(npc.transform.position)}");
        movement.Teleport(npc.transform.position + npc.transform.forward * 1.5f + Vector3.up * 0.1f);
        yield return 0.5f;
        player.transform.rotation = Quaternion.LookRotation(-npc.transform.forward, Vector3.up);
        yield return 1f;

        // Not enough cash: the option is there but cannot be picked.
        DevSmoke.Check(!seller.CheckChoice(ScooterItem.DisplayName, out var reason), "the scooter can be bought without money");
        DevSmoke.Log($"Without cash: '{reason}'");
        money.ChangeCashBalance(6000f, false, false);
        yield return 0.5f;
        DevSmoke.Check(seller.CheckChoice(ScooterItem.DisplayName, out reason), $"the scooter cannot be bought with $6000: {reason}");

        // The hotbar would show through the long list of boards in the picture.
        var inventory = S1.PlayerScripts.PlayerInventory.Instance;
        inventory.SetInventoryEnabled(false);
        DevSmoke.Check(StartShopDialogue(handler, seller), "the shop's dialogue was not found");
        yield return 2.5f;
        DevSmoke.Check(typeof(S1.Dialogue.DialogueHandler).GetProperty("ActiveDialogue")!.GetValue(null) != null, "the shop's dialogue did not open");
        yield return DevSmoke.Screenshot("shop_choices");
        var cash = money.cashBalance;
        seller.ChoiceCallback(ScooterItem.DisplayName);
        yield return 2f;
        yield return DevSmoke.Screenshot("shop_confirm");
        DevSmoke.Check(seller.CheckChoice("CONFIRM", out reason), $"the purchase cannot be confirmed: {reason}");
        seller.ChoiceCallback("CONFIRM");
        yield return 1f;
        handler.EndDialogue();
        inventory.SetInventoryEnabled(true);
        inventory.SetEquippingEnabled(true);
        yield return 1f;
        DevSmoke.Log($"Cash ${F(cash)} -> ${F(money.cashBalance)}");
        DevSmoke.Check(Mathf.Approximately(cash - money.cashBalance, 5000f), "the scooter did not cost $5000");
        var slot = SlotWith(ScooterItem.Id);
        DevSmoke.Check(slot != null, "the scooter is not in the inventory after buying it");
        DevSmoke.Log($"Bought: '{slot!.ItemInstance.Name}' x{slot.ItemInstance.Quantity}");

        // The item survives a save and a reload (the registry forgets runtime items on every scene change).
        yield return DevSmoke.SaveAndReload();
        slot = SlotWith(ScooterItem.Id);
        DevSmoke.Check(slot != null, "the scooter is gone after reloading the save");
        inventory = S1.PlayerScripts.PlayerInventory.Instance;
        inventory.Equip(slot!);
        yield return 0.8f;
        DevSmoke.Check(UnityQuery.TryCastTo<S1.Skating.Skateboard_Equippable>(inventory.Equippable, out var equippable), "the reloaded scooter is not held as a board");
        yield return DevSmoke.Screenshot("held_after_reload");
        equippable.Mount();
        yield return DevSmoke.WaitUntil(() => S1.PlayerScripts.Player.Local.ActiveSkateboard != null, 5f, "mounting after reload");
        yield return 1f;
        var board = S1.PlayerScripts.Player.Local.ActiveSkateboard;
        DevSmoke.Log($"After reload: riding {ScooterRide.Riding}, top speed {F(board.CurentSettings.TopSpeed_Kmh)} km/h, hover {F(board.CurentSettings.HoverHeight)}");
        DevSmoke.Check(ScooterRide.Riding && Mathf.Abs(board.CurentSettings.TopSpeed_Kmh - 41.6f) < 0.2f, "the reloaded scooter is not tuned");

        // A weather change makes the game rebuild the board's settings; the scooter must take them back.
        board.OnWeatherChange(new S1.Core.Weather.WeatherConditions());
        DevSmoke.Check(Mathf.Abs(board.CurentSettings.TopSpeed_Kmh - 32f) < 0.2f, "weather change did not reset the settings (test is broken)");
        yield return 0.3f;
        DevSmoke.Check(Mathf.Abs(board.CurentSettings.TopSpeed_Kmh - 41.6f) < 0.2f, "the scooter lost its tuning after a weather change");
        equippable.Dismount();
        yield return 1f;
        DevSmoke.Check(!ScooterRide.Riding, "still riding after dismount");
        DevSmoke.Finish(true, "shop done");
    }

    // --- riding --------------------------------------------------------------------------------------

    private sealed class Lane
    {
        public Vector3 Start;
        public Vector3 Direction;
        public float Length;
        /// <summary>Distance from the start to the curb and its height (0 when the lane is flat).</summary>
        public float CurbAt, CurbHeight;
    }

    private sealed class RideResult
    {
        public float TopSpeed, Distance, MinStamina, Seconds, StuckSeconds;
        public bool Finished;
        /// <summary>Speed (km/h) after each full second of the ride.</summary>
        public readonly System.Collections.Generic.List<float> SpeedBySecond = new();
    }

    private static bool Ground(Vector3 point, float fromY, int mask, out RaycastHit hit) =>
        Physics.Raycast(new Vector3(point.x, fromY, point.z), Vector3.down, out hit, 5f, mask, QueryTriggerInteraction.Ignore);

    /// <summary>
    /// Looks around the player for a straight stretch of paved ground: the longest flat one, or
    /// (withCurb) one with one step up — the highest found, 8 to 24 cm — in the middle and flat ground around it.
    /// </summary>
    private static Lane? FindLane(Vector3 around, int mask, float maxLength, bool withCurb, bool terrain = false)
    {
        const float step = 0.5f;
        Lane? best = null;
        int origins = 0, paved = 0, blocked = 0;
        var tags = new System.Collections.Generic.Dictionary<string, int>();
        for (var ix = -16; ix <= 16; ix++)
        {
            for (var iz = -16; iz <= 16; iz++)
            {
                var origin = around + new Vector3(ix * 5f, 0f, iz * 5f);
                if (!Ground(origin, around.y + 4f, mask, out var first))
                    continue;
                origins++;
                var tag = first.collider.gameObject.tag;
                tags[tag] = tags.TryGetValue(tag, out var n) ? n + 1 : 1;
                if ((tag == "Terrain") != terrain)
                    continue;
                paved++;
                for (var dir = 0; dir < 8; dir++)
                {
                    var direction = Quaternion.Euler(0f, dir * 45f, 0f) * Vector3.forward;
                    var y = first.point.y;
                    var curbAt = 0f;
                    var curbHeight = 0f;
                    var length = 0f;
                    for (var t = step; t <= maxLength; t += step)
                    {
                        if (!Ground(first.point + direction * t, y + 1.2f, mask, out var hit) || (hit.collider.gameObject.tag == "Terrain") != terrain)
                            break;
                        var rise = hit.point.y - y;
                        if (withCurb && curbAt == 0f && rise >= 0.08f && rise <= 0.24f && t >= 9f)
                        {
                            curbAt = t;
                            curbHeight = rise;
                        }
                        else if (Mathf.Abs(rise) > 0.045f)
                        {
                            break;
                        }
                        y = hit.point.y;
                        length = t;
                    }
                    if (withCurb ? (curbAt == 0f || length < curbAt + 8f) : length < (terrain ? 25f : 30f))
                        continue;
                    if (best != null && (withCurb ? best.CurbHeight >= curbHeight : best.Length >= length))
                        continue;
                    // Nothing in the way at body height.
                    if (Physics.SphereCast(first.point + Vector3.up * 1.0f, 0.4f, direction, out _, length, mask, QueryTriggerInteraction.Ignore))
                    {
                        blocked++;
                        continue;
                    }
                    best = new Lane { Start = first.point, Direction = direction, Length = withCurb ? curbAt + 8f : length, CurbAt = curbAt, CurbHeight = curbHeight };
                }
            }
        }
        DevSmoke.Log($"Lane search (curb {withCurb}, terrain {terrain}): {origins} ground points, {paved} matching, {blocked} lanes blocked; ground tags: " +
            string.Join(", ", tags.Select(kv => kv.Key + " x" + kv.Value)) +
            (best != null ? $"; best {F(best.Length)} m" : "; nothing found"));
        return best;
    }

    /// <summary>A picture of the board from a camera placed beside it (offset in the board's own space).</summary>
    private static void SideShot(Transform target, Vector3 localOffset, string name)
    {
        var main = Camera.main;
        if (main == null)
            return;
        var go = new GameObject("Smoke Camera");
        var camera = go.AddComponent<Camera>();
        camera.CopyFrom(main);
        camera.fieldOfView = 50f;
        go.transform.position = target.TransformPoint(localOffset);
        go.transform.LookAt(target.position + target.up * 0.75f);
        var rt = new RenderTexture(1600, 900, 24);
        camera.targetTexture = rt;
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var picture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        picture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        picture.Apply();
        RenderTexture.active = previous;
        camera.targetTexture = null;
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(DevSmoke.OutDir, name + ".png"), ImageConversion.EncodeToPNG(picture));
        Object.Destroy(picture);
        Object.Destroy(go);
        rt.Release();
        Object.Destroy(rt);
        DevSmoke.Log($"Side shot {name}.png");
    }

    /// <summary>Skateboard wheel meshes still drawn under the root (the scooter's own parts are not counted).</summary>
    private static int VisibleWheels(Transform root)
    {
        var count = 0;
        foreach (var renderer in UnityQuery.GetComponentsInChildren<MeshRenderer>(root, true))
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;
            var wheel = false;
            for (var t = renderer.transform; t != null && t != root; t = t.parent)
            {
                if (t.name == "ElectricScooter Parts")
                {
                    wheel = false;
                    break;
                }
                if (t.name.StartsWith("Wheel", System.StringComparison.OrdinalIgnoreCase))
                    wheel = true;
            }
            if (wheel)
                count++;
        }
        return count;
    }

    /// <summary>The rider stands in the scooter pose: palms on the grips, feet on the deck, legs (almost) straight.</summary>
    private static void CheckPose(S1.Skating.Skateboard board, string when)
    {
        var pose = RiderPose.Describe(board, out var hand, out var straightest, out var bent, out var feet);
        DevSmoke.Log($"Scooter rider {when}: {pose ?? "not held in the scooter pose"}");
        DevSmoke.Check(pose != null, $"the rider is not held in the scooter pose {when}");
        DevSmoke.Check(hand < 0.06f, $"a palm is {F(hand)} m off its grip {when}");
        DevSmoke.Check(feet, $"the rider's feet are off the deck {when}");
        DevSmoke.Check(bent > 150f, $"the rider's knees are bent to {F(bent)}° {when}");
    }

    /// <summary>Where the deck, the trucks and their parents sit and point, relative to the root.</summary>
    private static void Frames(StringBuilder sb, Transform root)
    {
        sb.AppendLine($"    {root.name}:");
        foreach (var t in UnityQuery.GetComponentsInChildren<Transform>(root, true))
        {
            if (!(t.name is "GoldSkateboardVisual" or "GoldSkateboard" or "Model" or "Skateboard" or "BoardContainer" or "Board" || t.name.StartsWith("Truck") || t.name.StartsWith("Wheel")))
                continue;
            var mesh = UiKit.Get<MeshFilter>(t);
            var bounds = mesh != null && mesh.sharedMesh != null ? $", mesh bounds {mesh.sharedMesh.bounds.center.ToString("F3")} size {mesh.sharedMesh.bounds.size.ToString("F3")}" : "";
            sb.AppendLine($"      {UnityQuery.PathOf(t)}: pos {root.InverseTransformPoint(t.position).ToString("F3")} right {root.InverseTransformDirection(t.right).ToString("F2")} up {root.InverseTransformDirection(t.up).ToString("F2")} forward {root.InverseTransformDirection(t.forward).ToString("F2")} scale {t.lossyScale.ToString("F3")}{bounds}");
        }
    }

    private static S1.PlayerScripts.HotbarSlot? SlotWith(string id)
    {
        foreach (var slot in UnityQuery.ToManaged(S1.PlayerScripts.PlayerInventory.Instance.hotbarSlots))
            if (slot.ItemInstance != null && slot.ItemInstance.ID == id)
                return slot;
        return null;
    }

    /// <summary>Rides the given board along the lane, pushing whenever the game would allow it.</summary>
    private static IEnumerator RideLane(string itemId, Lane lane, RideResult result, string? screenshot)
    {
        var player = S1.PlayerScripts.Player.Local;
        var movement = S1.PlayerScripts.PlayerMovement.Instance;
        var inventory = S1.PlayerScripts.PlayerInventory.Instance;
        movement.Teleport(lane.Start + Vector3.up * 0.15f);
        yield return 0.3f;
        player.transform.rotation = Quaternion.LookRotation(lane.Direction, Vector3.up);
        movement.SetStamina(S1.PlayerScripts.PlayerMovement.StaminaReserveMax);
        yield return 0.7f;

        if (SlotWith(itemId) == null)
            inventory.AddItemToInventory(S1.Registry.GetItem(itemId).GetDefaultInstance(1));
        yield return 0.3f;
        var slot = SlotWith(itemId);
        DevSmoke.Check(slot != null, $"{itemId} did not get into the hotbar");
        // Put away whatever is in the hands, the way the hotbar keys do.
        foreach (var other in UnityQuery.ToManaged(inventory.hotbarSlots))
            if (other.IsSelected)
                other.Deselect();
        yield return 0.2f;
        inventory.Equip(slot!);
        yield return 0.6f;
        DevSmoke.Check(UnityQuery.TryCastTo<S1.Skating.Skateboard_Equippable>(inventory.Equippable, out var equippable), $"{itemId} is not held as a skateboard");
        // Now and then the player drops through the ground after the teleport, already before the scooter
        // is taken in hand: put him back rather than test the ride in free fall.
        for (var tries = 0; tries < 3 && player.transform.position.y < lane.Start.y - 1f; tries++)
        {
            DevSmoke.Log($"The player fell through the ground (y {F(player.transform.position.y)}) before getting on {itemId}: teleporting again");
            movement.Teleport(lane.Start + Vector3.up * 0.15f);
            yield return 1f;
            player.transform.rotation = Quaternion.LookRotation(lane.Direction, Vector3.up);
        }
        if (screenshot != null)
            yield return DevSmoke.Screenshot(screenshot + "_held");
        if (itemId == ScooterItem.Id)
        {
            // The board in the avatar's hands is what the other players see. Dress it the way their
            // games do when the scooter is announced.
            var prop = player.Avatar.CurrentEquippable;
            DevSmoke.Check(prop != null, "the avatar holds nothing: other players would not see the scooter in the hands");
            if (screenshot != null)
                DevSmoke.Log("Board in the avatar's hands (what other players see): " + UnityQuery.DumpHierarchy(prop!.transform, 6));
            ScooterModel.DressHeld(prop!.transform, ScooterRide.HoverHeight);
            DevSmoke.Check(ScooterModel.IsDressed(prop.transform), "the board in the avatar's hands could not be dressed as a scooter");
            DevSmoke.Check(VisibleWheels(prop.transform) == 0, $"{VisibleWheels(prop.transform)} skateboard wheels show on the scooter in the avatar's hands");
            if (screenshot != null)
            {
                player.SetThirdPersonMeshesVisibility(true);
                SideShot(player.transform, new Vector3(-0.9f, 1.4f, 1.5f), screenshot + "_others_held");
                player.SetThirdPersonMeshesVisibility(false);
            }
        }
        equippable.Mount();
        yield return DevSmoke.WaitUntil(() => player.ActiveSkateboard != null, 5f, "mounting " + itemId);
        var board = player.ActiveSkateboard;
        var settle = new StringBuilder();
        var mounted = Time.time;
        // The scooter is put on its wheels at once: never higher than that, no drop from above.
        var highest = board != null ? board.transform.position.y + (itemId == ScooterItem.Id ? ScooterRide.HoverHeight - 0.096f : 0f) + 0.08f : 0f;
        var top = 0f;
        while (board != null && Time.time - mounted < 0.8f)
        {
            top = Mathf.Max(top, board.transform.position.y);
            settle.Append($" {F(Time.time - mounted)}s:{F(board.transform.position.y)}m/v{F(board.Rb.velocity.y)}/h{F(board.CurentSettings.HoverHeight)}/{F(Vector3.Angle(board.transform.up, Vector3.up))}°");
            yield return null;
        }
        DevSmoke.Log($"After mounting {itemId} (time: height / vertical speed / hover setting / tilt):{settle}; still riding: {board != null}");
        DevSmoke.Check(board != null, $"thrown off {itemId} right after mounting");
        if (board == null)
            yield break;
        DevSmoke.Check(top <= highest, $"{itemId} jumped up to {F(top)} m on mounting (at most {F(highest)} expected)");
        var slows = itemId != ScooterItem.Id || !Config.OffRoad;
        DevSmoke.Check(board.SlowOnTerrain == slows, $"{itemId} {(slows ? "does not slow down" : "still slows down")} on grass (off-road {Config.OffRoad})");
        if (itemId == ScooterItem.Id)
        {
            CheckPose(board, "after mounting");
            DevSmoke.Check(VisibleWheels(board.transform) == 0, $"{VisibleWheels(board.transform)} skateboard wheels show under the ridden scooter");
        }
        else
        {
            DevSmoke.Check(RiderPose.Describe(board, out _, out _, out _, out _) == null, $"the {itemId} rider got the scooter pose");
        }
        if (screenshot != null)
        {
            DevSmoke.Log("Riding model: " + ScooterModel.Describe(board.transform));
            DevSmoke.Log("Rider pose: " + ScooterModel.DescribePose(board));
            SideShot(board.transform, new Vector3(-2.6f, 0.9f, 0.3f), screenshot + "_left");
            SideShot(board.transform, new Vector3(1.6f, 1.2f, 2.2f), screenshot + "_front");
            SideShot(board.transform, new Vector3(1.4f, 1.3f, -2.0f), screenshot + "_rear");
        }

        var start = board.transform.position;
        var began = Time.time;
        var slowSince = -1f;
        result.MinStamina = movement.CurrentStaminaReserve;
        var shot = false;
        var pushShot = false;
        var posed = false;
        while (Time.time - began < 22f)
        {
            if (board == null)
                break;
            if (!board.IsPushing && board.isGrounded && board.TimeSincePushStart >= 1f)
                UiKit.SetMember(board, "pushQueued", true);
            var travelled = Vector3.Dot(board.transform.position - start, lane.Direction);
            result.Distance = travelled;
            result.TopSpeed = Mathf.Max(result.TopSpeed, board.CurrentSpeed_Kmh);
            result.MinStamina = Mathf.Min(result.MinStamina, movement.CurrentStaminaReserve);
            result.Seconds = Time.time - began;
            if (result.Seconds >= result.SpeedBySecond.Count + 1)
                result.SpeedBySecond.Add(board.CurrentSpeed_Kmh);
            if (board.CurrentSpeed_Kmh < 1.5f && result.Seconds > 2.5f)
            {
                if (slowSince < 0f)
                    slowSince = Time.time;
                result.StuckSeconds = Mathf.Max(result.StuckSeconds, Time.time - slowSince);
            }
            else
            {
                slowSince = -1f;
            }
            if (itemId == ScooterItem.Id && !posed && result.Seconds > 3f)
            {
                posed = true;
                CheckPose(board, "while riding");
            }
            if (screenshot != null && !shot && result.Seconds > 2.2f)
            {
                shot = true;
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(DevSmoke.OutDir, screenshot + "_riding.png"));
                SideShot(board.transform, new Vector3(-2.6f, 0.8f, 0.6f), screenshot + "_moving");
                DevSmoke.Log($"Held model while riding ({screenshot}): riding {equippable.IsRiding}, container {UnityQuery.PathOf(equippable.ModelContainer)} at {V(equippable.ModelContainer.localPosition)}, raised {V(equippable.ModelPosition_Raised.localPosition)}, lowered {V(equippable.ModelPosition_Lowered.localPosition)}");
            }
            // Halfway through a push: the moment the skateboarder kicks the road.
            if (screenshot != null && !pushShot && result.Seconds > 1.5f && board.TimeSincePushStart > 0.35f && board.TimeSincePushStart < 0.55f)
            {
                pushShot = true;
                SideShot(board.transform, new Vector3(-2.6f, 0.8f, 0.6f), screenshot + "_push");
            }
            if (travelled >= lane.Length - 5f)
            {
                result.Finished = true;
                break;
            }
            if (result.StuckSeconds > 3f)
                break;
            yield return null;
        }
        if (equippable != null && equippable.IsRiding)
            equippable.Dismount();
        yield return 1f;
    }

    private static string Describe(RideResult r) =>
        $"top {F(r.TopSpeed)} km/h, {F(r.Distance)} m in {F(r.Seconds)} s, min stamina {F(r.MinStamina)}, stuck {F(r.StuckSeconds)} s, finished {r.Finished}; km/h by second: " +
        string.Join(" ", r.SpeedBySecond.Select(v => v.ToString("0", CultureInfo.InvariantCulture)));

    private static IEnumerator Ride()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return 4f;

        // The item and the shop.
        var definition = ScooterItem.Definition;
        DevSmoke.Check(definition != null && S1.Registry.ItemExists(ScooterItem.Id), "scooter item is not registered");
        DevSmoke.Log($"Item '{definition!.Name}' ({definition.ID}), price {F(definition.BasePurchasePrice)}, icon {(definition.Icon != null ? definition.Icon.rect.size.ToString() : "none")}");
        DevSmoke.Check(definition.Icon != null && definition.Icon.name.StartsWith("ElectricScooter"), "scooter icon not loaded");
        var sellers = UnityQuery.FindInScenes<S1.Dialogue.DialogueController_SkateboardSeller>();
        DevSmoke.Check(sellers.Count > 0, "skate shop seller not found");
        foreach (var seller in sellers)
        {
            var option = UnityQuery.ToManaged(seller.Options).FirstOrDefault(o => o.Item != null && o.Item.ID == ScooterItem.Id);
            DevSmoke.Check(option != null, "the seller does not offer the scooter");
            DevSmoke.Log($"Seller offers '{option!.Name}' for ${F(option.Price)} ({seller.Options.Count} options)");
            DevSmoke.Check(Mathf.Approximately(option.Price, 5000f), "wrong price");
            // The dialogue window shows a fixed number of choices; anything beyond is silently dropped.
            // (The shop's own dialogue node has none: every choice is a board from the seller's list.)
            var canvas = S1.UI.DialogueCanvas.Instance;
            var shown = canvas != null ? Member(Member(canvas, "dialogueChoices")!, "Count") : null;
            DevSmoke.Log($"Shop dialogue: {seller.Options.Count} boards, the window shows up to {shown}");
            DevSmoke.Check(shown is int max && seller.Options.Count <= max, "the scooter does not fit into the shop's dialogue");
        }

        var golden = UnityQuery.TryCastTo<S1.Skating.Skateboard_Equippable>(S1.Registry.GetItem("goldenskateboard").Equippable, out var prefab)
            ? prefab.SkateboardPrefab : null;
        DevSmoke.Check(golden != null, "golden skateboard prefab not found");
        var mask = golden!.GroundDetectionMask.value;
        var home = S1.PlayerScripts.Player.Local.transform.position;

        // Straight line: speed and stamina.
        var flat = FindLane(home, mask, 75f, withCurb: false);
        if (flat == null)
        {
            // A real save may start indoors: the street by the new game's start has both kinds of lanes.
            home = new Vector3(-30f, 0f, 50f);
            flat = FindLane(home, mask, 75f, withCurb: false);
        }
        DevSmoke.Check(flat != null, "no straight paved lane found near the start");
        DevSmoke.Log($"Flat lane of {F(flat!.Length)} m from {V(flat.Start)} towards {V(flat.Direction)}; player was at {V(home)}");
        var goldenFlat = new RideResult();
        yield return RideLane("goldenskateboard", flat, goldenFlat, "golden");
        DevSmoke.Log("Golden skateboard, flat: " + Describe(goldenFlat));
        var scooterFlat = new RideResult();
        yield return RideLane(ScooterItem.Id, flat, scooterFlat, "scooter");
        DevSmoke.Log("Electric scooter, flat: " + Describe(scooterFlat) + $", drains blocked by the patch: {ScooterRide.DrainsBlocked}");
        DevSmoke.Check(goldenFlat.MinStamina < 95f, "the golden skateboard did not use stamina (test is broken)");
        DevSmoke.Check(scooterFlat.MinStamina >= 99.9f, $"the scooter used stamina ({F(scooterFlat.MinStamina)})");
        DevSmoke.Check(scooterFlat.Finished, "the scooter did not finish the flat lane");
        // Same seconds of riding: the scooter is 30% faster at every moment.
        var second = Mathf.Min(goldenFlat.SpeedBySecond.Count, scooterFlat.SpeedBySecond.Count) - 1;
        DevSmoke.Check(second >= 3, "the rides were too short to compare");
        var ratio = scooterFlat.SpeedBySecond[second] / goldenFlat.SpeedBySecond[second];
        DevSmoke.Log($"After {second + 1} s: scooter {F(scooterFlat.SpeedBySecond[second])} km/h, golden {F(goldenFlat.SpeedBySecond[second])} km/h, ratio {F(ratio)}");
        DevSmoke.Check(ratio > 1.22f && ratio < 1.38f, $"the scooter should be 30% faster, ratio is {F(ratio)}");

        // A curb: the skateboard stops at it, the scooter rolls over.
        var curb = FindLane(home, mask, 26f, withCurb: true);
        DevSmoke.Check(curb != null, "no lane with a curb found near the start");
        DevSmoke.Log($"Curb lane from {V(curb!.Start)} towards {V(curb.Direction)}: step of {F(curb.CurbHeight)} m after {F(curb.CurbAt)} m");
        var goldenCurb = new RideResult();
        yield return RideLane("goldenskateboard", curb, goldenCurb, null);
        DevSmoke.Log("Golden skateboard, curb: " + Describe(goldenCurb));
        var scooterCurb = new RideResult();
        yield return RideLane(ScooterItem.Id, curb, scooterCurb, "curb");
        DevSmoke.Log("Electric scooter, curb: " + Describe(scooterCurb));
        DevSmoke.Check(scooterCurb.Finished && scooterCurb.StuckSeconds < 0.5f, "the scooter got stuck at the curb");
        // The whole ride took over a minute in one scene: the shop looked for its seller once.
        DevSmoke.Log($"Skate shop searches: {ScooterShop.Searches}");
        DevSmoke.Check(ScooterShop.Searches == 1, $"the shop searched for the seller {ScooterShop.Searches} times");
        // In a game of one the announcements come back to the local player (and are dropped there).
        DevSmoke.Log($"Scooter announcements: {ScooterSync.Sent} sent, {ScooterSync.Echoes} came back");
        DevSmoke.Check(ScooterSync.Sent >= 4 && ScooterSync.Echoes == ScooterSync.Sent, "the scooter was not announced to other players");
        DevSmoke.Finish(true, "ride done");
    }

    /// <summary>Grass: the scooter slows down there like a skateboard, unless OffRoad is on.</summary>
    private static IEnumerator OffRoad()
    {
        yield return DevSmoke.LoadDisposableSave();
        yield return 4f;
        var golden = UnityQuery.TryCastTo<S1.Skating.Skateboard_Equippable>(S1.Registry.GetItem("goldenskateboard").Equippable, out var prefab)
            ? prefab.SkateboardPrefab : null;
        DevSmoke.Check(golden != null, "golden skateboard prefab not found");
        var mask = golden!.GroundDetectionMask.value;
        var home = S1.PlayerScripts.Player.Local.transform.position;
        var grass = FindLane(home, mask, 45f, withCurb: false, terrain: true);
        if (grass == null)
        {
            home = new Vector3(-30f, 0f, 50f);
            grass = FindLane(home, mask, 45f, withCurb: false, terrain: true);
        }
        DevSmoke.Check(grass != null, "no straight lane on grass or gravel found near the start");
        DevSmoke.Log($"Grass lane of {F(grass!.Length)} m from {V(grass.Start)} towards {V(grass.Direction)}");
        try
        {
            Config.OffRoadOverride = false;
            var slowed = new RideResult();
            yield return RideLane(ScooterItem.Id, grass, slowed, null);
            DevSmoke.Log("Scooter on grass, off-road off: " + Describe(slowed));
            Config.OffRoadOverride = true;
            var free = new RideResult();
            yield return RideLane(ScooterItem.Id, grass, free, "offroad");
            DevSmoke.Log("Scooter on grass, off-road on: " + Describe(free));
            // The third second: both are still going (the lane may end at something in the grass).
            var second = Mathf.Min(3, Mathf.Min(slowed.SpeedBySecond.Count, free.SpeedBySecond.Count)) - 1;
            DevSmoke.Check(second >= 2, "the rides on grass were too short to compare");
            var ratio = free.SpeedBySecond[second] / Mathf.Max(slowed.SpeedBySecond[second], 0.1f);
            DevSmoke.Log($"After {second + 1} s on grass: {F(free.SpeedBySecond[second])} km/h off-road, {F(slowed.SpeedBySecond[second])} km/h without, ratio {F(ratio)}");
            DevSmoke.Check(ratio > 1.4f, $"off-road mode is not faster on grass (ratio {F(ratio)})");
        }
        finally
        {
            Config.OffRoadOverride = null;
        }
        DevSmoke.Finish(true, "offroad done");
    }

    /// <summary>Saves a sprite's texture area as PNG (through a render texture: game textures are not readable).</summary>
    private static void SaveSprite(Sprite sprite, string fileName)
    {
        var source = sprite.texture;
        var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(source, rt);
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var rect = sprite.textureRect;
        var copy = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(rect, 0, 0);
        copy.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(DevSmoke.OutDir, fileName), ImageConversion.EncodeToPNG(copy));
        DevSmoke.Log($"Saved {fileName}: {copy.width}x{copy.height}, sprite rect {rect}, texture {source.width}x{source.height}, pivot {sprite.pivot}, ppu {sprite.pixelsPerUnit}");
        Object.Destroy(copy);
    }

    private static IEnumerator Inspect()
    {
        yield return DevSmoke.LoadDisposableSave();
        var sb = new StringBuilder();
        S1.Skating.Skateboard? golden = null;
        foreach (var def in UnityQuery.ToManaged(S1.Registry.Instance.GetAllItems()))
        {
            if (def == null || def.Equippable == null || !UnityQuery.TryCastTo<S1.Skating.Skateboard_Equippable>(def.Equippable, out var equippable))
                continue;
            var price = UnityQuery.TryCastTo<S1.ItemFramework.StorableItemDefinition>(def, out var storable)
                ? $"price {F(storable.BasePurchasePrice)} resell x{F(storable.ResellMultiplier)} rank-locked {storable.RequiresLevelToPurchase} stored item {(storable.StoredItem != null ? storable.StoredItem.name : "none")}"
                : "";
            sb.AppendLine($"== {def.ID}: '{def.Name}' ({UnityQuery.TypeName(def)}, object '{def.name}') {price}");
            sb.AppendLine($"  category {def.Category}, stack {def.StackLimit}, legal {def.legalStatus}, equip mode {def.EquipMode}, icon {(def.Icon != null ? def.Icon.name + " " + def.Icon.rect.size : "none")}, custom ui {def.CustomItemUI != null}, equippable data {def.EquippableData != null}");
            sb.AppendLine($"  description: {def.Description}");
            sb.AppendLine($"  equippable prefab '{equippable.name}', board prefab '{(equippable.SkateboardPrefab != null ? equippable.SkateboardPrefab.name : "none")}'");
            var board = equippable.SkateboardPrefab;
            if (board == null)
                continue;
            sb.AppendLine($"  rigidbody mass {F(board.Rb.mass)} drag {F(board.Rb.drag)} angular {F(board.Rb.angularDrag)} constraints {board.Rb.constraints}; ground mask {board.GroundDetectionMask.value}; slow on terrain {board.SlowOnTerrain}");
            Settings(sb, board.DefaultSettings);
            if (def.ID != "goldenskateboard")
                continue;
            golden = board;
            if (def.Icon != null)
                SaveSprite(def.Icon, "golden_icon.png");
            sb.AppendLine("  -- golden board prefab --");
            sb.AppendLine($"    CoM {V(board.CoM.localPosition)}, front axle {V(board.FrontAxlePosition.localPosition)}, rear axle {V(board.RearAxlePosition.localPosition)}, player container {V(board.PlayerContainer.localPosition)}");
            sb.AppendLine("    hover points: " + string.Join(" ", board.HoverPoints.Select(p => V(board.transform.InverseTransformPoint(p.position)))));
            sb.AppendLine("    main colliders: " + string.Join(", ", board.MainColliders.Select(c => c.name)));
            Colliders(sb, board.transform);
            Renderers(sb, board.transform);
            var rain = Member(board, "_rainOverrideData");
            sb.AppendLine($"    rain override: {rain}");
            DevSmoke.Write("board_hierarchy.txt", UnityQuery.DumpHierarchy(board.transform, 12));
            DevSmoke.Write("equippable_hierarchy.txt", UnityQuery.DumpHierarchy(equippable.transform, 12));
            sb.AppendLine("  -- golden equippable prefab --");
            Renderers(sb, equippable.transform);
            sb.AppendLine("  -- frames (relative to each root): ridden board, view model, the avatar's prop --");
            Frames(sb, board.transform);
            Frames(sb, equippable.transform);
            if (equippable.AvatarEquippable != null)
            {
                sb.AppendLine($"    avatar prop asset path '{equippable.AvatarEquippable.AssetPath}'");
                Frames(sb, equippable.AvatarEquippable.transform);
                DevSmoke.Write("avatar_prop_hierarchy.txt", UnityQuery.DumpHierarchy(equippable.AvatarEquippable.transform, 12));
            }
            var animation = board.Animation;
            sb.AppendLine($"    hands: L lowered {V(animation.LeftHandLoweredAlignment.Transform.localPosition)} raised {V(animation.LeftHandRaisedAlignment.Transform.localPosition)}, R lowered {V(animation.RightHandLoweredAlignment.Transform.localPosition)} raised {V(animation.RightHandRaisedAlignment.Transform.localPosition)}; hand container {UnityQuery.PathOf(animation.HandContainer)}");
        }

        foreach (var seller in UnityQuery.FindInScenes<S1.Dialogue.DialogueController_SkateboardSeller>())
        {
            sb.AppendLine($"== seller {UnityQuery.PathOf(seller.transform)} at {V(seller.transform.position)}");
            foreach (var option in UnityQuery.ToManaged(seller.Options))
                sb.AppendLine($"  option '{option.Name}' ${F(option.Price)} available {option.IsAvailable} ({option.NotAvailableReason}) item {(option.Item != null ? option.Item.ID : "none")}");
        }
        var canvas = S1.UI.DialogueCanvas.Instance;
        var choices = canvas != null ? Member(canvas, "dialogueChoices") : null;
        sb.AppendLine($"== dialogue canvas choice entries: {(choices != null ? Member(choices, "Count") : "?")}");
        sb.AppendLine($"== stamina max {F(S1.PlayerScripts.PlayerMovement.StaminaReserveMax)}, fixed dt {F(Time.fixedDeltaTime)}");
        DevSmoke.Write("skateboards.txt", sb.ToString());
        DevSmoke.Log(sb.ToString());
        DevSmoke.Check(golden != null, "golden skateboard not found");
        DevSmoke.Finish(true, "inspect done");
    }
}
#endif
