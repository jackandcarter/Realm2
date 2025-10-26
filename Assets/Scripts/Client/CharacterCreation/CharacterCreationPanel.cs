using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Client.CharacterCreation
{
    public class CharacterCreationPanel : MonoBehaviour
    {
        [SerializeField] private Transform raceListRoot;
        [SerializeField] private Button raceButtonTemplate;
        [SerializeField] private Text previewTitle;
        [SerializeField] private Text previewSummary;
        [SerializeField] private Slider heightSlider;
        [SerializeField] private Text heightValueLabel;
        [SerializeField] private Slider buildSlider;
        [SerializeField] private Text buildValueLabel;
        [SerializeField] private Transform featureListRoot;
        [SerializeField] private Text featureEntryTemplate;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private readonly List<Button> _spawnedRaceButtons = new();
        private readonly List<Text> _spawnedFeatureEntries = new();
        private RaceDefinition _selectedRace;

        public event Action<RaceDefinition> RaceSelected;
        public event Action<CharacterCreationSelection> Confirmed;
        public event Action Cancelled;

        public RaceDefinition SelectedRace => _selectedRace;
        public float SelectedHeight => heightSlider != null ? heightSlider.value : 0f;
        public float SelectedBuild => buildSlider != null ? buildSlider.value : 0f;

        private void Awake()
        {
            if (raceButtonTemplate != null)
            {
                raceButtonTemplate.gameObject.SetActive(false);
            }

            if (featureEntryTemplate != null)
            {
                featureEntryTemplate.gameObject.SetActive(false);
            }

            if (heightSlider != null)
            {
                heightSlider.onValueChanged.AddListener(OnHeightSliderChanged);
            }

            if (buildSlider != null)
            {
                buildSlider.onValueChanged.AddListener(OnBuildSliderChanged);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(NotifyConfirmed);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(NotifyCancelled);
            }
        }

        private void OnEnable()
        {
            EnsureRaceButtons();
        }

        private void OnDestroy()
        {
            if (heightSlider != null)
            {
                heightSlider.onValueChanged.RemoveListener(OnHeightSliderChanged);
            }

            if (buildSlider != null)
            {
                buildSlider.onValueChanged.RemoveListener(OnBuildSliderChanged);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(NotifyConfirmed);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(NotifyCancelled);
            }
        }

        public void Refresh()
        {
            EnsureRaceButtons(true);
        }

        private void EnsureRaceButtons(bool forceRefresh = false)
        {
            if (raceListRoot == null || raceButtonTemplate == null)
            {
                return;
            }

            if (forceRefresh)
            {
                ClearRaceButtons();
            }

            if (_spawnedRaceButtons.Count > 0)
            {
                if (_selectedRace == null && RaceCatalog.GetAllRaces().Count > 0)
                {
                    SelectRace(RaceCatalog.GetAllRaces()[0]);
                }

                return;
            }

            foreach (var race in RaceCatalog.GetAllRaces())
            {
                var button = Instantiate(raceButtonTemplate, raceListRoot);
                button.gameObject.SetActive(true);
                button.name = $"Race_{race.Id}";

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = race.DisplayName;
                }

                var captured = race;
                button.onClick.AddListener(() => SelectRace(captured));
                _spawnedRaceButtons.Add(button);
            }

            if (_spawnedRaceButtons.Count > 0)
            {
                SelectRace(RaceCatalog.GetAllRaces()[0]);
            }
        }

        private void ClearRaceButtons()
        {
            foreach (var button in _spawnedRaceButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            _spawnedRaceButtons.Clear();
            _selectedRace = null;
        }

        private void SelectRace(RaceDefinition race)
        {
            _selectedRace = race;

            if (previewTitle != null)
            {
                previewTitle.text = race?.DisplayName ?? string.Empty;
            }

            if (previewSummary != null)
            {
                previewSummary.text = BuildSummary(race);
            }

            ConfigureSlider(heightSlider, heightValueLabel, race?.Customization?.Height, 0.01f);
            ConfigureSlider(buildSlider, buildValueLabel, race?.Customization?.Build, 0.01f);
            PopulateFeatureList(race?.Customization?.AdjustableFeatures);

            RaceSelected?.Invoke(race);
        }

        private static string BuildSummary(RaceDefinition race)
        {
            if (race == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(race.AppearanceSummary))
            {
                sb.AppendLine(race.AppearanceSummary.Trim());
            }

            if (!string.IsNullOrWhiteSpace(race.LoreSummary))
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb.AppendLine(race.LoreSummary.Trim());
            }

            if (race.SignatureAbilities != null && race.SignatureAbilities.Length > 0)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb.AppendLine("Signature Abilities:");
                foreach (var ability in race.SignatureAbilities)
                {
                    if (!string.IsNullOrWhiteSpace(ability))
                    {
                        sb.Append(" • ");
                        sb.AppendLine(ability.Trim());
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        private void ConfigureSlider(Slider slider, Text label, FloatRange? range, float defaultStep)
        {
            if (slider == null)
            {
                return;
            }

            if (range.HasValue)
            {
                slider.minValue = range.Value.Min;
                slider.maxValue = range.Value.Max;
                var midpoint = Mathf.Lerp(range.Value.Min, range.Value.Max, 0.5f);
                slider.value = Mathf.Clamp(slider.value, slider.minValue, slider.maxValue);
                if (Mathf.Approximately(slider.value, 0f) || slider.value < slider.minValue || slider.value > slider.maxValue)
                {
                    slider.value = midpoint;
                }
            }
            else
            {
                slider.minValue = 0f;
                slider.maxValue = defaultStep;
                slider.value = defaultStep * 0.5f;
            }

            UpdateSliderLabel(slider, label);
        }

        private void PopulateFeatureList(IReadOnlyCollection<string> features)
        {
            foreach (var entry in _spawnedFeatureEntries)
            {
                if (entry != null)
                {
                    Destroy(entry.gameObject);
                }
            }

            _spawnedFeatureEntries.Clear();

            if (featureListRoot == null || featureEntryTemplate == null || features == null)
            {
                return;
            }

            foreach (var feature in features)
            {
                if (string.IsNullOrWhiteSpace(feature))
                {
                    continue;
                }

                var entry = Instantiate(featureEntryTemplate, featureListRoot);
                entry.gameObject.SetActive(true);
                entry.text = $"• {feature.Trim()}";
                _spawnedFeatureEntries.Add(entry);
            }
        }

        private void UpdateSliderLabel(Slider slider, Text label)
        {
            if (label == null || slider == null)
            {
                return;
            }

            label.text = slider.value.ToString("0.00");
        }

        private void OnHeightSliderChanged(float _)
        {
            UpdateSliderLabel(heightSlider, heightValueLabel);
        }

        private void OnBuildSliderChanged(float _)
        {
            UpdateSliderLabel(buildSlider, buildValueLabel);
        }

        private void NotifyConfirmed()
        {
            if (_selectedRace == null)
            {
                return;
            }

            var selection = new CharacterCreationSelection
            {
                Race = _selectedRace,
                Height = SelectedHeight,
                Build = SelectedBuild
            };

            Confirmed?.Invoke(selection);
        }

        private void NotifyCancelled()
        {
            Cancelled?.Invoke();
        }
    }

    [Serializable]
    public struct CharacterCreationSelection
    {
        public RaceDefinition Race;
        public float Height;
        public float Build;
    }
}
