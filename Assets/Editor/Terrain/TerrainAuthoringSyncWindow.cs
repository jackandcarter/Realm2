#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Building;
using Client;
using Client.Building;
using Client.Terrain;
using Digger.Modules.Core.Sources;
using EditorUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Realm.EditorTools
{
    public class TerrainAuthoringSyncWindow : EditorWindow
    {
        private enum RegionSelectionMode
        {
            AllInScene,
            SelectedObjects
        }

        private string _realmId;
        private string _worldApiUrl;
        private string _terrainApiUrl;
        private string _authToken;
        private bool _useMocks;
        private RegionSelectionMode _selectionMode = RegionSelectionMode.AllInScene;
        private BuildableZoneAsset _buildZoneAsset;
        private string _zoneIdPrefix;
        private string _terrainLayer = "base";
        private bool _markImmutableBase = true;
        private bool _includeVoxelMetadata = true;
        private bool _emitTerrainImportChangeLog;
        private string _terrainImportChangeType = "terrain:import";
        private string _lastExportPath;
        private string _defaultRegionId;
        private DiggerSystem _diggerOverride;

        [MenuItem("Tools/Realm/Terrain Authoring Sync")]
        public static void ShowWindow()
        {
            var window = GetWindow<TerrainAuthoringSyncWindow>("Terrain Authoring Sync");
            window.minSize = new Vector2(420f, 360f);
        }

        private void OnEnable()
        {
            _worldApiUrl ??= ApiClientRegistry.BaseUrl;
            _terrainApiUrl ??= ApiClientRegistry.BaseUrl;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            _realmId = EditorGUILayout.TextField(new GUIContent("Realm Id"), _realmId);
            _worldApiUrl = EditorGUILayout.TextField(new GUIContent("World API URL"), _worldApiUrl);
            _terrainApiUrl = EditorGUILayout.TextField(new GUIContent("Terrain API URL"), _terrainApiUrl);
            _authToken = EditorGUILayout.TextField(new GUIContent("Auth Token"), _authToken);
            _useMocks = EditorGUILayout.Toggle(new GUIContent("Use Mock Services"), _useMocks);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Region Metadata", EditorStyles.boldLabel);
            _selectionMode = (RegionSelectionMode)EditorGUILayout.EnumPopup(new GUIContent("Region Selection"), _selectionMode);

            if (GUILayout.Button(new GUIContent("Publish Regions", "Publish terrain region metadata to the backend.")))
            {
                PublishRegions();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build Zones", EditorStyles.boldLabel);
            _buildZoneAsset = (BuildableZoneAsset)EditorGUILayout.ObjectField(
                new GUIContent("Build Zone Asset"),
                _buildZoneAsset,
                typeof(BuildableZoneAsset),
                false);
            _zoneIdPrefix = EditorGUILayout.TextField(new GUIContent("Zone Id Prefix"), _zoneIdPrefix);

            if (GUILayout.Button(new GUIContent("Publish Build Zones", "Replace build zones in the backend with the zones from this asset.")))
            {
                PublishBuildZones();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Terrain Import/Export", EditorStyles.boldLabel);
            _terrainLayer = EditorGUILayout.TextField(new GUIContent("Terrain Layer", "Layer name stored in terrain payloads."), _terrainLayer);
            _markImmutableBase = EditorGUILayout.Toggle(new GUIContent("Immutable Base", "Marks payloads as immutable base terrain."), _markImmutableBase);
            _includeVoxelMetadata = EditorGUILayout.Toggle(new GUIContent("Include Voxel Metadata", "Include voxel metadata files with the export payload."), _includeVoxelMetadata);
            _emitTerrainImportChangeLog = EditorGUILayout.Toggle(new GUIContent("Emit Change Log", "Emit change log entries when importing terrain."), _emitTerrainImportChangeLog);
            _terrainImportChangeType = EditorGUILayout.TextField(new GUIContent("Change Type", "Change type stored in the import change log."), _terrainImportChangeType);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Export Terrain Payload", "Export chunk payloads to a JSON file.")))
                {
                    ExportTerrainPayload();
                }

                if (GUILayout.Button(new GUIContent("Upload Terrain Payload", "Upload chunk payloads directly to the backend.")))
                {
                    UploadTerrainPayload();
                }
            }

            if (!string.IsNullOrWhiteSpace(_lastExportPath))
            {
                EditorGUILayout.HelpBox($"Last export: {_lastExportPath}", MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Base Terrain Sync", EditorStyles.boldLabel);
            _defaultRegionId = EditorGUILayout.TextField(new GUIContent("Default Region Id", "Used when no TerrainRegion is available."), _defaultRegionId);
            _diggerOverride = (DiggerSystem)EditorGUILayout.ObjectField(
                new GUIContent("Digger Override", "Optional Digger system to sync instead of scanning the scene."),
                _diggerOverride,
                typeof(DiggerSystem),
                true);

            if (GUILayout.Button(new GUIContent("Sync Base Terrain Now", "Upload base terrain payloads with immutable flags.")))
            {
                SyncBaseTerrain();
            }
        }

        private void PublishRegions()
        {
            if (!ValidateConnection(_terrainApiUrl))
            {
                return;
            }

            var regions = ResolveRegions();
            if (regions.Count == 0)
            {
                EditorUtility.DisplayDialog("Terrain Authoring Sync", "No terrain regions found for the selected mode.", "Ok");
                return;
            }

            EditorCoroutineRunner.Start(PublishRegionsRoutine(regions));
        }

        private IEnumerator PublishRegionsRoutine(List<TerrainRegion> regions)
        {
            using var authScope = new AuthTokenScope(_authToken);
            var client = new TerrainRegionApiClient(_terrainApiUrl, _useMocks);
            var errors = new List<string>();

            foreach (var region in regions)
            {
                if (region == null || string.IsNullOrWhiteSpace(region.RegionId))
                {
                    errors.Add("Region is missing a Region Id.");
                    continue;
                }

                var payload = new TerrainRegionPayload
                {
                    zoneId = region.ZoneId,
                    mapWorldBounds = SerializableRect.FromRect(region.MapWorldBounds),
                    chunkOriginOffset = region.ChunkOriginOffset,
                    chunkSizeOverride = region.ChunkSizeOverride,
                    chunkSize = region.GetChunkSize(),
                    useTerrainBounds = region.UseTerrainBounds,
                    miniMapTextureName = region.MiniMapTexture != null ? region.MiniMapTexture.name : null,
                    worldMapTextureName = region.WorldMapTexture != null ? region.WorldMapTexture.name : null
                };

                var request = new TerrainRegionRequest
                {
                    regionId = region.RegionId,
                    name = region.GetDisplayName(),
                    bounds = SerializableBounds.FromBounds(region.GetWorldBounds()),
                    terrainCount = region.Terrains?.Count ?? 0,
                    payload = payload
                };

                ApiError error = null;
                yield return client.UpsertRegion(_realmId, request, _ => { }, err => error = err);
                if (error != null)
                {
                    errors.Add($"{region.RegionId}: {error.Message}");
                }
            }

            ReportCompletion("Region publish complete.", errors);
        }

        private void PublishBuildZones()
        {
            if (!ValidateConnection(_worldApiUrl))
            {
                return;
            }

            if (_buildZoneAsset == null)
            {
                EditorUtility.DisplayDialog("Terrain Authoring Sync", "Assign a Buildable Zone Asset before publishing.", "Ok");
                return;
            }

            EditorCoroutineRunner.Start(PublishBuildZonesRoutine());
        }

        private IEnumerator PublishBuildZonesRoutine()
        {
            using var authScope = new AuthTokenScope(_authToken);
            var client = new BuildZoneApiClient(_worldApiUrl, _useMocks);
            var zones = _buildZoneAsset?.Zones;
            if (zones == null || zones.Count == 0)
            {
                EditorUtility.DisplayDialog("Terrain Authoring Sync", "No build zones found on the selected asset.", "Ok");
                yield break;
            }

            var prefix = string.IsNullOrWhiteSpace(_zoneIdPrefix)
                ? ResolveZonePrefix()
                : _zoneIdPrefix.Trim();

            var definitions = new BuildZoneDefinition[zones.Count];
            for (var i = 0; i < zones.Count; i++)
            {
                var bounds = zones[i].ToBounds();
                definitions[i] = new BuildZoneDefinition
                {
                    zoneId = string.IsNullOrWhiteSpace(prefix) ? $"zone-{i + 1}" : $"{prefix}-zone-{i + 1}",
                    label = $"Zone {i + 1}",
                    bounds = SerializableBounds.FromBounds(bounds)
                };
            }

            ApiError error = null;
            yield return client.ReplaceZones(
                _realmId,
                new BuildZoneUpsertRequest { zones = definitions },
                _ => { },
                err => error = err);

            var errors = new List<string>();
            if (error != null)
            {
                errors.Add(error.Message);
            }

            ReportCompletion("Build zone publish complete.", errors);
        }

        private void ExportTerrainPayload()
        {
            var payload = BuildTerrainImportPayload();
            if (payload == null)
            {
                return;
            }

            var path = EditorUtility.SaveFilePanel("Export Terrain Payload", Application.dataPath, "terrain-import.json", "json");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            File.WriteAllText(path, JsonUtility.ToJson(payload, true), Encoding.UTF8);
            _lastExportPath = path;
            EditorUtility.DisplayDialog("Terrain Authoring Sync", "Terrain payload exported.", "Ok");
        }

        private void UploadTerrainPayload()
        {
            if (!ValidateConnection(_terrainApiUrl))
            {
                return;
            }

            var payload = BuildTerrainImportPayload();
            if (payload == null)
            {
                return;
            }

            EditorCoroutineRunner.Start(UploadTerrainPayloadRoutine(payload));
        }

        private void SyncBaseTerrain()
        {
            if (!ValidateConnection(_terrainApiUrl))
            {
                return;
            }

            var payload = BuildTerrainImportPayload(forceBaseTerrain: true);
            if (payload == null)
            {
                return;
            }

            EditorCoroutineRunner.Start(UploadTerrainPayloadRoutine(payload));
        }

        private IEnumerator UploadTerrainPayloadRoutine(TerrainImportRequest payload)
        {
            using var authScope = new AuthTokenScope(_authToken);
            var client = new TerrainImportApiClient(_terrainApiUrl, _useMocks);
            ApiError error = null;
            yield return client.ImportTerrain(_realmId, payload, _ => { }, err => error = err);

            var errors = new List<string>();
            if (error != null)
            {
                errors.Add(error.Message);
            }

            ReportCompletion("Terrain import complete.", errors);
        }

        private TerrainImportRequest BuildTerrainImportPayload(bool forceBaseTerrain = false)
        {
            var diggerSystems = ResolveDiggerSystems();
            if (diggerSystems.Count == 0)
            {
                EditorUtility.DisplayDialog("Terrain Authoring Sync", "No Digger systems found for export.", "Ok");
                return null;
            }

            var chunks = new List<TerrainImportChunk>();
            var errors = new List<string>();

            foreach (var digger in diggerSystems)
            {
                if (digger == null)
                {
                    errors.Add("Digger system was null.");
                    continue;
                }

                var regionId = ResolveRegionIdForDigger(digger);

                var diggerChunks = digger.GetComponentsInChildren<Chunk>(includeInactive: true);
                foreach (var diggerChunk in diggerChunks)
                {
                    if (diggerChunk == null)
                    {
                        continue;
                    }

                    var chunkPosition = diggerChunk.ChunkPosition;
                    var voxelPath = digger.GetEditorOnlyPathVoxelFile(chunkPosition);
                    if (!File.Exists(voxelPath))
                    {
                        errors.Add($"{regionId}: Missing voxel file for {Chunk.GetName(chunkPosition)}.");
                        continue;
                    }

                    var terrainLayer = forceBaseTerrain ? "base" : _terrainLayer;
                    var immutableBase = forceBaseTerrain || _markImmutableBase;
                    var payload = new TerrainChunkPayload
                    {
                        terrainLayer = string.IsNullOrWhiteSpace(terrainLayer) ? null : terrainLayer.Trim(),
                        immutableBase = immutableBase,
                        regionId = regionId,
                        chunkPosition = new SerializableVector3Int(new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z)),
                        digger = BuildDiggerPayload(digger, chunkPosition, voxelPath)
                    };

                    if (payload.digger == null)
                    {
                        errors.Add($"{regionId}: Failed to read voxel data for {Chunk.GetName(chunkPosition)}.");
                        continue;
                    }

                    chunks.Add(new TerrainImportChunk
                    {
                        chunkId = BuildChunkId(regionId, chunkPosition),
                        chunkX = chunkPosition.x,
                        chunkZ = chunkPosition.z,
                        payload = JsonUtility.ToJson(payload)
                    });
                }
            }

            if (errors.Count > 0)
            {
                ReportCompletion("Terrain export completed with errors.", errors);
            }

            if (chunks.Count == 0)
            {
                EditorUtility.DisplayDialog("Terrain Authoring Sync", "No terrain chunks were exported.", "Ok");
                return null;
            }

            return new TerrainImportRequest
            {
                chunks = chunks.ToArray(),
                emitChangeLog = forceBaseTerrain ? false : _emitTerrainImportChangeLog,
                changeType = forceBaseTerrain
                    ? "terrain:base-import"
                    : string.IsNullOrWhiteSpace(_terrainImportChangeType) ? null : _terrainImportChangeType.Trim()
            };
        }

        private TerrainDiggerPayload BuildDiggerPayload(DiggerSystem digger, Vector3i chunkPosition, string voxelPath)
        {
            try
            {
                var voxelBytes = File.ReadAllBytes(voxelPath);
                var voxelMetadataPath = digger.GetEditorOnlyPathVoxelMetadataFile(chunkPosition);
                var metadataBytes = _includeVoxelMetadata && File.Exists(voxelMetadataPath)
                    ? File.ReadAllBytes(voxelMetadataPath)
                    : null;

                return new TerrainDiggerPayload
                {
                    diggerVersion = DiggerSystem.DiggerVersion,
                    diggerDataVersion = digger.Version.ToString(),
                    sizeVox = digger.SizeVox,
                    heightmapScale = digger.HeightmapScale,
                    voxelData = Convert.ToBase64String(voxelBytes),
                    voxelMetadata = metadataBytes != null ? Convert.ToBase64String(metadataBytes) : null
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to build digger payload for {Chunk.GetName(chunkPosition)}: {ex.Message}");
                return null;
            }
        }

        private static DiggerSystem ResolveDiggerSystem(TerrainRegion region)
        {
            if (region == null)
            {
                return null;
            }

            if (region.DiggerSystem != null)
            {
                return region.DiggerSystem;
            }

            return region.GetComponentInChildren<DiggerSystem>(includeInactive: true);
        }

        private List<DiggerSystem> ResolveDiggerSystems()
        {
            if (_diggerOverride != null)
            {
                return new List<DiggerSystem> { _diggerOverride };
            }

            var regions = ResolveRegions();
            var diggers = regions
                .Select(ResolveDiggerSystem)
                .Where(digger => digger != null)
                .Distinct()
                .ToList();

            if (diggers.Count > 0)
            {
                return diggers;
            }

            return FindObjectsByType<DiggerSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
        }

        private string ResolveRegionIdForDigger(DiggerSystem digger)
        {
            var region = digger != null ? digger.GetComponentInParent<TerrainRegion>() : null;
            if (region != null && !string.IsNullOrWhiteSpace(region.RegionId))
            {
                return region.RegionId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(_defaultRegionId))
            {
                return _defaultRegionId.Trim();
            }

            var scene = SceneManager.GetActiveScene();
            return scene.IsValid() ? scene.name : "region";
        }

        private static string BuildChunkId(string regionId, Vector3i chunkPosition)
        {
            var prefix = string.IsNullOrWhiteSpace(regionId) ? "region" : regionId.Trim();
            return $"{prefix}:{chunkPosition.x}:{chunkPosition.y}:{chunkPosition.z}";
        }

        private List<TerrainRegion> ResolveRegions()
        {
            switch (_selectionMode)
            {
                case RegionSelectionMode.SelectedObjects:
                    return Selection.gameObjects
                        .SelectMany(go => go.GetComponentsInChildren<TerrainRegion>(true))
                        .Distinct()
                        .ToList();
                case RegionSelectionMode.AllInScene:
                default:
                    return TerrainRegionSelectionUtility.FindRegionsInScene();
            }
        }

        private bool ValidateConnection(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(_realmId))
            {
                EditorUtility.DisplayDialog("Terrain Authoring Sync", "Realm Id is required.", "Ok");
                return false;
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                EditorUtility.DisplayDialog("Terrain Authoring Sync", "API URL is required.", "Ok");
                return false;
            }

            return true;
        }

        private string ResolveZonePrefix()
        {
            if (_buildZoneAsset != null && !string.IsNullOrWhiteSpace(_buildZoneAsset.RegionId))
            {
                return _buildZoneAsset.RegionId.Trim();
            }

            var scene = SceneManager.GetActiveScene();
            return scene.IsValid() ? scene.name : string.Empty;
        }

        private static void ReportCompletion(string title, List<string> errors)
        {
            if (errors.Count == 0)
            {
                EditorUtility.DisplayDialog("Terrain Authoring Sync", title, "Ok");
                return;
            }

            EditorUtility.DisplayDialog(
                "Terrain Authoring Sync",
                $"{title}\n\nErrors:\n- {string.Join("\n- ", errors)}",
                "Ok");
        }

        private sealed class AuthTokenScope : IDisposable
        {
            private readonly string _previousAuthToken;
            private readonly string _previousRefreshToken;

            public AuthTokenScope(string token)
            {
                _previousAuthToken = SessionManager.AuthToken;
                _previousRefreshToken = SessionManager.RefreshToken;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    SessionManager.SetTokens(token, _previousRefreshToken);
                }
            }

            public void Dispose()
            {
                SessionManager.SetTokens(_previousAuthToken, _previousRefreshToken);
            }
        }
    }
}
#endif
