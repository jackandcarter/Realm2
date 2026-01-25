using System;
using UnityEngine;

namespace Client.Terrain
{
    [Serializable]
    public class TerrainImportRequest
    {
        public TerrainImportChunk[] chunks;
        public bool emitChangeLog;
        public string changeType;
    }

    [Serializable]
    public class TerrainImportChunk
    {
        public string chunkId;
        public int chunkX;
        public int chunkZ;
        public string payload;
        public bool isDeleted;
        public TerrainImportStructure[] structures;
        public TerrainImportPlot[] plots;
    }

    [Serializable]
    public class TerrainImportStructure
    {
        public string structureId;
        public string structureType;
        public string data;
        public bool isDeleted;
    }

    [Serializable]
    public class TerrainImportPlot
    {
        public string plotId;
        public string plotIdentifier;
        public string ownerUserId;
        public string data;
        public bool isDeleted;
    }

    [Serializable]
    public class TerrainImportResponse
    {
        public RealmChunkChange[] changes;
    }

    [Serializable]
    public class TerrainChunkPayload
    {
        public int payloadVersion = 1;
        public string terrainLayer;
        public bool immutableBase;
        public string regionId;
        public SerializableVector3Int chunkPosition;
        public TerrainDiggerPayload digger;
    }

    [Serializable]
    public class TerrainDiggerPayload
    {
        public int diggerVersion;
        public string diggerDataVersion;
        public int sizeVox;
        public Vector3 heightmapScale;
        public string voxelData;
        public string voxelMetadata;
    }
}
