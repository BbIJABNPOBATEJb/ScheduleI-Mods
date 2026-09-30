#if DEV && MONO
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using S1Shared.Dev;
using UnityEngine;

namespace Polyglot.Dev;

/// <summary>
/// Mono-only: walks every loaded game object/asset with reflection and writes all serialized
/// strings (with their field path) to corpus_objects.jsonl. Source material for translations.
/// </summary>
internal static class CorpusDump
{
    private const int MaxDepth = 6;

    public static int Write(string path)
    {
        var seen = new HashSet<string>();
        var count = 0;
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));

        void Emit(string text, string source, string owner)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            var key = text + "\u0001" + source;
            if (!seen.Add(key))
                return;
            writer.Write("{\"text\":");
            WriteJson(writer, text);
            writer.Write(",\"src\":");
            WriteJson(writer, source);
            writer.Write(",\"obj\":");
            WriteJson(writer, owner);
            writer.Write("}\n");
            count++;
        }

        foreach (var obj in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
        {
            if (obj == null)
                continue;
            var type = obj.GetType();
            switch (obj)
            {
                case TMPro.TMP_Text tmp:
                    Emit(tmp.text, "TMP_Text", PathOf(tmp.transform));
                    continue;
                case UnityEngine.UI.Text legacy:
                    Emit(legacy.text, "UI.Text", PathOf(legacy.transform));
                    continue;
                case TMPro.TMP_Dropdown dropdown:
                    foreach (var option in dropdown.options)
                        Emit(option.text, "TMP_Dropdown.options", PathOf(dropdown.transform));
                    continue;
            }
            if (!IsGameType(type) || !(obj is MonoBehaviour || obj is ScriptableObject))
                continue;
            var owner = obj is Component c ? PathOf(c.transform) : obj.name;
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            Walk(obj, type, type.Name, owner, 0, visited, Emit);
        }
        DevSmoke.Log($"Corpus: {count} strings");
        return count;
    }

    private static void Walk(object instance, Type type, string path, string owner, int depth,
        HashSet<object> visited, Action<string, string, string> emit)
    {
        if (depth > MaxDepth || !visited.Add(instance))
            return;
        for (var t = type; t != null && IsGameType(t); t = t.BaseType)
        {
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                    continue;
                if (field.IsNotSerialized)
                    continue;
                object? value;
                try { value = field.GetValue(instance); }
                catch { continue; }
                VisitValue(value, field.FieldType, path + "." + field.Name, owner, depth, visited, emit);
            }
        }
    }

    private static void VisitValue(object? value, Type declared, string path, string owner, int depth,
        HashSet<object> visited, Action<string, string, string> emit)
    {
        switch (value)
        {
            case null:
                return;
            case string s:
                emit(s, path, owner);
                return;
            case UnityEngine.Object:
                return; // enumerated on its own
            case IList list when !(value is string):
                var elementPath = path + "[]";
                foreach (var item in list)
                    VisitValue(item, item?.GetType() ?? typeof(object), elementPath, owner, depth + 1, visited, emit);
                return;
        }
        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || !IsWalkable(type))
            return;
        Walk(value, type, path, owner, depth + 1, visited, emit);
    }

    private static bool IsWalkable(Type type) =>
        IsGameType(type) && (type.IsSerializable || type.GetCustomAttribute<SerializableAttribute>() != null || type.IsValueType);

    private static bool IsGameType(Type type)
    {
        var asm = type.Assembly.GetName().Name;
        return asm == "Assembly-CSharp" || asm == "ScheduleOne.Core" || asm == "Assembly-CSharp-firstpass";
    }

    private static string PathOf(Transform t)
    {
        var parts = new List<string>();
        for (var cur = t; cur != null; cur = cur.parent)
            parts.Add(cur.name);
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static void WriteJson(TextWriter w, string s)
    {
        w.Write('"');
        foreach (var ch in s)
        {
            switch (ch)
            {
                case '"': w.Write("\\\""); break;
                case '\\': w.Write("\\\\"); break;
                case '\n': w.Write("\\n"); break;
                case '\r': w.Write("\\r"); break;
                case '\t': w.Write("\\t"); break;
                default:
                    if (ch < 0x20) w.Write("\\u" + ((int)ch).ToString("x4"));
                    else w.Write(ch);
                    break;
            }
        }
        w.Write('"');
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
#endif
