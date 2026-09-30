using HarmonyLib;
using Polyglot.Localization;

namespace Polyglot.Text;

/// <summary>
/// A few UIs rewrite their text before display: hints and phone calls turn &lt;h1&gt;…&lt;/h&gt; into colors and
/// &lt;Input_Jump&gt; into the bound key's name. Those texts are translated before that step, so the
/// translation files can use the same keys the game data has.
/// </summary>
internal static class SourceHooks
{
    [HarmonyPatch(typeof(S1.UI.HintDisplay), "ProcessText")]
    private static class HintPatch
    {
        private static void Prefix(ref string text) => text = Translator.Translate(text);

        private static void Postfix(string __result) => Translator.MarkTranslated(__result);
    }

    [HarmonyPatch(typeof(S1.UI.Phone.CallInterface), "ProcessText")]
    private static class CallPatch
    {
        private static void Prefix(ref string text) => text = Translator.Translate(text);

        private static void Postfix(string __result) => Translator.MarkTranslated(__result);
    }
}
