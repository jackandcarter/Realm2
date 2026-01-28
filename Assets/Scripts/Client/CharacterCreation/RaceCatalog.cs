using System;
using System.Collections.Generic;

namespace Client.CharacterCreation
{
    public static class RaceCatalog
    {
        private static readonly RaceDefinition[] Races =
        {
            new RaceDefinition
            {
                Id = "felarian",
                DisplayName = "Felarian",
                AppearanceSummary = "Feline ears, tails, retractable claws, and agile, lean builds with expressive, slitted eyes.",
                LoreSummary = "Mystics of Eldros who commune with forest spirits and blend archery, stealth, and time magic.",
                SignatureAbilities = new[]
                {
                    "Spiritstep Reflexes",
                    "Forest Whisper",
                    "Temporal Pounce"
                },
                AllowedClassIds = ClassRulesCatalog.GetAllowedClassIdsForRace("felarian"),
                StarterClassIds = ClassRulesCatalog.GetStarterClassIdsForRace("felarian"),
                Customization = new RaceCustomizationOptions
                {
                    Height = new FloatRange(1.55f, 1.9f),
                    Build = new FloatRange(0.35f, 0.7f),
                    FeatureOptions = new[]
                    {
                        new RaceFeatureDefinition
                        {
                            Id = "ears",
                            DisplayName = "Ear Shape",
                            Options = new[] { "Tufted", "Arched", "Long" },
                            DefaultOption = "Tufted"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "tail",
                            DisplayName = "Tail Length",
                            Options = new[] { "Short", "Balanced", "Long" },
                            DefaultOption = "Balanced"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "fur-pattern",
                            DisplayName = "Fur Pattern",
                            Options = new[] { "Solid", "Striped", "Mottled" },
                            DefaultOption = "Striped"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "eye-glow",
                            DisplayName = "Eye Glow",
                            Options = new[] { "Dim", "Radiant", "Luminous" },
                            DefaultOption = "Radiant"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "hair-color",
                            DisplayName = "Hair & Fur Color",
                            Options = new[] { "Shadow", "Ember", "Mist" },
                            DefaultOption = "Ember"
                        }
                    },
                    AdjustableFeatures = new[]
                    {
                        "Ear shape & tufting",
                        "Tail length & fur pattern",
                        "Claw sheen",
                        "Eye glow intensity",
                        "Hair & fur coloration"
                    }
                }
            },
            new RaceDefinition
            {
                Id = "human",
                DisplayName = "Human",
                AppearanceSummary = "Wide range of skin tones, hairstyles, and physiques inspired by the cultures of Elysium.",
                LoreSummary = "Resilient explorers descended from a fallen empire, renowned for diplomacy and adaptability.",
                SignatureAbilities = new[]
                {
                    "Versatile Training",
                    "Diplomat's Insight",
                    "Second Wind"
                },
                AllowedClassIds = ClassRulesCatalog.GetAllowedClassIdsForRace("human"),
                StarterClassIds = ClassRulesCatalog.GetStarterClassIdsForRace("human"),
                Customization = new RaceCustomizationOptions
                {
                    Height = new FloatRange(1.5f, 2.05f),
                    Build = new FloatRange(0.25f, 0.85f),
                    FeatureOptions = new[]
                    {
                        new RaceFeatureDefinition
                        {
                            Id = "body-proportions",
                            DisplayName = "Body Proportions",
                            Options = new[] { "Lean", "Balanced", "Sturdy" },
                            DefaultOption = "Balanced"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "facial-structure",
                            DisplayName = "Facial Structure",
                            Options = new[] { "Angular", "Classic", "Soft" },
                            DefaultOption = "Classic"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "hair-style",
                            DisplayName = "Hair Style",
                            Options = new[] { "Short", "Braided", "Long" },
                            DefaultOption = "Short"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "eye-color",
                            DisplayName = "Eye Color",
                            Options = new[] { "Sage", "Amber", "Steel" },
                            DefaultOption = "Sage"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "markings",
                            DisplayName = "Body Markings",
                            Options = new[] { "None", "Runic", "Tattooed" },
                            DefaultOption = "None"
                        }
                    },
                    AdjustableFeatures = new[]
                    {
                        "Body proportions",
                        "Facial structure & features",
                        "Hair styles & facial hair",
                        "Eye coloration",
                        "Body markings & tattoos"
                    }
                }
            },
            new RaceDefinition
            {
                Id = "crystallian",
                DisplayName = "Crystallian",
                AppearanceSummary = "Faceted gemstone skin, optional horn ridges or draconic scales, and luminous gem eyes.",
                LoreSummary = "Dragon-bonded guardians from Drakoria who channel elemental energies through crystalline bodies.",
                SignatureAbilities = new[]
                {
                    "Prismatic Bulwark",
                    "Elemental Channel",
                    "Gemcut Resonance"
                },
                AllowedClassIds = ClassRulesCatalog.GetAllowedClassIdsForRace("crystallian"),
                StarterClassIds = ClassRulesCatalog.GetStarterClassIdsForRace("crystallian"),
                Customization = new RaceCustomizationOptions
                {
                    Height = new FloatRange(1.8f, 2.3f),
                    Build = new FloatRange(0.45f, 0.95f),
                    FeatureOptions = new[]
                    {
                        new RaceFeatureDefinition
                        {
                            Id = "facet-pattern",
                            DisplayName = "Facet Pattern",
                            Options = new[] { "Prismatic", "Geometric", "Ridge" },
                            DefaultOption = "Prismatic"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "horn-style",
                            DisplayName = "Horn & Scale Style",
                            Options = new[] { "Crested", "Spined", "Smooth" },
                            DefaultOption = "Crested"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "inner-light",
                            DisplayName = "Internal Light Hue",
                            Options = new[] { "Azure", "Violet", "Gold" },
                            DefaultOption = "Azure"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "energy-traces",
                            DisplayName = "Energy Traces",
                            Options = new[] { "Threaded", "Flowing", "Crystalline" },
                            DefaultOption = "Threaded"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "armor-density",
                            DisplayName = "Armor Density",
                            Options = new[] { "Light", "Reinforced", "Bulwark" },
                            DefaultOption = "Reinforced"
                        }
                    },
                    AdjustableFeatures = new[]
                    {
                        "Crystal facet patterns",
                        "Horn & scale styles",
                        "Internal light hue",
                        "Vein-like energy traces",
                        "Armor plating density"
                    }
                }
            },
            new RaceDefinition
            {
                Id = "revenant",
                DisplayName = "Revenant",
                AppearanceSummary = "Ethereal, undead visages with spectral glow, exposed bone etchings, and arcane sigils.",
                LoreSummary = "Soulbound wanderers of Netheris seeking redemption through mastery of necromantic arts.",
                SignatureAbilities = new[]
                {
                    "Wraithwalk",
                    "Ancestral Ward",
                    "Veil Rend"
                },
                AllowedClassIds = ClassRulesCatalog.GetAllowedClassIdsForRace("revenant"),
                StarterClassIds = ClassRulesCatalog.GetStarterClassIdsForRace("revenant"),
                Customization = new RaceCustomizationOptions
                {
                    Height = new FloatRange(1.65f, 2.0f),
                    Build = new FloatRange(0.2f, 0.6f),
                    FeatureOptions = new[]
                    {
                        new RaceFeatureDefinition
                        {
                            Id = "glow",
                            DisplayName = "Glow Color",
                            Options = new[] { "Pale", "Spectral", "Ebon" },
                            DefaultOption = "Spectral"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "skeletal-exposure",
                            DisplayName = "Skeletal Exposure",
                            Options = new[] { "Subtle", "Revealed", "Bare" },
                            DefaultOption = "Revealed"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "sigils",
                            DisplayName = "Sigil Placement",
                            Options = new[] { "Veins", "Mask", "Core" },
                            DefaultOption = "Veins"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "veil-cloth",
                            DisplayName = "Veil Cloth",
                            Options = new[] { "Tattered", "Flowing", "Cloaked" },
                            DefaultOption = "Flowing"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "eye-flare",
                            DisplayName = "Eye Flare",
                            Options = new[] { "Ringed", "Shattered", "Ember" },
                            DefaultOption = "Ringed"
                        }
                    },
                    AdjustableFeatures = new[]
                    {
                        "Glow intensity & color",
                        "Skeletal exposure",
                        "Sigil placement",
                        "Veil cloth & tethers",
                        "Eye flare pattern"
                    }
                }
            },
            new RaceDefinition
            {
                Id = "gearling",
                DisplayName = "Gearling",
                AppearanceSummary = "Compact frames with mechanical limbs, glowing runes, and modular plating with moving gears.",
                LoreSummary = "Inventive magi-engineers from Gearspring who meld arcane runes with mechanical craftsmanship.",
                SignatureAbilities = new[]
                {
                    "Arcane Gadgetry",
                    "Runesmith's Flourish",
                    "Miniaturize"
                },
                AllowedClassIds = ClassRulesCatalog.GetAllowedClassIdsForRace("gearling"),
                StarterClassIds = ClassRulesCatalog.GetStarterClassIdsForRace("gearling"),
                Customization = new RaceCustomizationOptions
                {
                    Height = new FloatRange(1.0f, 1.4f),
                    Build = new FloatRange(0.25f, 0.55f),
                    FeatureOptions = new[]
                    {
                        new RaceFeatureDefinition
                        {
                            Id = "gear-config",
                            DisplayName = "Gear Configuration",
                            Options = new[] { "Compact", "Balanced", "Overclocked" },
                            DefaultOption = "Balanced"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "rune-glow",
                            DisplayName = "Rune Glow",
                            Options = new[] { "Cobalt", "Crimson", "Verdant" },
                            DefaultOption = "Cobalt"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "limb-chassis",
                            DisplayName = "Limb Chassis",
                            Options = new[] { "Articulated", "Reinforced", "Lightweight" },
                            DefaultOption = "Articulated"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "plating",
                            DisplayName = "Plating Style",
                            Options = new[] { "Etched", "Riveted", "Polished" },
                            DefaultOption = "Etched"
                        },
                        new RaceFeatureDefinition
                        {
                            Id = "eye-lenses",
                            DisplayName = "Eye Lenses",
                            Options = new[] { "Round", "Hex", "Catlike" },
                            DefaultOption = "Round"
                        }
                    },
                    AdjustableFeatures = new[]
                    {
                        "Gear configuration",
                        "Rune glow color",
                        "Limb chassis style",
                        "Hair cabling & plating",
                        "Eye lenses & aperture"
                    }
                }
            }
        };

        private static readonly Dictionary<string, RaceDefinition> Lookup;

        static RaceCatalog()
        {
            Lookup = new Dictionary<string, RaceDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var race in Races)
            {
                if (race?.Id == null || Lookup.ContainsKey(race.Id))
                {
                    continue;
                }

                Lookup[race.Id] = race;
            }
        }

        public static IReadOnlyList<RaceDefinition> GetAllRaces()
        {
            return Races;
        }

        public static bool TryGetRace(string raceId, out RaceDefinition race)
        {
            if (string.IsNullOrWhiteSpace(raceId))
            {
                race = null;
                return false;
            }

            return Lookup.TryGetValue(raceId, out race);
        }
    }
}
