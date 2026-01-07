using System;
using Client.UI.HUD.Dock;
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
        private const float SectionSpacing = 12f;
        private const int HorizontalPadding = 16;
        private const int VerticalPadding = 10;
        private static readonly Color SeparatorColor = new Color(0.25f, 0.28f, 0.33f, 0.85f);
        private const string LeftSeparatorName = "LeftCenterSeparator";
        private const string RightSeparatorName = "CenterRightSeparator";
        private static readonly string[] LeftSeparatorAliases = { "LeftSectionSeparator" };
        private static readonly string[] RightSeparatorAliases = { "RightSectionSeparator" };

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

            var layout = root.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = Undo.AddComponent<HorizontalLayoutGroup>(root);
            }

            layout.spacing = SectionSpacing;
            layout.padding = new RectOffset(HorizontalPadding, HorizontalPadding, VerticalPadding, VerticalPadding);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var leftSeparator = EnsureSeparator(root.transform, LeftSeparatorName, LeftSeparatorAliases);
            var rightSeparator = EnsureSeparator(root.transform, RightSeparatorName, RightSeparatorAliases);

            leftSection.SetSiblingIndex(0);
            leftSeparator.SetSiblingIndex(1);
            centerSection.SetSiblingIndex(2);
            rightSeparator.SetSiblingIndex(3);
            rightSection.SetSiblingIndex(4);

            EnsureDockShortcutSection(rightSection);
            EnsureRightSectionShortcutIds(rightSection);
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
                EnsureSectionLayout(existingRect, preferredWidth);
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

            EnsureSectionLayout(rect, preferredWidth);
            return rect;
        }

        private static void EnsureSectionLayout(RectTransform rect, float preferredWidth)
        {
            if (rect == null)
            {
                return;
            }

            var layout = rect.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = Undo.AddComponent<LayoutElement>(rect.gameObject);
            }

            layout.preferredWidth = preferredWidth;
            layout.minWidth = preferredWidth;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 1f;
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
