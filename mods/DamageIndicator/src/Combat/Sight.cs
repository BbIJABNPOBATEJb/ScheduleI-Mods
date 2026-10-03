using S1Shared;
using UnityEngine;

namespace DamageIndicator.Combat;

/// <summary>Whether you witnessed a hit: close enough, and nothing solid between your eyes and the character.</summary>
internal static class Sight
{
    public static bool Witnessed(Camera? camera, NpcHit hit, float range)
    {
        if (camera == null || hit.Npc == null)
            return false;
        var eye = camera.transform.position;
        var body = hit.Npc.transform.position + Vector3.up * 1.2f;
        var point = hit.Point != Vector3.zero ? hit.Point : body;
        if (Vector3.Distance(eye, point) > range)
            return false;
        return Clear(eye, point) || Clear(eye, Head(hit.Npc));
    }

    private static Vector3 Head(S1.NPCs.NPC npc)
    {
        var look = npc.Avatar != null ? npc.Avatar.LookController : null;
        return look != null && look.HeadBone != null ? look.HeadBone.position : npc.transform.position + Vector3.up * 1.7f;
    }

    /// <summary>No wall, building, vehicle or prop on the line. People (the one hit included) are not cover.</summary>
    private static bool Clear(Vector3 from, Vector3 to)
    {
        var delta = to - from;
        var distance = delta.magnitude;
        if (distance < 0.3f)
            return true;
        // Stops a little short: the line ends on the character's own body.
        var hits = Physics.RaycastAll(from, delta / distance, distance - 0.15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            var collider = hit.collider;
            if (collider == null)
                continue;
            if (UnityQuery.GetComponentInParent<S1.NPCs.NPC>(collider) != null
                || UnityQuery.GetComponentInParent<S1.PlayerScripts.Player>(collider) != null)
                continue;
            return false;
        }
        return true;
    }
}
