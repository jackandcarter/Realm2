using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace Client.Tests
{
    public class BackendIntegrationTests
    {
        private const string DefaultPassword = "Password123!";

        private static string BackendBaseUrl
        {
            get
            {
                var overrideUrl = Environment.GetEnvironmentVariable("REALM_BACKEND_URL");
                if (!string.IsNullOrWhiteSpace(overrideUrl))
                {
                    return overrideUrl.TrimEnd('/');
                }

                return "http://localhost:3000";
            }
        }

        private AuthService _authService;
        private RealmService _realmService;
        private CharacterService _characterService;

        [SetUp]
        public void SetUp()
        {
            SessionManager.Clear();
            var baseUrl = BackendBaseUrl;
            _authService = new AuthService(baseUrl, false);
            _realmService = new RealmService(baseUrl, false);
            _characterService = new CharacterService(baseUrl, false);
        }

        [TearDown]
        public void TearDown()
        {
            SessionManager.Clear();
        }

        [UnityTest]
        public IEnumerator RegisterLoginAndListRealms()
        {
            var email = UniqueEmail();
            yield return RegisterUser(email, DefaultPassword);

            yield return LoginUser(email, DefaultPassword);

            IReadOnlyList<RealmInfo> realms = null;
            ApiError realmError = null;
            yield return _realmService.GetRealms(list => realms = list, error => realmError = error);

            if (realmError != null)
            {
                Assert.Fail($"Realm listing failed: {realmError}");
            }

            Assert.IsNotNull(realms);
            Assert.IsTrue(realms.Count > 0, "Expected at least one realm from the backend.");
            foreach (var realm in realms)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(realm.id));
                Assert.IsFalse(string.IsNullOrWhiteSpace(realm.name));
            }
        }

        [UnityTest]
        public IEnumerator CreateCharacterUpdatesMembership()
        {
            var email = UniqueEmail();
            yield return RegisterUser(email, DefaultPassword);
            yield return LoginUser(email, DefaultPassword);

            IReadOnlyList<RealmInfo> realms = null;
            ApiError realmsError = null;
            yield return _realmService.GetRealms(list => realms = list, error => realmsError = error);
            if (realmsError != null)
            {
                Assert.Fail($"Realm listing failed: {realmsError}");
            }

            Assert.IsNotNull(realms);
            Assert.IsTrue(realms.Count > 0, "Expected at least one seeded realm.");

            var realm = realms[0];
            Assert.IsFalse(realm.isMember, "Fresh account should not be a member yet.");

            CharacterInfo created = null;
            ApiError createError = null;
            var characterName = $"UnityHero-{Guid.NewGuid():N}";
            yield return _characterService.CreateCharacter(realm.id, characterName, string.Empty, c => created = c, e => createError = e);

            if (createError != null)
            {
                Assert.Fail($"Character creation failed: {createError}");
            }

            Assert.IsNotNull(created);
            Assert.AreEqual(realm.id, created.realmId);
            Assert.AreEqual(characterName, created.name);

            IReadOnlyList<RealmInfo> updatedRealms = null;
            ApiError updatedError = null;
            yield return _realmService.GetRealms(list => updatedRealms = list, error => updatedError = error);
            if (updatedError != null)
            {
                Assert.Fail($"Realm refresh failed: {updatedError}");
            }

            RealmInfo updatedRealm = null;
            foreach (var info in updatedRealms)
            {
                if (info.id == realm.id)
                {
                    updatedRealm = info;
                    break;
                }
            }

            Assert.IsNotNull(updatedRealm, "Updated realm list did not contain the target realm.");
            Assert.IsTrue(updatedRealm.isMember, "Creating a character should mark the user as a member.");
            Assert.AreEqual("player", updatedRealm.membershipRole, "New characters should grant player membership.");

            RealmCharactersResponse roster = null;
            ApiError rosterError = null;
            yield return _characterService.GetCharacters(realm.id, result => roster = result, error => rosterError = error);

            if (rosterError != null)
            {
                Assert.Fail($"Fetching realm roster failed: {rosterError}");
            }

            Assert.IsNotNull(roster);
            Assert.IsNotNull(roster.membership);
            Assert.AreEqual("player", roster.membership.role);

            var foundCharacter = false;
            foreach (var character in roster.characters)
            {
                if (character.id == created.id)
                {
                    foundCharacter = true;
                    break;
                }
            }

            Assert.IsTrue(foundCharacter, "Newly created character should be returned by roster endpoint.");
        }

        private static string UniqueEmail()
        {
            return $"unity-{Guid.NewGuid():N}@example.com";
        }

        private IEnumerator RegisterUser(string email, string password)
        {
            using var request = new UnityWebRequest($"{BackendBaseUrl}/auth/register", UnityWebRequest.kHttpVerbPOST);
            var payload = JsonUtility.ToJson(new RegisterRequest
            {
                email = email,
                username = email.Split('@')[0],
                password = password
            });

            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Assert.Inconclusive($"Backend not reachable at {BackendBaseUrl}. Start the Express server before running integration tests.");
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Assert.Fail($"Registration failed with status {request.responseCode}: {request.downloadHandler.text}");
            }

            var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
            Assert.IsNotNull(response?.tokens, "Register response must contain tokens.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(response.tokens.accessToken), "Register response missing access token.");
        }

        private IEnumerator LoginUser(string email, string password)
        {
            AuthResponse loginResponse = null;
            ApiError loginError = null;

            yield return _authService.Login(email, password, response => loginResponse = response, error => loginError = error);

            if (loginError != null)
            {
                Assert.Fail($"Login failed: {loginError}");
            }

            Assert.IsNotNull(loginResponse);
            Assert.IsNotNull(loginResponse.tokens);
            Assert.IsFalse(string.IsNullOrWhiteSpace(SessionManager.AuthToken), "SessionManager should contain access token after login.");
        }

        [Serializable]
        private class RegisterRequest
        {
            public string email;
            public string username;
            public string password;
        }
    }
}
