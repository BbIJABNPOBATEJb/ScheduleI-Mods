using System.Collections.Generic;
using UnityEngine;

namespace S1Shared.UI;

/// <summary>Runtime-generated UI sprites (the game ships none we could rely on).</summary>
internal static class SpriteKit
{
    private static readonly Dictionary<int, Sprite> Rounded = new();
    private static Sprite? _glow;

    /// <summary>A white 9-sliced rounded rectangle; corners are <paramref name="radius"/> canvas units.</summary>
    public static Sprite RoundedRect(int radius)
    {
        if (Rounded.TryGetValue(radius, out var cached) && cached != null)
            return cached;
        var size = radius * 2 + 4;
        var pixels = new Color32[size * size];
        var inner = new Rect(radius + 0.5f, radius + 0.5f, size - 2 * radius - 1f, size - 2 * radius - 1f);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var px = x + 0.5f;
                var py = y + 0.5f;
                var dx = Mathf.Max(inner.xMin - px, 0f, px - inner.xMax);
                var dy = Mathf.Max(inner.yMin - py, 0f, py - inner.yMax);
                var dist = Mathf.Sqrt(dx * dx + dy * dy);
                var a = Mathf.Clamp01(radius - dist + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        var border = radius + 1f;
        var sprite = MakeSprite("Rounded" + radius, size, size, pixels, new Vector4(border, border, border, border));
        Rounded[radius] = sprite;
        return sprite;
    }

    /// <summary>A soft round glow (white center fading to transparent).</summary>
    public static Sprite Glow()
    {
        if (_glow != null)
            return _glow;
        const int size = 64;
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x + 0.5f) / size * 2f - 1f;
                var dy = (y + 0.5f) / size * 2f - 1f;
                var d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                var a = (1f - d) * (1f - d);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        _glow = MakeSprite("Glow", size, size, pixels, Vector4.zero);
        return _glow;
    }

    private static Sprite? _arrow;

    /// <summary>A flat navigation arrow pointing up (white, anti-aliased).</summary>
    public static Sprite FlatArrow()
    {
        if (_arrow != null)
            return _arrow;
        const int size = 64;
        var poly = new[] { new Vector2(32f, 62f), new Vector2(58f, 6f), new Vector2(32f, 20f), new Vector2(6f, 6f) };
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                // 4x4 supersampling for smooth edges.
                var hits = 0;
                for (var sy = 0; sy < 4; sy++)
                    for (var sx = 0; sx < 4; sx++)
                        if (Inside(poly, new Vector2(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f)))
                            hits++;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / 16));
            }
        }
        _arrow = MakeSprite("FlatArrow", size, size, pixels, Vector4.zero);
        return _arrow;
    }

    private static bool Inside(Vector2[] poly, Vector2 p)
    {
        var inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y)
                && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }

    private static Sprite MakeSprite(string name, int width, int height, Color32[] pixels, Vector4 border)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "S1Mods " + name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, border);
        sprite.name = texture.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
