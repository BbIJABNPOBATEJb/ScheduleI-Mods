using System;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using S1Shared;
using UnityEngine;
using Object = UnityEngine.Object;
#if IL2CPP
using FinalIK = Il2CppRootMotion.FinalIK;
#else
using FinalIK = RootMotion.FinalIK;
#endif

namespace ElectricScooter;

/// <summary>
/// How the rider stands on the scooter, after RebornReaper's ElectricScooter add-on: upright and facing
/// forward, one foot ahead of the other, palms on the grips with the fingers around them.
///
/// The pose is solved once, on the first frame of the ride, with the avatar's own IK rig (FinalIK's
/// BipedIK, which the game points at targets on the skateboard) and then with direct two-bone solves
/// for the arms and legs. After that the whole skeleton is held in that pose relative to the scooter
/// (the motor does the work, so there is no pushing, crouching or jumping to animate). Used for every
/// scooter rider shown on this machine: the local player and, in multiplayer, the others.
/// </summary>
internal static class RiderPose
{
    // RebornReaper's values.
    /// <summary>Forward lean of the lower spine, degrees.</summary>
    private const float Lean = 2f;
    /// <summary>The fingers point this much down from straight ahead, degrees.</summary>
    private const float GripTilt = 10f;
    /// <summary>The handlebar of a ridden scooter is raised at least this much, metres, and further if the arms need it.</summary>
    private const float MinRise = 0.15f;
    private const float MaxRise = 0.40f;
    /// <summary>Hands this much behind the top of the bar, so the thumbs come round under it.</summary>
    private const float Pullback = 0.02f;
    /// <summary>Ankles in the deck's frame (x right, z forward): the left one ahead, the right one behind.</summary>
    private const float AnkleX = 0.025f;
    private const float AnkleZ = 0.16f;

    /// <summary>Riders by their BipedIK.</summary>
    private static readonly Dictionary<int, Rider> Riders = new();
    private static readonly List<int> Gone = new();
    private static bool _warned;

    /// <summary>
    /// Starts posing the rider of a scooter; call once its model is built. Does nothing (the game's
    /// skateboard stance stays) if the avatar is not rigged as expected.
    /// </summary>
    public static void Track(S1.PlayerScripts.Player player, S1.Skating.Skateboard board, Transform parts)
    {
        try
        {
            var avatar = player.Avatar;
            var controller = avatar != null ? UiKit.InChildren<S1.AvatarFramework.Animation.AvatarIKController>(avatar) : null;
            var ik = controller != null ? controller.BodyIK : null;
            if (avatar == null || ik == null || ik.solvers == null || ik.references == null)
                return;
            var id = ik.GetInstanceID();
            if (Riders.TryGetValue(id, out var old))
            {
                if (old.Board != null && old.Board.GetInstanceID() == board.GetInstanceID())
                    return;
                old.Dispose();
                Riders.Remove(id);
            }
            Transform? leftGrip = null, rightGrip = null;
            for (var i = 0; i < parts.childCount; i++)
            {
                var child = parts.GetChild(i);
                if (child.name == "Grip")
                {
                    if (child.localPosition.x < 0f)
                        leftGrip = child;
                    else
                        rightGrip = child;
                }
            }
            var pelvis = ik.references.pelvis;
            var leftHand = ik.solvers.leftHand?.bone3?.transform;
            var rightHand = ik.solvers.rightHand?.bone3?.transform;
            if (leftGrip == null || rightGrip == null || pelvis == null || leftHand == null || rightHand == null
                || avatar.LeftShoulder == null || avatar.RightShoulder == null || !Below(pelvis, avatar.transform))
                return;
            var rider = new Rider(player, board, parts, ik, leftGrip, rightGrip, pelvis);
            rider.LeftPalm = Hand.PalmFrame(leftHand, true);
            rider.RightPalm = Hand.PalmFrame(rightHand, false);
            rider.LeftContact = Hand.ContactOffset(leftHand, rider.LeftPalm);
            rider.RightContact = Hand.ContactOffset(rightHand, rider.RightPalm);
            if (!ik.enabled)
            {
                rider.EnabledIK = true;
                ik.enabled = true;
            }
            Riders.Add(id, rider);
        }
        catch (Exception ex)
        {
            Warn($"the rider keeps the skateboard stance: {ex.Message}");
        }
    }

    /// <summary>True if the transform is a descendant (not the root itself) of the root.</summary>
    private static bool Below(Transform t, Transform root)
    {
        var id = root.GetInstanceID();
        for (var p = t.parent; p != null; p = p.parent)
            if (p.GetInstanceID() == id)
                return true;
        return false;
    }

    /// <summary>True once the rider of this board is held in the scooter pose.</summary>
    public static bool Holds(S1.Skating.Skateboard board)
    {
        if (Riders.Count == 0)
            return false;
        var id = board.GetInstanceID();
        foreach (var rider in Riders.Values)
            if (rider.Snapshot.Captured && rider.Board != null && rider.Board.GetInstanceID() == id)
                return true;
        return false;
    }

    /// <summary>Call every frame from OnLateUpdate: keeps the pose and lets go of riders who got off.</summary>
    public static void LateTick()
    {
        if (Riders.Count == 0)
            return;
        Gone.Clear();
        foreach (var entry in Riders)
        {
            var rider = entry.Value;
            try
            {
                if (!rider.Valid)
                    Gone.Add(entry.Key);
                else if (rider.Snapshot.Captured)
                    rider.Snapshot.Apply();
            }
            catch (Exception ex)
            {
                Gone.Add(entry.Key);
                Warn($"the rider's pose was dropped: {ex.Message}");
            }
        }
        foreach (var id in Gone)
        {
            Riders[id].Dispose();
            Riders.Remove(id);
        }
    }

    // The game updates the avatar's animation, its IK and the mods' late update in no fixed order; the
    // pose is put back after each of them.

    [HarmonyPatch(typeof(FinalIK.BipedIK), "UpdateSolver")]
    private static class Solve
    {
        private static bool Prefix(FinalIK.BipedIK __instance) => BeforeSolve(__instance);

        private static void Postfix(FinalIK.BipedIK __instance) => AfterSolve(__instance);
    }

    [HarmonyPatch(typeof(S1.AvatarFramework.Animation.AvatarAnimation), "LateUpdate")]
    private static class Animated
    {
        private static void Postfix(S1.AvatarFramework.Animation.AvatarAnimation __instance)
        {
            if (Riders.Count == 0)
                return;
            var id = __instance.GetInstanceID();
            foreach (var rider in Riders.Values)
            {
                if (rider.Animation != id || !rider.Snapshot.Captured)
                    continue;
                try
                {
                    if (rider.Valid)
                        rider.Snapshot.Apply();
                }
                catch (Exception ex)
                {
                    Warn($"the rider's pose was not applied: {ex.Message}");
                }
                return;
            }
        }
    }

    private static bool BeforeSolve(FinalIK.BipedIK ik)
    {
        if (Riders.Count == 0 || !Riders.TryGetValue(ik.GetInstanceID(), out var rider) || rider.Failed)
            return true;
        if (rider.Solving)
        {
            // The game's solve threw last time, so the pose was never finished.
            rider.Fail("the IK solve failed");
            return true;
        }
        if (!rider.Valid)
            return true;
        if (rider.Snapshot.Captured)
        {
            rider.Snapshot.Apply();
            return false;
        }
        try
        {
            rider.SaveSolver();
            rider.Solving = true;
            Stance(rider);
            AimHands(rider);
        }
        catch (Exception ex)
        {
            rider.Fail(ex.Message);
        }
        return true;
    }

    /// <summary>After the game's solve with the scooter targets: straightens the legs, fits the arms and holds the result.</summary>
    private static void AfterSolve(FinalIK.BipedIK ik)
    {
        if (Riders.Count == 0 || !Riders.TryGetValue(ik.GetInstanceID(), out var rider) || !rider.Solving)
            return;
        try
        {
            var solvers = ik.solvers;
            var parts = rider.Parts;
            StraightenLegs(rider);
            var spine = ik.references.spine;
            if (spine != null && spine.Length > 0 && spine[0] != null)
                spine[0].rotation = Quaternion.AngleAxis(Lean, parts.right) * spine[0].rotation;

            SetFoot(solvers.leftFoot, rider.LeftFoot);
            SetFoot(solvers.rightFoot, rider.RightFoot);
            SetKnees(rider);
            solvers.leftFoot.Update();
            solvers.rightFoot.Update();
            TwoBone(solvers.leftFoot, rider.LeftFoot, rider.LeftKnee);
            TwoBone(solvers.rightFoot, rider.RightFoot, rider.RightKnee);

            // Rather than crouch to the handlebar, the handlebar comes up to the hands.
            AimHands(rider);
            var fit = parts.forward * Pullback;
            var rise = Mathf.Max(MinRise, BarRise(solvers.leftHand, rider.LeftHand, parts.up, fit), BarRise(solvers.rightHand, rider.RightHand, parts.up, fit));
            ScooterModel.RaiseHandlebar(parts, Mathf.Min(rise, MaxRise));
            AimHands(rider);
            solvers.leftHand.Update();
            solvers.rightHand.Update();
            TwoBone(solvers.leftHand, rider.LeftHand, null);
            TwoBone(solvers.rightHand, rider.RightHand, null);
            try
            {
                Hand.Curl(solvers.leftHand.bone3.transform, rider.LeftPalm);
                Hand.Curl(solvers.rightHand.bone3.transform, rider.RightPalm);
            }
            catch (Exception ex)
            {
                // Open hands on the grips are still fine.
                Warn($"the rider's fingers stay straight: {ex.Message}");
            }
            rider.Snapshot.Capture();
        }
        catch (Exception ex)
        {
            ScooterModel.RaiseHandlebar(rider.Parts, 0f);
            rider.Fail(ex.Message);
        }
        finally
        {
            rider.RestoreSolver();
        }
    }

    /// <summary>Hips and feet turned to face the way the scooter goes, the feet on the deck, the knees bending forward.</summary>
    private static void Stance(Rider rider)
    {
        var solvers = rider.IK.solvers;
        var avatar = rider.Player.Avatar;
        var parts = rider.Parts;
        var up = parts.up;
        var pelvis = solvers.pelvis.transform;
        var leftAnkle = solvers.leftFoot.bone3.transform;
        var rightAnkle = solvers.rightFoot.bone3.transform;
        if (!rider.Calibrated)
        {
            // Which way the hips face, from the shoulders: the game turns them sideways on a skateboard.
            var across = Vector3.ProjectOnPlane(avatar.RightShoulder.position - avatar.LeftShoulder.position, up).normalized;
            var facing = Vector3.Cross(across, up).normalized;
            if (facing.sqrMagnitude < 0.5f)
                throw new InvalidOperationException("the shoulders are not level");
            rider.PelvisForward = Quaternion.Inverse(pelvis.rotation) * facing;
            rider.Calibrated = true;
        }
        var current = Vector3.ProjectOnPlane(pelvis.rotation * rider.PelvisForward, up).normalized;
        var wanted = Vector3.ProjectOnPlane(parts.forward, up).normalized;
        var turn = Quaternion.AngleAxis(Vector3.SignedAngle(current, wanted, up), up);

        // The spine target would pull the turned body back sideways.
        if (solvers.spine != null)
            solvers.spine.IKPositionWeight = 0f;

        // The lower ankle sets the height of both (averaging puts the lower foot in the air), and the raised
        // foot takes the pitch of the planted one.
        var leftHeight = parts.InverseTransformPoint(leftAnkle.position).y;
        var rightHeight = parts.InverseTransformPoint(rightAnkle.position).y;
        var height = Mathf.Clamp(Mathf.Min(leftHeight, rightHeight), 0.03f, 0.20f);
        var leftRotation = turn * leftAnkle.rotation;
        var rightRotation = turn * rightAnkle.rotation;
        var planted = Toes(leftHeight <= rightHeight ? leftAnkle : rightAnkle);
        var leftToes = Toes(leftAnkle);
        var rightToes = Toes(rightAnkle);
        if (planted.sqrMagnitude > 1e-6f)
        {
            if (leftToes.sqrMagnitude > 1e-6f)
                leftRotation = turn * Quaternion.FromToRotation(leftToes, planted) * leftAnkle.rotation;
            if (rightToes.sqrMagnitude > 1e-6f)
                rightRotation = turn * Quaternion.FromToRotation(rightToes, planted) * rightAnkle.rotation;
        }

        rider.PelvisTarget.position = pelvis.position;
        rider.PelvisTarget.rotation = turn * pelvis.rotation;
        solvers.pelvis.target = rider.PelvisTarget;
        solvers.pelvis.positionWeight = 0f;
        solvers.pelvis.rotationWeight = 1f;
        solvers.pelvis.rotationOffset = Vector3.zero;
        rider.LeftFoot.position = parts.TransformPoint(new Vector3(-AnkleX, height, AnkleZ));
        rider.RightFoot.position = parts.TransformPoint(new Vector3(AnkleX, height, -AnkleZ));
        rider.LeftFoot.rotation = leftRotation;
        rider.RightFoot.rotation = rightRotation;
        SetFoot(solvers.leftFoot, rider.LeftFoot);
        SetFoot(solvers.rightFoot, rider.RightFoot);
        SetKnees(rider);
    }

    /// <summary>Where a toe bone points from the ankle, or zero if the rig has no toes.</summary>
    private static Vector3 Toes(Transform ankle)
    {
        foreach (var child in UnityQuery.GetComponentsInChildren<Transform>(ankle, true))
            if (child != null && child.GetInstanceID() != ankle.GetInstanceID() && child.name.IndexOf("toe", StringComparison.OrdinalIgnoreCase) >= 0)
                return child.position - ankle.position;
        return Vector3.zero;
    }

    private static void SetFoot(FinalIK.IKSolverLimb limb, Transform target)
    {
        limb.target = target;
        limb.IKPosition = target.position;
        limb.IKRotation = target.rotation;
        limb.IKPositionWeight = 1f;
        limb.IKRotationWeight = 1f;
        limb.maintainRotationWeight = 0f;
    }

    /// <summary>Knees bend forward over the toes (the skateboard's knee targets would bend them sideways).</summary>
    private static void SetKnees(Rider rider)
    {
        var parts = rider.Parts;
        rider.LeftKnee.position = rider.LeftFoot.position + parts.forward * 0.55f + parts.up * 0.45f;
        rider.RightKnee.position = rider.RightFoot.position + parts.forward * 0.55f + parts.up * 0.45f;
        foreach (var (limb, goal) in new[] { (rider.IK.solvers.leftFoot, rider.LeftKnee), (rider.IK.solvers.rightFoot, rider.RightKnee) })
        {
            limb.bendGoal = goal;
            limb.bendModifier = FinalIK.IKSolverLimb.BendModifier.Goal;
            limb.bendModifierWeight = 1f;
        }
    }

    /// <summary>Palms on the grips: the wrist goes where the palm, not the wrist bone, touches the bar.</summary>
    private static void AimHands(Rider rider)
    {
        var parts = rider.Parts;
        var solvers = rider.IK.solvers;
        foreach (var (limb, grip, target, palm, contact) in new[]
                 {
                     (solvers.leftHand, rider.LeftGrip, rider.LeftHand, rider.LeftPalm, rider.LeftContact),
                     (solvers.rightHand, rider.RightGrip, rider.RightHand, rider.RightPalm, rider.RightContact),
                 })
        {
            var rotation = Hand.WristRotation(parts, palm, GripTilt);
            var radius = Mathf.Max(Mathf.Abs(grip.lossyScale.x), Mathf.Abs(grip.lossyScale.z)) * 0.5f;
            var touch = grip.position + parts.up * radius - parts.forward * Pullback;
            target.rotation = rotation;
            target.position = touch - rotation * contact;
            limb.target = target;
            limb.IKPosition = target.position;
            limb.IKRotation = rotation;
            limb.IKPositionWeight = 1f;
            limb.IKRotationWeight = 1f;
            limb.maintainRotationWeight = 0f;
        }
    }

    /// <summary>Hips over the ankles and as high as the legs reach, so the legs are straight.</summary>
    private static void StraightenLegs(Rider rider)
    {
        var solvers = rider.IK.solvers;
        var pelvis = solvers.pelvis.transform;
        var up = rider.Parts.up;
        var hips = (solvers.leftFoot.bone1.transform.position + solvers.rightFoot.bone1.transform.position) * 0.5f;
        var ankles = (rider.LeftFoot.position + rider.RightFoot.position) * 0.5f;
        pelvis.position += Vector3.ProjectOnPlane(ankles - hips, up);
        var left = Reach(solvers.leftFoot, rider.LeftFoot.position, up, 0.9999f) + Vector3.Dot(rider.LeftFoot.position - solvers.leftFoot.bone1.transform.position, up);
        var right = Reach(solvers.rightFoot, rider.RightFoot.position, up, 0.9999f) + Vector3.Dot(rider.RightFoot.position - solvers.rightFoot.bone1.transform.position, up);
        pelvis.position += up * Mathf.Min(left, right);
    }

    /// <summary>How far above the target the limb's root can be with the limb (almost) straight.</summary>
    private static float Reach(FinalIK.IKSolverLimb limb, Vector3 target, Vector3 up, float stretch)
    {
        var a = limb.bone1.transform.position;
        var b = limb.bone2.transform.position;
        var c = limb.bone3.transform.position;
        var length = (Vector3.Distance(a, b) + Vector3.Distance(b, c)) * stretch;
        var across = Vector3.ProjectOnPlane(a - target, up).sqrMagnitude;
        return Mathf.Sqrt(Mathf.Max(0f, length * length - across));
    }

    /// <summary>How much higher the handlebar must be for the arm to reach it without stretching.</summary>
    private static float BarRise(FinalIK.IKSolverLimb limb, Transform target, Vector3 up, Vector3 fit)
    {
        var a = limb.bone1.transform.position;
        var b = limb.bone2.transform.position;
        var c = limb.bone3.transform.position;
        var length = (Vector3.Distance(a, b) + Vector3.Distance(b, c)) * 0.97f;
        var delta = target.position + fit - a;
        var across = Vector3.ProjectOnPlane(delta, up).sqrMagnitude;
        if (across >= length * length)
            return 0f;
        return Mathf.Max(0f, -Mathf.Sqrt(length * length - across) - Vector3.Dot(delta, up));
    }

    /// <summary>
    /// Bends a limb (upper bone, middle bone, end) so its end is at the target, in the plane of the knee
    /// goal (legs) or of the current bend (arms); then turns the end like the target.
    /// </summary>
    private static void TwoBone(FinalIK.IKSolverLimb limb, Transform target, Transform? bendGoal)
    {
        var upper = limb.bone1?.transform;
        var middle = limb.bone2?.transform;
        var end = limb.bone3?.transform;
        if (upper == null || middle == null || end == null)
            throw new InvalidOperationException("the limb rig is incomplete");
        var normal = bendGoal != null
            ? Vector3.Cross(bendGoal.position - upper.position, target.position - upper.position)
            : Vector3.Cross(middle.position - upper.position, end.position - middle.position);
        if (normal.sqrMagnitude < 1e-6f)
            normal = limb.bendNormal;
        var first = Vector3.Distance(upper.position, middle.position);
        var second = Vector3.Distance(middle.position, end.position);
        var delta = target.position - upper.position;
        if (normal.sqrMagnitude < 1e-6f || first < 1e-4f || second < 1e-4f || delta.sqrMagnitude < 1e-6f)
            throw new InvalidOperationException("the limb cannot bend");
        var direction = delta.normalized;
        var distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(first - second) + 1e-4f, first + second - 1e-4f);
        var bend = Vector3.Cross(direction, normal).normalized;
        if (bend.sqrMagnitude < 0.5f)
            throw new InvalidOperationException("the limb has no bend direction");
        var along = (first * first - second * second + distance * distance) / (2f * distance);
        var height = Mathf.Sqrt(Mathf.Max(0f, first * first - along * along));
        var joint = upper.position + direction * along + bend * height;
        var reached = upper.position + direction * distance;
        upper.rotation = Quaternion.FromToRotation(middle.position - upper.position, joint - upper.position) * upper.rotation;
        middle.rotation = Quaternion.FromToRotation(end.position - middle.position, reached - middle.position) * middle.rotation;
        end.rotation = target.rotation;
    }

#if DEV
    /// <summary>
    /// The posed rider of this board, in the deck's frame: how far each palm's contact point is from its
    /// grip, where the ankles are, how straight the knees are. Null if no rider is held in the pose.
    /// </summary>
    public static string? Describe(S1.Skating.Skateboard board, out float worstHand, out float straightestKnee, out float bentKnee, out bool feetOnDeck)
    {
        worstHand = float.MaxValue;
        straightestKnee = 0f;
        bentKnee = 180f;
        feetOnDeck = false;
        Rider? rider = null;
        foreach (var r in Riders.Values)
            if (r.Board != null && r.Board.GetInstanceID() == board.GetInstanceID())
                rider = r;
        if (rider == null || !rider.Snapshot.Captured)
            return null;
        var parts = rider.Parts;
        var solvers = rider.IK.solvers;
        string P(Vector3 world) => parts.InverseTransformPoint(world).ToString("F3");
        float Hand(FinalIK.IKSolverLimb limb, Transform grip, Quaternion palm, Vector3 contact)
        {
            var hand = limb.bone3.transform;
            var touch = hand.position + hand.rotation * contact;
            var radius = Mathf.Max(Mathf.Abs(grip.lossyScale.x), Mathf.Abs(grip.lossyScale.z)) * 0.5f;
            var top = grip.position + parts.up * radius - parts.forward * Pullback;
            return Vector3.Distance(touch, top);
        }
        float Knee(FinalIK.IKSolverLimb limb) => Vector3.Angle(limb.bone1.transform.position - limb.bone2.transform.position, limb.bone3.transform.position - limb.bone2.transform.position);
        var leftHand = Hand(solvers.leftHand, rider.LeftGrip, rider.LeftPalm, rider.LeftContact);
        var rightHand = Hand(solvers.rightHand, rider.RightGrip, rider.RightPalm, rider.RightContact);
        worstHand = Mathf.Max(leftHand, rightHand);
        var leftKnee = Knee(solvers.leftFoot);
        var rightKnee = Knee(solvers.rightFoot);
        straightestKnee = Mathf.Max(leftKnee, rightKnee);
        bentKnee = Mathf.Min(leftKnee, rightKnee);
        var leftAnkle = parts.InverseTransformPoint(solvers.leftFoot.bone3.transform.position);
        var rightAnkle = parts.InverseTransformPoint(solvers.rightFoot.bone3.transform.position);
        feetOnDeck = true;
        foreach (var a in new[] { leftAnkle, rightAnkle })
            feetOnDeck &= Mathf.Abs(a.x) < 0.15f && Mathf.Abs(a.z) < 0.35f && a.y > -0.05f && a.y < 0.25f;
        var leftElbow = Knee(solvers.leftHand);
        var rightElbow = Knee(solvers.rightHand);
        return $"palms {leftHand:F3} / {rightHand:F3} m from the grips; ankles {leftAnkle:F3} / {rightAnkle:F3}; knees {leftKnee:F0}° / {rightKnee:F0}°; elbows {leftElbow:F0}° / {rightElbow:F0}°; " +
               $"pelvis {P(solvers.pelvis.transform.position)}; shoulders {P(rider.Player.Avatar.LeftShoulder.position)} / {P(rider.Player.Avatar.RightShoulder.position)}; grips {P(rider.LeftGrip.position)} / {P(rider.RightGrip.position)}";
    }
#endif

    private static void Warn(string message)
    {
        if (_warned)
            return;
        _warned = true;
        MelonLogger.Warning($"Electric Scooter: {message}");
    }

    /// <summary>A limb solver's settings, to put back after the scooter's own solve.</summary>
    private readonly struct LimbState
    {
        private readonly Transform? _target, _bendGoal;
        private readonly float _positionWeight, _rotationWeight, _maintainRotation, _bendWeight;
        private readonly Vector3 _position, _bendNormal;
        private readonly Quaternion _rotation;
        private readonly FinalIK.IKSolverLimb.BendModifier _bendModifier;

        public LimbState(FinalIK.IKSolverLimb limb)
        {
            _target = limb.target;
            _positionWeight = limb.IKPositionWeight;
            _rotationWeight = limb.IKRotationWeight;
            _position = limb.IKPosition;
            _rotation = limb.IKRotation;
            _maintainRotation = limb.maintainRotationWeight;
            _bendGoal = limb.bendGoal;
            _bendModifier = limb.bendModifier;
            _bendWeight = limb.bendModifierWeight;
            _bendNormal = limb.bendNormal;
        }

        public void Restore(FinalIK.IKSolverLimb limb)
        {
            limb.target = _target!;
            limb.IKPositionWeight = _positionWeight;
            limb.IKRotationWeight = _rotationWeight;
            limb.IKPosition = _position;
            limb.IKRotation = _rotation;
            limb.maintainRotationWeight = _maintainRotation;
            limb.bendGoal = _bendGoal!;
            limb.bendModifier = _bendModifier;
            limb.bendModifierWeight = _bendWeight;
            limb.bendNormal = _bendNormal;
        }
    }

    private sealed class Rider
    {
        public readonly S1.PlayerScripts.Player Player;
        public readonly S1.Skating.Skateboard Board;
        public readonly Transform Parts, LeftGrip, RightGrip;
        public readonly FinalIK.BipedIK IK;
        public readonly int Animation;
        public readonly Snapshot Snapshot;
        // Targets the limbs are solved to, kept on the scooter.
        public readonly Transform LeftHand, RightHand, PelvisTarget, LeftFoot, RightFoot, LeftKnee, RightKnee;
        public Quaternion LeftPalm, RightPalm;
        public Vector3 LeftContact, RightContact, PelvisForward;
        public bool EnabledIK, Solving, Failed, Calibrated;

        private LimbState _leftHand, _rightHand, _leftFoot, _rightFoot;
        private Transform? _pelvisTarget;
        private float _pelvisPositionWeight, _pelvisRotationWeight, _spineWeight;
        private Vector3 _pelvisOffset;

        public Rider(S1.PlayerScripts.Player player, S1.Skating.Skateboard board, Transform parts, FinalIK.BipedIK ik,
            Transform leftGrip, Transform rightGrip, Transform pelvis)
        {
            Player = player;
            Board = board;
            Parts = parts;
            IK = ik;
            LeftGrip = leftGrip;
            RightGrip = rightGrip;
            var animation = player.Avatar.Animation;
            Animation = animation != null ? animation.GetInstanceID() : 0;
            Snapshot = new Snapshot(pelvis, parts);
            LeftHand = Target("Left Hand");
            RightHand = Target("Right Hand");
            PelvisTarget = Target("Pelvis");
            LeftFoot = Target("Left Foot");
            RightFoot = Target("Right Foot");
            LeftKnee = Target("Left Knee");
            RightKnee = Target("Right Knee");
        }

        private Transform Target(string name)
        {
            var t = new GameObject("Rider " + name).transform;
            t.SetParent(Parts, false);
            return t;
        }

        /// <summary>Still on this scooter and on his feet.</summary>
        public bool Valid
        {
            get
            {
                if (Failed || Board == null || Parts == null || Player == null || LeftGrip == null || RightGrip == null)
                    return false;
                var active = Player.ActiveSkateboard;
                var avatar = Player.Avatar;
                return active != null && active.GetInstanceID() == Board.GetInstanceID() && avatar != null && !avatar.Ragdolled;
            }
        }

        public void SaveSolver()
        {
            var solvers = IK.solvers;
            _leftHand = new LimbState(solvers.leftHand);
            _rightHand = new LimbState(solvers.rightHand);
            _leftFoot = new LimbState(solvers.leftFoot);
            _rightFoot = new LimbState(solvers.rightFoot);
            _pelvisTarget = solvers.pelvis.target;
            _pelvisPositionWeight = solvers.pelvis.positionWeight;
            _pelvisRotationWeight = solvers.pelvis.rotationWeight;
            _pelvisOffset = solvers.pelvis.rotationOffset;
            _spineWeight = solvers.spine != null ? solvers.spine.IKPositionWeight : 0f;
        }

        public void RestoreSolver()
        {
            if (!Solving)
                return;
            Solving = false;
            if (IK == null || IK.solvers == null)
                return;
            var solvers = IK.solvers;
            _leftHand.Restore(solvers.leftHand);
            _rightHand.Restore(solvers.rightHand);
            _leftFoot.Restore(solvers.leftFoot);
            _rightFoot.Restore(solvers.rightFoot);
            solvers.pelvis.target = _pelvisTarget!;
            solvers.pelvis.positionWeight = _pelvisPositionWeight;
            solvers.pelvis.rotationWeight = _pelvisRotationWeight;
            solvers.pelvis.rotationOffset = _pelvisOffset;
            if (solvers.spine != null)
                solvers.spine.IKPositionWeight = _spineWeight;
        }

        /// <summary>Gives the rider back to the game's skateboard stance.</summary>
        public void Fail(string reason)
        {
            RestoreSolver();
            Snapshot.Restore();
            Failed = true;
            Warn($"the rider keeps the skateboard stance: {reason}");
        }

        public void Dispose()
        {
            RestoreSolver();
            Snapshot.Restore();
            if (EnabledIK && IK != null)
                IK.enabled = false;
            foreach (var t in new[] { LeftHand, RightHand, PelvisTarget, LeftFoot, RightFoot, LeftKnee, RightKnee })
                if (t != null)
                    Object.Destroy(t.gameObject);
        }
    }

    /// <summary>The skeleton from the pelvis down, held relative to the scooter.</summary>
    private sealed class Snapshot
    {
        private readonly Transform _frame;
        private readonly List<Transform> _bones = new();
        private readonly List<Vector3> _localPositions = new(), _positions = new();
        private readonly List<Quaternion> _localRotations = new(), _rotations = new();

        public bool Captured { get; private set; }

        public Snapshot(Transform pelvis, Transform frame)
        {
            _frame = frame;
            var bones = UnityQuery.GetComponentsInChildren<Transform>(pelvis, true);
            // Parents before children, so a child is placed after its parent has moved.
            bones.Sort((a, b) => Depth(a).CompareTo(Depth(b)));
            foreach (var bone in bones)
            {
                _bones.Add(bone);
                _localPositions.Add(bone.localPosition);
                _localRotations.Add(bone.localRotation);
            }
        }

        private static int Depth(Transform t)
        {
            var depth = 0;
            for (var p = t.parent; p != null; p = p.parent)
                depth++;
            return depth;
        }

        public void Capture()
        {
            var inverse = Quaternion.Inverse(_frame.rotation);
            _positions.Clear();
            _rotations.Clear();
            foreach (var bone in _bones)
            {
                _positions.Add(bone != null ? _frame.InverseTransformPoint(bone.position) : Vector3.zero);
                _rotations.Add(bone != null ? inverse * bone.rotation : Quaternion.identity);
            }
            Captured = true;
        }

        public void Apply()
        {
            if (!Captured || _frame == null)
                return;
            var rotation = _frame.rotation;
            for (var i = 0; i < _bones.Count; i++)
            {
                var bone = _bones[i];
                if (bone == null)
                    continue;
                bone.SetPositionAndRotation(_frame.TransformPoint(_positions[i]), rotation * _rotations[i]);
            }
        }

        /// <summary>Puts the bones back where the animation had them when the ride began.</summary>
        public void Restore()
        {
            if (!Captured)
                return;
            Captured = false;
            for (var i = 0; i < _bones.Count; i++)
            {
                var bone = _bones[i];
                if (bone == null)
                    continue;
                bone.localPosition = _localPositions[i];
                bone.localRotation = _localRotations[i];
            }
        }
    }

    /// <summary>The hand's anatomy, worked out from the finger bones' names and positions.</summary>
    private static class Hand
    {
        private static readonly string[] Fingers = { "index", "middle", "ring", "little", "thumb" };

        /// <summary>
        /// The hand bone's rotation relative to a frame whose forward runs along the fingers and whose up
        /// comes out of the palm (left and right hands have mirrored bone axes).
        /// </summary>
        public static Quaternion PalmFrame(Transform hand, bool left)
        {
            Transform? middle = null, index = null, little = null, thumb = null;
            float middleD = float.MaxValue, indexD = float.MaxValue, littleD = float.MaxValue, thumbD = float.MaxValue;
            foreach (var child in UnityQuery.GetComponentsInChildren<Transform>(hand, true))
            {
                if (child == null || child.GetInstanceID() == hand.GetInstanceID())
                    continue;
                var name = child.name.ToLowerInvariant();
                if (Ignored(name) || Endpoint(name))
                    continue;
                var d = (child.position - hand.position).sqrMagnitude;
                if (d < 1e-6f || d > 0.04f)
                    continue;
                if (name.Contains("middle") && d < middleD) { middle = child; middleD = d; }
                if (name.Contains("index") && d < indexD) { index = child; indexD = d; }
                if ((name.Contains("little") || name.Contains("pinky")) && d < littleD) { little = child; littleD = d; }
                if (name.Contains("thumb") && d < thumbD) { thumb = child; thumbD = d; }
            }
            var finger = middle != null ? middle : index;
            if (finger != null && ((index != null && little != null) || thumb != null))
            {
                var along = (finger.position - hand.position).normalized;
                var across = index != null && little != null ? index.position - little.position : thumb!.position - hand.position;
                var palm = Vector3.Cross(along, across).normalized * (left ? -1f : 1f);
                if (palm.sqrMagnitude > 0.5f)
                    return Quaternion.Inverse(hand.rotation) * Quaternion.LookRotation(along, palm);
            }
            return Quaternion.LookRotation(Vector3.up, left ? Vector3.back : Vector3.forward);
        }

        /// <summary>The hand bone's rotation for the fingers to point forward (tilted down) over the bar, palm down on it.</summary>
        public static Quaternion WristRotation(Transform parts, Quaternion palmFrame, float tilt)
        {
            var fingers = Quaternion.AngleAxis(tilt, parts.right) * parts.forward;
            return Quaternion.LookRotation(fingers, -parts.up) * Quaternion.Inverse(palmFrame);
        }

        /// <summary>
        /// From the wrist bone to the skin of the palm, in the hand bone's frame: 60% of the way to the
        /// fingers' bases, plus 8 mm out of the palm.
        /// </summary>
        public static Vector3 ContactOffset(Transform hand, Quaternion palmFrame)
        {
            var nearest = new Dictionary<string, (float Distance, Vector3 Position)>();
            foreach (var child in UnityQuery.GetComponentsInChildren<Transform>(hand, true))
            {
                if (child == null || child.GetInstanceID() == hand.GetInstanceID())
                    continue;
                var name = child.name.ToLowerInvariant();
                var finger = Finger(name);
                if (finger == null || finger == "thumb" || Endpoint(name))
                    continue;
                var d = (child.position - hand.position).sqrMagnitude;
                if (d < 1e-6f || d > 0.04f)
                    continue;
                if (!nearest.TryGetValue(finger, out var best) || d < best.Distance)
                    nearest[finger] = (d, child.position);
            }
            Vector3 local;
            if (nearest.Count > 0)
            {
                var centre = Vector3.zero;
                foreach (var entry in nearest.Values)
                    centre += entry.Position;
                centre /= nearest.Count;
                local = Quaternion.Inverse(hand.rotation) * ((centre - hand.position) * 0.6f);
            }
            else
            {
                local = palmFrame * Vector3.forward * 0.045f;
            }
            return local + palmFrame * Vector3.up * 0.008f;
        }

        /// <summary>Curls each finger and the thumb round the grip (the joints turn; nothing moves or stretches).</summary>
        public static void Curl(Transform hand, Quaternion palmFrame)
        {
            var joints = new List<(Transform Bone, Quaternion Rotation)>();
            foreach (var bone in UnityQuery.GetComponentsInChildren<Transform>(hand, true))
            {
                if (bone == null || bone.GetInstanceID() == hand.GetInstanceID() || Finger(bone.name.ToLowerInvariant()) == null || Endpoint(bone.name.ToLowerInvariant()))
                    continue;
                joints.Add((bone, bone.localRotation));
            }
            if (joints.Count == 0)
                return;
            try
            {
                var palmSide = hand.rotation * palmFrame * Vector3.up;
                var along = hand.rotation * palmFrame * Vector3.forward;
                foreach (var finger in Fingers)
                {
                    var root = FirstJoint(joints, hand, finger);
                    if (root == null)
                        continue;
                    var next = NextJoint(root, finger);
                    var direction = next != null ? (next.position - root.position).normalized : along;
                    var axis = Vector3.Cross(direction, palmSide).normalized;
                    if (axis.sqrMagnitude < 0.5f)
                        continue;
                    var joint = root;
                    for (var stage = 0; stage < 3 && joint != null; stage++)
                    {
                        next = NextJoint(joint, finger);
                        var degrees = finger == "thumb" ? (stage == 0 ? 15f : stage == 1 ? 25f : 20f) : (stage == 0 ? 18f : stage == 1 ? 50f : 35f);
                        joint.rotation = Quaternion.AngleAxis(degrees, axis) * joint.rotation;
                        joint = next != null && !Endpoint(next.name.ToLowerInvariant()) ? next : null;
                    }
                }
            }
            catch
            {
                foreach (var (bone, rotation) in joints)
                    if (bone != null)
                        bone.localRotation = rotation;
                throw;
            }
        }

        /// <summary>The finger's joint nearest the hand: no joint of the same finger above it.</summary>
        private static Transform? FirstJoint(List<(Transform Bone, Quaternion Rotation)> joints, Transform hand, string finger)
        {
            foreach (var (bone, _) in joints)
            {
                if (Finger(bone.name.ToLowerInvariant()) != finger)
                    continue;
                var nested = false;
                for (var p = bone.parent; p != null && p.GetInstanceID() != hand.GetInstanceID(); p = p.parent)
                {
                    var name = p.name.ToLowerInvariant();
                    if (Finger(name) == finger && !Endpoint(name))
                    {
                        nested = true;
                        break;
                    }
                }
                if (!nested)
                    return bone;
            }
            return null;
        }

        private static Transform? NextJoint(Transform joint, string finger)
        {
            for (var i = 0; i < joint.childCount; i++)
            {
                var child = joint.GetChild(i);
                var name = child.name.ToLowerInvariant();
                if (Finger(name) == finger || Endpoint(name))
                    return child;
            }
            return null;
        }

        private static string? Finger(string name)
        {
            if (Ignored(name))
                return null;
            if (name.Contains("index")) return "index";
            if (name.Contains("middle")) return "middle";
            if (name.Contains("ring")) return "ring";
            if (name.Contains("little") || name.Contains("pinky")) return "little";
            return name.Contains("thumb") ? "thumb" : null;
        }

        private static bool Ignored(string name) => name.Contains("target") || name.Contains("alignment");

        private static bool Endpoint(string name) => name.Contains("end") || name.Contains("tip");
    }
}
