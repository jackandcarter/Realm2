using System.Collections.Generic;
using UnityEngine;

namespace Client.Combat
{
    [CreateAssetMenu(menuName = "Realm/Combat/Weapon Combo Definition", fileName = "WeaponComboDefinition")]
    public class WeaponComboDefinition : ScriptableObject
    {
        [SerializeField] private string weaponId;
        [SerializeField] private List<WeaponComboInputType> comboSequence = new();
        [SerializeField] private string specialAttackAbilityId;

        public string WeaponId => weaponId;
        public IReadOnlyList<WeaponComboInputType> ComboSequence => comboSequence;
        public string SpecialAttackAbilityId => specialAttackAbilityId;
    }
}
