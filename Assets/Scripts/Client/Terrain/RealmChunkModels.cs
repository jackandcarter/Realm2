using System;

namespace Client.Terrain
{
    [Serializable]
    public class RealmChunkStructure
    {
        public string structureId;
        public string realmId;
        public string chunkId;
        public string structureType;
        public string data;
        public bool isDeleted;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class RealmChunkPlot
    {
        public string plotId;
        public string realmId;
        public string chunkId;
        public string plotIdentifier;
        public string ownerUserId;
        public string data;
        public bool isDeleted;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class RealmChunkSnapshot
    {
        public string chunkId;
        public string realmId;
        public int chunkX;
        public int chunkZ;
        public string payload;
        public bool isDeleted;
        public string createdAt;
        public string updatedAt;
        public RealmChunkStructure[] structures;
        public RealmChunkPlot[] plots;
    }

    [Serializable]
    public class RealmChunkMutation
    {
        public int chunkX;
        public int chunkZ;
        public string payload;
        public bool isDeleted;
    }

    [Serializable]
    public class RealmChunkStructureMutation
    {
        public string structureId;
        public string structureType;
        public string data;
        public bool isDeleted;
    }

    [Serializable]
    public class RealmChunkPlotMutation
    {
        public string plotId;
        public string plotIdentifier;
        public string ownerUserId;
        public string data;
        public bool isDeleted;
    }

    [Serializable]
    public class RealmChunkChangeRequest
    {
        public string changeType;
        public RealmChunkMutation chunk;
        public RealmChunkStructureMutation[] structures;
        public RealmChunkPlotMutation[] plots;
    }

    [Serializable]
    public class RealmChunkChange
    {
        public string changeId;
        public string realmId;
        public string chunkId;
        public string changeType;
        public string createdAt;
        public RealmChunkSnapshot chunk;
        public RealmChunkStructure[] structures;
        public RealmChunkPlot[] plots;
    }

    [Serializable]
    public class RealmChunkSnapshotResponse
    {
        public string realmId;
        public string serverTimestamp;
        public RealmChunkSnapshot[] chunks;
    }

    [Serializable]
    public class RealmChunkChangeFeedResponse
    {
        public string realmId;
        public string serverTimestamp;
        public RealmChunkChange[] changes;
    }
}
