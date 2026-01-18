using System.Collections.Generic;
using UnityEngine;

namespace Realm.UI.Tooltips
{
    public static class CombatTooltipDataBuilder
    {
        // Task Stub 1: Build payload from StatusEffectDefinition.
        public static CombatTooltipPayload BuildFromStatusEffect(Object statusDefinition)
        {
            // TODO: Extract name, description, icon, stat modifiers, duration, stack caps, refresh rule, dispel type.
            return new CombatTooltipPayload
            {
                Title = string.Empty,
                Description = string.Empty,
                Icon = null,
                StatModifiers = new List<CombatTooltipStatModifier>(),
                DurationSeconds = 0f,
                MaxStacks = 0,
                RefreshRule = string.Empty,
                DispelType = string.Empty
            };
        }

        // Task Stub 2: Build payload from AbilityDefinition.
        public static CombatTooltipPayload BuildFromAbility(Object abilityDefinition)
        {
            // TODO: Map ability name/description/icon + embedded status effects + runtime stat modifiers.
            return new CombatTooltipPayload
            {
                Title = string.Empty,
                Description = string.Empty,
                Icon = null,
                StatModifiers = new List<CombatTooltipStatModifier>(),
                DurationSeconds = 0f,
                MaxStacks = 0,
                RefreshRule = string.Empty,
                DispelType = string.Empty
            };
        }

        // Task Stub 3: Build payload from Item/Equipment definitions.
        public static CombatTooltipPayload BuildFromItem(Object itemDefinition)
        {
            // TODO: Map item name/description/icon + stat modifiers + embedded status effects.
            return new CombatTooltipPayload
            {
                Title = string.Empty,
                Description = string.Empty,
                Icon = null,
                StatModifiers = new List<CombatTooltipStatModifier>(),
                DurationSeconds = 0f,
                MaxStacks = 0,
                RefreshRule = string.Empty,
                DispelType = string.Empty
            };
        }
    }
}
