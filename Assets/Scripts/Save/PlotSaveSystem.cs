using System;
using System.Collections.Generic;
using System.Linq;
using Client.BuildState;
using Client.Terrain;

namespace Client.Save
{
    public static class PlotSaveSystem
    {
        public static BuildPlotDefinition[] LoadPlots(string realmId, string characterId)
        {
            var plots = BuildStateRepository.GetPlots(realmId, characterId);
            return plots?.Where(p => p != null).ToArray() ?? Array.Empty<BuildPlotDefinition>();
        }

        public static void SavePlots(string realmId, string characterId, IEnumerable<BuildPlotDefinition> definitions)
        {
            if (definitions == null)
            {
                return;
            }

            BuildStateRepository.SavePlots(realmId, characterId, definitions);
        }
    }
}
