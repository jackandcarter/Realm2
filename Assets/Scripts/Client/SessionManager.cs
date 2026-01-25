using System;

namespace Client
{
    public static class SessionManager
    {
        public static event Action<string> SelectedRealmChanged;
        public static event Action<string> SelectedCharacterChanged;
        public static event Action SessionCleared;

        public static string AuthToken { get; private set; }
        public static string RefreshToken { get; private set; }
        public static string SelectedRealmId { get; private set; }
        public static string SelectedCharacterId { get; private set; }

        public static void SetTokens(string authToken, string refreshToken)
        {
            AuthToken = authToken;
            RefreshToken = refreshToken;
        }

        public static void SetRealm(string realmId)
        {
            if (string.Equals(SelectedRealmId, realmId, StringComparison.Ordinal))
            {
                return;
            }

            SelectedRealmId = realmId;
            SelectedRealmChanged?.Invoke(realmId);
        }

        public static void SetCharacter(string characterId)
        {
            if (string.Equals(SelectedCharacterId, characterId, StringComparison.Ordinal))
            {
                return;
            }

            SelectedCharacterId = characterId;
            SelectedCharacterChanged?.Invoke(characterId);
        }

        public static void Clear()
        {
            var hadCharacter = !string.IsNullOrWhiteSpace(SelectedCharacterId);
            var hadRealm = !string.IsNullOrWhiteSpace(SelectedRealmId);

            AuthToken = null;
            RefreshToken = null;
            SelectedRealmId = null;
            SelectedCharacterId = null;

            if (hadRealm)
            {
                SelectedRealmChanged?.Invoke(null);
            }

            if (hadCharacter)
            {
                SelectedCharacterChanged?.Invoke(null);
            }

            SessionCleared?.Invoke();
        }
    }
}
