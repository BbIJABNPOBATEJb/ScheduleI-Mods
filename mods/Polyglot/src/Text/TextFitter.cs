using System.Collections.Generic;
using Polyglot.Localization;
using S1Shared;
using UnityEngine;

namespace Polyglot.Text;

/// <summary>
/// Translations are often longer (or wider) than English, and many game labels have fixed-size
/// boxes: text gets truncated ("Марихуа") or a word breaks letter by letter. For translated texts
/// that actually overflow their box, this turns on TMP auto-sizing with the original size as the
/// maximum, so the text shrinks just enough to fit. Boxes too small for a single line are left
/// alone (they overflow by design even in English).
/// </summary>
internal static class TextFitter
{
    private const float MinScale = 0.6f;
    private const int ChecksPerFrame = 40;

    private static readonly List<TextMeshProUGUI> Tracked = new();
    private static readonly HashSet<int> TrackedIds = new();
    private static int _cursor;

    public static int FittedCount { get; private set; }

    public static void Track(TMP_Text text)
    {
        if (!UnityQuery.TryCastTo<TextMeshProUGUI>(text, out var ugui) || !TrackedIds.Add(ugui.GetInstanceID()))
            return;
        Tracked.Add(ugui);
    }

    /// <summary>Checks a slice of the tracked texts each frame.</summary>
    public static void Tick()
    {
        EffectListFit.Tick();
        if (!Translator.Active)
            return;
        for (var n = 0; n < ChecksPerFrame && Tracked.Count > 0; n++)
        {
            if (_cursor >= Tracked.Count)
                _cursor = 0;
            var text = Tracked[_cursor];
            if (text == null)
            {
                Tracked.RemoveAt(_cursor);
                continue;
            }
            _cursor++;
            if (BrokenEffectList(text))
                KeepLines(text);
            else if (NeedsFit(text))
                Fit(text);
        }
    }

    private static bool NeedsFit(TextMeshProUGUI text)
    {
        if (!text.isActiveAndEnabled || text.enableAutoSizing || !(text.isTextOverflowing || text.isTextTruncated))
            return false;
        // Boxes that can't even hold one line (whiteboards, tiny world labels) overflow by design.
        var rect = text.rectTransform.rect;
        if (rect.height < text.fontSize * 0.9f || rect.width < text.fontSize * 2f)
            return false;
        var original = text.text;
        return !string.IsNullOrEmpty(original) && Translator.Translate(original) != original;
    }

    /// <summary>A list of effects where a long name broke onto a second line (see EffectListFit).</summary>
    private static bool BrokenEffectList(TextMeshProUGUI text)
    {
        if (!text.isActiveAndEnabled || text.textWrappingMode == TextWrappingModes.NoWrap || text.textWrappingMode == TextWrappingModes.PreserveWhitespaceNoWrap)
            return false;
        var original = text.text;
        if (string.IsNullOrEmpty(original))
            return false;
        var shown = Translator.Translate(original);
        return shown != original && EffectListFit.IsEffectList(shown) && text.textInfo.lineCount > EffectListFit.ExplicitLines(shown);
    }

    /// <summary>One line per effect, the font shrinking to the widest one.</summary>
    private static void KeepLines(TextMeshProUGUI text)
    {
        text.textWrappingMode = TextWrappingModes.NoWrap;
        if (!text.enableAutoSizing)
            Fit(text);
    }

    private static void Fit(TextMeshProUGUI text)
    {
        var size = text.fontSize;
        text.fontSizeMax = size;
        text.fontSizeMin = Mathf.Max(6f, size * MinScale);
        text.enableAutoSizing = true;
        FittedCount++;
    }
}
