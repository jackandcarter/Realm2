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
                Customization = new RaceCustomizationOptions
                {
                    Height = new FloatRange(1.55f, 1.9f),
                    Build = new FloatRange(0.35f, 0.7f),
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
                Customization = new RaceCustomizationOptions
                {
                    Height = new FloatRange(1.5f, 2.05f),
                    Build = new FloatRange(0.25f, 0.85f),
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
                Customization = new RaceCustomizationOptions
                {
                    Height = new FloatRange(1.8f, 2.3f),
                    Build = new FloatRange(0.45f, 0.95f),
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
                Customization = new RaceCustomizationOptions
                {
                    Height = new FloatRange(1.65f, 2.0f),
                    Build = new FloatRange(0.2f, 0.6f),
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
                Customization = new RaceCustomizationOptions
                {
                    Height = new FloatRange(1.0f, 1.4f),
                    Build = new FloatRange(0.25f, 0.55f),
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

        public static IReadOnlyList<RaceDefinition> GetAllRaces()
        {
            return Races;
        }
    }
}
