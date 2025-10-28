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

            if (string.IsNullOrWhiteSpace(character.raceId))
            {
                info.Status = CharacterClassStatus.Unavailable;
                info.StatusLabel = "Unknown race";
                info.Message = $"{characterName}'s race data is missing; class eligibility cannot be verified.";
                return info;
            }

            if (!ClassRulesCatalog.IsClassAllowedForRace(normalizedClassId, character.raceId))
            {
                var raceName = ResolveRaceName(character.raceId);
                info.Status = CharacterClassStatus.Forbidden;
                info.StatusLabel = $"Forbidden for {raceName}";
                info.Message = $"{info.ClassDisplay} is forbidden for {raceName}.";
                return info;
            }

            if (!ClassRulesCatalog.TryGetRule(normalizedClassId, out var rule) || rule == null)
            {
                info.Status = CharacterClassStatus.Unavailable;
                info.StatusLabel = "Unavailable";
                info.Message = $"{info.ClassDisplay} is missing class rule data.";
                return info;
            }

            if (!ClassUnlockUtility.TryGetState(character.classStates, normalizedClassId, out var state) || state == null)
            {
                return BuildLockedStatusInfo(info, rule, characterName, missingUnlock: true);
            }

            if (!state.Unlocked)
            {
                return BuildLockedStatusInfo(info, rule, characterName, missingUnlock: false);
            }

            info.Status = CharacterClassStatus.Valid;
            info.StatusLabel = "Unlocked";
            info.Message = $"{characterName} is ready to adventure as a {info.ClassDisplay}.";
            return info;
        }

        private static CharacterClassStatusInfo BuildLockedStatusInfo(
            CharacterClassStatusInfo baseInfo,
            ClassRuleDefinition rule,
            string characterName,
            bool missingUnlock)
        {
            if (rule.UnlockMethod == ClassUnlockMethod.Starter)
            {
                baseInfo.Status = CharacterClassStatus.Stale;
                baseInfo.StatusLabel = missingUnlock ? "Stale starter data" : "Starter locked";
                baseInfo.Message = $"{characterName}'s starter class {baseInfo.ClassDisplay} should already be unlocked.";
                return baseInfo;
            }

            baseInfo.Status = CharacterClassStatus.Locked;
            baseInfo.StatusLabel = "Locked";
            baseInfo.Message = missingUnlock
                ? $"{characterName} must complete the unlock quest for {baseInfo.ClassDisplay}."
                : $"{baseInfo.ClassDisplay} is locked. Complete its quest to unlock.";
            return baseInfo;
        }

        private static string ResolveRaceName(string raceId)
        {
            if (!string.IsNullOrWhiteSpace(raceId) && RaceCatalog.TryGetRace(raceId, out var race) && race != null)
            {
                return race.DisplayName;
            }

            return string.IsNullOrWhiteSpace(raceId) ? "Unknown" : raceId.Trim();
        }
    }
}
