using UnityEngine;

namespace Realm.Data
{
    [CreateAssetMenu(menuName = "Realm/Equipment/Weapon", fileName = "WeaponDefinition")]
    public class WeaponDefinition : EquipmentDefinition
    {
        [SerializeField, Tooltip("Weapon type this item belongs to.")]
        private WeaponTypeDefinition weaponType;

        public WeaponTypeDefinition WeaponType => weaponType;
    }
}
