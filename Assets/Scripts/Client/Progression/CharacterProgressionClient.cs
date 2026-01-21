using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.Progression
{
    public class CharacterProgressionClient
    {
        private readonly string _baseUrl;
        private readonly bool _useMock;

        public CharacterProgressionClient(string baseUrl, bool useMock)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _useMock = useMock;
        }

        public IEnumerator GetProgression(
            string characterId,
            Action<CharacterProgressionEnvelope> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                onError?.Invoke(new ApiError(400, "Character id is required."));
                yield break;
            }

            if (_useMock)
            {
                yield return RunMockProgression(characterId, onSuccess);
                yield break;
            }

            var url = $"{_baseUrl}/characters/{characterId}/progression";
            using var request = UnityWebRequest.Get(url);
            AttachAuthHeader(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var payload = JsonUtility.FromJson<CharacterProgressionEnvelope>(request.downloadHandler.text);
                onSuccess?.Invoke(Sanitize(payload));
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        public IEnumerator UpdateClassUnlocks(
            string characterId,
            CharacterClassUnlockEntry[] unlocks,
            int expectedVersion,
            Action<CharacterProgressionEnvelope> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                onError?.Invoke(new ApiError(400, "Character id is required."));
                yield break;
            }

            if (_useMock)
            {
                yield return RunMockUpdate(characterId, unlocks, expectedVersion, onSuccess);
                yield break;
            }

            var requestPayload = new CharacterProgressionUpdateRequest
            {
                classUnlocks = new CharacterClassUnlockUpdatePayload
                {
                    expectedVersion = expectedVersion,
                    unlocks = BuildUpdateEntries(unlocks)
                }
            };

            var json = JsonUtility.ToJson(requestPayload);
            using var request = new UnityWebRequest($"{_baseUrl}/characters/{characterId}/progression", UnityWebRequest.kHttpVerbPUT);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            AttachAuthHeader(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var payload = JsonUtility.FromJson<CharacterProgressionEnvelope>(request.downloadHandler.text);
                onSuccess?.Invoke(Sanitize(payload));
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        public IEnumerator UpdateInventory(
            string characterId,
            CharacterInventoryItemEntry[] items,
            int expectedVersion,
            Action<CharacterProgressionEnvelope> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                onError?.Invoke(new ApiError(400, "Character id is required."));
                yield break;
            }

            if (_useMock)
            {
                yield return RunMockProgression(characterId, onSuccess);
                yield break;
            }

            var requestPayload = new CharacterProgressionUpdateRequest
            {
                inventory = new CharacterInventoryUpdatePayload
                {
                    expectedVersion = expectedVersion,
                    items = items ?? Array.Empty<CharacterInventoryItemEntry>()
                }
            };

            var json = JsonUtility.ToJson(requestPayload);
            using var request = new UnityWebRequest($"{_baseUrl}/characters/{characterId}/progression", UnityWebRequest.kHttpVerbPUT);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            AttachAuthHeader(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var payload = JsonUtility.FromJson<CharacterProgressionEnvelope>(request.downloadHandler.text);
                onSuccess?.Invoke(Sanitize(payload));
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        public IEnumerator UpdateEquipment(
            string characterId,
            CharacterEquipmentEntry[] items,
            int expectedVersion,
            Action<CharacterProgressionEnvelope> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                onError?.Invoke(new ApiError(400, "Character id is required."));
                yield break;
            }

            if (_useMock)
            {
                yield return RunMockProgression(characterId, onSuccess);
                yield break;
            }

            var requestPayload = new CharacterProgressionUpdateRequest
            {
                equipment = new CharacterEquipmentUpdatePayload
                {
                    expectedVersion = expectedVersion,
                    items = items ?? Array.Empty<CharacterEquipmentEntry>()
                }
            };

            var json = JsonUtility.ToJson(requestPayload);
            using var request = new UnityWebRequest($"{_baseUrl}/characters/{characterId}/progression", UnityWebRequest.kHttpVerbPUT);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            AttachAuthHeader(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var payload = JsonUtility.FromJson<CharacterProgressionEnvelope>(request.downloadHandler.text);
                onSuccess?.Invoke(Sanitize(payload));
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        private static void AttachAuthHeader(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(SessionManager.AuthToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {SessionManager.AuthToken}");
            }
        }

        private static CharacterClassUnlockUpdateEntry[] BuildUpdateEntries(CharacterClassUnlockEntry[] unlocks)
        {
            if (unlocks == null || unlocks.Length == 0)
            {
                return Array.Empty<CharacterClassUnlockUpdateEntry>();
            }

            var result = new CharacterClassUnlockUpdateEntry[unlocks.Length];
            for (var i = 0; i < unlocks.Length; i++)
            {
                var entry = unlocks[i];
                result[i] = entry == null
                    ? null
                    : new CharacterClassUnlockUpdateEntry
                    {
                        classId = entry.classId,
                        unlocked = entry.unlocked,
                        unlockedAt = entry.unlocked ? entry.unlockedAt : null
                    };
            }

            return result;
        }

        private static CharacterProgressionEnvelope Sanitize(CharacterProgressionEnvelope payload)
        {
            if (payload == null)
            {
                payload = new CharacterProgressionEnvelope();
            }

            if (payload.progression == null)
            {
                payload.progression = new CharacterProgressionStats
                {
                    level = 1,
                    xp = 0,
                    version = 0,
                    updatedAt = DateTime.UtcNow.ToString("O")
                };
            }

            if (payload.classUnlocks == null)
            {
                payload.classUnlocks = new CharacterClassUnlockCollection
                {
                    version = 0,
                    updatedAt = DateTime.UtcNow.ToString("O"),
                    unlocks = Array.Empty<CharacterClassUnlockEntry>()
                };
            }
            else if (payload.classUnlocks.unlocks == null)
            {
                payload.classUnlocks.unlocks = Array.Empty<CharacterClassUnlockEntry>();
            }

            if (payload.inventory == null)
            {
                payload.inventory = new CharacterInventoryCollection
                {
                    version = 0,
                    updatedAt = DateTime.UtcNow.ToString("O"),
                    items = Array.Empty<CharacterInventoryItemEntry>()
                };
            }
            else if (payload.inventory.items == null)
            {
                payload.inventory.items = Array.Empty<CharacterInventoryItemEntry>();
            }

            if (payload.equipment == null)
            {
                payload.equipment = new CharacterEquipmentCollection
                {
                    version = 0,
                    updatedAt = DateTime.UtcNow.ToString("O"),
                    items = Array.Empty<CharacterEquipmentEntry>()
                };
            }
            else if (payload.equipment.items == null)
            {
                payload.equipment.items = Array.Empty<CharacterEquipmentEntry>();
            }

            if (payload.quests == null)
            {
                payload.quests = new CharacterQuestCollection
                {
                    version = 0,
                    updatedAt = DateTime.UtcNow.ToString("O"),
                    quests = Array.Empty<CharacterQuestStateEntry>()
                };
            }
            else if (payload.quests.quests == null)
            {
                payload.quests.quests = Array.Empty<CharacterQuestStateEntry>();
            }

            return payload;
        }

        private IEnumerator RunMockProgression(string characterId, Action<CharacterProgressionEnvelope> onSuccess)
        {
            yield return null;

            var now = DateTime.UtcNow.ToString("O");
            onSuccess?.Invoke(new CharacterProgressionEnvelope
            {
                progression = new CharacterProgressionStats
                {
                    level = 5,
                    xp = 1200,
                    version = 1,
                    updatedAt = now
                },
                classUnlocks = new CharacterClassUnlockCollection
                {
                    version = 2,
                    updatedAt = now,
                    unlocks = new[]
                    {
                        new CharacterClassUnlockEntry { classId = "builder", unlocked = true, unlockedAt = now },
                        new CharacterClassUnlockEntry { classId = "scout", unlocked = false, unlockedAt = string.Empty }
                    }
                },
                inventory = new CharacterInventoryCollection
                {
                    version = 1,
                    updatedAt = now,
                    items = new[]
                    {
                        new CharacterInventoryItemEntry { itemId = "timber", quantity = 42, metadataJson = "{}" }
                    }
                },
                equipment = new CharacterEquipmentCollection
                {
                    version = 1,
                    updatedAt = now,
                    items = Array.Empty<CharacterEquipmentEntry>()
                },
                quests = new CharacterQuestCollection
                {
                    version = 1,
                    updatedAt = now,
                    quests = new[]
                    {
                        new CharacterQuestStateEntry { questId = "builder_intro", status = "completed", progressJson = "{}", updatedAt = now }
                    }
                }
            });
        }

        private IEnumerator RunMockUpdate(
            string characterId,
            CharacterClassUnlockEntry[] unlocks,
            int expectedVersion,
            Action<CharacterProgressionEnvelope> onSuccess)
        {
            yield return RunMockProgression(characterId, onSuccess);
        }
    }
}
