using System;

namespace Client.Combat
{
    [Serializable]
    public struct CombatStats
    {
        public static readonly CombatStats Default = new CombatStats(
            strength: 0f,
            dexterity: 0f,
            vitality: 0f,
            attackPower: 0f,
            defense: 0f,
            critChance: 0f,
            critMultiplier: 1.5f);

        public float Strength;
        public float Dexterity;
        public float Vitality;
        public float AttackPower;
        public float Defense;
        public float CritChance;
        public float CritMultiplier;

        public CombatStats(
            float strength,
            float dexterity,
            float vitality,
            float attackPower,
            float defense,
            float critChance,
            float critMultiplier)
        {
            Strength = strength;
            Dexterity = dexterity;
            Vitality = vitality;
            AttackPower = attackPower;
            Defense = defense;
            CritChance = critChance;
            CritMultiplier = critMultiplier;
        }

        public float PhysicalPower => AttackPower + (Strength * 2f) + (Dexterity * 0.5f);
    }
}
