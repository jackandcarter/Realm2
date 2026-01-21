using System;
using System.Collections.Generic;
using Client;
using Client.Progression;
using UnityEngine;

namespace Client.Map
{
    public static class MapPinProgressionRepository
    {
        private static MapPinProgressionClient _client;
        private static MapPinProgressionSnapshot _snapshot;
        private static MapPinService _service;
        private static bool _loading;
        private static bool _suppressEvents;

        public static void SetClient(MapPinProgressionClient client)
        {
            _client = client;
            SessionManager.SelectedCharacterChanged -= HandleCharacterChanged;
            SessionManager.SelectedCharacterChanged += HandleCharacterChanged;
            SessionManager.SessionCleared -= HandleSessionCleared;
            SessionManager.SessionCleared += HandleSessionCleared;
        }

        public static void RegisterService(MapPinService service)
        {
            if (_service != null)
            {
                _service.PinUnlockChanged -= HandlePinUnlockChanged;
            }

            _service = service;
            if (_service != null)
            {
                _service.PinUnlockChanged += HandlePinUnlockChanged;
                ApplySnapshotToService();
                if (_snapshot == null && !string.IsNullOrWhiteSpace(SessionManager.SelectedCharacterId))
                {
                    RequestSnapshot(SessionManager.SelectedCharacterId);
                }
            }
        }

        private static void HandleCharacterChanged(string characterId)
        {
            _snapshot = null;
            if (!string.IsNullOrWhiteSpace(characterId))
            {
                RequestSnapshot(characterId);
            }
        }

        private static void HandleSessionCleared()
        {
            _snapshot = null;
        }

        private static void RequestSnapshot(string characterId)
        {
            if (_client == null || _loading)
            {
                return;
            }

            _loading = true;
            ProgressionCoroutineRunner.Run(
                _client.GetMapPins(
                    characterId,
                    snapshot =>
                    {
                        _loading = false;
                        _snapshot = snapshot;
                        ApplySnapshotToService();
                    },
                    error =>
                    {
                        _loading = false;
                        if (error != null)
                        {
                            Debug.LogWarning($"Map pin sync failed: {error.Message}");
                        }
                    }
                )
            );
        }

        private static void ApplySnapshotToService()
        {
            if (_service == null || _snapshot == null)
            {
                return;
            }

            _suppressEvents = true;
            _service.ApplyUnlockedPins(_snapshot.pins ?? Array.Empty<MapPinUnlockState>());
            _suppressEvents = false;
        }

        private static void HandlePinUnlockChanged(string pinId, bool unlocked)
        {
            if (_suppressEvents)
            {
                return;
            }

            var characterId = SessionManager.SelectedCharacterId;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            if (_snapshot == null)
            {
                _snapshot = new MapPinProgressionSnapshot
                {
                    version = 0,
                    updatedAt = DateTime.UtcNow.ToString("O"),
                    pins = Array.Empty<MapPinUnlockState>()
                };
            }

            var pins = new List<MapPinUnlockState>(_snapshot.pins ?? Array.Empty<MapPinUnlockState>());
            var normalized = pinId?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            var updated = false;
            for (var i = 0; i < pins.Count; i++)
            {
                if (string.Equals(pins[i].pinId, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    pins[i].unlocked = unlocked;
                    pins[i].updatedAt = DateTime.UtcNow.ToString("O");
                    updated = true;
                    break;
                }
            }

            if (!updated)
            {
                pins.Add(new MapPinUnlockState
                {
                    pinId = normalized,
                    unlocked = unlocked,
                    updatedAt = DateTime.UtcNow.ToString("O")
                });
            }

            _snapshot.pins = pins.ToArray();
            PersistSnapshot(characterId);
        }

        private static void PersistSnapshot(string characterId)
        {
            if (_client == null || _snapshot == null)
            {
                return;
            }

            var payload = new MapPinProgressionUpdateRequest
            {
                expectedVersion = _snapshot.version,
                pins = _snapshot.pins ?? Array.Empty<MapPinUnlockState>()
            };

            ProgressionCoroutineRunner.Run(
                _client.ReplaceMapPins(
                    characterId,
                    payload,
                    snapshot =>
                    {
                        if (snapshot != null)
                        {
                            _snapshot = snapshot;
                        }
                    },
                    error =>
                    {
                        if (error != null)
                        {
                            Debug.LogWarning($"Map pin save failed: {error.Message}");
                        }
                    }
                )
            );
        }
    }
}
