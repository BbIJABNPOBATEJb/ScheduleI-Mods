using System.Globalization;

namespace Polyglot.Localization;

/// <summary>Writing system; decides which fallback fonts a language needs.</summary>
internal enum Script
{
    Latin,
    Cyrillic,
    Greek,
    ChineseSimplified,
    ChineseTraditional,
    Japanese,
    Korean,
}

internal sealed class LanguageInfo
{
    public string Code { get; }
    public string EnglishName { get; set; }
    public string NativeName { get; set; }
    public Script Script { get; set; }
    public CultureInfo Culture { get; }

    public LanguageInfo(string code, string englishName, string nativeName, Script script)
    {
        Code = code;
        EnglishName = englishName;
        NativeName = nativeName;
        Script = script;
        Culture = TryCulture(code);
    }

    public bool IsSource => Code == LanguageCatalog.SourceCode;

    public override string ToString() => $"{NativeName} ({Code})";

    private static CultureInfo TryCulture(string code)
    {
        try { return CultureInfo.GetCultureInfo(code); }
        catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
    }
}
