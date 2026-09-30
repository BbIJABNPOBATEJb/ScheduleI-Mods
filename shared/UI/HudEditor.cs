using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace S1Shared.UI;

/// <summary>A movable, scalable HUD element. Its position is a normalized screen point (0..1) of its center.</summary>
internal sealed class HudElement
{
    private readonly Func<Vector2> _getPos;
    private readonly Action<Vector2> _setPos;
    private readonly Func<float> _getScale;
    private readonly Action<float> _setScale;

    public HudElement(string label, Vector2 defaultPos, float defaultScale,
        Func<Vector2> getPos, Action<Vector2> setPos, Func<float> getScale, Action<float> setScale)
    {
        Label = label;
        DefaultPos = defaultPos;
        DefaultScale = defaultScale;
        _getPos = getPos;
        _setPos = setPos;
        _getScale = getScale;
        _setScale = setScale;
    }

    public string Label { get; }
    public Vector2 DefaultPos { get; }
    public float DefaultScale { get; }
    public float MinScale { get; set; } = 0.4f;
    public float MaxScale { get; set; } = 3f;

    /// <summary>The element's root; assigned by the mod once its UI exists.</summary>
    public RectTransform? Rect { get; set; }

    public Vector2 Position
    {
        get => _getPos();
        set => _setPos(new Vector2(Mathf.Clamp01(value.x), Mathf.Clamp01(value.y)));
    }

    public float Scale
    {
        get => _getScale();
        set => _setScale(Mathf.Clamp(value, MinScale, MaxScale));
    }

    /// <summary>Places <see cref="Rect"/> at the stored position and scale.</summary>
    public void Apply()
    {
        if (Rect == null)
            return;
        var pos = Position;
        Rect.anchorMin = Rect.anchorMax = pos;
        Rect.pivot = new Vector2(0.5f, 0.5f);
        Rect.anchoredPosition = Vector2.zero;
        Rect.localScale = Vector3.one * Scale;
    }

    public void Reset()
    {
        Position = DefaultPos;
        Scale = DefaultScale;
        Apply();
    }
}

/// <summary>
/// Lets the player move and resize HUD elements with the mouse. Started from a settings screen
/// button: the screen is moved out of view (the game stays paused, so the cursor is free), every
/// element gets a frame, and a small panel explains the controls.
/// </summary>
internal sealed class HudEditor
{
    private static readonly Color FrameColor = new(1f, 0.82f, 0.25f, 1f);

    private readonly HudCanvas _canvas;
    private readonly SettingsWindow _window;
    private readonly IReadOnlyList<HudElement> _elements;
    private readonly List<GameObject> _decorations = new();
    private RectTransform? _panel;
    private RectTransform? _doneButton;
    private RectTransform? _resetButton;
    private HudElement? _dragging;
    private Vector2 _dragOffset;
    private bool _changed;

    public HudEditor(HudCanvas canvas, SettingsWindow window, IReadOnlyList<HudElement> elements)
    {
        _canvas = canvas;
        _window = window;
        _elements = elements;
    }

    public bool Active { get; private set; }

    public event Action? Started;

    /// <summary>Fired when editing ends; the argument tells whether anything moved.</summary>
    public event Action<bool>? Finished;

    public void Begin()
    {
        if (Active || _canvas.Root == null)
            return;
        Active = true;
        _changed = false;
        _window.SetHidden(true);
        _canvas.ShowWhilePaused = true;
        Started?.Invoke();
        foreach (var element in _elements)
            Decorate(element);
        BuildPanel();
    }

    public void End()
    {
        if (!Active)
            return;
        Active = false;
        _dragging = null;
        foreach (var go in _decorations)
            if (go != null)
                UnityEngine.Object.Destroy(go);
        _decorations.Clear();
        _canvas.ShowWhilePaused = false;
        _window.SetHidden(false);
        Finished?.Invoke(_changed);
    }

    /// <summary>Call every frame.</summary>
    public void Tick()
    {
        if (!Active)
            return;
        if (!_window.IsOpen || _canvas.Root == null)
        {
            End();
            return;
        }
        try
        {
            HandleInput();
        }
        catch (Exception ex)
        {
            MelonLoader.MelonLogger.Warning($"HUD editor input failed: {ex.Message}");
            End();
        }
    }

    private void HandleInput()
    {
        Vector2 mouse = Input.mousePosition;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            End();
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (Contains(_doneButton, mouse))
            {
                End();
                return;
            }
            if (Contains(_resetButton, mouse))
            {
                foreach (var element in _elements)
                    element.Reset();
                _changed = true;
                return;
            }
            var hit = ElementAt(mouse);
            if (hit != null)
            {
                _dragging = hit;
                _dragOffset = mouse - (Vector2)hit.Rect!.position;
            }
        }
        if (_dragging != null)
        {
            if (Input.GetMouseButton(0))
            {
                var center = mouse - _dragOffset;
                var pos = new Vector2(center.x / Screen.width, center.y / Screen.height);
                if (Mathf.Abs(center.x - Screen.width * 0.5f) < 10f)
                    pos.x = 0.5f; // snap to the vertical center line (e.g. under the compass)
                _dragging.Position = pos;
                _dragging.Apply();
                _changed = true;
            }
            else
            {
                _dragging = null;
            }
        }

        var hovered = ElementAt(mouse);
        if (hovered == null)
            return;
        var scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            hovered.Scale *= 1f + 0.08f * Mathf.Sign(scroll);
            hovered.Apply();
            _changed = true;
        }
        if (Input.GetMouseButtonDown(1))
        {
            hovered.Reset();
            _changed = true;
        }
    }

    private HudElement? ElementAt(Vector2 mouse)
    {
        for (var i = _elements.Count - 1; i >= 0; i--)
        {
            var element = _elements[i];
            if (element.Rect != null && element.Rect.gameObject.activeInHierarchy && Contains(element.Rect, mouse))
                return element;
        }
        return null;
    }

    private static bool Contains(RectTransform? rect, Vector2 screen) =>
        rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);

    // --- visuals -----------------------------------------------------------------------------------

    private void Decorate(HudElement element)
    {
        if (element.Rect == null)
            return;
        var frame = _canvas.NewRect("EditorFrame", element.Rect);
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.offsetMin = new Vector2(-6f, -6f);
        frame.offsetMax = new Vector2(6f, 6f);
        var fill = frame.gameObject.AddComponent<Image>();
        fill.sprite = SpriteKit.RoundedRect(6);
        fill.type = Image.Type.Sliced;
        fill.color = new Color(1f, 0.82f, 0.25f, 0.12f);
        fill.raycastTarget = false;
        AddEdge(frame, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), Vector2.zero);
        AddEdge(frame, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 2f));
        AddEdge(frame, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(2f, 0f));
        AddEdge(frame, new Vector2(1f, 0f), Vector2.one, new Vector2(-2f, 0f), Vector2.zero);

        var label = _canvas.NewText("EditorLabel", frame, 18f, FrameColor);
        var rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 4f);
        rect.sizeDelta = new Vector2(300f, 24f);
        label.text = element.Label;
        _decorations.Add(frame.gameObject);
    }

    private void AddEdge(RectTransform frame, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var edge = _canvas.NewImage("Edge", frame, FrameColor);
        var r = edge.rectTransform;
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = offsetMin;
        r.offsetMax = offsetMax;
    }

    private void BuildPanel()
    {
        var panel = _canvas.NewImage("HUD Editor", null, new Color(0.08f, 0.08f, 0.1f, 0.88f), SpriteKit.RoundedRect(10));
        _panel = panel.rectTransform;
        // Upper middle: clear of the compass (top) and the hotbar area (bottom).
        _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.66f);
        _panel.pivot = new Vector2(0.5f, 0.5f);
        _panel.anchoredPosition = Vector2.zero;
        _panel.sizeDelta = new Vector2(760f, 104f);
        _decorations.Add(panel.gameObject);

        var text = _canvas.NewText("Help", _panel, 19f, Color.white, outline: false);
        var tr = text.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -10f);
        tr.sizeDelta = new Vector2(-24f, 30f);
        text.text = "Drag to move  •  Mouse wheel to resize  •  Right-click to reset";

        _resetButton = MakeButton("Reset all", new Vector2(-110f, 14f), new Color(0.35f, 0.35f, 0.38f, 1f));
        _doneButton = MakeButton("Done", new Vector2(110f, 14f), new Color(0.2f, 0.55f, 0.95f, 1f));
    }

    private RectTransform MakeButton(string title, Vector2 pos, Color color)
    {
        var button = _canvas.NewImage(title, _panel, color, SpriteKit.RoundedRect(8));
        var rect = button.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(190f, 40f);
        var label = _canvas.NewText("Label", rect, 20f, Color.white, outline: false);
        var lr = label.rectTransform;
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        label.text = title;
        return rect;
    }
}
