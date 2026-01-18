using UnityEngine;

namespace Realm.UI.Tooltips
{
    public sealed class CombatTooltipBindings : MonoBehaviour
    {
        [SerializeField] private CombatTooltipController tooltipController;

        // Task Stub 7: Wire tooltip events from HUD ability buttons.
        public void RegisterAbilitySource(Object abilityDefinition)
        {
            RegisterAbilitySource(abilityDefinition, gameObject);
        }

        public CombatTooltipTrigger RegisterAbilitySource(Object abilityDefinition, GameObject target)
        {
            return RegisterSource(CombatTooltipSourceType.Ability, abilityDefinition, target);
        }

        // Task Stub 8: Wire tooltip events from inventory items.
        public void RegisterItemSource(Object itemDefinition)
        {
            RegisterItemSource(itemDefinition, gameObject);
        }

        public CombatTooltipTrigger RegisterItemSource(Object itemDefinition, GameObject target)
        {
            return RegisterSource(CombatTooltipSourceType.Item, itemDefinition, target);
        }

        // Task Stub 9: Wire tooltip events from equipment slots.
        public void RegisterEquipmentSource(Object equipmentDefinition)
        {
            RegisterEquipmentSource(equipmentDefinition, gameObject);
        }

        public CombatTooltipTrigger RegisterEquipmentSource(Object equipmentDefinition, GameObject target)
        {
            return RegisterSource(CombatTooltipSourceType.Equipment, equipmentDefinition, target);
        }

        // Task Stub 10: Wire tooltip events for status effect HUD icons.
        public void RegisterStatusSource(Object statusDefinition)
        {
            RegisterStatusSource(statusDefinition, gameObject);
        }

        public CombatTooltipTrigger RegisterStatusSource(Object statusDefinition, GameObject target)
        {
            return RegisterSource(CombatTooltipSourceType.Status, statusDefinition, target);
        }

        private CombatTooltipTrigger RegisterSource(CombatTooltipSourceType sourceType, Object definition, GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            var trigger = target.GetComponent<CombatTooltipTrigger>();
            if (trigger == null)
            {
                trigger = target.AddComponent<CombatTooltipTrigger>();
            }

            trigger.Configure(tooltipController, sourceType, definition);
            return trigger;
        }
    }
}
