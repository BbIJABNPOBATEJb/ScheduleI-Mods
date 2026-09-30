using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;
using MelonLoader.Utils;

namespace Polyglot.Localization;

/// <summary>
/// Known languages and where their translation files come from:
///  1. embedded in the mod DLL (languages/&lt;code&gt;.txt),
///  2. &lt;game&gt;/UserData/Polyglot/languages/&lt;code&gt;/*.txt — user files, override embedded entries.
/// A user folder with an unknown code adds a new language (name taken from the @name directive).
/// </summary>
internal static class LanguageCatalog
{
    public const string SourceCode = "en";

    private static readonly LanguageInfo[] BuiltIn =
    {
        new(SourceCode, "English", "English", Script.Latin),
        new("ru", "Russian", "Русский", Script.Cyrillic),
        new("uk", "Ukrainian", "Українська", Script.Cyrillic),
        new("de", "German", "Deutsch", Script.Latin),
        new("fr", "French", "Français", Script.Latin),
        new("es", "Spanish", "Español", Script.Latin),
        new("pt-BR", "Portuguese (Brazil)", "Português (Brasil)", Script.Latin),
        new("it", "Italian", "Italiano", Script.Latin),
        new("pl", "Polish", "Polski", Script.Latin),
        new("tr", "Turkish", "Türkçe", Script.Latin),
        new("zh-CN", "Chinese (Simplified)", "简体中文", Script.ChineseSimplified),
        new("ja", "Japanese", "日本語", Script.Japanese),
        new("ko", "Korean", "한국어", Script.Korean),
    };

    private static readonly List<LanguageInfo> Installed = new();
    private static readonly Dictionary<string, TranslationTable> Tables = new(StringComparer.OrdinalIgnoreCase);

    public static string UserRoot => Path.Combine(MelonEnvironment.UserDataDirectory, ModInfo.Name);
    public static string UserLanguagesDir => Path.Combine(UserRoot, "languages");

    public static IReadOnlyList<LanguageInfo> Languages => Installed;

    public static void Discover()
    {
        Installed.Clear();
        Tables.Clear();
        Directory.CreateDirectory(UserLanguagesDir);

        var embedded = EmbeddedLanguageCodes();
        var userDirs = Directory.GetDirectories(UserLanguagesDir).Select(Path.GetFileName).Where(n => !string.IsNullOrEmpty(n)).ToList();

        foreach (var lang in BuiltIn)
        {
            if (lang.IsSource || embedded.Contains(lang.Code) || userDirs.Contains(lang.Code, StringComparer.OrdinalIgnoreCase))
                Installed.Add(lang);
        }
        foreach (var dir in userDirs)
        {
            if (Installed.Any(l => l.Code.Equals(dir, StringComparison.OrdinalIgnoreCase)))
                continue;
            Installed.Add(new LanguageInfo(dir!, dir!, dir!, Script.Latin));
        }
    }

    public static LanguageInfo? Find(string code) =>
        Installed.FirstOrDefault(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    public static LanguageInfo Source => Installed[0];

    /// <summary>Loads (once) and returns the translation table of a language.</summary>
    public static TranslationTable GetTable(LanguageInfo lang)
    {
        if (Tables.TryGetValue(lang.Code, out var table))
            return table;

        table = new TranslationTable();
        if (!lang.IsSource)
        {
            using (var stream = OpenEmbedded(lang.Code))
            {
                if (stream != null)
                    TranslationFileParser.Load(stream, $"embedded:{lang.Code}", table, lang);
            }
            var userDir = Path.Combine(UserLanguagesDir, lang.Code);
            if (Directory.Exists(userDir))
            {
                foreach (var file in Directory.GetFiles(userDir, "*.txt", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    using var stream = File.OpenRead(file);
                    TranslationFileParser.Load(stream, file, table, lang);
                }
            }
            table.Seal();
            MelonLogger.Msg($"Loaded {lang}: {table.ExactCount} texts, {table.TemplateCount} templates, {table.RegexCount} regex rules");
        }
        Tables[lang.Code] = table;
        return table;
    }

    private static HashSet<string> EmbeddedLanguageCodes()
    {
        const string prefix = "Polyglot.languages.";
        return new HashSet<string>(
            Assembly.GetExecutingAssembly().GetManifestResourceNames()
                .Where(n => n.StartsWith(prefix) && n.EndsWith(".txt"))
                .Select(n => n.Substring(prefix.Length, n.Length - prefix.Length - 4)),
            StringComparer.OrdinalIgnoreCase);
    }

    private static Stream? OpenEmbedded(string code) =>
        Assembly.GetExecutingAssembly().GetManifestResourceStream($"Polyglot.languages.{code}.txt");
}
