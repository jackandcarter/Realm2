using System;
using System.Collections;
using Client;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.UI.Dock
{
    public class DockLayoutClient
    {
        private readonly string _baseUrl;
        private readonly bool _useMocks;

        public DockLayoutClient(string baseUrl, bool useMocks)
        {
            _baseUrl = baseUrl?.TrimEnd('/');
            _useMocks = useMocks;
        }

        public IEnumerator GetLayout(
            string characterId,
            string layoutKey,
            Action<DockLayoutSnapshot> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(layoutKey))
            {
                onError?.Invoke(new ApiError(400, "Character id and layout key are required."));
                yield break;
            }

            if (_useMocks)
            {
                onSuccess?.Invoke(new DockLayoutSnapshot
                {
                    layoutKey = layoutKey,
                    order = Array.Empty<string>(),
                    updatedAt = DateTime.UtcNow.ToString("O")
                });
                yield break;
            }

            var encodedKey = UnityWebRequest.EscapeURL(layoutKey);
            var url = $"{_baseUrl}/characters/{characterId}/dock-layouts/{encodedKey}";
            using var request = UnityWebRequest.Get(url);
            AttachAuthHeader(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var snapshot = JsonUtility.FromJson<DockLayoutSnapshot>(request.downloadHandler.text);
                if (snapshot == null)
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Dock layout response was empty."));
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

        public IEnumerator SaveLayout(
            string characterId,
            string layoutKey,
            DockLayoutUpdateRequest payload,
            Action<DockLayoutSnapshot> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(layoutKey))
            {
                onError?.Invoke(new ApiError(400, "Character id and layout key are required."));
                yield break;
            }

            if (_useMocks)
            {
                onSuccess?.Invoke(new DockLayoutSnapshot
                {
                    layoutKey = layoutKey,
                    order = payload?.order ?? Array.Empty<string>(),
                    updatedAt = DateTime.UtcNow.ToString("O")
                });
                yield break;
            }

            var encodedKey = UnityWebRequest.EscapeURL(layoutKey);
            var url = $"{_baseUrl}/characters/{characterId}/dock-layouts/{encodedKey}";
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload ?? new DockLayoutUpdateRequest())))
                {
                    contentType = "application/json"
                },
                downloadHandler = new DownloadHandlerBuffer()
            };
            AttachAuthHeader(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var snapshot = JsonUtility.FromJson<DockLayoutSnapshot>(request.downloadHandler.text);
                if (snapshot == null)
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Dock layout response was empty."));
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
    public class DockLayoutSnapshot
    {
        public string layoutKey;
        public string[] order;
        public string updatedAt;
    }

    [Serializable]
    public class DockLayoutUpdateRequest
    {
        public string[] order;
    }
}
