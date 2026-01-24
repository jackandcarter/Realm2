using System;
using System.Collections;
using System.Text;
using Client;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.Terrain
{
    public class TerrainRegionApiClient
    {
        private readonly string _baseUrl;
        private readonly bool _useMocks;

        public TerrainRegionApiClient(string baseUrl, bool useMocks)
        {
            _baseUrl = baseUrl?.TrimEnd('/');
            _useMocks = useMocks;
        }

        public IEnumerator UpsertRegion(
            string realmId,
            TerrainRegionRequest payload,
            Action<TerrainRegionResponse> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(realmId))
            {
                onError?.Invoke(new ApiError(400, "Realm id is required."));
                yield break;
            }

            if (_useMocks)
            {
                onSuccess?.Invoke(new TerrainRegionResponse());
                yield break;
            }

            var url = $"{_baseUrl}/realms/{realmId}/terrain/regions/{payload?.regionId}";
            var json = JsonUtility.ToJson(payload ?? new TerrainRegionRequest());
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            AttachAuthHeader(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<TerrainRegionResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(response);
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

    [Serializable]
    public class TerrainRegionRequest
    {
        public string regionId;
        public string name;
        public SerializableBounds bounds;
        public int terrainCount;
        public TerrainRegionPayload payload;
    }

    [Serializable]
    public class TerrainRegionPayload
    {
        public string zoneId;
        public SerializableRect mapWorldBounds;
        public Vector2 chunkOriginOffset;
        public float chunkSizeOverride;
        public float chunkSize;
        public bool useTerrainBounds;
        public string miniMapTextureName;
        public string worldMapTextureName;
    }

    [Serializable]
    public class TerrainRegionRecord
    {
        public string id;
        public string realmId;
        public string name;
        public string boundsJson;
        public int terrainCount;
        public string payloadJson;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class TerrainRegionResponse
    {
        public TerrainRegionRecord region;
    }
}
