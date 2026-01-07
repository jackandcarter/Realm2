using System.Collections.Generic;

namespace Realm.Data
{
    public class EquipmentSeedData
    {
        public string Guid;
        public string DisplayName;
        public string Description;
        public EquipmentSlot Slot = EquipmentSlot.Weapon;
        public List<string> RequiredClassIds = new();
        public List<EquipmentEquipEffect> EquipEffects = new();
    }
}
