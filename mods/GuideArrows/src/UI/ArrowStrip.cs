using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuideArrows.Render;
using GuideArrows.Targets;
using S1Shared.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GuideArrows.UI;

/// <summary>
/// The row of arrows (by default right under the compass). Each arrow turns smoothly towards its
/// target relative to where you look, tilts towards targets above/below, and shows the distance.
/// </summary>
internal sealed class ArrowStrip
{
    private const float TextHeight = 38f;

    /// <summary>Most people of one kind glowing at once with "outline all" (the nearest ones).</summary>
    private const int GlowAllPerKind = 12;

    /// <summary>The kinds whose every target can glow, not only the one the arrow points at.</summary>
    private static readonly TargetKind[] GlowAllKinds = { TargetKind.Buyer, TargetKind.Customer };

    private sealed class ArrowView
    {
        public string Key = "";
        public TargetKind Kind;
        public Target? Target;
        public RectTransform Root = null!;
        public CanvasGroup Group = null!;
        public RawImage Image = null!;
        public Image Flat = null!;
        public Image Shadow = null!;
        public TextMeshProUGUI Distance = null!;
        public TextMeshProUGUI Label = null!;
        public ArrowRenderer? Renderer;
        public float Yaw, YawVelocity, Pitch, PitchVelocity;
        public float Alpha;
        public float X;
        public float TargetX;
        public bool Wanted;
        public bool Placed;
        public float PulseAt = -10f;
    }

    private readonly HudCanvas _canvas;
    private readonly List<ArrowView> _views = new();
    private readonly List<(Target Target, float Distance)> _chosen = new();
    private readonly List<(Target Target, float Distance)> _near = new();
    private readonly List<Target> _glowing = new();
    private RectTransform? _root;

    public ArrowStrip(HudCanvas canvas)
    {
        _canvas = canvas;
        Element = new HudElement("Guide arrows", Config.DefaultPos, 1f,
            () => new Vector2(Config.PosX.Value, Config.PosY.Value),
            p =>
            {
                Config.PosX.Value = p.x;
                Config.PosY.Value = p.y;
            },
            () => Config.Scale.Value,
            s => Config.Scale.Value = s);
    }

    public HudElement Element { get; }

    /// <summary>Shows one sample arrow per kind (HUD editor, settings preview).</summary>
    public bool Preview { get; set; }

    public int VisibleCount => _views.Count(v => v.Wanted);

    /// <summary>The targets that glow: those with an arrow, and those you came close enough to that their arrow went away.</summary>
    public IEnumerable<(GameObject Root, Color Color)> Glowing
    {
        get
        {
            foreach (var target in _glowing)
            {
                if (target.Highlight != null)
                    yield return (target.Highlight, Config.ColorOf(target.Kind));
            }
        }
    }

#if DEV
    /// <summary>Smoke tests: the targets that have an arrow right now.</summary>
    public IEnumerable<Target> ArrowTargets => _chosen.Select(c => c.Target);

    /// <summary>Smoke tests: freeze every arrow at this yaw/pitch to check orientation.</summary>
    public static float? DebugYaw;
    public static float DebugPitch;
#endif

    public void Clear()
    {
        foreach (var view in _views)
            Destroy(view);
        _views.Clear();
    }

    public void Tick(Camera? camera)
    {
        if (_canvas.Root == null)
            return;
        if (_root == null)
        {
            _root = _canvas.NewRect("Guide Arrows");
            Element.Rect = _root;
        }
        Element.Apply();

        var size = (float)Config.Size.Value;
        var spacing = (float)Config.Spacing.Value;
        var player = S1.PlayerScripts.Player.Local;
        var show = Preview || (Config.Enabled.Value && camera != null && player != null && CompassVisible());

        _chosen.Clear();
        _glowing.Clear();
        if (Preview)
            ChoosePreview();
        else if (show)
            Choose(player!.transform.position);

        foreach (var view in _views)
            view.Wanted = false;
        var order = 0;
        foreach (var (target, _) in _chosen)
        {
            var key = Config.ArrowMode == ArrowMode.PerKind || Preview ? "kind:" + target.Kind : target.Key;
            var view = _views.FirstOrDefault(v => v.Key == key) ?? Create(key);
            if (view.Target == null || view.Target.Key != target.Key)
                view.PulseAt = Time.unscaledTime;
            view.Target = target;
            view.Kind = target.Kind;
            view.Wanted = true;
            view.TargetX = (order - (_chosen.Count - 1) * 0.5f) * (size + spacing);
            if (!view.Placed)
            {
                view.X = view.TargetX;
                view.Placed = true;
            }
            order++;
        }
        _root.sizeDelta = new Vector2(Mathf.Max(1, _chosen.Count) * (size + spacing), size + TextHeight);

        var dt = Time.unscaledDeltaTime;
        for (var i = _views.Count - 1; i >= 0; i--)
        {
            var view = _views[i];
            view.Alpha = Mathf.MoveTowards(view.Alpha, view.Wanted && show ? 1f : 0f, dt / 0.25f);
            if (view.Alpha <= 0f && !view.Wanted)
            {
                Destroy(view);
                _views.RemoveAt(i);
                continue;
            }
            view.X = Mathf.Lerp(view.X, view.TargetX, 1f - Mathf.Exp(-10f * dt));
            UpdateView(view, camera, size);
        }
    }

    // --- choosing targets --------------------------------------------------------------------------

    private void Choose(Vector3 from)
    {
        var hideWithin = Config.HideWithin.Value;
        var maxDistance = Config.MaxDistance.Value;
        var candidates = new List<(Target, float)>();
        foreach (var target in TargetScanner.Targets)
        {
            var d = Vector3.Distance(from, target.Position);
            if (maxDistance > 0 && d > maxDistance)
                continue;
            candidates.Add((target, d));
        }
        _near.Clear();
        Pick(candidates, _near);
        if (Config.ArrowMode == ArrowMode.PerKind)
        {
            // Each arrow stands for the nearest target of its kind. Once you are there, that arrow hides
            // (the target keeps glowing) instead of swinging round to the next nearest one.
            foreach (var pick in _near)
            {
                _glowing.Add(pick.Target);
                if (pick.Distance >= hideWithin)
                    _chosen.Add(pick);
            }
        }
        else
        {
            // Nearest targets you have not reached yet; what you walked up to keeps glowing.
            var all = new List<(Target, float)>(candidates);
            candidates.RemoveAll(c => c.Item2 < hideWithin);
            Pick(candidates, _chosen);
            foreach (var (target, _) in _chosen)
                _glowing.Add(target);
            foreach (var (target, distance) in _near)
            {
                if (distance < hideWithin && !_glowing.Contains(target))
                    _glowing.Add(target);
            }
            candidates = all;
        }
        if (Config.OutlineAll.Value)
            GlowEveryone(candidates);
    }

    /// <summary>Every buyer and potential customer around glows, not only the ones with an arrow.</summary>
    private void GlowEveryone(List<(Target, float)> candidates)
    {
        var range = Config.OutlineRange.Value;
        foreach (var kind in GlowAllKinds)
        {
            var near = candidates
                .Where(c => c.Item1.Kind == kind && c.Item1.Highlight != null && (range <= 0 || c.Item2 <= range))
                .OrderBy(c => c.Item2)
                .Take(GlowAllPerKind);
            foreach (var (target, _) in near)
            {
                if (!_glowing.Contains(target))
                    _glowing.Add(target);
            }
        }
    }

    private static void Pick(List<(Target, float)> candidates, List<(Target Target, float Distance)> into)
    {
        var max = Mathf.Clamp(Config.MaxArrows.Value, 1, 8);
        switch (Config.ArrowMode)
        {
            case ArrowMode.PerKind:
                foreach (var kind in TargetKinds.All)
                {
                    var best = candidates.Where(c => c.Item1.Kind == kind).OrderBy(c => c.Item2).FirstOrDefault();
                    if (best.Item1 != null && into.Count < max)
                        into.Add(best);
                }
                break;
            case ArrowMode.Nearest:
                var nearest = candidates.OrderBy(c => c.Item2).FirstOrDefault();
                if (nearest.Item1 != null)
                    into.Add(nearest);
                break;
            default:
                into.AddRange(candidates.OrderBy(c => c.Item2).Take(max)
                    .OrderBy(c => (int)c.Item1.Kind).ThenBy(c => c.Item2));
                break;
        }
    }

    private void ChoosePreview()
    {
        foreach (var kind in TargetKinds.All)
        {
            if (Config.KindEnabled(kind))
                _chosen.Add((new Target { Kind = kind, Key = "preview:" + kind, Label = TargetKinds.Name(kind) }, 40f + 90f * (int)kind));
        }
    }

    // --- one arrow ----------------------------------------------------------------------------------

    private void UpdateView(ArrowView view, Camera? camera, float size)
    {
        var target = view.Target;
        view.Group.alpha = view.Alpha * Mathf.Clamp01(Config.Opacity.Value / 100f);
        view.Root.anchoredPosition = new Vector2(view.X, TextHeight * 0.5f);
        view.Root.sizeDelta = new Vector2(size, size);
        if (target == null)
            return;

        float yaw, pitch, distance;
        if (Preview)
        {
            yaw = Time.unscaledTime * 40f + (int)view.Kind * 72f;
            pitch = 0f;
            distance = 40f + 90f * (int)view.Kind;
        }
        else if (camera != null)
        {
            var from = camera.transform.position;
            var to = target.Position - from;
            var flatForward = Flat(camera.transform.forward);
            var flatTo = Flat(to);
            yaw = Vector3.SignedAngle(flatForward, flatTo, Vector3.up);
            var horizontal = new Vector2(to.x, to.z).magnitude;
            pitch = Config.PitchToTarget.Value ? Mathf.Clamp(Mathf.Atan2(to.y, horizontal) * Mathf.Rad2Deg, -45f, 45f) * 0.8f : 0f;
            var player = S1.PlayerScripts.Player.Local;
            distance = Vector3.Distance(player != null ? player.transform.position : from, target.Position);
        }
        else
        {
            return;
        }

#if DEV
        if (DebugYaw.HasValue)
        {
            yaw = DebugYaw.Value;
            pitch = DebugPitch;
            view.Yaw = yaw;
            view.Pitch = pitch;
        }
#endif
        var smooth = 1f / Mathf.Max(0.5f, Config.TurnSpeed.Value);
        view.Yaw = Mathf.SmoothDampAngle(view.Yaw, yaw, ref view.YawVelocity, smooth, Mathf.Infinity, Time.unscaledDeltaTime);
        view.Pitch = Mathf.SmoothDampAngle(view.Pitch, pitch, ref view.PitchVelocity, smooth, Mathf.Infinity, Time.unscaledDeltaTime);

        var color = Config.ColorOf(view.Kind);
        var pulse = Mathf.Clamp01(1f - (Time.unscaledTime - view.PulseAt) / 0.35f);
        var scale = 1f + 0.18f * Mathf.Sin(pulse * Mathf.PI);
        if (view.Renderer != null)
        {
            var pixels = Mathf.RoundToInt(size * Element.Scale * Mathf.Max(1f, _canvas.ScaleFactor) * 1.3f);
            view.Renderer.Render(pixels, color, view.Yaw, view.Pitch, Config.ViewAngle.Value, scale);
            view.Image.texture = view.Renderer.Texture;
        }
        else
        {
            view.Flat.color = color;
            view.Flat.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -view.Yaw);
            view.Flat.rectTransform.localScale = Vector3.one * (0.7f * scale);
        }

        view.Distance.gameObject.SetActive(Config.ShowDistance.Value);
        if (Config.ShowDistance.Value)
            view.Distance.text = FormatDistance(distance);
        var label = Config.ShowLabels.Value ? LabelFor(target) : "";
        view.Label.gameObject.SetActive(label.Length > 0);
        if (label.Length > 0)
        {
            view.Label.text = label;
            // As wide as one arrow's place in the row, so that long names do not run into the next one.
            view.Label.rectTransform.sizeDelta = new Vector2(Mathf.Max(48f, size + Config.Spacing.Value - 6f), 20f);
        }
    }

    private static string LabelFor(Target target) =>
        string.IsNullOrEmpty(target.Label) ? TargetKinds.Name(target.Kind) : target.Label;

    private ArrowView Create(string key)
    {
        var view = new ArrowView { Key = key };
        view.Root = _canvas.NewRect("Arrow " + key, _root);
        view.Root.anchorMin = view.Root.anchorMax = new Vector2(0.5f, 0.5f);
        view.Root.pivot = new Vector2(0.5f, 0.5f);
        view.Group = view.Root.gameObject.AddComponent<CanvasGroup>();
        view.Group.blocksRaycasts = false;
        view.Group.alpha = 0f;

        view.Shadow = _canvas.NewImage("Shadow", view.Root, new Color(0f, 0f, 0f, 0.28f), SpriteKit.Glow());
        var sr = view.Shadow.rectTransform;
        sr.anchorMin = new Vector2(0.08f, 0.12f);
        sr.anchorMax = new Vector2(0.92f, 0.52f);
        sr.offsetMin = sr.offsetMax = Vector2.zero;

        var imageRect = _canvas.NewRect("Arrow", view.Root);
        Stretch(imageRect);
        view.Image = imageRect.gameObject.AddComponent<RawImage>();
        view.Image.raycastTarget = false;

        view.Flat = _canvas.NewImage("Flat", view.Root, Color.white, SpriteKit.FlatArrow());
        Stretch(view.Flat.rectTransform);

        if (ArrowRenderer.Supported)
        {
            view.Renderer = new ArrowRenderer();
            view.Flat.gameObject.SetActive(false);
        }
        else
        {
            view.Image.gameObject.SetActive(false);
        }

        view.Distance = _canvas.NewText("Distance", view.Root, 17f, Color.white);
        var dr = view.Distance.rectTransform;
        dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 0f);
        dr.pivot = new Vector2(0.5f, 1f);
        dr.anchoredPosition = new Vector2(0f, 4f);
        dr.sizeDelta = new Vector2(120f, 22f);
        view.Distance.fontStyle = FontStyles.Bold;

        view.Label = _canvas.NewText("Label", view.Root, 14f, new Color(1f, 1f, 1f, 0.85f));
        var lr = view.Label.rectTransform;
        lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0f);
        lr.pivot = new Vector2(0.5f, 1f);
        lr.anchoredPosition = new Vector2(0f, -15f);
        lr.sizeDelta = new Vector2(140f, 20f);
        view.Label.overflowMode = TextOverflowModes.Ellipsis;

        _views.Add(view);
        return view;
    }

    private static void Destroy(ArrowView view)
    {
        view.Renderer?.Dispose();
        if (view.Root != null)
            UnityEngine.Object.Destroy(view.Root.gameObject);
    }

    private static bool CompassVisible()
    {
        var compass = S1.UI.Compass.CompassManager.Instance;
        return compass == null || compass.Container == null || compass.Container.gameObject.activeSelf;
    }

    private static string FormatDistance(float meters)
    {
        var imperial = false;
        try
        {
            var settings = S1.DevUtilities.Settings.Instance;
            imperial = settings != null && settings.UnitType == S1.DevUtilities.Settings.EUnitType.Imperial;
        }
        catch (Exception)
        {
            // Settings not ready: metric.
        }
        if (imperial)
        {
            var feet = meters * 3.28084f;
            return feet >= 2640f
                ? (feet / 5280f).ToString("0.0", CultureInfo.InvariantCulture) + " mi"
                : Mathf.RoundToInt(feet) + " ft";
        }
        return meters >= 1000f
            ? (meters / 1000f).ToString("0.0", CultureInfo.InvariantCulture) + " km"
            : Mathf.RoundToInt(meters) + " m";
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
