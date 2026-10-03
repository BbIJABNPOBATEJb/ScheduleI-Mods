using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using Polyglot.Localization;
using S1Shared;
using UnityEngine;

namespace Polyglot.Text;

/// <summary>
/// Text messages on the phone. A message bubble sizes itself from its text's preferred size, and the
/// conversation stacks the bubbles by their heights — both measured on the English text, while the
/// translation is only put in when the text is drawn. Longer translations then ran out of their
/// bubbles and over the next message. Here the bubble is measured again with the translated text, and
/// an opened conversation is stacked again with the new heights (also after switching languages).
/// </summary>
internal static class MessageBubbleFit
{
    // The game's own layout constants (MessageBubble.RefreshDisplayedText, MSGConversation).
    private const float WidthPadding = 50f;
    private const float HeightPadding = 25f;
    private const float MinHeight = 75f;
    private const float SideMargin = 25f;
    private const float ResponseTop = 25f;
    private const float ResponseOffset = 35f;
    private const float ResponseSpacing = 5f;

    private static bool _failed;

    [HarmonyPatch(typeof(S1.UI.Phone.Messages.MessageBubble), nameof(S1.UI.Phone.Messages.MessageBubble.RefreshDisplayedText))]
    private static class RefreshPatch
    {
        private static void Postfix(S1.UI.Phone.Messages.MessageBubble __instance) => Guard(() => Fit(__instance));
    }

    [HarmonyPatch(typeof(S1.Messaging.MSGConversation), nameof(S1.Messaging.MSGConversation.SetOpen))]
    private static class OpenPatch
    {
        private static void Postfix(S1.Messaging.MSGConversation __instance, bool open)
        {
            if (open)
                Guard(() => Restack(__instance));
        }
    }

    private static void Guard(Action action)
    {
        if (_failed || !Translator.Active)
            return;
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _failed = true;
            MelonLogger.Warning($"Message bubbles could not be fitted to translated text: {ex}");
        }
    }

    /// <summary>Same sizing as the game's, measured on the text that is actually shown.</summary>
    private static void Fit(S1.UI.Phone.Messages.MessageBubble bubble)
    {
        var content = Content(bubble);
        if (content == null)
            return;
        var original = content.text;
        if (string.IsNullOrEmpty(original))
            return;
        var translated = Translator.Translate(original);
        if (translated == original)
            return;

        var rect = UiKit.Get<RectTransform>(bubble);
        if (rect == null)
            return;
        var generator = content.cachedTextGeneratorForLayout;
        var settings = content.GetGenerationSettings(Vector2.zero);
        // Measured at the designed size: shrinking to fit is what the translation must not need.
        settings.resizeTextForBestFit = false;
        var width = generator.GetPreferredWidth(translated, settings) / content.pixelsPerUnit;
        rect.sizeDelta = new Vector2(Mathf.Clamp(width + WidthPadding, bubble.bubble_MinWidth, bubble.bubble_MaxWidth), MinHeight);

        // The text's rectangle follows the bubble's new width.
        settings = content.GetGenerationSettings(new Vector2(content.GetPixelAdjustedRect().size.x, 0f));
        settings.resizeTextForBestFit = false;
        var height = Mathf.Max(generator.GetPreferredHeight(translated, settings) / content.pixelsPerUnit + HeightPadding, MinHeight);
        SetHeight(bubble, height);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);

        if (!bubble.autosetPosition)
            return;
        var side = bubble.alignment switch
        {
            S1.UI.Phone.Messages.MessageBubble.Alignment.Right => -1f,
            S1.UI.Phone.Messages.MessageBubble.Alignment.Center => 0f,
            _ => 1f,
        };
        rect.anchoredPosition = new Vector2((rect.sizeDelta.x / 2f + SideMargin) * side, -height / 2f);
    }

    /// <summary>Positions the conversation's bubbles and reply options the way the game does, with the current heights.</summary>
    private static void Restack(S1.Messaging.MSGConversation conversation)
    {
        var bubbles = Bubbles(conversation);
        var stacked = 0f;
        foreach (var bubble in bubbles)
        {
            if (bubble == null)
                continue;
            var container = bubble.Container;
            container.anchoredPosition = new Vector2(container.anchoredPosition.x, -stacked - bubble.SpacingAbove - bubble.Height / 2f);
            stacked += bubble.SpacingAbove + bubble.Height;
        }
        var bubbleContainer = BubbleContainer(conversation);
        if (bubbleContainer != null && bubbles.Count > 0)
            bubbleContainer.sizeDelta = new Vector2(bubbleContainer.sizeDelta.x, stacked + 50f);

        var responseContainer = ResponseContainer(conversation);
        if (responseContainer == null)
            return;
        var top = ResponseTop;
        var total = 0f;
        var any = false;
        foreach (var rect in ResponseRects(conversation))
        {
            var bubble = rect != null ? UiKit.Get<S1.UI.Phone.Messages.MessageBubble>(rect) : null;
            if (bubble == null)
                continue;
            rect!.anchoredPosition = new Vector2(0f, -top - ResponseOffset);
            total = top + bubble.Height + ResponseTop;
            top += bubble.Height + ResponseSpacing;
            any = true;
        }
        if (!any)
            return;
        responseContainer.sizeDelta = new Vector2(responseContainer.sizeDelta.x, total);
        responseContainer.anchoredPosition = new Vector2(0f, total / 2f);
    }

    // --- non-public members of the game's classes (IL2CPP proxies expose them as properties) ------------

    // The list of a conversation's bubbles is "bubbles" in 0.4.6 and "_bubbles" in 0.4.7.
    private static readonly Func<object, object?>? BubblesGetter = Getter(typeof(S1.Messaging.MSGConversation), "bubbles", "_bubbles");

    private static Func<object, object?>? Getter(Type type, params string[] names)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var name in names)
        {
            var property = type.GetProperty(name, flags);
            if (property != null)
                return property.GetValue;
            var field = type.GetField(name, flags);
            if (field != null)
                return field.GetValue;
        }
        return null;
    }

#if IL2CPP
    internal static List<S1.UI.Phone.Messages.MessageBubble> Bubbles(S1.Messaging.MSGConversation c) =>
        UnityQuery.ToManaged(BubblesGetter?.Invoke(c) as Il2CppSystem.Collections.Generic.List<S1.UI.Phone.Messages.MessageBubble>);

    internal static UnityEngine.UI.Text? Content(S1.UI.Phone.Messages.MessageBubble bubble) => bubble.content;
    private static RectTransform? BubbleContainer(S1.Messaging.MSGConversation c) => c.bubbleContainer;
    private static RectTransform? ResponseContainer(S1.Messaging.MSGConversation c) => c.responseContainer;
    private static List<RectTransform> ResponseRects(S1.Messaging.MSGConversation c) => UnityQuery.ToManaged(c.responseRects);
#else
    internal static List<S1.UI.Phone.Messages.MessageBubble> Bubbles(S1.Messaging.MSGConversation c) =>
        UnityQuery.ToManaged(BubblesGetter?.Invoke(c) as List<S1.UI.Phone.Messages.MessageBubble>);

    private static readonly FieldInfo ContentField = AccessTools.Field(typeof(S1.UI.Phone.Messages.MessageBubble), "content");
    private static readonly FieldInfo BubbleContainerField = AccessTools.Field(typeof(S1.Messaging.MSGConversation), "bubbleContainer");
    private static readonly FieldInfo ResponseContainerField = AccessTools.Field(typeof(S1.Messaging.MSGConversation), "responseContainer");
    private static readonly FieldInfo ResponseRectsField = AccessTools.Field(typeof(S1.Messaging.MSGConversation), "responseRects");

    internal static UnityEngine.UI.Text? Content(S1.UI.Phone.Messages.MessageBubble bubble) => (UnityEngine.UI.Text?)ContentField.GetValue(bubble);
    private static RectTransform? BubbleContainer(S1.Messaging.MSGConversation c) => (RectTransform?)BubbleContainerField.GetValue(c);
    private static RectTransform? ResponseContainer(S1.Messaging.MSGConversation c) => (RectTransform?)ResponseContainerField.GetValue(c);
    private static List<RectTransform> ResponseRects(S1.Messaging.MSGConversation c) =>
        UnityQuery.ToManaged((List<RectTransform>?)ResponseRectsField.GetValue(c));
#endif

    private static readonly MethodInfo? HeightSetter =
        AccessTools.PropertySetter(typeof(S1.UI.Phone.Messages.MessageBubble), nameof(S1.UI.Phone.Messages.MessageBubble.Height));

    private static void SetHeight(S1.UI.Phone.Messages.MessageBubble bubble, float height) =>
        HeightSetter?.Invoke(bubble, new object[] { height });
}
