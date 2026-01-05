using System.Linq;
using UnityEngine;

namespace Client.World
{
    public static class PlayerSpawnService
    {
        public static Vector3 ResolveSpawnPosition(Vector3 fallback)
        {
            var points = Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
            if (points == null || points.Length == 0)
            {
                return fallback;
            }

            var preferred = points.FirstOrDefault(point => point != null && point.UseAsFallback);
            if (preferred != null)
            {
                return preferred.transform.position;
            }

            var first = points.FirstOrDefault(point => point != null);
            return first != null ? first.transform.position : fallback;
        }
    }
}
