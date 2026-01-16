using System.Collections.Generic;

namespace Client.Combat.Stats
{
    public sealed class CombatStatSnapshot
    {
        public IReadOnlyDictionary<string, float> BaseStats { get; }
        public IReadOnlyDictionary<string, float> DerivedStats { get; }
        public IReadOnlyDictionary<string, float> FinalStats { get; }

        public CombatStatSnapshot(
            IReadOnlyDictionary<string, float> baseStats,
            IReadOnlyDictionary<string, float> derivedStats,
            IReadOnlyDictionary<string, float> finalStats)
        {
            BaseStats = baseStats;
            DerivedStats = derivedStats;
            FinalStats = finalStats;
        }

        public float GetStat(string statId, float fallback = 0f)
        {
            if (string.IsNullOrWhiteSpace(statId))
            {
                return fallback;
            }

            return FinalStats != null && FinalStats.TryGetValue(statId, out var value) ? value : fallback;
        }
    }
}
