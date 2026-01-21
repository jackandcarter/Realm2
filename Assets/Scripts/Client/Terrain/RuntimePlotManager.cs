using System;
using System.Collections.Generic;
using System.Linq;
using Building.Runtime;
using Client.BuildState;
using Client.Player;
using Client.Save;
using UnityEngine;

namespace Client.Terrain
{
    [DisallowMultipleComponent]
    public class RuntimePlotManager : MonoBehaviour
    {
        private const float DefaultPlotHeightPadding = 0.1f;

        [SerializeField] private UnityEngine.Terrain targetTerrain;
        [SerializeField] private BuildZoneService buildZoneService;
        [SerializeField] private Transform plotRoot;
        [SerializeField] private Material defaultPlotMaterial;
        [SerializeField] private float minimumPlotSize = 1f;

        private readonly Dictionary<string, RuntimePlotInstance> _plots = new(StringComparer.OrdinalIgnoreCase);

        private string _activeRealmId;
        private string _activeCharacterId;

        public event Action<IReadOnlyList<BuildPlotDefinition>> PlotsChanged;

        private struct TerrainNormalizedArea
        {
            public float MinX;
            public float MinZ;
            public float MaxX;
            public float MaxZ;
        }

        private struct RuntimePlotInstance
        {
            public BuildPlotDefinition Definition;
            public GameObject PlotObject;
        }

        private void Awake()
        {
            if (plotRoot == null)
            {
                plotRoot = transform;
            }

            if (buildZoneService == null)
            {
                buildZoneService = BuildZoneService.Instance;
            }

            ReloadFromSave();
        }

        private void OnEnable()
        {
            SessionManager.SelectedCharacterChanged += OnCharacterChanged;
            SessionManager.SessionCleared += OnSessionCleared;
            BuildStateRepository.BuildStateUpdated += OnBuildStateUpdated;
        }

        private void OnDisable()
        {
            SessionManager.SelectedCharacterChanged -= OnCharacterChanged;
            SessionManager.SessionCleared -= OnSessionCleared;
            BuildStateRepository.BuildStateUpdated -= OnBuildStateUpdated;
        }

        public IReadOnlyCollection<BuildPlotDefinition> GetPlots()
        {
            return CreateSnapshot();
        }

        public bool TryCreatePlot(BuildPlotDefinition definition, bool persist = true, bool ignorePermissions = false)
        {
            return TryCreatePlot(definition, out _, persist, ignorePermissions);
        }

        public bool TryCreatePlot(BuildPlotDefinition definition, out string failureReason, bool persist = true, bool ignorePermissions = false)
        {
            failureReason = null;
            if (!ignorePermissions && !PlayerClassStateManager.IsArkitectAvailable)
            {
                failureReason = "Only builders can place plots at runtime.";
                Debug.LogWarning(failureReason);
                return false;
            }

            if (definition == null)
            {
                failureReason = "Cannot create a null plot definition.";
                Debug.LogWarning(failureReason);
                return false;
            }

            if (definition.Bounds.size.x < minimumPlotSize || definition.Bounds.size.z < minimumPlotSize)
            {
                failureReason = "Plot size is too small to be created.";
                Debug.LogWarning(failureReason);
                return false;
            }

            if (_plots.ContainsKey(definition.PlotId))
            {
                failureReason = $"A plot with id '{definition.PlotId}' already exists.";
                Debug.LogWarning(failureReason);
                return false;
            }

            if (IntersectsExisting(definition.Bounds))
            {
                failureReason = $"Cannot create plot '{definition.PlotId}' because it intersects an existing plot.";
                Debug.LogWarning(failureReason);
                return false;
            }

            if (!ignorePermissions && !ValidateAgainstZones(definition.Bounds, out var zoneFailure))
            {
                failureReason = zoneFailure;
                Debug.LogWarning(zoneFailure);
                return false;
            }

            var instance = CreateRuntimeInstance(definition);
            if (instance.PlotObject == null)
            {
                failureReason = "Unable to create plot visuals.";
                return false;
            }

            _plots[instance.Definition.PlotId] = instance;
            if (persist)
            {
                Persist();
            }

            RaisePlotsChanged();
            return true;
        }

        public bool TryModifyElevation(string plotId, float elevationDelta)
        {
            if (!_plots.TryGetValue(plotId, out var instance))
            {
                Debug.LogWarning($"Cannot modify elevation for unknown plot '{plotId}'.");
                return false;
            }

            if (!PlayerClassStateManager.IsArkitectAvailable)
            {
                Debug.LogWarning("Only builders can modify plot elevation.");
                return false;
            }

            if (targetTerrain == null)
            {
                Debug.LogWarning("No terrain configured for elevation modifications.");
                return false;
            }

            if (Mathf.Approximately(elevationDelta, 0f))
            {
                return false;
            }

            var definition = instance.Definition;
            var terrainData = targetTerrain.terrainData;
            if (terrainData == null)
            {
                return false;
            }

            var bounds = definition.Bounds;
            var normalized = GetNormalizedTerrainBounds(bounds);
            if (!normalized.HasValue)
            {
                return false;
            }

            ApplyHeightDelta(terrainData, normalized.Value, elevationDelta);

            var newElevation = definition.BaseElevation + elevationDelta;
            definition.SetBaseElevation(newElevation);
            instance.Definition = definition;
            UpdatePlotObjectHeight(instance.PlotObject, newElevation);
            _plots[plotId] = instance;

            Persist();
            RaisePlotsChanged();
            return true;
        }

        public bool TryPaintMaterial(string plotId, int terrainLayerIndex)
        {
            if (!_plots.TryGetValue(plotId, out var instance))
            {
                Debug.LogWarning($"Cannot paint material for unknown plot '{plotId}'.");
                return false;
            }

            if (!PlayerClassStateManager.IsArkitectAvailable)
            {
                Debug.LogWarning("Only builders can paint plot materials.");
                return false;
            }

            if (targetTerrain == null)
            {
                Debug.LogWarning("No terrain configured for material painting.");
                return false;
            }

            var terrainData = targetTerrain.terrainData;
            if (terrainData == null)
            {
                return false;
            }

            if (terrainLayerIndex < 0 || terrainLayerIndex >= terrainData.alphamapLayers)
            {
                Debug.LogWarning("Requested terrain material index is not available on the configured terrain.");
                return false;
            }

            var definition = instance.Definition;
            var normalized = GetNormalizedTerrainBounds(definition.Bounds);
            if (!normalized.HasValue)
            {
                return false;
            }

            ApplySplatMap(terrainData, normalized.Value, terrainLayerIndex);
            definition.SetMaterialLayerIndex(terrainLayerIndex);
            instance.Definition = definition;
            _plots[plotId] = instance;

            Persist();
            RaisePlotsChanged();
            return true;
        }

        private void ApplyHeightDelta(TerrainData terrainData, TerrainNormalizedArea area, float elevationDelta)
        {
            var heightResolution = terrainData.heightmapResolution;
            var terrainSize = terrainData.size;

            var xMin = Mathf.Clamp(Mathf.RoundToInt(area.MinX * (heightResolution - 1)), 0, heightResolution - 1);
            var zMin = Mathf.Clamp(Mathf.RoundToInt(area.MinZ * (heightResolution - 1)), 0, heightResolution - 1);
            var xMax = Mathf.Clamp(Mathf.RoundToInt(area.MaxX * (heightResolution - 1)), 0, heightResolution - 1);
            var zMax = Mathf.Clamp(Mathf.RoundToInt(area.MaxZ * (heightResolution - 1)), 0, heightResolution - 1);

            var width = Mathf.Max(1, xMax - xMin + 1);
            var height = Mathf.Max(1, zMax - zMin + 1);
            var heights = terrainData.GetHeights(xMin, zMin, width, height);
            var deltaNormalized = elevationDelta / Mathf.Max(terrainSize.y, 0.0001f);

            for (var z = 0; z < height; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    var newHeight = Mathf.Clamp01(heights[z, x] + deltaNormalized);
                    heights[z, x] = newHeight;
                }
            }

            terrainData.SetHeights(xMin, zMin, heights);
        }

        private void ApplySplatMap(TerrainData terrainData, TerrainNormalizedArea area, int layerIndex)
        {
            var alphaResolution = terrainData.alphamapResolution;
            var xMin = Mathf.Clamp(Mathf.RoundToInt(area.MinX * (alphaResolution - 1)), 0, alphaResolution - 1);
            var zMin = Mathf.Clamp(Mathf.RoundToInt(area.MinZ * (alphaResolution - 1)), 0, alphaResolution - 1);
            var xMax = Mathf.Clamp(Mathf.RoundToInt(area.MaxX * (alphaResolution - 1)), 0, alphaResolution - 1);
            var zMax = Mathf.Clamp(Mathf.RoundToInt(area.MaxZ * (alphaResolution - 1)), 0, alphaResolution - 1);

            var width = Mathf.Max(1, xMax - xMin + 1);
            var height = Mathf.Max(1, zMax - zMin + 1);

            var alphaMaps = terrainData.GetAlphamaps(xMin, zMin, width, height);
            var layers = alphaMaps.GetLength(2);

            for (var z = 0; z < height; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    for (var layer = 0; layer < layers; layer++)
                    {
                        alphaMaps[z, x, layer] = layer == layerIndex ? 1f : 0f;
                    }
                }
            }

            terrainData.SetAlphamaps(xMin, zMin, alphaMaps);
        }

        private void UpdatePlotObjectHeight(GameObject plotObject, float elevation)
        {
            if (plotObject == null)
            {
                return;
            }

            var position = plotObject.transform.position;
            position.y = elevation + DefaultPlotHeightPadding;
            plotObject.transform.position = position;
        }

        private RuntimePlotInstance CreateRuntimeInstance(BuildPlotDefinition definition)
        {
            var copy = new BuildPlotDefinition(definition);
            var bounds = copy.Bounds;
            if (bounds.size.x <= 0f || bounds.size.z <= 0f)
            {
                return default;
            }

            var plotObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plotObject.name = $"Plot_{copy.PlotId}";
            plotObject.transform.SetParent(plotRoot, true);
            plotObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            plotObject.transform.position = new Vector3(bounds.center.x, copy.BaseElevation + DefaultPlotHeightPadding, bounds.center.z);
            plotObject.transform.localScale = new Vector3(bounds.size.x, bounds.size.z, 1f);

            var renderer = plotObject.GetComponent<MeshRenderer>();
            if (renderer != null && defaultPlotMaterial != null)
            {
                renderer.sharedMaterial = defaultPlotMaterial;
            }

            return new RuntimePlotInstance
            {
                Definition = copy,
                PlotObject = plotObject
            };
        }

        private bool IntersectsExisting(Bounds bounds)
        {
            foreach (var existing in _plots.Values)
            {
                if (existing.Definition.Bounds.Intersects(bounds))
                {
                    return true;
                }
            }

            return false;
        }

        private TerrainNormalizedArea? GetNormalizedTerrainBounds(Bounds bounds)
        {
            if (targetTerrain == null)
            {
                return null;
            }

            var terrainData = targetTerrain.terrainData;
            if (terrainData == null)
            {
                return null;
            }

            var terrainSize = terrainData.size;
            var terrainTransform = targetTerrain.transform;

            var min = bounds.min;
            var max = bounds.max;

            var terrainMin = terrainTransform.InverseTransformPoint(new Vector3(min.x, 0f, min.z));
            var terrainMax = terrainTransform.InverseTransformPoint(new Vector3(max.x, 0f, max.z));

            var sizeX = Mathf.Max(terrainSize.x, Mathf.Epsilon);
            var sizeZ = Mathf.Max(terrainSize.z, Mathf.Epsilon);

            var normalizedMinX = Mathf.Clamp01(terrainMin.x / sizeX);
            var normalizedMinZ = Mathf.Clamp01(terrainMin.z / sizeZ);
            var normalizedMaxX = Mathf.Clamp01(terrainMax.x / sizeX);
            var normalizedMaxZ = Mathf.Clamp01(terrainMax.z / sizeZ);

            var minX = Mathf.Min(normalizedMinX, normalizedMaxX);
            var maxX = Mathf.Max(normalizedMinX, normalizedMaxX);
            var minZ = Mathf.Min(normalizedMinZ, normalizedMaxZ);
            var maxZ = Mathf.Max(normalizedMinZ, normalizedMaxZ);

            if (Mathf.Approximately(maxX - minX, 0f) || Mathf.Approximately(maxZ - minZ, 0f))
            {
                return null;
            }

            return new TerrainNormalizedArea
            {
                MinX = minX,
                MinZ = minZ,
                MaxX = maxX,
                MaxZ = maxZ
            };
        }

        private void OnCharacterChanged(string _)
        {
            ReloadFromSave();
        }

        private void OnSessionCleared()
        {
            ReloadFromSave();
        }

        private void OnBuildStateUpdated(string realmId, string characterId)
        {
            if (!string.Equals(_activeRealmId, realmId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(_activeCharacterId, characterId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ReloadFromSave();
        }

        private void ReloadFromSave()
        {
            _activeRealmId = SessionManager.SelectedRealmId ?? "default";
            _activeCharacterId = SessionManager.SelectedCharacterId ?? "global";

            foreach (var instance in _plots.Values)
            {
                if (instance.PlotObject != null)
                {
                    Destroy(instance.PlotObject);
                }
            }

            _plots.Clear();

            var saved = PlotSaveSystem.LoadPlots(_activeRealmId, _activeCharacterId);
            if (saved != null)
            {
                foreach (var definition in saved)
                {
                    TryCreatePlot(definition, persist: false, ignorePermissions: true);
                }
            }

            RaisePlotsChanged();
        }

        private void Persist()
        {
            PlotSaveSystem.SavePlots(_activeRealmId, _activeCharacterId, _plots.Values.Select(p => p.Definition));
        }

        private void RaisePlotsChanged()
        {
            if (PlotsChanged == null)
            {
                return;
            }

            var snapshot = CreateSnapshot();
            PlotsChanged.Invoke(snapshot);
        }

        private List<BuildPlotDefinition> CreateSnapshot()
        {
            return _plots.Values.Select(p => new BuildPlotDefinition(p.Definition)).ToList();
        }

        private bool ValidateAgainstZones(Bounds bounds, out string failureReason)
        {
            failureReason = null;

            if (buildZoneService == null)
            {
                buildZoneService = BuildZoneService.Instance;
            }

            if (buildZoneService == null)
            {
                return true;
            }

            return buildZoneService.ValidatePlacement(bounds, out failureReason);
        }
    }
}
