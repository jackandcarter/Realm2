using System;
using Realm.Data;
using UnityEngine;

namespace Client.Combat.Stats
{
    [Serializable]
    public struct CombatStatModifier
    {
        [SerializeField] private StatDefinition stat;
        [SerializeField] private string statId;
        [SerializeField] private float flatModifier;
        [SerializeField] private float percentModifier;

        public StatDefinition Stat => stat;
        public string StatId => statId;
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
