using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.Configuration
{
    internal static class ConfigurationAssetCloneUtility
    {
        private const string MenuPath = "Assets/Realm/Clone Configuration";

        [MenuItem(MenuPath, false, 2000)]
        private static void CloneSelected()
        {
            var selectedAssets = GetSelectedConfigurationAssets();
            if (selectedAssets.Count == 0)
            {
                EditorUtility.DisplayDialog("Clone Configuration", "Select at least one configuration asset to clone.", "OK");
                return;
            }

            var createdAssets = new List<UnityEngine.Object>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var original in selectedAssets)
                {
                    var clone = UnityEngine.Object.Instantiate(original);
                    clone.name = $"{original.name} Variant";

                    var originalPath = AssetDatabase.GetAssetPath(original);
                    var directory = Path.GetDirectoryName(originalPath);
                    var proposedFileName = string.IsNullOrEmpty(directory)
                        ? $"{clone.name}.asset"
                        : Path.Combine(directory, $"{clone.name}.asset").Replace('\\', '/');
                    var uniquePath = AssetDatabase.GenerateUniqueAssetPath(proposedFileName);

                    AssetDatabase.CreateAsset(clone, uniquePath);
                    createdAssets.Add(clone);

                    if (clone is ConfigurationAsset configurationAsset)
                    {
                        configurationAsset.RecordCloneCreation();
                    }

                    RegenerateGuidIfPresent(clone);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (createdAssets.Count > 0)
            {
                Selection.objects = createdAssets.ToArray();
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateClone()
        {
            return Selection.objects.Any(IsConfigurationAssetSelection);
        }

        private static bool IsConfigurationAssetSelection(UnityEngine.Object candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            var path = AssetDatabase.GetAssetPath(candidate);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return AssetDatabase.LoadAssetAtPath<ConfigurationAsset>(path) != null;
        }

        private static List<ConfigurationAsset> GetSelectedConfigurationAssets()
        {
            var results = new List<ConfigurationAsset>();
            var seen = new HashSet<ConfigurationAsset>();

            foreach (var obj in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<ConfigurationAsset>(path);
                if (asset != null && seen.Add(asset))
                {
                    results.Add(asset);
                }
            }

            return results;
        }

        private static void RegenerateGuidIfPresent(UnityEngine.Object asset)
        {
            if (asset is not ConfigurationAsset configurationAsset)
            {
                return;
            }

            if (asset is not IGuidIdentified)
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
            configurationAsset.RecordManualModification();
        }
    }
}
