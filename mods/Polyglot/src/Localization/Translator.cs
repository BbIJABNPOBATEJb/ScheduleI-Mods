using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using MelonLoader;

namespace Polyglot.Localization;

/// <summary>
/// Translates rendered text. The game always keeps its English strings; translation happens
/// at render time (see TextHooks), so switching language only needs a re-render.
///
/// Lookup order for a text (whitespace around it is preserved):
///   exact → numbers as {0},{1}… → &lt;PLACEHOLDER&gt; templates → regex rules → ALL-CAPS variant
///   → per line (multi-line texts) → per segment between rich-text tags.
/// </summary>
internal static class Translator
{
    private const int MaxCacheSize = 40000;
    private const int MaxDepth = 2;

    private static readonly Regex Numbers = new(@"(?<![\p{L}\p{N}_#])\d+(?:[.,:]\d+)*", RegexOptions.Compiled);
    private static readonly Regex RichTag = new(@"<[^<>]+>", RegexOptions.Compiled);

    private static readonly Dictionary<string, string> Cache = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Protected = new(StringComparer.Ordinal);
    private static TranslationTable? _table;

    public static LanguageInfo Current { get; private set; } = null!;

    /// <summary>True when a non-source language is active.</summary>
    public static bool Active => _table != null;

    public static event Action? LanguageChanged;

    public static void SetLanguage(LanguageInfo language)
    {
        Current = language;
        _table = language.IsSource ? null : LanguageCatalog.GetTable(language);
        Cache.Clear();
        MissLog.SetLanguage(language);
        MelonLogger.Msg($"Language: {language}");
        LanguageChanged?.Invoke();
    }

    /// <summary>Texts that must never be translated (e.g. language names in the language picker).</summary>
    public static void Protect(string text) => Protected.Add(text);

    public static string Translate(string text)
    {
        if (_table == null || string.IsNullOrEmpty(text) || Protected.Contains(text))
            return text;
        if (Cache.TryGetValue(text, out var cached))
            return cached;

        string result;
        try
        {
            result = TranslateCore(text, 0, record: true);
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Translate failed for \"{text}\": {ex}");
            result = text;
        }
        if (Cache.Count >= MaxCacheSize)
            Cache.Clear();
        Cache[text] = result;
        return result;
    }

    private static string TranslateCore(string text, int depth, bool record)
    {
        if (!HasLetter(text))
            return text;

        // Keep surrounding whitespace out of the key.
        var start = 0;
        var end = text.Length;
        while (start < end && char.IsWhiteSpace(text[start])) start++;
        while (end > start && char.IsWhiteSpace(text[end - 1])) end--;
        var core = start == 0 && end == text.Length ? text : text.Substring(start, end - start);

        var translated = TranslateWhole(core);
        if (translated == null && depth < MaxDepth)
            translated = TranslateParts(core, depth);

        if (translated == null)
        {
            if (record)
                MissLog.Record(NormalizeNumbers(core, null));
            return text;
        }
        return start == 0 && end == text.Length ? translated : text.Substring(0, start) + translated + text.Substring(end);
    }

    private static string? TranslateWhole(string core)
    {
        var table = _table!;
        if (table.TryExact(core, out var value))
            return value;

        var numbers = new List<string>();
        var normalized = NormalizeNumbers(core, numbers);
        if (numbers.Count > 0 && table.TryExact(normalized, out value))
            return FillNumbers(value, numbers);

        foreach (var template in table.Templates)
        {
            var applied = template.TryApply(core, TranslateValue);
            if (applied != null)
                return applied;
        }
        foreach (var rule in table.Regexes)
        {
            var applied = rule.TryApply(core, TranslateValue);
            if (applied != null)
                return applied;
        }

        if (IsAllCaps(core))
        {
            if (table.TryUpper(core.ToUpperInvariant(), out value))
                return value.ToUpper(Current.Culture);
            if (numbers.Count > 0 && table.TryUpper(normalized.ToUpperInvariant(), out value))
                return FillNumbers(value, numbers).ToUpper(Current.Culture);
        }
        return null;
    }

    /// <summary>Translates line by line, then segment by segment between rich-text tags.</summary>
    private static string? TranslateParts(string core, int depth)
    {
        if (core.IndexOf('\n') >= 0)
        {
            var lines = core.Split('\n');
            var changed = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                var tr = TranslateCore(line, depth + 1, record: true);
                if (!ReferenceEquals(tr, line) && tr != line)
                {
                    lines[i] = lines[i].EndsWith("\r") ? tr + "\r" : tr;
                    changed = true;
                }
            }
            return changed ? string.Join("\n", lines) : null;
        }

        if (core.IndexOf('<') >= 0 && RichTag.IsMatch(core))
        {
            var sb = new StringBuilder(core.Length + 16);
            var changed = false;
            var last = 0;
            foreach (Match tag in RichTag.Matches(core))
            {
                changed |= AppendSegment(sb, core.Substring(last, tag.Index - last), depth);
                sb.Append(tag.Value);
                last = tag.Index + tag.Length;
            }
            changed |= AppendSegment(sb, core.Substring(last), depth);
            return changed ? sb.ToString() : null;
        }
        return null;
    }

    private static bool AppendSegment(StringBuilder sb, string segment, int depth)
    {
        var tr = TranslateCore(segment, depth + 1, record: true);
        sb.Append(tr);
        return tr != segment;
    }

    /// <summary>Values captured by templates (names, items) are translated when a translation exists.</summary>
    private static string TranslateValue(string value) => TranslateCore(value, MaxDepth, record: false);

    public static string NormalizeNumbers(string text, List<string>? found)
    {
        var index = 0;
        return Numbers.Replace(text, m =>
        {
            found?.Add(m.Value);
            return "{" + index++ + "}";
        });
    }

    private static string FillNumbers(string value, List<string> numbers)
    {
        for (var i = 0; i < numbers.Count; i++)
            value = value.Replace("{" + i + "}", numbers[i]);
        return value;
    }

    private static bool HasLetter(string s)
    {
        foreach (var c in s)
            if (char.IsLetter(c))
                return true;
        return false;
    }

    private static bool IsAllCaps(string s)
    {
        var letters = 0;
        foreach (var c in RichTag.Replace(s, ""))
        {
            if (!char.IsLetter(c))
                continue;
            if (char.IsLower(c))
                return false;
            letters++;
        }
        return letters > 1;
    }
}
