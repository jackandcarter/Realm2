using System.Collections.Generic;
using System.Linq;
using Client.Combat;
using Realm.Abilities;
using UnityEngine;

namespace Client.Combat.Pipeline
{
    public static class CombatTargetResolver
    {
        public static List<CombatEntity> ResolveTargets(
            AbilityTargetingConfig targeting,
            CombatEntity caster,
            CombatEntity primaryTarget,
            Vector3? groundPoint,
            IEnumerable<CombatEntity> candidates)
        {
            var resolved = new List<CombatEntity>();
            var config = targeting ?? new AbilityTargetingConfig();
            var allCandidates = candidates?.Where(entity => entity != null).ToList() ?? new List<CombatEntity>();

            switch (config.Mode)
            {
                case AbilityTargetingMode.Self:
                    if (caster != null)
                    {
                        resolved.Add(caster);
                    }
                    break;
                case AbilityTargetingMode.Ally:
                    resolved.AddRange(ResolveSingleTarget(caster, primaryTarget, allCandidates, true, config));
                    break;
                case AbilityTargetingMode.Enemy:
                    resolved.AddRange(ResolveSingleTarget(caster, primaryTarget, allCandidates, false, config));
                    break;
                case AbilityTargetingMode.Area:
                    resolved.AddRange(ResolveAreaTargets(config, caster, primaryTarget, groundPoint, allCandidates));
                    break;
                case AbilityTargetingMode.Global:
                    resolved.AddRange(ResolveGlobalTargets(config, caster, allCandidates));
                    break;
            }

            return LimitTargets(resolved, config.MaxTargets);
        }

        private static IEnumerable<CombatEntity> ResolveSingleTarget(
            CombatEntity caster,
            CombatEntity primaryTarget,
            List<CombatEntity> candidates,
            bool wantsAlly,
            AbilityTargetingConfig config)
        {
            if (primaryTarget != null && MatchesTeam(caster, primaryTarget, wantsAlly))
            {
                return new[] { primaryTarget };
            }

            if (config.RequiresPrimaryTarget)
            {
                return new CombatEntity[0];
            }

            return candidates.Where(candidate => MatchesTeam(caster, candidate, wantsAlly));
        }

        private static IEnumerable<CombatEntity> ResolveAreaTargets(
            AbilityTargetingConfig config,
            CombatEntity caster,
            CombatEntity primaryTarget,
            Vector3? groundPoint,
            List<CombatEntity> candidates)
        {
            Vector3 origin;
            if (groundPoint.HasValue)
            {
                origin = groundPoint.Value;
            }
            else if (primaryTarget != null)
            {
                origin = primaryTarget.Position;
            }
            else if (caster != null)
            {
                origin = caster.Position;
            }
            else
            {
                return new CombatEntity[0];
            }

            var results = new List<CombatEntity>();
            foreach (var candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (!config.CanAffectCaster && candidate == caster)
                {
                    continue;
                }

                if (IsInsideArea(config, origin, caster, candidate.Position))
                {
                    results.Add(candidate);
                }
            }

            return results;
        }

        private static IEnumerable<CombatEntity> ResolveGlobalTargets(
            AbilityTargetingConfig config,
            CombatEntity caster,
            List<CombatEntity> candidates)
        {
            if (config.CanAffectCaster)
            {
                return candidates;
            }

            return candidates.Where(candidate => candidate != caster);
        }

        private static bool IsInsideArea(
            AbilityTargetingConfig config,
            Vector3 origin,
            CombatEntity caster,
            Vector3 targetPosition)
        {
            var distance = Vector3.Distance(origin, targetPosition);
            var size = Mathf.Max(0f, config.AreaSize);

            switch (config.AreaShape)
            {
                case AbilityAreaShape.Circle:
                    return distance <= size;
                case AbilityAreaShape.Line:
                    {
                        var forward = caster != null ? caster.AimOrigin.forward : Vector3.forward;
                        var direction = targetPosition - origin;
                        var forwardDistance = Vector3.Dot(direction, forward);
                        return forwardDistance >= 0f && forwardDistance <= size;
                    }
                case AbilityAreaShape.Cone:
                    {
                        var forward = caster != null ? caster.AimOrigin.forward : Vector3.forward;
                        var direction = (targetPosition - origin);
                        if (direction.sqrMagnitude <= Mathf.Epsilon)
                        {
                            return true;
                        }

                        var angle = Vector3.Angle(forward, direction);
                        var halfAngle = Mathf.Clamp(size, 0f, 180f);
                        return angle <= halfAngle;
                    }
                default:
                    return distance <= size;
            }
        }

        private static bool MatchesTeam(CombatEntity caster, CombatEntity candidate, bool wantsAlly)
        {
            if (caster == null || candidate == null)
            {
                return false;
            }

            if (caster.Team == CombatTeam.Neutral || candidate.Team == CombatTeam.Neutral)
            {
                return wantsAlly;
            }

            return wantsAlly ? caster.Team == candidate.Team : caster.Team != candidate.Team;
        }

        private static List<CombatEntity> LimitTargets(List<CombatEntity> targets, int maxTargets)
        {
            if (targets == null)
            {
                return new List<CombatEntity>();
            }

            if (maxTargets <= 0 || targets.Count <= maxTargets)
            {
                return targets;
            }

            return targets.Take(maxTargets).ToList();
        }
    }
}
