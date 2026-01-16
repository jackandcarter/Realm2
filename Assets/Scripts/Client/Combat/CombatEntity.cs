using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.Combat
{
    public enum CombatTeam
    {
        Neutral = 0,
        Ally = 1,
        Enemy = 2
    }

    [DisallowMultipleComponent]
    public class CombatEntity : MonoBehaviour
    {
        [SerializeField] private string entityId;
        [SerializeField] private CombatTeam team = CombatTeam.Neutral;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private Transform aimOrigin;
        [SerializeField] private MonoBehaviour statsProvider;

        private ICombatStatProvider _statsProvider;

        public string EntityId => entityId;
        public CombatTeam Team => team;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public Transform AimOrigin => aimOrigin != null ? aimOrigin : transform;
        public Vector3 Position => AimOrigin != null ? AimOrigin.position : transform.position;

        public event Action<float, float> HealthChanged;

        private void Awake()
        {
            ResolveStatProvider();
            EnsureEntityId();
            ClampHealth();
        }

        private void OnEnable()
        {
            ResolveStatProvider();
            EnsureEntityId();
            ClampHealth();
            CombatEntityRegistry.Register(this);
        }

        private void OnDisable()
        {
            CombatEntityRegistry.Unregister(this);
        }

        private void OnValidate()
        {
            if (statsProvider != null && statsProvider is not ICombatStatProvider)
            {
                statsProvider = null;
            }

            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            EnsureEntityId();
        }

        public bool TryGetStat(string statId, out float value)
        {
            if (_statsProvider == null)
            {
                value = 0f;
                return false;
            }

            return _statsProvider.TryGetStat(statId, out value);
        }

        public float GetStatOrDefault(string statId, float fallback = 0f)
        {
            return TryGetStat(statId, out var value) ? value : fallback;
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            var newHealth = Mathf.Max(0f, currentHealth - amount);
            if (!Mathf.Approximately(newHealth, currentHealth))
            {
                currentHealth = newHealth;
                HealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }

        public void ApplyHeal(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            var newHealth = Mathf.Min(maxHealth, currentHealth + amount);
            if (!Mathf.Approximately(newHealth, currentHealth))
            {
                currentHealth = newHealth;
                HealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }

        private void ResolveStatProvider()
        {
            if (statsProvider != null && statsProvider is ICombatStatProvider provider)
            {
                _statsProvider = provider;
                return;
            }

            _statsProvider = GetComponent<ICombatStatProvider>();
        }

        private void ClampHealth()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        private void EnsureEntityId()
        {
            if (!string.IsNullOrWhiteSpace(entityId))
            {
                entityId = entityId.Trim();
                return;
            }

            entityId = Guid.NewGuid().ToString("N");
        }
    }

    public static class CombatEntityRegistry
    {
        private static readonly HashSet<CombatEntity> Entities = new();

        public static IReadOnlyCollection<CombatEntity> All => Entities;

        public static void Register(CombatEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            Entities.Add(entity);
        }

        public static void Unregister(CombatEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            Entities.Remove(entity);
        }
    }
}
