using System;
using System.Collections;
using System.Text;
using Client;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.Terrain
{
    public class TerrainImportApiClient
    {
        private readonly string _baseUrl;
        private readonly bool _useMocks;

        public TerrainImportApiClient(string baseUrl, bool useMocks)
        {
            _baseUrl = baseUrl?.TrimEnd('/');
            _useMocks = useMocks;
        }

        public IEnumerator ImportTerrain(
            string realmId,
            TerrainImportRequest payload,
            Action<TerrainImportResponse> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(realmId))
            {
                onError?.Invoke(new ApiError(400, "Realm id is required."));
                yield break;
            }

            if (_useMocks)
            {
                onSuccess?.Invoke(new TerrainImportResponse { changes = Array.Empty<RealmChunkChange>() });
                yield break;
            }

            var url = $"{_baseUrl}/realms/{realmId}/terrain/import";
            var json = JsonUtility.ToJson(payload ?? new TerrainImportRequest());
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            AttachAuthHeader(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success || request.responseCode == 201)
            {
                var response = JsonUtility.FromJson<TerrainImportResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(response ?? new TerrainImportResponse());
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
    }
}
