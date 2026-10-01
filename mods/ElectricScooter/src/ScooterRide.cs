using System;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace ElectricScooter;

/// <summary>
/// Turns the board the local player has just mounted into the scooter (when the scooter item is the
/// one equipped): faster, riding high on big wheels so curbs pass underneath, and free of stamina.
///
/// The board is the Golden Skateboard's own networked prefab, so nothing new has to be spawned; only
/// its settings object is replaced — on the rider's machine, which is the one simulating the board.
/// </summary>
internal static class ScooterRide
{
    /// <summary>The golden board hovers 0.096 m above the ground.</summary>
    private const float VanillaHoverHeight = 0.096f;
    /// <summary>Half of the board collider's thickness plus how far the board sags under its weight.</summary>
    private const float ColliderDrop = 0.04f;
    private const float MinPushStamina = 12.5f;

    private static S1.Skating.Skateboard? _board;
    private static S1.Experimental.SkateboardSettings? _tuned;
    private static float _stamina;
    private static bool _failed;

    /// <summary>True while the local player rides the scooter.</summary>
    public static bool Riding => _board != null;

    /// <summary>Hover height that lets a curb of the configured step height pass under the board.</summary>
    public static float HoverHeight => Config.StepHeight + ColliderDrop;

    public static void Tick()
    {
        if (_failed)
            return;
        try
        {
            var player = S1.PlayerScripts.Player.Local;
            var board = player != null ? player.ActiveSkateboard : null;
            if (board == null)
            {
                _board = null;
                _tuned = null;
                return;
            }
            if (_board == null || _board.GetInstanceID() != board.GetInstanceID())
            {
                var inventory = S1.PlayerScripts.PlayerInventory.Instance;
                if (inventory == null || !ScooterItem.Is(inventory.EquippedItem))
                    return;
                Begin(board);
            }
            else if (!Same(board.CurentSettings, _tuned))
            {
                // The game rebuilds the settings from the prefab when the weather changes.
                Tune(board);
            }
            KeepStamina();
            ScooterModel.HoldHandlebar();
        }
        catch (Exception ex)
        {
            _failed = true;
            _board = null;
            MelonLogger.Error($"Electric Scooter ride failed: {ex}");
        }
    }

    private static void Begin(S1.Skating.Skateboard board)
    {
        _board = board;
        Tune(board);
        // The game spawned the board at a skateboard's height; put it on its wheels at once.
        var lift = board.transform.up * (HoverHeight - VanillaHoverHeight);
        board.transform.position += lift;
        board.Rb.position += lift;
        var movement = S1.PlayerScripts.PlayerMovement.Instance;
        _stamina = movement != null ? Mathf.Max(movement.CurrentStaminaReserve, MinPushStamina) : MinPushStamina;
        ScooterModel.Dress(board);
    }

    private static void Tune(S1.Skating.Skateboard board)
    {
        var settings = board.CurentSettings.Clone();
        // Faster in every respect: the same ride as the golden board, only with all speeds scaled.
        var speed = Config.Speed;
        settings.TopSpeed_Kmh *= speed;
        settings.PushForceMultiplier *= speed;
        // The game looks this curve up at 1 / top speed; keep it reading the same value as before.
        settings.LongitudinalFrictionCurve = Stretched(settings.LongitudinalFrictionCurve, 1f / speed);
        // Hover like the game's own big-wheeled Offroad Skateboard, only higher.
        settings.HoverHeight = HoverHeight;
        settings.HoverRayLength = HoverHeight + 0.1f;
        settings.Hover_P = 120f;
        settings.Hover_I = 5f;
        settings.Hover_D = 2f;
#if IL2CPP
        board._settings = settings;
#else
        SettingsField.SetValue(board, settings);
#endif
        _tuned = settings;
    }

    /// <summary>The curve with its time axis scaled.</summary>
    private static AnimationCurve Stretched(AnimationCurve curve, float factor)
    {
        var source = curve.keys;
        var keys = new Keyframe[source.Length];
        for (var i = 0; i < keys.Length; i++)
        {
            var key = source[i];
            key.time *= factor;
            key.inTangent /= factor;
            key.outTangent /= factor;
            keys[i] = key;
        }
        return new AnimationCurve(keys);
    }

#if !IL2CPP
    private static readonly System.Reflection.FieldInfo SettingsField = AccessTools.Field(typeof(S1.Skating.Skateboard), "_settings");
#endif

    private static bool Same(S1.Experimental.SkateboardSettings? a, S1.Experimental.SkateboardSettings? b)
    {
        if (a == null || b == null)
            return a == null && b == null;
#if IL2CPP
        return a.Pointer == b.Pointer;
#else
        return ReferenceEquals(a, b);
#endif
    }

    /// <summary>
    /// Safety net for the stamina patch below (the method is small enough to be inlined on IL2CPP):
    /// whatever a push took is given back before the next frame is drawn.
    /// </summary>
    private static void KeepStamina()
    {
        if (!Config.InfiniteStamina)
            return;
        var movement = S1.PlayerScripts.PlayerMovement.Instance;
        if (movement == null)
            return;
        var current = movement.CurrentStaminaReserve;
        if (current < _stamina)
            movement.SetStamina(_stamina, false);
        else
            _stamina = current;
    }

    [HarmonyPatch(typeof(S1.PlayerScripts.PlayerMovement), nameof(S1.PlayerScripts.PlayerMovement.ChangeStamina))]
    private static class NoStaminaDrain
    {
        private static void Prefix(ref float change)
        {
            if (change < 0f && Riding && Config.InfiniteStamina)
            {
                change = 0f;
#if DEV
                DrainsBlocked++;
#endif
            }
        }
    }

#if DEV
    /// <summary>How many stamina drains the patch has blocked (to see whether the hook fires at all).</summary>
    public static int DrainsBlocked;
#endif
}
