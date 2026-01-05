using Client.Biomes;
using Digger.Modules.Core.Sources.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Digger.Modules.Core.Sources.Operations
{
    public struct BiomePaintOverrides
    {
        public int NoiseSeed;
        public float NoiseScale;
        public bool UseHeightErosion;
        public bool UseSlopeErosion;

        public static BiomePaintOverrides Default => new BiomePaintOverrides
        {
            NoiseSeed = 0,
            NoiseScale = 1f,
            UseHeightErosion = true,
            UseSlopeErosion = true
        };
    }

    public class BiomePaintOperation : IOperation<VoxelBiomePaintJob>
    {
        public BiomePreset Preset;
        public Vector3 Position;
        public Vector3 Size = new Vector3(4f, 4f, 4f);
        public BrushType Brush = BrushType.Sphere;
        public float Opacity = 1f;
        public bool OpacityIsTarget;
        public BiomePaintOverrides Overrides = BiomePaintOverrides.Default;

        public ModificationArea GetAreaToModify(DiggerSystem digger)
        {
            if (Preset == null)
            {
                Debug.LogWarning("BiomePaintOperation requires a BiomePreset.");
                return new ModificationArea { NeedsModification = false };
            }

            if (!Preset.HasLayers)
            {
                Debug.LogWarning("BiomePaintOperation preset has no layers.");
                return new ModificationArea { NeedsModification = false };
            }

            var radius = math.max(math.max(Size.x, Size.y), Size.z);
            return ModificationAreaUtils.GetSphericalAreaToModify(digger, Position, radius);
        }

        public VoxelBiomePaintJob Do(VoxelChunk chunk)
        {
            var layerArray = new NativeArray<BiomeLayerData>(Preset.Layers.Count, Allocator.TempJob);
            for (var i = 0; i < Preset.Layers.Count; i++)
            {
                var layer = Preset.Layers[i];
                layerArray[i] = new BiomeLayerData
                {
                    TextureIndex = layer.TextureIndex,
                    MinHeight = layer.MinHeight,
                    MaxHeight = layer.MaxHeight,
                    Falloff = layer.HeightFalloff,
                    MinSlope = layer.MinSlope,
                    MaxSlope = layer.MaxSlope,
                    NoiseAmplitude = layer.NoiseAmplitude,
                    NoiseFrequency = layer.NoiseFrequency
                };
            }

            var job = new VoxelBiomePaintJob
            {
                SizeVox = chunk.SizeVox,
                SizeVox2 = chunk.SizeVox * chunk.SizeVox,
                HeightmapScale = chunk.HeightmapScale,
                ChunkAltitude = chunk.WorldPosition.y,
                Voxels = new NativeArray<Voxel>(chunk.VoxelArray, Allocator.TempJob),
                Heights = new NativeArray<float>(chunk.HeightArray, Allocator.TempJob),
                Layers = layerArray,
                Brush = Brush,
                Center = new float3(Position.x, Position.y, Position.z) - chunk.AbsoluteWorldPosition,
                Size = Size,
                Intensity = Opacity,
                IsTargetIntensity = OpacityIsTarget,
                NoiseSeed = Overrides.NoiseSeed,
                NoiseScale = Overrides.NoiseScale,
                UseHeightErosion = Overrides.UseHeightErosion,
                UseSlopeErosion = Overrides.UseSlopeErosion
            };
            job.PostConstruct();
            return job;
        }

        public void Complete(VoxelBiomePaintJob job, VoxelChunk chunk)
        {
            job.Voxels.CopyTo(chunk.VoxelArray);
            job.Dispose();
        }
    }
}
