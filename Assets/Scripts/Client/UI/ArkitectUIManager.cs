using System;
using System.Collections.Generic;
using System.Text;
using Building;
using Client.CharacterCreation;
using Client.Builder;
using Client.UI.HUD;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    [RequireComponent(typeof(RectTransform))]
    public class ArkitectUIManager : MonoBehaviour, IClassUiModule
    {
        [Header("Containers")]
        [SerializeField] private RectTransform panelsContainer;
        [SerializeField] private RectTransform tabContainer;

        [Header("Tab Buttons")]
        [SerializeField] private Button plotsTabButton;
        [SerializeField] private Button terrainTabButton;
        [SerializeField] private Button materialsTabButton;
        [SerializeField] private Button blueprintsTabButton;
        [SerializeField] private Button commissionTabButton;

        [Header("Panels")]
        [SerializeField] private GameObject plotsPanel;
        [SerializeField] private GameObject terrainPanel;
        [SerializeField] private GameObject materialsPanel;
        [SerializeField] private GameObject blueprintsPanel;
        [SerializeField] private GameObject commissionPanel;
        [SerializeField] private ArkitectPlotPanelController plotPanelController;
        [SerializeField] private BuilderDockAbilityBinder dockAbilityBinder;

        [Header("Styling")]
        [SerializeField] private Color activeTabColor = new Color(0.533f, 0.286f, 0.741f, 1f);
        [SerializeField] private Color inactiveTabColor = new Color(0.118f, 0.149f, 0.231f, 0.9f);

        private readonly List<TabBinding> _tabs = new List<TabBinding>();
        private GameObject _activePanel;
        private bool _hasPermissions = true;
        private static TMP_FontAsset _defaultFont;

        private const string PanelsRegistryId = "arkitect.ui.panels";
        private const string TabBarRegistryId = "arkitect.ui.tabbar";
        private const string PlotsPanelRegistryId = "arkitect.ui.panel.plots";
        private const string TerrainPanelRegistryId = "arkitect.ui.panel.terrain";
        private const string MaterialsPanelRegistryId = "arkitect.ui.panel.materials";
        private const string BlueprintsPanelRegistryId = "arkitect.ui.panel.blueprints";
        private const string CommissionsPanelRegistryId = "arkitect.ui.panel.commissions";

        private struct TabBinding
        {
            public Button Button;
            public GameObject Panel;
        }

        public string ClassId => ClassUnlockUtility.BuilderClassId;

        public void Mount(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            if (!TryGetComponent(out RectTransform rectTransform))
            {
                Debug.LogError("ArkitectUIManager requires a RectTransform on the same GameObject.", this);
                return;
            }

            rectTransform.SetParent(parent, false);
            gameObject.SetActive(true);

            InitializeUi();
            dockAbilityBinder?.Mount(parent);
        }

        public void Unmount()
        {
            gameObject.SetActive(false);
            dockAbilityBinder?.Unmount();
        }

        public void OnAbilityStateChanged(string abilityId, bool enabled)
        {
            ApplyPermissions(enabled);
            dockAbilityBinder?.OnAbilityStateChanged(abilityId, enabled);
        }

        private void ApplyPermissions(bool available)
        {
            _hasPermissions = available;

            if (tabContainer != null)
            {
                tabContainer.gameObject.SetActive(available);
            }

            if (panelsContainer != null)
            {
                panelsContainer.gameObject.SetActive(available);
            }

            foreach (var tab in _tabs)
            {
                if (tab.Button != null)
                {
                    tab.Button.interactable = available;
                }

                if (tab.Panel != null)
                {
                    tab.Panel.SetActive(available && tab.Panel == _activePanel);
                }
            }
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                InitializeUi();
            }
        }

        private void OnEnable()
        {
            InitializeUi();
        }

        private void InitializeUi()
        {
            if (!EnsureCanvasSetup())
            {
                return;
            }
            EnsureContainers();
            EnsureTabs();
            EnsurePanels();
            EnsurePlotPanelController();
            WireTabs();

            if (_activePanel == null)
            {
                _activePanel = plotsPanel != null ? plotsPanel : terrainPanel ?? materialsPanel ?? blueprintsPanel ?? commissionPanel;
            }

            if (_activePanel != null)
            {
                ShowPanel(_activePanel);
            }

            ApplyPermissions(_hasPermissions);
        }

        private bool EnsureCanvasSetup()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("ArkitectUIManager requires a Canvas component in the scene.", this);
                return false;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            if (!TryGetComponent<RectTransform>(out var rectTransform))
            {
                Debug.LogError("ArkitectUIManager requires a RectTransform component in the scene.", this);
                return false;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            if (GetComponent<CanvasScaler>() == null)
            {
                Debug.LogError("ArkitectUIManager requires a CanvasScaler component in the scene.", this);
                return false;
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                Debug.LogError("ArkitectUIManager requires a GraphicRaycaster component in the scene.", this);
                return false;
            }

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            return true;
        }

        private void EnsureContainers()
        {
            if (panelsContainer == null && ArkitectRegistry.TryGetUiPanel(PanelsRegistryId, out var trackedPanels))
            {
                panelsContainer = trackedPanels != null ? trackedPanels.GetComponent<RectTransform>() : null;
            }

            if (panelsContainer == null)
            {
                var existing = transform.Find("Panels");
                if (existing != null)
                {
                    panelsContainer = existing as RectTransform;
                }
            }

            if (panelsContainer == null)
            {
                Debug.LogWarning("ArkitectUIManager is missing the Panels container. Assign it in the scene.", this);
            }
            else
            {
                ConfigurePanelsContainer(panelsContainer);
                ReparentIfNecessary(panelsContainer, transform as RectTransform);
                ArkitectRegistry.RegisterUiPanel(PanelsRegistryId, panelsContainer.gameObject);
            }

            if (tabContainer == null && ArkitectRegistry.TryGetUiPanel(TabBarRegistryId, out var trackedTabBar))
            {
                tabContainer = trackedTabBar != null ? trackedTabBar.GetComponent<RectTransform>() : null;
            }

            if (tabContainer == null)
            {
                var existing = transform.Find("TabBar");
                if (existing != null)
                {
                    tabContainer = existing as RectTransform;
                }
            }

            if (tabContainer == null)
            {
                Debug.LogWarning("ArkitectUIManager is missing the TabBar container. Assign it in the scene.", this);
            }
            else
            {
                ConfigureTabContainer(tabContainer);
                ReparentIfNecessary(tabContainer, transform as RectTransform);
                ArkitectRegistry.RegisterUiPanel(TabBarRegistryId, tabContainer.gameObject);
            }
        }

        private void EnsureTabs()
        {
            EnsureButton(ref plotsTabButton, "PlotsTab", "Plots");
            EnsureButton(ref terrainTabButton, "TerrainTab", "Terrain");
            EnsureButton(ref materialsTabButton, "MaterialsTab", "Materials");
            EnsureButton(ref blueprintsTabButton, "BlueprintsTab", "Blueprints");
            EnsureButton(ref commissionTabButton, "CommissionsTab", "Commissions");
        }

        private void EnsureButton(ref Button buttonReference, string objectName, string label)
        {
            if (buttonReference == null && tabContainer != null)
            {
                var existing = tabContainer.Find(objectName);
                if (existing != null && existing.TryGetComponent(out Button existingButton))
                {
                    buttonReference = existingButton;
                }
            }

            if (buttonReference == null)
            {
                Debug.LogWarning($"ArkitectUIManager is missing the {objectName} button. Assign it in the scene.", this);
            }
            else
            {
                UpdateButtonLabel(buttonReference, label);
            }
        }

        private void EnsurePanels()
        {
            EnsurePanel(ref plotsPanel, "PlotsPanel",
                "Claim frontier plots and grow settlements to embody Realm's player-driven worldbuilding.",
                PlotsPanelRegistryId);
            EnsurePanel(ref terrainPanel, "TerrainPanel",
                "Shape the land within your plots using Arkitect magicks and unlock new terrain rituals.",
                TerrainPanelRegistryId);
            EnsurePanel(ref materialsPanel, "MaterialsPanel",
                "Curate arcane reagents and technomantic alloys that empower construction and crafting.",
                MaterialsPanelRegistryId);
            EnsurePanel(ref blueprintsPanel, "BlueprintsPanel",
                "Unlock radiant designs that blend crystalline spires with luminous machinery.",
                BlueprintsPanelRegistryId);
            EnsurePanel(ref commissionPanel, "CommissionsPanel",
                "Review community commissions and collaborate on magitech megastructures for Elysium.",
                CommissionsPanelRegistryId);
        }

        private void EnsurePlotPanelController()
        {
            if (plotPanelController == null && plotsPanel != null)
            {
                plotPanelController = plotsPanel.GetComponentInChildren<ArkitectPlotPanelController>(true);
            }
        }

        private void EnsurePanel(ref GameObject panelReference, string panelName, string description, string registryId)
        {
            if (panelReference == null && !string.IsNullOrWhiteSpace(registryId) &&
                ArkitectRegistry.TryGetUiPanel(registryId, out var trackedPanel) && trackedPanel != null)
            {
                panelReference = trackedPanel;
            }

            if (panelReference == null && panelsContainer != null)
            {
                var existing = panelsContainer.Find(panelName);
                if (existing != null)
                {
                    panelReference = existing.gameObject;
                }
            }

            if (panelReference == null)
            {
                Debug.LogWarning($"ArkitectUIManager is missing the {panelName} panel. Assign it in the scene.", this);
            }
            else
            {
                ReparentIfNecessary(panelReference.transform, panelsContainer);
                RefreshPanelVisuals(panelReference.transform, description);
            }

            if (panelReference != null && !string.IsNullOrWhiteSpace(registryId))
            {
                ArkitectRegistry.RegisterUiPanel(registryId, panelReference);
            }
        }

        private void WireTabs()
        {
            _tabs.Clear();
            RegisterTab(plotsTabButton, plotsPanel);
            RegisterTab(terrainTabButton, terrainPanel);
            RegisterTab(materialsTabButton, materialsPanel);
            RegisterTab(blueprintsTabButton, blueprintsPanel);
            RegisterTab(commissionTabButton, commissionPanel);
        }

        private void RegisterTab(Button button, GameObject panel)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            var targetPanel = panel;
            button.onClick.AddListener(() => ShowPanel(targetPanel));

            if (!button.TryGetComponent(out Image image))
            {
                Debug.LogWarning($"ArkitectUIManager tab button '{button.name}' is missing an Image component.", button);
                return;
            }

            image.color = inactiveTabColor;

            _tabs.Add(new TabBinding { Button = button, Panel = panel });
        }

        private void ShowPanel(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            foreach (var tab in _tabs)
            {
                var isActive = tab.Panel == panel;
                if (tab.Panel != null)
                {
                    tab.Panel.SetActive(isActive);
                }

                if (tab.Button != null && tab.Button.TryGetComponent(out Image image))
                {
                    image.color = isActive ? activeTabColor : inactiveTabColor;
                }
            }

            _activePanel = panel;
        }

        private void ConfigureTabContainer(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -30f);
            rect.sizeDelta = new Vector2(0f, 90f);

            if (!rect.TryGetComponent(out HorizontalLayoutGroup layout))
            {
                Debug.LogWarning("ArkitectUIManager TabBar is missing a HorizontalLayoutGroup component.", rect);
                return;
            }

            layout.spacing = 18f;
            layout.padding.left = 24;
            layout.padding.right = 24;
            layout.padding.top = 18;
            layout.padding.bottom = 18;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void ConfigurePanelsContainer(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -30f);
            rect.sizeDelta = new Vector2(0f, -120f);
        }

        private void RefreshPanelVisuals(Transform panelTransform, string description)
        {
            if (panelTransform == null)
            {
                return;
            }

            if (panelTransform.TryGetComponent(out Image panelImage))
            {
                panelImage.color = new Color(0.109f, 0.137f, 0.211f, 0.88f);
            }

            var title = panelTransform.Find("Title");
            if (title != null && title.TryGetComponent(out TMP_Text titleText) && string.IsNullOrWhiteSpace(titleText.text))
            {
                titleText.text = SplitTitle(panelTransform.name);
            }

            if (title != null && title.TryGetComponent(out TMP_Text resolvedTitle))
            {
                resolvedTitle.color = new Color(0.898f, 0.768f, 1f, 1f);
                resolvedTitle.fontStyle = FontStyles.Bold;
                resolvedTitle.fontSize = 28;
                resolvedTitle.alignment = TextAlignmentOptions.TopLeft;
                resolvedTitle.font = resolvedTitle.font == null ? GetDefaultFontAsset() : resolvedTitle.font;
            }

            var body = panelTransform.Find("Description");
            if (body != null && body.TryGetComponent(out TMP_Text bodyText))
            {
                if (string.IsNullOrWhiteSpace(bodyText.text) || bodyText.text == "Description")
                {
                    bodyText.text = description;
                }

                bodyText.color = new Color(0.788f, 0.862f, 0.976f, 1f);
                bodyText.fontStyle = FontStyles.Normal;
                bodyText.fontSize = 20;
                bodyText.alignment = TextAlignmentOptions.TopLeft;
                bodyText.font = bodyText.font == null ? GetDefaultFontAsset() : bodyText.font;
            }
        }

        private static void ReparentIfNecessary(Transform target, Transform parent)
        {
            if (target == null || parent == null)
            {
                return;
            }

            if (target.parent != parent)
            {
                target.SetParent(parent, false);
            }
        }

        private void UpdateButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            var labelTransform = button.transform.Find("Label");
            if (labelTransform == null)
            {
                labelTransform = button.transform.Find("Text");
            }

            if (labelTransform == null)
            {
                Debug.LogWarning($"ArkitectUIManager button '{button.name}' is missing a Label/Text child.", button);
                return;
            }

            if (labelTransform.TryGetComponent(out TMP_Text text))
            {
                text.text = label;
                text.fontStyle = FontStyles.Bold;
                text.fontSize = 22;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(0.839f, 0.925f, 0.992f, 1f);
                text.font = text.font == null ? GetDefaultFontAsset() : text.font;
            }
        }

        private static TMP_FontAsset GetDefaultFontAsset()
        {
            if (_defaultFont == null)
            {
                _defaultFont = TMP_Settings.defaultFontAsset;
            }

            return _defaultFont;
        }

        private string SplitTitle(string panelName)
        {
            if (string.IsNullOrEmpty(panelName))
            {
                return panelName;
            }

            var workingName = panelName.EndsWith("Panel", StringComparison.Ordinal)
                ? panelName[..^"Panel".Length]
                : panelName;

            if (workingName.Length == 0)
            {
                return panelName;
            }

            var builder = new StringBuilder();
            builder.Append(workingName[0]);
            for (var i = 1; i < workingName.Length; i++)
            {
                var c = workingName[i];
                if (char.IsUpper(c) && !char.IsWhiteSpace(workingName[i - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
