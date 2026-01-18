using UnityEngine;

namespace Realm.UI.Tooltips
{
    public sealed class CombatTooltipBindings : MonoBehaviour
    {
        [SerializeField] private CombatTooltipController tooltipController;

        // Task Stub 7: Wire tooltip events from HUD ability buttons.
        public void RegisterAbilitySource(Object abilityDefinition)
        {
            // TODO: Build payload via CombatTooltipDataBuilder and connect hover handlers.
        }

        // Task Stub 8: Wire tooltip events from inventory items.
        public void RegisterItemSource(Object itemDefinition)
        {
            // TODO: Build payload via CombatTooltipDataBuilder and connect hover handlers.
        }

        // Task Stub 9: Wire tooltip events from equipment slots.
        public void RegisterEquipmentSource(Object equipmentDefinition)
        {
            // TODO: Build payload via CombatTooltipDataBuilder and connect hover handlers.
        }

        // Task Stub 10: Wire tooltip events for status effect HUD icons.
        public void RegisterStatusSource(Object statusDefinition)
        {
            // TODO: Build payload via CombatTooltipDataBuilder and connect hover handlers.
        }
    }
}
