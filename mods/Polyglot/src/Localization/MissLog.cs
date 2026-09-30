using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Polyglot.Localization;

/// <summary>
/// Collects texts that had no translation (numbers normalized to {0}, {1}...) and appends them to
/// UserData/Polyglot/untranslated_&lt;code&gt;.txt in the translation file format, ready to fill in.
/// </summary>
internal static class MissLog
{
    private static readonly HashSet<string> Seen = new(StringComparer.Ordinal);
    private static readonly List<string> Pending = new();
    private static string? _path;

    public static bool Enabled { get; set; }
    public static int Count => Seen.Count;

    public static void SetLanguage(LanguageInfo lang)
    {
        Flush();
        Seen.Clear();
        _path = lang.IsSource ? null : Path.Combine(LanguageCatalog.UserRoot, $"untranslated_{lang.Code}.txt");
        if (_path != null && File.Exists(_path))
        {
            foreach (var line in File.ReadAllLines(_path, Encoding.UTF8))
            {
                var sep = line.LastIndexOf('=');
                if (sep > 0)
                    Seen.Add(TranslationFileParser.Unescape(line.Substring(0, sep)));
            }
        }
    }

    public static void Record(string text)
    {
        if (!Enabled || _path == null || text.Length > 2000 || !Seen.Add(text))
            return;
        Pending.Add(TranslationFileParser.Escape(text, isKey: true) + "=");
    }

    public static void Flush()
    {
        if (Pending.Count == 0 || _path == null)
            return;
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.AppendAllLines(_path, Pending, new UTF8Encoding(false));
        Pending.Clear();
    }
}
