using UnityEngine;

namespace Client.Quests
{
    public class BuilderQuestCompletionHandler : MonoBehaviour
    {
        [SerializeField]
        private bool logUnlockEvents = true;

        public void CompleteBuilderQuest()
        {
            var characterId = SessionManager.SelectedCharacterId;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                if (logUnlockEvents)
                {
                    Debug.LogWarning("Cannot complete Builder quest because no character is currently selected.");
                }

                return;
            }

            StartCoroutine(QuestRepository.CompleteQuestAsync(
                characterId,
                "quest-builder-arkitect",
                "{}",
                response =>
                {
                    if (!logUnlockEvents)
                    {
                        return;
                    }

                    var requestId = response != null ? response.requestId : "unknown";
                    Debug.Log($"Builder quest completion requested for character {characterId}. Request: {requestId}.");
                },
                error =>
                {
                    if (logUnlockEvents)
                    {
                        Debug.LogWarning($"Failed to complete Builder quest for {characterId}: {error.Message}");
                    }
                }));
        }
    }
}
