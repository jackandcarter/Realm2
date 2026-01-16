using System.Collections.Generic;

namespace Client.Combat.Stats
{
    public interface ICombatStatSnapshotProvider
    {
        IReadOnlyDictionary<string, float> GetStatsSnapshot();
    }
}
