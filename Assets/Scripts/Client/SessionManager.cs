namespace Client
{
    public static class SessionManager
    {
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
            SelectedRealmId = realmId;
        }

        public static void SetCharacter(string characterId)
        {
            SelectedCharacterId = characterId;
        }

        public static void Clear()
        {
            AuthToken = null;
            RefreshToken = null;
            SelectedRealmId = null;
            SelectedCharacterId = null;
        }
    }
}
