using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Polyglot.Localization;

/// <summary>
/// Hints and phone calls write &lt;h1&gt;word&lt;/h&gt; in their data and swap it for a color just before
/// display. SourceHooks translate before that swap; this is the fallback for text that only reaches
/// Polyglot with the colors already in (e.g. when the game's text processing is inlined natively):
/// the known highlight colors are turned back into &lt;h1&gt;/&lt;h2&gt;/&lt;h3&gt; for the lookup.
/// </summary>
internal static class HighlightTags
{
    private static readonly Regex ColorTag = new(@"<color=#([0-9A-Fa-f]{6})(?:[0-9A-Fa-f]{2})?>", RegexOptions.Compiled);

    // HintDisplay.ProcessText; the phone call color is read from CallInterface once it exists.
    private static readonly Dictionary<string, string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["88CBFF"] = "h1",
        ["F86266"] = "h2",
        ["46CB4F"] = "h3",
    };

    private static bool _callColorLearned;

    public static bool TryToTags(string text, out string tagged, out Dictionary<string, string> colors)
    {
        LearnCallColor();
        var found = new Dictionary<string, string>();
        var unknown = false;
        tagged = ColorTag.Replace(text, m =>
        {
            if (!Known.TryGetValue(m.Groups[1].Value, out var tag))
            {
                unknown = true;
                return m.Value;
            }
            found[tag] = m.Value;
            return "<" + tag + ">";
        });
        colors = found;
        if (unknown || found.Count == 0)
            return false;
        tagged = tagged.Replace("</color>", "</h>");
        return true;
    }

    public static string FromTags(string text, Dictionary<string, string> colors)
    {
        foreach (var pair in colors)
            text = text.Replace("<" + pair.Key + ">", pair.Value);
        return text.Replace("</h>", "</color>");
    }

    private static void LearnCallColor()
    {
        if (_callColorLearned)
            return;
        try
        {
            var call = S1.UI.Phone.CallInterface.Instance;
            if (call == null)
                return;
            _callColorLearned = true;
            Known[ColorUtility.ToHtmlStringRGB(call.Highlight1Color)] = "h1";
        }
        catch (Exception)
        {
            _callColorLearned = true;
        }
    }
}
