using System.Collections.Generic;
using System.Linq;
using Client.Terrain;
using UnityEngine;

namespace Building
{
    [CreateAssetMenu(fileName = "BuildableZoneAsset", menuName = "Realm/Building/Buildable Zone Asset", order = 0)]
    public class BuildableZoneAsset : ScriptableObject
    {
        [SerializeField] private string sceneName;
        [SerializeField] private string regionId;
        [SerializeField] private List<SerializableBounds> zones = new();

        public string SceneName => sceneName;

        public string RegionId => regionId;

        public IReadOnlyList<SerializableBounds> Zones => zones;

        public void SetSceneName(string value)
        {
            sceneName = value;
        }

        public void SetRegionId(string value)
        {
            regionId = value;
        }

        public void SetZones(IEnumerable<Bounds> newZones)
        {
            zones.Clear();
            if (newZones == null)
            {
                return;
            }

            foreach (var bounds in newZones)
            {
                zones.Add(SerializableBounds.FromBounds(bounds));
            }
        }

        public void AddZone(Bounds zone)
        {
            zones.Add(SerializableBounds.FromBounds(zone));
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= zones.Count)
            {
                return;
            }

            zones.RemoveAt(index);
        }

        public List<Bounds> GetZoneBounds()
        {
            return zones.Select(z => z.ToBounds()).ToList();
        }
    }
}
