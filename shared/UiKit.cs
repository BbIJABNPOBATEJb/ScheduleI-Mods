using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if IL2CPP
using Il2CppInterop.Runtime;
#endif

namespace S1Shared;

/// <summary>Cross-runtime helpers for wiring and cloning uGUI objects.</summary>
internal static class UiKit
{
    /// <summary>Replaces all (incl. serialized) click handlers with <paramref name="action"/>.</summary>
    public static void OnClick(Button button, Action action)
    {
        button.onClick = new Button.ButtonClickedEvent();
#if IL2CPP
        button.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(action));
#else
        button.onClick.AddListener(new UnityAction(action));
#endif
    }

    /// <summary>Replaces all (incl. serialized) value handlers with <paramref name="action"/>.</summary>
    public static void OnValueChanged(Slider slider, Action<float> action)
    {
        slider.onValueChanged = new Slider.SliderEvent();
#if IL2CPP
        slider.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<float>>(action));
#else
        slider.onValueChanged.AddListener(new UnityAction<float>(action));
#endif
    }

    public static T? Get<T>(Component c) where T : Component
    {
#if IL2CPP
        return c.GetComponent(Il2CppType.Of<T>())?.TryCast<T>();
#else
        return c.GetComponent<T>();
#endif
    }

    public static T? Get<T>(GameObject go) where T : Component => Get<T>(go.transform);

    public static T? InChildren<T>(Component c) where T : Component =>
        UnityQuery.GetComponentsInChildren<T>(c, true).FirstOrDefault();

    /// <summary>Depth-first search for a descendant by name.</summary>
    public static Transform? Find(Transform root, string name)
    {
        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.name == name)
                return child;
            var deeper = Find(child, name);
            if (deeper != null)
                return deeper;
        }
        return null;
    }

    public static List<Transform> Children(Transform t)
    {
        var list = new List<Transform>();
        for (var i = 0; i < t.childCount; i++)
            list.Add(t.GetChild(i));
        return list;
    }

    public static string Namespace(Component c)
    {
#if IL2CPP
        return c.GetIl2CppType().Namespace ?? "";
#else
        return c.GetType().Namespace ?? "";
#endif
    }

    /// <summary>Sets a (possibly non-public) field on a game object; IL2CPP proxies expose it as a property.</summary>
    public static void SetMember(object target, string name, object? value)
    {
        var type = target.GetType();
        var property = AccessTools.Property(type, name);
        if (property != null && property.CanWrite)
        {
            property.SetValue(target, value);
            return;
        }
        AccessTools.Field(type, name)?.SetValue(target, value);
    }
}
