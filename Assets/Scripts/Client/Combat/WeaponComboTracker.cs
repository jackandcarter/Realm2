using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.Combat
{
    [DisallowMultipleComponent]
    public class WeaponComboTracker : MonoBehaviour
    {
        [Header("Definitions")]
        [SerializeField] private List<WeaponComboDefinition> comboDefinitions = new();
        [SerializeField] private string equippedWeaponId;

        [Header("Timing")]
        [SerializeField] private float comboInputTimeoutSeconds = 1.25f;
        [SerializeField] private float specialReadyTimeoutSeconds = 2f;
        [SerializeField] private float specialCooldownSeconds = 4f;

        private readonly List<WeaponComboInputType> _inputBuffer = new();
        private WeaponComboDefinition _equippedDefinition;
        private float _lastInputTime = Mathf.NegativeInfinity;
        private float _specialReadyExpiresAt = Mathf.NegativeInfinity;
        private float _cooldownEndsAt = Mathf.NegativeInfinity;

        public event Action<bool> SpecialReadyChanged;

        public bool IsSpecialReady { get; private set; }
        public float SpecialCooldownSeconds => specialCooldownSeconds;
        public float SpecialCooldownRemaining => Mathf.Max(0f, _cooldownEndsAt - Time.unscaledTime);
        public WeaponComboDefinition EquippedDefinition => _equippedDefinition;
        public string CurrentSpecialAttackAbilityId =>
            _equippedDefinition != null ? _equippedDefinition.SpecialAttackAbilityId : string.Empty;

        private void Awake()
        {
            ResolveEquippedDefinition();
        }

        private void Update()
        {
            if (IsSpecialReady && Time.unscaledTime >= _specialReadyExpiresAt)
            {
                ResetSpecialReady();
            }
        }

        private void OnValidate()
        {
            comboInputTimeoutSeconds = Mathf.Max(0f, comboInputTimeoutSeconds);
            specialReadyTimeoutSeconds = Mathf.Max(0f, specialReadyTimeoutSeconds);
            specialCooldownSeconds = Mathf.Max(0f, specialCooldownSeconds);
        }

        public void SetEquippedWeaponId(string weaponId)
        {
            equippedWeaponId = weaponId;
            ResolveEquippedDefinition();
            ClearComboState();
        }

        public void RegisterAttackInput(WeaponComboInputType inputType)
        {
            if (_equippedDefinition == null || _equippedDefinition.ComboSequence.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (now < _cooldownEndsAt)
            {
                return;
            }

            if (now - _lastInputTime > comboInputTimeoutSeconds)
            {
                _inputBuffer.Clear();
            }

            _lastInputTime = now;
            _inputBuffer.Add(inputType);

            var comboLength = _equippedDefinition.ComboSequence.Count;
            if (_inputBuffer.Count > comboLength)
            {
                _inputBuffer.RemoveRange(0, _inputBuffer.Count - comboLength);
            }

            if (MatchesCombo(_equippedDefinition.ComboSequence, _inputBuffer))
            {
                SetSpecialReady(now);
            }
        }

        public bool TryConsumeSpecialReady()
        {
            if (!IsSpecialReady)
            {
                return false;
            }

            ResetSpecialReady();
            _cooldownEndsAt = Time.unscaledTime + specialCooldownSeconds;
            _inputBuffer.Clear();
            return true;
        }

        private void ResolveEquippedDefinition()
        {
            _equippedDefinition = null;
            if (comboDefinitions == null || comboDefinitions.Count == 0)
            {
                return;
            }

            if (comboDefinitions.Count == 1 && string.IsNullOrWhiteSpace(equippedWeaponId))
            {
                _equippedDefinition = comboDefinitions[0];
                return;
            }

            foreach (var definition in comboDefinitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.WeaponId))
                {
                    continue;
                }

                if (string.Equals(definition.WeaponId, equippedWeaponId, StringComparison.OrdinalIgnoreCase))
                {
                    _equippedDefinition = definition;
                    return;
                }
            }
        }

        private void SetSpecialReady(float now)
        {
            _specialReadyExpiresAt = now + specialReadyTimeoutSeconds;
            if (IsSpecialReady)
            {
                return;
            }

            IsSpecialReady = true;
            SpecialReadyChanged?.Invoke(true);
        }

        private void ResetSpecialReady()
        {
            if (!IsSpecialReady)
            {
                return;
            }

            IsSpecialReady = false;
            _specialReadyExpiresAt = Mathf.NegativeInfinity;
            SpecialReadyChanged?.Invoke(false);
        }

        private void ClearComboState()
        {
            _inputBuffer.Clear();
            _lastInputTime = Mathf.NegativeInfinity;
            ResetSpecialReady();
        }

        private static bool MatchesCombo(
            IReadOnlyList<WeaponComboInputType> combo,
            IReadOnlyList<WeaponComboInputType> buffer)
        {
            if (combo.Count != buffer.Count)
            {
                return false;
            }

            for (var i = 0; i < combo.Count; i++)
            {
                if (combo[i] != buffer[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
