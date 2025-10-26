using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Client.CharacterCreation;
using UnityEngine;
using UnityEngine.Networking;

namespace Client
{
    [Serializable]
    public class CharacterAppearanceInfo
    {
        public float height;
        public float build;
    }

    [Serializable]
    public class CharacterInfo
    {
        public string id;
        public string userId;
        public string realmId;
        public string name;
        public string bio;
        public string raceId;
        public CharacterAppearanceInfo appearance = new CharacterAppearanceInfo();
        public string createdAt;
    }

    [Serializable]
    internal class CreateCharacterRequest
    {
        public string realmId;
        public string name;
        public string bio;
        public string raceId;
        public CharacterAppearanceRequest appearance;
    }

    [Serializable]
    internal class CharacterAppearanceRequest
    {
        public float height;
        public float build;
    }

    [Serializable]
    internal class CharacterResponse
    {
        public CharacterInfo character;
    }

    [Serializable]
    public class RealmDetails
    {
        public string id;
        public string name;
        public string narrative;
        public string createdAt;
    }

    [Serializable]
    public class RealmMembershipInfo
    {
        public string id;
        public string realmId;
        public string userId;
        public string role;
        public string createdAt;
    }

    [Serializable]
    public class RealmCharactersResponse
    {
        public RealmDetails realm;
        public RealmMembershipInfo membership;
        public CharacterInfo[] characters;
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

        public IEnumerator GetCharacters(string realmId, Action<RealmCharactersResponse> onSuccess, Action<ApiError> onError)
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
                var response = JsonUtility.FromJson<RealmCharactersResponse>(request.downloadHandler.text);
                if (response == null)
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Character roster response was empty."));
                    yield break;
                }

                response.characters ??= Array.Empty<CharacterInfo>();
                onSuccess?.Invoke(response);
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(request));
            }
        }

        public IEnumerator CreateCharacter(string realmId, string name, string bio, Action<CharacterInfo> onSuccess, Action<ApiError> onError, CharacterCreationSelection? selection = null)
        {
            if (_useMock)
            {
                yield return RunMockCreateCharacter(realmId, name, bio, onSuccess, onError, selection);
                yield break;
            }

            var payload = JsonUtility.ToJson(new CreateCharacterRequest
            {
                realmId = realmId,
                name = name,
                bio = bio,
                raceId = selection.HasValue && selection.Value.Race != null ? selection.Value.Race.Id : null,
                appearance = BuildAppearancePayload(selection)
            });

            using var request = new UnityWebRequest($"{_baseUrl}/characters", UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            AttachAuthHeader(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<CharacterResponse>(request.downloadHandler.text);
                if (response?.character == null)
                {
                    onError?.Invoke(new ApiError(request.responseCode, "Character creation response was empty."));
                    yield break;
                }

                onSuccess?.Invoke(response.character);
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

        private IEnumerator RunMockCharacters(string realmId, Action<RealmCharactersResponse> onSuccess)
        {
            yield return null;

            onSuccess?.Invoke(new RealmCharactersResponse
            {
                realm = new RealmDetails
                {
                    id = realmId,
                    name = "Mock Realm",
                    narrative = "A conjured world used for UI flows.",
                    createdAt = DateTime.UtcNow.ToString("O")
                },
                membership = new RealmMembershipInfo
                {
                    id = Guid.NewGuid().ToString("N"),
                    realmId = realmId,
                    userId = Guid.NewGuid().ToString("N"),
                    role = "player",
                    createdAt = DateTime.UtcNow.ToString("O")
                },
                characters = new[]
                {
                    new CharacterInfo
                    {
                        id = $"{realmId}-1",
                        realmId = realmId,
                        userId = Guid.NewGuid().ToString("N"),
                        name = "Aeloria",
                        bio = "An arcane adept forged in testing.",
                        raceId = "felarian",
                        appearance = new CharacterAppearanceInfo
                        {
                            height = 1.72f,
                            build = 0.48f
                        },
                        createdAt = DateTime.UtcNow.ToString("O")
                    },
                    new CharacterInfo
                    {
                        id = $"{realmId}-2",
                        realmId = realmId,
                        userId = Guid.NewGuid().ToString("N"),
                        name = "Bram",
                        bio = "Warrior of the mock clans.",
                        raceId = "human",
                        appearance = new CharacterAppearanceInfo
                        {
                            height = 1.85f,
                            build = 0.62f
                        },
                        createdAt = DateTime.UtcNow.ToString("O")
                    }
                }
            });
        }

        private static CharacterAppearanceRequest BuildAppearancePayload(CharacterCreationSelection? selection)
        {
            if (!selection.HasValue || selection.Value.Race == null)
            {
                return null;
            }

            return new CharacterAppearanceRequest
            {
                height = selection.Value.Height,
                build = selection.Value.Build
            };
        }

        private IEnumerator RunMockCreateCharacter(string realmId, string name, string bio, Action<CharacterInfo> onSuccess, Action<ApiError> onError, CharacterCreationSelection? selection)
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
                userId = Guid.NewGuid().ToString("N"),
                name = name,
                realmId = realmId,
                bio = string.IsNullOrWhiteSpace(bio) ? "" : bio,
                raceId = selection.HasValue && selection.Value.Race != null ? selection.Value.Race.Id : "human",
                appearance = selection.HasValue
                    ? new CharacterAppearanceInfo
                    {
                        height = selection.Value.Height,
                        build = selection.Value.Build
                    }
                    : new CharacterAppearanceInfo
                    {
                        height = 1.75f,
                        build = 0.5f
                    },
                createdAt = DateTime.UtcNow.ToString("O")
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
