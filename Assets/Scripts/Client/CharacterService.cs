using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Client
{
    [Serializable]
    public class CharacterInfo
    {
        public string id;
        public string name;
        public string realmId;
        public string className;
        public int level;
    }

    [Serializable]
    internal class CharacterListResponse
    {
        public CharacterInfo[] characters;
    }

    [Serializable]
    internal class CreateCharacterRequest
    {
        public string realmId;
        public string name;
        public string className;
    }

    [Serializable]
    internal class SelectCharacterRequest
    {
        public string characterId;
    }

    public class CharacterService
    {
        private readonly string _baseUrl;
        private readonly bool _useMock;

        public CharacterService(string baseUrl, bool useMock)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _useMock = useMock;
        }

        public IEnumerator GetCharacters(string realmId, Action<IReadOnlyList<CharacterInfo>> onSuccess, Action<ApiError> onError)
        {
            if (_useMock)
            {
                yield return RunMockCharacters(realmId, onSuccess);
                yield break;
            }

            using var request = UnityWebRequest.Get($"{_baseUrl}/realms/{realmId}/characters");
            AttachAuthHeader(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<CharacterListResponse>(request.downloadHandler.text);
                var characters = response?.characters ?? Array.Empty<CharacterInfo>();
                onSuccess?.Invoke(characters);
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        public IEnumerator CreateCharacter(string realmId, string name, string className, Action<CharacterInfo> onSuccess, Action<ApiError> onError)
        {
            if (_useMock)
            {
                yield return RunMockCreateCharacter(realmId, name, className, onSuccess, onError);
                yield break;
            }

            var payload = JsonUtility.ToJson(new CreateCharacterRequest
            {
                realmId = realmId,
                name = name,
                className = className
            });

            using var request = new UnityWebRequest($"{_baseUrl}/characters", UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            AttachAuthHeader(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var character = JsonUtility.FromJson<CharacterInfo>(request.downloadHandler.text);
                onSuccess?.Invoke(character);
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        public IEnumerator SelectCharacter(string characterId, Action onSuccess, Action<ApiError> onError)
        {
            if (_useMock)
            {
                yield return RunMockSelectCharacter(characterId, onSuccess, onError);
                yield break;
            }

            var payload = JsonUtility.ToJson(new SelectCharacterRequest { characterId = characterId });

            using var request = new UnityWebRequest($"{_baseUrl}/characters/select", UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            AttachAuthHeader(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke();
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

        private IEnumerator RunMockCharacters(string realmId, Action<IReadOnlyList<CharacterInfo>> onSuccess)
        {
            yield return null;

            onSuccess?.Invoke(new[]
            {
                new CharacterInfo { id = $"{realmId}-1", name = "Aeloria", realmId = realmId, className = "Mage", level = 5 },
                new CharacterInfo { id = $"{realmId}-2", name = "Bram", realmId = realmId, className = "Warrior", level = 8 }
            });
        }

        private IEnumerator RunMockCreateCharacter(string realmId, string name, string className, Action<CharacterInfo> onSuccess, Action<ApiError> onError)
        {
            yield return null;

            if (string.IsNullOrWhiteSpace(name))
            {
                onError?.Invoke(new ApiError(400, "Character name is required."));
                yield break;
            }

            var character = new CharacterInfo
            {
                id = Guid.NewGuid().ToString("N"),
                name = name,
                realmId = realmId,
                className = string.IsNullOrWhiteSpace(className) ? "Adventurer" : className,
                level = 1
            };

            onSuccess?.Invoke(character);
        }

        private IEnumerator RunMockSelectCharacter(string characterId, Action onSuccess, Action<ApiError> onError)
        {
            yield return null;

            if (string.IsNullOrWhiteSpace(characterId))
            {
                onError?.Invoke(new ApiError(400, "No character selected."));
                yield break;
            }

            onSuccess?.Invoke();
        }
    }
}
