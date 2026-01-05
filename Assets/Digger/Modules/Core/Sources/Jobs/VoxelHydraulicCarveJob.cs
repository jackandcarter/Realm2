using System;
using Digger.Modules.Core.Sources;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Digger.Modules.Core.Sources.Jobs
{
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
    public struct VoxelHydraulicCarveJob : IJobParallelFor
    {
        public int SizeVox;
        public int SizeVox2;
        public BrushType Brush;
        public float3 HeightmapScale;
        public float3 Center;
        public float3 Size;
        public float Strength;
        public float SlopeThreshold;
        public float ChunkAltitude;

        [ReadOnly] [NativeDisableParallelForRestriction]
        public NativeArray<Voxel> Voxels;

        [ReadOnly] [NativeDisableParallelForRestriction]
        public NativeArray<float> Heights;

        [WriteOnly]
        public NativeArray<Voxel> VoxelsOut;

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
            var voxel = Voxels[index];
            if (Strength <= 0f)
            {
                VoxelsOut[index] = voxel;
                return;
            }

            var pi = Utils.IndexToXYZ(index, SizeVox, SizeVox2);
            var p = pi * HeightmapScale;
            var distance = ComputeDistance(p);
            if (distance < 0f)
            {
                VoxelsOut[index] = voxel;
                return;
            }

            if (!Utils.IsOnSurface(pi, HeightmapScale.y, p.y + ChunkAltitude, SizeVox, Heights))
            {
                VoxelsOut[index] = voxel;
                return;
            }

            var slopeDegrees = ComputeSlopeDegrees(pi);
            if (slopeDegrees <= SlopeThreshold)
            {
                VoxelsOut[index] = voxel;
                return;
            }

            var heightCenter = Heights[Utils.XYZToHeightIndex(new int3(pi.x, 0, pi.z), SizeVox)];
            var heightMin = GetLowestNeighborHeight(pi, heightCenter);
            var flow = math.max(0f, heightCenter - heightMin);
            if (flow <= 0f)
            {
                VoxelsOut[index] = voxel;
                return;
            }

            var slopeFactor = math.saturate((slopeDegrees - SlopeThreshold) / math.max(0.01f, 90f - SlopeThreshold));
            var normalizedFlow = flow / math.max(HeightmapScale.y, 0.0001f);
            var carveAmount = Strength * slopeFactor * normalizedFlow;
            if (carveAmount <= 0f)
            {
                VoxelsOut[index] = voxel;
                return;
            }

            voxel.Value += carveAmount;
            if (voxel.Alteration == Voxel.Unaltered)
            {
                voxel.Alteration = Voxel.FarAboveSurface;
            }

            var terrainHeightValue = p.y + ChunkAltitude - heightCenter;
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

            VoxelsOut[index] = voxel;
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

        private float GetLowestNeighborHeight(int3 pi, float centerHeight)
        {
            var minHeight = centerHeight;
            minHeight = math.min(minHeight, Heights[Utils.XYZToHeightIndex(new int3(pi.x - 1, 0, pi.z), SizeVox)]);
            minHeight = math.min(minHeight, Heights[Utils.XYZToHeightIndex(new int3(pi.x + 1, 0, pi.z), SizeVox)]);
            minHeight = math.min(minHeight, Heights[Utils.XYZToHeightIndex(new int3(pi.x, 0, pi.z - 1), SizeVox)]);
            minHeight = math.min(minHeight, Heights[Utils.XYZToHeightIndex(new int3(pi.x, 0, pi.z + 1), SizeVox)]);
            return minHeight;
        }
    }
}
