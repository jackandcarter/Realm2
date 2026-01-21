using System;
using System.Collections;
using Client;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.Building
{
    public class BuildZoneValidationClient
    {
        private readonly string _baseUrl;
        private readonly bool _useMocks;

        public BuildZoneValidationClient(string baseUrl, bool useMocks)
        {
            _baseUrl = baseUrl?.TrimEnd('/');
            _useMocks = useMocks;
        }

        public IEnumerator ValidateBounds(
            string realmId,
            BuildZoneValidationRequest payload,
            Action<BuildZoneValidationResponse> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(realmId))
            {
                onError?.Invoke(new ApiError(400, "Realm id is required."));
                yield break;
            }

            if (_useMocks)
            {
                onSuccess?.Invoke(new BuildZoneValidationResponse { isValid = true });
                yield break;
            }

            var url = $"{_baseUrl}/realms/{realmId}/build-zones/validate";
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload ?? new BuildZoneValidationRequest())))
                {
                    contentType = "application/json"
                },
                downloadHandler = new DownloadHandlerBuffer()
            };
            AttachAuthHeader(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<BuildZoneValidationResponse>(request.downloadHandler.text);
                if (response == null)
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Build zone validation response was empty."));
                }
                else
                {
                    onSuccess?.Invoke(response);
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
    public class BuildZoneValidationRequest
    {
        public BuildZoneBounds bounds;
    }

    [Serializable]
    public class BuildZoneBounds
    {
        public Vector3 center;
        public Vector3 size;
    }

    [Serializable]
    public class BuildZoneValidationResponse
    {
        public bool isValid;
        public string zoneId;
        public string failureReason;
    }
}
