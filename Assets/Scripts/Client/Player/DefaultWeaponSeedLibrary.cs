using System;
using System.Collections.Generic;
using Client.CharacterCreation;
using Realm.Data;
using UnityEngine;

namespace Client.Player
{
    public static class DefaultWeaponSeedLibrary
    {
        private static readonly Dictionary<string, WeaponDefinition> CachedWeapons =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, WeaponSeedData> Seeds =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["warrior"] = BuildSeed("warrior", "Warrior Training Sword", 14f, "2a7f1c9b6f0b4c28a8a4d5f8b7a1c001"),
                ["wizard"] = BuildSeed("wizard", "Wizard Focus Staff", 9f, "3e51b07f9f5c4b5aa31e116d9bb2c002"),
                ["time-mage"] = BuildSeed("time-mage", "Chronomancer Wand", 8f, "4c2e8fd2b2f6417e9c1a07e6e8c0c003"),
                ["necromancer"] = BuildSeed("necromancer", "Bone Scythe", 12f, "5d7a1b3efc1e4b9c8c2d9a4fb7d7c004"),
                ["technomancer"] = BuildSeed("technomancer", "Gearling Arc Lance", 10f, "6f8b3c4ad2a64287b8c1d6e4a7b2c005"),
                ["sage"] = BuildSeed("sage", "Radiant Rod", 8.5f, "7a4d2e1c8bcd4f4f9a2e3b7c1d0ac006"),
                ["rogue"] = BuildSeed("rogue", "Shadow Dagger", 11f, "8b1c4d7e9f2a4c3d9e8b1f3c2d4be007"),
                ["ranger"] = BuildSeed("ranger", "Oak Training Bow", 10.5f, "9c5e7b1a3d4f4c2e8b7a1c2d3e4f9008"),
                ["builder"] = BuildSeed("builder", "Utility Hammer", 6f, "0d7b2c5e9f1a4b3c8d7e6f5a4b3c1009")
            };

        public static bool TryCreateDefaultWeapon(string classId, out WeaponDefinition weapon)
        {
            weapon = null;
            if (string.IsNullOrWhiteSpace(classId))
            {
                return false;
            }

            if (CachedWeapons.TryGetValue(classId, out weapon) && weapon != null)
            {
                return true;
            }

            if (!Seeds.TryGetValue(classId, out var seed))
            {
                return false;
            }

            weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.name = seed.DisplayName;
            weapon.ApplySeed(seed);
            CachedWeapons[classId] = weapon;
            return true;
        }

        private static WeaponSeedData BuildSeed(string classId, string displayName, float baseDamage, string guid)
        {
            return new WeaponSeedData
            {
                Guid = guid,
                DisplayName = displayName,
                Description = $"Seeded weapon for {ClassCatalog.TryGetClass(classId, out var definition) ? definition.DisplayName : classId}.",
                Slot = EquipmentSlot.Weapon,
                RequiredClassIds = new List<string> { classId },
                BaseDamage = baseDamage,
                LightAttack = new WeaponAttackProfile(0.8f, 0.7f, 0.2f, 0.25f),
                MediumAttack = new WeaponAttackProfile(1f, 0.82f, 0.3f, 0.35f),
                HeavyAttack = new WeaponAttackProfile(1.35f, 0.92f, 0.45f, 0.55f)
            };
        }
    }
}
