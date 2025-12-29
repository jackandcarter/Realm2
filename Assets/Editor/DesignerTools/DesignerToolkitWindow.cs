using System;
using System.Collections.Generic;
using System.IO;
using Realm.Abilities;
using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.DesignerTools
{
    public class DesignerToolkitWindow : EditorWindow
    {
        private const string StatDefinitionFilter = "t:Realm.Data.StatDefinition";
        private const string StatCategoryFilter = "t:Realm.Data.StatCategory";
        private const string StatProfileFilter = "t:Realm.Data.StatProfileDefinition";
        private const string ClassDefinitionFilter = "t:Realm.Data.ClassDefinition";
        private const string AbilityDefinitionFilter = "t:Realm.Abilities.AbilityDefinition";

        private Vector2 _scrollPosition;

        [MenuItem("Tools/Designer/Designer Toolkit", priority = 90)]
        public static void ShowWindow()
        {
            var window = GetWindow<DesignerToolkitWindow>("Designer Toolkit");
            window.minSize = new Vector2(840f, 520f);
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

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;
                DrawRegistrySection(profile);
                EditorGUILayout.Space();
                DrawAssetCreationSection(profile);
                EditorGUILayout.Space();
                DrawShortcutSection();
            }
        }

        private static void DrawToolbar(DesignerToolkitProfile profile)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Save Profile", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    profile.SaveProfile();
                }

                if (GUILayout.Button("Reset Defaults", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                {
                    ResetDefaults(profile);
                }

                GUILayout.FlexibleSpace();
            }
        }

        private static void DrawRegistrySection(DesignerToolkitProfile profile)
        {
            EditorGUILayout.LabelField("Registry", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("The registry aggregates stat, class, and ability assets for runtime lookups. Sync it whenever new assets are created.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            var registry = (StatRegistry)EditorGUILayout.ObjectField("Stat Registry", profile.StatRegistry, typeof(StatRegistry), false);
            if (EditorGUI.EndChangeCheck())
            {
                profile.StatRegistry = registry;
                profile.SaveProfile();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(profile.StatRegistry != null))
                {
                    if (GUILayout.Button("Create Registry", GUILayout.Width(140f)))
                    {
                        profile.StatRegistry = CreateRegistryAsset(profile);
                        profile.SaveProfile();
                    }
                }

                using (new EditorGUI.DisabledScope(profile.StatRegistry == null))
                {
                    if (GUILayout.Button("Sync Registry", GUILayout.Width(140f)))
                    {
                        SyncRegistry(profile.StatRegistry);
                    }
                }
            }

            if (profile.StatRegistry != null)
            {
                EditorGUILayout.LabelField("Registry Stats", $"{profile.StatRegistry.StatDefinitions.Count}");
                EditorGUILayout.LabelField("Registry Categories", $"{profile.StatRegistry.Categories.Count}");
                EditorGUILayout.LabelField("Registry Classes", $"{profile.StatRegistry.Classes.Count}");
                EditorGUILayout.LabelField("Registry Abilities", $"{profile.StatRegistry.Abilities.Count}");
            }
        }

        private static void DrawAssetCreationSection(DesignerToolkitProfile profile)
        {
            EditorGUILayout.LabelField("Asset Creation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Define default folders for new assets. Use the Create buttons to generate new ScriptableObjects in those locations.", MessageType.Info);

            DrawFolderField("Stat Definitions Folder", profile.StatDefinitionsFolder, value =>
            {
                profile.StatDefinitionsFolder = value;
                profile.SaveProfile();
            });
            DrawFolderField("Stat Categories Folder", profile.StatCategoriesFolder, value =>
            {
                profile.StatCategoriesFolder = value;
                profile.SaveProfile();
            });
            DrawFolderField("Stat Profiles Folder", profile.StatProfilesFolder, value =>
            {
                profile.StatProfilesFolder = value;
                profile.SaveProfile();
            });
            DrawFolderField("Class Definitions Folder", profile.ClassDefinitionsFolder, value =>
            {
                profile.ClassDefinitionsFolder = value;
                profile.SaveProfile();
            });
            DrawFolderField("Ability Definitions Folder", profile.AbilityDefinitionsFolder, value =>
            {
                profile.AbilityDefinitionsFolder = value;
                profile.SaveProfile();
            });

            EditorGUILayout.Space();

            DrawCreateRow("Stat Definition", profile.StatDefinitionsFolder, "StatDefinition", () =>
                CreateAsset<StatDefinition>("StatDefinition", profile.StatDefinitionsFolder));

            DrawCreateRow("Stat Category", profile.StatCategoriesFolder, "StatCategory", () =>
                CreateAsset<StatCategory>("StatCategory", profile.StatCategoriesFolder));

            DrawCreateRow("Stat Profile", profile.StatProfilesFolder, "StatProfile", () =>
                CreateAsset<StatProfileDefinition>("StatProfile", profile.StatProfilesFolder));

            DrawCreateRow("Class Definition", profile.ClassDefinitionsFolder, "ClassDefinition", () =>
                CreateAsset<ClassDefinition>("ClassDefinition", profile.ClassDefinitionsFolder));

            DrawCreateRow("Ability Definition", profile.AbilityDefinitionsFolder, "AbilityDefinition", () =>
                CreateAsset<AbilityDefinition>("AbilityDefinition", profile.AbilityDefinitionsFolder));
        }

        private static void DrawShortcutSection()
        {
            EditorGUILayout.LabelField("Shortcuts", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Jump directly into specialized editors for deeper configuration.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Stat Profile Studio", GUILayout.Width(170f)))
                {
                    StatProfileCreatorWindow.ShowWindow();
                }

                if (GUILayout.Button("Class Ability Planner", GUILayout.Width(170f)))
                {
                    ClassAbilityAssignmentWindow.ShowWindow();
                }

                if (GUILayout.Button("Ability Designer", GUILayout.Width(150f)))
                {
                    AbilityDesignerWindow.Open();
                }
            }
        }

        private static void DrawFolderField(string label, string currentValue, Action<string> onChange)
        {
            EditorGUI.BeginChangeCheck();
            var updatedValue = EditorGUILayout.TextField(label, currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                onChange?.Invoke(updatedValue);
            }
        }

        private static void DrawCreateRow(string label, string folder, string typeName, Action onCreate)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{label} Assets", GUILayout.Width(170f));
                EditorGUILayout.LabelField(CountAssets(typeName).ToString(), GUILayout.Width(60f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button($"Create {label}", GUILayout.Width(160f)))
                {
                    if (string.IsNullOrWhiteSpace(folder))
                    {
                        EditorUtility.DisplayDialog("Missing Folder", $"Provide a folder path before creating a {label}.", "OK");
                        return;
                    }

                    onCreate?.Invoke();
                }
            }
        }

        private static int CountAssets(string typeName)
        {
            var filter = typeName switch
            {
                "StatDefinition" => StatDefinitionFilter,
                "StatCategory" => StatCategoryFilter,
                "StatProfile" => StatProfileFilter,
                "ClassDefinition" => ClassDefinitionFilter,
                "AbilityDefinition" => AbilityDefinitionFilter,
                _ => string.Empty
            };

            return string.IsNullOrEmpty(filter) ? 0 : AssetDatabase.FindAssets(filter).Length;
        }

        private static void ResetDefaults(DesignerToolkitProfile profile)
        {
            profile.StatDefinitionsFolder = "Assets/ScriptableObjects/Stats";
            profile.StatCategoriesFolder = "Assets/ScriptableObjects/Stats";
            profile.StatProfilesFolder = "Assets/ScriptableObjects/Stats";
            profile.ClassDefinitionsFolder = "Assets/ScriptableObjects/Classes";
            profile.AbilityDefinitionsFolder = "Assets/ScriptableObjects/Abilities";
            profile.RegistryAssetPath = "Assets/ScriptableObjects/Stats/StatRegistry.asset";
            profile.SaveProfile();
        }

        private static StatRegistry CreateRegistryAsset(DesignerToolkitProfile profile)
        {
            var path = profile.RegistryAssetPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = "Assets/ScriptableObjects/Stats/StatRegistry.asset";
                profile.RegistryAssetPath = path;
            }

            EnsureFolder(Path.GetDirectoryName(path));
            var registry = CreateInstance<StatRegistry>();
            AssetDatabase.CreateAsset(registry, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = registry;
            EditorGUIUtility.PingObject(registry);
            return registry;
        }

        private static void SyncRegistry(StatRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            var statDefinitions = LoadAssets<StatDefinition>(StatDefinitionFilter);
            var categories = LoadAssets<StatCategory>(StatCategoryFilter);
            var classes = LoadAssets<ClassDefinition>(ClassDefinitionFilter);
            var abilities = LoadAssets<AbilityDefinition>(AbilityDefinitionFilter);

            var serializedObject = new SerializedObject(registry);
            ApplyList(serializedObject, "statDefinitions", statDefinitions);
            ApplyList(serializedObject, "categories", categories);
            ApplyList(serializedObject, "classes", classes);
            ApplyList(serializedObject, "abilities", abilities);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyList<T>(SerializedObject serializedObject, string propertyName, IReadOnlyList<T> assets)
            where T : UnityEngine.Object
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.arraySize = assets.Count;
            for (var i = 0; i < assets.Count; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                element.objectReferenceValue = assets[i];
            }
        }

        private static List<T> LoadAssets<T>(string filter) where T : UnityEngine.Object
        {
            var results = new List<T>();
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    results.Add(asset);
                }
            }

            return results;
        }

        private static T CreateAsset<T>(string defaultName, string folder) where T : ScriptableObject
        {
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
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

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
    }
}
