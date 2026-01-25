using System;
using System.Collections;
using Client.Progression;
using UnityEngine;

namespace Client.Quests
{
    public class QuestNpcInteractor : MonoBehaviour
    {
        [SerializeField]
        private string questId;

        [SerializeField]
        private bool logEvents = true;

        public string QuestId => questId;

        public void OfferQuest()
        {
            var characterId = SessionManager.SelectedCharacterId;
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(questId))
            {
                return;
            }

            StartCoroutine(StartQuestRoutine(characterId));
        }

        public void CompleteQuest()
        {
            var characterId = SessionManager.SelectedCharacterId;
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(questId))
            {
                return;
            }

            StartCoroutine(QuestRepository.CompleteQuestAsync(
                characterId,
                questId,
                "{}",
                response =>
                {
                    if (logEvents)
                    {
                        Debug.Log($"Quest completion submitted for {questId} (request {response?.requestId ?? "unknown"}).", this);
                    }
                },
                error =>
                {
                    if (logEvents)
                    {
                        Debug.LogWarning($"Quest completion failed for {questId}: {error.Message}", this);
                    }
                }));
        }

        private IEnumerator StartQuestRoutine(string characterId)
        {
            var existing = QuestRepository.GetQuestStates(characterId);
            foreach (var quest in existing)
            {
                if (quest == null || !string.Equals(quest.questId, questId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (logEvents)
                {
                    Debug.Log($"Quest {questId} already tracked with status {quest.status}.", this);
                }
                yield break;
            }

            var updated = new CharacterQuestStateEntry[existing.Length + 1];
            Array.Copy(existing, updated, existing.Length);
            updated[existing.Length] = new CharacterQuestStateEntry
            {
                questId = questId,
                status = "active",
                progressJson = "{}",
                updatedAt = DateTime.UtcNow.ToString("O")
            };

            yield return QuestRepository.UpdateQuestsAsync(
                characterId,
                updated,
                success =>
                {
                    if (logEvents && success)
                    {
                        Debug.Log($"Quest {questId} activated for character {characterId}.", this);
                    }
                },
                error =>
                {
                    if (logEvents)
                    {
                        Debug.LogWarning($"Failed to activate quest {questId}: {error.Message}", this);
                    }
                });
        }
    }
}
