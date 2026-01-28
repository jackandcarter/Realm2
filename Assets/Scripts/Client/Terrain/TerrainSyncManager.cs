using System;
using System.Collections;
using System.Collections.Generic;
using Client;
using UnityEngine;

namespace Client.Terrain
{
    [DisallowMultipleComponent]
    public class TerrainSyncManager : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private string baseApiUrlOverride;
        [SerializeField] private bool useMockServices;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool enableRealtime = true;
        [SerializeField] private bool enablePollingFallback = true;
        [SerializeField] private float pollIntervalSeconds = 4f;
        [SerializeField] private int pollBatchLimit = 200;

        [Header("Terrain")]
        [SerializeField] private TerrainRegionManager regionManager;

        private readonly Dictionary<string, string> _lastPayloads = new(StringComparer.OrdinalIgnoreCase);
        private RealmChunkCache _cache;
        private RealmChunkApiClient _apiClient;
        private RealmChunkStreamClient _streamClient;
        private RealmChunkRealtimeController _realtimeController;
        private TerrainChunkPayloadApplier _payloadApplier;
        private Coroutine _syncRoutine;
        private Coroutine _pollRoutine;
        private string _realmId;
        private bool _isActive;
        private bool _realtimeConnected;

        public event Action<ApiError> SyncError;

        private void Awake()
        {
            if (regionManager == null)
            {
                regionManager = FindFirstObjectByType<TerrainRegionManager>(FindObjectsInactive.Include);
            }

            _cache = new RealmChunkCache();
            _payloadApplier = new TerrainChunkPayloadApplier(regionManager);

            _cache.ChunkUpdated += HandleChunkUpdated;
            _cache.ChunkRemoved += HandleChunkRemoved;
        }

        private void OnEnable()
        {
            if (autoStart)
            {
                StartSync(SessionManager.SelectedRealmId);
            }
        }

        private void OnDisable()
        {
            StopSync();
        }

        private void OnDestroy()
        {
            StopSync();

            if (_cache != null)
            {
                _cache.ChunkUpdated -= HandleChunkUpdated;
                _cache.ChunkRemoved -= HandleChunkRemoved;
            }
        }

        public void StartSync(string realmId)
        {
            if (_isActive)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(realmId))
            {
                Debug.LogWarning("TerrainSyncManager requires a realm id to start syncing.");
                return;
            }

            var baseUrl = ResolveBaseUrl();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                Debug.LogWarning("TerrainSyncManager could not resolve a base API URL.");
                return;
            }

            _realmId = realmId;
            _cache.Clear();
            _lastPayloads.Clear();
            _apiClient = new RealmChunkApiClient(baseUrl, useMockServices || ApiClientRegistry.UseMockServices);
            _payloadApplier = new TerrainChunkPayloadApplier(regionManager);

            _isActive = true;
            _syncRoutine = StartCoroutine(SyncRoutine());
        }

        public void StopSync()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;

            if (_syncRoutine != null)
            {
                StopCoroutine(_syncRoutine);
                _syncRoutine = null;
            }

            if (_pollRoutine != null)
            {
                StopCoroutine(_pollRoutine);
                _pollRoutine = null;
            }

            if (_realtimeController != null)
            {
                _realtimeController.Dispose();
                _realtimeController = null;
            }

            if (_streamClient != null)
            {
                _streamClient.Subscribed -= HandleRealtimeSubscribed;
                _streamClient.ConnectionClosed -= HandleRealtimeClosed;
                _streamClient.Dispose();
                _streamClient = null;
            }

            _realtimeConnected = false;
        }

        public void SubmitChunkMutation(
            string chunkId,
            RealmChunkChangeRequest request,
            RealmChunkChange predictedChange = null,
            Action<RealmChunkChange> onSuccess = null,
            Action<ApiError> onError = null)
        {
            if (string.IsNullOrWhiteSpace(_realmId) || string.IsNullOrWhiteSpace(chunkId))
            {
                onError?.Invoke(new ApiError(-1, "Cannot submit terrain change without a realm and chunk id."));
                return;
            }

            if (enableRealtime && _realtimeController != null)
            {
                var requestId = Guid.NewGuid().ToString("N");
                _realtimeController.PredictAndSubmitChange(chunkId, request, predictedChange, requestId);
                onSuccess?.Invoke(predictedChange);
                return;
            }

            if (_apiClient == null)
            {
                onError?.Invoke(new ApiError(-1, "Terrain API client is not initialized."));
                return;
            }

            StartCoroutine(_apiClient.RecordChange(
                _realmId,
                chunkId,
                request,
                change =>
                {
                    if (change != null)
                    {
                        _cache.ApplyChange(change);
                    }

                    onSuccess?.Invoke(change);
                },
                error =>
                {
                    onError?.Invoke(error);
                    SyncError?.Invoke(error);
                }));
        }

        private IEnumerator SyncRoutine()
        {
            yield return _apiClient.GetSnapshot(
                _realmId,
                null,
                response => _cache.ApplySnapshot(response),
                error =>
                {
                    Debug.LogWarning($"Terrain snapshot failed: {error?.Message}");
                    SyncError?.Invoke(error);
                });

            if (!_isActive)
            {
                yield break;
            }

            if (enableRealtime)
            {
                _streamClient = new RealmChunkStreamClient(ResolveBaseUrl());
                _streamClient.Subscribed += HandleRealtimeSubscribed;
                _streamClient.ConnectionClosed += HandleRealtimeClosed;
                _realtimeController = new RealmChunkRealtimeController(_cache, _streamClient);
                _realtimeConnected = false;
                yield return _realtimeController.Connect(
                    _realmId,
                    error =>
                    {
                        _realtimeConnected = false;
                        SyncError?.Invoke(error);
                    });
            }

            if (enablePollingFallback)
            {
                _pollRoutine = StartCoroutine(PollChangesRoutine());
            }
        }

        private IEnumerator PollChangesRoutine()
        {
            var wait = new WaitForSeconds(Mathf.Max(0.5f, pollIntervalSeconds));
            while (_isActive)
            {
                if (!enablePollingFallback)
                {
                    yield break;
                }

                if (_apiClient != null && (!_realtimeConnected || !enableRealtime))
                {
                    yield return _apiClient.GetChanges(
                        _realmId,
                        _cache.LastServerTimestamp,
                        pollBatchLimit,
                        response => _cache.ApplyChanges(response),
                        error => SyncError?.Invoke(error));
                }

                yield return wait;
            }
        }

        private string ResolveBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(baseApiUrlOverride))
            {
                return baseApiUrlOverride.TrimEnd('/');
            }

            if (!string.IsNullOrWhiteSpace(SessionManager.SelectedRealmServiceUrl))
            {
                return SessionManager.SelectedRealmServiceUrl.TrimEnd('/');
            }

            return ApiClientRegistry.BaseUrl;
        }

        private void HandleChunkUpdated(RealmChunkState state)
        {
            if (state == null || state.IsDeleted)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(state.Payload))
            {
                return;
            }

            if (_lastPayloads.TryGetValue(state.ChunkId, out var lastPayload) && lastPayload == state.Payload)
            {
                return;
            }

            if (_payloadApplier.TryApplyPayload(state.Payload, state.ChunkId, out var info))
            {
                _lastPayloads[state.ChunkId] = state.Payload;
                _payloadApplier.ReloadChunk(info);
            }
        }

        private void HandleChunkRemoved(string chunkId)
        {
            if (string.IsNullOrWhiteSpace(chunkId))
            {
                return;
            }

            _lastPayloads.Remove(chunkId);
        }

        private void HandleRealtimeClosed(string _)
        {
            _realtimeConnected = false;
        }

        private void HandleRealtimeSubscribed(string _)
        {
            _realtimeConnected = true;
        }
    }
}
