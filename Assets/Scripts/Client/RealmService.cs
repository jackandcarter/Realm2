using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Client
{
    [Serializable]
    public class RealmInfo
    {
        public string id;
        public string name;
        public int population;
    }

    [Serializable]
    internal class RealmListResponse
    {
        public RealmInfo[] realms;
    }

    public class RealmService
    {
        private readonly string _baseUrl;
        private readonly bool _useMock;

        public RealmService(string baseUrl, bool useMock)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _useMock = useMock;
        }

        public IEnumerator GetRealms(Action<IReadOnlyList<RealmInfo>> onSuccess, Action<ApiError> onError)
        {
            if (_useMock)
            {
                yield return RunMockRealms(onSuccess);
                yield break;
            }

            using var request = UnityWebRequest.Get($"{_baseUrl}/realms");
            AttachAuthHeader(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<RealmListResponse>(request.downloadHandler.text);
                var realms = response?.realms ?? Array.Empty<RealmInfo>();
                onSuccess?.Invoke(realms);
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

        private IEnumerator RunMockRealms(Action<IReadOnlyList<RealmInfo>> onSuccess)
        {
            yield return null;

            onSuccess?.Invoke(new[]
            {
                new RealmInfo { id = "1", name = "Valoria", population = 1280 },
                new RealmInfo { id = "2", name = "Eldamar", population = 890 },
                new RealmInfo { id = "3", name = "Karthos", population = 432 }
            });
        }
    }
}
