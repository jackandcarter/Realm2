using Client.CharacterCreation;
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
                    Debug.LogWarning("Cannot unlock Builder class because no character is currently selected.");
                }

                return;
            }

            StartCoroutine(ClassUnlockRepository.UnlockClassAsync(
                characterId,
                ClassUnlockUtility.BuilderClassId,
                success =>
                {
                    if (!logUnlockEvents)
                    {
                        return;
                    }

                    if (success)
                    {
                        Debug.Log($"Builder class unlocked for character {characterId}.");
                    }
                    else
                    {
                        Debug.Log($"Builder class was already unlocked for character {characterId}.");
                    }
                },
                error =>
                {
                    if (logUnlockEvents)
                    {
                        Debug.LogWarning($"Failed to unlock Builder class for {characterId}: {error.Message}");
                    }
                }));
        }
    }
}
