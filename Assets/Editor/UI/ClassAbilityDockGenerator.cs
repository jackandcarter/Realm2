using System.IO;
using Client.UI.HUD.Dock;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Realm.Editor.UI
{
    public static class ClassAbilityDockGenerator
    {
        private const string PrefabPath = "Assets/UI/Shared/Dock/ClassAbilityDock.prefab";
        private const string MenuRoot = "Tools/Realm/UI";

        [MenuItem(MenuRoot + "/Generate Class Ability Dock", priority = 130)]
        public static void GenerateClassDock()
        {
            var font = LoadDefaultFontAsset();
            var dock = FindOrCreateDockRoot();

            var rootRect = dock.GetComponent<RectTransform>();
            ConfigureRoot(rootRect, dock.GetComponent<Image>());

            var weaponBar = FindOrCreateChildRect(dock.transform, "WeaponAbilityBar");
            ConfigureWeaponBar(weaponBar);

            var abilityBar = FindOrCreateChildRect(dock.transform, "AbilityRow");
            ConfigureAbilityBar(abilityBar);

            CreateWeaponButton(weaponBar, "LightAttack", "Light", font, isSpecial: false);
            CreateWeaponButton(weaponBar, "MediumAttack", "Medium", font, isSpecial: false);
            CreateWeaponButton(weaponBar, "HeavyAttack", "Heavy", font, isSpecial: false);
            CreateWeaponButton(weaponBar, "SpecialAttack", "Special", font, isSpecial: true);

            var template = FindOrCreateAbilityItemTemplate(abilityBar, font);
            ApplyModuleBindings(dock.GetComponent<ClassAbilityDockModule>(), abilityBar, template);

            SavePrefab(dock.gameObject);
            Selection.activeGameObject = dock.gameObject;
        }

        private static ClassAbilityDockModule FindOrCreateDockRoot()
        {
            var existing = Object.FindFirstObjectByType<ClassAbilityDockModule>();
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("ClassAbilityDock", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ClassAbilityDockModule));
            Undo.RegisterCreatedObjectUndo(root, "Create Class Ability Dock");
            ApplyUiLayer(root);
            return root.GetComponent<ClassAbilityDockModule>();
        }

        private static RectTransform FindOrCreateChildRect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing as RectTransform;
            }

            var child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, "Create Class Dock UI Element");
            child.transform.SetParent(parent, false);
            ApplyUiLayer(child);
            return child.GetComponent<RectTransform>();
        }

        private static void ConfigureRoot(RectTransform rect, Image background)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            rect.sizeDelta = new Vector2(640f, 160f);

            if (background == null)
            {
                return;
            }

            background.color = new Color(0.06f, 0.07f, 0.09f, 0.85f);
        }

        private static void ConfigureWeaponBar(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            rect.sizeDelta = new Vector2(600f, 56f);

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

        private static void ConfigureAbilityBar(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            rect.sizeDelta = new Vector2(600f, 80f);

            var layout = rect.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = Undo.AddComponent<HorizontalLayoutGroup>(rect.gameObject);
            }

            layout.spacing = 10f;
            layout.padding.left = 8;
            layout.padding.right = 8;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
        }

        private static void CreateWeaponButton(RectTransform parent, string name, string label, TMP_FontAsset font, bool isSpecial)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return;
            }

            var button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(button, "Create Weapon Ability Button");
            button.transform.SetParent(parent, false);
            ApplyUiLayer(button);

            var image = button.GetComponent<Image>();
            image.color = isSpecial ? new Color(0.2f, 0.2f, 0.2f, 0.8f) : new Color(0.2f, 0.26f, 0.36f, 0.9f);

            var layout = button.GetComponent<LayoutElement>();
            layout.preferredWidth = 120f;
            layout.preferredHeight = 48f;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(button.transform, false);
            ApplyUiLayer(labelObject);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = labelObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.86f, 0.91f, 0.98f, 1f);

            var buttonComponent = button.GetComponent<Button>();
            buttonComponent.interactable = !isSpecial;
        }

        private static AbilityDockItem FindOrCreateAbilityItemTemplate(RectTransform parent, TMP_FontAsset font)
        {
            var existing = parent.Find("AbilityDockItemTemplate");
            if (existing != null && existing.TryGetComponent(out AbilityDockItem existingItem))
            {
                EnsureAbilityItemBindings(existingItem, font);
                existing.gameObject.SetActive(false);
                return existingItem;
            }

            var item = new GameObject("AbilityDockItemTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(AbilityDockItem));
            Undo.RegisterCreatedObjectUndo(item, "Create Ability Dock Item Template");
            item.transform.SetParent(parent, false);
            ApplyUiLayer(item);

            var layout = item.AddComponent<LayoutElement>();
            layout.preferredWidth = 64f;
            layout.preferredHeight = 64f;

            var image = item.GetComponent<Image>();
            image.color = new Color(0.15f, 0.2f, 0.3f, 0.9f);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(item.transform, false);
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
            text.color = new Color(0.84f, 0.92f, 0.99f, 1f);

            var itemComponent = item.GetComponent<AbilityDockItem>();
            EnsureAbilityItemBindings(itemComponent, font);

            item.SetActive(false);
            return itemComponent;
        }

        private static void EnsureAbilityItemBindings(AbilityDockItem item, TMP_FontAsset font)
        {
            if (item == null)
            {
                return;
            }

            var image = item.GetComponent<Image>();
            var label = item.GetComponentInChildren<TextMeshProUGUI>();
            var canvasGroup = item.GetComponent<CanvasGroup>();

            var serialized = new SerializedObject(item);
            serialized.FindProperty("iconImage").objectReferenceValue = image;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (label != null && font != null)
            {
                label.font = font;
            }
        }

        private static void ApplyModuleBindings(ClassAbilityDockModule module, RectTransform abilityBar, AbilityDockItem template)
        {
            if (module == null)
            {
                return;
            }

            var serialized = new SerializedObject(module);
            serialized.FindProperty("itemContainer").objectReferenceValue = abilityBar;
            serialized.FindProperty("itemPrefab").objectReferenceValue = template;
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
