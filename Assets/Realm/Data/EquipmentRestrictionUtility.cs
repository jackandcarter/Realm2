using System;
using System.Collections.Generic;

namespace Realm.Data
{
    public static class EquipmentRestrictionUtility
    {
        public static bool IsClassAllowedForEquipment(string classId, EquipmentDefinition equipment)
        {
            if (equipment == null)
            {
                return false;
            }

            var requiredIds = equipment.RequiredClassIds;
            if (requiredIds != null && requiredIds.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(classId))
                {
                    return false;
                }

                for (var i = 0; i < requiredIds.Count; i++)
                {
                    if (string.Equals(requiredIds[i], classId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }

        public static bool IsClassAllowedForEquipment(ClassDefinition classDefinition, EquipmentDefinition equipment, out string reason)
        {
            reason = null;

            if (equipment == null)
            {
                reason = "No equipment supplied.";
                return false;
            }

            if (classDefinition == null)
            {
                return true;
            }

            if (!IsClassAllowedForEquipment(classDefinition.ClassId, equipment))
            {
                reason = "Class id is not listed for this equipment.";
                return false;
            }

            if (equipment is WeaponDefinition weapon && weapon.WeaponType != null)
            {
                if (classDefinition.AllowedWeaponTypes != null &&
                    classDefinition.AllowedWeaponTypes.Count > 0 &&
                    !ContainsGuid(classDefinition.AllowedWeaponTypes, weapon.WeaponType))
                {
                    reason = $"Weapon type {GetDisplayName(weapon.WeaponType)} is not allowed for this class.";
                    return false;
                }
            }

            if (equipment is ArmorDefinition armor && armor.ArmorType != null)
            {
                if (classDefinition.AllowedArmorTypes != null &&
                    classDefinition.AllowedArmorTypes.Count > 0 &&
                    !ContainsGuid(classDefinition.AllowedArmorTypes, armor.ArmorType))
                {
                    reason = $"Armor type {GetDisplayName(armor.ArmorType)} is not allowed for this class.";
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsGuid<T>(IReadOnlyList<T> values, T candidate) where T : class, IGuidIdentified
        {
            if (values == null)
            {
                return false;
            }

            if (candidate == null)
            {
                return false;
            }

            var candidateGuid = candidate.Guid;
            if (string.IsNullOrWhiteSpace(candidateGuid))
            {
                return false;
            }

            for (var i = 0; i < values.Count; i++)
            {
                var entry = values[i];
                if (entry == null)
                {
                    continue;
                }

                if (string.Equals(entry.Guid, candidateGuid, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetDisplayName(WeaponTypeDefinition weaponType)
        {
            if (weaponType == null)
            {
                return "Unknown";
            }

            return string.IsNullOrWhiteSpace(weaponType.DisplayName) ? weaponType.name : weaponType.DisplayName;
        }

        private static string GetDisplayName(ArmorTypeDefinition armorType)
        {
            if (armorType == null)
            {
                return "Unknown";
            }

            return string.IsNullOrWhiteSpace(armorType.DisplayName) ? armorType.name : armorType.DisplayName;
        }
    }
}
