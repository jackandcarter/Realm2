using System;
using System.Collections.Generic;
using System.Linq;
using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.Configuration
{
    public class ConfigurationDiagnosticsWindow : EditorWindow
    {
        private readonly List<ValidationIssue> _issues = new List<ValidationIssue>();
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Realm/Configuration Diagnostics")]
        public static void ShowWindow()
        {
            var window = GetWindow<ConfigurationDiagnosticsWindow>();
            window.titleContent = new GUIContent("Configuration Diagnostics", EditorGUIUtility.IconContent("d_console.infoicon.sml").image);
            window.minSize = new Vector2(520f, 320f);
            window.Scan();
            window.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox("No configuration issues detected.", MessageType.Info);
                return;
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;
                foreach (var issue in _issues)
                {
                    DrawIssue(issue);
                }
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Scan", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    Scan();
                }

                using (new EditorGUI.DisabledScope(_issues.Count == 0))
                {
                    if (GUILayout.Button("Repair All", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                    {
                        RepairAll();
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawIssue(ValidationIssue issue)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(issue.AssetPath, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(issue.Message, EditorStyles.wordWrappedLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Ping", GUILayout.Width(80f)))
                    {
                        PingAsset(issue.AssetPath);
                    }

                    using (new EditorGUI.DisabledScope(issue.FixAction == null))
                    {
                        if (GUILayout.Button("Repair", GUILayout.Width(80f)))
                        {
                            issue.FixAction?.Invoke();
                            PostFixRefresh();
                        }
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        private void Scan()
        {
            _issues.Clear();

            var assetGuids = AssetDatabase.FindAssets("t:ConfigurationAsset");
            var guidUsage = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var guid in assetGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ConfigurationAsset>(path);
                if (asset == null)
                {
                    continue;
                }

                if (asset is IGuidIdentified identified)
                {
                    var assetGuid = identified.Guid;
                    if (string.IsNullOrWhiteSpace(assetGuid))
                    {
                        _issues.Add(ValidationIssue.CreateMissingGuid(path));
                    }
                    else
                    {
                        if (!guidUsage.TryGetValue(assetGuid, out var list))
                        {
                            list = new List<string>();
                            guidUsage.Add(assetGuid, list);
                        }

                        list.Add(path);
                    }
                }

                CollectBrokenReferences(path, asset);
            }

            foreach (var kvp in guidUsage)
            {
                if (kvp.Value.Count <= 1)
                {
                    continue;
                }

                for (var i = 1; i < kvp.Value.Count; i++)
                {
                    _issues.Add(ValidationIssue.CreateDuplicateGuid(kvp.Value[i], kvp.Key));
                }
            }

            Repaint();
        }

        private void CollectBrokenReferences(string assetPath, ConfigurationAsset asset)
        {
            var serializedObject = new SerializedObject(asset);
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (iterator.propertyPath == "m_Script")
                {
                    continue;
                }

                if (iterator.objectReferenceValue == null && iterator.objectReferenceInstanceIDValue != 0)
                {
                    var propertyPath = iterator.propertyPath;
                    _issues.Add(ValidationIssue.CreateBrokenReference(assetPath, propertyPath));
                }
            }
        }

        private void RepairAll()
        {
            var fixable = _issues.Where(issue => issue.FixAction != null).ToList();
            foreach (var issue in fixable)
            {
                issue.FixAction?.Invoke();
            }

            PostFixRefresh();
        }

        private static void PingAsset(string assetPath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
            {
                return;
            }

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        private void PostFixRefresh()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Scan();
        }

        private class ValidationIssue
        {
            public string AssetPath { get; }
            public string Message { get; }
            public Action FixAction { get; }

            private ValidationIssue(string assetPath, string message, Action fixAction)
            {
                AssetPath = assetPath;
                Message = message;
                FixAction = fixAction;
            }

            public static ValidationIssue CreateMissingGuid(string assetPath)
            {
                return new ValidationIssue(assetPath, "Missing or empty GUID.", () => RepairGuid(assetPath));
            }

            public static ValidationIssue CreateDuplicateGuid(string assetPath, string duplicateGuid)
            {
                return new ValidationIssue(assetPath, $"Duplicate GUID detected ({duplicateGuid}).", () => RepairGuid(assetPath));
            }

            public static ValidationIssue CreateBrokenReference(string assetPath, string propertyPath)
            {
                return new ValidationIssue(assetPath, $"Broken reference on '{propertyPath}'.", () => RepairBrokenReference(assetPath, propertyPath));
            }

            private static void RepairGuid(string assetPath)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ConfigurationAsset>(assetPath);
                if (asset == null)
                {
                    return;
                }

                var serializedObject = new SerializedObject(asset);
                var guidProperty = serializedObject.FindProperty("guid");
                if (guidProperty == null)
                {
                    return;
                }

                guidProperty.stringValue = Guid.NewGuid().ToString("N");
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                asset.RecordManualModification();
                EditorUtility.SetDirty(asset);
            }

            private static void RepairBrokenReference(string assetPath, string propertyPath)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ConfigurationAsset>(assetPath);
                if (asset == null)
                {
                    return;
                }

                var serializedObject = new SerializedObject(asset);
                var property = serializedObject.FindProperty(propertyPath);
                if (property == null)
                {
                    return;
                }

                property.objectReferenceValue = null;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                asset.RecordManualModification();
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
