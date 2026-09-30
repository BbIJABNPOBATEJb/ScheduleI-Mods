using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Polyglot.Localization;

/// <summary>All translations of one language.</summary>
internal sealed class TranslationTable
{
    /// <summary>Game placeholder tokens such as &lt;PRICE&gt; or &lt;NPC_NAME&gt; (TMP rich-text tags are lowercase).</summary>
    public static readonly Regex PlaceholderToken = new(@"<([A-Z][A-Z0-9_]*)>", RegexOptions.Compiled);

    private readonly Dictionary<string, string> _exact = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _upper = new(StringComparer.Ordinal);
    private readonly List<PlaceholderTemplate> _templates = new();
    private readonly List<RegexRule> _regexes = new();

    public int ExactCount => _exact.Count;
    public int TemplateCount => _templates.Count;
    public int RegexCount => _regexes.Count;
    public IReadOnlyList<PlaceholderTemplate> Templates => _templates;
    public IReadOnlyList<RegexRule> Regexes => _regexes;

    // DialogueCanvas / WorldspaceDialogueRenderer recolor named colors before display.
    private static readonly (string Name, string Shown)[] DisplayedColors =
    {
        ("<color=red>", "<color=#FF6666>"),
        ("<color=green>", "<color=#93FF58>"),
        ("<color=blue>", "<color=#76C9FF>"),
    };

    public void Add(string key, string value)
    {
        if (key.Length == 0 || value.Length == 0)
            return;
        AddOne(key, value);

        // Also register the variant the player actually sees on screen.
        var shownKey = key;
        var shownValue = value;
        foreach (var (name, shown) in DisplayedColors)
        {
            shownKey = shownKey.Replace(name, shown);
            shownValue = shownValue.Replace(name, shown);
        }
        if (!ReferenceEquals(shownKey, key) && shownKey != key)
            AddOne(shownKey, shownValue);
    }

    private void AddOne(string key, string value)
    {
        _exact[key] = value;
        if (PlaceholderToken.IsMatch(key))
        {
            _templates.RemoveAll(t => t.Key == key);
            _templates.Add(new PlaceholderTemplate(key, value));
        }
    }

    public void AddRegex(Regex pattern, string replacement) => _regexes.Add(new RegexRule(pattern, replacement));

    /// <summary>Call after loading all files.</summary>
    public void Seal()
    {
        _upper.Clear();
        foreach (var kv in _exact)
        {
            var upperKey = kv.Key.ToUpperInvariant();
            if (!_upper.ContainsKey(upperKey))
                _upper[upperKey] = kv.Value;
        }
        // Most specific (longest literal prefix) first.
        _templates.Sort((a, b) => b.Prefix.Length.CompareTo(a.Prefix.Length));
    }

    public bool TryExact(string key, out string value) => _exact.TryGetValue(key, out value!);

    public bool TryUpper(string upperKey, out string value) => _upper.TryGetValue(upperKey, out value!);
}

internal sealed class PlaceholderTemplate
{
    public string Key { get; }
    public string Value { get; }
    public string Prefix { get; }
    public Regex Pattern { get; }
    public string[] Names { get; }

    public PlaceholderTemplate(string key, string value)
    {
        Key = key;
        Value = value;
        var names = new List<string>();
        var sb = new StringBuilder("^");
        var last = 0;
        foreach (Match m in TranslationTable.PlaceholderToken.Matches(key))
        {
            sb.Append(Regex.Escape(key.Substring(last, m.Index - last)));
            var name = m.Groups[1].Value;
            var index = names.IndexOf(name);
            if (index >= 0)
            {
                sb.Append(@"\k<p").Append(index).Append('>');
            }
            else
            {
                sb.Append("(?<p").Append(names.Count).Append(">.+?)");
                names.Add(name);
            }
            last = m.Index + m.Length;
        }
        sb.Append(Regex.Escape(key.Substring(last))).Append('$');
        Pattern = new Regex(sb.ToString(), RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Names = names.ToArray();
        var first = TranslationTable.PlaceholderToken.Match(key);
        Prefix = key.Substring(0, first.Index);
    }

    public string? TryApply(string text, Func<string, string> translateValue)
    {
        if (Prefix.Length > 0 && !text.StartsWith(Prefix, StringComparison.Ordinal))
            return null;
        var m = Pattern.Match(text);
        if (!m.Success)
            return null;
        var result = Value;
        for (var i = 0; i < Names.Length; i++)
            result = result.Replace("<" + Names[i] + ">", translateValue(m.Groups["p" + i].Value));
        return result;
    }
}

internal sealed class RegexRule
{
    private static readonly Regex GroupRef = new(@"\$(\d+)", RegexOptions.Compiled);

    public Regex Pattern { get; }
    public string Replacement { get; }

    public RegexRule(Regex pattern, string replacement)
    {
        Pattern = pattern;
        Replacement = replacement;
    }

    public string? TryApply(string text, Func<string, string> translateValue)
    {
        var m = Pattern.Match(text);
        if (!m.Success)
            return null;
        return GroupRef.Replace(Replacement, r =>
        {
            var index = int.Parse(r.Groups[1].Value);
            return index < m.Groups.Count ? translateValue(m.Groups[index].Value) : r.Value;
        });
    }

    public override string ToString() => Pattern.ToString();
}
