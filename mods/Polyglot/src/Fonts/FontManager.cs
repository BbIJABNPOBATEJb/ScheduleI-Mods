using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;
using Polyglot.Localization;
using S1Shared;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
#if IL2CPP
using FontAssetList = Il2CppSystem.Collections.Generic.List<Il2CppTMPro.TMP_FontAsset>;
#else
using FontAssetList = System.Collections.Generic.List<TMPro.TMP_FontAsset>;
#endif

namespace Polyglot.Fonts;

/// <summary>
/// The game's TMP font atlases (Open Sans, Caveat, Handlee, ...) only contain Latin glyphs.
/// For every game font this adds a matching-weight fallback built at runtime from the embedded
/// Open Sans / Caveat files (Latin Extended, Cyrillic, Greek, Vietnamese), so translated text keeps
/// the game's look. CJK languages additionally get a system font as a global fallback.
/// </summary>
internal static class FontManager
{
    private const string OurPrefix = "Polyglot ";

    // Game font asset name prefix → embedded font. More specific prefixes first.
    private static readonly (string GamePrefix, string File)[] StyleMap =
    {
        ("OpenSans-SemiBoldItalic", "OpenSans-SemiBoldItalic"),
        ("OpenSans-MediumItalic", "OpenSans-MediumItalic"),
        ("OpenSans-BoldItalic", "OpenSans-BoldItalic"),
        ("OpenSans-SemiBold", "OpenSans-SemiBold"),
        ("OpenSans-Medium", "OpenSans-Medium"),
        ("OpenSans-Bold", "OpenSans-Bold"),
        ("OpenSans-Light", "OpenSans-Light"),
        ("Caveat", "Caveat-Regular"),
        ("Handlee", "Caveat-Regular"),
    };
    private const string DefaultStyle = "OpenSans-Regular";

    // Windows font files per script, first existing one wins (faceIndex 0 of .ttc collections).
    private static readonly Dictionary<Script, string[]> SystemFonts = new()
    {
        [Script.ChineseSimplified] = new[] { "msyh.ttc", "msyh.ttf", "Deng.ttf", "simhei.ttf", "simsun.ttc" },
        [Script.ChineseTraditional] = new[] { "msjh.ttc", "msjh.ttf", "mingliu.ttc" },
        [Script.Japanese] = new[] { "YuGothM.ttc", "meiryo.ttc", "msgothic.ttc" },
        [Script.Korean] = new[] { "malgun.ttf", "gulim.ttc" },
    };
    private static readonly Dictionary<Script, string[]> SystemFamilies = new()
    {
        [Script.ChineseSimplified] = new[] { "Noto Sans CJK SC", "Source Han Sans SC", "WenQuanYi Micro Hei", "Droid Sans Fallback" },
        [Script.ChineseTraditional] = new[] { "Noto Sans CJK TC", "Source Han Sans TC", "Droid Sans Fallback" },
        [Script.Japanese] = new[] { "Noto Sans CJK JP", "Source Han Sans JP", "TakaoGothic", "Droid Sans Fallback" },
        [Script.Korean] = new[] { "Noto Sans CJK KR", "Source Han Sans KR", "NanumGothic", "Droid Sans Fallback" },
    };

    private static readonly Dictionary<string, TMP_FontAsset?> Created = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<int> Patched = new();
    private static string FontsDir => Path.Combine(LanguageCatalog.UserRoot, "fonts");

    /// <summary>Attaches fallbacks to every loaded game font. Cheap to call again (e.g. on scene load).</summary>
    public static void EnsureFallbacks(LanguageInfo language)
    {
        if (language.IsSource)
            return;

        foreach (var font in UnityQuery.FindAllIncludingAssets<TMP_FontAsset>())
        {
            if (font.name.StartsWith(OurPrefix, StringComparison.Ordinal) || !Patched.Add(font.GetInstanceID()))
                continue;
            var fallback = GetEmbedded(StyleFor(font.name));
            if (fallback != null)
                InsertFallback(font, fallback);
        }

        var global = GetSystemFallback(language.Script);
        if (global != null)
            AddGlobalFallback(global);
    }

    public static string Describe()
    {
        var fonts = UnityQuery.FindAllIncludingAssets<TMP_FontAsset>();
        return string.Join("\n", fonts.Select(f =>
            $"  {f.name}: mode={f.atlasPopulationMode} chars={f.characterTable?.Count} fallbacks={f.fallbackFontAssetTable?.Count} " +
            $"hasЖ={f.HasCharacter('Ж', false, false)} hasé={f.HasCharacter('é', false, false)}"));
    }

    private static string StyleFor(string gameFontName)
    {
        foreach (var (prefix, file) in StyleMap)
            if (gameFontName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return file;
        return DefaultStyle;
    }

    private static void InsertFallback(TMP_FontAsset font, TMP_FontAsset fallback)
    {
        var table = font.fallbackFontAssetTable;
        if (table == null)
        {
            table = new FontAssetList();
            font.fallbackFontAssetTable = table;
        }
        for (var i = 0; i < table.Count; i++)
            if (table[i] == fallback)
                return;
        table.Insert(0, fallback);
    }

    private static void AddGlobalFallback(TMP_FontAsset fallback)
    {
        var list = TMP_Settings.fallbackFontAssets;
        if (list == null)
            return;
        for (var i = 0; i < list.Count; i++)
            if (list[i] == fallback)
                return;
        list.Add(fallback);
    }

    private static TMP_FontAsset? GetEmbedded(string style)
    {
        if (Created.TryGetValue(style, out var asset))
            return asset;
        asset = null;
        try
        {
            var path = ExtractEmbeddedFont(style + ".ttf");
            asset = path != null ? CreateFromFile(path, style) : null;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"Font {style} failed: {ex.Message}");
        }
        Created[style] = asset;
        return asset;
    }

    private static TMP_FontAsset? GetSystemFallback(Script script)
    {
        var custom = PolyglotConfig.CustomFontPath.Value;
        var needsSystemFont = SystemFonts.ContainsKey(script);
        if (!needsSystemFont && string.IsNullOrWhiteSpace(custom))
            return null;

        var key = "system:" + script;
        if (Created.TryGetValue(key, out var asset))
            return asset;

        asset = null;
        if (!string.IsNullOrWhiteSpace(custom) && File.Exists(custom))
            asset = CreateFromFile(custom, Path.GetFileNameWithoutExtension(custom));

        if (asset == null && needsSystemFont)
        {
            var windowsFonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            var userFonts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");
            foreach (var file in SystemFonts[script])
            {
                var path = new[] { windowsFonts, userFonts }.Select(d => Path.Combine(d, file)).FirstOrDefault(File.Exists);
                if (path != null && (asset = CreateFromFile(path, file)) != null)
                    break;
            }
            if (asset == null)
            {
                foreach (var family in SystemFamilies[script])
                {
                    asset = TMP_FontAsset.CreateFontAsset(family, "Regular", 90);
                    if (asset != null)
                    {
                        Keep(asset, family);
                        break;
                    }
                }
            }
            if (asset == null)
                MelonLogger.Warning($"No system font found for {script}. Set CustomFontPath in MelonPreferences.cfg.");
        }
        Created[key] = asset;
        return asset;
    }

    private static TMP_FontAsset? CreateFromFile(string path, string name)
    {
        var asset = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
        if (asset == null)
        {
            MelonLogger.Warning($"Could not load font {path}");
            return null;
        }
        Keep(asset, name);
        MelonLogger.Msg($"Font ready: {asset.name}");
        return asset;
    }

    private static void Keep(TMP_FontAsset asset, string name)
    {
        asset.name = OurPrefix + name;
        asset.hideFlags = HideFlags.HideAndDontSave;
        if (asset.material != null)
            asset.material.hideFlags = HideFlags.HideAndDontSave;
        var atlases = asset.atlasTextures;
        if (atlases != null)
            foreach (var tex in atlases)
                if (tex != null)
                    tex.hideFlags = HideFlags.HideAndDontSave;
    }

    private static string? ExtractEmbeddedFont(string fileName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Polyglot.fonts." + fileName);
        if (stream == null)
            return null;
        Directory.CreateDirectory(FontsDir);
        var path = Path.Combine(FontsDir, fileName);
        if (!File.Exists(path) || new FileInfo(path).Length != stream.Length)
        {
            using var file = File.Create(path);
            stream.CopyTo(file);
        }
        return path;
    }
}
