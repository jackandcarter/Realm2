using Digger.Modules.Core.Sources.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Digger.Modules.Core.Sources.Operations
{
    public class NoiseShapeOperation : IOperation<VoxelNoiseShapeJob>
    {
        public Vector3 Position;
        public Vector3 Size = new Vector3(4f, 4f, 4f);
        public BrushType Brush = BrushType.Sphere;
        public float Intensity = 1f;
        public int NoiseSeed;
        public int NoiseOctaves = 4;
        public float NoiseFrequency = 0.05f;
        public float RidgeSharpness;
        public int TerraceSteps;

        public ModificationArea GetAreaToModify(DiggerSystem digger)
        {
            var radius = math.max(math.max(Size.x, Size.y), Size.z);
            return ModificationAreaUtils.GetSphericalAreaToModify(digger, Position, radius);
        }

        public VoxelNoiseShapeJob Do(VoxelChunk chunk)
        {
            var job = new VoxelNoiseShapeJob
            {
                SizeVox = chunk.SizeVox,
                SizeVox2 = chunk.SizeVox * chunk.SizeVox,
                HeightmapScale = chunk.HeightmapScale,
                ChunkAltitude = chunk.WorldPosition.y,
                Voxels = new NativeArray<Voxel>(chunk.VoxelArray, Allocator.TempJob),
                Heights = new NativeArray<float>(chunk.HeightArray, Allocator.TempJob),
                Holes = new NativeArray<int>(chunk.HolesArray, Allocator.TempJob),
                NewHolesConcurrentCounter = new NativeArray<int>(1, Allocator.TempJob),
                Brush = Brush,
                Intensity = Intensity,
                Center = new float3(Position.x, Position.y, Position.z) - chunk.AbsoluteWorldPosition,
                Size = Size,
                NoiseSeed = NoiseSeed,
                NoiseOctaves = NoiseOctaves,
                NoiseFrequency = NoiseFrequency,
                RidgeSharpness = RidgeSharpness,
                TerraceSteps = TerraceSteps
            };
            job.PostConstruct();
            return job;
        }

        public void Complete(VoxelNoiseShapeJob job, VoxelChunk chunk)
        {
            job.Voxels.CopyTo(chunk.VoxelArray);
            job.Voxels.Dispose();
            job.Heights.Dispose();

            if (job.NewHolesConcurrentCounter[0] > 0)
            {
                chunk.Cutter.Cut(job.Holes, chunk.VoxelPosition, chunk.ChunkPosition);
            }

            job.NewHolesConcurrentCounter.Dispose();
            job.Holes.Dispose();
        }
    }
}
