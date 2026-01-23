using System;
using Client.CharacterCreation;

namespace Client
{
    public enum CharacterClassStatus
    {
        Unassigned,
        Valid,
        Locked,
        Forbidden,
        Unavailable,
        Stale
    }

    public struct CharacterClassStatusInfo
    {
        public CharacterClassStatus Status;
        public string ClassDisplay;
        public string StatusLabel;
        public string Message;

        public bool CanPlay => Status == CharacterClassStatus.Valid;
        public bool RequiresAttention => Status != CharacterClassStatus.Valid;
    }

    public static class CharacterClassStatusUtility
    {
        public static CharacterClassStatusInfo Evaluate(CharacterInfo character)
        {
            var info = new CharacterClassStatusInfo
            {
                Status = CharacterClassStatus.Unassigned,
                ClassDisplay = "—",
                StatusLabel = string.Empty,
                Message = "Select a character to view their details."
            };

            if (character == null)
            {
                return info;
            }

            var characterName = string.IsNullOrWhiteSpace(character.name) ? "This character" : character.name.Trim();
            var normalizedClassId = character.classId != null ? character.classId.Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedClassId))
            {
                info.Status = CharacterClassStatus.Unassigned;
                info.ClassDisplay = "Unassigned";
                info.StatusLabel = "Unassigned";
                info.Message = $"{characterName} has not selected a class yet.";
                return info;
            }

            if (!ClassCatalog.TryGetClass(normalizedClassId, out var classDefinition) || classDefinition == null)
            {
                info.Status = CharacterClassStatus.Unavailable;
                info.ClassDisplay = normalizedClassId;
                info.StatusLabel = "Unavailable";
                info.Message = $"{characterName}'s class data is unavailable. Please choose a different character.";
                return info;
            }

            info.ClassDisplay = classDefinition.DisplayName;

            if (!ClassUnlockUtility.TryGetState(character.classStates, normalizedClassId, out var state) || state == null)
            {
                info.Status = CharacterClassStatus.Locked;
                info.StatusLabel = "Locked";
                info.Message = $"{characterName}'s class unlock status is not yet available from the server.";
                return info;
            }

            if (!state.Unlocked)
            {
                info.Status = CharacterClassStatus.Locked;
                info.StatusLabel = "Locked";
                info.Message = $"{info.ClassDisplay} is locked. Unlock status is controlled by the server.";
                return info;
            }

            info.Status = CharacterClassStatus.Valid;
            info.StatusLabel = "Unlocked";
            info.Message = $"{characterName} is ready to adventure as a {info.ClassDisplay}.";
            return info;
        }

    }
}
