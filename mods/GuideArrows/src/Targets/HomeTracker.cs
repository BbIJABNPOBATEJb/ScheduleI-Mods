using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using S1Shared;
using UnityEngine;

namespace GuideArrows.Targets;

internal enum HomeMode
{
    Auto = 0,
    LastSlept = 1,
    MostValuable = 2,
}

/// <summary>
/// Finds the player's "home": the owned property they last slept in (remembered per world), or the
/// owned property whose placed equipment is worth the most.
/// </summary>
internal static class HomeTracker
{
    private const float ValueRefreshSeconds = 10f;

    private static string? _worldPath;
    private static string? _file;
    private static Vector3? _sleptAt;
    private static string _sleptProperty = "";
    private static S1.Property.Property? _mostValuable;
    private static float _mostValuableWorth;
    private static float _nextValueRefresh;

#if MONO
    private static readonly FieldInfo? BuildablesField = AccessTools.Field(typeof(S1.Property.Property), "BuildableItems");
#endif

    /// <summary>Call every frame: follows the loaded world.</summary>
    public static void Tick()
    {
        var loadManager = S1.Persistence.LoadManager.Instance;
        var path = loadManager != null && loadManager.IsGameLoaded ? loadManager.LoadedGameFolderPath : null;
        if (string.IsNullOrEmpty(path))
            path = null;
        if (path != _worldPath)
            SwitchWorld(path);
    }

    public static Target? Current(HomeMode mode)
    {
        if (_worldPath == null)
            return null;
        if (mode != HomeMode.MostValuable && _sleptAt.HasValue && StillOwned(_sleptProperty))
            return new Target { Kind = TargetKind.Home, Key = "home:slept", Label = LabelFor(_sleptProperty), Fixed = _sleptAt.Value };
        if (mode == HomeMode.LastSlept)
            return null;

        if (Time.unscaledTime >= _nextValueRefresh)
        {
            _nextValueRefresh = Time.unscaledTime + ValueRefreshSeconds;
            RefreshMostValuable();
        }
        if (_mostValuable == null)
            return null;
        var anchor = _mostValuable.PoI != null ? _mostValuable.PoI.transform : _mostValuable.transform;
        return new Target { Kind = TargetKind.Home, Key = "home:" + _mostValuable.PropertyCode, Label = _mostValuable.PropertyName, Anchor = anchor };
    }

    /// <summary>Remembers where the player went to sleep (in an owned property).</summary>
    public static void OnSleepStart()
    {
        var player = S1.PlayerScripts.Player.Local;
        if (player == null || _worldPath == null)
            return;
        var property = player.CurrentProperty ?? player.LastVisitedProperty;
        if (property == null || !property.IsOwned)
            return;
        _sleptAt = player.transform.position;
        _sleptProperty = property.PropertyCode;
        Save();
        MelonLogger.Msg($"Home: slept at {property.PropertyName}");
    }

    public static string Describe() =>
        $"slept={(_sleptAt.HasValue ? _sleptProperty + " " + _sleptAt.Value : "-")} mostValuable={(_mostValuable != null ? _mostValuable.PropertyName + " $" + _mostValuableWorth : "-")}";

    private static void RefreshMostValuable()
    {
        _mostValuable = null;
        _mostValuableWorth = -1f;
        foreach (var property in UnityQuery.ToManaged(S1.Property.Property.OwnedProperties))
        {
            if (property == null)
                continue;
            var worth = EquipmentValue(property);
            if (worth > _mostValuableWorth)
            {
                _mostValuableWorth = worth;
                _mostValuable = property;
            }
        }
    }

    /// <summary>Sum of the purchase prices of everything built/placed in the property.</summary>
    private static float EquipmentValue(S1.Property.Property property)
    {
        var total = 0f;
        foreach (var item in Buildables(property))
        {
            try
            {
                var definition = item != null && item.ItemInstance != null ? item.ItemInstance.Definition : null;
#if IL2CPP
                var storable = definition?.TryCast<S1.ItemFramework.StorableItemDefinition>();
#else
                var storable = definition as S1.ItemFramework.StorableItemDefinition;
#endif
                if (storable != null)
                    total += storable.BasePurchasePrice;
            }
            catch (Exception)
            {
                // Items that are still being placed have no instance yet.
            }
        }
        return total;
    }

    private static List<S1.EntityFramework.BuildableItem> Buildables(S1.Property.Property property)
    {
#if IL2CPP
        return UnityQuery.ToManaged(property.BuildableItems);
#else
        return BuildablesField?.GetValue(property) as List<S1.EntityFramework.BuildableItem> ?? new List<S1.EntityFramework.BuildableItem>();
#endif
    }

    private static bool StillOwned(string code) =>
        UnityQuery.ToManaged(S1.Property.Property.OwnedProperties).Any(p => p != null && p.PropertyCode == code);

    private static string LabelFor(string code) =>
        UnityQuery.ToManaged(S1.Property.Property.OwnedProperties).FirstOrDefault(p => p != null && p.PropertyCode == code)?.PropertyName ?? "";

    // --- per-world memory ---------------------------------------------------------------------------

    private static void SwitchWorld(string? path)
    {
        _worldPath = path;
        _sleptAt = null;
        _sleptProperty = "";
        _mostValuable = null;
        _nextValueRefresh = 0f;
        _file = path == null ? null : Path.Combine(MelonEnvironment.UserDataDirectory, ModInfo.Name, "worlds", WorldKey(path) + ".cfg");
        if (_file == null || !File.Exists(_file))
            return;
        foreach (var line in File.ReadAllLines(_file))
        {
            var parts = line.Split('=');
            if (parts.Length != 2)
                continue;
            if (parts[0] == "sleptProperty")
                _sleptProperty = parts[1].Trim();
            else if (parts[0] == "sleptAt" && TryParseVector(parts[1], out var v))
                _sleptAt = v;
        }
    }

    private static void Save()
    {
        if (_file == null || !_sleptAt.HasValue)
            return;
        if (GuideArrowsMod.SmokeActive)
            return; // smoke tests never write the player's files
        Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
        var v = _sleptAt.Value;
        File.WriteAllText(_file, "sleptProperty=" + _sleptProperty + "\nsleptAt=" +
            string.Join(",", new[] { v.x, v.y, v.z }.Select(f => f.ToString("0.###", CultureInfo.InvariantCulture))) + "\n");
    }

    private static bool TryParseVector(string text, out Vector3 v)
    {
        v = default;
        var p = text.Split(',');
        if (p.Length != 3)
            return false;
        var ok = float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out v.x)
            & float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out v.y)
            & float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out v.z);
        return ok;
    }

    /// <summary>"&lt;steam id folder&gt;_&lt;save folder&gt;", same scheme as World Rates.</summary>
    private static string WorldKey(string path)
    {
        var parts = path.Replace('\\', '/').TrimEnd('/').Split('/');
        var key = string.Join("_", parts.Skip(Math.Max(0, parts.Length - 2)));
        foreach (var c in Path.GetInvalidFileNameChars())
            key = key.Replace(c, '_');
        return key;
    }

    [HarmonyPatch]
    private static class SleepStartPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.GetDeclaredMethods(typeof(S1.GameTime.TimeManager)).First(m => m.Name.StartsWith("RpcLogic___StartSleep", StringComparison.Ordinal));

        private static void Prefix()
        {
            try
            {
                OnSleepStart();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Home tracking failed: {ex.Message}");
            }
        }
    }
}
