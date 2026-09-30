using System;
using System.IO;
using System.Linq;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

namespace WorldRates.Rates;

/// <summary>
/// Settings of the loaded world. Each world (save folder) has its own file in
/// UserData/WorldRates/worlds/; a new world starts from UserData/WorldRates/defaults.cfg.
/// </summary>
internal static class RatesState
{
    public static string Root => Path.Combine(MelonEnvironment.UserDataDirectory, ModInfo.Name);
    private static string DefaultsPath => Path.Combine(Root, "defaults.cfg");

    public static RateSettings Current { get; } = new();
    public static string? WorldPath { get; private set; }
    public static string? WorldName { get; private set; }
    public static bool InWorld => WorldPath != null;

    public static event Action? WorldChanged;

    private static string? _settingsFile;
    private static bool _dirty;
    private static float _saveAt;

    public static void Init()
    {
        Directory.CreateDirectory(Path.Combine(Root, "worlds"));
        Current.Changed += _ =>
        {
            _dirty = true;
            _saveAt = Time.realtimeSinceStartup + 1f;
        };
        LoadDefaults();
    }

    /// <summary>Only the host (or single player) applies multipliers.</summary>
    public static bool IsAuthority
    {
        get
        {
#if IL2CPP
            return Il2CppFishNet.InstanceFinder.IsServer || !Il2CppFishNet.InstanceFinder.IsClient;
#else
            return FishNet.InstanceFinder.IsServer || !FishNet.InstanceFinder.IsClient;
#endif
        }
    }

    public static float Multiplier(string id, string groupTotalId) =>
        InWorld ? Current.Get(groupTotalId) * Current.Get(id) : 1f;

    /// <summary>Called every frame; follows the game's loaded world and flushes pending saves.</summary>
    public static void Tick()
    {
        var loadManager = S1.Persistence.LoadManager.Instance;
        // Switch as soon as loading starts, so storages created during the load already use the world's rates.
        var path = loadManager != null && (loadManager.IsGameLoaded || loadManager.IsLoading) ? loadManager.LoadedGameFolderPath : null;
        if (string.IsNullOrEmpty(path))
            path = null;
        if (path != WorldPath)
            SwitchWorld(path);

        if (_dirty && Time.realtimeSinceStartup >= _saveAt)
            Save();
    }

    public static void Save()
    {
        _dirty = false;
        if (_settingsFile != null)
            Current.SaveTo(_settingsFile);
    }

    public static void SaveIfChanged()
    {
        if (_dirty)
            Save();
    }

    public static void SaveAsDefaults() => Current.SaveTo(DefaultsPath);

    private static void SwitchWorld(string? path)
    {
        if (_dirty)
            Save();
        WorldPath = path;
        if (path == null)
        {
            WorldName = null;
            _settingsFile = null;
            LoadDefaults();
            MelonLogger.Msg("Left world, rates inactive");
        }
        else
        {
            WorldName = Path.GetFileName(path.TrimEnd('\\', '/'));
            _settingsFile = Path.Combine(Root, "worlds", WorldKey(path) + ".cfg");
            LoadDefaults();
            if (Current.LoadFrom(_settingsFile))
                MelonLogger.Msg($"World '{WorldName}': rates loaded from {Path.GetFileName(_settingsFile)}");
            else
                MelonLogger.Msg($"World '{WorldName}': new world, using defaults");
            _dirty = false;
        }
        WorldChanged?.Invoke();
    }

    private static void LoadDefaults()
    {
        Current.Reset();
        Current.LoadFrom(DefaultsPath);
        _dirty = false;
    }

    /// <summary>"&lt;steam id folder&gt;_&lt;save folder&gt;" — stable for a save slot, readable for humans.</summary>
    private static string WorldKey(string path)
    {
        var parts = path.Replace('\\', '/').TrimEnd('/').Split('/');
        var key = string.Join("_", parts.Skip(Math.Max(0, parts.Length - 2)));
        foreach (var c in Path.GetInvalidFileNameChars())
            key = key.Replace(c, '_');
        return key;
    }
}
