using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Realm.UI.Tooltips
{
    public sealed class CombatTooltipView : MonoBehaviour
    {
        [Header("Core Fields")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private Image iconImage;

        [Header("Status Metadata")]
        [SerializeField] private TMP_Text durationLabel;
        [SerializeField] private TMP_Text stacksLabel;
        [SerializeField] private TMP_Text refreshRuleLabel;
        [SerializeField] private TMP_Text dispelTypeLabel;

        [Header("Stat Modifiers")]
        [SerializeField] private RectTransform statModifierContainer;
        [SerializeField] private CombatTooltipStatModifierRow statModifierRowPrefab;

        public void Bind(CombatTooltipPayload payload)
        {
            // Task Stub 4: Populate UI fields from payload and drive stat modifier rows.
            if (titleLabel != null)
            {
                titleLabel.text = payload.Title;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = payload.Description;
            }

            if (iconImage != null)
            {
                iconImage.sprite = payload.Icon;
                iconImage.enabled = payload.Icon != null;
            }

            ApplyMetadata(payload);
            ApplyStatModifiers(payload.StatModifiers);
        }

        private void ApplyMetadata(CombatTooltipPayload payload)
        {
            // TODO: Format duration/stacks/refresh/dispel fields with proper labels.
            if (durationLabel != null)
            {
                durationLabel.text = payload.DurationSeconds > 0f ? payload.DurationSeconds.ToString("0.##") : string.Empty;
            }

            if (stacksLabel != null)
            {
                stacksLabel.text = payload.MaxStacks > 0 ? payload.MaxStacks.ToString() : string.Empty;
            }

            if (refreshRuleLabel != null)
            {
                refreshRuleLabel.text = payload.RefreshRule ?? string.Empty;
            }

            if (dispelTypeLabel != null)
            {
                dispelTypeLabel.text = payload.DispelType ?? string.Empty;
            }
        }

        private void ApplyStatModifiers(IReadOnlyList<CombatTooltipStatModifier> modifiers)
        {
            // TODO: Pool or clear rows before populating with stat modifier data.
            if (statModifierContainer == null || statModifierRowPrefab == null)
            {
                return;
            }

            for (int i = statModifierContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(statModifierContainer.GetChild(i).gameObject);
            }

            if (modifiers == null)
            {
                return;
            }

            foreach (var modifier in modifiers)
            {
                var row = Instantiate(statModifierRowPrefab, statModifierContainer);
                row.Bind(modifier);
            }
        }
    }
}
