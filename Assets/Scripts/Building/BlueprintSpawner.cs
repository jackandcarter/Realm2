using System;
using System.Collections.Generic;
using UnityEngine;

namespace Building
{
    [DefaultExecutionOrder(-50)]
    public class BlueprintSpawner : MonoBehaviour
    {
        [SerializeField] private Transform playerAnchor;
        [SerializeField] private float spawnDistance = 4f;
        [SerializeField] private float spawnHeightOffset = 0.5f;
        [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;
        [SerializeField] private List<BlueprintDefinition> blueprints = new List<BlueprintDefinition>();

        private readonly Dictionary<string, BlueprintDefinition> _lookup =
            new Dictionary<string, BlueprintDefinition>(StringComparer.OrdinalIgnoreCase);

        private bool _initialized;

        public IReadOnlyList<BlueprintDefinition> Blueprints => blueprints;

        public void SetPlayerAnchor(Transform anchor)
        {
            playerAnchor = anchor;
        }

        public bool TrySpawn(string blueprintId, out ConstructionInstance instance)
        {
            EnsureInitialized();
            instance = null;

            if (string.IsNullOrWhiteSpace(blueprintId))
            {
                return TrySpawnDefault(out instance);
            }

            if (!_lookup.TryGetValue(blueprintId.Trim(), out var definition))
            {
                Debug.LogWarning($"Blueprint '{blueprintId}' is not registered with the BlueprintSpawner.");
                return false;
            }

            return TrySpawn(definition, out instance);
        }

        public bool TrySpawnDefault(out ConstructionInstance instance)
        {
            EnsureInitialized();
            instance = null;

            if (blueprints == null || blueprints.Count == 0)
            {
                Debug.LogWarning("BlueprintSpawner has no blueprint definitions configured.");
                return false;
            }

            var definition = blueprints[0];
            if (definition == null)
            {
                Debug.LogWarning("BlueprintSpawner default definition is null.");
                return false;
            }

            return TrySpawn(definition, out instance);
        }

        private bool TrySpawn(BlueprintDefinition definition, out ConstructionInstance instance)
        {
            instance = null;
            if (definition == null || definition.Prefab == null)
            {
                Debug.LogWarning("Cannot spawn blueprint because the definition or prefab is null.");
                return false;
            }

            var anchor = ResolveAnchor();
            if (anchor == null)
            {
                Debug.LogWarning("BlueprintSpawner could not resolve a player anchor for spawning.");
                return false;
            }

            var forward = anchor.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = anchor.up;
            }

            forward.Normalize();
            var spawnPosition = anchor.position + forward * spawnDistance + Vector3.up * spawnHeightOffset + definition.PositionOffset;
            var spawnRotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(definition.RotationOffset);

            if (IsColliding(definition, spawnPosition, spawnRotation))
            {
                Debug.LogWarning($"Cannot spawn blueprint '{definition.BlueprintId}' because the spawn location is obstructed.");
                return false;
            }

            var go = Instantiate(definition.Prefab, spawnPosition, spawnRotation);
            if (go == null)
            {
                return false;
            }

            instance = go.GetComponent<ConstructionInstance>();
            if (instance == null)
            {
                instance = go.AddComponent<ConstructionInstance>();
            }

            instance.Initialize(definition.BlueprintId, placed: false);
            instance.Persist();
            return true;
        }

        private Transform ResolveAnchor()
        {
            if (playerAnchor != null)
            {
                return playerAnchor;
            }

            if (Camera.main != null)
            {
                playerAnchor = Camera.main.transform;
            }

            return playerAnchor;
        }

        private bool IsColliding(BlueprintDefinition definition, Vector3 position, Quaternion rotation)
        {
            var bounds = definition.GetBounds();
            if (bounds.size == Vector3.zero)
            {
                return false;
            }

            var center = position + rotation * bounds.center;
            var halfExtents = bounds.extents;
            return Physics.CheckBox(center, halfExtents, rotation, collisionMask);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _lookup.Clear();
            if (blueprints != null)
            {
                foreach (var blueprint in blueprints)
                {
                    if (blueprint == null || string.IsNullOrWhiteSpace(blueprint.BlueprintId))
                    {
                        continue;
                    }

                    var id = blueprint.BlueprintId.Trim();
                    if (_lookup.ContainsKey(id))
                    {
                        Debug.LogWarning($"Duplicate blueprint id detected: {id}. Only the first entry will be used.");
                        continue;
                    }

                    blueprint.CacheBounds();
                    _lookup[id] = blueprint;
                }
            }

            _initialized = true;
        }

        [Serializable]
        public class BlueprintDefinition
        {
            [SerializeField] private string blueprintId;
            [SerializeField] private GameObject prefab;
            [SerializeField] private Vector3 positionOffset = Vector3.zero;
            [SerializeField] private Vector3 rotationOffset = Vector3.zero;

            private Bounds _cachedBounds;
            private bool _boundsInitialized;

            public string BlueprintId => blueprintId;
            public GameObject Prefab => prefab;
            public Vector3 PositionOffset => positionOffset;
            public Vector3 RotationOffset => rotationOffset;

            public Bounds GetBounds()
            {
                if (!_boundsInitialized)
                {
                    CacheBounds();
                }

                return _cachedBounds;
            }

            public void CacheBounds()
            {
                if (prefab == null)
                {
                    _cachedBounds = default;
                    _boundsInitialized = true;
                    return;
                }

                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (renderers == null || renderers.Length == 0)
                {
                    _cachedBounds = default;
                    _boundsInitialized = true;
                    return;
                }

                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                var localCenter = prefab.transform.InverseTransformPoint(bounds.center);
                var localSize = prefab.transform.InverseTransformVector(bounds.size);
                _cachedBounds = new Bounds(localCenter, new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z)));
                _boundsInitialized = true;
            }
        }
    }
}
