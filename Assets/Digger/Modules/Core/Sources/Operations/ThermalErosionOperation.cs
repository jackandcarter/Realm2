using Digger.Modules.Core.Sources.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Digger.Modules.Core.Sources.Operations
{
    public class ThermalErosionOperation : IOperation<VoxelThermalErosionJob>
    {
        public Vector3 Position;
        public Vector3 Size = new Vector3(4f, 4f, 4f);
        public BrushType Brush = BrushType.Sphere;
        public float Strength = 0.35f;
        public float SlopeThreshold = 30f;

        public ModificationArea GetAreaToModify(DiggerSystem digger)
        {
            var radius = math.max(math.max(Size.x, Size.y), Size.z);
            return ModificationAreaUtils.GetSphericalAreaToModify(digger, Position, radius);
        }

        public VoxelThermalErosionJob Do(VoxelChunk chunk)
        {
            var job = new VoxelThermalErosionJob
            {
                SizeVox = chunk.SizeVox,
                SizeVox2 = chunk.SizeVox * chunk.SizeVox,
                HeightmapScale = chunk.HeightmapScale,
                ChunkAltitude = chunk.WorldPosition.y,
                Voxels = new NativeArray<Voxel>(chunk.VoxelArray, Allocator.TempJob),
                VoxelsOut = new NativeArray<Voxel>(chunk.VoxelArray, Allocator.TempJob),
                Heights = new NativeArray<float>(chunk.HeightArray, Allocator.TempJob),
                Holes = new NativeArray<int>(chunk.HolesArray, Allocator.TempJob),
                NewHolesConcurrentCounter = new NativeArray<int>(1, Allocator.TempJob),
                Brush = Brush,
                Center = Position - chunk.AbsoluteWorldPosition,
                Size = Size,
                Strength = Strength,
                SlopeThreshold = SlopeThreshold
            };
            job.PostConstruct();
            return job;
        }

        public void Complete(VoxelThermalErosionJob job, VoxelChunk chunk)
        {
            job.Voxels.Dispose();
            job.Heights.Dispose();

            job.VoxelsOut.CopyTo(chunk.VoxelArray);
            job.VoxelsOut.Dispose();

            if (job.NewHolesConcurrentCounter[0] > 0)
            {
                chunk.Cutter.Cut(job.Holes, chunk.VoxelPosition, chunk.ChunkPosition);
            }

            job.NewHolesConcurrentCounter.Dispose();
            job.Holes.Dispose();
        }
    }
}
