using System.Collections.Generic;
using S1Shared.UI;
using UnityEngine;

namespace DamageIndicator.UI;

/// <summary>Floating numbers: pop in at a world point (a hit) or a screen point, rise and fade out.</summary>
internal sealed class DamageNumbers
{
    private const int MaxActive = 48;
    private const float Rise = 80f;

    private sealed class Item
    {
        public TextMeshProUGUI Text = null!;
        public RectTransform Rect = null!;
        public float Start;
        public float Duration;
        public Vector3 World;
        public bool InWorld;
        public Vector2 CanvasPos;
        public Vector2 Drift;
        public float Size;
        public Color Color;
    }

    private readonly HudCanvas _canvas;
    private readonly List<Item> _active = new();
    private readonly Stack<Item> _pool = new();
    private RectTransform? _layer;
    private float _side = 1f;

    public DamageNumbers(HudCanvas canvas) => _canvas = canvas;

#if DEV
    /// <summary>Smoke tests: numbers spawned so far.</summary>
    public int Spawned { get; private set; }
#endif

    public void SpawnWorld(Vector3 world, string text, Color color, float size) =>
        Spawn(text, color, size, item =>
        {
            item.InWorld = true;
            item.World = world;
        });

    /// <summary>At a canvas position (origin bottom-left, canvas units).</summary>
    public void SpawnCanvas(Vector2 canvasPos, string text, Color color, float size) =>
        Spawn(text, color, size, item =>
        {
            item.InWorld = false;
            item.CanvasPos = canvasPos;
        });

    public void Clear()
    {
        foreach (var item in _active)
            Recycle(item);
        _active.Clear();
    }

    /// <summary>Call every frame (late, after cameras moved).</summary>
    public void Tick(Camera? camera)
    {
        var now = Time.unscaledTime;
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var item = _active[i];
            var t = (now - item.Start) / item.Duration;
            if (t >= 1f || item.Rect == null)
            {
                Recycle(item);
                _active.RemoveAt(i);
                continue;
            }
            Vector2 basePos;
            if (item.InWorld)
            {
                if (camera == null)
                {
                    item.Rect.gameObject.SetActive(false);
                    continue;
                }
                var screen = camera.WorldToScreenPoint(item.World);
                if (screen.z <= 0.1f)
                {
                    item.Rect.gameObject.SetActive(false);
                    continue;
                }
                basePos = _canvas.ScreenToCanvas(screen);
            }
            else
            {
                basePos = item.CanvasPos;
            }
            if (!item.Rect.gameObject.activeSelf)
                item.Rect.gameObject.SetActive(true);

            var ease = 1f - (1f - t) * (1f - t);
            item.Rect.anchoredPosition = basePos + new Vector2(item.Drift.x * (item.InWorld ? 0.6f + 0.4f * ease : ease), Rise * ease + item.Drift.y);
            var pop = t < 0.12f ? Mathf.Lerp(1.55f, 1f, t / 0.12f) : 1f;
            item.Rect.localScale = Vector3.one * pop;
            var c = item.Color;
            c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            item.Text.color = c;
        }
    }

    private void Spawn(string text, Color color, float size, System.Action<Item> place)
    {
        if (_canvas.Root == null)
            return;
#if DEV
        Spawned++;
#endif
        if (_active.Count >= MaxActive)
        {
            Recycle(_active[0]);
            _active.RemoveAt(0);
        }
        var item = _pool.Count > 0 ? _pool.Pop() : Create();
        if (item.Rect == null)
            item = Create();
        place(item);
        item.Start = Time.unscaledTime;
        item.Duration = Mathf.Max(0.3f, Config.NumberDuration.Value);
        // World hits start beside the point (the health bar sits right above heads), alternating sides.
        _side = -_side;
        item.Drift = item.InWorld
            ? new Vector2(_side * Random.Range(55f, 85f), Random.Range(-10f, 10f))
            : new Vector2(Random.Range(-12f, 12f), Random.Range(-4f, 6f));
        item.Size = size;
        item.Color = color;
        item.Text.text = text;
        item.Text.fontSize = size;
        item.Text.color = color;
        item.Rect.SetAsLastSibling();
        item.Rect.gameObject.SetActive(false);
        _active.Add(item);
    }

    private Item Create()
    {
        if (_layer == null)
        {
            _layer = _canvas.NewRect("Damage Numbers");
            _layer.anchorMin = Vector2.zero;
            _layer.anchorMax = Vector2.one;
            _layer.offsetMin = _layer.offsetMax = Vector2.zero;
        }
        var text = _canvas.NewText("Number", _layer, 30f, Color.white);
        text.fontStyle = FontStyles.Bold;
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200f, 50f);
        return new Item { Text = text, Rect = rect };
    }

    private void Recycle(Item item)
    {
        if (item.Rect == null)
            return;
        item.Rect.gameObject.SetActive(false);
        _pool.Push(item);
    }
}
