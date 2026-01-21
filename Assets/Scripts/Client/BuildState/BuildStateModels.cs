using System;
using Building;
using Client.Terrain;
namespace Client.BuildState
{
    [Serializable]
    public class BuildStateSnapshot
    {
        public string id;
        public string realmId;
        public string characterId;
        public BuildPlotDefinition[] plots;
        public ConstructionInstance.SerializableConstructionState[] constructions;
        public string updatedAt;
    }

    [Serializable]
    public class BuildStateUpdateRequest
    {
        public BuildPlotDefinition[] plots;
        public ConstructionInstance.SerializableConstructionState[] constructions;
    }

}
