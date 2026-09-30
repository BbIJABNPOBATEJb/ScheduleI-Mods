using System;
using System.Collections.Generic;
using DamageIndicator.Combat;
using S1Shared.UI;
using UnityEngine;

namespace DamageIndicator.UI;

/// <summary>Health bars over characters' heads, shown after they take damage (or while they fight you).</summary>
internal sealed class NpcBars
{
    private const float FlinchStun = 1.25f; // NPC heavy flinch (impact force ≥ 100, below ragdoll)
    private const float CombatScanInterval = 0.5f;

    private sealed class Entry
    {
        public S1.NPCs.NPC Npc = null!;
        public HealthBarView? View;
        public float LastDamage = -100f;
        public float StunUntil;
        public float StunTotal = 1f;
        public bool InCombat;
        public float Alpha;
        public float Distance;
    }

    private readonly HudCanvas _canvas;
    private readonly Dictionary<int, Entry> _entries = new();
    private readonly Stack<HealthBarView> _pool = new();
    private readonly List<int> _remove = new();
    private readonly List<Entry> _visible = new();
    private RectTransform? _layer;
    private float _nextCombatScan;

    public NpcBars(HudCanvas canvas) => _canvas = canvas;

    public void OnHit(NpcHit hit)
    {
        var entry = Get(hit.Npc);
        var now = Time.unscaledTime;
        entry.LastDamage = now;
        if (hit.Force >= 100f && hit.Force < 150f)
        {
            entry.StunUntil = now + FlinchStun;
            entry.StunTotal = FlinchStun;
        }
    }

    public void Clear()
    {
        foreach (var entry in _entries.Values)
            Release(entry);
        _entries.Clear();
    }

    /// <summary>Call late in the frame, after characters and the camera moved.</summary>
    public void Tick(Camera? camera)
    {
        var mode = Config.NpcBarMode;
        var now = Time.unscaledTime;
        if (mode == NpcBarMode.InCombat && now >= _nextCombatScan)
        {
            _nextCombatScan = now + CombatScanInterval;
            ScanCombat();
        }

        _visible.Clear();
        _remove.Clear();
        foreach (var pair in _entries)
        {
            var entry = pair.Value;
            if (entry.Npc == null)
            {
                Release(entry);
                _remove.Add(pair.Key);
                continue;
            }
            var want = mode != NpcBarMode.Never && camera != null && Config.Enabled.Value
                && (now - entry.LastDamage < Config.NpcHideDelay.Value || (mode == NpcBarMode.InCombat && entry.InCombat));
            Vector3 screen = default;
            if (want)
            {
                var head = HeadPosition(entry.Npc);
                entry.Distance = Vector3.Distance(camera!.transform.position, head);
                screen = camera.WorldToScreenPoint(head);
                want = screen.z > 0.2f && entry.Distance <= Config.NpcMaxDistance.Value;
            }
            entry.Alpha = Mathf.MoveTowards(entry.Alpha, want ? 1f : 0f, Time.unscaledDeltaTime / (want ? 0.15f : 0.4f));
            if (entry.Alpha <= 0f)
            {
                if (entry.View != null)
                    Release(entry);
                if (now - entry.LastDamage > 60f && !entry.InCombat)
                    _remove.Add(pair.Key);
                continue;
            }
            if (!want)
            {
                if (entry.View != null)
                    entry.View.Alpha = entry.Alpha;
                continue;
            }
            entry.View ??= Take();
            Update(entry, screen, now);
            _visible.Add(entry);
        }
        foreach (var key in _remove)
            _entries.Remove(key);

        // Nearer bars on top.
        _visible.Sort((a, b) => b.Distance.CompareTo(a.Distance));
        foreach (var entry in _visible)
            entry.View!.Root.SetAsLastSibling();
    }

    private void Update(Entry entry, Vector3 screen, float now)
    {
        var view = entry.View!;
        var npc = entry.Npc;
        view.Alpha = entry.Alpha;
        view.Root.anchoredPosition = _canvas.ScreenToCanvas(screen);
        var scale = Mathf.Clamp(7f / Mathf.Max(entry.Distance, 0.1f), 0.55f, 1.1f) * Config.NpcBarScale.Value;
        view.Root.localScale = Vector3.one * scale;

        var health = npc.Health;
        var max = MaxHealth(npc);
        var hp = health != null ? health.Health : 0f;
        var dead = health != null && (health.IsDead || npc.Behaviour.DeadBehaviour.Active);
        var knockedOut = !dead && health != null && (health.IsKnockedOut || npc.Behaviour.UnconsciousBehaviour.Active);
        view.SetHealth(max > 0f ? hp / max : 0f);
        view.SetDead(dead);
        view.SetName(Config.NpcNames.Value ? SafeName(npc) : null);
        view.SetValueText(Config.NpcValueText.Value ? Mathf.CeilToInt(Mathf.Max(0f, hp)).ToString() : null);

        if (dead)
            view.SetBadge("DEAD", new Color(0.8f, 0.8f, 0.8f));
        else if (knockedOut && Config.Stun.Value)
            view.SetBadge("KO", HealthBarView.StunColor);
        else
            view.SetBadge(null, Color.white);

        if (!Config.Stun.Value || dead)
            view.SetStun(-1f, false);
        else if (knockedOut)
            view.SetStun(1f, false);
        else if (npc.Avatar != null && npc.Avatar.Ragdolled)
            view.SetStun(1f, true);
        else if (now < entry.StunUntil)
            view.SetStun((entry.StunUntil - now) / entry.StunTotal, false);
        else
            view.SetStun(-1f, false);
        view.Tick();
    }

    private void ScanCombat()
    {
        var local = S1.PlayerScripts.Player.Local;
        foreach (var entry in _entries.Values)
            entry.InCombat = false;
        if (local == null)
            return;
        foreach (var npc in S1Shared.UnityQuery.ToManaged(S1.NPCs.NPCManager.NPCRegistry))
        {
            try
            {
                if (npc == null || !IsFighting(npc, local))
                    continue;
                Get(npc).InCombat = true;
            }
            catch (Exception)
            {
                // NPCs that are still spawning have no behaviour yet.
            }
        }
    }

    private static bool IsFighting(S1.NPCs.NPC npc, S1.PlayerScripts.Player local)
    {
        var combat = npc.Behaviour?.CombatBehaviour;
        if (combat == null || !combat.Active || combat.Target == null)
            return false;
#if IL2CPP
        var player = combat.Target.TryCast<S1.PlayerScripts.Player>();
#else
        var player = combat.Target as S1.PlayerScripts.Player;
#endif
        return player != null && player == local;
    }

    private Entry Get(S1.NPCs.NPC npc)
    {
        var id = npc.GetInstanceID();
        if (!_entries.TryGetValue(id, out var entry))
        {
            entry = new Entry { Npc = npc };
            _entries[id] = entry;
        }
        return entry;
    }

    private HealthBarView Take()
    {
        while (_pool.Count > 0)
        {
            var pooled = _pool.Pop();
            if (pooled.Root != null)
            {
                pooled.Root.gameObject.SetActive(true);
                return pooled;
            }
        }
        if (_layer == null)
        {
            _layer = _canvas.NewRect("Character Bars");
            _layer.anchorMin = Vector2.zero;
            _layer.anchorMax = Vector2.one;
            _layer.offsetMin = _layer.offsetMax = Vector2.zero;
            _layer.SetAsFirstSibling();
        }
        var view = new HealthBarView(_canvas, _layer, "Character Bar", 110f, 10f, withName: true);
        view.Root.anchorMin = view.Root.anchorMax = Vector2.zero;
        view.Root.pivot = new Vector2(0.5f, 0f);
        return view;
    }

    private void Release(Entry entry)
    {
        if (entry.View == null)
            return;
        if (entry.View.Root != null)
        {
            entry.View.Root.gameObject.SetActive(false);
            _pool.Push(entry.View);
        }
        entry.View = null;
    }

    private static Vector3 HeadPosition(S1.NPCs.NPC npc)
    {
        var avatar = npc.Avatar;
        if (avatar != null && avatar.HeadBone != null)
            return avatar.HeadBone.position + Vector3.up * 0.42f;
        return npc.transform.position + Vector3.up * 2.1f;
    }

    private static float MaxHealth(S1.NPCs.NPC npc)
    {
        try
        {
            return Mathf.Max(1f, npc.Health.MaxHealth);
        }
        catch (Exception)
        {
            return 100f;
        }
    }

    private static string? SafeName(S1.NPCs.NPC npc)
    {
        try
        {
            return npc.FirstName;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
