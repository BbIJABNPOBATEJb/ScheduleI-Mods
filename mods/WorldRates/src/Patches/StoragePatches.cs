using System;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using S1Shared;
using UnityEngine;
using WorldRates.Rates;
#if IL2CPP
using SlotList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.ItemFramework.ItemSlot>;
#else
using SlotList = System.Collections.Generic.List<ScheduleOne.ItemFramework.ItemSlot>;
#endif

namespace WorldRates.Patches;

/// <summary>
/// Storage capacity. Placed storage (racks, shelves, safes...) and vehicle trunks get
/// base slots × multiplier (max 20, the game's UI limit). Capacity can change live: extra slots are
/// added at the end; shrinking only removes empty trailing slots, so items are never deleted.
/// Loading a save that has more items than the current capacity grows the storage to fit.
/// </summary>
internal static class StoragePatches
{
    private const int MaxSlots = 20;
    private const int MaxColumns = 10;

    private sealed class Info
    {
        public int BaseSlots;
        public int BaseRows;
        public string RateId = "";
        public S1.Vehicles.LandVehicle? Vehicle;
    }

    private static readonly Dictionary<int, Info> Known = new();

#if DEV
    public static Action<string>? Trace;
#endif

    /// <summary>Applies the current multipliers to every tracked storage in the world.</summary>
    public static void ApplyAll()
    {
        var changed = 0;
        foreach (var entity in UnityQuery.FindInScenes<S1.Storage.StorageEntity>())
        {
            if (!Known.TryGetValue(entity.GetInstanceID(), out var info))
                continue;
#if DEV
            if (Trace != null)
                Trace($"apply {UnityQuery.PathOf(entity.transform)} slots={entity.ItemSlots.Count} base={info.BaseSlots} target={TargetSlots(info)}");
#endif
            if (Resize(entity, TargetSlots(info), info))
                changed++;
        }
        if (changed == 0)
            return;
        // Dragging a slider applies many times per second: log once it settles.
        _pendingLog += changed;
        if (Time.realtimeSinceStartup - _lastLog < 2f)
            return;
        MelonLogger.Msg($"Storage capacity updated on {_pendingLog} containers");
        _pendingLog = 0;
        _lastLog = Time.realtimeSinceStartup;
    }

    private static int _pendingLog;
    private static float _lastLog = -10f;

    public static string Describe()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var entity in UnityQuery.FindInScenes<S1.Storage.StorageEntity>())
        {
            Known.TryGetValue(entity.GetInstanceID(), out var info);
            sb.Append(UnityQuery.PathOf(entity.transform)).Append(" | ").Append(entity.StorageEntityName)
              .Append(" slots=").Append(entity.ItemSlots.Count).Append(" rows=").Append(entity.DisplayRowCount)
              .Append(" rate=").Append(info?.RateId ?? "-").Append(" base=").Append(info?.BaseSlots ?? 0).Append('\n');
        }
        return sb.ToString();
    }

    private static string? RateIdFor(S1.Storage.StorageEntity entity)
    {
        if (UnityQuery.GetComponentInParent<S1.Vehicles.LandVehicle>(entity) != null)
            return RateCatalog.StorageVehicles;
        if (UnityQuery.GetComponentInParent<S1.ObjectScripts.PlaceableStorageEntity>(entity) != null
            || UnityQuery.GetComponentInParent<S1.ObjectScripts.SurfaceStorageEntity>(entity) != null)
            return RateCatalog.StorageFurniture;
        return null;
    }

    private static int TargetSlots(Info info)
    {
        // NPC, decorative and delivery vehicles keep their trunks; only the player's vehicles grow.
        if (info.Vehicle != null && !info.Vehicle.IsPlayerOwned)
            return info.BaseSlots;
        var mult = RatesState.InWorld ? RatesState.Current.Get(info.RateId) : 1f;
        return Mathf.Clamp(Mathf.RoundToInt(info.BaseSlots * mult), 1, Math.Max(MaxSlots, info.BaseSlots));
    }

    private static int RowsFor(Info info, int slots)
    {
        var baseColumns = Mathf.CeilToInt(info.BaseSlots / (float)Math.Max(1, info.BaseRows));
        var columns = Mathf.Clamp(baseColumns, 1, MaxColumns);
        return Mathf.Clamp(Mathf.CeilToInt(slots / (float)columns), info.BaseRows, 5);
    }

    private static bool Resize(S1.Storage.StorageEntity entity, int target, Info info)
    {
        var slots = entity.ItemSlots;
        var current = slots.Count;
        if (target > current)
        {
            if (entity.IsOpened || current == 0)
                return false; // don't rebuild slots under an open storage menu; retried on next apply
            var template = slots[0];
            var added = new List<S1.ItemFramework.ItemSlot>();
            for (var i = current; i < target; i++)
            {
                var slot = new S1.ItemFramework.ItemSlot(entity.SlotsAreFilterable);
                slot.onItemDataChanged = template.onItemDataChanged;
                slot.SetSlotOwner(entity.AsOwner());
                template.SiblingSet?.AddSlot(slot);
                added.Add(slot);
            }
            SyncTransitSlots(entity, added, null);
        }
        else if (target < current)
        {
            var removed = new List<S1.ItemFramework.ItemSlot>();
            for (var i = current - 1; i >= target; i--)
            {
                var slot = slots[i];
                if (slot.ItemInstance != null || entity.IsOpened)
                    break;
                slots.RemoveAt(i);
                slot.SiblingSet?.Slots.Remove(slot);
                removed.Add(slot);
            }
            if (removed.Count == 0)
                return false;
            SyncTransitSlots(entity, null, removed);
        }
        else
        {
            return false;
        }
        entity.SlotCount = slots.Count;
        entity.DisplayRowCount = RowsFor(info, slots.Count);
        return true;
    }

    /// <summary>Employees move items through PlaceableStorageEntity.InputSlots/OutputSlots.</summary>
    private static void SyncTransitSlots(S1.Storage.StorageEntity entity, List<S1.ItemFramework.ItemSlot>? added,
        List<S1.ItemFramework.ItemSlot>? removed)
    {
        var placeable = UnityQuery.GetComponentInParent<S1.ObjectScripts.PlaceableStorageEntity>(entity);
        if (placeable == null)
            return;
        foreach (var list in new[] { placeable.InputSlots, placeable.OutputSlots })
        {
            if (list == null)
                continue;
            if (added != null)
                foreach (var slot in added)
                    list.Add(slot);
            if (removed != null)
                foreach (var slot in removed)
                    list.Remove(slot);
        }
    }

    private static S1.ItemFramework.IItemSlotOwner AsOwner(this S1.Storage.StorageEntity entity)
    {
#if IL2CPP
        return new S1.ItemFramework.IItemSlotOwner(entity.Pointer);
#else
        return entity;
#endif
    }

    [HarmonyPatch(typeof(S1.Storage.StorageEntity), nameof(S1.Storage.StorageEntity.Awake))]
    private static class AwakePatch
    {
        // Runs before the game creates the slots: just change how many it creates.
        private static void Prefix(S1.Storage.StorageEntity __instance)
        {
            try
            {
                var rateId = RateIdFor(__instance);
                if (rateId == null)
                    return;
                var info = new Info
                {
                    BaseSlots = __instance.SlotCount,
                    BaseRows = __instance.DisplayRowCount,
                    RateId = rateId,
                    Vehicle = UnityQuery.GetComponentInParent<S1.Vehicles.LandVehicle>(__instance),
                };
                Known[__instance.GetInstanceID()] = info;
                var target = TargetSlots(info);
                __instance.SlotCount = target;
                __instance.DisplayRowCount = RowsFor(info, target);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Storage setup failed: {ex}");
            }
        }
    }

    /// <summary>Never drop saved items: grow the storage when the save has more slots than it currently has.</summary>
    private static void GrowToFit(int savedSlots, S1.ItemFramework.ItemSlot? firstSlot)
    {
        if (firstSlot == null)
            return;
        var owner = firstSlot.SlotOwner;
        if (owner == null || !UnityQuery.TryCastTo<S1.Storage.StorageEntity>(OwnerObject(owner), out var entity))
            return;
        if (!Known.TryGetValue(entity.GetInstanceID(), out var info))
            info = new Info { BaseSlots = entity.SlotCount, BaseRows = entity.DisplayRowCount };
        if (Resize(entity, Math.Min(savedSlots, MaxSlots), info))
            MelonLogger.Msg($"'{entity.StorageEntityName}' grown to {entity.ItemSlots.Count} slots to keep saved items");
    }

    private static UnityEngine.Object? OwnerObject(S1.ItemFramework.IItemSlotOwner owner)
    {
#if IL2CPP
        return new UnityEngine.Object(owner.Pointer);
#else
        return owner as UnityEngine.Object;
#endif
    }

    [HarmonyPatch(typeof(S1.Persistence.Datas.ItemSet), nameof(S1.Persistence.Datas.ItemSet.LoadTo),
        new[] { typeof(SlotList) })]
    private static class LoadItemSet
    {
        private static void Prefix(S1.Persistence.Datas.ItemSet __instance, SlotList slots)
        {
            if (__instance.Items != null && __instance.Items.Length > slots.Count && slots.Count > 0)
                GrowToFit(__instance.Items.Length, slots[0]);
        }
    }

    [HarmonyPatch(typeof(S1.Persistence.Datas.DeserializedItemSet), nameof(S1.Persistence.Datas.DeserializedItemSet.LoadTo),
        new[] { typeof(SlotList) })]
    private static class LoadDeserializedItemSet
    {
        private static void Prefix(S1.Persistence.Datas.DeserializedItemSet __instance, SlotList slots)
        {
            if (__instance.Items != null && __instance.Items.Length > slots.Count && slots.Count > 0)
                GrowToFit(__instance.Items.Length, slots[0]);
        }
    }
}
