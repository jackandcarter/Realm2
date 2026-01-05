using Client.World;
using UnityEngine;

namespace Client.Player
{
    public class PlayerBootstrapper : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Vector3 fallbackSpawnPosition = new Vector3(0f, 2f, 0f);
        [SerializeField] private bool spawnOnStart = true;

        [Header("Camera Settings")]
        [SerializeField] private PlayerCameraRig cameraRig;

        private GameObject _playerInstance;

        private void Start()
        {
            if (!Application.isPlaying || !spawnOnStart)
            {
                return;
            }

            SpawnPlayer();
        }

        public void SpawnPlayer()
        {
            if (_playerInstance != null)
            {
                return;
            }

            var spawnPosition = PlayerSpawnService.ResolveSpawnPosition(fallbackSpawnPosition);
            if (playerPrefab != null)
            {
                _playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                _playerInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _playerInstance.name = "PlayerAvatar";
                _playerInstance.transform.position = spawnPosition;
                _playerInstance.transform.rotation = Quaternion.identity;
                var controller = _playerInstance.AddComponent<CharacterController>();
                controller.height = 1.8f;
                controller.radius = 0.3f;
                controller.center = new Vector3(0f, 0.9f, 0f);
                _playerInstance.AddComponent<PlayerAvatarController>();
            }

            if (cameraRig != null)
            {
                var controller = _playerInstance.GetComponent<PlayerAvatarController>();
                var target = controller != null ? controller.CameraRoot : _playerInstance.transform;
                cameraRig.SetTarget(target);
            }
        }
    }
}
