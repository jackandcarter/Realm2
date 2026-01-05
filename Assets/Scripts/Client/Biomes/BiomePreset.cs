using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.Biomes
{
    [CreateAssetMenu(menuName = "Realm/Terrain/Biome Preset", fileName = "BiomePreset")]
    public class BiomePreset : ScriptableObject
    {
        [Serializable]
        public struct PaintSettings
        {
            [SerializeField] private int noiseSeed;
            [SerializeField] private float noiseScale;
            [SerializeField] private bool useHeightErosion;
            [SerializeField] private bool useSlopeErosion;

            public int NoiseSeed
            {
                get => noiseSeed;
                set => noiseSeed = value;
            }

            public float NoiseScale
            {
                get => noiseScale;
                set => noiseScale = value;
            }

            public bool UseHeightErosion
            {
                get => useHeightErosion;
                set => useHeightErosion = value;
            }

            public bool UseSlopeErosion
            {
                get => useSlopeErosion;
                set => useSlopeErosion = value;
            }

            public static PaintSettings Default => new PaintSettings
            {
                noiseSeed = 0,
                noiseScale = 1f,
                useHeightErosion = true,
                useSlopeErosion = true
            };

            public PaintSettings WithClampedValues()
            {
                return new PaintSettings
                {
                    noiseSeed = noiseSeed,
                    noiseScale = Mathf.Max(0f, noiseScale),
                    useHeightErosion = useHeightErosion,
                    useSlopeErosion = useSlopeErosion
                };
            }
        }

        [Serializable]
        public struct NoiseShapeSettings
        {
            [SerializeField] private bool enabled;
            [SerializeField] private int seed;
            [SerializeField] private int octaves;
            [SerializeField] private float frequency;
            [SerializeField] private float ridgeSharpness;
            [SerializeField] private int terraceSteps;

            public bool Enabled
            {
                get => enabled;
                set => enabled = value;
            }

            public int Seed
            {
                get => seed;
                set => seed = value;
            }

            public int Octaves
            {
                get => octaves;
                set => octaves = value;
            }

            public float Frequency
            {
                get => frequency;
                set => frequency = value;
            }

            public float RidgeSharpness
            {
                get => ridgeSharpness;
                set => ridgeSharpness = value;
            }

            public int TerraceSteps
            {
                get => terraceSteps;
                set => terraceSteps = value;
            }

            public static NoiseShapeSettings Default => new NoiseShapeSettings
            {
                enabled = false,
                seed = 0,
                octaves = 4,
                frequency = 0.05f,
                ridgeSharpness = 0f,
                terraceSteps = 0
            };

            public NoiseShapeSettings WithClampedValues()
            {
                return new NoiseShapeSettings
                {
                    enabled = enabled,
                    seed = seed,
                    octaves = Mathf.Clamp(octaves, 1, 8),
                    frequency = Mathf.Max(0f, frequency),
                    ridgeSharpness = Mathf.Max(0f, ridgeSharpness),
                    terraceSteps = Mathf.Clamp(terraceSteps, 0, 12)
                };
            }
        }

        [Serializable]
        public struct ThermalErosionSettings
        {
            [SerializeField] private bool enabled;
            [SerializeField] private float strength;
            [SerializeField] private float slopeThreshold;

            public bool Enabled
            {
                get => enabled;
                set => enabled = value;
            }

            public float Strength
            {
                get => strength;
                set => strength = value;
            }

            public float SlopeThreshold
            {
                get => slopeThreshold;
                set => slopeThreshold = value;
            }

            public static ThermalErosionSettings Default => new ThermalErosionSettings
            {
                enabled = true,
                strength = 0.35f,
                slopeThreshold = 30f
            };

            public ThermalErosionSettings WithClampedValues()
            {
                return new ThermalErosionSettings
                {
                    enabled = enabled,
                    strength = Mathf.Max(0f, strength),
                    slopeThreshold = Mathf.Clamp(slopeThreshold, 0f, 90f)
                };
            }
        }

        [Serializable]
        public struct HydraulicCarveSettings
        {
            [SerializeField] private bool enabled;
            [SerializeField] private float strength;
            [SerializeField] private float slopeThreshold;

            public bool Enabled
            {
                get => enabled;
                set => enabled = value;
            }

            public float Strength
            {
                get => strength;
                set => strength = value;
            }

            public float SlopeThreshold
            {
                get => slopeThreshold;
                set => slopeThreshold = value;
            }

            public static HydraulicCarveSettings Default => new HydraulicCarveSettings
            {
                enabled = true,
                strength = 0.4f,
                slopeThreshold = 15f
            };

            public HydraulicCarveSettings WithClampedValues()
            {
                return new HydraulicCarveSettings
                {
                    enabled = enabled,
                    strength = Mathf.Max(0f, strength),
                    slopeThreshold = Mathf.Clamp(slopeThreshold, 0f, 90f)
                };
            }
        }

        [Serializable]
        public struct CaveSettings
        {
            [SerializeField] private bool enabled;
            [SerializeField] private float minDepth;
            [SerializeField] private float maxDepth;
            [SerializeField] private float noiseScale;
            [SerializeField] private float threshold;
            [SerializeField] private float stalactiteFrequency;
            [SerializeField] private float stalagmiteFrequency;

            public bool Enabled
            {
                get => enabled;
                set => enabled = value;
            }

            public float MinDepth
            {
                get => minDepth;
                set => minDepth = value;
            }

            public float MaxDepth
            {
                get => maxDepth;
                set => maxDepth = value;
            }

            public float NoiseScale
            {
                get => noiseScale;
                set => noiseScale = value;
            }

            public float Threshold
            {
                get => threshold;
                set => threshold = value;
            }

            public float StalactiteFrequency
            {
                get => stalactiteFrequency;
                set => stalactiteFrequency = value;
            }

            public float StalagmiteFrequency
            {
                get => stalagmiteFrequency;
                set => stalagmiteFrequency = value;
            }

            public CaveSettings WithClampedValues()
            {
                var clampedMin = Mathf.Max(0f, minDepth);
                var clampedMax = Mathf.Max(clampedMin, maxDepth);

                return new CaveSettings
                {
                    enabled = enabled,
                    minDepth = clampedMin,
                    maxDepth = clampedMax,
                    noiseScale = Mathf.Max(0f, noiseScale),
                    threshold = Mathf.Clamp01(threshold),
                    stalactiteFrequency = Mathf.Max(0f, stalactiteFrequency),
                    stalagmiteFrequency = Mathf.Max(0f, stalagmiteFrequency)
                };
            }
        }

        [Serializable]
        public struct BiomeLayer
        {
            [SerializeField] private int textureIndex;
            [SerializeField] private float minHeight;
            [SerializeField] private float maxHeight;
            [SerializeField] private float heightFalloff;
            [SerializeField] private float minSlope;
            [SerializeField] private float maxSlope;
            [SerializeField] private float noiseAmplitude;
            [SerializeField] private float noiseFrequency;

            public int TextureIndex => textureIndex;
            public float MinHeight => minHeight;
            public float MaxHeight => maxHeight;
            public float HeightFalloff => heightFalloff;
            public float MinSlope => minSlope;
            public float MaxSlope => maxSlope;
            public float NoiseAmplitude => noiseAmplitude;
            public float NoiseFrequency => noiseFrequency;

            public BiomeLayer(int textureIndex, float minHeight, float maxHeight, float heightFalloff, float minSlope, float maxSlope,
                float noiseAmplitude, float noiseFrequency)
            {
                this.textureIndex = textureIndex;
                this.minHeight = minHeight;
                this.maxHeight = maxHeight;
                this.heightFalloff = heightFalloff;
                this.minSlope = minSlope;
                this.maxSlope = maxSlope;
                this.noiseAmplitude = noiseAmplitude;
                this.noiseFrequency = noiseFrequency;
            }

            public BiomeLayer WithClampedValues()
            {
                return new BiomeLayer(
                    textureIndex,
                    minHeight,
                    Mathf.Max(minHeight, maxHeight),
                    Mathf.Max(0f, heightFalloff),
                    Mathf.Clamp(minSlope, 0f, 90f),
                    Mathf.Clamp(maxSlope, 0f, 90f),
                    Mathf.Max(0f, noiseAmplitude),
                    Mathf.Max(0f, noiseFrequency)
                );
            }
        }

        [SerializeField] private PaintSettings paintSettings = PaintSettings.Default;
        [SerializeField] private NoiseShapeSettings noiseShape = NoiseShapeSettings.Default;
        [SerializeField] private ThermalErosionSettings thermalErosion = ThermalErosionSettings.Default;
        [SerializeField] private HydraulicCarveSettings hydraulicCarve = HydraulicCarveSettings.Default;
        [SerializeField] private List<BiomeLayer> layers = new List<BiomeLayer>();
        [SerializeField] private CaveSettings caves = new CaveSettings
        {
            Enabled = false,
            MinDepth = 5f,
            MaxDepth = 50f,
            NoiseScale = 0.05f,
            Threshold = 0.45f,
            StalactiteFrequency = 0.2f,
            StalagmiteFrequency = 0.2f
        };

        public IReadOnlyList<BiomeLayer> Layers => layers;
        public PaintSettings Paint
        {
            get => paintSettings;
            set => paintSettings = value.WithClampedValues();
        }

        public NoiseShapeSettings NoiseShape
        {
            get => noiseShape;
            set => noiseShape = value.WithClampedValues();
        }

        public ThermalErosionSettings ThermalErosion
        {
            get => thermalErosion;
            set => thermalErosion = value.WithClampedValues();
        }

        public HydraulicCarveSettings HydraulicCarve
        {
            get => hydraulicCarve;
            set => hydraulicCarve = value.WithClampedValues();
        }

        public CaveSettings Caves
        {
            get => caves;
            set => caves = value.WithClampedValues();
        }

        public bool HasLayers => layers != null && layers.Count > 0;

        public IReadOnlyList<string> Validate()
        {
            var issues = new List<string>();

            if (layers == null || layers.Count == 0)
            {
                issues.Add("Biome preset has no layers defined.");
                return issues;
            }

            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (layer.TextureIndex < 0)
                {
                    issues.Add($"Layer {i} is missing a valid texture index.");
                }

                if (layer.MaxHeight < layer.MinHeight)
                {
                    issues.Add($"Layer {i} has a max height lower than min height.");
                }

                if (layer.MaxSlope < layer.MinSlope)
                {
                    issues.Add($"Layer {i} has a max slope lower than min slope.");
                }
            }

            for (var i = 0; i < layers.Count; i++)
            {
                for (var j = i + 1; j < layers.Count; j++)
                {
                    var a = layers[i];
                    var b = layers[j];
                    if (RangesOverlap(a.MinHeight, a.MaxHeight, b.MinHeight, b.MaxHeight))
                    {
                        issues.Add($"Layers {i} and {j} have overlapping height ranges.");
                    }
                }
            }

            return issues;
        }

        private void OnValidate()
        {
            if (layers == null)
            {
                layers = new List<BiomeLayer>();
            }

            for (var i = 0; i < layers.Count; i++)
            {
                layers[i] = layers[i].WithClampedValues();
            }

            paintSettings = paintSettings.WithClampedValues();
            noiseShape = noiseShape.WithClampedValues();
            thermalErosion = thermalErosion.WithClampedValues();
            hydraulicCarve = hydraulicCarve.WithClampedValues();
            caves = caves.WithClampedValues();

            var issues = Validate();
            if (issues.Count == 0)
            {
                return;
            }

            foreach (var issue in issues)
            {
                Debug.LogWarning($"[{name}] {issue}", this);
            }
        }

        private static bool RangesOverlap(float minA, float maxA, float minB, float maxB)
        {
            if (minA > maxA)
            {
                (minA, maxA) = (maxA, minA);
            }

            if (minB > maxB)
            {
                (minB, maxB) = (maxB, minB);
            }

            return minA <= maxB && maxA >= minB;
        }
    }
}
