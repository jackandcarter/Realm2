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

            var abilityButtons = FindOrCreateChildRect(root.transform, "AbilityButtons");
            ConfigureAbilityButtons(abilityButtons);

            var statusLabel = FindOrCreateStatusLabel(root.transform, font);

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

            var root = new GameObject("ArkitectDockModule", typeof(RectTransform), typeof(ArkitectUIManager), typeof(BuilderAbilityController), typeof(BuilderDockAbilityBinder));
            Undo.RegisterCreatedObjectUndo(root, "Create Arkitect Dock");
            ApplyUiLayer(root);
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
            rect.sizeDelta = new Vector2(600f, 120f);
        }

        private static void ConfigureAbilityButtons(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            rect.sizeDelta = new Vector2(560f, 64f);

            var layout = rect.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = Undo.AddComponent<HorizontalLayoutGroup>(rect.gameObject);
            }

            layout.spacing = 12f;
            layout.padding.left = 8;
            layout.padding.right = 8;
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
    }
}
