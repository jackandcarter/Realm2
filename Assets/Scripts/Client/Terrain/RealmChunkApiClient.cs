using System;
using System.Collections;
using System.Text;
using Client;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.Terrain
{
    public class RealmChunkApiClient
    {
        private readonly string _baseUrl;
        private readonly bool _useMock;

        public RealmChunkApiClient(string baseUrl, bool useMock)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _useMock = useMock;
        }

        public IEnumerator GetSnapshot(
            string realmId,
            string sinceTimestamp,
            Action<RealmChunkSnapshotResponse> onSuccess,
            Action<ApiError> onError)
        {
            if (_useMock)
            {
                yield return RunMockSnapshot(realmId, onSuccess);
                yield break;
            }

            var url = $"{_baseUrl}/realms/{realmId}/chunks";
            if (!string.IsNullOrEmpty(sinceTimestamp))
            {
                url += $"?since={UnityWebRequest.EscapeURL(sinceTimestamp)}";
            }

            using var request = UnityWebRequest.Get(url);
            AttachAuthHeader(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<RealmChunkSnapshotResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        public IEnumerator GetChanges(
            string realmId,
            string sinceTimestamp,
            int? limit,
            Action<RealmChunkChangeFeedResponse> onSuccess,
            Action<ApiError> onError)
        {
            if (_useMock)
            {
                yield return RunMockChanges(realmId, onSuccess);
                yield break;
            }

            var query = string.Empty;
            if (!string.IsNullOrEmpty(sinceTimestamp))
            {
                query = $"?since={UnityWebRequest.EscapeURL(sinceTimestamp)}";
            }

            if (limit.HasValue && limit.Value > 0)
            {
                var separator = string.IsNullOrEmpty(query) ? '?' : '&';
                query += $"{separator}limit={limit.Value}";
            }

            var url = $"{_baseUrl}/realms/{realmId}/chunks/changes{query}";

            using var request = UnityWebRequest.Get(url);
            AttachAuthHeader(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<RealmChunkChangeFeedResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        public IEnumerator RecordChange(
            string realmId,
            string chunkId,
            RealmChunkChangeRequest requestBody,
            Action<RealmChunkChange> onSuccess,
            Action<ApiError> onError)
        {
            if (_useMock)
            {
                yield return RunMockChange(onSuccess);
                yield break;
            }

            var url = $"{_baseUrl}/realms/{realmId}/chunks/{chunkId}/changes";
            var json = JsonUtility.ToJson(requestBody ?? new RealmChunkChangeRequest());
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
                var wrapper = JsonUtility.FromJson<ChangeResponseWrapper>(request.downloadHandler.text);
                onSuccess?.Invoke(wrapper?.change);
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

        private IEnumerator RunMockSnapshot(string realmId, Action<RealmChunkSnapshotResponse> onSuccess)
        {
            yield return null;
            onSuccess?.Invoke(new RealmChunkSnapshotResponse
            {
                realmId = realmId,
                serverTimestamp = DateTime.UtcNow.ToString("O"),
                chunks = Array.Empty<RealmChunkSnapshot>()
            });
        }

        private IEnumerator RunMockChanges(string realmId, Action<RealmChunkChangeFeedResponse> onSuccess)
        {
            yield return null;
            onSuccess?.Invoke(new RealmChunkChangeFeedResponse
            {
                realmId = realmId,
                serverTimestamp = DateTime.UtcNow.ToString("O"),
                changes = Array.Empty<RealmChunkChange>()
            });
        }

        private IEnumerator RunMockChange(Action<RealmChunkChange> onSuccess)
        {
            yield return null;
            onSuccess?.Invoke(null);
        }

        [Serializable]
        private class ChangeResponseWrapper
        {
            public RealmChunkChange change;
        }
    }
}
