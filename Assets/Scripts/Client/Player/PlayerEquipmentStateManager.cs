using System;
using System.Collections.Generic;
using Client.Equipment;
using Client.Progression;
using Realm.Data;
using UnityEngine;

namespace Client.Player
{
    public static class PlayerEquipmentStateManager
    {
        private static readonly Dictionary<string, Dictionary<string, Dictionary<EquipmentSlot, EquipmentDefinition>>>
            EquipmentByCharacterAndClass = new(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, EquipmentDefinition> _equipmentLookup;
        private static bool _equipmentLookupLoaded;

        private const string DefaultWeaponCatalogResourcePath = "Equipment/DefaultWeaponCatalog";
        private static bool _initialized;
        private static string _currentCharacterId;
        private static string _currentClassId;
        private static DefaultWeaponCatalog _defaultWeaponCatalog;
        private static bool _defaultWeaponCatalogLoaded;

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

            if (TryGetEquipmentForActiveClass(out var equipped) &&
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

            var equipped = GetOrCreateEquipmentForCharacterClass(_currentCharacterId, classId);
            equipped[equipment.Slot] = equipment;
            EquipmentChanged?.Invoke(equipment.Slot, equipment);
            PersistClassEquipment(classId);
            return true;
        }

        public static void Unequip(EquipmentSlot slot)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(_currentCharacterId))
            {
                return;
            }

            if (TryGetEquipmentForActiveClass(out var equipped) &&
                equipped.Remove(slot))
            {
                EquipmentChanged?.Invoke(slot, null);
                PersistClassEquipment(_currentClassId);
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
            CharacterProgressionCache.ProgressionSnapshotChanged += OnProgressionSnapshotChanged;
            EquipmentRepository.EquipmentChanged += OnEquipmentRepositoryChanged;

            if (!string.IsNullOrWhiteSpace(SessionManager.SelectedCharacterId))
            {
                _currentCharacterId = SessionManager.SelectedCharacterId;
                _currentClassId = PlayerClassStateManager.ActiveClassId;
                EnsureEquipmentCompatibility(_currentClassId);
            }
        }

        private static void OnSelectedCharacterChanged(string characterId)
        {
            EnsureInitialized();

            _currentCharacterId = string.IsNullOrWhiteSpace(characterId) ? null : characterId;
            _currentClassId = PlayerClassStateManager.ActiveClassId;
            EnsureEquipmentCompatibility(_currentClassId);
            EnsureDefaultWeaponEquipped(_currentClassId);
            BroadcastEquipmentSnapshot();
        }

        private static void OnSessionCleared()
        {
            EnsureInitialized();

            EquipmentByCharacterAndClass.Clear();
            _currentCharacterId = null;
            _currentClassId = null;
        }

        private static void OnActiveClassChanged(string classId)
        {
            EnsureInitialized();
            _currentClassId = NormalizeClassId(classId);
            EnsureEquipmentCompatibility(_currentClassId);
            EnsureDefaultWeaponEquipped(_currentClassId);
            BroadcastEquipmentSnapshot();
        }

        private static void OnProgressionSnapshotChanged(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            if (CharacterProgressionCache.TryGet(characterId, out var snapshot) && snapshot?.equipment?.items != null)
            {
                EquipmentRepository.ApplyEquipmentState(characterId, snapshot.equipment);
            }
        }

        private static void OnEquipmentRepositoryChanged(string characterId, CharacterEquipmentEntry[] entries)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            ApplyEquipmentEntries(characterId, entries);
        }

        private static void EnsureEquipmentCompatibility(string classId)
        {
            if (string.IsNullOrWhiteSpace(_currentCharacterId) ||
                string.IsNullOrWhiteSpace(classId) ||
                !TryGetEquipmentForClass(_currentCharacterId, classId, out var equipped) ||
                equipped.Count == 0)
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

        private static void EnsureDefaultWeaponEquipped(string classId)
        {
            if (string.IsNullOrWhiteSpace(classId) || string.IsNullOrWhiteSpace(_currentCharacterId))
            {
                return;
            }

            if (GetEquippedItem(EquipmentSlot.Weapon) != null)
            {
                return;
            }

            if (TryGetDefaultWeapon(classId, out var weapon))
            {
                TryEquip(weapon);
            }
        }

        private static bool TryGetDefaultWeapon(string classId, out WeaponDefinition weapon, bool allowSeedFallback = false)
        {
            weapon = null;
            if (!_defaultWeaponCatalogLoaded)
            {
                _defaultWeaponCatalog = Resources.Load<DefaultWeaponCatalog>(DefaultWeaponCatalogResourcePath);
                _defaultWeaponCatalogLoaded = true;
            }

            if (_defaultWeaponCatalog != null)
            {
                if (_defaultWeaponCatalog.TryGetDefaultWeapon(classId, out weapon))
                {
                    return true;
                }

                Debug.LogWarning($"Default weapon catalog has no weapon entry for class '{classId}'.");
                return allowSeedFallback && DefaultWeaponSeedLibrary.TryCreateDefaultWeapon(classId, out weapon);
            }

            Debug.LogWarning("Default weapon catalog asset was not found. Assign a DefaultWeaponCatalog asset in Resources/Equipment to enable class defaults.");
            return allowSeedFallback && DefaultWeaponSeedLibrary.TryCreateDefaultWeapon(classId, out weapon);
        }

        private static Dictionary<EquipmentSlot, EquipmentDefinition> GetOrCreateEquipmentForCharacterClass(
            string characterId,
            string classId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(classId))
            {
                return new Dictionary<EquipmentSlot, EquipmentDefinition>();
            }

            if (!EquipmentByCharacterAndClass.TryGetValue(characterId, out var byClass))
            {
                byClass = new Dictionary<string, Dictionary<EquipmentSlot, EquipmentDefinition>>(StringComparer.OrdinalIgnoreCase);
                EquipmentByCharacterAndClass[characterId] = byClass;
            }

            if (!byClass.TryGetValue(classId, out var equipped))
            {
                equipped = new Dictionary<EquipmentSlot, EquipmentDefinition>();
                byClass[classId] = equipped;
            }

            return equipped;
        }

        private static bool TryGetEquipmentForActiveClass(out Dictionary<EquipmentSlot, EquipmentDefinition> equipped)
        {
            equipped = null;
            if (string.IsNullOrWhiteSpace(_currentCharacterId) || string.IsNullOrWhiteSpace(_currentClassId))
            {
                return false;
            }

            return TryGetEquipmentForClass(_currentCharacterId, _currentClassId, out equipped);
        }

        private static bool TryGetEquipmentForClass(
            string characterId,
            string classId,
            out Dictionary<EquipmentSlot, EquipmentDefinition> equipped)
        {
            equipped = null;
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(classId))
            {
                return false;
            }

            return EquipmentByCharacterAndClass.TryGetValue(characterId, out var byClass)
                   && byClass.TryGetValue(classId, out equipped);
        }

        private static void ApplyEquipmentEntries(string characterId, CharacterEquipmentEntry[] entries)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            if (!EquipmentByCharacterAndClass.TryGetValue(characterId, out var byClass))
            {
                byClass = new Dictionary<string, Dictionary<EquipmentSlot, EquipmentDefinition>>(StringComparer.OrdinalIgnoreCase);
                EquipmentByCharacterAndClass[characterId] = byClass;
            }
            else
            {
                byClass.Clear();
            }

            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.classId) || string.IsNullOrWhiteSpace(entry.slot))
                    {
                        continue;
                    }

                    if (!TryParseSlot(entry.slot, out var slot))
                    {
                        continue;
                    }

                    if (!TryResolveEquipment(entry.itemId, out var equipment))
                    {
                        continue;
                    }

                    var classId = entry.classId.Trim();
                    if (!byClass.TryGetValue(classId, out var equipped))
                    {
                        equipped = new Dictionary<EquipmentSlot, EquipmentDefinition>();
                        byClass[classId] = equipped;
                    }

                    equipped[slot] = equipment;
                }
            }

            if (string.Equals(characterId, _currentCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                EnsureEquipmentCompatibility(_currentClassId);
                EnsureDefaultWeaponEquipped(_currentClassId);
                BroadcastEquipmentSnapshot();
            }
        }

        private static void PersistClassEquipment(string classId)
        {
            if (string.IsNullOrWhiteSpace(_currentCharacterId) || string.IsNullOrWhiteSpace(classId))
            {
                return;
            }

            if (!TryGetEquipmentForClass(_currentCharacterId, classId, out var equipped))
            {
                equipped = new Dictionary<EquipmentSlot, EquipmentDefinition>();
            }

            var entries = BuildEquipmentEntries(classId, equipped);
            ProgressionCoroutineRunner.Run(
                EquipmentRepository.ReplaceClassEquipmentAsync(
                    _currentCharacterId,
                    classId,
                    entries,
                    _ => { },
                    _ => { })
            );
        }

        private static CharacterEquipmentEntry[] BuildEquipmentEntries(
            string classId,
            Dictionary<EquipmentSlot, EquipmentDefinition> equipped)
        {
            if (string.IsNullOrWhiteSpace(classId) || equipped == null || equipped.Count == 0)
            {
                return Array.Empty<CharacterEquipmentEntry>();
            }

            var entries = new List<CharacterEquipmentEntry>();
            foreach (var kvp in equipped)
            {
                if (kvp.Value == null || string.IsNullOrWhiteSpace(kvp.Value.Guid))
                {
                    continue;
                }

                entries.Add(new CharacterEquipmentEntry
                {
                    classId = classId,
                    slot = kvp.Key.ToString().ToLowerInvariant(),
                    itemId = kvp.Value.Guid,
                    metadataJson = "{}"
                });
            }

            return entries.ToArray();
        }

        private static bool TryParseSlot(string slot, out EquipmentSlot parsed)
        {
            parsed = EquipmentSlot.Weapon;
            if (string.IsNullOrWhiteSpace(slot))
            {
                return false;
            }

            return Enum.TryParse(slot, true, out parsed);
        }

        private static bool TryResolveEquipment(string itemId, out EquipmentDefinition equipment)
        {
            equipment = null;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            EnsureEquipmentLookup();
            if (_equipmentLookup != null && _equipmentLookup.TryGetValue(itemId.Trim(), out var definition))
            {
                equipment = definition;
                return true;
            }

            return false;
        }

        private static void EnsureEquipmentLookup()
        {
            if (_equipmentLookupLoaded)
            {
                return;
            }

            _equipmentLookupLoaded = true;
            _equipmentLookup = new Dictionary<string, EquipmentDefinition>(StringComparer.OrdinalIgnoreCase);

#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:EquipmentDefinition");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<EquipmentDefinition>(path);
                RegisterEquipment(asset);
            }
#endif

            var resources = Resources.LoadAll<EquipmentDefinition>(string.Empty);
            if (resources != null)
            {
                foreach (var asset in resources)
                {
                    RegisterEquipment(asset);
                }
            }
        }

        private static void RegisterEquipment(EquipmentDefinition equipment)
        {
            if (equipment == null || string.IsNullOrWhiteSpace(equipment.Guid))
            {
                return;
            }

            _equipmentLookup.TryAdd(equipment.Guid.Trim(), equipment);
        }

        private static void BroadcastEquipmentSnapshot()
        {
            if (string.IsNullOrWhiteSpace(_currentCharacterId) || string.IsNullOrWhiteSpace(_currentClassId))
            {
                return;
            }

            var equipped = GetOrCreateEquipmentForCharacterClass(_currentCharacterId, _currentClassId);
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                equipped.TryGetValue(slot, out var item);
                EquipmentChanged?.Invoke(slot, item);
            }
        }

        private static string NormalizeClassId(string classId)
        {
            return string.IsNullOrWhiteSpace(classId) ? null : classId.Trim();
        }
    }
}
