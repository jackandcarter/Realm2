using System;
using Digger.Modules.Core.Sources;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Digger.Modules.Core.Sources.Jobs
{
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
    public struct VoxelCaveCarveJob : IJobParallelFor
    {
        public int SizeVox;
        public int SizeVox2;
        public BrushType Brush;
        public float3 HeightmapScale;
        public float3 Center;
        public float3 Size;
        public float ChunkAltitude;
        public int NoiseSeed;
        public float NoiseScale;
        public float Threshold;
        public float MinDepth;
        public float MaxDepth;
        public float StalactiteFrequency;
        public float StalagmiteFrequency;

        [ReadOnly] [NativeDisableParallelForRestriction]
        public NativeArray<float> Heights;

        public NativeArray<Voxel> Voxels;

        [WriteOnly] [NativeDisableParallelForRestriction]
        public NativeArray<int> Holes;

        [WriteOnly] [NativeDisableParallelForRestriction]
        public NativeArray<int> NewHolesConcurrentCounter;

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
            if (NoiseScale <= 0f)
            {
                return;
            }

            var pi = Utils.IndexToXYZ(index, SizeVox, SizeVox2);
            var p = pi * HeightmapScale;
            var terrainHeight = Heights[Utils.XYZToHeightIndex(pi, SizeVox)];
            var terrainHeightValue = p.y + ChunkAltitude - terrainHeight;

            var distance = ComputeDistance(p);
            if (distance < 0f)
            {
                return;
            }

            var depth = terrainHeight - (p.y + ChunkAltitude);
            if (depth < MinDepth || depth > MaxDepth)
            {
                return;
            }

            var worldPosition = p + new float3(0f, ChunkAltitude, 0f);
            var noiseValue = NoiseUtils.Perlin(worldPosition, NoiseSeed, NoiseScale);
            var normalizedNoise = (noiseValue + 1f) * 0.5f;
            var adjustedThreshold = ApplyFeatureThresholds(worldPosition, depth);
            if (normalizedNoise >= adjustedThreshold)
            {
                return;
            }

            var voxel = Voxels[index];
            voxel.Value = math.max(voxel.Value, 1f);
            if (voxel.Alteration == Voxel.Unaltered)
            {
                voxel.Alteration = Voxel.FarAboveSurface;
            }

            voxel = Utils.AdjustAlteration(voxel, pi, HeightmapScale.y, p.y + ChunkAltitude, terrainHeightValue, SizeVox, Heights);

            if (voxel.IsAlteredNearBelowSurface || voxel.IsAlteredNearAboveSurface)
            {
                NativeCollections.Utils.IncrementAt(NewHolesConcurrentCounter, 0);
                NativeCollections.Utils.IncrementAt(Holes, Utils.XZToHoleIndex(pi.x, pi.z, SizeVox));
                if (pi.x >= 1)
                {
                    NativeCollections.Utils.IncrementAt(Holes, Utils.XZToHoleIndex(pi.x - 1, pi.z, SizeVox));
                    if (pi.z >= 1)
                    {
                        NativeCollections.Utils.IncrementAt(Holes, Utils.XZToHoleIndex(pi.x - 1, pi.z - 1, SizeVox));
                    }
                }

                if (pi.z >= 1)
                {
                    NativeCollections.Utils.IncrementAt(Holes, Utils.XZToHoleIndex(pi.x, pi.z - 1, SizeVox));
                }
            }

            Voxels[index] = voxel;
        }

        private float ApplyFeatureThresholds(float3 worldPosition, float depth)
        {
            var adjustedThreshold = Threshold;
            if (StalactiteFrequency <= 0f && StalagmiteFrequency <= 0f)
            {
                return math.clamp(adjustedThreshold, 0f, 1f);
            }

            var depthRange = math.max(0.0001f, MaxDepth - MinDepth);
            var normalizedDepth = math.saturate((depth - MinDepth) / depthRange);
            var noisePosition = new float3(worldPosition.x, 0f, worldPosition.z);

            if (StalactiteFrequency > 0f)
            {
                var stalactiteNoise = NoiseUtils.Perlin(noisePosition, NoiseSeed + 101, StalactiteFrequency);
                var stalactiteMask = (stalactiteNoise + 1f) * 0.5f;
                adjustedThreshold += stalactiteMask * (1f - normalizedDepth) * 0.25f;
            }

            if (StalagmiteFrequency > 0f)
            {
                var stalagmiteNoise = NoiseUtils.Perlin(noisePosition, NoiseSeed + 213, StalagmiteFrequency);
                var stalagmiteMask = (stalagmiteNoise + 1f) * 0.5f;
                adjustedThreshold += stalagmiteMask * normalizedDepth * 0.25f;
            }

            return math.clamp(adjustedThreshold, 0f, 1f);
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
    }
}
