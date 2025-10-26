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

            if (ClassUnlockRepository.UnlockClass(characterId, ClassUnlockUtility.BuilderClassId))
            {
                if (logUnlockEvents)
                {
                    Debug.Log($"Builder class unlocked for character {characterId}.");
                }
            }
            else if (logUnlockEvents)
            {
                Debug.Log($"Builder class was already unlocked for character {characterId}.");
            }
        }
    }
}
