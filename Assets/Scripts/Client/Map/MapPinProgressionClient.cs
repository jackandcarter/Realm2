using System;
using System.Collections;
using Client;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.Map
{
    public class MapPinProgressionClient
    {
        private readonly string _baseUrl;
        private readonly bool _useMocks;

        public MapPinProgressionClient(string baseUrl, bool useMocks)
        {
            _baseUrl = baseUrl?.TrimEnd('/');
            _useMocks = useMocks;
        }

        public IEnumerator GetMapPins(
            string characterId,
            Action<MapPinProgressionSnapshot> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                onError?.Invoke(new ApiError(400, "Character id is required."));
                yield break;
            }

            if (_useMocks)
            {
                onSuccess?.Invoke(new MapPinProgressionSnapshot
                {
                    version = 0,
                    updatedAt = DateTime.UtcNow.ToString("O"),
                    pins = Array.Empty<MapPinUnlockState>()
                });
                yield break;
            }

            var url = $"{_baseUrl}/characters/{characterId}/map-pins";
            using var request = UnityWebRequest.Get(url);
            AttachAuthHeader(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var snapshot = JsonUtility.FromJson<MapPinProgressionSnapshot>(request.downloadHandler.text);
                if (snapshot == null)
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Map pin response was empty."));
                }
                else
                {
                    onSuccess?.Invoke(snapshot);
                }
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        public IEnumerator ReplaceMapPins(
            string characterId,
            MapPinProgressionUpdateRequest payload,
            Action<MapPinProgressionSnapshot> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                onError?.Invoke(new ApiError(400, "Character id is required."));
                yield break;
            }

            if (_useMocks)
            {
                onSuccess?.Invoke(new MapPinProgressionSnapshot
                {
                    version = (payload?.expectedVersion ?? 0) + 1,
                    updatedAt = DateTime.UtcNow.ToString("O"),
                    pins = payload?.pins ?? Array.Empty<MapPinUnlockState>()
                });
                yield break;
            }

            var url = $"{_baseUrl}/characters/{characterId}/map-pins";
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload ?? new MapPinProgressionUpdateRequest())))
                {
                    contentType = "application/json"
                },
                downloadHandler = new DownloadHandlerBuffer()
            };
            AttachAuthHeader(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var snapshot = JsonUtility.FromJson<MapPinProgressionSnapshot>(request.downloadHandler.text);
                if (snapshot == null)
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Map pin response was empty."));
                }
                else
                {
                    onSuccess?.Invoke(snapshot);
                }
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        private static void AttachAuthHeader(UnityWebRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(SessionManager.AuthToken))
            {
                return;
            }

            request.SetRequestHeader("Authorization", $"Bearer {SessionManager.AuthToken}");
        }
    }

    [Serializable]
    public class MapPinProgressionSnapshot
    {
        public int version;
        public string updatedAt;
        public MapPinUnlockState[] pins;
    }

    [Serializable]
    public class MapPinUnlockState
    {
        public string pinId;
        public bool unlocked;
        public string updatedAt;
    }

    [Serializable]
    public class MapPinProgressionUpdateRequest
    {
        public int expectedVersion;
        public MapPinUnlockState[] pins;
    }
}
