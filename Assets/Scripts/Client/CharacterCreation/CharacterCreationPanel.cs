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
        [SerializeField] private Transform previewRoot;
        [SerializeField] private RaceVisualConfig raceVisualConfig;
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
        private readonly List<RaceViewModel> _raceViewModels = new();
        private RaceDefinition _selectedRace;
        private RaceViewModel _selectedRaceViewModel;
        private GameObject _activePreviewInstance;

        public event Action<RaceViewModel> RaceSelected;
        public event Action<CharacterCreationSelection> Confirmed;
        public event Action Cancelled;

        public RaceDefinition SelectedRace => _selectedRace;
        public RaceViewModel SelectedRaceViewModel => _selectedRaceViewModel;
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
                confirmButton.interactable = false;
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(NotifyCancelled);
            }

            UpdateConfirmButtonState();
        }

        private void OnEnable()
        {
            EnsureRaceButtons();
        }

        private void OnDisable()
        {
            ClearPreviewInstance();
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

            ClearPreviewInstance();
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
                if (_selectedRace == null && _raceViewModels.Count > 0)
                {
                    SelectRace(_raceViewModels[0]);
                }

                return;
            }

            _raceViewModels.Clear();

            foreach (var race in RaceCatalog.GetAllRaces())
            {
                raceVisualConfig?.TryGetVisualForRace(race.Id, out var visuals);
                var viewModel = new RaceViewModel(race, visuals);
                _raceViewModels.Add(viewModel);

                var button = Instantiate(raceButtonTemplate, raceListRoot);
                button.gameObject.SetActive(true);
                button.name = $"Race_{race.Id}";

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = race.DisplayName;
                }

                var captured = viewModel;
                button.onClick.AddListener(() => SelectRace(captured));
                _spawnedRaceButtons.Add(button);
            }

            if (_spawnedRaceButtons.Count > 0)
            {
                SelectRace(_raceViewModels[0]);
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
            _selectedRaceViewModel = null;
            _raceViewModels.Clear();
            ClearPreviewInstance();
            UpdateConfirmButtonState();
        }

        private void SelectRace(RaceViewModel viewModel)
        {
            _selectedRaceViewModel = viewModel;
            _selectedRace = viewModel?.Definition;

            if (previewTitle != null)
            {
                previewTitle.text = _selectedRace?.DisplayName ?? string.Empty;
            }

            if (previewSummary != null)
            {
                previewSummary.text = BuildSummary(_selectedRace);
            }

            ConfigureSlider(heightSlider, heightValueLabel, _selectedRace?.Customization?.Height, 0.01f);
            ConfigureSlider(buildSlider, buildValueLabel, _selectedRace?.Customization?.Build, 0.01f);
            PopulateFeatureList(_selectedRace?.Customization?.AdjustableFeatures);
            UpdateRacePreview(viewModel);

            UpdateConfirmButtonState();
            RaceSelected?.Invoke(viewModel);
        }

        private void UpdateRacePreview(RaceViewModel viewModel)
        {
            ClearPreviewInstance();

            if (previewRoot == null || viewModel?.PreviewPrefab == null)
            {
                return;
            }

            _activePreviewInstance = Instantiate(viewModel.PreviewPrefab, previewRoot);
            if (_activePreviewInstance == null)
            {
                return;
            }

            _activePreviewInstance.transform.localPosition = Vector3.zero;
            _activePreviewInstance.transform.localRotation = Quaternion.identity;
            _activePreviewInstance.transform.localScale = Vector3.one;
            viewModel.ApplyDefaultMaterials(_activePreviewInstance);
        }

        private void ClearPreviewInstance()
        {
            if (_activePreviewInstance != null)
            {
                Destroy(_activePreviewInstance);
                _activePreviewInstance = null;
            }
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
            UpdateConfirmButtonState();
        }

        private void UpdateConfirmButtonState()
        {
            if (confirmButton == null)
            {
                return;
            }

            var hasRace = _selectedRace != null;
            var heightValid = !hasRace || heightSlider == null || IsValueWithinRange(_selectedRace?.Customization?.Height, SelectedHeight);
            var buildValid = !hasRace || buildSlider == null || IsValueWithinRange(_selectedRace?.Customization?.Build, SelectedBuild);

            confirmButton.interactable = hasRace && heightValid && buildValid;
        }

        private bool IsCurrentSelectionValid()
        {
            if (_selectedRace == null)
            {
                return false;
            }

            if (heightSlider != null && !IsValueWithinRange(_selectedRace.Customization?.Height, SelectedHeight))
            {
                return false;
            }

            if (buildSlider != null && !IsValueWithinRange(_selectedRace.Customization?.Build, SelectedBuild))
            {
                return false;
            }

            return true;
        }

        private static bool IsValueWithinRange(FloatRange? range, float value)
        {
            if (!range.HasValue)
            {
                return true;
            }

            return value >= range.Value.Min - Mathf.Epsilon && value <= range.Value.Max + Mathf.Epsilon;
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
            UpdateConfirmButtonState();
        }

        private void OnBuildSliderChanged(float _)
        {
            UpdateSliderLabel(buildSlider, buildValueLabel);
            UpdateConfirmButtonState();
        }

        private void NotifyConfirmed()
        {
            if (!IsCurrentSelectionValid())
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
