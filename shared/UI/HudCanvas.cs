using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace S1Shared.UI;

/// <summary>
/// A screen-space overlay canvas for a mod's HUD elements. It follows the game's HUD: same scale
/// (Settings → Interface Scale), drawn just above it, hidden whenever the game hides its HUD
/// (death, cutscenes) or the pause menu is open.
/// </summary>
internal sealed class HudCanvas
{
    private readonly string _name;
    private Canvas? _canvas;
    private CanvasScaler? _scaler;
    private TMP_FontAsset? _font;
    private Material? _outlineMaterial;

    public HudCanvas(string name) => _name = name;

    public RectTransform? Root { get; private set; }

    /// <summary>True while the canvas exists and is drawn.</summary>
    public bool Visible => _canvas != null && _canvas.enabled;

    /// <summary>Forces the canvas visible while paused (HUD layout editing).</summary>
    public bool ShowWhilePaused { get; set; }

    /// <summary>The game HUD's font (falls back to TMP's default font).</summary>
    public TMP_FontAsset? Font
    {
        get
        {
            if (_font == null)
                _font = FindHudFont();
            return _font;
        }
    }

    /// <summary>A shared material of <see cref="Font"/> with a dark outline, for text over the 3D world.</summary>
    public Material? OutlineMaterial
    {
        get
        {
            if (_outlineMaterial == null && Font != null)
            {
                _outlineMaterial = new Material(Font.material) { name = _name + " Outline" };
                _outlineMaterial.EnableKeyword("OUTLINE_ON");
                _outlineMaterial.SetFloat("_OutlineWidth", 0.22f);
                _outlineMaterial.SetColor("_OutlineColor", new Color(0f, 0f, 0f, 0.9f));
                _outlineMaterial.SetFloat("_FaceDilate", 0.1f);
            }
            return _outlineMaterial;
        }
    }

    /// <summary>Creates the canvas on first use; call every frame. Returns false when there is no game HUD.</summary>
    public bool Tick()
    {
        var hud = S1.UI.HUD.Instance;
        if (_canvas == null)
        {
            if (hud == null)
                return false;
            Create();
        }
        var hudCanvas = hud != null ? hud.canvas : null;
        var pause = S1.UI.PauseMenu.Instance;
        var paused = pause != null && pause.IsPaused;
        var show = hudCanvas != null && (ShowWhilePaused || (hudCanvas.enabled && !paused));
        if (_canvas!.enabled != show)
            _canvas.enabled = show;
        if (hudCanvas != null)
        {
            // Just above the HUD; above every menu while the HUD editor runs over the pause menu.
            _canvas.sortingOrder = ShowWhilePaused ? 30000 : hudCanvas.sortingOrder + 1;
            var scale = hudCanvas.scaleFactor;
            if (scale > 0.01f && !Mathf.Approximately(_scaler!.scaleFactor, scale))
                _scaler.scaleFactor = scale;
        }
        return show;
    }

    private void Create()
    {
        var go = new GameObject(_name);
        Object.DontDestroyOnLoad(go);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _scaler = go.AddComponent<CanvasScaler>();
        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        Root = UiKit.Get<RectTransform>(go);
    }

    /// <summary>Screen pixels per canvas unit (the game's interface scale).</summary>
    public float ScaleFactor => _scaler != null && _scaler.scaleFactor > 0.01f ? _scaler.scaleFactor : 1f;

    /// <summary>Canvas size in its own units (screen pixels divided by the HUD scale).</summary>
    public Vector2 Size => Root != null ? Root.rect.size : new Vector2(Screen.width, Screen.height);

    public RectTransform NewRect(string name, Transform? parent = null)
    {
        var go = new GameObject(name);
        var rect = go.AddComponent<RectTransform>();
        rect.SetParent(parent ?? Root, false);
        return rect;
    }

    public Image NewImage(string name, Transform? parent, Color color, Sprite? sprite = null)
    {
        var rect = NewRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        if (sprite != null && sprite.border != Vector4.zero)
            image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public TextMeshProUGUI NewText(string name, Transform? parent, float size, Color color, bool outline = true)
    {
        var rect = NewRect(name, parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null)
            text.font = Font;
        if (outline && OutlineMaterial != null)
            text.fontSharedMaterial = OutlineMaterial;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>Converts a screen point (pixels) to a position on this canvas (origin bottom-left).</summary>
    public Vector2 ScreenToCanvas(Vector2 screen)
    {
        var scale = _scaler != null && _scaler.scaleFactor > 0.01f ? _scaler.scaleFactor : 1f;
        return screen / scale;
    }

    private static TMP_FontAsset? FindHudFont()
    {
        var hud = S1.UI.HUD.Instance;
        if (hud != null)
        {
            var text = UnityQuery.GetComponentsInChildren<TMP_Text>(hud, true).FirstOrDefault(t => t.font != null);
            if (text != null)
                return text.font;
        }
        return TMP_Settings.defaultFontAsset;
    }
}
