using System;
using Realm.Data;
using UnityEngine;

namespace Client.Combat.Stats
{
    public enum CombatStatModifierSource
    {
        Equipment,
        Buff,
        Debuff,
        Other
    }

    [Serializable]
    public struct CombatStatModifier
    {
        [SerializeField] private StatDefinition stat;
        [SerializeField] private string statId;
        [SerializeField] private CombatStatModifierSource source;
        [SerializeField] private float flatModifier;
        [SerializeField] private float percentModifier;

        public StatDefinition Stat => stat;
        public string StatId => statId;
        public CombatStatModifierSource Source => source;
        public float FlatModifier => flatModifier;
        public float PercentModifier => percentModifier;

        public string ResolveStatId()
        {
            if (stat != null && !string.IsNullOrWhiteSpace(stat.Guid))
            {
                return stat.Guid;
            }

            return string.IsNullOrWhiteSpace(statId) ? string.Empty : statId.Trim();
        }
    }
}
