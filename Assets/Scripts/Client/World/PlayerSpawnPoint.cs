using UnityEngine;

namespace Client.World
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private bool useAsFallback;

        public bool UseAsFallback => useAsFallback;
    }
}
