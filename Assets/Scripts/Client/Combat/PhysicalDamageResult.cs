namespace Client.Combat
{
    public readonly struct PhysicalDamageResult
    {
        public readonly float BaseDamage;
        public readonly float Multiplier;
        public readonly float StatBonus;
        public readonly float TotalDamage;

        public PhysicalDamageResult(float baseDamage, float multiplier, float statBonus, float totalDamage)
        {
            BaseDamage = baseDamage;
            Multiplier = multiplier;
            StatBonus = statBonus;
            TotalDamage = totalDamage;
        }
    }
}
