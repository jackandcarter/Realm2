using System;
using System.Collections.Generic;
using System.IO;
using Realm.Data;
using Realm.EditorTools;
using UnityEditor;
using UnityEngine;
using AbilityDefinition = Realm.Abilities.AbilityDefinition;

namespace Realm.Editor.DesignerTools
{
    public class DesignerToolkitWindow : EditorWindow
    {
        private const string StatDefinitionFilter = "t:Realm.Data.StatDefinition";
        private const string StatCategoryFilter = "t:Realm.Data.StatCategory";
        private const string StatProfileFilter = "t:Realm.Data.StatProfileDefinition";
        private const string ClassDefinitionFilter = "t:Realm.Data.ClassDefinition";
        private const string AbilityDefinitionFilter = "t:Realm.Abilities.AbilityDefinition";
        private const string WeaponTypeDefinitionFilter = "t:Realm.Data.WeaponTypeDefinition";
        private const string ArmorTypeDefinitionFilter = "t:Realm.Data.ArmorTypeDefinition";
        private const string WeaponDefinitionFilter = "t:Realm.Data.WeaponDefinition";
        private const string ArmorDefinitionFilter = "t:Realm.Data.ArmorDefinition";

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
                if (GUILayout.Button(new GUIContent("Save Profile", "Persist the current toolkit folder paths and registry selection."), EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    profile.SaveProfile();
                }

                if (GUILayout.Button(new GUIContent("Reset Defaults", "Restore the toolkit profile to the default folders and registry settings."), EditorStyles.toolbarButton, GUILayout.Width(110f)))
                {
                    ResetDefaults(profile);
                }

                GUILayout.FlexibleSpace();
            }
        }

        private static void DrawRegistrySection(DesignerToolkitProfile profile)
        {
            EditorGUILayout.LabelField("Registry", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("The registry aggregates stat, class, ability, and equipment assets for runtime lookups. Sync it whenever new assets are created.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            var registry = (StatRegistry)EditorGUILayout.ObjectField(new GUIContent("Stat Registry", "Registry asset used at runtime for stat, class, and ability lookups."), profile.StatRegistry, typeof(StatRegistry), false);
            if (EditorGUI.EndChangeCheck())
            {
                profile.StatRegistry = registry;
                profile.SaveProfile();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(profile.StatRegistry != null))
                {
                    if (GUILayout.Button(new GUIContent("Create Registry", "Create a new StatRegistry asset in the configured folder."), GUILayout.Width(140f)))
                    {
                        profile.StatRegistry = CreateRegistryAsset(profile);
                        profile.SaveProfile();
                    }
                }

                using (new EditorGUI.DisabledScope(profile.StatRegistry == null))
                {
                    if (GUILayout.Button(new GUIContent("Sync Registry", "Scan the project and refresh the registry asset with current data."), GUILayout.Width(140f)))
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
                EditorGUILayout.LabelField("Registry Weapon Types", $"{profile.StatRegistry.WeaponTypes.Count}");
                EditorGUILayout.LabelField("Registry Armor Types", $"{profile.StatRegistry.ArmorTypes.Count}");
                EditorGUILayout.LabelField("Registry Weapons", $"{profile.StatRegistry.Weapons.Count}");
                EditorGUILayout.LabelField("Registry Armors", $"{profile.StatRegistry.Armors.Count}");
            }
        }

        private static void DrawAssetCreationSection(DesignerToolkitProfile profile)
        {
            EditorGUILayout.LabelField("Asset Creation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Define default folders for new assets. Use the Create buttons to generate new ScriptableObjects in those locations.", MessageType.Info);

            DrawFolderField(new GUIContent("Stat Definitions Folder", "Default folder path for new StatDefinition assets."), profile.StatDefinitionsFolder, value =>
            {
                profile.StatDefinitionsFolder = value;
                profile.SaveProfile();
            });
            DrawFolderField(new GUIContent("Stat Categories Folder", "Default folder path for new StatCategory assets."), profile.StatCategoriesFolder, value =>
            {
                profile.StatCategoriesFolder = value;
                profile.SaveProfile();
            });
            DrawFolderField(new GUIContent("Stat Profiles Folder", "Default folder path for new StatProfileDefinition assets."), profile.StatProfilesFolder, value =>
            {
                profile.StatProfilesFolder = value;
                profile.SaveProfile();
            });
            DrawFolderField(new GUIContent("Class Definitions Folder", "Default folder path for new ClassDefinition assets."), profile.ClassDefinitionsFolder, value =>
            {
                profile.ClassDefinitionsFolder = value;
                profile.SaveProfile();
            });
            DrawFolderField(new GUIContent("Ability Definitions Folder", "Default folder path for new AbilityDefinition assets."), profile.AbilityDefinitionsFolder, value =>
            {
                profile.AbilityDefinitionsFolder = value;
                profile.SaveProfile();
            });
            DrawFolderField(new GUIContent("Weapon Type Definitions Folder", "Default folder path for new WeaponTypeDefinition assets."), profile.WeaponTypeDefinitionsFolder, value =>
            {
                profile.WeaponTypeDefinitionsFolder = value;
                profile.SaveProfile();
            });
            DrawFolderField(new GUIContent("Armor Type Definitions Folder", "Default folder path for new ArmorTypeDefinition assets."), profile.ArmorTypeDefinitionsFolder, value =>
            {
                profile.ArmorTypeDefinitionsFolder = value;
                profile.SaveProfile();
            });
            DrawFolderField(new GUIContent("Weapon Definitions Folder", "Default folder path for new WeaponDefinition assets."), profile.WeaponDefinitionsFolder, value =>
            {
                profile.WeaponDefinitionsFolder = value;
                profile.SaveProfile();
            });
            DrawFolderField(new GUIContent("Armor Definitions Folder", "Default folder path for new ArmorDefinition assets."), profile.ArmorDefinitionsFolder, value =>
            {
                profile.ArmorDefinitionsFolder = value;
                profile.SaveProfile();
            });

            EditorGUILayout.Space();

            DrawCreateRow(new GUIContent("Stat Definition", "Create a new StatDefinition in the configured folder."), profile.StatDefinitionsFolder, "StatDefinition", () =>
                CreateAsset<StatDefinition>("StatDefinition", profile.StatDefinitionsFolder));

            DrawCreateRow(new GUIContent("Stat Category", "Create a new StatCategory in the configured folder."), profile.StatCategoriesFolder, "StatCategory", () =>
                CreateAsset<StatCategory>("StatCategory", profile.StatCategoriesFolder));

            DrawCreateRow(new GUIContent("Stat Profile", "Create a new StatProfileDefinition in the configured folder."), profile.StatProfilesFolder, "StatProfile", () =>
                CreateAsset<Realm.Data.StatProfileDefinition>("StatProfile", profile.StatProfilesFolder));

            DrawCreateRow(new GUIContent("Class Definition", "Create a new ClassDefinition in the configured folder."), profile.ClassDefinitionsFolder, "ClassDefinition", () =>
                CreateAsset<ClassDefinition>("ClassDefinition", profile.ClassDefinitionsFolder));

            DrawCreateRow(new GUIContent("Ability Definition", "Create a new AbilityDefinition in the configured folder."), profile.AbilityDefinitionsFolder, "AbilityDefinition", () =>
                CreateAsset<AbilityDefinition>("AbilityDefinition", profile.AbilityDefinitionsFolder));

            DrawCreateRow(new GUIContent("Weapon Type", "Create a new WeaponTypeDefinition in the configured folder."), profile.WeaponTypeDefinitionsFolder, "WeaponTypeDefinition", () =>
                CreateAsset<WeaponTypeDefinition>("WeaponTypeDefinition", profile.WeaponTypeDefinitionsFolder));

            DrawCreateRow(new GUIContent("Armor Type", "Create a new ArmorTypeDefinition in the configured folder."), profile.ArmorTypeDefinitionsFolder, "ArmorTypeDefinition", () =>
                CreateAsset<ArmorTypeDefinition>("ArmorTypeDefinition", profile.ArmorTypeDefinitionsFolder));

            DrawCreateRow(new GUIContent("Weapon", "Create a new WeaponDefinition in the configured folder."), profile.WeaponDefinitionsFolder, "WeaponDefinition", () =>
                CreateWeaponDefinition(profile.WeaponDefinitionsFolder));

            DrawCreateRow(new GUIContent("Armor", "Create a new ArmorDefinition in the configured folder."), profile.ArmorDefinitionsFolder, "ArmorDefinition", () =>
                CreateAsset<ArmorDefinition>("ArmorDefinition", profile.ArmorDefinitionsFolder));
        }

        private static void DrawShortcutSection()
        {
            EditorGUILayout.LabelField("Shortcuts", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Jump directly into specialized editors for deeper configuration.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Stat Profile Studio", "Open the stat profile editor for curve and formula setup."), GUILayout.Width(170f)))
                {
                    Realm.Editor.DesignerTools.StatProfileCreatorWindow.ShowWindow();
                }

                if (GUILayout.Button(new GUIContent("Class Ability Planner", "Assign abilities and unlock conditions per class."), GUILayout.Width(170f)))
                {
                    ClassAbilityAssignmentWindow.ShowWindow();
                }

                if (GUILayout.Button(new GUIContent("Ability Designer", "Open the ability definition editor."), GUILayout.Width(150f)))
                {
                    AbilityDesignerWindow.Open();
                }

                if (GUILayout.Button(new GUIContent("Equipment Studio", "Open the equipment editor for weapons and armor."), GUILayout.Width(170f)))
                {
                    EquipmentDesignerWindow.ShowWindow();
                }
            }
        }

        private static void DrawFolderField(GUIContent label, string currentValue, Action<string> onChange)
        {
            EditorGUI.BeginChangeCheck();
            var updatedValue = EditorGUILayout.TextField(label, currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                onChange?.Invoke(updatedValue);
            }
        }

        private static void DrawCreateRow(GUIContent label, string folder, string typeName, Action onCreate)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent($"{label.text} Assets", label.tooltip), GUILayout.Width(170f));
                EditorGUILayout.LabelField(CountAssets(typeName).ToString(), GUILayout.Width(60f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent($"Create {label.text}", label.tooltip), GUILayout.Width(160f)))
                {
                    if (string.IsNullOrWhiteSpace(folder))
                    {
                        EditorUtility.DisplayDialog("Missing Folder", $"Provide a folder path before creating a {label.text}.", "OK");
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
                "WeaponTypeDefinition" => WeaponTypeDefinitionFilter,
                "ArmorTypeDefinition" => ArmorTypeDefinitionFilter,
                "WeaponDefinition" => WeaponDefinitionFilter,
                "ArmorDefinition" => ArmorDefinitionFilter,
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
            profile.WeaponTypeDefinitionsFolder = "Assets/ScriptableObjects/Equipment/WeaponTypes";
            profile.ArmorTypeDefinitionsFolder = "Assets/ScriptableObjects/Equipment/ArmorTypes";
            profile.WeaponDefinitionsFolder = "Assets/ScriptableObjects/Equipment/Weapons";
            profile.ArmorDefinitionsFolder = "Assets/ScriptableObjects/Equipment/Armors";
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
            var weaponTypes = LoadAssets<WeaponTypeDefinition>(WeaponTypeDefinitionFilter);
            var armorTypes = LoadAssets<ArmorTypeDefinition>(ArmorTypeDefinitionFilter);
            var weapons = LoadAssets<WeaponDefinition>(WeaponDefinitionFilter);
            var armors = LoadAssets<ArmorDefinition>(ArmorDefinitionFilter);

            var serializedObject = new SerializedObject(registry);
            ApplyList(serializedObject, "statDefinitions", statDefinitions);
            ApplyList(serializedObject, "categories", categories);
            ApplyList(serializedObject, "classes", classes);
            ApplyList(serializedObject, "abilities", abilities);
            ApplyList(serializedObject, "weaponTypes", weaponTypes);
            ApplyList(serializedObject, "armorTypes", armorTypes);
            ApplyList(serializedObject, "weapons", weapons);
            ApplyList(serializedObject, "armors", armors);
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

        private static WeaponDefinition CreateWeaponDefinition(string folder)
        {
            EnsureFolder(folder);
            var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, "WeaponDefinition.asset"));
            var asset = CreateInstance<WeaponDefinition>();
            AssetDatabase.CreateAsset(asset, path);

            var serializedObject = new SerializedObject(asset);
            var baseDamage = serializedObject.FindProperty("baseDamage");
            if (baseDamage != null)
            {
                baseDamage.floatValue = 10f;
            }

            ApplyAttackProfile(serializedObject.FindProperty("lightAttack"), WeaponAttackProfile.DefaultLight);
            ApplyAttackProfile(serializedObject.FindProperty("mediumAttack"), WeaponAttackProfile.DefaultMedium);
            ApplyAttackProfile(serializedObject.FindProperty("heavyAttack"), WeaponAttackProfile.DefaultHeavy);

            var specialAttack = serializedObject.FindProperty("specialAttack");
            if (specialAttack != null)
            {
                specialAttack.objectReferenceValue = null;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        private static void ApplyAttackProfile(SerializedProperty property, WeaponAttackProfile profile)
        {
            if (property == null)
            {
                return;
            }

            var damageMultiplier = property.FindPropertyRelative("damageMultiplier");
            if (damageMultiplier != null)
            {
                damageMultiplier.floatValue = profile.DamageMultiplier;
            }

            var accuracy = property.FindPropertyRelative("accuracy");
            if (accuracy != null)
            {
                accuracy.floatValue = profile.Accuracy;
            }

            var windup = property.FindPropertyRelative("windupSeconds");
            if (windup != null)
            {
                windup.floatValue = profile.WindupSeconds;
            }

            var recovery = property.FindPropertyRelative("recoverySeconds");
            if (recovery != null)
            {
                recovery.floatValue = profile.RecoverySeconds;
            }
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
