using System;
using TMPro;
using UnityEngine;

namespace Client.CharacterCreation
{
    public class CharacterCreationFeatureEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Dropdown dropdown;

        private Action<CharacterCreationFeatureEntry> _onChanged;

        public string FeatureId { get; private set; }

        public string SelectedOption
        {
            get
            {
                if (dropdown == null || dropdown.options == null || dropdown.options.Count == 0)
                {
                    return string.Empty;
                }

                var index = Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1);
                return dropdown.options[index].text ?? string.Empty;
            }
        }

        public void Configure(RaceFeatureDefinition definition, Action<CharacterCreationFeatureEntry> onChanged)
        {
            _onChanged = onChanged;
            FeatureId = definition?.Id ?? string.Empty;

            if (label != null)
            {
                label.text = definition?.DisplayName ?? string.Empty;
            }

            if (dropdown == null)
            {
                return;
            }

            dropdown.onValueChanged.RemoveListener(HandleDropdownChanged);
            dropdown.options.Clear();

            var options = definition?.Options ?? Array.Empty<string>();
            if (options.Length == 0)
            {
                options = new[] { "Standard", "Variant A", "Variant B" };
            }

            foreach (var option in options)
            {
                if (string.IsNullOrWhiteSpace(option))
                {
                    continue;
                }

                dropdown.options.Add(new TMP_Dropdown.OptionData(option.Trim()));
            }

            var defaultIndex = 0;
            if (!string.IsNullOrWhiteSpace(definition?.DefaultOption))
            {
                for (var i = 0; i < dropdown.options.Count; i++)
                {
                    if (string.Equals(dropdown.options[i].text, definition.DefaultOption, StringComparison.OrdinalIgnoreCase))
                    {
                        defaultIndex = i;
                        break;
                    }
                }
            }

            dropdown.value = Mathf.Clamp(defaultIndex, 0, Mathf.Max(0, dropdown.options.Count - 1));
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(HandleDropdownChanged);
        }

        private void OnDestroy()
        {
            if (dropdown != null)
            {
                dropdown.onValueChanged.RemoveListener(HandleDropdownChanged);
            }
        }

        private void HandleDropdownChanged(int _)
        {
            _onChanged?.Invoke(this);
        }
    }
}
