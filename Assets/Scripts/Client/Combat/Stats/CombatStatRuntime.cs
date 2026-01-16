using System;
using System.Collections.Generic;
using Realm.Data;
using UnityEngine;

namespace Client.Combat.Stats
{
    [DisallowMultipleComponent]
    public class CombatStatRuntime : MonoBehaviour, ICombatStatProvider
    {
        [Header("Definitions")]
        [SerializeField] private StatRegistry statRegistry;
        [SerializeField] private ClassDefinition classDefinition;
        [SerializeField] private StatProfileDefinition statProfileOverride;

        [Header("Runtime")]
        [SerializeField] private int level = 1;
        [SerializeField] private List<CombatStatModifier> modifiers = new();
        [SerializeField] private bool autoRefresh = true;
        [SerializeField, Range(0f, 1f)] private float normalizedRandom = 0.5f;

        private readonly CombatStatCalculator _calculator = new();
        private CombatStatSnapshot _snapshot;

        public event Action<CombatStatSnapshot> StatsRefreshed;

        private void Awake()
        {
            if (autoRefresh)
            {
                RefreshStats();
            }
        }

        private void OnEnable()
        {
            if (autoRefresh)
            {
                RefreshStats();
            }
        }

        private void OnValidate()
        {
            level = Mathf.Max(1, level);
            normalizedRandom = Mathf.Clamp01(normalizedRandom);
            if (autoRefresh && Application.isPlaying)
            {
                RefreshStats();
            }
        }

        public void RefreshStats()
        {
            var profile = statProfileOverride != null
                ? statProfileOverride
                : classDefinition != null
                    ? classDefinition.StatProfile
                    : null;

            _snapshot = _calculator.Calculate(
                statRegistry,
                classDefinition,
                profile,
                level,
                modifiers,
                normalizedRandom);

            StatsRefreshed?.Invoke(_snapshot);
        }

        public bool TryGetStat(string statId, out float value)
        {
            value = 0f;
            if (_snapshot == null || string.IsNullOrWhiteSpace(statId))
            {
                return false;
            }

            value = _snapshot.GetStat(statId, 0f);
            return true;
        }

        public float GetStatOrDefault(string statId, float fallback = 0f)
        {
            if (_snapshot == null)
            {
                return fallback;
            }

            return _snapshot.GetStat(statId, fallback);
        }
    }
}
