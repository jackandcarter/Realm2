using Realm.UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI.HUD.Inventory
{
    [DisallowMultipleComponent]
    public class InventorySlotView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text quantityLabel;

        private CombatTooltipTrigger _tooltipTrigger;

        public void SetItem(Sprite icon, int quantity, Object tooltipDefinition, CombatTooltipBindings bindings)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (quantityLabel != null)
            {
                quantityLabel.text = quantity > 1 ? quantity.ToString() : string.Empty;
            }

            ConfigureTooltip(tooltipDefinition, bindings);
        }

        public void Clear(CombatTooltipBindings bindings)
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (quantityLabel != null)
            {
                quantityLabel.text = string.Empty;
            }

            ConfigureTooltip(null, bindings);
        }

        private void ConfigureTooltip(Object definition, CombatTooltipBindings bindings)
        {
            if (bindings == null)
            {
                if (_tooltipTrigger != null)
                {
                    Destroy(_tooltipTrigger);
                    _tooltipTrigger = null;
                }

                return;
            }

            if (definition == null)
            {
                if (_tooltipTrigger != null)
                {
                    Destroy(_tooltipTrigger);
                    _tooltipTrigger = null;
                }

                return;
            }

            _tooltipTrigger = bindings.RegisterItemSource(definition, gameObject);
        }

        private void Reset()
        {
            if (iconImage == null)
            {
                iconImage = GetComponentInChildren<Image>();
            }

            if (quantityLabel == null)
            {
                quantityLabel = GetComponentInChildren<Text>();
            }
        }
    }
}
