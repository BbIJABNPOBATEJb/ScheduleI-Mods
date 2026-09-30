using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuideArrows.Render;

/// <summary>
/// Renders one 3D arrow into its own anti-aliased RenderTexture (shown by a RawImage). No scene camera
/// or layer is involved: a command buffer draws the mesh with a fixed "studio" view, and lighting is
/// computed per face on the CPU into vertex colors, so the arrow looks the same day and night.
/// </summary>
internal sealed class ArrowRenderer : IDisposable
{
    private const float CameraDistance = 4.6f;
    private const float FieldOfView = 27f;

    private static Material? _material;
    private static bool _materialFailed;
    private static readonly ArrowMesh Geometry = new();
    private static readonly Vector3 LightDir = new Vector3(-0.45f, 0.85f, -0.35f).normalized;

    private readonly Mesh _mesh;
    private readonly Color[] _colors;
    private readonly CommandBuffer _commands = new() { name = "GuideArrows arrow" };
    private RenderTexture? _texture;

    public ArrowRenderer()
    {
        _mesh = new Mesh { name = "GuideArrows Arrow", hideFlags = HideFlags.HideAndDontSave };
        _mesh.vertices = Geometry.Vertices.ToArray();
        _mesh.triangles = Geometry.Triangles.ToArray();
        _colors = new Color[Geometry.Vertices.Count];
        _mesh.colors = _colors;
        _mesh.RecalculateBounds();
    }

    /// <summary>False when no usable shader exists (then the 2D fallback arrow is used).</summary>
    public static bool Supported => GetMaterial() != null;

    public RenderTexture? Texture => _texture;

    /// <summary>
    /// Draws the arrow: <paramref name="yaw"/> turns it (0 = pointing away from the viewer), <paramref name="pitch"/>
    /// raises the nose, <paramref name="viewAngle"/> is how steeply we look down on it.
    /// </summary>
    public void Render(int size, Color color, float yaw, float pitch, float viewAngle, float scale)
    {
        var material = GetMaterial();
        if (material == null)
            return;
        EnsureTexture(size);

        var rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-pitch, 0f, 0f);
        var view = viewAngle * Mathf.Deg2Rad;
        var cameraPos = new Vector3(0f, Mathf.Sin(view), -Mathf.Cos(view)) * CameraDistance;
        Shade(rotation, cameraPos.normalized, color);

        var cameraRot = Quaternion.LookRotation(-cameraPos, Vector3.up);
        var viewMatrix = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * Matrix4x4.TRS(cameraPos, cameraRot, Vector3.one).inverse;
        var projection = Matrix4x4.Perspective(FieldOfView, 1f, 0.5f, 20f);
        var model = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one * scale);

        _commands.Clear();
        _commands.SetRenderTarget(_texture);
        _commands.ClearRenderTarget(true, true, new Color(color.r, color.g, color.b, 0f));
        _commands.SetViewProjectionMatrices(viewMatrix, projection);
        _commands.DrawMesh(_mesh, model, material, 0, 0);
        Graphics.ExecuteCommandBuffer(_commands);
    }

    /// <summary>Per-face lighting: soft diffuse, a sharp highlight and a bright rim, like tinted glass.</summary>
    private void Shade(Quaternion rotation, Vector3 toCamera, Color baseColor)
    {
        var half = (LightDir + toCamera).normalized;
        var linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        var top = Color.Lerp(baseColor, Color.white, 0.18f);
        var dark = baseColor * 0.55f;
        for (var i = 0; i < _colors.Length; i++)
        {
            var n = rotation * Geometry.Normals[i];
            var diffuse = Mathf.Max(0f, Vector3.Dot(n, LightDir));
            var facing = Mathf.Max(0f, Vector3.Dot(n, toCamera));
            var spec = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(n, half)), 32f);
            var rim = Mathf.Pow(1f - facing, 3f);
            Color c;
            if (Geometry.IsTop[i])
            {
                // Glassy face: lighter towards the tip.
                var along = Geometry.Along[i];
                c = Color.Lerp(top, Color.Lerp(baseColor, Color.white, 0.5f), along * 0.7f) * (0.72f + 0.28f * diffuse);
            }
            else
            {
                c = Color.Lerp(dark, baseColor, 0.25f + 0.75f * diffuse);
            }
            c += Color.white * (0.7f * spec) + baseColor * (0.35f * rim);
            c.a = 1f;
            _colors[i] = linear ? c.linear : c;
        }
        _mesh.colors = _colors;
    }

    private void EnsureTexture(int size)
    {
        size = Mathf.Clamp(size, 32, 512);
        if (_texture != null && _texture.width == size && _texture.IsCreated())
            return;
        if (_texture != null)
        {
            _texture.Release();
            UnityEngine.Object.Destroy(_texture);
        }
        _texture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32)
        {
            name = "GuideArrows Arrow",
            antiAliasing = 4,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };
        _texture.Create();
    }

    private static Material? GetMaterial()
    {
        if (_material != null || _materialFailed)
            return _material;
        foreach (var name in new[] { "Hidden/Internal-Colored", "Sprites/Default", "UI/Default" })
        {
            var shader = Shader.Find(name);
            if (shader == null)
                continue;
            _material = new Material(shader) { name = "GuideArrows Arrow", hideFlags = HideFlags.HideAndDontSave };
            if (name == "Hidden/Internal-Colored")
            {
                _material.SetInt("_SrcBlend", (int)BlendMode.One);
                _material.SetInt("_DstBlend", (int)BlendMode.Zero);
                _material.SetInt("_Cull", (int)CullMode.Back);
                _material.SetInt("_ZWrite", 1);
                _material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            }
            MelonLogger.Msg($"Arrow shader: {name}");
            return _material;
        }
        _materialFailed = true;
        MelonLogger.Warning("No shader for 3D arrows found; using flat arrows");
        return null;
    }

    public void Dispose()
    {
        if (_texture != null)
        {
            _texture.Release();
            UnityEngine.Object.Destroy(_texture);
            _texture = null;
        }
        UnityEngine.Object.Destroy(_mesh);
        _commands.Release();
    }
}
