using Realm.Abilities;

namespace Client.Combat
{
    public readonly struct WeaponAttackResolution
    {
        public readonly WeaponAttackRequest Request;
        public readonly float TotalDamage;
        public readonly float Accuracy;
        public readonly AbilityHitboxConfig Hitbox;
        public readonly AbilityDefinition SpecialAbility;

        public WeaponAttackResolution(
            WeaponAttackRequest request,
            float totalDamage,
            float accuracy,
            AbilityHitboxConfig hitbox,
            AbilityDefinition specialAbility)
        {
            Request = request;
            TotalDamage = totalDamage;
            Accuracy = accuracy;
            Hitbox = hitbox;
            SpecialAbility = specialAbility;
        }
    }
}
