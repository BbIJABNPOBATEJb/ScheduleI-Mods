using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MelonLoader;

namespace Polyglot.Localization;

/// <summary>
/// Translation file format (UTF-8, one entry per line):
/// <code>
/// // comment
/// @name=Русский            directive: native name (also @english=..., @script=Cyrillic)
/// Continue=Продолжить      exact text
/// Day {0}=День {0}          {0},{1}... stand for numbers found in the text
/// It'll cost &lt;PRICE&gt;.=Это будет стоить &lt;PRICE&gt;.   game placeholders are matched and re-inserted
/// r:"^Sold (.+) to (.+)$"=Продано: $1 → $2              regex; captured groups get translated too
/// </code>
/// Escapes: \n \r \t \\ and \= (a literal '=' inside the key).
/// </summary>
internal static class TranslationFileParser
{
    public static void Load(Stream stream, string source, TranslationTable table, LanguageInfo lang)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var lineNo = 0;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lineNo++;
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                continue;
            try
            {
                if (line[0] == '@')
                    ApplyDirective(line, lang);
                else if (line.StartsWith("r:\"", StringComparison.Ordinal))
                    ParseRegex(line, table);
                else
                    ParseEntry(line, table);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"{source}:{lineNo}: {ex.Message}");
            }
        }
    }

    private static void ApplyDirective(string line, LanguageInfo lang)
    {
        var eq = line.IndexOf('=');
        if (eq < 0)
            return;
        var name = line.Substring(1, eq - 1).Trim().ToLowerInvariant();
        var value = line.Substring(eq + 1).Trim();
        switch (name)
        {
            case "name": lang.NativeName = value; break;
            case "english": lang.EnglishName = value; break;
            case "script" when Enum.TryParse<Script>(value, true, out var script): lang.Script = script; break;
        }
    }

    private static void ParseEntry(string line, TranslationTable table)
    {
        var sep = FindSeparator(line, 0);
        if (sep <= 0)
            throw new FormatException("expected key=value");
        table.Add(Unescape(line.Substring(0, sep)), Unescape(line.Substring(sep + 1)));
    }

    private static void ParseRegex(string line, TranslationTable table)
    {
        // r:"pattern"=replacement   (the pattern ends at the first `"=` sequence)
        var end = line.IndexOf("\"=", 3, StringComparison.Ordinal);
        if (end < 0)
            throw new FormatException("expected r:\"pattern\"=replacement");
        var pattern = line.Substring(3, end - 3);
        var replacement = Unescape(line.Substring(end + 2));
        table.AddRegex(new Regex(pattern, RegexOptions.Singleline | RegexOptions.CultureInvariant), replacement);
    }

    private static int FindSeparator(string line, int start)
    {
        for (var i = start; i < line.Length; i++)
        {
            if (line[i] == '\\')
                i++;
            else if (line[i] == '=')
                return i;
        }
        return -1;
    }

    public static string Unescape(string s)
    {
        if (s.IndexOf('\\') < 0)
            return s;
        var sb = new StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c != '\\' || i == s.Length - 1)
            {
                sb.Append(c);
                continue;
            }
            var next = s[++i];
            sb.Append(next switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                _ => next,
            });
        }
        return sb.ToString();
    }

    public static string Escape(string s, bool isKey)
    {
        var sb = new StringBuilder(s.Length + 8);
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '=' when isKey: sb.Append("\\="); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
