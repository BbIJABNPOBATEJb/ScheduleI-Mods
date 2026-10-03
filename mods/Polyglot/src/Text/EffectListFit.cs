using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MelonLoader;
using UnityEngine;

namespace Polyglot.Text;

/// <summary>
/// Lists of product effects ("•  Anti-gravity", one per line) in the product manager, contacts,
/// item tooltips, the mixing station and the handover screen. Translated effect names are often one
/// long word ("Антигравитационный") that no longer fits the line: the label then breaks after the
/// bullet and the list runs into what follows. Such lists stay one line per effect and the font
/// shrinks just enough for the widest line (not below 60%).
/// </summary>
internal static class EffectListFit
{
    private const float MinScale = 0.6f;
    private const int MaxLineLength = 40;

    private static readonly Regex Tags = new("<[^>]*>", RegexOptions.Compiled);
    private static readonly Dictionary<int, int> DesignedSize = new();
    private static readonly List<UnityEngine.UI.Text> Pending = new();
    private static readonly HashSet<int> PendingIds = new();
    private static bool _failed;

    /// <summary>Short bullet lines only: "•  Name" per line, not paragraphs that happen to start with a bullet.</summary>
    public static bool IsEffectList(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('•') < 0)
            return false;
        var any = false;
        foreach (var raw in Tags.Replace(text, "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;
            if (line[0] != '•' || line.Length > MaxLineLength)
                return false;
            any = true;
        }
        return any;
    }

    public static int ExplicitLines(string text) => text.Split('\n').Length;

    /// <summary>Fitted before: it is measured again whenever it is drawn (another product, another language).</summary>
    public static bool Knows(UnityEngine.UI.Text text) => DesignedSize.ContainsKey(text.GetInstanceID());

    /// <summary>A legacy text drew an effect list: measure it after the frame's layout pass (not while drawing).</summary>
    public static void Notice(UnityEngine.UI.Text text)
    {
        if (!_failed && PendingIds.Add(text.GetInstanceID()))
            Pending.Add(text);
    }

    public static void Tick()
    {
        if (_failed || Pending.Count == 0)
            return;
        try
        {
            foreach (var text in Pending)
            {
                if (text != null)
                    Fit(text);
            }
        }
        catch (Exception ex)
        {
            _failed = true;
            MelonLogger.Warning($"Effect lists could not be fitted: {ex}");
        }
        Pending.Clear();
        PendingIds.Clear();
    }

    private static void Fit(UnityEngine.UI.Text text)
    {
        var room = text.rectTransform.rect.width;
        if (room <= 1f)
            return;
        var id = text.GetInstanceID();
        if (!DesignedSize.TryGetValue(id, out var designed))
            DesignedSize[id] = designed = text.fontSize;

        var translated = Localization.Translator.Translate(TextHooks.LegacyOriginal(text));
        var settings = text.GetGenerationSettings(Vector2.zero);
        settings.fontSize = designed;
        settings.resizeTextForBestFit = false;
        settings.horizontalOverflow = HorizontalWrapMode.Overflow;
        var width = text.cachedTextGeneratorForLayout.GetPreferredWidth(translated, settings) / text.pixelsPerUnit;
        var size = width > room
            ? Mathf.Max(Mathf.CeilToInt(designed * MinScale), Mathf.FloorToInt(designed * room / width))
            : designed;
        if (text.fontSize == size && text.resizeTextMaxSize == size)
            return;
        text.fontSize = size;
        // Best fit (turned on for translated texts) must not grow it back to the designed size.
        text.resizeTextMaxSize = size;
        text.resizeTextMinSize = Mathf.Min(text.resizeTextMinSize, size);
    }
}
