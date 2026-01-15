using UnityEngine;

namespace Client.Combat
{
    [DisallowMultipleComponent]
    public class BasicCombatStatsProvider : MonoBehaviour, ICombatStatsProvider
    {
        [Header("Primary Stats")]
        [SerializeField] private float strength;
        [SerializeField] private float dexterity;
        [SerializeField] private float vitality;

        [Header("Combat Stats")]
        [SerializeField] private float attackPower;
        [SerializeField] private float defense;
        [SerializeField, Range(0f, 1f)] private float critChance;
        [SerializeField] private float critMultiplier = 1.5f;

        public CombatStats GetCombatStats()
        {
            return new CombatStats(
                strength,
                dexterity,
                vitality,
                attackPower,
                defense,
                critChance,
                critMultiplier);
        }
    }
}
