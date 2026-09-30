using System;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using Polyglot.Localization;
using S1Shared;
using UnityEngine.UI;
#if IL2CPP
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
#endif

namespace Polyglot.Text;

#if IL2CPP
/// <summary>Managed implementation of TMP's ITextPreprocessor, injected into the IL2CPP domain.</summary>
internal sealed class TranslatingPreprocessor : Il2CppSystem.Object
{
    public TranslatingPreprocessor(IntPtr pointer) : base(pointer) { }

    public TranslatingPreprocessor() : base(ClassInjector.DerivedConstructorPointer<TranslatingPreprocessor>()) =>
        ClassInjector.DerivedConstructorBody(this);

    public string PreprocessText(string text) => TextHooks.Preprocess(text);
}
#else
internal sealed class TranslatingPreprocessor : ITextPreprocessor
{
    public string PreprocessText(string text) => TextHooks.Preprocess(text);
}
#endif

/// <summary>
/// Hooks text rendering. TextMeshPro: every text component gets our ITextPreprocessor, which TMP
/// calls while parsing text for rendering — the component (and the game code reading it) keeps the
/// original English string. Legacy uGUI Text: m_Text is swapped only while the mesh is built.
/// </summary>
internal static class TextHooks
{
    private static ITextPreprocessor _preprocessor = null!;
#if IL2CPP
    private static TranslatingPreprocessor _instance = null!;
#endif
    private static readonly Dictionary<int, bool> InputTextCache = new();
    private static readonly HashSet<int> Excluded = new();

    public static void Init()
    {
#if IL2CPP
        ClassInjector.RegisterTypeInIl2Cpp<TranslatingPreprocessor>(new RegisterTypeOptions
        {
            Interfaces = new[] { typeof(ITextPreprocessor) },
        });
        _instance = new TranslatingPreprocessor();
        _preprocessor = new ITextPreprocessor(_instance.Pointer);
#else
        _preprocessor = new TranslatingPreprocessor();
#endif
        Translator.LanguageChanged += RefreshAll;
    }

    public static string Preprocess(string text)
    {
        try
        {
            return Translator.Translate(text);
        }
        catch (Exception ex)
        {
            MelonLogger.Error(ex);
            return text;
        }
    }

    public static bool IsOurs(ITextPreprocessor? preprocessor)
    {
#if IL2CPP
        return preprocessor != null && preprocessor.Pointer == _instance.Pointer;
#else
        return ReferenceEquals(preprocessor, _preprocessor);
#endif
    }

    public static void Attach(TMP_Text text)
    {
        if (text == null || text.textPreprocessor != null || Excluded.Contains(text.GetInstanceID()) || IsInputText(text))
            return;
        text.textPreprocessor = _preprocessor;
        TextFitter.Track(text);
    }

    /// <summary>Keeps a text out of automatic translation (the mod sets it itself).</summary>
    public static void Exclude(TMP_Text text)
    {
        Excluded.Add(text.GetInstanceID());
        if (IsOurs(text.textPreprocessor))
            text.textPreprocessor = null;
    }

    /// <summary>Re-renders every text so the new language shows up immediately.</summary>
    public static void RefreshAll()
    {
        var active = Translator.Active;
        var count = 0;
        foreach (var text in UnityQuery.FindInScenes<TMP_Text>())
        {
            if (active)
                Attach(text);
            if (!IsOurs(text.textPreprocessor))
                continue;
            text.havePropertiesChanged = false;
            text.havePropertiesChanged = true;
            count++;
        }
        foreach (var text in UnityQuery.FindInScenes<UnityEngine.UI.Text>())
            text.SetAllDirty();
        MelonLogger.Msg($"Refreshed {count} texts");
    }

    private static bool IsInputText(TMP_Text text)
    {
        var id = text.GetInstanceID();
        if (InputTextCache.TryGetValue(id, out var cached))
            return cached;
        var input = UnityQuery.GetComponentInParent<TMP_InputField>(text);
        var result = input != null && input.textComponent == text;
        InputTextCache[id] = result;
        return result;
    }

    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    private static class UguiEnablePatch
    {
        private static void Postfix(TextMeshProUGUI __instance)
        {
            if (Translator.Active)
                Attach(__instance);
        }
    }

    [HarmonyPatch(typeof(TextMeshPro), "OnEnable")]
    private static class WorldEnablePatch
    {
        private static void Postfix(TextMeshPro __instance)
        {
            if (Translator.Active)
                Attach(__instance);
        }
    }

    /// <summary>
    /// Typewriter effects (dialogue, phone calls) reveal up to the English text's length.
    /// Rescale to the translated length so longer translations are not cut off.
    /// </summary>
    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.maxVisibleCharacters), MethodType.Setter)]
    private static class MaxVisiblePatch
    {
        private static void Prefix(TMP_Text __instance, ref int value)
        {
            if (!Translator.Active || value <= 0 || value >= 99999 || !IsOurs(__instance.textPreprocessor))
                return;
            var original = __instance.text;
            if (string.IsNullOrEmpty(original))
                return;
            var translated = Translator.Translate(original);
            if (translated.Length == original.Length)
                return;
            value = value >= original.Length ? 99999 : (int)((long)value * translated.Length / original.Length);
        }
    }

    [HarmonyPatch(typeof(UnityEngine.UI.Text), "OnPopulateMesh", typeof(VertexHelper))]
    private static class LegacyTextPatch
    {
        private static void Prefix(UnityEngine.UI.Text __instance, out string? __state)
        {
            __state = LegacyText.SwapIn(__instance);
            if (__state == null)
                return;
            // Best fit only shrinks when the text doesn't fit, never below 60% of the designed size.
            if (!__instance.resizeTextForBestFit)
            {
                var size = __instance.fontSize;
                __instance.resizeTextMaxSize = size;
                __instance.resizeTextMinSize = Math.Max(6, (int)(size * 0.6f));
                __instance.resizeTextForBestFit = true;
            }
        }

        private static void Finalizer(UnityEngine.UI.Text __instance, string? __state) => LegacyText.SwapOut(__instance, __state);
    }

    /// <summary>Layout (ContentSizeFitter etc.) must measure the translated text, not the English one.</summary>
    [HarmonyPatch(typeof(UnityEngine.UI.Text), nameof(UnityEngine.UI.Text.preferredWidth), MethodType.Getter)]
    private static class LegacyPreferredWidthPatch
    {
        private static void Prefix(UnityEngine.UI.Text __instance, out string? __state) => __state = LegacyText.SwapIn(__instance);
        private static void Finalizer(UnityEngine.UI.Text __instance, string? __state) => LegacyText.SwapOut(__instance, __state);
    }

    [HarmonyPatch(typeof(UnityEngine.UI.Text), nameof(UnityEngine.UI.Text.preferredHeight), MethodType.Getter)]
    private static class LegacyPreferredHeightPatch
    {
        private static void Prefix(UnityEngine.UI.Text __instance, out string? __state) => __state = LegacyText.SwapIn(__instance);
        private static void Finalizer(UnityEngine.UI.Text __instance, string? __state) => LegacyText.SwapOut(__instance, __state);
    }

    private static class LegacyText
    {
        private static readonly Dictionary<int, bool> InputCache = new();
        // Texts currently holding their translation (nested layout calls must not translate it again).
        private static readonly HashSet<int> Swapped = new();

        /// <summary>Puts the translation into m_Text; returns the original to restore, or null.</summary>
        public static string? SwapIn(UnityEngine.UI.Text text)
        {
            if (!Translator.Active)
                return null;
            var id = text.GetInstanceID();
            if (Swapped.Contains(id))
                return null;
            var original = Get(text);
            if (string.IsNullOrEmpty(original) || IsInputText(text))
                return null;
            var translated = Translator.Translate(original);
            if (translated == original)
                return null;
            Swapped.Add(id);
            Set(text, translated);
            return original;
        }

        public static void SwapOut(UnityEngine.UI.Text text, string? original)
        {
            if (original == null)
                return;
            Set(text, original);
            Swapped.Remove(text.GetInstanceID());
        }

#if !IL2CPP
        private static readonly AccessTools.FieldRef<UnityEngine.UI.Text, string> Field =
            AccessTools.FieldRefAccess<UnityEngine.UI.Text, string>("m_Text");
#endif

        public static string Get(UnityEngine.UI.Text text)
        {
#if IL2CPP
            return text.m_Text;
#else
            return Field(text);
#endif
        }

        public static void Set(UnityEngine.UI.Text text, string value)
        {
#if IL2CPP
            text.m_Text = value;
#else
            Field(text) = value;
#endif
        }

        public static bool IsInputText(UnityEngine.UI.Text text)
        {
            var id = text.GetInstanceID();
            if (InputCache.TryGetValue(id, out var cached))
                return cached;
            var input = UnityQuery.GetComponentInParent<InputField>(text);
            var result = input != null && input.textComponent == text;
            InputCache[id] = result;
            return result;
        }
    }
}
