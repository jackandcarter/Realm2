using System;
using System.Collections.Generic;
using System.Linq;
using Building;
using Client.Terrain;
using Digger.Modules.Core.Sources;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Building.Runtime
{
    [DefaultExecutionOrder(-200)]
    public class BuildZoneService : MonoBehaviour
    {
        private const float DefaultSampleSpacing = 2f;
        private const int VerticalSampleCount = 3;

        private static BuildZoneService _instance;

        [SerializeField] private string resourcesFolder = "BuildableZones";
        [SerializeField] private BuildableZoneAsset overrideAsset;
        [SerializeField] private float voxelClearancePadding = 0.1f;

        private readonly List<Bounds> _activeZones = new();
        private readonly Dictionary<Vector3Int, Chunk> _chunkLookup = new();
        private DiggerSystem _diggerSystem;

        public static BuildZoneService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var existing = FindObjectOfType<BuildZoneService>(true);
                    if (existing != null)
                    {
                        _instance = existing;
                    }
                    else
                    {
                        var go = new GameObject("BuildZoneService");
                        _instance = go.AddComponent<BuildZoneService>();
                        DontDestroyOnLoad(go);
                    }
                }

                return _instance;
            }
        }

        public IReadOnlyList<Bounds> ActiveZones => _activeZones;

        public bool HasActiveZones => _activeZones.Count > 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
            InitializeForScene(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InitializeForScene(scene);
        }

        public void InitializeForScene(Scene scene)
        {
            _activeZones.Clear();
            _chunkLookup.Clear();
            _diggerSystem = FindObjectOfType<DiggerSystem>();
            if (_diggerSystem != null)
            {
                CacheChunks(_diggerSystem);
            }

            var asset = overrideAsset ?? LoadAssetForScene(scene);
            if (asset == null)
            {
                return;
            }

            foreach (var serializable in asset.Zones ?? Array.Empty<SerializableBounds>())
            {
                _activeZones.Add(serializable.ToBounds());
            }
        }

        private void CacheChunks(DiggerSystem digger)
        {
            _chunkLookup.Clear();
            if (digger == null)
            {
                return;
            }

            var chunks = digger.GetComponentsInChildren<Chunk>(includeInactive: true);
            foreach (var chunk in chunks)
            {
                if (chunk == null)
                {
                    continue;
                }

                var cp = chunk.ChunkPosition;
                var key = new Vector3Int(cp.x, cp.y, cp.z);
                if (_chunkLookup.ContainsKey(key))
                {
                    continue;
                }

                _chunkLookup.Add(key, chunk);
            }
        }

        private BuildableZoneAsset LoadAssetForScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var candidates = Resources.LoadAll<BuildableZoneAsset>(resourcesFolder);
            if (candidates == null || candidates.Length == 0)
            {
                return null;
            }

            var sceneName = scene.name;
            var regionId = scene.path;

            foreach (var candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(candidate.SceneName) && !string.Equals(candidate.SceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(candidate.RegionId) && !string.Equals(candidate.RegionId, regionId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        public bool IsPositionInsideZone(Vector3 worldPosition, out Bounds containingZone)
        {
            foreach (var zone in _activeZones)
            {
                if (zone.Contains(new Vector3(worldPosition.x, zone.center.y, worldPosition.z)))
                {
                    containingZone = zone;
                    return true;
                }
            }

            containingZone = default;
            return false;
        }

        public bool IsBoundsInsideAnyZone(Bounds bounds, out Bounds containingZone)
        {
            foreach (var zone in _activeZones)
            {
                if (IsBoundsInside(zone, bounds))
                {
                    containingZone = zone;
                    return true;
                }
            }

            containingZone = default;
            return false;
        }

        private static bool IsBoundsInside(Bounds container, Bounds candidate)
        {
            var containerMin = container.min;
            var containerMax = container.max;
            var candidateMin = candidate.min;
            var candidateMax = candidate.max;

            return candidateMin.x >= containerMin.x &&
                   candidateMax.x <= containerMax.x &&
                   candidateMin.z >= containerMin.z &&
                   candidateMax.z <= containerMax.z;
        }

        public bool ValidatePlacement(Bounds bounds, out string failureReason)
        {
            failureReason = null;
            if (!HasActiveZones)
            {
                return true;
            }

            if (!IsBoundsInsideAnyZone(bounds, out _))
            {
                failureReason = "Target plot must be inside an approved build zone.";
                return false;
            }

            if (_diggerSystem == null)
            {
                _diggerSystem = FindObjectOfType<DiggerSystem>();
                if (_diggerSystem != null && _chunkLookup.Count == 0)
                {
                    CacheChunks(_diggerSystem);
                }
            }

            if (_diggerSystem == null)
            {
                return true;
            }

            if (!IsAreaCleared(bounds))
            {
                failureReason = "Plot overlaps uncleared terrain voxels.";
                return false;
            }

            return true;
        }

        private bool IsAreaCleared(Bounds bounds)
        {
            if (_diggerSystem == null)
            {
                return true;
            }

            var spacing = Mathf.Max(0.5f, DefaultSampleSpacing);
            var min = bounds.min;
            var max = bounds.max;

            for (var x = min.x; x <= max.x + 0.01f; x += spacing)
            {
                for (var z = min.z; z <= max.z + 0.01f; z += spacing)
                {
                    var samplePoint = new Vector3(x, min.y + voxelClearancePadding, z);
                    if (!IsPointClear(samplePoint))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsPointClear(Vector3 worldPoint)
        {
            if (_diggerSystem == null)
            {
                return true;
            }

            if (!_chunkLookup.Any())
            {
                CacheChunks(_diggerSystem);
            }

            var localPoint = _diggerSystem.transform.InverseTransformPoint(worldPoint);
            var heightmapScale = _diggerSystem.HeightmapScale;
            if (math.abs(heightmapScale.x) < 1e-5f || math.abs(heightmapScale.y) < 1e-5f || math.abs(heightmapScale.z) < 1e-5f)
            {
                return true;
            }

            var voxelPoint = new float3(localPoint.x / heightmapScale.x,
                                        localPoint.y / heightmapScale.y,
                                        localPoint.z / heightmapScale.z);

            var chunkSize = _diggerSystem.SizeOfMesh;
            var chunkCoord = new Vector3Int(Mathf.FloorToInt(voxelPoint.x / chunkSize),
                                            Mathf.FloorToInt(voxelPoint.y / chunkSize),
                                            Mathf.FloorToInt(voxelPoint.z / chunkSize));

            if (!_chunkLookup.TryGetValue(chunkCoord, out var chunk) || chunk == null)
            {
                return true;
            }

            chunk.LoadVoxels(false);
            var voxelChunk = chunk.GetComponentInChildren<VoxelChunk>();
            if (voxelChunk == null || voxelChunk.VoxelArray == null || voxelChunk.VoxelArray.Length == 0)
            {
                return true;
            }

            var chunkVoxelOrigin = chunk.VoxelPosition;
            var relative = new int3(Mathf.Clamp((int)Mathf.Floor(voxelPoint.x - chunkVoxelOrigin.x), 0, voxelChunk.SizeVox - 1),
                                    Mathf.Clamp((int)Mathf.Floor(voxelPoint.y - chunkVoxelOrigin.y), 0, voxelChunk.SizeVox - 1),
                                    Mathf.Clamp((int)Mathf.Floor(voxelPoint.z - chunkVoxelOrigin.z), 0, voxelChunk.SizeVox - 1));

            var sizeVox = voxelChunk.SizeVox;
            var sizeVox2 = sizeVox * sizeVox;

            for (var i = 0; i < VerticalSampleCount; i++)
            {
                var sampleY = Mathf.Clamp(relative.y - i, 0, sizeVox - 1);
                var index = relative.x * sizeVox2 + sampleY * sizeVox + relative.z;
                if (index < 0 || index >= voxelChunk.VoxelArray.Length)
                {
                    continue;
                }

                var voxel = voxelChunk.VoxelArray[index];
                if (voxel.IsInside)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
