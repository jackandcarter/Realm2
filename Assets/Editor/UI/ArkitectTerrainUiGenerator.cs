using Client.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Realm.Editor.UI
{
    public static class ArkitectTerrainUiGenerator
    {
        private const string MenuRoot = "Tools/Realm/UI";

        [MenuItem(MenuRoot + "/Generate Arkitect Terrain Tools UI", priority = 130)]
        public static void GenerateTerrainUi()
        {
            ArkitectUiFoundationGenerator.GenerateFoundation();

            var manager = Object.FindFirstObjectByType<ArkitectUIManager>();
            if (manager == null)
            {
                Debug.LogWarning("ArkitectTerrainUiGenerator could not locate an ArkitectUIManager in the scene.");
                return;
            }

            var terrainPanel = manager.transform.Find("Panels/TerrainPanel");
            if (terrainPanel == null)
            {
                Debug.LogWarning("ArkitectTerrainUiGenerator could not locate the TerrainPanel. Run the foundation generator first.");
                return;
            }

            var font = TMP_Settings.defaultFontAsset;
            BuildTerrainPanel(terrainPanel, font);
            Selection.activeGameObject = terrainPanel.gameObject;
        }

        private static void BuildTerrainPanel(Transform panelTransform, TMP_FontAsset font)
        {
            var layout = FindOrCreateChildRect(panelTransform, "TerrainLayout");
            ConfigureLayoutRoot(layout);

            var toolsColumn = FindOrCreateColumn(layout, "ToolsColumn", 220f);
            var infoColumn = FindOrCreateColumn(layout, "InfoColumn", 740f);

            var toolsSidebar = FindOrCreateToolsSidebar(toolsColumn);

            var toolsCard = FindOrCreateCard(infoColumn, "TerrainToolsCard", new Color(0.1f, 0.14f, 0.22f, 0.92f), 220f);
            EnsureCardText(toolsCard, "Title", "Tool Details", font, 20, FontStyles.Bold,
                new Color(0.9f, 0.82f, 1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -16f), new Vector2(-20f, -50f));
            EnsureCardText(toolsCard, "Body", "Select a tool on the left to review its context, requirements, and usage notes.",
                font, 15, FontStyles.Normal, new Color(0.72f, 0.82f, 0.95f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -52f), new Vector2(-20f, -86f));

            var toolPanelsRoot = FindOrCreateToolPanelsRoot(toolsCard);
            EnsureToolDetailPanel(toolPanelsRoot, "AddLandPanel", "Add Land", "Raise new terrain inside your plots with controlled elevation adjustments.", font);
            EnsureToolDetailPanel(toolPanelsRoot, "RemoveLandPanel", "Remove Land", "Lower terrain to carve channels, basins, and open spaces.", font);
            EnsureToolDetailPanel(toolPanelsRoot, "PlaceWaterPanel", "Place Water", "Set water tables and shorelines to create lakes and rivers.", font);
            EnsureToolDetailPanel(toolPanelsRoot, "FlattenPanel", "Flatten", "Level terrain to a target height for consistent building foundations.", font);
            EnsureToolDetailPanel(toolPanelsRoot, "SmoothPanel", "Smooth", "Blend sharp edges for natural transitions and erosion effects.", font);
            EnsureToolDetailPanel(toolPanelsRoot, "RaiseLowerPanel", "Raise/Lower", "Push and pull terrain for rapid elevation sculpting.", font);

            var overviewCard = FindOrCreateCard(infoColumn, "PlotRulesCard", new Color(0.12f, 0.16f, 0.24f, 0.9f), 130f);
            EnsureCardText(overviewCard, "Title", "Plot-Bound Terrain Crafting", font, 20, FontStyles.Bold,
                new Color(0.9f, 0.82f, 1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -16f), new Vector2(-20f, -50f));
            EnsureCardText(overviewCard, "Body",
                "Builders can only shape terrain inside plots they own or share. Outside plot borders remain untouched, protecting the world beyond your claimed land.",
                font, 16, FontStyles.Normal, new Color(0.78f, 0.86f, 0.98f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -52f), new Vector2(-20f, -106f));

            var blueprintCard = FindOrCreateCard(infoColumn, "BlueprintCard", new Color(0.11f, 0.15f, 0.23f, 0.9f), 130f);
            EnsureCardText(blueprintCard, "Title", "Blueprints & Plots", font, 20, FontStyles.Bold,
                new Color(0.9f, 0.82f, 1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -16f), new Vector2(-20f, -50f));
            EnsureCardText(blueprintCard, "Body",
                "Blueprints stamp completed structures into the world. Plots define the buildable zone, with permissions shared between trusted Builders.",
                font, 16, FontStyles.Normal, new Color(0.78f, 0.86f, 0.98f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -52f), new Vector2(-20f, -106f));

            var abilitiesCard = FindOrCreateCard(infoColumn, "TerrainAbilitiesCard", new Color(0.11f, 0.15f, 0.23f, 0.92f), 280f);
            EnsureCardText(abilitiesCard, "Title", "Builder Terrain Abilities", font, 20, FontStyles.Bold,
                new Color(0.9f, 0.82f, 1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -16f), new Vector2(-20f, -50f));
            EnsureCardText(abilitiesCard, "Body", "Unlocked abilities are ready to slot into the dock. Locked abilities list their unlock requirements.",
                font, 15, FontStyles.Normal, new Color(0.72f, 0.82f, 0.95f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -52f), new Vector2(-20f, -82f));
            EnsureAbilityList(abilitiesCard, font);

            EnsureToolButton(toolsSidebar, "AddLandTool", "Add Land", "Unlock: Terraweaving I", font, false);
            EnsureToolButton(toolsSidebar, "RemoveLandTool", "Remove Land", "Unlock: Terraweaving I", font, false);
            EnsureToolButton(toolsSidebar, "PlaceWaterTool", "Place Water", "Unlock: Hydromancy I", font, true);
            EnsureToolButton(toolsSidebar, "FlattenTool", "Flatten", "Unlock: Surveyor's Rite", font, false);
            EnsureToolButton(toolsSidebar, "SmoothTool", "Smooth", "Unlock: Geomancy II", font, true);
            EnsureToolButton(toolsSidebar, "RaiseLowerTool", "Raise/Lower", "Unlock: Geomancy I", font, true);

            EnsureToolPanelController(panelTransform, toolsSidebar, toolPanelsRoot);
        }

        private static RectTransform FindOrCreateChildRect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing as RectTransform;
            }

            var child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, "Create Arkitect Terrain UI Element");
            child.transform.SetParent(parent, false);
            ApplyUiLayer(child);
            return child.GetComponent<RectTransform>();
        }

        private static void ConfigureLayoutRoot(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -140f);
            rect.sizeDelta = new Vector2(980f, 0f);

            var layout = rect.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = Undo.AddComponent<HorizontalLayoutGroup>(rect.gameObject);
            }

            layout.spacing = 16f;
            layout.padding.top = 12;
            layout.padding.bottom = 12;
            layout.padding.left = 12;
            layout.padding.right = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            var fitter = rect.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = Undo.AddComponent<ContentSizeFitter>(rect.gameObject);
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static RectTransform FindOrCreateColumn(RectTransform parent, string name, float preferredWidth)
        {
            var existing = parent.Find(name);
            if (existing == null)
            {
                var column = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
                Undo.RegisterCreatedObjectUndo(column, "Create Arkitect Terrain Column");
                column.transform.SetParent(parent, false);
                ApplyUiLayer(column);
                existing = column.transform;
            }

            if (existing.TryGetComponent(out LayoutElement layout))
            {
                layout.preferredWidth = preferredWidth;
                layout.flexibleWidth = 0f;
            }

            if (existing.TryGetComponent(out VerticalLayoutGroup group))
            {
                group.spacing = 16f;
                group.padding.top = 0;
                group.padding.bottom = 0;
                group.padding.left = 0;
                group.padding.right = 0;
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandHeight = false;
                group.childForceExpandWidth = true;
            }

            return existing as RectTransform;
        }

        private static RectTransform FindOrCreateCard(RectTransform parent, string name, Color color, float preferredHeight)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                EnsureCardVisuals(existing, color, preferredHeight);
                return existing as RectTransform;
            }

            var card = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(card, "Create Arkitect Terrain Card");
            card.transform.SetParent(parent, false);
            ApplyUiLayer(card);

            EnsureCardVisuals(card.transform, color, preferredHeight);
            return card.GetComponent<RectTransform>();
        }

        private static RectTransform FindOrCreateToolsSidebar(RectTransform parent)
        {
            var existing = parent.Find("ToolsSidebar");
            if (existing == null)
            {
                var sidebar = new GameObject("ToolsSidebar", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
                Undo.RegisterCreatedObjectUndo(sidebar, "Create Arkitect Terrain Sidebar");
                sidebar.transform.SetParent(parent, false);
                ApplyUiLayer(sidebar);
                existing = sidebar.transform;
            }

            if (existing.TryGetComponent(out LayoutElement layout))
            {
                layout.preferredWidth = 220f;
                layout.flexibleWidth = 0f;
            }

            if (existing.TryGetComponent(out VerticalLayoutGroup group))
            {
                group.spacing = 12f;
                group.padding.top = 12;
                group.padding.bottom = 12;
                group.padding.left = 0;
                group.padding.right = 0;
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandHeight = false;
                group.childForceExpandWidth = true;
            }

            return existing as RectTransform;
        }

        private static RectTransform FindOrCreateToolPanelsRoot(RectTransform card)
        {
            var existing = card.Find("ToolDetailPanels");
            if (existing == null)
            {
                var root = new GameObject("ToolDetailPanels", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                Undo.RegisterCreatedObjectUndo(root, "Create Arkitect Terrain Tool Panels");
                root.transform.SetParent(card, false);
                ApplyUiLayer(root);
                existing = root.transform;
            }

            var rect = existing.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(20f, 18f);
            rect.offsetMax = new Vector2(-20f, -90f);

            if (existing.TryGetComponent(out VerticalLayoutGroup group))
            {
                group.spacing = 12f;
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandHeight = false;
                group.childForceExpandWidth = true;
            }

            if (existing.TryGetComponent(out ContentSizeFitter fitter))
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            return existing as RectTransform;
        }

        private static void EnsureToolDetailPanel(RectTransform parent, string name, string title, string description, TMP_FontAsset font)
        {
            if (parent == null)
            {
                return;
            }

            var panel = parent.Find(name);
            if (panel == null)
            {
                var panelObject = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
                Undo.RegisterCreatedObjectUndo(panelObject, "Create Arkitect Terrain Tool Panel");
                panelObject.transform.SetParent(parent, false);
                ApplyUiLayer(panelObject);
                panel = panelObject.transform;
            }

            if (panel.TryGetComponent(out LayoutElement layout))
            {
                layout.preferredHeight = 120f;
            }

            EnsureAbilityEntryLabel(panel, "Title", title, font, 16, FontStyles.Bold,
                new Color(0.9f, 0.92f, 1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -6f), new Vector2(0f, -30f));
            EnsureAbilityEntryLabel(panel, "Body", description, font, 13, FontStyles.Normal,
                new Color(0.72f, 0.82f, 0.95f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -32f), new Vector2(0f, -72f));

            if (panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(false);
            }
        }

        private static void EnsureCardVisuals(Transform cardTransform, Color color, float preferredHeight)
        {
            if (cardTransform == null)
            {
                return;
            }

            if (cardTransform.TryGetComponent(out Image image))
            {
                image.color = color;
            }

            if (cardTransform.TryGetComponent(out LayoutElement layout))
            {
                layout.preferredHeight = preferredHeight;
            }
        }

        private static void EnsureCardText(RectTransform card, string name, string text, TMP_FontAsset font, int fontSize, FontStyles style, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var label = card.Find(name);
            if (label == null)
            {
                var labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(labelObject, "Create Arkitect Terrain Card Label");
                labelObject.transform.SetParent(card, false);
                ApplyUiLayer(labelObject);
                label = labelObject.transform;
            }

            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            if (label.TryGetComponent(out TextMeshProUGUI textComponent))
            {
                textComponent.text = text;
                textComponent.font = font != null ? font : textComponent.font;
                textComponent.fontSize = fontSize;
                textComponent.fontStyle = style;
                textComponent.color = color;
                textComponent.alignment = TextAlignmentOptions.TopLeft;
            }
        }

        private static void EnsureToolButton(RectTransform parent, string name, string label, string unlockText, TMP_FontAsset font, bool locked)
        {
            var existing = parent.Find(name);
            if (existing == null)
            {
                var button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
                Undo.RegisterCreatedObjectUndo(button, "Create Arkitect Terrain Tool Button");
                button.transform.SetParent(parent, false);
                ApplyUiLayer(button);
                existing = button.transform;
            }

            if (existing.TryGetComponent(out Image image))
            {
                image.color = locked ? new Color(0.12f, 0.15f, 0.22f, 0.9f) : new Color(0.16f, 0.22f, 0.32f, 0.95f);
            }

            if (existing.TryGetComponent(out LayoutElement layout))
            {
                layout.preferredHeight = 72f;
                layout.preferredWidth = 220f;
            }

            EnsureToolLabel(existing, "ToolName", label, font, 16, FontStyles.Bold, new Color(0.88f, 0.92f, 1f, 1f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -10f), new Vector2(-16f, -36f));
            EnsureToolLabel(existing, "UnlockLabel", unlockText, font, 11, FontStyles.Normal, new Color(0.7f, 0.78f, 0.9f, 1f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -36f), new Vector2(-16f, -62f));

            var badgeText = locked ? "Locked" : "Unlocked";
            var badgeColor = locked ? new Color(0.82f, 0.45f, 0.57f, 1f) : new Color(0.46f, 0.86f, 0.65f, 1f);
            EnsureToolLabel(existing, "StatusBadge", badgeText, font, 11, FontStyles.Bold, badgeColor,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-88f, -6f), new Vector2(-12f, -30f), TextAlignmentOptions.MidlineRight);
        }

        private static void EnsureToolLabel(Transform parent, string name, string text, TMP_FontAsset font, int fontSize, FontStyles style, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var label = parent.Find(name);
            if (label == null)
            {
                var labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(labelObject, "Create Arkitect Terrain Tool Label");
                labelObject.transform.SetParent(parent, false);
                ApplyUiLayer(labelObject);
                label = labelObject.transform;
            }

            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            if (label.TryGetComponent(out TextMeshProUGUI textComponent))
            {
                textComponent.text = text;
                textComponent.font = font != null ? font : textComponent.font;
                textComponent.fontSize = fontSize;
                textComponent.fontStyle = style;
                textComponent.color = color;
                textComponent.alignment = alignment;
            }
        }

        private static void EnsureAbilityList(RectTransform card, TMP_FontAsset font)
        {
            var listTransform = card.Find("AbilityList");
            if (listTransform == null)
            {
                var listObject = new GameObject("AbilityList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                Undo.RegisterCreatedObjectUndo(listObject, "Create Arkitect Terrain Ability List");
                listObject.transform.SetParent(card, false);
                ApplyUiLayer(listObject);
                listTransform = listObject.transform;
            }

            var listRect = listTransform.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 0f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.offsetMin = new Vector2(20f, 20f);
            listRect.offsetMax = new Vector2(-20f, -90f);

            if (listTransform.TryGetComponent(out VerticalLayoutGroup group))
            {
                group.spacing = 10f;
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandHeight = false;
                group.childForceExpandWidth = true;
            }

            if (listTransform.TryGetComponent(out ContentSizeFitter fitter))
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            var template = listTransform.Find("AbilityEntryTemplate");
            if (template == null)
            {
                var templateObject = new GameObject("AbilityEntryTemplate", typeof(RectTransform), typeof(LayoutElement));
                Undo.RegisterCreatedObjectUndo(templateObject, "Create Arkitect Terrain Ability Entry");
                templateObject.transform.SetParent(listTransform, false);
                ApplyUiLayer(templateObject);
                template = templateObject.transform;
            }

            if (template.TryGetComponent(out LayoutElement templateLayout))
            {
                templateLayout.preferredHeight = 72f;
                templateLayout.minHeight = 64f;
            }

            EnsureAbilityEntryLabel(template, "AbilityName", "Ability Name", font, 16, FontStyles.Bold,
                new Color(0.9f, 0.92f, 1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -6f), new Vector2(0f, -28f));
            EnsureAbilityEntryLabel(template, "AbilityDescription", "Ability description text goes here.", font, 13, FontStyles.Normal,
                new Color(0.72f, 0.82f, 0.95f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -30f), new Vector2(0f, -56f));
            EnsureAbilityEntryLabel(template, "AbilityUnlock", "Unlock: Level 3", font, 12, FontStyles.Italic,
                new Color(0.6f, 0.74f, 0.9f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 6f), new Vector2(0f, 24f), TextAlignmentOptions.BottomLeft);

            var templateObjectActive = template.gameObject;
            if (templateObjectActive.activeSelf)
            {
                templateObjectActive.SetActive(false);
            }

            if (!card.TryGetComponent(out ArkitectTerrainAbilityListPanel _))
            {
                Undo.AddComponent<ArkitectTerrainAbilityListPanel>(card.gameObject);
            }
        }

        private static void EnsureToolPanelController(Transform panelTransform, RectTransform toolButtonRoot, RectTransform toolPanelRoot)
        {
            if (panelTransform == null)
            {
                return;
            }

            if (!panelTransform.TryGetComponent(out ArkitectTerrainToolPanelController controller))
            {
                controller = Undo.AddComponent<ArkitectTerrainToolPanelController>(panelTransform.gameObject);
            }

            controller.Configure(toolButtonRoot, toolPanelRoot);
        }

        private static void EnsureAbilityEntryLabel(Transform parent, string name, string text, TMP_FontAsset font, int fontSize, FontStyles style, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var label = parent.Find(name);
            if (label == null)
            {
                var labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(labelObject, "Create Arkitect Terrain Ability Label");
                labelObject.transform.SetParent(parent, false);
                ApplyUiLayer(labelObject);
                label = labelObject.transform;
            }

            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            if (label.TryGetComponent(out TextMeshProUGUI textComponent))
            {
                textComponent.text = text;
                textComponent.font = font != null ? font : textComponent.font;
                textComponent.fontSize = fontSize;
                textComponent.fontStyle = style;
                textComponent.color = color;
                textComponent.alignment = alignment;
            }
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
