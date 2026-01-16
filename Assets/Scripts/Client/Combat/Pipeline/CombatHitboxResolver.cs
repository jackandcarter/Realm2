using System.Collections.Generic;
using System.Linq;
using Client.Combat;
using Realm.Abilities;
using UnityEngine;

namespace Client.Combat.Pipeline
{
    public static class CombatHitboxResolver
    {
        public static List<CombatEntity> FilterTargets(
            AbilityHitboxConfig hitbox,
            CombatEntity caster,
            IEnumerable<CombatEntity> candidates)
        {
            if (hitbox == null)
            {
                return candidates?.Where(candidate => candidate != null).ToList() ?? new List<CombatEntity>();
            }

            var results = new List<CombatEntity>();
            foreach (var candidate in candidates ?? new List<CombatEntity>())
            {
                if (candidate == null)
                {
                    continue;
                }

                if (IsWithinHitbox(hitbox, caster, candidate.Position))
                {
                    results.Add(candidate);
                }
            }

            return results;
        }

        private static bool IsWithinHitbox(AbilityHitboxConfig hitbox, CombatEntity caster, Vector3 targetPosition)
        {
            var originTransform = caster != null ? caster.AimOrigin : null;
            var origin = originTransform != null ? originTransform.position : Vector3.zero;
            var forward = originTransform != null ? originTransform.forward : Vector3.forward;
            var rotation = hitbox.UseCasterFacing && originTransform != null ? originTransform.rotation : Quaternion.identity;
            var offset = hitbox.UseCasterFacing ? rotation * hitbox.Offset : hitbox.Offset;
            var center = origin + offset;

            switch (hitbox.Shape)
            {
                case AbilityHitboxShape.Sphere:
                    return Vector3.Distance(center, targetPosition) <= Mathf.Max(0f, hitbox.Radius);
                case AbilityHitboxShape.Capsule:
                    return IsWithinCapsule(center, forward, hitbox.Length, hitbox.Radius, targetPosition);
                case AbilityHitboxShape.Box:
                    return IsWithinBox(center, rotation, hitbox.Size, targetPosition);
                case AbilityHitboxShape.Cone:
                    return IsWithinCone(center, forward, hitbox.Length, hitbox.Radius, targetPosition);
                default:
                    return false;
            }
        }

        private static bool IsWithinCapsule(
            Vector3 center,
            Vector3 forward,
            float length,
            float radius,
            Vector3 targetPosition)
        {
            var halfLength = Mathf.Max(0f, length) * 0.5f;
            var start = center - forward.normalized * halfLength;
            var end = center + forward.normalized * halfLength;

            var point = ClosestPointOnSegment(start, end, targetPosition);
            var distance = Vector3.Distance(point, targetPosition);
            return distance <= Mathf.Max(0f, radius);
        }

        private static bool IsWithinBox(
            Vector3 center,
            Quaternion rotation,
            Vector3 size,
            Vector3 targetPosition)
        {
            var local = Quaternion.Inverse(rotation) * (targetPosition - center);
            var half = size * 0.5f;
            return Mathf.Abs(local.x) <= Mathf.Abs(half.x) &&
                   Mathf.Abs(local.y) <= Mathf.Abs(half.y) &&
                   Mathf.Abs(local.z) <= Mathf.Abs(half.z);
        }

        private static bool IsWithinCone(
            Vector3 center,
            Vector3 forward,
            float length,
            float angleDegrees,
            Vector3 targetPosition)
        {
            var direction = targetPosition - center;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return true;
            }

            var distance = direction.magnitude;
            if (distance > Mathf.Max(0f, length))
            {
                return false;
            }

            var angle = Vector3.Angle(forward, direction);
            return angle <= Mathf.Max(0f, angleDegrees);
        }

        private static Vector3 ClosestPointOnSegment(Vector3 start, Vector3 end, Vector3 point)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return start;
            }

            var t = Vector3.Dot(point - start, segment) / lengthSquared;
            t = Mathf.Clamp01(t);
            return start + segment * t;
        }
    }
}
