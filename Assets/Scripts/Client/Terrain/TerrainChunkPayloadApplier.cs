using System;
using System.Collections.Generic;
using System.IO;
using Digger.Modules.Core.Sources;
using UnityEngine;

namespace Client.Terrain
{
    public class TerrainChunkPayloadApplier
    {
        private readonly TerrainRegionManager _regionManager;
        private readonly Dictionary<string, TerrainRegion> _regionLookup = new(StringComparer.OrdinalIgnoreCase);

        public TerrainChunkPayloadApplier(TerrainRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public bool TryApplyPayload(string payloadJson, string chunkId, out AppliedChunkInfo info)
        {
            info = default;
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return false;
            }

            TerrainChunkPayload payload;
            try
            {
                payload = JsonUtility.FromJson<TerrainChunkPayload>(payloadJson);
            }
            catch (ArgumentException ex)
            {
                Debug.LogWarning($"Failed to parse chunk payload for {chunkId}: {ex.Message}");
                return false;
            }

            if (payload == null)
            {
                return false;
            }

            return TryApplyPayload(payload, chunkId, out info);
        }

        public bool TryApplyPayload(TerrainChunkPayload payload, string chunkId, out AppliedChunkInfo info)
        {
            info = default;
            if (payload == null)
            {
                return false;
            }

            if (payload.digger == null || string.IsNullOrEmpty(payload.digger.voxelData))
            {
                Debug.LogWarning($"Chunk payload for {chunkId} is missing voxel data.");
                return false;
            }

            if (!TryResolveRegion(payload.regionId, out var region))
            {
                Debug.LogWarning($"No terrain region found for chunk payload {chunkId} (region '{payload.regionId}').");
                return false;
            }

            var diggerSystem = ResolveDiggerSystem(region);
            if (diggerSystem == null)
            {
                Debug.LogWarning($"Region '{payload.regionId}' does not have a Digger system configured.");
                return false;
            }

            var chunkPosition = payload.chunkPosition.ToVector3Int();
            var diggerChunk = new Vector3i(chunkPosition.x, chunkPosition.y, chunkPosition.z);

            if (!TryDecodePayload(payload.digger, out var voxelBytes, out var metadataBytes))
            {
                Debug.LogWarning($"Failed to decode voxel data for chunk {chunkId}.");
                return false;
            }

            if (!EnsureInitialized(diggerSystem))
            {
                Debug.LogWarning($"Digger system for region '{payload.regionId}' failed to initialize.");
                return false;
            }

            var voxelPath = diggerSystem.GetPathVoxelFile(diggerChunk, true);
            EnsureDirectory(voxelPath);
            File.WriteAllBytes(voxelPath, voxelBytes);

            var metadataPath = diggerSystem.GetPathVoxelMetadataFile(diggerChunk, true);
            if (metadataBytes != null && metadataBytes.Length > 0)
            {
                EnsureDirectory(metadataPath);
                File.WriteAllBytes(metadataPath, metadataBytes);
            }
            else if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }

            info = new AppliedChunkInfo
            {
                ChunkId = chunkId,
                RegionId = payload.regionId,
                ChunkPosition = diggerChunk,
                DiggerSystem = diggerSystem
            };

            return true;
        }

        public bool ReloadChunk(AppliedChunkInfo info)
        {
            if (info.DiggerSystem == null)
            {
                return false;
            }

            if (!EnsureInitialized(info.DiggerSystem))
            {
                return false;
            }

            var chunk = FindChunk(info.DiggerSystem, info.ChunkPosition);
            if (chunk == null)
            {
                info.DiggerSystem.Init(LoadType.Minimal_and_LoadVoxels_and_RebuildMeshes);
                return true;
            }

            chunk.LoadVoxels(false);
            chunk.RebuildMeshes();
            return true;
        }

        public void ReloadSystem(DiggerSystem diggerSystem)
        {
            if (diggerSystem == null)
            {
                return;
            }

            if (!EnsureInitialized(diggerSystem))
            {
                return;
            }

            diggerSystem.Init(LoadType.Minimal_and_LoadVoxels_and_RebuildMeshes);
        }

        private bool TryResolveRegion(string regionId, out TerrainRegion region)
        {
            region = null;
            if (string.IsNullOrWhiteSpace(regionId))
            {
                return false;
            }

            if (_regionLookup.TryGetValue(regionId, out region) && region != null)
            {
                return true;
            }

            var manager = _regionManager;
            if (manager == null)
            {
                manager = UnityEngine.Object.FindFirstObjectByType<TerrainRegionManager>(FindObjectsInactive.Include);
            }

            if (manager == null)
            {
                return false;
            }

            foreach (var entry in manager.Regions)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.RegionId))
                {
                    continue;
                }

                _regionLookup[entry.RegionId] = entry;
            }

            if (_regionLookup.TryGetValue(regionId, out region) && region != null)
            {
                return true;
            }

            return false;
        }

        private static DiggerSystem ResolveDiggerSystem(TerrainRegion region)
        {
            if (region == null)
            {
                return null;
            }

            if (region.DiggerSystem != null)
            {
                return region.DiggerSystem;
            }

            return region.GetComponentInChildren<DiggerSystem>(includeInactive: true);
        }

        private static Chunk FindChunk(DiggerSystem diggerSystem, Vector3i chunkPosition)
        {
            if (diggerSystem == null)
            {
                return null;
            }

            var chunks = diggerSystem.GetComponentsInChildren<Chunk>(includeInactive: true);
            foreach (var chunk in chunks)
            {
                if (chunk != null && chunk.ChunkPosition.Equals(chunkPosition))
                {
                    return chunk;
                }
            }

            return null;
        }

        private static bool TryDecodePayload(TerrainDiggerPayload payload, out byte[] voxelBytes, out byte[] metadataBytes)
        {
            voxelBytes = null;
            metadataBytes = null;
            if (payload == null || string.IsNullOrEmpty(payload.voxelData))
            {
                return false;
            }

            try
            {
                voxelBytes = Convert.FromBase64String(payload.voxelData);
                metadataBytes = string.IsNullOrEmpty(payload.voxelMetadata) ? null : Convert.FromBase64String(payload.voxelMetadata);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool EnsureInitialized(DiggerSystem diggerSystem)
        {
            if (diggerSystem == null)
            {
                return false;
            }

            if (!diggerSystem.IsInitialized)
            {
                diggerSystem.PreInit(true);
                diggerSystem.Init(LoadType.Minimal_and_LoadVoxels);
            }

            return diggerSystem.IsInitialized;
        }

        private static void EnsureDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }

    public struct AppliedChunkInfo
    {
        public string ChunkId;
        public string RegionId;
        public Vector3i ChunkPosition;
        public DiggerSystem DiggerSystem;
    }
}
