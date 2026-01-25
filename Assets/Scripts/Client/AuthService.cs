using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Client
{
    [Serializable]
    public class AuthTokens
    {
        public string accessToken;
        public string refreshToken;
    }

    [Serializable]
    public class AuthenticatedUser
    {
        public string id;
        public string email;
        public string username;
        public string createdAt;
    }

    [Serializable]
    public class AuthResponse
    {
        public AuthenticatedUser user;
        public AuthTokens tokens;
    }

    [Serializable]
    public class LoginRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class RegisterRequest
    {
        public string email;
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

        public IEnumerator Login(string email, string password, Action<AuthResponse> onSuccess, Action<ApiError> onError)
        {
            if (_useMock)
            {
                yield return RunMockLogin(email, password, onSuccess, onError);
                yield break;
            }

            var payload = JsonUtility.ToJson(new LoginRequest
            {
                email = email,
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
                if (response == null || response.tokens == null || string.IsNullOrWhiteSpace(response.tokens.accessToken))
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Login succeeded but no token was returned."));
                    yield break;
                }

                SessionManager.SetTokens(response.tokens.accessToken, response.tokens.refreshToken);
                onSuccess?.Invoke(response);
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        public IEnumerator Register(string email, string username, string password, Action<AuthResponse> onSuccess, Action<ApiError> onError)
        {
            if (_useMock)
            {
                yield return RunMockRegister(email, username, password, onSuccess, onError);
                yield break;
            }

            var payload = JsonUtility.ToJson(new RegisterRequest
            {
                email = email,
                username = username,
                password = password
            });

            using var request = new UnityWebRequest($"{_baseUrl}/auth/register", UnityWebRequest.kHttpVerbPOST);
            var bodyRaw = Encoding.UTF8.GetBytes(payload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success || request.responseCode == 201)
            {
                var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                if (response == null || response.tokens == null || string.IsNullOrWhiteSpace(response.tokens.accessToken))
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Account created but no token was returned."));
                    yield break;
                }

                SessionManager.SetTokens(response.tokens.accessToken, response.tokens.refreshToken);
                onSuccess?.Invoke(response);
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        private IEnumerator RunMockLogin(string email, string password, Action<AuthResponse> onSuccess, Action<ApiError> onError)
        {
            yield return null;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                onError?.Invoke(new ApiError(400, "Email and password are required."));
                yield break;
            }

            var response = new AuthResponse
            {
                tokens = new AuthTokens
                {
                    accessToken = $"mock-token-{Guid.NewGuid():N}",
                    refreshToken = $"mock-refresh-{Guid.NewGuid():N}"
                },
                user = new AuthenticatedUser
                {
                    id = Guid.NewGuid().ToString("N"),
                    email = email,
                    username = email.Split('@')[0],
                    createdAt = DateTime.UtcNow.ToString("O")
                }
            };

            SessionManager.SetTokens(response.tokens.accessToken, response.tokens.refreshToken);
            onSuccess?.Invoke(response);
        }

        private IEnumerator RunMockRegister(string email, string username, string password, Action<AuthResponse> onSuccess, Action<ApiError> onError)
        {
            yield return null;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                onError?.Invoke(new ApiError(400, "Email, username, and password are required."));
                yield break;
            }

            var response = new AuthResponse
            {
                tokens = new AuthTokens
                {
                    accessToken = $"mock-token-{Guid.NewGuid():N}",
                    refreshToken = $"mock-refresh-{Guid.NewGuid():N}"
                },
                user = new AuthenticatedUser
                {
                    id = Guid.NewGuid().ToString("N"),
                    email = email,
                    username = username,
                    createdAt = DateTime.UtcNow.ToString("O")
                }
            };

            SessionManager.SetTokens(response.tokens.accessToken, response.tokens.refreshToken);
            onSuccess?.Invoke(response);
        }
    }
}
