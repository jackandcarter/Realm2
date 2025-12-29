using UnityEngine;

namespace Realm.Data
{
    [CreateAssetMenu(menuName = "Realm/Equipment/Armor", fileName = "ArmorDefinition")]
    public class ArmorDefinition : EquipmentDefinition
    {
        [SerializeField, Tooltip("Armor type this item belongs to.")]
        private ArmorTypeDefinition armorType;

        public ArmorTypeDefinition ArmorType => armorType;
    }
}
