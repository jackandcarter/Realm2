using System;
using System.Collections.Generic;
using Client;
using Client.Building;
using Client.Player;
using Client.Progression;
using Client.Terrain;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI
{
    public class ArkitectPlotPanelController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private RuntimePlotManager plotManager;
        [SerializeField] private BuildZoneVisualizer zoneVisualizer;

        [Header("Plot Inputs")]
        [SerializeField] private InputField plotIdField;
        [SerializeField] private InputField centerXField;
        [SerializeField] private InputField centerZField;
        [SerializeField] private InputField widthField;
        [SerializeField] private InputField lengthField;
        [SerializeField] private InputField elevationField;
        [SerializeField] private InputField materialIndexField;

        [Header("Controls")]
        [SerializeField] private Dropdown plotDropdown;
        [SerializeField] private Button createPlotButton;
        [SerializeField] private Button modifyElevationButton;
        [SerializeField] private Button paintMaterialButton;
        [SerializeField] private Text feedbackText;
        [SerializeField] private bool validateBuildZonesOnServer = true;

        private readonly List<BuildPlotDefinition> _cachedPlots = new();
        private Color _defaultFeedbackColor = Color.white;
        private bool _feedbackColorInitialized;
        private BuildZoneValidationClient _zoneValidationClient;

        private void Awake()
        {
            if (plotManager == null)
            {
                plotManager = FindFirstObjectByType<RuntimePlotManager>();
            }

            if (zoneVisualizer == null)
            {
                zoneVisualizer = FindFirstObjectByType<BuildZoneVisualizer>(FindObjectsInactive.Include);
            }

            EnsureValidationClient();
        }

        private void OnEnable()
        {
            if (plotManager != null)
            {
                plotManager.PlotsChanged += OnPlotsChanged;
            }

            PlayerClassStateManager.ArkitectAvailabilityChanged += OnArkitectAvailabilityChanged;
            WireButtons(true);
            EnsureValidationClient();

            RefreshPlots();
            ApplyPermissions(PlayerClassStateManager.IsArkitectAvailable);
            SetFeedback(string.Empty, false);
        }

        private void OnDisable()
        {
            if (plotManager != null)
            {
                plotManager.PlotsChanged -= OnPlotsChanged;
            }

            PlayerClassStateManager.ArkitectAvailabilityChanged -= OnArkitectAvailabilityChanged;
            WireButtons(false);
            if (zoneVisualizer != null)
            {
                zoneVisualizer.HideZones();
            }
        }

        private void WireButtons(bool subscribe)
        {
            if (createPlotButton != null)
            {
                if (subscribe)
                {
                    createPlotButton.onClick.AddListener(OnCreatePlotClicked);
                }
                else
                {
                    createPlotButton.onClick.RemoveListener(OnCreatePlotClicked);
                }
            }

            if (modifyElevationButton != null)
            {
                if (subscribe)
                {
                    modifyElevationButton.onClick.AddListener(OnModifyElevationClicked);
                }
                else
                {
                    modifyElevationButton.onClick.RemoveListener(OnModifyElevationClicked);
                }
            }

            if (paintMaterialButton != null)
            {
                if (subscribe)
                {
                    paintMaterialButton.onClick.AddListener(OnPaintMaterialClicked);
                }
                else
                {
                    paintMaterialButton.onClick.RemoveListener(OnPaintMaterialClicked);
                }
            }
        }

        private void OnPlotsChanged(IReadOnlyList<BuildPlotDefinition> plots)
        {
            _cachedPlots.Clear();
            if (plots != null)
            {
                _cachedPlots.AddRange(plots);
            }

            RefreshDropdown();
        }

        private void OnArkitectAvailabilityChanged(bool available)
        {
            ApplyPermissions(available);
        }

        private void ApplyPermissions(bool available)
        {
            if (createPlotButton != null)
            {
                createPlotButton.interactable = available;
            }

            if (modifyElevationButton != null)
            {
                modifyElevationButton.interactable = available;
            }

            if (paintMaterialButton != null)
            {
                paintMaterialButton.interactable = available;
            }

            UpdateZoneVisualization(available);
        }

        private void RefreshPlots()
        {
            if (plotManager == null)
            {
                return;
            }

            var plots = plotManager.GetPlots();
            OnPlotsChanged(plots == null ? Array.Empty<BuildPlotDefinition>() : new List<BuildPlotDefinition>(plots));
        }

        private void RefreshDropdown()
        {
            if (plotDropdown == null)
            {
                return;
            }

            plotDropdown.ClearOptions();

            var options = new List<string>();
            foreach (var plot in _cachedPlots)
            {
                if (plot == null)
                {
                    options.Add(string.Empty);
                    continue;
                }
                options.Add(string.IsNullOrWhiteSpace(plot.PlotIdentifier) ? plot.PlotId : plot.PlotIdentifier);
            }

            if (options.Count == 0)
            {
                options.Add("No plots");
                plotDropdown.interactable = false;
            }
            else
            {
                plotDropdown.interactable = true;
            }

            plotDropdown.AddOptions(options);
            plotDropdown.value = 0;
            plotDropdown.RefreshShownValue();
        }

        private void OnCreatePlotClicked()
        {
            if (plotManager == null || !PlayerClassStateManager.IsArkitectAvailable)
            {
                return;
            }

            var bounds = BuildBoundsFromInputs();
            var elevation = ParseFloat(elevationField, 0f);
            var materialIndex = Mathf.Max(0, Mathf.RoundToInt(ParseFloat(materialIndexField, 0f)));
            var plotId = string.IsNullOrWhiteSpace(plotIdField?.text) ? Guid.NewGuid().ToString("N") : plotIdField.text.Trim();

            var definition = new BuildPlotDefinition(plotId, bounds, elevation, materialIndex);
            if (validateBuildZonesOnServer && _zoneValidationClient != null && !string.IsNullOrWhiteSpace(SessionManager.SelectedRealmId))
            {
                var payload = new BuildZoneValidationRequest
                {
                    bounds = new BuildZoneBounds
                    {
                        center = bounds.center,
                        size = bounds.size
                    }
                };
                ProgressionCoroutineRunner.Run(
                    _zoneValidationClient.ValidateBounds(
                        SessionManager.SelectedRealmId,
                        payload,
                        response =>
                        {
                            if (response != null && !response.isValid)
                            {
                                SetFeedback(
                                    string.IsNullOrWhiteSpace(response.failureReason)
                                        ? "Unable to create plot."
                                        : response.failureReason,
                                    true
                                );
                                return;
                            }

                            TryCreatePlot(definition);
                        },
                        error =>
                        {
                            if (error != null)
                            {
                                SetFeedback(error.Message, true);
                                return;
                            }

                            TryCreatePlot(definition);
                        }
                    )
                );
                return;
            }

            TryCreatePlot(definition);
        }

        private void TryCreatePlot(BuildPlotDefinition definition)
        {
            if (definition == null || plotManager == null)
            {
                return;
            }

            if (plotManager.TryCreatePlot(definition, out var failureReason))
            {
                RefreshPlots();
                SetFeedback($"Created plot '{definition.PlotId}'.", false);
                zoneVisualizer?.Refresh();
            }
            else
            {
                SetFeedback(string.IsNullOrWhiteSpace(failureReason) ? "Unable to create plot." : failureReason, true);
            }
        }

        private void OnModifyElevationClicked()
        {
            if (plotManager == null || !PlayerClassStateManager.IsArkitectAvailable)
            {
                return;
            }

            var plotId = GetSelectedPlotId();
            if (string.IsNullOrWhiteSpace(plotId))
            {
                return;
            }

            var delta = ParseFloat(elevationField, 0f);
            if (Mathf.Approximately(delta, 0f))
            {
                return;
            }

            if (plotManager.TryModifyElevation(plotId, delta))
            {
                RefreshPlots();
            }
        }

        private void OnPaintMaterialClicked()
        {
            if (plotManager == null || !PlayerClassStateManager.IsArkitectAvailable)
            {
                return;
            }

            var plotId = GetSelectedPlotId();
            if (string.IsNullOrWhiteSpace(plotId))
            {
                return;
            }

            var materialIndex = Mathf.Max(0, Mathf.RoundToInt(ParseFloat(materialIndexField, 0f)));
            if (plotManager.TryPaintMaterial(plotId, materialIndex))
            {
                RefreshPlots();
            }
        }

        private string GetSelectedPlotId()
        {
            if (plotDropdown == null || _cachedPlots.Count == 0)
            {
                return null;
            }

            var index = Mathf.Clamp(plotDropdown.value, 0, _cachedPlots.Count - 1);
            return _cachedPlots[index]?.PlotId;
        }

        private Bounds BuildBoundsFromInputs()
        {
            var center = new Vector3(ParseFloat(centerXField, 0f), 0f, ParseFloat(centerZField, 0f));
            var size = new Vector3(Mathf.Max(1f, ParseFloat(widthField, 5f)), DefaultPlotDepth(), Mathf.Max(1f, ParseFloat(lengthField, 5f)));
            return new Bounds(center, size);
        }

        private void UpdateZoneVisualization(bool available)
        {
            if (zoneVisualizer == null)
            {
                return;
            }

            if (available && isActiveAndEnabled)
            {
                zoneVisualizer.ShowZones();
            }
            else
            {
                zoneVisualizer.HideZones();
            }
        }

        private void SetFeedback(string message, bool isError)
        {
            if (feedbackText == null)
            {
                return;
            }

            if (!_feedbackColorInitialized)
            {
                _defaultFeedbackColor = feedbackText.color;
                _feedbackColorInitialized = true;
            }

            feedbackText.text = message ?? string.Empty;
            feedbackText.color = isError ? Color.red : _defaultFeedbackColor;
        }

        private void EnsureValidationClient()
        {
            if (_zoneValidationClient != null || !ApiClientRegistry.IsConfigured)
            {
                return;
            }

            _zoneValidationClient = new BuildZoneValidationClient(
                ApiClientRegistry.BaseUrl,
                ApiClientRegistry.UseMockServices
            );
        }

        private static float DefaultPlotDepth()
        {
            return 0.5f;
        }

        private static float ParseFloat(InputField field, float fallback)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.text))
            {
                return fallback;
            }

            return float.TryParse(field.text, out var value) ? value : fallback;
        }
    }
}
