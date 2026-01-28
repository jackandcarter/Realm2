using System.IO;
using Client.CharacterCreation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Realm.Editor.UI
{
    public static class CharacterCreationUiGenerator
    {
        private const string PrefabPath = "Assets/UI/CharacterCreation/CharacterCreationPanel.prefab";
        private const string MenuRoot = "Tools/Realm/UI";

        [MenuItem(MenuRoot + "/Generate Character Creation UI", priority = 110)]
        public static void GenerateCharacterCreationPanel()
        {
            var root = new GameObject(
                "CharacterCreationPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter),
                typeof(CharacterCreationPanel)
            );
            Undo.RegisterCreatedObjectUndo(root, "Create Character Creation Panel");
            ApplyUiLayer(root);

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(960f, 720f);
            rect.anchoredPosition = Vector2.zero;

            var image = root.GetComponent<Image>();
            image.color = new Color(0.05f, 0.06f, 0.1f, 0.96f);

            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 32, 32);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = root.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var font = TMP_Settings.defaultFontAsset;

            var header = CreateText(root.transform as RectTransform, "Header", "Character Creation", font, 30, FontStyles.Bold);
            header.alignment = TextAlignmentOptions.Center;

            var raceSection = CreateSection(root.transform as RectTransform, "RaceSection", "Select Race", font);
            var raceListRoot = CreateListRoot(raceSection, "RaceListRoot");
            var raceButtonTemplate = CreateButton(raceListRoot, "RaceButtonTemplate", "Race Name", font);
            raceButtonTemplate.gameObject.SetActive(false);

            var previewSection = CreateSection(root.transform as RectTransform, "PreviewSection", "Preview", font);
            var previewTitle = CreateText(previewSection, "PreviewTitle", "", font, 20, FontStyles.Bold);
            var previewSummary = CreateText(previewSection, "PreviewSummary", "", font, 16, FontStyles.Normal);
            previewSummary.enableWordWrapping = true;
            previewSummary.alignment = TextAlignmentOptions.TopLeft;
            var previewRoot = CreateMount(previewSection, "PreviewRoot");
            previewRoot.sizeDelta = new Vector2(0f, 240f);

            var classSection = CreateSection(root.transform as RectTransform, "ClassSection", "Select Class", font);
            var classListRoot = CreateListRoot(classSection, "ClassListRoot");
            var classButtonTemplate = CreateButton(classListRoot, "ClassButtonTemplate", "Class Name", font);
            classButtonTemplate.gameObject.SetActive(false);
            var classSummaryLabel = CreateText(classSection, "ClassSummaryLabel", "", font, 14, FontStyles.Normal);
            classSummaryLabel.enableWordWrapping = true;
            classSummaryLabel.alignment = TextAlignmentOptions.TopLeft;

            var heightRow = CreateSliderRow(root.transform as RectTransform, "Height", font, out var heightSlider, out var heightValueLabel);
            var buildRow = CreateSliderRow(root.transform as RectTransform, "Build", font, out var buildSlider, out var buildValueLabel);

            var featureSection = CreateSection(root.transform as RectTransform, "FeatureSection", "Features", font);
            var featureListRoot = CreateListRoot(featureSection, "FeatureListRoot");
            var featureEntryTemplate = CreateFeatureEntryTemplate(featureListRoot, font);
            featureEntryTemplate.gameObject.SetActive(false);

            var actionRow = new GameObject("ActionRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(actionRow, "Create Action Row");
            actionRow.transform.SetParent(root.transform, false);
            ApplyUiLayer(actionRow);
            var actionLayout = actionRow.GetComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 12f;
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childControlWidth = true;
            actionLayout.childForceExpandWidth = true;

            var confirmButton = CreateButton(actionRow.transform as RectTransform, "ConfirmButton", "Confirm", font);
            var cancelButton = CreateButton(actionRow.transform as RectTransform, "CancelButton", "Cancel", font);

            var serialized = new SerializedObject(root.GetComponent<CharacterCreationPanel>());
            serialized.FindProperty("raceListRoot").objectReferenceValue = raceListRoot;
            serialized.FindProperty("raceButtonTemplate").objectReferenceValue = raceButtonTemplate;
            serialized.FindProperty("previewRoot").objectReferenceValue = previewRoot;
            serialized.FindProperty("previewTitle").objectReferenceValue = previewTitle;
            serialized.FindProperty("previewSummary").objectReferenceValue = previewSummary;
            serialized.FindProperty("classListRoot").objectReferenceValue = classListRoot;
            serialized.FindProperty("classButtonTemplate").objectReferenceValue = classButtonTemplate;
            serialized.FindProperty("classSummaryLabel").objectReferenceValue = classSummaryLabel;
            serialized.FindProperty("heightSlider").objectReferenceValue = heightSlider;
            serialized.FindProperty("heightValueLabel").objectReferenceValue = heightValueLabel;
            serialized.FindProperty("buildSlider").objectReferenceValue = buildSlider;
            serialized.FindProperty("buildValueLabel").objectReferenceValue = buildValueLabel;
            serialized.FindProperty("featureListRoot").objectReferenceValue = featureListRoot;
            serialized.FindProperty("featureOptionTemplate").objectReferenceValue = featureEntryTemplate;
            serialized.FindProperty("confirmButton").objectReferenceValue = confirmButton;
            serialized.FindProperty("cancelButton").objectReferenceValue = cancelButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root);
            Selection.activeGameObject = root;
        }

        private static RectTransform CreateSection(RectTransform parent, string name, string header, TMP_FontAsset font)
        {
            var section = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(section, "Create Character Creation Section");
            section.transform.SetParent(parent, false);
            ApplyUiLayer(section);

            var layout = section.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;

            var label = CreateText(section.transform as RectTransform, name + "Header", header, font, 20, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Left;

            return section.GetComponent<RectTransform>();
        }

        private static RectTransform CreateListRoot(RectTransform parent, string name)
        {
            var listObject = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(listObject, "Create Character Creation List Root");
            listObject.transform.SetParent(parent, false);
            ApplyUiLayer(listObject);

            var layout = listObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var fitter = listObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return listObject.GetComponent<RectTransform>();
        }

        private static TMP_Text CreateText(RectTransform parent, string name, string text, TMP_FontAsset font, int fontSize, FontStyles style)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(label, "Create Character Creation Text");
            label.transform.SetParent(parent, false);
            ApplyUiLayer(label);

            var textComponent = label.GetComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = style;
            textComponent.color = new Color(0.9f, 0.92f, 0.98f, 1f);
            if (font != null)
            {
                textComponent.font = font;
            }

            var layout = label.GetComponent<LayoutElement>();
            layout.preferredHeight = fontSize + 12f;

            return textComponent;
        }

        private static Button CreateButton(RectTransform parent, string name, string label, TMP_FontAsset font)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(buttonObject, "Create Character Creation Button");
            buttonObject.transform.SetParent(parent, false);
            ApplyUiLayer(buttonObject);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 40f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.28f, 1f);

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 40f;

            var text = CreateText(buttonObject.transform as RectTransform, "Label", label, font, 16, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            return buttonObject.GetComponent<Button>();
        }

        private static RectTransform CreateMount(RectTransform parent, string name)
        {
            var mount = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(mount, "Create Character Creation Mount");
            mount.transform.SetParent(parent, false);
            ApplyUiLayer(mount);
            var rect = mount.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(0f, 200f);
            return rect;
        }

        private static RectTransform CreateSliderRow(RectTransform parent, string label, TMP_FontAsset font, out Slider slider, out TMP_Text valueLabel)
        {
            var row = new GameObject($"{label}Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(row, "Create Character Creation Slider Row");
            row.transform.SetParent(parent, false);
            ApplyUiLayer(row);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            var labelText = CreateText(row.transform as RectTransform, $"{label}Label", label, font, 16, FontStyles.Bold);
            labelText.alignment = TextAlignmentOptions.Left;

            slider = CreateSlider(row.transform as RectTransform, $"{label}Slider");
            valueLabel = CreateText(row.transform as RectTransform, $"{label}Value", "0.00", font, 14, FontStyles.Normal);
            valueLabel.alignment = TextAlignmentOptions.Right;

            return row.GetComponent<RectTransform>();
        }

        private static Slider CreateSlider(RectTransform parent, string name)
        {
            var sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(sliderObject, "Create Character Creation Slider");
            sliderObject.transform.SetParent(parent, false);
            ApplyUiLayer(sliderObject);

            var layout = sliderObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 24f;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sliderObject.transform, false);
            ApplyUiLayer(background);
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.12f, 0.14f, 0.18f, 0.9f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            ApplyUiLayer(fillArea);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(10f, 0f);
            fillAreaRect.offsetMax = new Vector2(-10f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            ApplyUiLayer(fill);
            var fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.35f, 0.7f, 1f, 1f);

            var handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleSlideArea.transform.SetParent(sliderObject.transform, false);
            ApplyUiLayer(handleSlideArea);
            var handleAreaRect = handleSlideArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleSlideArea.transform, false);
            ApplyUiLayer(handle);
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(0.9f, 0.92f, 0.98f, 1f);

            var slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        private static CharacterCreationFeatureEntry CreateFeatureEntryTemplate(RectTransform parent, TMP_FontAsset font)
        {
            var entry = new GameObject("FeatureEntryTemplate", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(CharacterCreationFeatureEntry));
            Undo.RegisterCreatedObjectUndo(entry, "Create Feature Entry Template");
            entry.transform.SetParent(parent, false);
            ApplyUiLayer(entry);

            var layout = entry.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var label = CreateText(entry.transform as RectTransform, "FeatureLabel", "Feature", font, 16, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Left;

            var dropdown = CreateDropdown(entry.transform as RectTransform, "FeatureDropdown", font);

            var serialized = new SerializedObject(entry.GetComponent<CharacterCreationFeatureEntry>());
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.FindProperty("dropdown").objectReferenceValue = dropdown;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return entry.GetComponent<CharacterCreationFeatureEntry>();
        }

        private static TMP_Dropdown CreateDropdown(RectTransform parent, string name, TMP_FontAsset font)
        {
            var dropdownObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(dropdownObject, "Create Feature Dropdown");
            dropdownObject.transform.SetParent(parent, false);
            ApplyUiLayer(dropdownObject);

            var image = dropdownObject.GetComponent<Image>();
            image.color = new Color(0.13f, 0.14f, 0.18f, 0.95f);

            var layout = dropdownObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 36f;

            var label = CreateText(dropdownObject.transform as RectTransform, "Label", "Option", font, 16, FontStyles.Normal);
            label.alignment = TextAlignmentOptions.Left;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 6f);
            labelRect.offsetMax = new Vector2(-30f, -6f);

            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(arrow, "Create Dropdown Arrow");
            arrow.transform.SetParent(dropdownObject.transform, false);
            ApplyUiLayer(arrow);
            var arrowImage = arrow.GetComponent<Image>();
            arrowImage.color = new Color(0.85f, 0.86f, 0.9f, 0.9f);
            var arrowRect = arrow.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(18f, 18f);
            arrowRect.anchoredPosition = new Vector2(-12f, 0f);

            var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            Undo.RegisterCreatedObjectUndo(template, "Create Dropdown Template");
            template.transform.SetParent(dropdownObject.transform, false);
            ApplyUiLayer(template);
            template.SetActive(false);

            var templateImage = template.GetComponent<Image>();
            templateImage.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.sizeDelta = new Vector2(0f, 180f);
            templateRect.anchoredPosition = new Vector2(0f, -4f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(template.transform, false);
            ApplyUiLayer(viewport);
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-4f, -4f);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            ApplyUiLayer(content);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            var contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 4f;
            var contentFitter = content.GetComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            ApplyUiLayer(item);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0f, 24f);

            var itemBackground = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBackground.transform.SetParent(item.transform, false);
            ApplyUiLayer(itemBackground);
            var itemBackgroundImage = itemBackground.GetComponent<Image>();
            itemBackgroundImage.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            var itemBackgroundRect = itemBackground.GetComponent<RectTransform>();
            itemBackgroundRect.anchorMin = Vector2.zero;
            itemBackgroundRect.anchorMax = Vector2.one;
            itemBackgroundRect.offsetMin = Vector2.zero;
            itemBackgroundRect.offsetMax = Vector2.zero;

            var itemCheckmark = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            itemCheckmark.transform.SetParent(item.transform, false);
            ApplyUiLayer(itemCheckmark);
            var checkmarkImage = itemCheckmark.GetComponent<Image>();
            checkmarkImage.color = new Color(0.35f, 0.7f, 1f, 1f);
            var checkmarkRect = itemCheckmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
            checkmarkRect.sizeDelta = new Vector2(16f, 16f);
            checkmarkRect.anchoredPosition = new Vector2(10f, 0f);

            var itemLabel = CreateText(item.transform as RectTransform, "Item Label", "Option", font, 14, FontStyles.Normal);
            itemLabel.alignment = TextAlignmentOptions.Left;
            var itemLabelRect = itemLabel.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(30f, 2f);
            itemLabelRect.offsetMax = new Vector2(-10f, -2f);

            var dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            dropdown.template = template.GetComponent<RectTransform>();
            dropdown.captionText = label;
            dropdown.itemText = itemLabel;
            dropdown.targetGraphic = image;

            var scrollRect = template.GetComponent<ScrollRect>();
            scrollRect.content = content.GetComponent<RectTransform>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var toggle = item.GetComponent<Toggle>();
            toggle.targetGraphic = itemBackgroundImage;
            toggle.graphic = checkmarkImage;

            return dropdown;
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
