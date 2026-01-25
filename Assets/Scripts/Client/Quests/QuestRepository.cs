using System;
using System.Collections;
using System.Collections.Generic;
using Client;
using Client.Progression;

namespace Client.Quests
{
    public static class QuestRepository
    {
        private static readonly Dictionary<string, CharacterQuestStateEntry[]> QuestStatesByCharacter =
            new(StringComparer.OrdinalIgnoreCase);
        private static CharacterProgressionClient _progressionClient;

        public static event Action<string, CharacterQuestStateEntry[]> QuestStatesChanged;

        public static void SetProgressionClient(CharacterProgressionClient client)
        {
            _progressionClient = client;
        }

        public static CharacterQuestStateEntry[] GetQuestStates(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)
                || !QuestStatesByCharacter.TryGetValue(characterId, out var states))
            {
                return Array.Empty<CharacterQuestStateEntry>();
            }

            return CloneStates(states);
        }

        public static IEnumerator SyncWithServer(
            string characterId,
            Action<CharacterProgressionEnvelope> onSuccess = null,
            Action<ApiError> onError = null)
        {
            if (string.IsNullOrWhiteSpace(characterId) || _progressionClient == null)
            {
                onSuccess?.Invoke(null);
                yield break;
            }

            CharacterProgressionEnvelope snapshot = null;
            ApiError error = null;

            yield return _progressionClient.GetProgression(
                characterId,
                payload => snapshot = payload,
                apiError => error = apiError);

            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            if (snapshot != null)
            {
                ApplySnapshot(characterId, snapshot);
            }

            onSuccess?.Invoke(snapshot);
        }

        public static IEnumerator UpdateQuestsAsync(
            string characterId,
            CharacterQuestStateEntry[] quests,
            Action<bool> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                onSuccess?.Invoke(false);
                yield break;
            }

            var sanitized = Sanitize(quests);

            if (_progressionClient == null)
            {
                onSuccess?.Invoke(false);
                yield break;
            }

            var expectedVersion = CharacterProgressionCache.TryGet(characterId, out var snapshot) && snapshot?.quests != null
                ? snapshot.quests.version
                : 0;

            CharacterProgressionEnvelope response = null;
            ApiError error = null;

            yield return _progressionClient.UpdateQuests(
                characterId,
                sanitized,
                expectedVersion,
                payload => response = payload,
                apiError => error = apiError);

            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            if (response != null)
            {
                ApplySnapshot(characterId, response);
            }

            onSuccess?.Invoke(true);
        }

        public static IEnumerator CompleteQuestAsync(
            string characterId,
            string questId,
            string progressJson,
            Action<ProgressionIntentResponse> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(questId))
            {
                onSuccess?.Invoke(null);
                yield break;
            }

            if (_progressionClient == null)
            {
                onSuccess?.Invoke(null);
                yield break;
            }

            ProgressionIntentResponse response = null;
            ApiError error = null;

            yield return _progressionClient.CompleteQuest(
                characterId,
                questId,
                progressJson,
                payload => response = payload,
                apiError => error = apiError);

            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            onSuccess?.Invoke(response);
        }

        public static void ApplySnapshot(string characterId, CharacterProgressionEnvelope snapshot)
        {
            if (string.IsNullOrWhiteSpace(characterId) || snapshot == null)
            {
                return;
            }

            CharacterProgressionCache.Store(characterId, snapshot);

            ApplyQuestState(characterId, snapshot.quests);
        }

        public static void ApplyQuestState(string characterId, CharacterQuestCollection collection)
        {
            if (string.IsNullOrWhiteSpace(characterId) || collection?.quests == null)
            {
                return;
            }

            QuestStatesByCharacter[characterId] = CloneStates(collection.quests);
            QuestStatesChanged?.Invoke(characterId, CloneStates(collection.quests));
        }

        private static CharacterQuestStateEntry[] CloneStates(CharacterQuestStateEntry[] quests)
        {
            if (quests == null || quests.Length == 0)
            {
                return Array.Empty<CharacterQuestStateEntry>();
            }

            var clone = new CharacterQuestStateEntry[quests.Length];
            for (var i = 0; i < quests.Length; i++)
            {
                var entry = quests[i];
                clone[i] = entry == null
                    ? null
                    : new CharacterQuestStateEntry
                    {
                        questId = entry.questId,
                        status = entry.status,
                        progressJson = entry.progressJson,
                        updatedAt = entry.updatedAt
                    };
            }

            return clone;
        }

        private static CharacterQuestStateEntry[] Sanitize(CharacterQuestStateEntry[] quests)
        {
            if (quests == null || quests.Length == 0)
            {
                return Array.Empty<CharacterQuestStateEntry>();
            }

            var sanitized = new List<CharacterQuestStateEntry>();
            foreach (var quest in quests)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.questId) || string.IsNullOrWhiteSpace(quest.status))
                {
                    continue;
                }

                sanitized.Add(new CharacterQuestStateEntry
                {
                    questId = quest.questId.Trim(),
                    status = quest.status.Trim(),
                    progressJson = string.IsNullOrWhiteSpace(quest.progressJson) ? "{}" : quest.progressJson.Trim(),
                    updatedAt = quest.updatedAt
                });
            }

            return sanitized.ToArray();
        }
    }
}
