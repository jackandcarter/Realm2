using System;
using System.Collections;
using System.Text;
using Client;
using Client.Terrain;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.Building
{
    public class BuildZoneApiClient
    {
        private readonly string _baseUrl;
        private readonly bool _useMocks;

        public BuildZoneApiClient(string baseUrl, bool useMocks)
        {
            _baseUrl = baseUrl?.TrimEnd('/');
            _useMocks = useMocks;
        }

        public IEnumerator ReplaceZones(
            string realmId,
            BuildZoneUpsertRequest payload,
            Action<BuildZoneUpsertResponse> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(realmId))
            {
                onError?.Invoke(new ApiError(400, "Realm id is required."));
                yield break;
            }

            if (_useMocks)
            {
                onSuccess?.Invoke(new BuildZoneUpsertResponse());
                yield break;
            }

            var url = $"{_baseUrl}/realms/{realmId}/build-zones";
            var json = JsonUtility.ToJson(payload ?? new BuildZoneUpsertRequest());
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
                var response = JsonUtility.FromJson<BuildZoneUpsertResponse>(request.downloadHandler.text);
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
    public class BuildZoneDefinition
    {
        public string zoneId;
        public string label;
        public SerializableBounds bounds;
    }

    [Serializable]
    public class BuildZoneUpsertRequest
    {
        public BuildZoneDefinition[] zones;
    }

    [Serializable]
    public class BuildZoneRecord
    {
        public string id;
        public string realmId;
        public string label;
        public float centerX;
        public float centerY;
        public float centerZ;
        public float sizeX;
        public float sizeY;
        public float sizeZ;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class BuildZoneUpsertResponse
    {
        public BuildZoneRecord[] zones;
    }
}
