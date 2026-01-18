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
        private readonly List<CombatTooltipStatModifierRow> _statModifierRows = new();

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
                if (!string.IsNullOrWhiteSpace(payload.DurationLabel))
                {
                    durationLabel.text = $"Duration: {payload.DurationLabel}";
                }
                else
                {
                    durationLabel.text = payload.DurationSeconds > 0f ? $"Duration: {payload.DurationSeconds:0.##}s" : string.Empty;
                }
            }

            if (stacksLabel != null)
            {
                stacksLabel.text = payload.MaxStacks > 1 ? $"Max Stacks: {payload.MaxStacks}" : string.Empty;
            }

            if (refreshRuleLabel != null)
            {
                refreshRuleLabel.text = !string.IsNullOrWhiteSpace(payload.RefreshRule)
                    ? $"Refresh: {payload.RefreshRule}"
                    : string.Empty;
            }

            if (dispelTypeLabel != null)
            {
                dispelTypeLabel.text = !string.IsNullOrWhiteSpace(payload.DispelType)
                    ? $"Dispel: {payload.DispelType}"
                    : string.Empty;
            }
        }

        private void ApplyStatModifiers(IReadOnlyList<CombatTooltipStatModifier> modifiers)
        {
            // TODO: Pool or clear rows before populating with stat modifier data.
            if (statModifierContainer == null || statModifierRowPrefab == null)
            {
                return;
            }

            EnsureRowCount(modifiers?.Count ?? 0);

            if (modifiers == null)
            {
                HideAllRows();
                return;
            }

            for (var i = 0; i < modifiers.Count; i++)
            {
                var row = _statModifierRows[i];
                row.gameObject.SetActive(true);
                row.Bind(modifiers[i]);
            }

            for (var i = modifiers.Count; i < _statModifierRows.Count; i++)
            {
                _statModifierRows[i].gameObject.SetActive(false);
            }
        }

        private void EnsureRowCount(int count)
        {
            if (count <= _statModifierRows.Count)
            {
                return;
            }

            for (var i = _statModifierRows.Count; i < count; i++)
            {
                var row = Instantiate(statModifierRowPrefab, statModifierContainer);
                row.gameObject.SetActive(false);
                _statModifierRows.Add(row);
            }
        }

        private void HideAllRows()
        {
            foreach (var row in _statModifierRows)
            {
                row.gameObject.SetActive(false);
            }
        }
    }
}
