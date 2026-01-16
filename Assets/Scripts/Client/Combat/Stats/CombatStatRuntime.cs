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
        private bool _isDirty = true;

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
                MarkDirty();
            }
        }

        public void RefreshStats()
        {
            _isDirty = false;
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
            EnsureSnapshot();
            if (_snapshot == null || string.IsNullOrWhiteSpace(statId))
            {
                return false;
            }

            value = _snapshot.GetStat(statId, 0f);
            return true;
        }

        public float GetStatOrDefault(string statId, float fallback = 0f)
        {
            EnsureSnapshot();
            if (_snapshot == null)
            {
                return fallback;
            }

            return _snapshot.GetStat(statId, fallback);
        }

        public void SetLevel(int newLevel)
        {
            var clamped = Mathf.Max(1, newLevel);
            if (level == clamped)
            {
                return;
            }

            level = clamped;
            MarkDirty();
        }

        public void SetClassDefinition(ClassDefinition newClass)
        {
            if (classDefinition == newClass)
            {
                return;
            }

            classDefinition = newClass;
            MarkDirty();
        }

        public void SetStatProfileOverride(StatProfileDefinition profile)
        {
            if (statProfileOverride == profile)
            {
                return;
            }

            statProfileOverride = profile;
            MarkDirty();
        }

        public void AddModifier(CombatStatModifier modifier)
        {
            modifiers ??= new List<CombatStatModifier>();
            modifiers.Add(modifier);
            MarkDirty();
        }

        public bool RemoveModifier(CombatStatModifier modifier)
        {
            if (modifiers == null)
            {
                return false;
            }

            var removed = modifiers.Remove(modifier);
            if (removed)
            {
                MarkDirty();
            }

            return removed;
        }

        public void ClearModifiers()
        {
            if (modifiers == null || modifiers.Count == 0)
            {
                return;
            }

            modifiers.Clear();
            MarkDirty();
        }

        public void MarkDirty()
        {
            _isDirty = true;
            if (autoRefresh && Application.isPlaying)
            {
                RefreshStats();
            }
        }

        private void EnsureSnapshot()
        {
            if (!_isDirty)
            {
                return;
            }

            if (autoRefresh && Application.isPlaying)
            {
                RefreshStats();
            }
        }
    }
}
