using System;
using System.Collections.Generic;
using MelonLoader;
using S1Shared;
using UnityEngine;

namespace ElectricScooter;

/// <summary>
/// The scooter's looks, built in code on top of the Golden Skateboard's model: the skateboard trucks
/// are swapped for two big wheels at the ends of the deck, and a steering column with a handlebar is
/// added at the front. Used for the board being ridden and for the one held in the hands.
/// </summary>
internal static class ScooterModel
{
    private const string PartsName = "ElectricScooter Parts";

    /// <summary>Wheel centres, metres in front of / behind the middle of the deck.</summary>
    private const float WheelOffset = 0.52f;
    private const float WheelWidth = 0.055f;
    private const float HandlebarHeight = 0.98f;
    private const float HandlebarWidth = 0.5f;
    /// <summary>How far behind the front axle the handlebar is.</summary>
    private const float HandlebarRake = 0.2f;

    private static Mesh? _cylinder;
    private static Mesh? _box;
    private static Material? _gold, _dark, _tyre, _steel;
    private static bool _failed;

    /// <summary>Dresses the board the player rides.</summary>
    public static void Dress(S1.Skating.Skateboard board)
    {
        var visuals = UiKit.Get<S1.Skating.SkateboardVisuals>(board);
        var truck = FindTruck(board.transform);
        if (visuals == null || visuals.Board == null || truck == null)
            return;
        // The parts lean with the deck but are laid out in the trucks' frame: x right, y up, z forward.
        var hover = ScooterRide.HoverHeight;
        _ridden = Build(visuals.Board, truck.parent, truck, hover, folded: false);
        _riddenAnimation = board.Animation;
        if (_ridden == null)
            return;
        // The speed trails of the skateboard's rear wheels move down to where the scooter's wheel meets the road.
        var effects = UiKit.Get<S1.Skating.SkateboardEffects>(board);
        if (effects == null || effects.Trails == null)
            return;
        for (var i = 0; i < effects.Trails.Length; i++)
        {
            var trail = effects.Trails[i];
            if (trail == null)
                continue;
            var side = i % 2 == 0 ? -0.02f : 0.02f;
            trail.transform.position = _ridden.TransformPoint(new Vector3(side, Ground(hover) + 0.015f, -WheelOffset));
            trail.Clear();
        }
    }

    private static Transform? _ridden;
    private static S1.Skating.SkateboardAnimation? _riddenAnimation;

    /// <summary>Dresses the board held in the hands (first-person view model): the scooter is carried folded.</summary>
    public static void DressHeld(S1.Skating.Skateboard_Equippable equippable)
    {
        var truck = FindTruck(equippable.transform);
        if (truck != null)
            Build(truck.parent, truck.parent, truck, ScooterRide.HoverHeight, folded: true);
    }

    public static bool IsDressed(Transform root) => FindParts(root) != null;

    private static Transform? FindParts(Transform root)
    {
        foreach (var t in UnityQuery.GetComponentsInChildren<Transform>(root, true))
            if (t.name == PartsName && Owns(root, t))
                return t;
        return null;
    }

    /// <summary>
    /// The rider — with the board he holds in his hands — is parented into the board he rides, so a
    /// search under the ridden board also finds the held one. This tells them apart.
    /// </summary>
    private static bool Owns(Transform root, Transform descendant) =>
        UnityQuery.GetComponentInParent<S1.PlayerScripts.Player>(descendant) == null
        || UnityQuery.GetComponentInParent<S1.PlayerScripts.Player>(root) != null;

    private static Transform? FindTruck(Transform root)
    {
        foreach (var renderer in UnityQuery.GetComponentsInChildren<MeshRenderer>(root, false))
        {
            if (!renderer.name.StartsWith("Truck", StringComparison.Ordinal) || UiKit.Find(renderer.transform, "Wheel") == null)
                continue;
            if (Owns(root, renderer.transform))
                return renderer.transform;
        }
        return null;
    }

    // The layout, in the board's frame (x right, y up, z forward; the deck's top is at y = 0).

    /// <summary>The road under the hovering board.</summary>
    private static float Ground(float hover) => -(hover - 0.012f);

    /// <summary>The wheels fill the space between the road and the deck.</summary>
    private static float Radius(float hover) => Mathf.Min((hover - 0.02f) / 2f, 0.16f);

    /// <summary>Where the front fork meets the steering column, just above the front wheel.</summary>
    private static Vector3 Crown(float hover) => new(0f, Ground(hover) + Radius(hover) * 2f + 0.035f, WheelOffset - 0.035f);

    private static Vector3 HandlebarCentre(Vector3 crown, bool folded) => folded
        // Folded, the column lies back along the deck.
        ? new Vector3(0f, crown.y + 0.05f, crown.z - (HandlebarHeight - crown.y))
        : new Vector3(0f, HandlebarHeight, WheelOffset - HandlebarRake);

    private static Transform? Build(Transform parent, Transform frame, Transform truck, float hoverHeight, bool folded)
    {
        if (_failed)
            return null;
        try
        {
            var layer = truck.gameObject.layer;
            EnsureAssets(truck);

            var parts = new GameObject(PartsName).transform;
            parts.SetParent(parent, false);
            parts.position = frame.position;
            parts.rotation = frame.rotation;

            // Skateboard trucks and their little wheels go away.
            for (var i = 0; i < frame.childCount; i++)
            {
                var child = frame.GetChild(i);
                if (child.name.StartsWith("Truck", StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
            }

            var radius = Radius(hoverHeight);
            var axle = Ground(hoverHeight) + radius;
            var wheelTop = axle + radius;
            const float nose = 0.40f;
            const float deckLevel = 0.004f;

            foreach (var z in new[] { WheelOffset, -WheelOffset })
            {
                var centre = new Vector3(0f, axle, z);
                var half = Vector3.right * (WheelWidth / 2f);
                Rod(parts, "Tyre", centre - half, centre + half, radius, _tyre!, layer);
                Rod(parts, "Rim", centre - half * 1.08f, centre + half * 1.08f, radius * 0.56f, _steel!, layer);
                Rod(parts, "Hub", centre - half * 1.3f, centre + half * 1.3f, radius * 0.17f, _gold!, layer);
                // Fork blades on both sides of the wheel, from the axle up to the deck's level.
                foreach (var x in new[] { -0.042f, 0.042f })
                    Rod(parts, "Fork", new Vector3(x, axle, z), new Vector3(x, wheelTop + 0.035f, z - Mathf.Sign(z) * 0.035f), 0.011f, _dark!, layer);
            }

            // Neck from the deck's nose to the fork crown, crown bar, and the steering column.
            var crown = Crown(hoverHeight);
            Rod(parts, "Neck", new Vector3(0f, deckLevel, nose - 0.08f), crown, 0.024f, _gold!, layer);
            Rod(parts, "Crown", crown + Vector3.left * 0.05f, crown + Vector3.right * 0.05f, 0.016f, _dark!, layer);
            var top = HandlebarCentre(crown, folded);
            Rod(parts, "Column", crown, top, 0.019f, _gold!, layer);
            Rod(parts, "Handlebar", top + Vector3.left * (HandlebarWidth / 2f), top + Vector3.right * (HandlebarWidth / 2f), 0.013f, _dark!, layer);
            foreach (var side in new[] { -1f, 1f })
                Rod(parts, "Grip", top + Vector3.right * side * (HandlebarWidth / 2f - 0.11f), top + Vector3.right * side * (HandlebarWidth / 2f + 0.005f),
                    0.019f, _tyre!, layer);
            if (!folded)
            {
                // Headlight on the column.
                var lamp = Vector3.Lerp(crown, top, 0.78f);
                Rod(parts, "Headlight", lamp + new Vector3(0f, 0f, 0.015f), lamp + new Vector3(0f, -0.004f, 0.06f), 0.026f, _dark!, layer);
            }

            // Rear: fender over the wheel, held by the tail of the deck.
            var rearCrown = new Vector3(0f, wheelTop + 0.035f, -WheelOffset + 0.035f);
            Rod(parts, "Tail", new Vector3(0f, deckLevel, -nose + 0.08f), rearCrown, 0.02f, _gold!, layer);
            Rod(parts, "Rear Crown", rearCrown + Vector3.left * 0.05f, rearCrown + Vector3.right * 0.05f, 0.016f, _dark!, layer);
            Box(parts, "Fender", new Vector3(0f, wheelTop + 0.03f, -WheelOffset - 0.03f), new Vector3(0.07f, 0.01f, 0.2f), Quaternion.Euler(-14f, 0f, 0f), _dark!, layer);

            // Battery pack under the deck.
            Box(parts, "Battery", new Vector3(0f, -0.035f, 0f), new Vector3(0.15f, 0.045f, 0.5f), Quaternion.identity, _dark!, layer);
            return parts;
        }
        catch (Exception ex)
        {
            _failed = true;
            MelonLogger.Warning($"Electric Scooter model could not be built (the scooter still works): {ex}");
            return null;
        }
    }

    /// <summary>
    /// Keeps the rider's hands on the grips; call every frame while riding. The game places each hand
    /// between a "lowered" and a "raised" pose stored on the board, relative to the rider's moving
    /// torso; both poses are kept on the grip, also during the push animation.
    /// </summary>
    public static void HoldHandlebar()
    {
        if (_handsFailed || _ridden == null || _riddenAnimation == null)
            return;
        try
        {
            var animation = _riddenAnimation;
            if (animation.HandContainer == null)
                return;
            var top = HandlebarCentre(Crown(ScooterRide.HoverHeight), false) + Vector3.up * 0.025f;
            var reach = Vector3.right * (HandlebarWidth / 2f - 0.05f);
            Place(animation, _ridden.TransformPoint(top - reach),
                animation.LeftHandAlignment, animation.LeftHandLoweredAlignment, animation.LeftHandRaisedAlignment);
            Place(animation, _ridden.TransformPoint(top + reach),
                animation.RightHandAlignment, animation.RightHandLoweredAlignment, animation.RightHandRaisedAlignment);
        }
        catch (Exception ex)
        {
            _handsFailed = true;
            MelonLogger.Warning($"Electric Scooter: hands could not be put on the handlebar: {ex.Message}");
        }
    }

    private static bool _handsFailed;

    private static void Place(S1.Skating.SkateboardAnimation animation, Vector3 grip, params S1.Skating.SkateboardAnimation.AlignmentSet[] sets)
    {
        var local = animation.HandContainer.InverseTransformPoint(grip);
        foreach (var set in sets)
        {
            if (set == null || set.Default == null)
                continue;
            set.Default.localPosition = local;
            set.Animated = set.Default;
            if (set.Transform != null)
                set.Transform.localPosition = local;
        }
    }

#if DEV
    /// <summary>Where the parts ended up, in the space of the given root (to check the model's layout).</summary>
    public static string Describe(Transform root)
    {
        var parts = FindParts(root);
        if (parts == null)
            return "no parts";
        var sb = new System.Text.StringBuilder();
        sb.Append($"parts under {UnityQuery.PathOf(parts.parent)}: pos {root.InverseTransformPoint(parts.position)}, up {root.InverseTransformDirection(parts.up)}, forward {root.InverseTransformDirection(parts.forward)}, scale {parts.lossyScale}");
        for (var i = 0; i < parts.childCount; i++)
        {
            var child = parts.GetChild(i);
            if (child.name is "Column" or "Handlebar" or "Tyre")
                sb.AppendLine().Append($"  {child.name}: {root.InverseTransformPoint(child.position)} up {root.InverseTransformDirection(child.up)} size {child.lossyScale}");
        }
        return sb.ToString();
    }
#endif

    private static void Rod(Transform parts, string name, Vector3 from, Vector3 to, float radius, Material material, int layer)
    {
        var go = NewPart(parts, name, _cylinder!, material, layer);
        var t = go.transform;
        t.localPosition = (from + to) / 2f;
        t.localRotation = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);
        t.localScale = new Vector3(radius * 2f, (to - from).magnitude, radius * 2f);
    }

    private static void Box(Transform parts, string name, Vector3 centre, Vector3 size, Quaternion rotation, Material material, int layer)
    {
        var go = NewPart(parts, name, _box!, material, layer);
        var t = go.transform;
        t.localPosition = centre;
        t.localRotation = rotation;
        t.localScale = size;
    }

    private static GameObject NewPart(Transform parts, string name, Mesh mesh, Material material, int layer)
    {
        var go = new GameObject(name) { layer = layer };
        go.transform.SetParent(parts, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    // --- shared assets -------------------------------------------------------------------------------

    private static void EnsureAssets(Transform truck)
    {
        _cylinder ??= MakeCylinder(24);
        _box ??= MakeBox();
        if (_gold != null && _dark != null && _tyre != null && _steel != null)
            return;
        // A lit material of the game to copy the shader setup from; colours are set here (the deck's own
        // gold comes from a texture the generated parts have no coordinates for).
        var basis = UiKit.Get<MeshRenderer>(truck)!.sharedMaterial;
        // Barely metallic: the game's world has little to reflect, metals come out black.
        _gold = Tinted(basis, "ElectricScooter Gold", new Color(0.96f, 0.72f, 0.14f), 0.1f, 0.55f);
        _steel = Tinted(basis, "ElectricScooter Steel", new Color(0.66f, 0.68f, 0.72f), 0.1f, 0.5f);
        _dark = Tinted(basis, "ElectricScooter Dark", new Color(0.13f, 0.13f, 0.15f), 0f, 0.4f);
        _tyre = Tinted(basis, "ElectricScooter Tyre", new Color(0.05f, 0.05f, 0.055f), 0f, 0.15f);
    }

    private static Material Tinted(Material source, string name, Color color, float metallic, float smoothness)
    {
        var material = new Material(source) { name = name, hideFlags = HideFlags.DontUnloadUnusedAsset };
        foreach (var property in new[] { "_BaseColor", "_Color" })
            if (material.HasProperty(property))
                material.SetColor(property, color);
        foreach (var property in new[] { "_BaseMap", "_MainTex", "_MetallicGlossMap", "_BumpMap", "_OcclusionMap", "_ParallaxMap" })
            if (material.HasProperty(property))
                material.SetTexture(property, null);
        // Without their maps these features would read "white": fully metallic, which renders black here.
        foreach (var keyword in new[] { "_METALLICSPECGLOSSMAP", "_NORMALMAP", "_OCCLUSIONMAP", "_PARALLAXMAP", "_EMISSION" })
            material.DisableKeyword(keyword);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    /// <summary>Unit cylinder along Y: diameter 1, height 1, centred.</summary>
    private static Mesh MakeCylinder(int segments)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        for (var i = 0; i <= segments; i++)
        {
            var angle = i * Mathf.PI * 2f / segments;
            var n = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            vertices.Add(n * 0.5f + Vector3.down * 0.5f);
            vertices.Add(n * 0.5f + Vector3.up * 0.5f);
            normals.Add(n);
            normals.Add(n);
        }
        for (var i = 0; i < segments; i++)
        {
            var a = i * 2;
            triangles.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 });
        }
        foreach (var y in new[] { -0.5f, 0.5f })
        {
            var centre = vertices.Count;
            var normal = y > 0f ? Vector3.up : Vector3.down;
            vertices.Add(new Vector3(0f, y, 0f));
            normals.Add(normal);
            for (var i = 0; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * 0.5f, y, Mathf.Sin(angle) * 0.5f));
                normals.Add(normal);
            }
            for (var i = 0; i < segments; i++)
            {
                if (y > 0f)
                    triangles.AddRange(new[] { centre, centre + 2 + i, centre + 1 + i });
                else
                    triangles.AddRange(new[] { centre, centre + 1 + i, centre + 2 + i });
            }
        }
        return MakeMesh("ElectricScooter Cylinder", vertices, normals, triangles);
    }

    /// <summary>Unit cube, centred, with flat faces.</summary>
    private static Mesh MakeBox()
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        var axes = new[] { Vector3.right, Vector3.up, Vector3.forward };
        for (var axis = 0; axis < 3; axis++)
        {
            foreach (var sign in new[] { -1f, 1f })
            {
                var normal = axes[axis] * sign;
                var u = axes[(axis + 1) % 3];
                var v = axes[(axis + 2) % 3];
                var start = vertices.Count;
                vertices.Add(normal * 0.5f - u * 0.5f - v * 0.5f);
                vertices.Add(normal * 0.5f + u * 0.5f - v * 0.5f);
                vertices.Add(normal * 0.5f + u * 0.5f + v * 0.5f);
                vertices.Add(normal * 0.5f - u * 0.5f + v * 0.5f);
                for (var i = 0; i < 4; i++)
                    normals.Add(normal);
                if (sign > 0f)
                    triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                else
                    triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            }
        }
        return MakeMesh("ElectricScooter Box", vertices, normals, triangles);
    }

    private static Mesh MakeMesh(string name, List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
    {
        var mesh = new Mesh { name = name, hideFlags = HideFlags.DontUnloadUnusedAsset };
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = new Vector2[vertices.Count];
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
        return mesh;
    }
}
