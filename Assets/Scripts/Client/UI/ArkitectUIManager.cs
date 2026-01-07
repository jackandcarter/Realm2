using System;
using System.Collections.Generic;
using System.Text;
using Building;
using Client.CharacterCreation;
using Client.UI.HUD;
using Client.UI.HUD.Dock;
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

        [Header("Styling")]
        [SerializeField] private Color activeTabColor = new Color(0.533f, 0.286f, 0.741f, 1f);
        [SerializeField] private Color inactiveTabColor = new Color(0.118f, 0.149f, 0.231f, 0.9f);

        private readonly List<TabBinding> _tabs = new List<TabBinding>();
        private readonly Dictionary<GameObject, string> _panelRegistryLookup = new Dictionary<GameObject, string>();
        private readonly Dictionary<string, GameObject> _registryPanelLookup =
            new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<GameObject, DockShortcutDragSource> _panelShortcutLookup =
            new Dictionary<GameObject, DockShortcutDragSource>();
        private readonly HashSet<string> _minimizedPanelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private GameObject _activePanel;
        private bool _hasPermissions = true;
        private static TMP_FontAsset _defaultFont;
        private DockShortcutSection _dockShortcutSection;

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
        }

        public void Unmount()
        {
            gameObject.SetActive(false);
        }

        public void OnAbilityStateChanged(string abilityId, bool enabled)
        {
            ApplyPermissions(enabled);
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
                    tab.Panel.SetActive(available && tab.Panel == _activePanel && !IsPanelMinimized(tab.Panel));
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
            LoadMinimizedPanels();

            if (_activePanel == null)
            {
                _activePanel = plotsPanel != null ? plotsPanel : terrainPanel ?? materialsPanel ?? blueprintsPanel ?? commissionPanel;
            }

            if (_activePanel != null && IsPanelMinimized(_activePanel))
            {
                _activePanel = null;
            }

            if (_activePanel == null)
            {
                SelectFirstAvailablePanel();
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
                RegisterPanel(panelReference, registryId);
            }
        }

        private void WireTabs()
        {
            _tabs.Clear();
            RegisterTab(plotsTabButton, plotsPanel, PlotsPanelRegistryId);
            RegisterTab(terrainTabButton, terrainPanel, TerrainPanelRegistryId);
            RegisterTab(materialsTabButton, materialsPanel, MaterialsPanelRegistryId);
            RegisterTab(blueprintsTabButton, blueprintsPanel, BlueprintsPanelRegistryId);
            RegisterTab(commissionTabButton, commissionPanel, CommissionsPanelRegistryId);
        }

        private void RegisterTab(Button button, GameObject panel, string registryId)
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
            var dockSource = BindDockShortcutSource(button, targetPanel, registryId);
            if (dockSource != null && panel != null)
            {
                _panelShortcutLookup[panel] = dockSource;
            }

            _tabs.Add(new TabBinding { Button = button, Panel = panel });
        }

        private DockShortcutDragSource BindDockShortcutSource(Button button, GameObject panel, string registryId)
        {
            if (button == null || panel == null || string.IsNullOrWhiteSpace(registryId))
            {
                return null;
            }

            var source = button.GetComponent<DockShortcutDragSource>();
            if (source == null)
            {
                source = button.gameObject.AddComponent<DockShortcutDragSource>();
            }

            var displayName = ResolveButtonLabel(button, panel);
            var icon = button.TryGetComponent(out Image image) ? image.sprite : null;
            var actionMetadata = new DockShortcutActionMetadata("arkitect.panel", registryId);
            var entry = new DockShortcutEntry(registryId, displayName, icon, actionMetadata);

            source.Configure(entry, () => ShowPanel(panel));
            return source;
        }

        private static string ResolveButtonLabel(Button button, GameObject panel)
        {
            if (button == null)
            {
                return panel != null ? panel.name : string.Empty;
            }

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && !string.IsNullOrWhiteSpace(label.text))
            {
                return label.text.Trim();
            }

            var legacyLabel = button.GetComponentInChildren<Text>(true);
            if (legacyLabel != null && !string.IsNullOrWhiteSpace(legacyLabel.text))
            {
                return legacyLabel.text.Trim();
            }

            return panel != null ? panel.name : button.name;
        }

        private void ShowPanel(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            RestorePanelIfMinimized(panel);

            foreach (var tab in _tabs)
            {
                var isActive = tab.Panel == panel;
                if (tab.Panel != null)
                {
                    tab.Panel.SetActive(isActive && !IsPanelMinimized(tab.Panel) && _hasPermissions);
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
                layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
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

        private void RegisterPanel(GameObject panel, string registryId)
        {
            if (panel == null || string.IsNullOrWhiteSpace(registryId))
            {
                return;
            }

            _panelRegistryLookup[panel] = registryId;
            _registryPanelLookup[registryId] = panel;
            EnsureMinimizeButton(panel.transform, panel, registryId);
        }

        private void EnsureMinimizeButton(Transform panelTransform, GameObject panel, string registryId)
        {
            if (panelTransform == null)
            {
                return;
            }

            var existing = panelTransform.Find("MinimizeButton");
            Button button;
            if (existing != null && existing.TryGetComponent(out Button existingButton))
            {
                button = existingButton;
            }
            else
            {
                var buttonObject = new GameObject("MinimizeButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(panelTransform, false);
                button = buttonObject.GetComponent<Button>();

                if (buttonObject.TryGetComponent(out Image image))
                {
                    image.color = new Color(0.298f, 0.349f, 0.482f, 0.9f);
                }

                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(buttonObject.transform, false);
                if (labelObject.TryGetComponent(out TMP_Text label))
                {
                    label.text = "–";
                    label.fontStyle = FontStyles.Bold;
                    label.fontSize = 28;
                    label.alignment = TextAlignmentOptions.Center;
                    label.color = new Color(0.898f, 0.768f, 1f, 1f);
                    label.font = label.font == null ? GetDefaultFontAsset() : label.font;
                }

                if (buttonObject.TryGetComponent(out RectTransform buttonRect))
                {
                    buttonRect.anchorMin = new Vector2(1f, 1f);
                    buttonRect.anchorMax = new Vector2(1f, 1f);
                    buttonRect.pivot = new Vector2(1f, 1f);
                    buttonRect.anchoredPosition = new Vector2(-24f, -24f);
                    buttonRect.sizeDelta = new Vector2(36f, 36f);
                }

                if (labelObject.TryGetComponent(out RectTransform labelRect))
                {
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = Vector2.zero;
                    labelRect.offsetMax = Vector2.zero;
                }
            }

            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => MinimizePanel(panel, registryId));
        }

        private void MinimizePanel(GameObject panel, string registryId)
        {
            if (panel == null || string.IsNullOrWhiteSpace(registryId))
            {
                return;
            }

            if (!_minimizedPanelIds.Add(registryId))
            {
                return;
            }

            SaveMinimizedPanels();
            panel.SetActive(false);
            AddDockShortcutForPanel(panel, registryId);

            if (_activePanel == panel)
            {
                _activePanel = null;
                SelectFirstAvailablePanel();
                if (_activePanel != null)
                {
                    ShowPanel(_activePanel);
                }
            }
        }

        private void RestorePanelIfMinimized(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            if (!TryGetRegistryId(panel, out var registryId))
            {
                return;
            }

            if (!_minimizedPanelIds.Remove(registryId))
            {
                return;
            }

            SaveMinimizedPanels();
            RemoveDockShortcut(registryId);
        }

        private bool TryGetRegistryId(GameObject panel, out string registryId)
        {
            registryId = null;
            if (panel == null)
            {
                return false;
            }

            return _panelRegistryLookup.TryGetValue(panel, out registryId);
        }

        private bool IsPanelMinimized(GameObject panel)
        {
            if (panel == null)
            {
                return false;
            }

            return TryGetRegistryId(panel, out var registryId) && _minimizedPanelIds.Contains(registryId);
        }

        private void SelectFirstAvailablePanel()
        {
            foreach (var tab in _tabs)
            {
                if (tab.Panel == null)
                {
                    continue;
                }

                if (!IsPanelMinimized(tab.Panel))
                {
                    _activePanel = tab.Panel;
                    return;
                }
            }
        }

        private void LoadMinimizedPanels()
        {
            _minimizedPanelIds.Clear();
            var minimized = ArkitectPanelMinimizedStore.GetMinimizedPanels();
            if (minimized != null)
            {
                foreach (var registryId in minimized)
                {
                    if (string.IsNullOrWhiteSpace(registryId))
                    {
                        continue;
                    }

                    _minimizedPanelIds.Add(registryId.Trim());
                }
            }

            foreach (var entry in _registryPanelLookup)
            {
                if (entry.Value == null)
                {
                    continue;
                }

                if (_minimizedPanelIds.Contains(entry.Key))
                {
                    entry.Value.SetActive(false);
                    AddDockShortcutForPanel(entry.Value, entry.Key);
                }
            }
        }

        private void SaveMinimizedPanels()
        {
            ArkitectPanelMinimizedStore.SaveMinimizedPanels(_minimizedPanelIds);
        }

        private void AddDockShortcutForPanel(GameObject panel, string registryId)
        {
            if (panel == null || string.IsNullOrWhiteSpace(registryId))
            {
                return;
            }

            var dockSection = GetDockShortcutSection();
            if (dockSection == null)
            {
                return;
            }

            if (!_panelShortcutLookup.TryGetValue(panel, out var source) || source == null)
            {
                return;
            }

            dockSection.AddShortcutFromSource(source);
        }

        private void RemoveDockShortcut(string registryId)
        {
            if (string.IsNullOrWhiteSpace(registryId))
            {
                return;
            }

            var dockSection = GetDockShortcutSection();
            dockSection?.RemoveShortcut(registryId);
        }

        private DockShortcutSection GetDockShortcutSection()
        {
            if (_dockShortcutSection == null)
            {
#if UNITY_2023_1_OR_NEWER
                _dockShortcutSection = UnityEngine.Object.FindFirstObjectByType<DockShortcutSection>(FindObjectsInactive.Include);
#else
                _dockShortcutSection = FindObjectOfType<DockShortcutSection>(true);
#endif
            }

            return _dockShortcutSection;
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
