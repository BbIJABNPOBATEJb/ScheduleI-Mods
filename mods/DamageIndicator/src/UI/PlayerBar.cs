using System;
using S1Shared.UI;
using UnityEngine;

namespace DamageIndicator.UI;

/// <summary>Your health bar (movable), your damage/heal numbers and the tased (stun) bar.</summary>
internal sealed class PlayerBar
{
    private const float MaxHealth = 100f; // PlayerHealth.MaxHealth
    private const float TaseDuration = 2f; // Taser.TaseDuration
    private const float Height = 16f;

    private readonly HudCanvas _canvas;
    private readonly DamageNumbers _numbers;
    private HealthBarView? _view;
    private float _lastHealth = -1f;
    private bool _wasAlive;
    private bool _wasFull = true;
    private bool _wasTased;
    private float _tasedAt = -100f;
    private float _visibleUntil;
    private float _alpha;

    public PlayerBar(HudCanvas canvas, DamageNumbers numbers)
    {
        _canvas = canvas;
        _numbers = numbers;
        Element = new HudElement("Your health", Config.DefaultPlayerBarPos, 1f,
            () => new Vector2(Config.PlayerBarX.Value, Config.PlayerBarY.Value),
            p =>
            {
                Config.PlayerBarX.Value = p.x;
                Config.PlayerBarY.Value = p.y;
            },
            () => Config.PlayerBarScale.Value,
            s => Config.PlayerBarScale.Value = s);
    }

    public HudElement Element { get; }

    /// <summary>Shows the bar with sample values (HUD editor).</summary>
    public bool Preview { get; set; }

    public void Reset()
    {
        _lastHealth = -1f;
        _visibleUntil = 0f;
        _alpha = 0f;
        if (_view != null)
            _view.Alpha = 0f;
    }

    public void Tick()
    {
        if (_canvas.Root == null)
            return;
        if (_view == null || _view.Root == null)
        {
            _view = new HealthBarView(_canvas, _canvas.Root, "Your Health", Config.PlayerBarWidth.Value, Height, withName: false);
            _view.Alpha = 0f;
            Element.Rect = _view.Root;
        }
        _view.SetWidth(Config.PlayerBarWidth.Value);
        Element.Apply();

        if (Preview)
        {
            _view.SetHealth(0.72f, instant: true);
            _view.SetValueText(Config.PlayerValueText.Value ? "72" : null);
            _view.SetStun(Config.Stun.Value ? 0.5f : -1f, false);
            _view.SetDead(false);
            _view.Alpha = 1f;
            _view.Tick();
            return;
        }

        var player = S1.PlayerScripts.Player.Local;
        var health = player != null ? player.Health : null;
        if (health == null)
        {
            _lastHealth = -1f;
            _view.Alpha = 0f;
            return;
        }
        var now = Time.unscaledTime;
        var hp = health.CurrentHealth;
        var alive = health.IsAlive;
        if (_lastHealth < 0f)
        {
            _lastHealth = hp;
            _wasAlive = alive;
            _view.SetHealth(hp / MaxHealth, instant: true);
        }
        var delta = hp - _lastHealth;
        if (delta < -0.01f && alive)
            OnDamage(-delta, now);
        else if (delta > 0.01f && _wasAlive && alive && delta >= 2f)
            OnHeal(delta);
        if (alive && !_wasAlive)
            _view.SetHealth(hp / MaxHealth, instant: true);
        _lastHealth = hp;
        _wasAlive = alive;

        var full = hp >= MaxHealth - 0.01f;
        if (full && !_wasFull)
            _visibleUntil = Mathf.Max(_visibleUntil, now + Config.PlayerHideDelay.Value);
        _wasFull = full;

        var tased = SafeIsTased(player!);
        if (tased && !_wasTased)
            _tasedAt = now;
        _wasTased = tased;
        var stunLeft = 1f - (now - _tasedAt) / TaseDuration;
        if (stunLeft > 0f && Config.Stun.Value)
            _visibleUntil = Mathf.Max(_visibleUntil, now + 0.5f);

        var want = Config.Enabled.Value && alive && Config.PlayerBarMode switch
        {
            PlayerBarMode.Always => true,
            PlayerBarMode.WhileHurt => !full || now < _visibleUntil,
            PlayerBarMode.AfterDamage => now < _visibleUntil,
            _ => false,
        };
        _alpha = Mathf.MoveTowards(_alpha, want ? 1f : 0f, Time.unscaledDeltaTime / (want ? 0.15f : 0.5f));
        _view.Alpha = _alpha;
        if (_alpha <= 0f)
            return;
        _view.SetHealth(hp / MaxHealth);
        _view.SetDead(false);
        _view.SetValueText(Config.PlayerValueText.Value ? Mathf.CeilToInt(hp).ToString() : null);
        _view.SetStun(Config.Stun.Value && stunLeft > 0f ? stunLeft : -1f, false);
        _view.SetBadge(null, Color.white);
        _view.Tick();
    }

    private void OnDamage(float amount, float now)
    {
        _visibleUntil = now + Config.PlayerHideDelay.Value;
        if (Config.Enabled.Value && Config.PlayerNumbers.Value)
            _numbers.SpawnCanvas(NumberOrigin(), "-" + Mathf.Max(1, Mathf.RoundToInt(amount)), HealthBarView.HealthColor,
                28f * Config.NumberScale.Value);
    }

    private void OnHeal(float amount)
    {
        if (Config.Enabled.Value && Config.PlayerHealNumbers.Value)
            _numbers.SpawnCanvas(NumberOrigin(), "+" + Mathf.RoundToInt(amount), HealthBarView.HealColor,
                26f * Config.NumberScale.Value);
    }

    /// <summary>Just right of the bar's right end, in canvas units.</summary>
    private Vector2 NumberOrigin()
    {
        var size = _canvas.Size;
        var pos = Element.Position;
        var halfWidth = Config.PlayerBarWidth.Value * 0.5f * Element.Scale;
        return new Vector2(pos.x * size.x + halfWidth + 34f, pos.y * size.y + 6f);
    }

    private static bool SafeIsTased(S1.PlayerScripts.Player player)
    {
        try
        {
            return player.IsTased;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
