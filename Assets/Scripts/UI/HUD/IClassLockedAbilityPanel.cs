using System.Collections.Generic;
using Client.CharacterCreation;

namespace Client.UI.HUD
{
    public interface IClassLockedAbilityPanel
    {
        void SetLockedAbilities(string classId, IReadOnlyList<ClassAbilityCatalog.ClassAbilityDockEntry> lockedAbilities);
    }
}
