using S1Shared.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DamageIndicator.UI;

/// <summary>
/// A health bar: dark frame, red fill with a light "recent damage" trail that drains after a short
/// delay, an optional yellow stun bar underneath, a value text, a name above and a KO/dead badge.
/// </summary>
internal sealed class HealthBarView
{
    public static readonly Color HealthColor = new(0.9f, 0.2f, 0.18f, 1f);
    public static readonly Color TrailColor = new(1f, 1f, 1f, 0.6f);
    public static readonly Color HealColor = new(0.45f, 0.95f, 0.45f, 1f);
    public static readonly Color StunColor = new(1f, 0.78f, 0.1f, 1f);
    private static readonly Color FrameColor = new(0f, 0f, 0f, 0.62f);
    private static readonly Color DeadColor = new(0.45f, 0.45f, 0.45f, 1f);

    private const float TrailDelay = 0.45f;
    private const float TrailSpeed = 0.9f; // fraction per second

    private readonly RectTransform _bar;
    private readonly RectTransform _fill;
    private readonly RectTransform _trail;
    private readonly Image _fillImage;
    private readonly RectTransform _stun;
    private readonly RectTransform _stunFill;
    private readonly Image _stunImage;
    private readonly TextMeshProUGUI _value;
    private readonly TextMeshProUGUI? _name;
    private readonly TextMeshProUGUI _badge;
    private readonly CanvasGroup _group;
    private readonly float _height;

    private float _shown = 1f;
    private float _trailValue = 1f;
    private float _trailHoldUntil;
    private float _stunValue = -1f;
    private bool _stunPulse;

    public HealthBarView(HudCanvas canvas, Transform? parent, string name, float width, float height, bool withName)
    {
        _height = height;
        Root = canvas.NewRect(name, parent);
        Root.sizeDelta = new Vector2(width, height);
        _group = Root.gameObject.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable = false;

        var frame = canvas.NewImage("Frame", Root, FrameColor, SpriteKit.RoundedRect(4));
        Stretch(frame.rectTransform, 2f);
        _bar = canvas.NewRect("Bar", Root);
        Stretch(_bar, 0f);
        var trail = canvas.NewImage("Trail", _bar, TrailColor, SpriteKit.RoundedRect(3));
        _trail = trail.rectTransform;
        _fillImage = canvas.NewImage("Fill", _bar, HealthColor, SpriteKit.RoundedRect(3));
        _fill = _fillImage.rectTransform;
        var shine = canvas.NewImage("Shine", _fill, new Color(1f, 1f, 1f, 0.18f), SpriteKit.RoundedRect(2));
        var sr = shine.rectTransform;
        sr.anchorMin = new Vector2(0f, 0.55f);
        sr.anchorMax = Vector2.one;
        sr.offsetMin = new Vector2(1f, 0f);
        sr.offsetMax = new Vector2(-1f, -1f);
        SetFraction(_trail, 1f);
        SetFraction(_fill, 1f);

        _stun = canvas.NewRect("Stun", Root);
        _stun.anchorMin = new Vector2(0f, 0f);
        _stun.anchorMax = new Vector2(1f, 0f);
        _stun.pivot = new Vector2(0.5f, 1f);
        _stun.anchoredPosition = new Vector2(0f, -4f);
        _stun.sizeDelta = new Vector2(0f, Mathf.Max(4f, height * 0.35f));
        var stunFrame = canvas.NewImage("Frame", _stun, FrameColor, SpriteKit.RoundedRect(3));
        Stretch(stunFrame.rectTransform, 1.5f);
        _stunImage = canvas.NewImage("Fill", _stun, StunColor, SpriteKit.RoundedRect(2));
        _stunFill = _stunImage.rectTransform;
        SetFraction(_stunFill, 1f);
        _stun.gameObject.SetActive(false);

        _value = canvas.NewText("Value", Root, Mathf.Max(12f, height * 0.95f), Color.white);
        Stretch(_value.rectTransform, 0f);
        _value.rectTransform.offsetMax = new Vector2(-6f, 0f);
        _value.alignment = TextAlignmentOptions.MidlineRight;
        _value.fontStyle = FontStyles.Bold;

        if (withName)
        {
            _name = canvas.NewText("Name", Root, 17f, Color.white);
            var nr = _name.rectTransform;
            nr.anchorMin = new Vector2(0f, 1f);
            nr.anchorMax = new Vector2(1f, 1f);
            nr.pivot = new Vector2(0.5f, 0f);
            nr.anchoredPosition = new Vector2(0f, 3f);
            nr.sizeDelta = new Vector2(80f, 22f);
        }

        // KO / DEAD, centered over the (empty) bar.
        _badge = canvas.NewText("Badge", Root, Mathf.Max(15f, height * 1.6f), StunColor);
        var br = _badge.rectTransform;
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
        br.pivot = new Vector2(0.5f, 0.5f);
        br.anchoredPosition = Vector2.zero;
        br.sizeDelta = new Vector2(width, height * 2.5f);
        _badge.alignment = TextAlignmentOptions.Center;
        _badge.fontStyle = FontStyles.Bold;
        _badge.gameObject.SetActive(false);
    }

    public RectTransform Root { get; }

    public float Alpha
    {
        get => _group.alpha;
        set => _group.alpha = value;
    }

    public void SetWidth(float width) => Root.sizeDelta = new Vector2(width, _height);

    public void SetName(string? name)
    {
        if (_name == null)
            return;
        _name.gameObject.SetActive(!string.IsNullOrEmpty(name));
        if (!string.IsNullOrEmpty(name))
            _name.text = name;
    }

    public void SetValueText(string? text)
    {
        _value.gameObject.SetActive(text != null);
        if (text != null)
            _value.text = text;
    }

    /// <summary>Sets the health fraction; a drop leaves a trail that drains after a moment.</summary>
    public void SetHealth(float fraction, bool instant = false)
    {
        fraction = Mathf.Clamp01(fraction);
        if (instant)
        {
            _shown = _trailValue = fraction;
        }
        else if (fraction < _shown - 0.0001f)
        {
            _trailValue = Mathf.Max(_trailValue, _shown);
            _trailHoldUntil = Time.unscaledTime + TrailDelay;
            _shown = fraction;
        }
        else
        {
            _shown = fraction;
            if (_trailValue < _shown)
                _trailValue = _shown;
        }
    }

    public void SetDead(bool dead) => _fillImage.color = dead ? DeadColor : HealthColor;

    /// <summary>Stun bar: 0..1 remaining, or negative to hide. Pulsing marks an open-ended stun.</summary>
    public void SetStun(float fraction, bool pulse)
    {
        _stunValue = fraction;
        _stunPulse = pulse;
        var show = fraction >= 0f;
        if (_stun.gameObject.activeSelf != show)
            _stun.gameObject.SetActive(show);
    }

    public void SetBadge(string? text, Color color)
    {
        var show = !string.IsNullOrEmpty(text);
        if (_badge.gameObject.activeSelf != show)
            _badge.gameObject.SetActive(show);
        if (show)
        {
            _badge.text = text;
            _badge.color = color;
        }
    }

    /// <summary>Call every frame while visible.</summary>
    public void Tick()
    {
        var now = Time.unscaledTime;
        if (_trailValue > _shown && now >= _trailHoldUntil)
            _trailValue = Mathf.Max(_shown, _trailValue - TrailSpeed * Time.unscaledDeltaTime);
        SetFraction(_fill, _shown);
        SetFraction(_trail, _trailValue);
        if (_stunValue >= 0f)
        {
            SetFraction(_stunFill, _stunValue);
            var a = _stunPulse ? 0.65f + 0.35f * Mathf.Sin(now * 8f) : 1f;
            var c = StunColor;
            c.a = a;
            _stunImage.color = c;
        }
    }

    private static void SetFraction(RectTransform r, float fraction)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        var visible = fraction > 0.001f;
        if (r.gameObject.activeSelf != visible)
            r.gameObject.SetActive(visible);
    }

    private static void Stretch(RectTransform r, float outset)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(-outset, -outset);
        r.offsetMax = new Vector2(outset, outset);
    }
}
