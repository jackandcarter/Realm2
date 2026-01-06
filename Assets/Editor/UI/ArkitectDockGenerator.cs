using System.IO;
using Client.Builder;
using Client.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Realm.Editor.UI
{
    public static class ArkitectDockGenerator
    {
        private const string PrefabPath = "Assets/UI/Arkitect/ArkitectDock.prefab";
        private const string MenuRoot = "Tools/Realm/UI";

        [MenuItem(MenuRoot + "/Generate Arkitect Dock", priority = 125)]
        public static void GenerateDock()
        {
            var font = LoadDefaultFontAsset();
            var manager = FindOrCreateDockRoot();

            var root = manager.gameObject;
            var rootRect = root.GetComponent<RectTransform>();
            ConfigureRoot(rootRect);

            DockGeneratorUtility.ConfigureMasterDockLayout(root, out var leftSection, out var centerSection, out _);

            EnsureLeftWeaponSection(leftSection, font);

            var abilityButtons = FindOrCreateChildRect(centerSection.transform, "AbilityButtons");
            ConfigureAbilityButtons(abilityButtons);

            var statusLabel = FindOrCreateStatusLabel(leftSection.transform, font);

            var buttonTemplate = FindOrCreateButtonTemplate(abilityButtons, font);

            ApplyBinderBindings(manager, abilityButtons, buttonTemplate, statusLabel);

            SavePrefab(root);
            Selection.activeGameObject = root;
        }

        private static ArkitectUIManager FindOrCreateDockRoot()
        {
            var existing = Object.FindFirstObjectByType<ArkitectUIManager>();
            if (existing != null && existing.gameObject.name == "ArkitectDockModule")
            {
                return existing;
            }

            var root = DockGeneratorUtility.InstantiateDockRoot("ArkitectDockModule", "Create Arkitect Dock");
            EnsureComponent<ArkitectUIManager>(root);
            EnsureComponent<BuilderAbilityController>(root);
            EnsureComponent<BuilderDockAbilityBinder>(root);
            return root.GetComponent<ArkitectUIManager>();
        }

        private static RectTransform FindOrCreateChildRect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing as RectTransform;
            }

            var child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, "Create Arkitect Dock UI Element");
            child.transform.SetParent(parent, false);
            ApplyUiLayer(child);
            return child.GetComponent<RectTransform>();
        }

        private static void ConfigureRoot(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 32f);
            rect.sizeDelta = new Vector2(640f, 160f);
        }

        private static void ConfigureAbilityButtons(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var layout = rect.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = Undo.AddComponent<HorizontalLayoutGroup>(rect.gameObject);
            }

            layout.spacing = 12f;
            layout.padding.left = 12;
            layout.padding.right = 12;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
        }

        private static TextMeshProUGUI FindOrCreateStatusLabel(Transform parent, TMP_FontAsset font)
        {
            var existing = parent.Find("StatusLabel");
            if (existing != null && existing.TryGetComponent(out TextMeshProUGUI existingText))
            {
                EnsureStatusLabel(existingText, font);
                return existingText;
            }

            var label = new GameObject("StatusLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(label, "Create Arkitect Dock Status Label");
            label.transform.SetParent(parent, false);
            ApplyUiLayer(label);

            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            rect.sizeDelta = new Vector2(0f, 28f);

            var text = label.GetComponent<TextMeshProUGUI>();
            EnsureStatusLabel(text, font);
            return text;
        }

        private static void EnsureStatusLabel(TextMeshProUGUI text, TMP_FontAsset font)
        {
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.82f, 0.89f, 0.97f, 1f);
            if (string.IsNullOrWhiteSpace(text.text))
            {
                text.text = "Ready.";
            }

            var layout = text.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = Undo.AddComponent<LayoutElement>(text.gameObject);
            }

            layout.ignoreLayout = true;
        }

        private static GameObject FindOrCreateButtonTemplate(RectTransform parent, TMP_FontAsset font)
        {
            var existing = parent.Find("AbilityButtonTemplate");
            if (existing != null)
            {
                var existingButton = existing.GetComponent<Button>();
                if (existingButton != null)
                {
                    EnsureButtonLabel(existingButton, font);
                }

                existing.gameObject.SetActive(false);
                return existing.gameObject;
            }

            var button = new GameObject("AbilityButtonTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(button, "Create Arkitect Dock Button Template");
            button.transform.SetParent(parent, false);
            ApplyUiLayer(button);

            var image = button.GetComponent<Image>();
            image.color = new Color(0.2f, 0.26f, 0.36f, 0.9f);

            var layout = button.GetComponent<LayoutElement>();
            layout.preferredWidth = 64f;
            layout.preferredHeight = 64f;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(button.transform, false);
            ApplyUiLayer(labelObject);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = labelObject.GetComponent<TextMeshProUGUI>();
            text.text = "Ability";
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.86f, 0.91f, 0.98f, 1f);

            var buttonComponent = button.GetComponent<Button>();
            EnsureButtonLabel(buttonComponent, font);

            button.SetActive(false);
            return button;
        }

        private static void EnsureButtonLabel(Button button, TMP_FontAsset font)
        {
            if (button == null)
            {
                return;
            }

            var labelTransform = button.transform.Find("Label");
            if (labelTransform == null)
            {
                return;
            }

            if (!labelTransform.TryGetComponent(out TextMeshProUGUI text))
            {
                return;
            }

            if (font != null)
            {
                text.font = font;
            }

            text.fontStyle = FontStyles.Bold;
        }

        private static void ApplyBinderBindings(ArkitectUIManager manager, RectTransform container, GameObject template, TextMeshProUGUI statusLabel)
        {
            var binder = manager != null ? manager.GetComponent<BuilderDockAbilityBinder>() : null;
            if (binder == null)
            {
                return;
            }

            var serialized = new SerializedObject(binder);
            serialized.FindProperty("buttonContainer").objectReferenceValue = container;
            serialized.FindProperty("buttonPrefab").objectReferenceValue = template;
            serialized.FindProperty("statusLabel").objectReferenceValue = statusLabel;
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

        private static void EnsureComponent<T>(GameObject target) where T : Component
        {
            if (target.GetComponent<T>() == null)
            {
                Undo.AddComponent<T>(target);
            }
        }

        private static void EnsureLeftWeaponSection(RectTransform leftSection, TMP_FontAsset font)
        {
            if (leftSection == null)
            {
                return;
            }

            var layout = leftSection.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = Undo.AddComponent<HorizontalLayoutGroup>(leftSection.gameObject);
            }

            layout.spacing = 8f;
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            EnsureWeaponButton(leftSection, "LightWeaponButton", "Light", font);
            EnsureWeaponButton(leftSection, "MediumWeaponButton", "Medium", font);
            EnsureWeaponButton(leftSection, "HeavyWeaponButton", "Heavy", font);
        }

        private static void EnsureWeaponButton(Transform parent, string name, string label, TMP_FontAsset font)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Button existingButton))
            {
                ConfigureWeaponButton(existingButton, label, font);
                return;
            }

            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(buttonObject, "Create Arkitect Dock Weapon Button");
            buttonObject.transform.SetParent(parent, false);
            ApplyUiLayer(buttonObject);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 40f);

            var button = buttonObject.GetComponent<Button>();
            ConfigureWeaponButton(button, label, font);
        }

        private static void ConfigureWeaponButton(Button button, string label, TMP_FontAsset font)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image == null)
            {
                image = Undo.AddComponent<Image>(button.gameObject);
            }

            image.color = new Color(0.118f, 0.149f, 0.231f, 0.9f);

            var layout = button.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = Undo.AddComponent<HorizontalLayoutGroup>(button.gameObject);
            }

            layout.spacing = 6f;
            layout.padding = new RectOffset(10, 10, 4, 4);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var icon = FindOrCreateWeaponIcon(button.transform);
            icon.color = Color.white;

            var text = FindOrCreateWeaponLabel(button.transform, label);
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = 16f;
            text.color = new Color(0.902f, 0.922f, 0.957f, 1f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
        }

        private static Image FindOrCreateWeaponIcon(Transform parent)
        {
            var existing = parent.Find("Icon");
            if (existing != null && existing.TryGetComponent(out Image existingImage))
            {
                return existingImage;
            }

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(iconObject, "Create Arkitect Dock Weapon Icon");
            iconObject.transform.SetParent(parent, false);
            ApplyUiLayer(iconObject);

            var rect = iconObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(20f, 20f);

            var image = iconObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static TextMeshProUGUI FindOrCreateWeaponLabel(Transform parent, string label)
        {
            var existing = parent.Find("Label");
            if (existing != null && existing.TryGetComponent(out TextMeshProUGUI existingText))
            {
                existingText.text = label;
                return existingText;
            }

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelObject, "Create Arkitect Dock Weapon Label");
            labelObject.transform.SetParent(parent, false);
            ApplyUiLayer(labelObject);

            var rect = labelObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 24f);

            var text = labelObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            return text;
        }

    }
}
