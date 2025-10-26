using System;
using System.Collections.Generic;

namespace Client.CharacterCreation
{
    public static class ClassCatalog
    {
        private static readonly CharacterClassDefinition[] Classes =
        {
            new CharacterClassDefinition
            {
                Id = "warrior",
                DisplayName = "Warrior",
                RoleSummary = "Frontline melee fighters capable of tanking or high damage output.",
                Description = "Masters of physical combat who draw enemy attention while dealing or mitigating damage."
            },
            new CharacterClassDefinition
            {
                Id = "wizard",
                DisplayName = "Wizard",
                RoleSummary = "Ranged casters wielding destructive elemental magic.",
                Description = "Arcane specialists who bombard foes with elemental spells from afar."
            },
            new CharacterClassDefinition
            {
                Id = "time-mage",
                DisplayName = "Time Mage",
                RoleSummary = "Support casters manipulating time and space for the party.",
                Description = "Green magic practitioners who slow foes, hasten allies, and create portals across the field."
            },
            new CharacterClassDefinition
            {
                Id = "necromancer",
                DisplayName = "Necromancer",
                RoleSummary = "Revenant-exclusive hybrid of melee prowess and necrotic support.",
                Description = "Revenant specialists who command the dead, reinforcing allies with necromantic arts."
            },
            new CharacterClassDefinition
            {
                Id = "technomancer",
                DisplayName = "Technomancer",
                RoleSummary = "Gearling-exclusive support engineers harnessing arcane machinery.",
                Description = "Gearling innovators who blend runes and gadgets to empower allies and control the battlefield."
            },
            new CharacterClassDefinition
            {
                Id = "sage",
                DisplayName = "Sage",
                RoleSummary = "Primary healers focused on restorative white magic.",
                Description = "Devoted healers who mend wounds, revive allies, and sustain parties through attrition."
            },
            new CharacterClassDefinition
            {
                Id = "rogue",
                DisplayName = "Rogue",
                RoleSummary = "Agile skirmishers relying on stealth and burst damage.",
                Description = "Stealthy melee combatants who strike from the shadows and disrupt enemy lines."
            },
            new CharacterClassDefinition
            {
                Id = "ranger",
                DisplayName = "Ranger",
                RoleSummary = "Felarian-exclusive ranged support and precision damage dealer.",
                Description = "Felarian bow experts who provide ranged pressure alongside defensive boons."
            },
            new CharacterClassDefinition
            {
                Id = "builder",
                DisplayName = "Builder",
                RoleSummary = "Non-combat architects responsible for settlements and structures.",
                Description = "Craftspeople who unlock the Arkitect interface to raise communities and infrastructure."
            }
        };

        private static readonly Dictionary<string, CharacterClassDefinition> Lookup;

        static ClassCatalog()
        {
            Lookup = new Dictionary<string, CharacterClassDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in Classes)
            {
                if (definition?.Id == null || Lookup.ContainsKey(definition.Id))
                {
                    continue;
                }

                Lookup[definition.Id] = definition;
            }
        }

        public static IReadOnlyList<CharacterClassDefinition> GetAllClasses()
        {
            return Classes;
        }

        public static bool TryGetClass(string classId, out CharacterClassDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(classId))
            {
                definition = null;
                return false;
            }

            return Lookup.TryGetValue(classId, out definition);
        }
    }
}
