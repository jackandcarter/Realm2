using System;
using System.Collections;
using Client;
using Client.Terrain;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.BuildState
{
    public class BuildStateClient
    {
        private readonly string _baseUrl;
        private readonly bool _useMocks;

        public BuildStateClient(string baseUrl, bool useMocks)
        {
            _baseUrl = baseUrl?.TrimEnd('/');
            _useMocks = useMocks;
        }

        public IEnumerator GetBuildState(
            string characterId,
            string realmId,
            Action<BuildStateSnapshot> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                onError?.Invoke(new ApiError(400, "Character id is required."));
                yield break;
            }

            if (_useMocks)
            {
                onSuccess?.Invoke(new BuildStateSnapshot
                {
                    id = string.Empty,
                    characterId = characterId,
                    realmId = realmId,
                    plots = Array.Empty<BuildPlotDefinition>(),
                    constructions = Array.Empty<global::Building.ConstructionInstance.SerializableConstructionState>(),
                    updatedAt = DateTime.UtcNow.ToString("O")
                });
                yield break;
            }

            var query = !string.IsNullOrWhiteSpace(realmId)
                ? $"?realmId={UnityWebRequest.EscapeURL(realmId)}"
                : string.Empty;
            var url = $"{_baseUrl}/characters/{characterId}/build-state{query}";
            using var request = UnityWebRequest.Get(url);
            AttachAuthHeader(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var snapshot = JsonUtility.FromJson<BuildStateSnapshot>(request.downloadHandler.text);
                if (snapshot == null)
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Build state response was empty."));
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

        public IEnumerator ReplaceBuildState(
            string characterId,
            string realmId,
            BuildStateUpdateRequest payload,
            Action<BuildStateSnapshot> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                onError?.Invoke(new ApiError(400, "Character id is required."));
                yield break;
            }

            if (_useMocks)
            {
                onSuccess?.Invoke(new BuildStateSnapshot
                {
                    id = string.Empty,
                    characterId = characterId,
                    realmId = realmId,
                    plots = payload?.plots ?? Array.Empty<BuildPlotDefinition>(),
                    constructions = payload?.constructions
                        ?? Array.Empty<global::Building.ConstructionInstance.SerializableConstructionState>(),
                    updatedAt = DateTime.UtcNow.ToString("O")
                });
                yield break;
            }

            var query = !string.IsNullOrWhiteSpace(realmId)
                ? $"?realmId={UnityWebRequest.EscapeURL(realmId)}"
                : string.Empty;
            var url = $"{_baseUrl}/characters/{characterId}/build-state{query}";
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload ?? new BuildStateUpdateRequest())))
                {
                    contentType = "application/json"
                },
                downloadHandler = new DownloadHandlerBuffer()
            };
            AttachAuthHeader(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var snapshot = JsonUtility.FromJson<BuildStateSnapshot>(request.downloadHandler.text);
                if (snapshot == null)
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Build state response was empty."));
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
}
