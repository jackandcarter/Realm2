using System;
using System.Collections.Generic;
using Realm.Data;
using UnityEngine;

namespace Client.Combat.Stats
{
    public sealed class CombatStatCalculator
    {
        public CombatStatSnapshot Calculate(
            StatRegistry registry,
            ClassDefinition classDefinition,
            StatProfileDefinition profile,
            int level,
            IReadOnlyList<CombatStatModifier> modifiers,
            float normalizedRandom = 0.5f)
        {
            var baseStats = EvaluateBaseStats(profile, classDefinition, level, normalizedRandom);
            ApplyModifiers(baseStats, modifiers);

            var derivedStats = EvaluateDerivedStats(registry, baseStats);
            var finalStats = MergeFinalStats(baseStats, derivedStats);

            return new CombatStatSnapshot(baseStats, derivedStats, finalStats);
        }

        private static Dictionary<string, float> EvaluateBaseStats(
            StatProfileDefinition profile,
            ClassDefinition classDefinition,
            int level,
            float normalizedRandom)
        {
            var baseStats = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            if (profile != null && profile.StatCurves != null)
            {
                foreach (var curve in profile.StatCurves)
                {
                    AddCurveValue(baseStats, curve, level, normalizedRandom);
                }
            }

            if (classDefinition != null)
            {
                foreach (var curve in classDefinition.BaseStatCurves)
                {
                    AddCurveValue(baseStats, curve, level, normalizedRandom);
                }

                foreach (var curve in classDefinition.GrowthModifiers)
                {
                    AddCurveValue(baseStats, curve, level, normalizedRandom);
                }
            }

            return baseStats;
        }

        private static void AddCurveValue(
            IDictionary<string, float> stats,
            ClassStatCurve curve,
            int level,
            float normalizedRandom)
        {
            if (curve == null || curve.Stat == null || string.IsNullOrWhiteSpace(curve.Stat.Guid))
            {
                return;
            }

            var value = curve.EvaluateJitteredValue(level, normalizedRandom);
            var key = curve.Stat.Guid;
            if (stats.TryGetValue(key, out var existing))
            {
                stats[key] = existing + value;
            }
            else
            {
                stats[key] = value;
            }
        }

        private static void ApplyModifiers(
            IDictionary<string, float> stats,
            IReadOnlyList<CombatStatModifier> modifiers)
        {
            if (modifiers == null)
            {
                return;
            }

            foreach (var modifier in modifiers)
            {
                var statId = modifier.ResolveStatId();
                if (string.IsNullOrWhiteSpace(statId))
                {
                    continue;
                }

                if (!stats.TryGetValue(statId, out var value))
                {
                    value = 0f;
                }

                value += modifier.FlatModifier;
                value *= 1f + modifier.PercentModifier;
                stats[statId] = value;
            }
        }

        private static Dictionary<string, float> EvaluateDerivedStats(
            StatRegistry registry,
            IReadOnlyDictionary<string, float> baseStats)
        {
            var derivedStats = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            if (registry == null || registry.StatDefinitions == null)
            {
                return derivedStats;
            }

            foreach (var stat in registry.StatDefinitions)
            {
                if (stat == null || stat.Ratios == null || stat.Ratios.Count == 0)
                {
                    continue;
                }

                var value = ResolveDerivedValue(stat, registry, baseStats, derivedStats, new HashSet<string>());
                derivedStats[stat.Guid] = value;
            }

            return derivedStats;
        }

        private static float ResolveDerivedValue(
            StatDefinition stat,
            StatRegistry registry,
            IReadOnlyDictionary<string, float> baseStats,
            IDictionary<string, float> derivedStats,
            HashSet<string> visited)
        {
            if (stat == null || string.IsNullOrWhiteSpace(stat.Guid))
            {
                return 0f;
            }

            if (derivedStats.TryGetValue(stat.Guid, out var cached))
            {
                return cached;
            }

            if (!visited.Add(stat.Guid))
            {
                return 0f;
            }

            var total = 0f;
            foreach (var ratio in stat.Ratios)
            {
                if (ratio == null || ratio.SourceStat == null || string.IsNullOrWhiteSpace(ratio.SourceStat.Guid))
                {
                    continue;
                }

                var sourceId = ratio.SourceStat.Guid;
                if (!baseStats.TryGetValue(sourceId, out var sourceValue))
                {
                    if (registry != null && registry.TryGetStat(sourceId, out var sourceStat) &&
                        sourceStat != null && sourceStat.Ratios != null && sourceStat.Ratios.Count > 0)
                    {
                        sourceValue = ResolveDerivedValue(sourceStat, registry, baseStats, derivedStats, visited);
                    }
                }

                total += sourceValue * ratio.Ratio;
            }

            derivedStats[stat.Guid] = total;
            return total;
        }

        private static Dictionary<string, float> MergeFinalStats(
            IReadOnlyDictionary<string, float> baseStats,
            IReadOnlyDictionary<string, float> derivedStats)
        {
            var finalStats = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            if (baseStats != null)
            {
                foreach (var pair in baseStats)
                {
                    finalStats[pair.Key] = pair.Value;
                }
            }

            if (derivedStats != null)
            {
                foreach (var pair in derivedStats)
                {
                    if (finalStats.TryGetValue(pair.Key, out var existing))
                    {
                        finalStats[pair.Key] = existing + pair.Value;
                    }
                    else
                    {
                        finalStats[pair.Key] = pair.Value;
                    }
                }
            }

            return finalStats;
        }
    }
}
