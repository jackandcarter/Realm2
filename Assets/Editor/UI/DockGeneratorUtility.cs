using System;
using Client.Combat;
using Client.UI.HUD.Dock;
using Client.UI.HUD.Inventory;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Realm.Editor.UI
{
    internal static class DockGeneratorUtility
    {
        internal const string MasterDockPrefabPath = "Assets/UI/Shared/Dock/MasterDock.prefab";
        private const string DockShortcutItemPrefabPath = "Assets/UI/Shared/Dock/DockShortcutItem.prefab";

        private const float SeparatorWidth = 2f;
        private const float SeparatorHeight = 48f;
        private static readonly Color SeparatorColor = new Color(0.25f, 0.28f, 0.33f, 0.85f);
        private const string LeftSeparatorName = "LeftCenterSeparator";
        private const string RightSeparatorName = "CenterRightSeparator";
        private static readonly string[] LeftSeparatorAliases = { "LeftSectionSeparator" };
        private static readonly string[] RightSeparatorAliases = { "RightSectionSeparator" };
        private const string InventoryPanelName = "InventoryPanel";
        private const string InventorySlotRootName = "InventorySlots";
        private const int InventorySlotCount = 8;
        private static readonly Color InventoryPanelColor = new Color(0.094f, 0.114f, 0.169f, 0.95f);
        private static readonly Color InventorySlotColor = new Color(0.118f, 0.149f, 0.231f, 0.9f);

        internal static GameObject InstantiateDockRoot(string name, string undoLabel)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MasterDockPrefabPath);
            GameObject root;
            if (prefab != null)
            {
                root = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (root != null)
                {
                    Undo.RegisterCreatedObjectUndo(root, undoLabel);
                    root.name = name;
                    ApplyUiLayer(root);
                    return root;
                }
            }

            root = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(root, undoLabel);
            ApplyUiLayer(root);
            return root;
        }

        internal static void ConfigureMasterDockLayout(GameObject root, out RectTransform leftSection, out RectTransform centerSection, out RectTransform rightSection)
        {
            leftSection = FindOrCreateSection(root.transform, "LeftSection", 200f);
            centerSection = FindOrCreateSection(root.transform, "CenterSection", 240f);
            rightSection = FindOrCreateSection(root.transform, "RightSection", 200f);

            RemoveHorizontalLayoutGroup(root);
            RemoveSectionLayout(leftSection);
            RemoveSectionLayout(centerSection);
            RemoveSectionLayout(rightSection);

            var leftSeparator = EnsureSeparator(root.transform, LeftSeparatorName, LeftSeparatorAliases);
            var rightSeparator = EnsureSeparator(root.transform, RightSeparatorName, RightSeparatorAliases);

            leftSection.SetSiblingIndex(0);
            leftSeparator.SetSiblingIndex(1);
            centerSection.SetSiblingIndex(2);
            rightSeparator.SetSiblingIndex(3);
            rightSection.SetSiblingIndex(4);

            EnsureWeaponButtons(leftSection);
            EnsureDockShortcutSection(rightSection);
            EnsureRightSectionShortcutIds(rightSection);
            var inventoryPanel = EnsureInventoryPanel(root.transform);
            EnsureInventoryShortcutSource(root, inventoryPanel);
        }

        internal static void EnsureDockShortcutSection(RectTransform rightSection)
        {
            if (rightSection == null)
            {
                return;
            }

            var section = rightSection.GetComponent<DockShortcutSection>();
            if (section == null)
            {
                section = Undo.AddComponent<DockShortcutSection>(rightSection.gameObject);
            }

            var itemPrefab = LoadDockShortcutItem();
            if (itemPrefab == null)
            {
                return;
            }

            var serialized = new SerializedObject(section);
            serialized.FindProperty("itemPrefab").objectReferenceValue = itemPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static DockShortcutItem LoadDockShortcutItem()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DockShortcutItemPrefabPath);
            if (prefab == null)
            {
                return null;
            }

            return prefab.GetComponent<DockShortcutItem>();
        }

        private static RectTransform FindOrCreateSection(Transform parent, string name, float preferredWidth)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
            {
                return existingRect;
            }

            var section = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(section, "Create Dock Section");
            section.transform.SetParent(parent, false);
            ApplyUiLayer(section);

            var rect = section.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(preferredWidth, 0f);

            return rect;
        }

        private static void RemoveSectionLayout(Component rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            var layout = rectTransform.GetComponent<LayoutElement>();
            if (layout != null)
            {
                Undo.DestroyObjectImmediate(layout);
            }
        }

        private static void RemoveHorizontalLayoutGroup(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var layout = root.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                Undo.DestroyObjectImmediate(layout);
            }
        }

        private static RectTransform EnsureSeparator(Transform parent, string name, params string[] aliases)
        {
            var existing = FindByName(parent, name, aliases);
            if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
            {
                EnsureSeparatorName(existingRect.gameObject, name);
                EnsureSeparatorComponents(existingRect);
                EnsureSeparatorLayout(existingRect);
                return existingRect;
            }

            var separator = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(separator, "Create Dock Separator");
            separator.transform.SetParent(parent, false);
            ApplyUiLayer(separator);

            var rect = separator.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(SeparatorWidth, SeparatorHeight);

            EnsureSeparatorComponents(rect);
            EnsureSeparatorLayout(rect);
            return rect;
        }

        private static Transform FindByName(Transform parent, string name, params string[] aliases)
        {
            if (parent == null)
            {
                return null;
            }

            var direct = parent.Find(name);
            if (direct != null)
            {
                return direct;
            }

            if (aliases == null)
            {
                return null;
            }

            foreach (var alias in aliases)
            {
                if (string.IsNullOrWhiteSpace(alias))
                {
                    continue;
                }

                var match = parent.Find(alias);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void EnsureSeparatorName(GameObject separator, string targetName)
        {
            if (separator == null || string.IsNullOrWhiteSpace(targetName))
            {
                return;
            }

            if (!string.Equals(separator.name, targetName, StringComparison.Ordinal))
            {
                Undo.RecordObject(separator, "Rename Dock Separator");
                separator.name = targetName;
            }
        }

        private static void EnsureSeparatorComponents(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            var image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = Undo.AddComponent<Image>(rect.gameObject);
            }

            image.color = SeparatorColor;

            var layout = rect.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = Undo.AddComponent<LayoutElement>(rect.gameObject);
            }

            layout.preferredWidth = SeparatorWidth;
            layout.minWidth = SeparatorWidth;
            layout.flexibleWidth = 0f;
            layout.preferredHeight = SeparatorHeight;
            layout.minHeight = SeparatorHeight;
            layout.flexibleHeight = 1f;
        }

        private static void EnsureSeparatorLayout(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(SeparatorWidth, SeparatorHeight);
        }

        private static void EnsureRightSectionShortcutIds(RectTransform rightSection)
        {
            if (rightSection == null)
            {
                return;
            }

            foreach (Transform child in rightSection)
            {
                if (child == null)
                {
                    continue;
                }

                var shortcutId = child.GetComponent<DockShortcutId>();
                if (shortcutId == null)
                {
                    shortcutId = Undo.AddComponent<DockShortcutId>(child.gameObject);
                }

                var serialized = new SerializedObject(shortcutId);
                var property = serialized.FindProperty("shortcutId");
                if (property != null && string.IsNullOrWhiteSpace(property.stringValue))
                {
                    property.stringValue = child.gameObject.name;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static InventoryPanelController EnsureInventoryPanel(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            var existing = root.Find(InventoryPanelName);
            GameObject panelObject;
            if (existing == null)
            {
                panelObject = new GameObject(
                    InventoryPanelName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(CanvasGroup),
                    typeof(InventoryPanelController));
                Undo.RegisterCreatedObjectUndo(panelObject, "Create Inventory Panel");
                panelObject.transform.SetParent(root, false);
                ApplyUiLayer(panelObject);
            }
            else
            {
                panelObject = existing.gameObject;
            }

            var rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-12f, 168f);
            rect.sizeDelta = new Vector2(320f, 220f);

            var image = panelObject.GetComponent<Image>();
            if (image == null)
            {
                image = Undo.AddComponent<Image>(panelObject);
            }

            image.color = InventoryPanelColor;

            var canvasGroup = panelObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = Undo.AddComponent<CanvasGroup>(panelObject);
            }

            var controller = panelObject.GetComponent<InventoryPanelController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<InventoryPanelController>(panelObject);
            }

            var slotRoot = EnsureInventorySlotRoot(panelObject.transform);
            EnsureInventorySlots(slotRoot, controller);

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("slotRoot").objectReferenceValue = slotRoot;
            serialized.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        private static RectTransform EnsureInventorySlotRoot(Transform panel)
        {
            var existing = panel.Find(InventorySlotRootName);
            if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
            {
                return existingRect;
            }

            var slotRoot = new GameObject(InventorySlotRootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(slotRoot, "Create Inventory Slots");
            slotRoot.transform.SetParent(panel, false);
            ApplyUiLayer(slotRoot);

            var rect = slotRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(-24f, -24f);

            return rect;
        }

        private static void EnsureInventorySlots(RectTransform slotRoot, InventoryPanelController controller)
        {
            if (slotRoot == null)
            {
                return;
            }

            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var slots = new InventorySlotView[InventorySlotCount];

            for (var i = 0; i < InventorySlotCount; i++)
            {
                var slotName = $"InventorySlot_{i + 1}";
                var existing = slotRoot.Find(slotName);
                GameObject slotObject;
                if (existing == null)
                {
                    slotObject = new GameObject(
                        slotName,
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image),
                        typeof(InventorySlotView));
                    Undo.RegisterCreatedObjectUndo(slotObject, "Create Inventory Slot");
                    slotObject.transform.SetParent(slotRoot, false);
                    ApplyUiLayer(slotObject);
                }
                else
                {
                    slotObject = existing.gameObject;
                }

                var rect = slotObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(56f, 56f);
                rect.anchoredPosition = new Vector2(12f + (i % 4) * 64f, -12f - (i / 4) * 64f);

                var image = slotObject.GetComponent<Image>();
                if (image == null)
                {
                    image = Undo.AddComponent<Image>(slotObject);
                }

                image.color = InventorySlotColor;

                var icon = EnsureSlotChildImage(slotObject.transform);
                var quantity = EnsureSlotChildLabel(slotObject.transform, font);

                var slotView = slotObject.GetComponent<InventorySlotView>();
                if (slotView == null)
                {
                    slotView = Undo.AddComponent<InventorySlotView>(slotObject);
                }

                var slotSerialized = new SerializedObject(slotView);
                slotSerialized.FindProperty("iconImage").objectReferenceValue = icon;
                slotSerialized.FindProperty("quantityLabel").objectReferenceValue = quantity;
                slotSerialized.ApplyModifiedPropertiesWithoutUndo();

                slots[i] = slotView;
            }

            if (controller != null)
            {
                var serialized = new SerializedObject(controller);
                var slotsProperty = serialized.FindProperty("slots");
                if (slotsProperty != null)
                {
                    slotsProperty.arraySize = slots.Length;
                    for (var i = 0; i < slots.Length; i++)
                    {
                        slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Image EnsureSlotChildImage(Transform parent)
        {
            var existing = parent.Find("Icon");
            GameObject iconObject;
            if (existing == null)
            {
                iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(iconObject, "Create Inventory Slot Icon");
                iconObject.transform.SetParent(parent, false);
                ApplyUiLayer(iconObject);
            }
            else
            {
                iconObject = existing.gameObject;
            }

            var rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(40f, 40f);

            return iconObject.GetComponent<Image>();
        }

        private static Text EnsureSlotChildLabel(Transform parent, Font font)
        {
            var existing = parent.Find("Quantity");
            GameObject labelObject;
            if (existing == null)
            {
                labelObject = new GameObject("Quantity", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                Undo.RegisterCreatedObjectUndo(labelObject, "Create Inventory Slot Quantity");
                labelObject.transform.SetParent(parent, false);
                ApplyUiLayer(labelObject);
            }
            else
            {
                labelObject = existing.gameObject;
            }

            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-4f, 4f);
            rect.sizeDelta = new Vector2(48f, 20f);

            var text = labelObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 14;
            text.alignment = TextAnchor.LowerRight;
            text.color = Color.white;

            return text;
        }

        private static void EnsureInventoryShortcutSource(GameObject root, InventoryPanelController panelController)
        {
            if (root == null)
            {
                return;
            }

            var source = root.GetComponent<InventoryDockShortcutSource>();
            if (source == null)
            {
                source = Undo.AddComponent<InventoryDockShortcutSource>(root);
            }

            var serialized = new SerializedObject(source);
            serialized.FindProperty("shortcutId").stringValue = "inventory";
            serialized.FindProperty("displayName").stringValue = "Inventory";
            serialized.FindProperty("panelController").objectReferenceValue = panelController;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyUiLayer(GameObject target)
        {
            var uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                target.layer = uiLayer;
            }
        }

        private static void EnsureWeaponButtons(RectTransform leftSection)
        {
            if (leftSection == null)
            {
                return;
            }

            EnsureLeftSectionLayout(leftSection);

            var light = EnsureWeaponButton(leftSection, "LightWeaponButton", "Light");
            var medium = EnsureWeaponButton(leftSection, "MediumWeaponButton", "Medium");
            var heavy = EnsureWeaponButton(leftSection, "HeavyWeaponButton", "Heavy");
            var special = EnsureWeaponButton(leftSection, "SpecialWeaponButton", "Special");
            var specialIndicator = EnsureSpecialReadyIndicator(special);

            var controller = leftSection.GetComponent<WeaponDockController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<WeaponDockController>(leftSection.gameObject);
            }

            var attackController = leftSection.GetComponent<WeaponAttackController>();
            if (attackController == null)
            {
                attackController = Undo.AddComponent<WeaponAttackController>(leftSection.gameObject);
            }

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("lightButton").objectReferenceValue = light;
            serialized.FindProperty("mediumButton").objectReferenceValue = medium;
            serialized.FindProperty("heavyButton").objectReferenceValue = heavy;
            serialized.FindProperty("specialButton").objectReferenceValue = special;
            serialized.FindProperty("specialReadyIndicator").objectReferenceValue = specialIndicator;
            serialized.FindProperty("attackController").objectReferenceValue = attackController;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureLeftSectionLayout(RectTransform leftSection)
        {
            var existingLayout = leftSection.GetComponent<LayoutGroup>();
            if (existingLayout != null && existingLayout is not VerticalLayoutGroup)
            {
                Undo.DestroyObjectImmediate(existingLayout);
            }

            var layout = leftSection.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = Undo.AddComponent<VerticalLayoutGroup>(leftSection.gameObject);
            }

            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static Button EnsureWeaponButton(
            RectTransform parent,
            string name,
            string labelText)
        {
            var existing = parent.Find(name);
            GameObject instance;
            if (existing == null)
            {
                instance = CreateWeaponButton(parent, name);
            }
            else
            {
                instance = existing.gameObject;
            }

            EnsureWeaponButtonComponents(instance, labelText);

            return instance.GetComponent<Button>();
        }

        private static GameObject CreateWeaponButton(RectTransform parent, string name)
        {
            var instance = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(HorizontalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(instance, "Create Weapon Dock Button");
            instance.transform.SetParent(parent, false);
            ApplyUiLayer(instance);

            var rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(140f, 44f);

            return instance;
        }

        private static void EnsureWeaponButtonComponents(GameObject instance, string labelText)
        {
            if (instance == null)
            {
                return;
            }

            var image = instance.GetComponent<Image>();
            if (image == null)
            {
                image = Undo.AddComponent<Image>(instance);
            }

            image.color = new Color(0.118f, 0.149f, 0.231f, 0.9f);

            var button = instance.GetComponent<Button>();
            if (button == null)
            {
                button = Undo.AddComponent<Button>(instance);
            }

            button.targetGraphic = image;

            var layout = instance.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = Undo.AddComponent<HorizontalLayoutGroup>(instance);
            }

            layout.padding = new RectOffset(12, 12, 6, 6);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var labelTransform = instance.transform.Find("Label");
            GameObject labelObject;
            if (labelTransform == null)
            {
                labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(labelObject, "Create Weapon Dock Label");
                labelObject.transform.SetParent(instance.transform, false);
                ApplyUiLayer(labelObject);

                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.sizeDelta = Vector2.zero;
            }
            else
            {
                labelObject = labelTransform.gameObject;
            }

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                label = Undo.AddComponent<TextMeshProUGUI>(labelObject);
            }

            label.text = labelText;
            label.alignment = TextAlignmentOptions.Left;
            label.fontSize = 18f;
            label.color = Color.white;

            var layoutElement = labelObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = Undo.AddComponent<LayoutElement>(labelObject);
            }

            layoutElement.preferredWidth = 0f;
            layoutElement.preferredHeight = 0f;
            layoutElement.flexibleWidth = 1f;
        }

        private static Image EnsureSpecialReadyIndicator(Button specialButton)
        {
            if (specialButton == null)
            {
                return null;
            }

            var existing = specialButton.transform.Find("SpecialReadyIndicator");
            if (existing != null && existing.TryGetComponent(out Image existingImage))
            {
                return existingImage;
            }

            var indicator = new GameObject("SpecialReadyIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(indicator, "Create Special Ready Indicator");
            indicator.transform.SetParent(specialButton.transform, false);
            ApplyUiLayer(indicator);

            var rect = indicator.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;

            var image = indicator.GetComponent<Image>();
            image.color = new Color(0.972f, 0.757f, 0.2f, 0.6f);
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }
    }
}
