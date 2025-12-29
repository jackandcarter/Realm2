using System;
using System.Collections.Generic;
using Realm.Abilities;
using UnityEngine;

namespace Realm.Data
{
    [CreateAssetMenu(menuName = "Realm/Stats/Stat Registry", fileName = "StatRegistry")]
    public class StatRegistry : ConfigurationAsset
    {
        [SerializeField]
        private List<StatDefinition> statDefinitions = new();

        [SerializeField]
        private List<StatCategory> categories = new();

        [SerializeField]
        private List<ClassDefinition> classes = new();

        [SerializeField]
        private List<AbilityDefinition> abilities = new();

        [SerializeField]
        private List<WeaponTypeDefinition> weaponTypes = new();

        [SerializeField]
        private List<ArmorTypeDefinition> armorTypes = new();

        [SerializeField]
        private List<WeaponDefinition> weapons = new();

        [SerializeField]
        private List<ArmorDefinition> armors = new();

        private Dictionary<string, StatDefinition> _statsByGuid;
        private Dictionary<string, StatCategory> _categoriesByGuid;
        private Dictionary<string, ClassDefinition> _classesByGuid;
        private Dictionary<string, AbilityDefinition> _abilitiesByGuid;
        private Dictionary<string, WeaponTypeDefinition> _weaponTypesByGuid;
        private Dictionary<string, ArmorTypeDefinition> _armorTypesByGuid;
        private Dictionary<string, WeaponDefinition> _weaponsByGuid;
        private Dictionary<string, ArmorDefinition> _armorsByGuid;

        public IReadOnlyList<StatDefinition> StatDefinitions => statDefinitions;
        public IReadOnlyList<StatCategory> Categories => categories;
        public IReadOnlyList<ClassDefinition> Classes => classes;
        public IReadOnlyList<AbilityDefinition> Abilities => abilities;
        public IReadOnlyList<WeaponTypeDefinition> WeaponTypes => weaponTypes;
        public IReadOnlyList<ArmorTypeDefinition> ArmorTypes => armorTypes;
        public IReadOnlyList<WeaponDefinition> Weapons => weapons;
        public IReadOnlyList<ArmorDefinition> Armors => armors;

        protected override void OnEnable()
        {
            base.OnEnable();
            BuildLookups();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SanitizeList(statDefinitions);
            SanitizeList(categories);
            SanitizeList(classes);
            SanitizeList(abilities);
            SanitizeList(weaponTypes);
            SanitizeList(armorTypes);
            SanitizeList(weapons);
            SanitizeList(armors);
            BuildLookups();
        }

        public void BuildLookups()
        {
            _statsByGuid = BuildLookup(statDefinitions);
            _categoriesByGuid = BuildLookup(categories);
            _classesByGuid = BuildLookup(classes);
            _abilitiesByGuid = BuildLookup(abilities);
            _weaponTypesByGuid = BuildLookup(weaponTypes);
            _armorTypesByGuid = BuildLookup(armorTypes);
            _weaponsByGuid = BuildLookup(weapons);
            _armorsByGuid = BuildLookup(armors);
        }

        public bool TryGetStat(string guid, out StatDefinition definition)
        {
            return TryGetFromLookup(_statsByGuid, guid, out definition);
        }

        public bool TryGetCategory(string guid, out StatCategory category)
        {
            return TryGetFromLookup(_categoriesByGuid, guid, out category);
        }

        public bool TryGetClass(string guid, out ClassDefinition classDefinition)
        {
            return TryGetFromLookup(_classesByGuid, guid, out classDefinition);
        }

        public bool TryGetAbility(string guid, out AbilityDefinition abilityDefinition)
        {
            return TryGetFromLookup(_abilitiesByGuid, guid, out abilityDefinition);
        }

        public bool TryGetWeaponType(string guid, out WeaponTypeDefinition weaponType)
        {
            return TryGetFromLookup(_weaponTypesByGuid, guid, out weaponType);
        }

        public bool TryGetArmorType(string guid, out ArmorTypeDefinition armorType)
        {
            return TryGetFromLookup(_armorTypesByGuid, guid, out armorType);
        }

        public bool TryGetWeapon(string guid, out WeaponDefinition weapon)
        {
            return TryGetFromLookup(_weaponsByGuid, guid, out weapon);
        }

        public bool TryGetArmor(string guid, out ArmorDefinition armor)
        {
            return TryGetFromLookup(_armorsByGuid, guid, out armor);
        }

        public StatDefinition GetStat(string guid)
        {
            return GetFromLookup(_statsByGuid, guid, nameof(StatDefinition));
        }

        public StatCategory GetCategory(string guid)
        {
            return GetFromLookup(_categoriesByGuid, guid, nameof(StatCategory));
        }

        public ClassDefinition GetClass(string guid)
        {
            return GetFromLookup(_classesByGuid, guid, nameof(ClassDefinition));
        }

        public AbilityDefinition GetAbility(string guid)
        {
            return GetFromLookup(_abilitiesByGuid, guid, nameof(AbilityDefinition));
        }

        public WeaponTypeDefinition GetWeaponType(string guid)
        {
            return GetFromLookup(_weaponTypesByGuid, guid, nameof(WeaponTypeDefinition));
        }

        public ArmorTypeDefinition GetArmorType(string guid)
        {
            return GetFromLookup(_armorTypesByGuid, guid, nameof(ArmorTypeDefinition));
        }

        public WeaponDefinition GetWeapon(string guid)
        {
            return GetFromLookup(_weaponsByGuid, guid, nameof(WeaponDefinition));
        }

        public ArmorDefinition GetArmor(string guid)
        {
            return GetFromLookup(_armorsByGuid, guid, nameof(ArmorDefinition));
        }

        private static Dictionary<string, T> BuildLookup<T>(List<T> entries) where T : ScriptableObject, IGuidIdentified
        {
            var lookup = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            if (entries == null)
            {
                return lookup;
            }

            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    entries.RemoveAt(i);
                    continue;
                }

                var guid = entry.Guid;
                if (string.IsNullOrWhiteSpace(guid))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"Entry '{entry.name}' is missing a GUID and will be ignored.", entry);
#endif
                    continue;
                }

                if (lookup.ContainsKey(guid))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"Duplicate GUID '{guid}' found for entry '{entry.name}'.", entry);
#endif
                    continue;
                }

                lookup.Add(guid, entry);
            }

            return lookup;
        }

        private static bool TryGetFromLookup<T>(Dictionary<string, T> lookup, string guid, out T value) where T : UnityEngine.Object
        {
            if (lookup == null || string.IsNullOrWhiteSpace(guid))
            {
                value = null;
                return false;
            }

            return lookup.TryGetValue(guid, out value);
        }

        private static T GetFromLookup<T>(Dictionary<string, T> lookup, string guid, string friendlyName) where T : UnityEngine.Object
        {
            if (TryGetFromLookup(lookup, guid, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException($"{friendlyName} with guid '{guid}' was not registered.");
        }

        private static void SanitizeList<T>(List<T> entries) where T : ScriptableObject, IGuidIdentified
        {
            if (entries == null)
            {
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    entries.RemoveAt(i);
                    continue;
                }

                var guid = entry.Guid;
                if (string.IsNullOrWhiteSpace(guid) || !seen.Add(guid))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"Removing invalid or duplicate entry '{entry.name}' from registry list.", entry);
#endif
                    entries.RemoveAt(i);
                }
            }
        }
    }

    public interface IGuidIdentified
    {
        string Guid { get; }
    }
}
