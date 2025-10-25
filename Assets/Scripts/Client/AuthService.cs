using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Client
{
    [Serializable]
    public class AuthResponse
    {
        public string token;
        public string refreshToken;
    }

    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    public class AuthService
    {
        private readonly string _baseUrl;
        private readonly bool _useMock;

        public AuthService(string baseUrl, bool useMock)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _useMock = useMock;
        }

        public IEnumerator Login(string username, string password, Action<AuthResponse> onSuccess, Action<ApiError> onError)
        {
            if (_useMock)
            {
                yield return RunMockLogin(username, password, onSuccess, onError);
                yield break;
            }

            var payload = JsonUtility.ToJson(new LoginRequest
            {
                username = username,
                password = password
            });

            using var request = new UnityWebRequest($"{_baseUrl}/auth/login", UnityWebRequest.kHttpVerbPOST);
            var bodyRaw = Encoding.UTF8.GetBytes(payload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                if (response == null || string.IsNullOrWhiteSpace(response.token))
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Login succeeded but no token was returned."));
                    yield break;
                }

                SessionManager.SetTokens(response.token, response.refreshToken);
                onSuccess?.Invoke(response);
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        private IEnumerator RunMockLogin(string username, string password, Action<AuthResponse> onSuccess, Action<ApiError> onError)
        {
            yield return null;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                onError?.Invoke(new ApiError(400, "Username and password are required."));
                yield break;
            }

            var response = new AuthResponse
            {
                token = $"mock-token-{Guid.NewGuid():N}",
                refreshToken = $"mock-refresh-{Guid.NewGuid():N}"
            };

            SessionManager.SetTokens(response.token, response.refreshToken);
            onSuccess?.Invoke(response);
        }
    }
}
