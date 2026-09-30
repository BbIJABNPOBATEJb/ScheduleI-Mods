using System.Collections.Generic;
using UnityEngine;

namespace GuideArrows.Render;

/// <summary>
/// Geometry of a beveled 3D navigation arrow (a triangle with a notch at the back), pointing +Z,
/// flat-shaded: every face has its own vertices and a face normal, so lighting is computed per face.
/// </summary>
internal sealed class ArrowMesh
{
    // Outline, clockwise seen from above (x right, z forward).
    private static readonly Vector2[] Outline =
    {
        new(0f, 1.0f),      // tip
        new(0.74f, -0.74f), // right wing
        new(0f, -0.32f),    // notch
        new(-0.74f, -0.74f),// left wing
    };

    private const float HalfThickness = 0.13f;
    private const float Bevel = 0.075f;

    public readonly List<Vector3> Vertices = new();
    public readonly List<int> Triangles = new();
    /// <summary>Per vertex: its face normal (model space).</summary>
    public readonly List<Vector3> Normals = new();
    /// <summary>Per vertex: 0 at the back, 1 at the tip (for a gradient).</summary>
    public readonly List<float> Along = new();
    /// <summary>Per vertex: true for the top face (the arrow's "glass" surface).</summary>
    public readonly List<bool> IsTop = new();

    public ArrowMesh()
    {
        var n = Outline.Length;
        var inset = Inset(Outline, Bevel);
        var h = HalfThickness;
        var hb = HalfThickness - Bevel;

        Vector3 Top(int i) => new(inset[i].x, h, inset[i].y);
        Vector3 Bottom(int i) => new(inset[i].x, -h, inset[i].y);
        Vector3 OuterTop(int i) => new(Outline[i].x, hb, Outline[i].y);
        Vector3 OuterBottom(int i) => new(Outline[i].x, -hb, Outline[i].y);

        // Top and bottom faces (the outline is a concave quad: split at the notch).
        AddTriangle(Top(0), Top(1), Top(2), Vector3.up, true);
        AddTriangle(Top(0), Top(2), Top(3), Vector3.up, true);
        AddTriangle(Bottom(0), Bottom(1), Bottom(2), Vector3.down, false);
        AddTriangle(Bottom(0), Bottom(2), Bottom(3), Vector3.down, false);

        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            var outward = -InwardNormal(Outline[i], Outline[j]);
            var side = new Vector3(outward.x, 0f, outward.y);
            AddQuad(Top(i), Top(j), OuterTop(j), OuterTop(i), (side + Vector3.up).normalized, false);
            AddQuad(OuterTop(i), OuterTop(j), OuterBottom(j), OuterBottom(i), side, false);
            AddQuad(OuterBottom(i), OuterBottom(j), Bottom(j), Bottom(i), (side + Vector3.down).normalized, false);
        }
    }

    private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, bool top)
    {
        AddTriangle(a, b, c, outward, top);
        AddTriangle(a, c, d, outward, top);
    }

    /// <summary>Adds a triangle wound so that it faces <paramref name="outward"/> (Unity: clockwise = front).</summary>
    private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward, bool top)
    {
        var normal = Vector3.Cross(b - a, c - a);
        if (normal.sqrMagnitude < 1e-10f)
            return;
        if (Vector3.Dot(normal, outward) < 0f)
            (b, c) = (c, b);
        normal = Vector3.Cross(b - a, c - a).normalized;
        var start = Vertices.Count;
        foreach (var v in new[] { a, b, c })
        {
            Vertices.Add(v);
            Normals.Add(normal);
            Along.Add(Mathf.InverseLerp(-0.74f, 1f, v.z));
            IsTop.Add(top);
        }
        Triangles.Add(start);
        Triangles.Add(start + 1);
        Triangles.Add(start + 2);
    }

    /// <summary>Unit normal pointing into a clockwise polygon for the edge a→b.</summary>
    private static Vector2 InwardNormal(Vector2 a, Vector2 b)
    {
        var d = (b - a).normalized;
        return new Vector2(d.y, -d.x);
    }

    private static Vector2[] Inset(Vector2[] outline, float amount)
    {
        var n = outline.Length;
        var result = new Vector2[n];
        for (var i = 0; i < n; i++)
        {
            var prev = outline[(i + n - 1) % n];
            var cur = outline[i];
            var next = outline[(i + 1) % n];
            var n1 = InwardNormal(prev, cur);
            var n2 = InwardNormal(cur, next);
            var miter = (n1 + n2) / (1f + Vector2.Dot(n1, n2));
            result[i] = cur + miter * amount;
        }
        return result;
    }
}
