using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Digger.Modules.Core.Sources.Jobs
{
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
    public struct VoxelBiomePaintJob : IJobParallelFor, IDisposable
    {
        public int SizeVox;
        public int SizeVox2;
        public BrushType Brush;
        public float3 HeightmapScale;
        public float3 Center;
        public float3 Size;
        public float Intensity;
        public bool IsTargetIntensity;
        public float ChunkAltitude;

        [ReadOnly] [NativeDisableParallelForRestriction]
        public NativeArray<float> Heights;

        [ReadOnly]
        public NativeArray<BiomeLayerData> Layers;

        public NativeArray<Voxel> Voxels;

        private double coneAngle;
        private float upsideDownSign;

        public void PostConstruct()
        {
            if (Size.y > 0.1f)
            {
                coneAngle = Math.Atan((double)Size.x / Size.y);
            }

            upsideDownSign = 1f;
        }

        public void Execute(int index)
        {
            if (Layers.Length == 0)
            {
                return;
            }

            var pi = Utils.IndexToXYZ(index, SizeVox, SizeVox2);
            var p = pi * HeightmapScale;
            var distance = ComputeDistance(p);
            if (distance < 0f)
            {
                return;
            }

            if (!Utils.IsOnSurface(pi, HeightmapScale.y, p.y + ChunkAltitude, SizeVox, Heights))
            {
                return;
            }

            var terrainHeight = Heights[Utils.XYZToHeightIndex(new int3(pi.x, 0, pi.z), SizeVox)];
            var slopeDegrees = ComputeSlopeDegrees(pi);

            var voxel = Voxels[index];
            for (var i = 0; i < Layers.Length; i++)
            {
                var layer = Layers[i];
                if (layer.TextureIndex < 0)
                {
                    continue;
                }

                if (slopeDegrees < layer.MinSlope || slopeDegrees > layer.MaxSlope)
                {
                    continue;
                }

                var heightWeight = ComputeHeightWeight(layer, terrainHeight);
                if (heightWeight <= 0f)
                {
                    continue;
                }

                var noiseWeight = ComputeNoiseWeight(layer, p);
                var weight = math.saturate(heightWeight * noiseWeight) * Intensity;
                if (weight <= 0f)
                {
                    continue;
                }

                ApplyTexture(ref voxel, layer.TextureIndex, weight);
            }

            Voxels[index] = voxel;
        }

        private float ComputeDistance(float3 p)
        {
            switch (Brush)
            {
                case BrushType.Sphere:
                    return ComputeSphereDistances(p);
                case BrushType.HalfSphere:
                    return ComputeHalfSphereDistances(p);
                case BrushType.RoundedCube:
                    return ComputeCubeDistances(p);
                case BrushType.Stalagmite:
                    return ComputeConeDistances(p);
                default:
                    return -1f;
            }
        }

        private float ComputeSphereDistances(float3 p)
        {
            var radius = Size.x;
            var radiusHeightRatio = radius / math.max(Size.y, 0.01f);
            var vec = p - Center;
            var distance = math.sqrt(vec.x * vec.x + vec.y * vec.y * radiusHeightRatio * radiusHeightRatio + vec.z * vec.z);
            return radius - distance;
        }

        private float ComputeHalfSphereDistances(float3 p)
        {
            var vec = p - Center;
            var distance = math.sqrt(vec.x * vec.x + vec.y * vec.y + vec.z * vec.z);
            return math.min(Size.x - distance, vec.y);
        }

        private float ComputeCubeDistances(float3 p)
        {
            var vec = p - Center;
            return math.min(math.min(Size.x - math.abs(vec.x), Size.y - math.abs(vec.y)), Size.z - math.abs(vec.z));
        }

        private float ComputeConeDistances(float3 p)
        {
            var coneVertex = Center + new float3(0, upsideDownSign * Size.y * 0.95f, 0);
            var vec = p - coneVertex;
            var distance = math.sqrt(vec.x * vec.x + vec.y * vec.y + vec.z * vec.z);
            var flatDistance = math.sqrt(vec.x * vec.x + vec.z * vec.z);
            var pointAngle = Math.Asin((double)flatDistance / distance);
            var d = -distance * Math.Sin(math.abs(pointAngle - coneAngle)) * Math.Sign(pointAngle - coneAngle);
            return math.min(math.min((float)d, Size.y + upsideDownSign * vec.y), -upsideDownSign * vec.y);
        }

        private float ComputeSlopeDegrees(int3 pi)
        {
            var xMinus = math.clamp(pi.x - 1, -1, SizeVox);
            var xPlus = math.clamp(pi.x + 1, -1, SizeVox);
            var zMinus = math.clamp(pi.z - 1, -1, SizeVox);
            var zPlus = math.clamp(pi.z + 1, -1, SizeVox);

            var heightL = Heights[Utils.XYZToHeightIndex(new int3(xMinus, 0, pi.z), SizeVox)];
            var heightR = Heights[Utils.XYZToHeightIndex(new int3(xPlus, 0, pi.z), SizeVox)];
            var heightD = Heights[Utils.XYZToHeightIndex(new int3(pi.x, 0, zMinus), SizeVox)];
            var heightU = Heights[Utils.XYZToHeightIndex(new int3(pi.x, 0, zPlus), SizeVox)];

            var dx = (heightR - heightL) / math.max(HeightmapScale.x * 2f, 0.0001f);
            var dz = (heightU - heightD) / math.max(HeightmapScale.z * 2f, 0.0001f);
            var slopeRadians = math.atan(math.sqrt(dx * dx + dz * dz));
            return math.degrees(slopeRadians);
        }

        private static float ComputeHeightWeight(BiomeLayerData layer, float height)
        {
            if (height < layer.MinHeight || height > layer.MaxHeight)
            {
                return 0f;
            }

            if (layer.Falloff <= 0f)
            {
                return 1f;
            }

            var minWeight = math.saturate(math.unlerp(layer.MinHeight, layer.MinHeight + layer.Falloff, height));
            var maxWeight = math.saturate(1f - math.unlerp(layer.MaxHeight - layer.Falloff, layer.MaxHeight, height));
            return math.min(minWeight, maxWeight);
        }

        private static float ComputeNoiseWeight(BiomeLayerData layer, float3 p)
        {
            if (layer.NoiseAmplitude <= 0f || layer.NoiseFrequency <= 0f)
            {
                return 1f;
            }

            var noiseValue = noise.snoise(new float2(p.x, p.z) * layer.NoiseFrequency);
            return math.saturate(1f + noiseValue * layer.NoiseAmplitude);
        }

        private void ApplyTexture(ref Voxel voxel, int textureIndex, float weight)
        {
            if (IsTargetIntensity)
            {
                if (textureIndex < 28)
                {
                    voxel.SetTexture((uint)textureIndex, weight);
                }
                else if (textureIndex == 28)
                {
                    voxel.NormalizedWetnessWeight = weight;
                }
                else if (textureIndex == 29)
                {
                    voxel.NormalizedPuddlesWeight = weight;
                }
                else if (textureIndex == 30)
                {
                    voxel.NormalizedStreamsWeight = weight;
                }
                else if (textureIndex == 31)
                {
                    voxel.NormalizedLavaWeight = weight;
                }
            }
            else
            {
                if (textureIndex < 28)
                {
                    voxel.AddTexture((uint)textureIndex, weight);
                }
                else if (textureIndex == 28)
                {
                    voxel.NormalizedWetnessWeight += weight;
                }
                else if (textureIndex == 29)
                {
                    voxel.NormalizedPuddlesWeight += weight;
                }
                else if (textureIndex == 30)
                {
                    voxel.NormalizedStreamsWeight += weight;
                }
                else if (textureIndex == 31)
                {
                    voxel.NormalizedLavaWeight += weight;
                }
            }
        }

        public void Dispose()
        {
            if (Voxels.IsCreated)
            {
                Voxels.Dispose();
            }

            if (Heights.IsCreated)
            {
                Heights.Dispose();
            }

            if (Layers.IsCreated)
            {
                Layers.Dispose();
            }
        }
    }

    public struct BiomeLayerData
    {
        public int TextureIndex;
        public float MinHeight;
        public float MaxHeight;
        public float Falloff;
        public float MinSlope;
        public float MaxSlope;
        public float NoiseAmplitude;
        public float NoiseFrequency;
    }
}
