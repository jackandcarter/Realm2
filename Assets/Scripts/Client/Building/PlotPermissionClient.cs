using System;
using System.Collections;
using Client;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.Building
{
    public class PlotPermissionClient
    {
        private readonly string _baseUrl;
        private readonly bool _useMocks;

        public PlotPermissionClient(string baseUrl, bool useMocks)
        {
            _baseUrl = baseUrl?.TrimEnd('/');
            _useMocks = useMocks;
        }

        public IEnumerator GetPermissions(
            string realmId,
            string plotId,
            Action<PlotPermissionSnapshot> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(realmId) || string.IsNullOrWhiteSpace(plotId))
            {
                onError?.Invoke(new ApiError(400, "Realm id and plot id are required."));
                yield break;
            }

            if (_useMocks)
            {
                onSuccess?.Invoke(new PlotPermissionSnapshot
                {
                    plotId = plotId,
                    ownerUserId = null,
                    permissions = Array.Empty<PlotPermissionEntry>()
                });
                yield break;
            }

            var url = $"{_baseUrl}/realms/{UnityWebRequest.EscapeURL(realmId)}/plots/{UnityWebRequest.EscapeURL(plotId)}/permissions";
            using var request = UnityWebRequest.Get(url);
            AttachAuthHeader(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var snapshot = JsonUtility.FromJson<PlotPermissionSnapshot>(request.downloadHandler.text);
                if (snapshot == null)
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Plot permission response was empty."));
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

        public IEnumerator ReplacePermissions(
            string realmId,
            string plotId,
            PlotPermissionEntry[] permissions,
            Action<PlotPermissionSnapshot> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(realmId) || string.IsNullOrWhiteSpace(plotId))
            {
                onError?.Invoke(new ApiError(400, "Realm id and plot id are required."));
                yield break;
            }

            if (_useMocks)
            {
                onSuccess?.Invoke(new PlotPermissionSnapshot
                {
                    plotId = plotId,
                    ownerUserId = null,
                    permissions = permissions ?? Array.Empty<PlotPermissionEntry>()
                });
                yield break;
            }

            var url = $"{_baseUrl}/realms/{UnityWebRequest.EscapeURL(realmId)}/plots/{UnityWebRequest.EscapeURL(plotId)}/permissions";
            var payload = new PlotPermissionUpdateRequest { permissions = permissions ?? Array.Empty<PlotPermissionEntry>() };
            var json = JsonUtility.ToJson(payload);
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json))
                {
                    contentType = "application/json"
                },
                downloadHandler = new DownloadHandlerBuffer()
            };
            AttachAuthHeader(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var snapshot = JsonUtility.FromJson<PlotPermissionSnapshot>(request.downloadHandler.text);
                if (snapshot == null)
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Plot permission response was empty."));
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

        [Serializable]
        private class PlotPermissionUpdateRequest
        {
            public PlotPermissionEntry[] permissions;
        }
    }
}
