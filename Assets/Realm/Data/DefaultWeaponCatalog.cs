using System;
using System.Collections.Generic;
using UnityEngine;

namespace Realm.Data
{
    [CreateAssetMenu(menuName = "Realm/Equipment/Default Weapon Catalog", fileName = "DefaultWeaponCatalog")]
    public class DefaultWeaponCatalog : ScriptableObject
    {
        [SerializeField] private List<DefaultWeaponEntry> entries = new();

        public bool TryGetDefaultWeapon(string classId, out WeaponDefinition weapon)
        {
            weapon = null;
            if (entries == null || string.IsNullOrWhiteSpace(classId))
            {
                return false;
            }

            foreach (var entry in entries)
            {
                if (entry == null || entry.Weapon == null || string.IsNullOrWhiteSpace(entry.ClassId))
                {
                    continue;
                }

                if (string.Equals(entry.ClassId.Trim(), classId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    weapon = entry.Weapon;
                    return true;
                }
            }

            return false;
        }

        [Serializable]
        public class DefaultWeaponEntry
        {
            public string ClassId;
            public WeaponDefinition Weapon;
        }
    }
}
