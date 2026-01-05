using System.IO;
using Client.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Realm.Editor.UI
{
    public static class ArkitectUiFoundationGenerator
    {
        private const string PrefabPath = "Assets/UI/Arkitect/ArkitectCanvas.prefab";
        private const string MenuRoot = "Tools/Realm/UI";

        [MenuItem(MenuRoot + "/Generate Arkitect UI Foundation", priority = 120)]
        public static void GenerateFoundation()
        {
            var manager = FindOrCreateManager();
            var font = LoadDefaultFontAsset();

            EnsureEventSystem();

            var root = manager.transform;
            var panelsContainer = FindOrCreateChildRect(root, "Panels");
            var tabBar = FindOrCreateChildRect(root, "TabBar");

            ConfigurePanelsContainer(panelsContainer);
            ConfigureTabBar(tabBar);

            var plotsPanel = FindOrCreatePanel(panelsContainer, "PlotsPanel", "Plots", "Claim frontier plots and grow settlements to embody Realm's player-driven worldbuilding.", font);
            var terrainPanel = FindOrCreatePanel(panelsContainer, "TerrainPanel", "Terrain", "Shape the land within your plots using Arkitect magicks and unlock new terrain rituals.", font);
            var materialsPanel = FindOrCreatePanel(panelsContainer, "MaterialsPanel", "Materials", "Curate arcane reagents and technomantic alloys that empower construction and crafting.", font);
            var blueprintsPanel = FindOrCreatePanel(panelsContainer, "BlueprintsPanel", "Blueprints", "Unlock radiant designs that blend crystalline spires with luminous machinery.", font);
            var commissionsPanel = FindOrCreatePanel(panelsContainer, "CommissionsPanel", "Commissions", "Review community commissions and collaborate on magitech megastructures for Elysium.", font);

            var plotsTabButton = FindOrCreateTabButton(tabBar, "PlotsTab", "Plots", font);
            var terrainTabButton = FindOrCreateTabButton(tabBar, "TerrainTab", "Terrain", font);
            var materialsTabButton = FindOrCreateTabButton(tabBar, "MaterialsTab", "Materials", font);
            var blueprintsTabButton = FindOrCreateTabButton(tabBar, "BlueprintsTab", "Blueprints", font);
            var commissionsTabButton = FindOrCreateTabButton(tabBar, "CommissionsTab", "Commissions", font);

            ApplyManagerBindings(manager, panelsContainer, tabBar, plotsTabButton, terrainTabButton, materialsTabButton, blueprintsTabButton, commissionsTabButton, plotsPanel, terrainPanel, materialsPanel, blueprintsPanel, commissionsPanel);

            SavePrefab(manager.gameObject);
            Selection.activeGameObject = manager.gameObject;
        }

        private static ArkitectUIManager FindOrCreateManager()
        {
            var existing = Object.FindFirstObjectByType<ArkitectUIManager>();
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("ArkitectCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ArkitectUIManager));
            Undo.RegisterCreatedObjectUndo(root, "Create Arkitect UI Foundation");
            ApplyUiLayer(root);
            return root.GetComponent<ArkitectUIManager>();
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                return;
            }

            var system = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(system, "Create EventSystem");
        }

        private static RectTransform FindOrCreateChildRect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing as RectTransform;
            }

            var child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, "Create Arkitect UI Element");
            child.transform.SetParent(parent, false);
            ApplyUiLayer(child);
            return child.GetComponent<RectTransform>();
        }

        private static Button FindOrCreateTabButton(RectTransform parent, string name, string label, TMP_FontAsset font)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Button existingButton))
            {
                EnsureButtonLabel(existingButton, label, font);
                return existingButton;
            }

            var button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(button, "Create Arkitect Tab Button");
            button.transform.SetParent(parent, false);
            ApplyUiLayer(button);

            var image = button.GetComponent<Image>();
            image.color = new Color(0.118f, 0.149f, 0.231f, 0.9f);

            var layout = button.GetComponent<LayoutElement>();
            layout.preferredWidth = 180f;
            layout.preferredHeight = 48f;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(button.transform, false);
            ApplyUiLayer(labelObject);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            EnsureButtonLabel(button.GetComponent<Button>(), label, font);
            return button.GetComponent<Button>();
        }

        private static GameObject FindOrCreatePanel(RectTransform parent, string name, string title, string description, TMP_FontAsset font)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                EnsurePanelText(existing, title, description, font);
                return existing.gameObject;
            }

            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(panel, "Create Arkitect Panel");
            panel.transform.SetParent(parent, false);
            ApplyUiLayer(panel);

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.109f, 0.137f, 0.211f, 0.88f);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(24f, 24f);
            panelRect.offsetMax = new Vector2(-24f, -24f);

            CreatePanelLabel(panel.transform, "Title", title, font, 28, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -24f), new Vector2(0f, -64f));
            CreatePanelLabel(panel.transform, "Description", description, font, 20, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -80f), new Vector2(0f, -220f));

            return panel;
        }

        private static void CreatePanelLabel(Transform parent, string name, string text, TMP_FontAsset font, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(label, "Create Arkitect Panel Label");
            label.transform.SetParent(parent, false);
            ApplyUiLayer(label);

            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var textComponent = label.GetComponent<TextMeshProUGUI>();
            textComponent.text = text;
            if (font != null)
            {
                textComponent.font = font;
            }

            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAlignmentOptions.TopLeft;
            textComponent.color = new Color(0.839f, 0.925f, 0.992f, 1f);
        }

        private static void EnsurePanelText(Transform panel, string title, string description, TMP_FontAsset font)
        {
            var titleTransform = panel.Find("Title");
            if (titleTransform != null && titleTransform.TryGetComponent(out TextMeshProUGUI titleText))
            {
                if (string.IsNullOrWhiteSpace(titleText.text))
                {
                    titleText.text = title;
                }

                if (font != null)
                {
                    titleText.font = font;
                }

                titleText.fontSize = 28;
                titleText.alignment = TextAlignmentOptions.TopLeft;
                titleText.color = new Color(0.898f, 0.768f, 1f, 1f);
                titleText.fontStyle = FontStyles.Bold;
            }

            var descriptionTransform = panel.Find("Description");
            if (descriptionTransform != null && descriptionTransform.TryGetComponent(out TextMeshProUGUI descriptionText))
            {
                if (string.IsNullOrWhiteSpace(descriptionText.text))
                {
                    descriptionText.text = description;
                }

                if (font != null)
                {
                    descriptionText.font = font;
                }

                descriptionText.fontSize = 20;
                descriptionText.alignment = TextAlignmentOptions.TopLeft;
                descriptionText.color = new Color(0.788f, 0.862f, 0.976f, 1f);
                descriptionText.fontStyle = FontStyles.Normal;
            }
        }

        private static void EnsureButtonLabel(Button button, string label, TMP_FontAsset font)
        {
            if (button == null)
            {
                return;
            }

            var labelTransform = button.transform.Find("Label");
            if (labelTransform == null)
            {
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(labelObject, "Create Arkitect Button Label");
                labelObject.transform.SetParent(button.transform, false);
                ApplyUiLayer(labelObject);
                labelTransform = labelObject.transform;

                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            if (!labelTransform.TryGetComponent(out TextMeshProUGUI text))
            {
                return;
            }

            text.text = label;
            if (font != null)
            {
                text.font = font;
            }

            text.fontStyle = FontStyles.Bold;
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.839f, 0.925f, 0.992f, 1f);
        }

        private static void ConfigureTabBar(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -30f);
            rect.sizeDelta = new Vector2(0f, 90f);

            var layout = rect.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = Undo.AddComponent<HorizontalLayoutGroup>(rect.gameObject);
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

        private static void ConfigurePanelsContainer(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -30f);
            rect.sizeDelta = new Vector2(0f, -120f);
        }

        private static void ApplyManagerBindings(ArkitectUIManager manager, RectTransform panels, RectTransform tabBar, Button plotsTab, Button terrainTab, Button materialsTab, Button blueprintsTab, Button commissionsTab, GameObject plotsPanel, GameObject terrainPanel, GameObject materialsPanel, GameObject blueprintsPanel, GameObject commissionsPanel)
        {
            var serialized = new SerializedObject(manager);
            serialized.FindProperty("panelsContainer").objectReferenceValue = panels;
            serialized.FindProperty("tabContainer").objectReferenceValue = tabBar;
            serialized.FindProperty("plotsTabButton").objectReferenceValue = plotsTab;
            serialized.FindProperty("terrainTabButton").objectReferenceValue = terrainTab;
            serialized.FindProperty("materialsTabButton").objectReferenceValue = materialsTab;
            serialized.FindProperty("blueprintsTabButton").objectReferenceValue = blueprintsTab;
            serialized.FindProperty("commissionTabButton").objectReferenceValue = commissionsTab;
            serialized.FindProperty("plotsPanel").objectReferenceValue = plotsPanel;
            serialized.FindProperty("terrainPanel").objectReferenceValue = terrainPanel;
            serialized.FindProperty("materialsPanel").objectReferenceValue = materialsPanel;
            serialized.FindProperty("blueprintsPanel").objectReferenceValue = blueprintsPanel;
            serialized.FindProperty("commissionPanel").objectReferenceValue = commissionsPanel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SavePrefab(GameObject root)
        {
            var directory = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        }

        private static TMP_FontAsset LoadDefaultFontAsset()
        {
            return TMP_Settings.defaultFontAsset;
        }

        private static void ApplyUiLayer(GameObject target)
        {
            var uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                target.layer = uiLayer;
            }
        }
    }
}
