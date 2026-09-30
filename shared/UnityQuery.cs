using System.Collections.Generic;
using UnityEngine;
#if IL2CPP
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
#endif

namespace S1Shared;

internal static class UnityQuery
{
    /// <summary>A managed copy of a game list (IL2CPP lists don't support LINQ).</summary>
#if IL2CPP
    public static List<T> ToManaged<T>(Il2CppSystem.Collections.Generic.List<T>? list)
    {
        var result = new List<T>();
        if (list == null)
            return result;
        for (var i = 0; i < list.Count; i++)
            result.Add(list[i]);
        return result;
    }
#else
    public static List<T> ToManaged<T>(List<T>? list) => list == null ? new List<T>() : new List<T>(list);
#endif

    /// <summary>All loaded objects of type T, including inactive ones and prefabs held in memory.</summary>
    public static List<T> FindAllIncludingAssets<T>() where T : Object
    {
        var result = new List<T>();
#if IL2CPP
        foreach (var obj in Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>()))
        {
            var cast = obj?.TryCast<T>();
            if (cast != null)
                result.Add(cast);
        }
#else
        result.AddRange(Resources.FindObjectsOfTypeAll<T>());
#endif
        return result;
    }

    /// <summary>Scene objects of type T (active and inactive), excluding prefab assets.</summary>
    public static List<T> FindInScenes<T>() where T : Component
    {
        var result = new List<T>();
        foreach (var c in FindAllIncludingAssets<T>())
        {
            if (c.gameObject.scene.IsValid())
                result.Add(c);
        }
        return result;
    }

    public static T? GetComponentInParent<T>(Component component) where T : Component
    {
#if IL2CPP
        return component.GetComponentInParent(Il2CppType.Of<T>())?.TryCast<T>();
#else
        return component.GetComponentInParent<T>();
#endif
    }

    public static List<T> GetComponentsInChildren<T>(Component root, bool includeInactive) where T : Component
    {
        var result = new List<T>();
#if IL2CPP
        foreach (var c in root.GetComponentsInChildren(Il2CppType.Of<T>(), includeInactive))
        {
            var cast = c?.TryCast<T>();
            if (cast != null)
                result.Add(cast);
        }
#else
        result.AddRange(root.GetComponentsInChildren<T>(includeInactive));
#endif
        return result;
    }

    public static bool TryCastTo<T>(Object? obj, out T result) where T : Object
    {
#if IL2CPP
        result = obj?.TryCast<T>()!;
#else
        result = (obj as T)!;
#endif
        return result != null;
    }

    public static string TypeName(Object obj)
    {
#if IL2CPP
        return obj.GetIl2CppType().Name;
#else
        return obj.GetType().Name;
#endif
    }

    /// <summary>Indented dump of a hierarchy: names, active state, component types and texts.</summary>
    public static string DumpHierarchy(Transform root, int maxDepth = 12)
    {
        var sb = new System.Text.StringBuilder();
        void Walk(Transform t, int depth)
        {
            sb.Append(' ', depth * 2).Append(t.name).Append(t.gameObject.activeSelf ? "" : " [inactive]").Append("  {");
            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null) continue;
                sb.Append(TypeName(c)).Append(' ');
                if (TryCastTo<TMP_Text>(c, out var tmp))
                    sb.Append("\"").Append(tmp.text?.Replace("\n", "\\n")).Append("\" ");
            }
            sb.Append("}\n");
            if (depth >= maxDepth) return;
            for (var i = 0; i < t.childCount; i++)
                Walk(t.GetChild(i), depth + 1);
        }
        Walk(root, 0);
        return sb.ToString();
    }

    public static string PathOf(Transform t)
    {
        var parts = new List<string>();
        for (var cur = t; cur != null; cur = cur.parent)
            parts.Add(cur.name);
        parts.Reverse();
        return string.Join("/", parts);
    }
}
