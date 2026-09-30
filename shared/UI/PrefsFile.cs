using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace S1Shared.UI;

/// <summary>
/// A MelonPreferences category whose changes (e.g. from dragging a slider) are written to disk
/// once they settle, instead of on every value.
/// </summary>
internal sealed class PrefsFile
{
    private readonly MelonPreferences_Category _category;
    private readonly List<Action> _resets = new();
    private readonly List<Action> _originals = new();
    private float _saveAt = -1f;

    public PrefsFile(string name) => _category = MelonPreferences.CreateCategory(name);

    /// <summary>Smoke tests turn saving off so they never touch the player's settings.</summary>
    public bool SavingDisabled { get; set; }

    public MelonPreferences_Entry<T> Entry<T>(string id, T defaultValue, string description) =>
        _category.CreateEntry(id, defaultValue, description: description);

    /// <summary>A typed setting that schedules a save when changed and can be reset to its default.</summary>
    public Setting<T> Add<T>(string id, T defaultValue, string description)
    {
        var entry = Entry(id, defaultValue, description);
        var setting = new Setting<T>(this, entry, defaultValue);
        _resets.Add(setting.Reset);
        var loaded = entry.Value;
        _originals.Add(() => entry.Value = loaded);
        return setting;
    }

    /// <summary>
    /// Puts back the values read at startup. MelonLoader saves every preference when the game quits,
    /// so smoke tests call this on quit to leave the player's settings untouched.
    /// </summary>
    public void RestoreLoadedValues()
    {
        foreach (var restore in _originals)
            restore();
    }

    public void ResetAll()
    {
        foreach (var reset in _resets)
            reset();
        MarkDirty();
    }

    public void MarkDirty() => _saveAt = Time.realtimeSinceStartup + 1f;

    /// <summary>Call every frame.</summary>
    public void Tick()
    {
        if (_saveAt > 0f && Time.realtimeSinceStartup >= _saveAt)
            SaveNow();
    }

    public void SaveNow()
    {
        _saveAt = -1f;
        if (!SavingDisabled)
            _category.SaveToFile(false);
    }
}

internal sealed class Setting<T>
{
    private readonly PrefsFile _file;
    private readonly MelonPreferences_Entry<T> _entry;

    public Setting(PrefsFile file, MelonPreferences_Entry<T> entry, T defaultValue)
    {
        _file = file;
        _entry = entry;
        Default = defaultValue;
    }

    public T Default { get; }

    public T Value
    {
        get => _entry.Value;
        set
        {
            _entry.Value = value;
            _file.MarkDirty();
        }
    }

    public void Reset() => _entry.Value = Default;
}

/// <summary>Hotkeys through legacy input, which the game keeps enabled next to the Input System.</summary>
internal static class Keys
{
    private static bool _broken;

    public static bool Down(KeyCode key)
    {
        if (key == KeyCode.None || _broken)
            return false;
        try
        {
            if (S1.GameInput.IsTyping)
                return false;
            return Input.GetKeyDown(key);
        }
        catch (Exception ex)
        {
            _broken = true;
            MelonLogger.Warning($"Hotkeys disabled, legacy input unavailable: {ex.Message}");
            return false;
        }
    }
}
