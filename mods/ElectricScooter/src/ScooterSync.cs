using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using MelonLoader;
using S1Shared;
using UnityEngine;

namespace ElectricScooter;

/// <summary>
/// Multiplayer: lets the other players see the scooter. The scooter is the Golden Skateboard's board
/// and models with parts built on top, and only the rider's game knows it is a scooter; everybody
/// else would see him on the skateboard. So the rider's game tells the others, with the message the
/// game itself passes to the item in a player's hands (an "equippable message"); their games then
/// build the scooter on that board or prop and pose the rider on it. A game without the mod ignores
/// the message, and its player keeps seeing the skateboard.
/// </summary>
internal static class ScooterSync
{
    /// <summary>
    /// The message: "ElectricScooter|1|held|240" or "ElectricScooter|1|ride|240|1234": in the hands or
    /// ridden, the hover height in mm (the wheels' size), and the ridden board's network id.
    /// </summary>
    private const string Tag = "ElectricScooter|1|";
    /// <summary>The scooter in the hands is announced again every few seconds, for players who come in later.</summary>
    private const float HeldRepeat = 4f;
    /// <summary>
    /// A ride is announced on mounting, once more a moment later, and again when somebody joins: on the
    /// other machines the rider has nothing in his hands then, and games without the mod log a warning
    /// for every message that finds no item in the hands.
    /// </summary>
    private const float RideRepeat = 3f;
    private const int RideMessages = 2;
    /// <summary>How long a ride message waits for the board it names to show up on this machine.</summary>
    private const float RideWait = 10f;

    private static string? _state;
    private static int _sent;
    private static float _next;
    private static int _players;
    private static bool _failed;
    private static readonly Dictionary<int, Ride> Waiting = new();
    private static readonly List<int> Done = new();

#if DEV
    /// <summary>Messages sent, and our own messages that came back (they do in a game of one).</summary>
    public static int Sent, Echoes;
#endif

    public static void Tick()
    {
        if (_failed)
            return;
        try
        {
            Announce();
            Settle();
        }
        catch (Exception ex)
        {
            _failed = true;
            MelonLogger.Warning($"Electric Scooter: other players will see a skateboard instead of your scooter: {ex.Message}");
        }
    }

    /// <summary>Tells the other players what the local player does with the scooter.</summary>
    private static void Announce()
    {
        var player = S1.PlayerScripts.Player.Local;
        if (player == null || player.NetworkObject == null)
        {
            _state = null;
            return;
        }
        var mm = Mathf.RoundToInt(ScooterRide.HoverHeight * 1000f).ToString(CultureInfo.InvariantCulture);
        string? state = null;
        var board = ScooterRide.Board;
        if (board != null)
        {
            if (board.NetworkObject != null)
                state = $"ride|{mm}|{board.NetworkObject.ObjectId.ToString(CultureInfo.InvariantCulture)}";
        }
        else
        {
            // The others see the board in the hands when the game shows it in the local avatar's hands
            // too (it sends both from the same call). Right after a ride the hands stay empty.
            var inventory = S1.PlayerScripts.PlayerInventory.Instance;
            if (inventory != null && ScooterItem.Is(inventory.EquippedItem) && player.Avatar != null && player.Avatar.CurrentEquippable != null)
                state = $"held|{mm}";
        }
        var list = S1.PlayerScripts.Player.PlayerList;
        var players = list != null ? list.Count : 0;
        var joined = players > _players;
        _players = players;
        if (state == null)
        {
            _state = null;
            return;
        }
        var now = Time.unscaledTime;
        if (state != _state || joined)
        {
            _state = state;
            _sent = 0;
            _next = now;
        }
        var riding = state[0] == 'r';
        if (now < _next || (riding && _sent >= RideMessages))
            return;
        _sent++;
        _next = now + (riding ? RideRepeat : HeldRepeat);
        player.SendEquippableMessage_Networked(Tag + state, UnityEngine.Random.Range(1, int.MaxValue));
#if DEV
        Sent++;
#endif
    }

    [HarmonyPatch(typeof(S1.AvatarFramework.Avatar), nameof(S1.AvatarFramework.Avatar.ReceiveEquippableMessage))]
    private static class Receive
    {
        private static bool Prefix(S1.AvatarFramework.Avatar __instance, string message)
        {
            if (message == null || !message.StartsWith(Tag, StringComparison.Ordinal))
                return true;
            try
            {
                Received(__instance, message.Substring(Tag.Length));
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Electric Scooter: another player's scooter could not be shown: {ex.Message}");
            }
            // Not for the item in the hands: the game would only warn that there is none while riding.
            return false;
        }
    }

    private static void Received(S1.AvatarFramework.Avatar avatar, string body)
    {
        var fields = body.Split('|');
        if (fields.Length < 2 || !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mm))
            return;
        var player = PlayerOf(avatar);
        if (player == null)
            return;
        if (player.IsLocalPlayer)
        {
#if DEV
            Echoes++;
#endif
            return;
        }
        var hover = Mathf.Clamp(mm / 1000f, 0.14f, 0.44f);
        switch (fields[0])
        {
            case "held":
                // The message comes after the one that put the board in the avatar's hands, so this is it.
                var prop = avatar.CurrentEquippable;
                if (prop != null && !ScooterModel.IsDressed(prop.transform))
                    ScooterModel.DressHeld(prop.transform, hover);
                break;
            case "ride":
                if (fields.Length >= 3 && int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var boardId))
                {
                    Waiting[player.GetInstanceID()] = new Ride(player, boardId, hover, Time.unscaledTime + RideWait);
                    Settle();
                }
                break;
        }
    }

    private static S1.PlayerScripts.Player? PlayerOf(S1.AvatarFramework.Avatar avatar)
    {
        var player = UnityQuery.GetComponentInParent<S1.PlayerScripts.Player>(avatar);
        if (player != null)
            return player;
        var id = avatar.GetInstanceID();
        foreach (var p in UnityQuery.ToManaged(S1.PlayerScripts.Player.PlayerList))
            if (p != null && p.Avatar != null && p.Avatar.GetInstanceID() == id)
                return p;
        return null;
    }

    /// <summary>Builds the scooter on announced rides once their boards are here.</summary>
    private static void Settle()
    {
        if (Waiting.Count == 0)
            return;
        Done.Clear();
        var now = Time.unscaledTime;
        foreach (var entry in Waiting)
        {
            var ride = entry.Value;
            var board = ride.Player != null ? ride.Player.ActiveSkateboard : null;
            if (board != null && board.NetworkObject != null && board.NetworkObject.ObjectId == ride.BoardId)
            {
                if (!ScooterModel.IsDressed(board.transform))
                {
                    var parts = ScooterModel.DressRemote(board, ride.Hover);
                    if (parts != null)
                        RiderPose.Track(ride.Player!, board, parts);
                }
                Done.Add(entry.Key);
            }
            else if (ride.Player == null || now > ride.Until)
            {
                Done.Add(entry.Key);
            }
        }
        foreach (var key in Done)
            Waiting.Remove(key);
    }

    private sealed class Ride
    {
        public readonly S1.PlayerScripts.Player? Player;
        public readonly int BoardId;
        public readonly float Hover, Until;

        public Ride(S1.PlayerScripts.Player player, int boardId, float hover, float until)
        {
            Player = player;
            BoardId = boardId;
            Hover = hover;
            Until = until;
        }
    }
}
