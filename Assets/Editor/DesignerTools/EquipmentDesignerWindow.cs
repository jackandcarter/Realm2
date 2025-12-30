using System;
using System.Collections.Generic;
using System.IO;
using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.DesignerTools
{
    public class EquipmentDesignerWindow : EditorWindow
    {
        private const string WeaponDefinitionFilter = "t:Realm.Data.WeaponDefinition";
        private const string ArmorDefinitionFilter = "t:Realm.Data.ArmorDefinition";

        private readonly List<WeaponDefinition> _weapons = new();
        private readonly List<ArmorDefinition> _armors = new();
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private int _tabIndex;
        private int _selectedIndex = -1;
        private Editor _cachedEditor;

        [MenuItem("Tools/Designer/Equipment Studio", priority = 120)]
        public static void ShowWindow()
        {
            var window = GetWindow<EquipmentDesignerWindow>("Equipment Studio");
            window.minSize = new Vector2(860f, 520f);
        }

        private void OnEnable()
        {
            RefreshAssets();
        }

        private void OnDisable()
        {
            DestroyCachedEditor();
        }

        private void OnGUI()
        {
            var profile = DesignerToolkitProfile.Instance;
            if (profile == null)
            {
                EditorGUILayout.HelpBox("Unable to load the designer toolkit profile.", MessageType.Error);
                return;
            }

            DrawToolbar(profile);

            _tabIndex = GUILayout.Toolbar(_tabIndex, new[] { "Weapons", "Armors" });

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawListPanel(profile);
                DrawDetailPanel();
            }
        }

        private void DrawToolbar(DesignerToolkitProfile profile)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(new GUIContent("Refresh", "Reload weapon and armor assets."), EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    RefreshAssets();
                }

                GUILayout.Space(8f);

                if (_tabIndex == 0)
                {
                    if (GUILayout.Button(new GUIContent("Create Weapon", "Create a new WeaponDefinition asset in the configured folder."), EditorStyles.toolbarButton, GUILayout.Width(110f)))
                    {
                        CreateAsset<WeaponDefinition>("WeaponDefinition", profile.WeaponDefinitionsFolder);
                        RefreshAssets();
                    }
                }
                else
                {
                    if (GUILayout.Button(new GUIContent("Create Armor", "Create a new ArmorDefinition asset in the configured folder."), EditorStyles.toolbarButton, GUILayout.Width(110f)))
                    {
                        CreateAsset<ArmorDefinition>("ArmorDefinition", profile.ArmorDefinitionsFolder);
                        RefreshAssets();
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawListPanel(DesignerToolkitProfile profile)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(280f)))
            {
                EditorGUILayout.LabelField(_tabIndex == 0 ? "Weapons" : "Armors", EditorStyles.boldLabel);

                using (var scroll = new EditorGUILayout.ScrollViewScope(_listScroll, GUILayout.ExpandHeight(true)))
                {
                    _listScroll = scroll.scrollPosition;
                    if (_tabIndex == 0)
                    {
                        DrawListEntries(_weapons);
                    }
                    else
                    {
                        DrawListEntries(_armors);
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Ping Selection"))
                {
                    var selection = GetSelectedAsset();
                    if (selection != null)
                    {
                        Selection.activeObject = selection;
                        EditorGUIUtility.PingObject(selection);
                    }
                }
            }
        }

        private void DrawDetailPanel()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                var selection = GetSelectedAsset();
                if (selection == null)
                {
                    EditorGUILayout.HelpBox("Select a weapon or armor asset to edit its properties.", MessageType.Info);
                    return;
                }

                EditorGUILayout.HelpBox("Assign dock/inventory icons, class restrictions, and equip behaviors to ensure runtime consistency.", MessageType.Info);

                using (var scroll = new EditorGUILayout.ScrollViewScope(_detailScroll))
                {
                    _detailScroll = scroll.scrollPosition;
                    DrawSelectionEditor(selection);
                }
            }
        }

        private void DrawSelectionEditor(UnityEngine.Object asset)
        {
            if (_cachedEditor == null || _cachedEditor.target != asset)
            {
                DestroyCachedEditor();
                _cachedEditor = CreateEditor(asset);
            }

            _cachedEditor?.OnInspectorGUI();
        }

        private void RefreshAssets()
        {
            _weapons.Clear();
            _armors.Clear();

            LoadAssets(WeaponDefinitionFilter, _weapons);
            LoadAssets(ArmorDefinitionFilter, _armors);

            _selectedIndex = -1;
            DestroyCachedEditor();
        }

        private void DrawListEntries<T>(IReadOnlyList<T> assets) where T : UnityEngine.Object
        {
            for (var i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];
                if (asset == null)
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(asset.name) ? "Unnamed" : asset.name;
                var selected = _selectedIndex == i;
                if (GUILayout.Toggle(selected, label, EditorStyles.toolbarButton) && !selected)
                {
                    SelectAsset(i);
                }
            }
        }

        private void SelectAsset(int index)
        {
            _selectedIndex = index;
            DestroyCachedEditor();
        }

        private UnityEngine.Object GetSelectedAsset()
        {
            if (_selectedIndex < 0)
            {
                return null;
            }

            if (_tabIndex == 0)
            {
                return _selectedIndex < _weapons.Count ? _weapons[_selectedIndex] : null;
            }

            return _selectedIndex < _armors.Count ? _armors[_selectedIndex] : null;
        }

        private static void LoadAssets<T>(string filter, List<T> results) where T : UnityEngine.Object
        {
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    results.Add(asset);
                }
            }

            results.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        }

        private static T CreateAsset<T>(string defaultName, string folder) where T : ScriptableObject
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                folder = "Assets";
            }

            EnsureFolder(folder);
            var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, $"{defaultName}.asset"));
            var asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        private static void EnsureFolder(string folderPath)
        {
            folderPath = folderPath.Replace("\\", "/").TrimEnd('/');
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = "Assets";
            var segments = folderPath.Split('/');
            for (var i = 1; i < segments.Length; i++)
            {
                var current = $"{parent}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(current))
                {
                    AssetDatabase.CreateFolder(parent, segments[i]);
                }

                parent = current;
            }
        }

        private void DestroyCachedEditor()
        {
            if (_cachedEditor == null)
            {
                return;
            }

            DestroyImmediate(_cachedEditor);
            _cachedEditor = null;
        }
    }
}
