using System;
using System.Collections.Generic;
using MelonLoader;
using S1Shared;
using UnityEngine;
using UnityEngine.Rendering;
#if IL2CPP
using Il2CppEPOOutline;
#else
using EPOOutline;
#endif

namespace GuideArrows.Render;

/// <summary>
/// Makes what the arrows point at glow: an outline in the arrow's color, drawn over everything in
/// front of it (walls, cars, buildings). It uses the outline effect the game itself highlights people
/// and furniture with (Easy Performant Outline): the "single" style is drawn without a depth test.
/// </summary>
internal static class TargetOutlines
{
    private const float FadeTime = 0.3f;
    private const float RefreshInterval = 1f;
    private const string HostName = "GuideArrows Outline";

    private sealed class Glow
    {
        public GameObject Root = null!;
        public GameObject? Host;
        public Outlinable? Outline;
        public int Renderers;
        public int Targets;
        public Color Color;
        public float Alpha;
        public bool Wanted;
        public float CheckAt;
    }

    private static readonly Dictionary<int, Glow> Glows = new();
    private static readonly List<int> Finished = new();
    private static bool _failed;

#if DEV
    /// <summary>Smoke tests: the objects that have an outline right now, and how many meshes it covers.</summary>
    public static IEnumerable<(GameObject Root, int Renderers, float Alpha)> Active
    {
        get
        {
            foreach (var glow in Glows.Values)
            {
                if (glow.Outline != null)
                    yield return (glow.Root, glow.Targets, glow.Alpha);
            }
        }
    }
#endif

    /// <summary>Glows on the given objects, fades out the others. Call every frame.</summary>
    public static void Tick(IEnumerable<(GameObject Root, Color Color)> wanted)
    {
        if (_failed)
            return;
        try
        {
            Update(wanted);
        }
        catch (Exception ex)
        {
            _failed = true;
            MelonLogger.Warning($"Outlines are turned off, they failed: {ex}");
            Clear();
        }
    }

    public static void Clear()
    {
        foreach (var glow in Glows.Values)
            DestroyHost(glow);
        Glows.Clear();
    }

    private static void Update(IEnumerable<(GameObject Root, Color Color)> wanted)
    {
        foreach (var glow in Glows.Values)
            glow.Wanted = false;
        foreach (var (root, color) in wanted)
        {
            if (root == null)
                continue;
            var id = root.GetInstanceID();
            if (!Glows.TryGetValue(id, out var glow))
                Glows[id] = glow = new Glow { Root = root };
            glow.Wanted = true;
            glow.Color = color;
        }

        var now = Time.unscaledTime;
        var step = Time.unscaledDeltaTime / FadeTime;
        Finished.Clear();
        foreach (var pair in Glows)
        {
            var glow = pair.Value;
            if (glow.Root == null)
            {
                DestroyHost(glow); // the person or object is gone
                Finished.Add(pair.Key);
                continue;
            }
            glow.Alpha = Mathf.MoveTowards(glow.Alpha, glow.Wanted ? 1f : 0f, step);
            if (glow.Alpha <= 0f && !glow.Wanted)
            {
                DestroyHost(glow);
                Finished.Add(pair.Key);
                continue;
            }
            // Clothes, held items: rebuild when the object's renderers change.
            if (now >= glow.CheckAt)
            {
                glow.CheckAt = now + RefreshInterval;
                var renderers = Renderers(glow.Root);
                if (renderers.Count != glow.Renderers || (glow.Outline == null && renderers.Count > 0))
                    Build(glow, renderers);
            }
            if (glow.Outline != null)
            {
                var c = glow.Color;
                c.a = glow.Alpha;
                glow.Outline.OutlineParameters.Color = c;
            }
        }
        foreach (var id in Finished)
            Glows.Remove(id);
    }

    private static void Build(Glow glow, List<Renderer> renderers)
    {
        DestroyHost(glow);
        glow.Renderers = renderers.Count;
        glow.Targets = 0;
        if (renderers.Count == 0)
            return;
        // A separate object, not a child of the person: their own code walks their children.
        var host = new GameObject(HostName);
        // The effect skips outlines whose own object is on a layer the camera does not draw.
        host.layer = renderers[0].gameObject.layer;
        var outline = host.AddComponent<Outlinable>();
        outline.RenderStyle = RenderStyle.Single;
        outline.OutlineParameters.DilateShift = 1f;
        outline.OutlineParameters.BlurShift = 0.4f;
        outline.OutlineParameters.Color = Color.clear;
        foreach (var renderer in renderers)
        {
            for (var i = 0; i < SubmeshCount(renderer); i++)
            {
                outline.TryAddTarget(new OutlineTarget(renderer, i));
                glow.Targets++;
            }
        }
        glow.Host = host;
        glow.Outline = outline;
#if DEV
        MelonLogger.Msg($"Outline built on '{glow.Root.name}' ({UnityQuery.PathOf(glow.Root.transform)}): {renderers.Count} renderers, {glow.Targets} targets");
#endif
    }

    private static void DestroyHost(Glow glow)
    {
        if (glow.Host != null)
            UnityEngine.Object.Destroy(glow.Host);
        glow.Host = null;
        glow.Outline = null;
    }

    /// <summary>The visible meshes of the object (not shadow-only ones, not particles or lines).</summary>
    private static List<Renderer> Renderers(GameObject root)
    {
        var list = new List<Renderer>();
        foreach (var renderer in UnityQuery.GetComponentsInChildren<Renderer>(root.transform, false))
        {
            if (renderer == null || !renderer.enabled || renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
                continue;
            if (SubmeshCount(renderer) > 0)
                list.Add(renderer);
        }
        return list;
    }

    /// <summary>
    /// The parts of the mesh this renderer draws: one per material. Static batching merges the meshes of
    /// a whole area into one, so the mesh itself can have thousands of parts that belong to other objects.
    /// </summary>
    private static int SubmeshCount(Renderer renderer)
    {
        Mesh? mesh = null;
        if (UnityQuery.TryCastTo<SkinnedMeshRenderer>(renderer, out var skinned))
            mesh = skinned.sharedMesh;
        else if (UnityQuery.TryCastTo<MeshRenderer>(renderer, out _))
        {
            var filter = UiKit.Get<MeshFilter>(renderer);
            mesh = filter != null ? filter.sharedMesh : null;
        }
        if (mesh == null)
            return 0;
        var materials = renderer.sharedMaterials.Length;
        return renderer.isPartOfStaticBatch ? materials : Mathf.Min(materials, mesh.subMeshCount);
    }
}
