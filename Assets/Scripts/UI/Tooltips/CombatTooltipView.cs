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
            ApplyLabel(titleLabel, payload.Title);
            ApplyLabel(descriptionLabel, payload.Description);

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
            if (durationLabel != null)
            {
                var durationText = !string.IsNullOrWhiteSpace(payload.DurationLabel)
                    ? $"Duration: {payload.DurationLabel}"
                    : payload.DurationSeconds > 0f ? $"Duration: {payload.DurationSeconds:0.##}s" : string.Empty;
                ApplyLabel(durationLabel, durationText);
            }

            if (stacksLabel != null)
            {
                ApplyLabel(stacksLabel, payload.MaxStacks > 1 ? $"Max Stacks: {payload.MaxStacks}" : string.Empty);
            }

            if (refreshRuleLabel != null)
            {
                ApplyLabel(refreshRuleLabel, !string.IsNullOrWhiteSpace(payload.RefreshRule)
                    ? $"Refresh: {payload.RefreshRule}"
                    : string.Empty);
            }

            if (dispelTypeLabel != null)
            {
                ApplyLabel(dispelTypeLabel, !string.IsNullOrWhiteSpace(payload.DispelType)
                    ? $"Dispel: {payload.DispelType}"
                    : string.Empty);
            }
        }

        private void ApplyStatModifiers(IReadOnlyList<CombatTooltipStatModifier> modifiers)
        {
            if (statModifierContainer == null || statModifierRowPrefab == null)
            {
                return;
            }

            var modifierCount = modifiers?.Count ?? 0;
            statModifierContainer.gameObject.SetActive(modifierCount > 0);

            if (modifierCount == 0)
            {
                HideAllRows();
                return;
            }

            EnsureRowCount(modifierCount);

            for (var i = 0; i < modifierCount; i++)
            {
                var row = _statModifierRows[i];
                row.gameObject.SetActive(true);
                row.Bind(modifiers[i]);
            }

            for (var i = modifierCount; i < _statModifierRows.Count; i++)
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

        private static void ApplyLabel(TMP_Text label, string value)
        {
            if (label == null)
            {
                return;
            }

            label.text = value ?? string.Empty;
            label.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
        }
    }
}
