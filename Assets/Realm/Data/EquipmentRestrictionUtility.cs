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
                    !Contains(classDefinition.AllowedWeaponTypes, weapon.WeaponType.WeaponType))
                {
                    reason = $"Weapon type {weapon.WeaponType.WeaponType} is not allowed for this class.";
                    return false;
                }
            }

            if (equipment is ArmorDefinition armor && armor.ArmorType != null)
            {
                if (classDefinition.AllowedArmorTypes != null &&
                    classDefinition.AllowedArmorTypes.Count > 0 &&
                    !Contains(classDefinition.AllowedArmorTypes, armor.ArmorType.ArmorType))
                {
                    reason = $"Armor type {armor.ArmorType.ArmorType} is not allowed for this class.";
                    return false;
                }
            }

            return true;
        }

        private static bool Contains<T>(IReadOnlyList<T> values, T candidate) where T : struct
        {
            if (values == null)
            {
                return false;
            }

            for (var i = 0; i < values.Count; i++)
            {
                if (Equals(values[i], candidate))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
