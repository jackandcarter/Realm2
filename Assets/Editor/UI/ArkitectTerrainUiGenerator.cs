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

            var overviewCard = FindOrCreateCard(layout, "PlotRulesCard", new Color(0.12f, 0.16f, 0.24f, 0.9f), 140f);
            EnsureCardText(overviewCard, "Title", "Plot-Bound Terrain Crafting", font, 22, FontStyles.Bold,
                new Color(0.9f, 0.82f, 1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -16f), new Vector2(-20f, -50f));
            EnsureCardText(overviewCard, "Body",
                "Builders can only shape terrain inside plots they own or share. Outside plot borders remain untouched, protecting the world beyond your claimed land.",
                font, 18, FontStyles.Normal, new Color(0.78f, 0.86f, 0.98f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -54f), new Vector2(-20f, -120f));

            var blueprintCard = FindOrCreateCard(layout, "BlueprintCard", new Color(0.11f, 0.15f, 0.23f, 0.9f), 140f);
            EnsureCardText(blueprintCard, "Title", "Blueprints & Plots", font, 22, FontStyles.Bold,
                new Color(0.9f, 0.82f, 1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -16f), new Vector2(-20f, -50f));
            EnsureCardText(blueprintCard, "Body",
                "Blueprints stamp completed structures into the world. Plots define the buildable zone, with permissions shared between trusted Builders.",
                font, 18, FontStyles.Normal, new Color(0.78f, 0.86f, 0.98f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -54f), new Vector2(-20f, -120f));

            var toolsCard = FindOrCreateCard(layout, "TerrainToolsCard", new Color(0.1f, 0.14f, 0.22f, 0.92f), 420f);
            EnsureCardText(toolsCard, "Title", "Terrain Tools", font, 22, FontStyles.Bold,
                new Color(0.9f, 0.82f, 1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -16f), new Vector2(-20f, -50f));
            EnsureCardText(toolsCard, "Body", "Unlock Arkitect rituals to sculpt land, water, and elevation with precision.",
                font, 18, FontStyles.Normal, new Color(0.72f, 0.82f, 0.95f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -54f), new Vector2(-20f, -90f));

            var toolsGrid = FindOrCreateToolsGrid(toolsCard);
            EnsureToolButton(toolsGrid, "AddLandTool", "Add Land", "Unlock: Terraweaving I", font, false);
            EnsureToolButton(toolsGrid, "RemoveLandTool", "Remove Land", "Unlock: Terraweaving I", font, false);
            EnsureToolButton(toolsGrid, "PlaceWaterTool", "Place Water", "Unlock: Hydromancy I", font, true);
            EnsureToolButton(toolsGrid, "FlattenTool", "Flatten", "Unlock: Surveyor's Rite", font, false);
            EnsureToolButton(toolsGrid, "SmoothTool", "Smooth", "Unlock: Geomancy II", font, true);
            EnsureToolButton(toolsGrid, "RaiseLowerTool", "Raise/Lower", "Unlock: Geomancy I", font, true);
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
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(24f, 24f);
            rect.offsetMax = new Vector2(-24f, -240f);

            var layout = rect.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = Undo.AddComponent<VerticalLayoutGroup>(rect.gameObject);
            }

            layout.spacing = 18f;
            layout.padding.top = 8;
            layout.padding.bottom = 8;
            layout.padding.left = 8;
            layout.padding.right = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
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

        private static RectTransform FindOrCreateToolsGrid(RectTransform parent)
        {
            var existing = parent.Find("ToolsGrid");
            if (existing != null)
            {
                EnsureToolsGridLayout(existing as RectTransform);
                return existing as RectTransform;
            }

            var grid = new GameObject("ToolsGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(grid, "Create Arkitect Terrain Tools Grid");
            grid.transform.SetParent(parent, false);
            ApplyUiLayer(grid);
            EnsureToolsGridLayout(grid.GetComponent<RectTransform>());
            return grid.GetComponent<RectTransform>();
        }

        private static void EnsureToolsGridLayout(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(16f, 24f);
            rect.offsetMax = new Vector2(-16f, -24f);

            if (rect.TryGetComponent(out LayoutElement layoutElement))
            {
                layoutElement.preferredHeight = 280f;
            }

            if (rect.TryGetComponent(out GridLayoutGroup grid))
            {
                grid.cellSize = new Vector2(240f, 96f);
                grid.spacing = new Vector2(16f, 16f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2;
                grid.childAlignment = TextAnchor.UpperLeft;
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
                layout.preferredHeight = 96f;
                layout.preferredWidth = 240f;
            }

            EnsureToolLabel(existing, "ToolName", label, font, 18, FontStyles.Bold, new Color(0.88f, 0.92f, 1f, 1f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -12f), new Vector2(-16f, -44f));
            EnsureToolLabel(existing, "UnlockLabel", unlockText, font, 14, FontStyles.Normal, new Color(0.7f, 0.78f, 0.9f, 1f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -46f), new Vector2(-16f, -76f));

            var badgeText = locked ? "Locked" : "Unlocked";
            var badgeColor = locked ? new Color(0.82f, 0.45f, 0.57f, 1f) : new Color(0.46f, 0.86f, 0.65f, 1f);
            EnsureToolLabel(existing, "StatusBadge", badgeText, font, 12, FontStyles.Bold, badgeColor,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-88f, -8f), new Vector2(-12f, -32f), TextAlignmentOptions.MidlineRight);
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
