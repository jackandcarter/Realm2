using System;
using System.Collections.Generic;
using Client.Combat.Stats;
using Realm.Combat.Data;
using UnityEngine;

namespace Client.Combat.StatusEffects
{
    [DisallowMultipleComponent]
    public class CombatStatusRuntime : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CombatEntity combatEntity;
        [SerializeField] private CombatStatRuntime statRuntime;

        [Header("Runtime")]
        [SerializeField] private bool autoTick = true;

        private readonly Dictionary<string, StatusRuntimeEntry> _activeStatuses =
            new(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindCombatEntity();
        }

        private void OnDisable()
        {
            UnbindCombatEntity();
            ClearAllStatuses();
        }

        private void Update()
        {
            if (!autoTick)
            {
                return;
            }

            Tick(Time.time);
        }

        public void Tick(float now)
        {
            if (_activeStatuses.Count == 0)
            {
                return;
            }

            var expired = ListPool<string>.Get();
            try
            {
                foreach (var entry in _activeStatuses.Values)
                {
                    if (entry.ExpiresAt.HasValue && now >= entry.ExpiresAt.Value)
                    {
                        expired.Add(entry.StatusId);
                    }
                }

                foreach (var statusId in expired)
                {
                    RemoveStatus(statusId);
                }
            }
            finally
            {
                ListPool<string>.Release(expired);
            }
        }

        public void ApplyStatus(string statusId, StatusEffectType type, float durationSeconds, float magnitude)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                return;
            }

            statusId = statusId.Trim();
            var definition = StatusEffectRegistry.GetStatus(statusId);
            var refreshRule = definition != null ? definition.RefreshRule : StatusRefreshRule.RefreshDuration;
            var maxStacks = definition != null ? Mathf.Max(1, definition.MaxStacks) : 1;

            if (!_activeStatuses.TryGetValue(statusId, out var entry))
            {
                entry = new StatusRuntimeEntry
                {
                    StatusId = statusId,
                    Definition = definition,
                    Type = definition != null ? definition.Type : type,
                    RefreshRule = refreshRule,
                    MaxStacks = maxStacks
                };
                _activeStatuses[statusId] = entry;
            }
            else
            {
                entry.Definition = definition;
                entry.Type = definition != null ? definition.Type : type;
                entry.RefreshRule = refreshRule;
                entry.MaxStacks = maxStacks;
            }

            var now = Time.time;
            var normalizedMagnitude = Mathf.Approximately(magnitude, 0f) ? 1f : magnitude;
            var shouldReapply = !Mathf.Approximately(entry.LastMagnitude, normalizedMagnitude);

            switch (entry.RefreshRule)
            {
                case StatusRefreshRule.RefreshDuration:
                    shouldReapply = shouldReapply || entry.Stacks == 0;
                    entry.Stacks = Mathf.Clamp(entry.Stacks, 1, entry.MaxStacks);
                    entry.ExpiresAt = ResolveExpiry(now, durationSeconds);
                    break;
                case StatusRefreshRule.AddStacks:
                    if (entry.Stacks < entry.MaxStacks)
                    {
                        entry.Stacks++;
                        shouldReapply = true;
                    }
                    entry.ExpiresAt = ResolveExpiry(now, durationSeconds);
                    break;
                case StatusRefreshRule.Ignore:
                    if (entry.Stacks == 0)
                    {
                        entry.Stacks = 1;
                        shouldReapply = true;
                        entry.ExpiresAt = ResolveExpiry(now, durationSeconds);
                    }
                    break;
            }

            entry.LastMagnitude = normalizedMagnitude;
            if (entry.Stacks > 0 && (shouldReapply || entry.AppliedModifiers.Count == 0))
            {
                ReapplyModifiers(entry);
            }
        }

        public void RemoveStatus(string statusId)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                return;
            }

            if (!_activeStatuses.TryGetValue(statusId, out var entry))
            {
                return;
            }

            RemoveModifiers(entry);
            _activeStatuses.Remove(statusId);
        }

        public void ClearAllStatuses()
        {
            foreach (var entry in _activeStatuses.Values)
            {
                RemoveModifiers(entry);
            }

            _activeStatuses.Clear();
        }

        private void HandleBuffApplied(string buffName, float durationSeconds, float magnitude)
        {
            ApplyStatus(buffName, StatusEffectType.Buff, durationSeconds, magnitude);
        }

        private void HandleDebuffApplied(string debuffName, float durationSeconds, float magnitude)
        {
            ApplyStatus(debuffName, StatusEffectType.Debuff, durationSeconds, magnitude);
        }

        private void HandleStateChanged(string stateName, float durationSeconds)
        {
            ApplyStatus(stateName, StatusEffectType.CrowdControl, durationSeconds, 1f);
        }

        private void BindCombatEntity()
        {
            if (combatEntity == null)
            {
                return;
            }

            combatEntity.BuffApplied -= HandleBuffApplied;
            combatEntity.BuffApplied += HandleBuffApplied;
            combatEntity.DebuffApplied -= HandleDebuffApplied;
            combatEntity.DebuffApplied += HandleDebuffApplied;
            combatEntity.StateChanged -= HandleStateChanged;
            combatEntity.StateChanged += HandleStateChanged;
        }

        private void UnbindCombatEntity()
        {
            if (combatEntity == null)
            {
                return;
            }

            combatEntity.BuffApplied -= HandleBuffApplied;
            combatEntity.DebuffApplied -= HandleDebuffApplied;
            combatEntity.StateChanged -= HandleStateChanged;
        }

        private void ResolveReferences()
        {
            if (combatEntity == null)
            {
                combatEntity = GetComponent<CombatEntity>();
            }

            if (statRuntime == null)
            {
                statRuntime = GetComponent<CombatStatRuntime>();
            }
        }

        private static float? ResolveExpiry(float now, float durationSeconds)
        {
            return durationSeconds > 0f ? now + durationSeconds : null;
        }

        private void ReapplyModifiers(StatusRuntimeEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            RemoveModifiers(entry);

            if (statRuntime == null || entry.Definition == null || entry.Definition.Modifiers == null)
            {
                return;
            }

            var scalar = Mathf.Approximately(entry.LastMagnitude, 0f) ? 1f : entry.LastMagnitude;
            var source = ResolveSource(entry.Type);
            for (var i = 0; i < entry.Stacks; i++)
            {
                foreach (var modifierDefinition in entry.Definition.Modifiers)
                {
                    if (modifierDefinition == null || string.IsNullOrWhiteSpace(modifierDefinition.StatId))
                    {
                        continue;
                    }

                    var modifier = new CombatStatModifier(
                        modifierDefinition.StatId.Trim(),
                        source,
                        modifierDefinition.Value * scalar);
                    entry.AppliedModifiers.Add(modifier);
                    statRuntime.AddModifier(modifier);
                }
            }
        }

        private void RemoveModifiers(StatusRuntimeEntry entry)
        {
            if (statRuntime == null || entry == null || entry.AppliedModifiers.Count == 0)
            {
                entry?.AppliedModifiers.Clear();
                return;
            }

            foreach (var modifier in entry.AppliedModifiers)
            {
                statRuntime.RemoveModifier(modifier);
            }

            entry.AppliedModifiers.Clear();
        }

        private static CombatStatModifierSource ResolveSource(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Buff => CombatStatModifierSource.Buff,
                StatusEffectType.Debuff => CombatStatModifierSource.Debuff,
                _ => CombatStatModifierSource.Other
            };
        }

        private class StatusRuntimeEntry
        {
            public string StatusId;
            public StatusEffectDefinition Definition;
            public StatusEffectType Type;
            public StatusRefreshRule RefreshRule;
            public int MaxStacks;
            public int Stacks;
            public float LastMagnitude;
            public float? ExpiresAt;
            public readonly List<CombatStatModifier> AppliedModifiers = new();
        }
    }

    internal static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new();

        public static List<T> Get()
        {
            return Pool.Count > 0 ? Pool.Pop() : new List<T>();
        }

        public static void Release(List<T> list)
        {
            if (list == null)
            {
                return;
            }

            list.Clear();
            Pool.Push(list);
        }
    }
}
