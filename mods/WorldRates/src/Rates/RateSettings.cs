using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WorldRates.Rates;

/// <summary>Multiplier values of one world. Missing ids default to 1 (vanilla).</summary>
internal sealed class RateSettings
{
    private readonly Dictionary<string, float> _values = new(StringComparer.Ordinal);

    public event Action<string>? Changed;

    public float Get(string id) => _values.TryGetValue(id, out var v) ? v : 1f;

    public void Set(string id, float value)
    {
        var def = RateCatalog.Find(id);
        if (def == null)
            return;
        value = Mathf.Clamp((float)Math.Round(value / def.Step) * def.Step, def.Min, def.Max);
        if (_values.TryGetValue(id, out var old) && Mathf.Approximately(old, value))
            return;
        _values[id] = value;
        Changed?.Invoke(id);
    }

    public void SetAll(RateGroup group, float value)
    {
        foreach (var def in RateCatalog.InGroup(group))
            Set(def.Id, def.IsGroupTotal ? value : 1f);
    }

    public void Reset()
    {
        foreach (var def in RateCatalog.All)
            Set(def.Id, 1f);
    }

    /// <summary>
    /// Rust-style "xN server": all experience and income ×N, storage up to its maximum. Deal frequency
    /// is a matter of taste (×10 floods the phone), so presets leave it alone; only "vanilla" resets it.
    /// </summary>
    public void ApplyPreset(float n)
    {
        foreach (var def in RateCatalog.All)
        {
            if (def.Group == RateGroup.Storage)
                Set(def.Id, Mathf.Clamp(n, def.Min, def.Max));
            else if (def.Group != RateGroup.Deals)
                Set(def.Id, def.IsGroupTotal ? n : 1f);
            else if (Mathf.Approximately(n, 1f))
                Set(def.Id, 1f);
        }
    }

    public bool IsVanilla => RateCatalog.All.All(d => Mathf.Approximately(Get(d.Id), 1f));

    public void CopyFrom(RateSettings other)
    {
        foreach (var def in RateCatalog.All)
            Set(def.Id, other.Get(def.Id));
    }

    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.Append("# WorldRates multipliers (1 = vanilla). Edit in game: pause menu -> World Rates, or F10.\n");
        foreach (var def in RateCatalog.All)
            sb.Append(def.Id).Append('=').Append(Get(def.Id).ToString("0.###", CultureInfo.InvariantCulture)).Append('\n');
        return sb.ToString();
    }

    public void Deserialize(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#')
                continue;
            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;
            if (float.TryParse(line.Substring(eq + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                Set(line.Substring(0, eq).Trim(), value);
        }
    }

    public void SaveTo(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Serialize(), new UTF8Encoding(false));
    }

    public bool LoadFrom(string path)
    {
        if (!File.Exists(path))
            return false;
        Deserialize(File.ReadAllText(path, Encoding.UTF8));
        return true;
    }
}
