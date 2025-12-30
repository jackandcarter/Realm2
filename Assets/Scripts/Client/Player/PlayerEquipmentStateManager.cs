using System;
using System.Collections.Generic;
using Realm.Data;

namespace Client.Player
{
    public static class PlayerEquipmentStateManager
    {
        private static readonly Dictionary<string, Dictionary<EquipmentSlot, EquipmentDefinition>> EquipmentByCharacter =
            new(StringComparer.OrdinalIgnoreCase);

        private static bool _initialized;
        private static string _currentCharacterId;

        public static event Action<EquipmentSlot, EquipmentDefinition> EquipmentChanged;
        public static event Action<string, IReadOnlyList<EquipmentSlot>> EquipmentRestrictionsViolated;
        public static event Action<string> WeaponSelectionRequired;

        public static EquipmentDefinition GetEquippedItem(EquipmentSlot slot)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(_currentCharacterId))
            {
                return null;
            }

            if (EquipmentByCharacter.TryGetValue(_currentCharacterId, out var equipped) &&
                equipped.TryGetValue(slot, out var item))
            {
                return item;
            }

            return null;
        }

        public static bool TryEquip(EquipmentDefinition equipment)
        {
            EnsureInitialized();

            if (equipment == null || string.IsNullOrWhiteSpace(_currentCharacterId))
            {
                return false;
            }

            var classId = PlayerClassStateManager.ActiveClassId;
            if (!EquipmentRestrictionUtility.IsClassAllowedForEquipment(classId, equipment))
            {
                return false;
            }

            var equipped = GetOrCreateEquipmentForCharacter(_currentCharacterId);
            equipped[equipment.Slot] = equipment;
            EquipmentChanged?.Invoke(equipment.Slot, equipment);
            return true;
        }

        public static void Unequip(EquipmentSlot slot)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(_currentCharacterId))
            {
                return;
            }

            if (EquipmentByCharacter.TryGetValue(_currentCharacterId, out var equipped) &&
                equipped.Remove(slot))
            {
                EquipmentChanged?.Invoke(slot, null);
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            SessionManager.SelectedCharacterChanged += OnSelectedCharacterChanged;
            SessionManager.SessionCleared += OnSessionCleared;
            PlayerClassStateManager.ActiveClassChanged += OnActiveClassChanged;

            if (!string.IsNullOrWhiteSpace(SessionManager.SelectedCharacterId))
            {
                _currentCharacterId = SessionManager.SelectedCharacterId;
                EnsureEquipmentCompatibility(PlayerClassStateManager.ActiveClassId);
            }
        }

        private static void OnSelectedCharacterChanged(string characterId)
        {
            EnsureInitialized();

            _currentCharacterId = string.IsNullOrWhiteSpace(characterId) ? null : characterId;
            EnsureEquipmentCompatibility(PlayerClassStateManager.ActiveClassId);
        }

        private static void OnSessionCleared()
        {
            EnsureInitialized();

            EquipmentByCharacter.Clear();
            _currentCharacterId = null;
        }

        private static void OnActiveClassChanged(string classId)
        {
            EnsureInitialized();
            EnsureEquipmentCompatibility(classId);
        }

        private static void EnsureEquipmentCompatibility(string classId)
        {
            if (string.IsNullOrWhiteSpace(_currentCharacterId) ||
                !EquipmentByCharacter.TryGetValue(_currentCharacterId, out var equipped) ||
                equipped.Count == 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(classId))
            {
                return;
            }

            var invalidSlots = new List<EquipmentSlot>();
            var slots = new List<EquipmentSlot>(equipped.Keys);

            foreach (var slot in slots)
            {
                if (!equipped.TryGetValue(slot, out var item) || item == null)
                {
                    continue;
                }

                if (EquipmentRestrictionUtility.IsClassAllowedForEquipment(classId, item))
                {
                    continue;
                }

                equipped.Remove(slot);
                invalidSlots.Add(slot);
                EquipmentChanged?.Invoke(slot, null);
            }

            if (invalidSlots.Count == 0)
            {
                return;
            }

            EquipmentRestrictionsViolated?.Invoke(classId, invalidSlots);

            if (invalidSlots.Contains(EquipmentSlot.Weapon))
            {
                WeaponSelectionRequired?.Invoke(classId);
            }
        }

        private static Dictionary<EquipmentSlot, EquipmentDefinition> GetOrCreateEquipmentForCharacter(string characterId)
        {
            if (!EquipmentByCharacter.TryGetValue(characterId, out var equipped))
            {
                equipped = new Dictionary<EquipmentSlot, EquipmentDefinition>();
                EquipmentByCharacter[characterId] = equipped;
            }

            return equipped;
        }
    }
}
