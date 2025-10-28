using System;
using System.Collections.Generic;
using System.IO;
using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.DesignerTools
{
    public class StatProfileCreatorWindow : EditorWindow
    {
        private readonly List<StatProfileDefinition> _profiles = new();
        private Vector2 _profileScroll;
        private Vector2 _detailScroll;
        private int _selectedIndex = -1;
        private SerializedObject _serializedProfile;
        private string _search = string.Empty;

        [MenuItem("Tools/Designer/Stat Profile Studio", priority = 110)]
        public static void ShowWindow()
        {
            var window = GetWindow<StatProfileCreatorWindow>("Stat Profile Studio");
            window.minSize = new Vector2(900f, 540f);
            window.RefreshProfiles();
        }

        private void OnEnable()
        {
            RefreshProfiles();
        }

        private void RefreshProfiles()
        {
            _profiles.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Realm.Data.StatProfileDefinition"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<StatProfileDefinition>(path);
                if (profile != null)
                {
                    _profiles.Add(profile);
                }
            }

            _profiles.Sort((a, b) => string.Compare(a?.DisplayName ?? a?.name, b?.DisplayName ?? b?.name, StringComparison.OrdinalIgnoreCase));
            if (_selectedIndex >= _profiles.Count)
            {
                _selectedIndex = _profiles.Count - 1;
            }

            LoadSelectedProfile();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawProfileList();
                DrawProfileDetails();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    RefreshProfiles();
                }

                GUILayout.Space(8f);

                if (GUILayout.Button("New Profile", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    CreateNewProfile();
                }

                GUILayout.FlexibleSpace();
                _search = GUILayout.TextField(_search, GUILayout.Width(220f));
            }
        }

        private void DrawProfileList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(260f)))
            {
                EditorGUILayout.LabelField("Stat Profiles", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Profiles encapsulate JRPG stat curves and formulas that can be shared across multiple classes.", MessageType.Info);

                using (var scroll = new EditorGUILayout.ScrollViewScope(_profileScroll))
                {
                    _profileScroll = scroll.scrollPosition;

                    for (var i = 0; i < _profiles.Count; i++)
                    {
                        var profile = _profiles[i];
                        if (!Matches(profile.DisplayName, _search))
                        {
                            continue;
                        }

                        var label = string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.name : profile.DisplayName;
                        if (GUILayout.Toggle(_selectedIndex == i, label, EditorStyles.toolbarButton))
                        {
                            if (_selectedIndex != i)
                            {
                                _selectedIndex = i;
                                LoadSelectedProfile();
                            }
                        }
                    }
                }
            }
        }

        private void DrawProfileDetails()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (_serializedProfile == null)
                {
                    EditorGUILayout.HelpBox("Select or create a stat profile to edit its curves.", MessageType.Info);
                    return;
                }

                _serializedProfile.Update();

                using (var scroll = new EditorGUILayout.ScrollViewScope(_detailScroll))
                {
                    _detailScroll = scroll.scrollPosition;

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(_serializedProfile.FindProperty("guid"));
                    }

                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("displayName"));
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("description"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Curve Library", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Each entry maps a StatDefinition to JRPG-style base, growth, and formula coefficients. Use the curve foldouts to define your ratio-based system.", MessageType.Info);
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("statCurves"), true);
                }

                if (_serializedProfile.ApplyModifiedProperties())
                {
                    var profile = _profiles[_selectedIndex];
                    EditorUtility.SetDirty(profile);
                }
            }
        }

        private void CreateNewProfile()
        {
            var asset = CreateInstance<StatProfileDefinition>();
            asset.name = "NewStatProfile";
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/ScriptableObjects/Stats/NewStatProfile.asset");
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                var parent = Path.GetDirectoryName(directory);
                var folderName = Path.GetFileName(directory);
                if (!string.IsNullOrEmpty(parent) && AssetDatabase.IsValidFolder(parent))
                {
                    AssetDatabase.CreateFolder(parent, folderName);
                }
                else if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshProfiles();
            _selectedIndex = _profiles.IndexOf(asset);
            if (_selectedIndex < 0)
            {
                _profiles.Add(asset);
                _selectedIndex = _profiles.Count - 1;
            }

            LoadSelectedProfile();
        }

        private void LoadSelectedProfile()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _profiles.Count)
            {
                _serializedProfile = new SerializedObject(_profiles[_selectedIndex]);
            }
            else
            {
                _serializedProfile = null;
            }
        }

        private static bool Matches(string value, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            return value != null && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
