using System.Collections.Generic;
using Realm.Abilities;

namespace Realm.Data
{
    public class WeaponSeedData : EquipmentSeedData
    {
        public float BaseDamage = 10f;
        public WeaponAttackProfile LightAttack = WeaponAttackProfile.DefaultLight;
        public WeaponAttackProfile MediumAttack = WeaponAttackProfile.DefaultMedium;
        public WeaponAttackProfile HeavyAttack = WeaponAttackProfile.DefaultHeavy;
        public AbilityDefinition SpecialAttack;
    }
}
