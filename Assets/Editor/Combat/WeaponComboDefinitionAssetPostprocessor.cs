using System.IO;
using Client.Combat;
using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.Combat
{
    public class WeaponComboDefinitionAssetPostprocessor : AssetPostprocessor
    {
        private const string ComboSearchFilter = "t:WeaponComboDefinition";

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets == null || importedAssets.Length == 0)
            {
                return;
            }

            foreach (var assetPath in importedAssets)
            {
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(assetPath);
                if (weapon == null)
                {
                    continue;
                }

                EnsureComboDefinition(weapon);
            }
        }

        private static void EnsureComboDefinition(WeaponDefinition weapon)
        {
            if (weapon == null || string.IsNullOrWhiteSpace(weapon.Guid))
            {
                return;
            }

            if (FindComboDefinition(weapon.Guid) != null)
            {
                return;
            }

            var comboDefinition = ScriptableObject.CreateInstance<WeaponComboDefinition>();
            var serialized = new SerializedObject(comboDefinition);
            serialized.FindProperty("weaponId").stringValue = weapon.Guid;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var weaponPath = AssetDatabase.GetAssetPath(weapon);
            var directory = string.IsNullOrWhiteSpace(weaponPath)
                ? "Assets"
                : Path.GetDirectoryName(weaponPath);
            var assetName = $"{weapon.name}_Combo";
            var comboPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, $"{assetName}.asset"));
            AssetDatabase.CreateAsset(comboDefinition, comboPath);
            AssetDatabase.SaveAssets();
        }

        private static WeaponComboDefinition FindComboDefinition(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return null;
            }

            var guids = AssetDatabase.FindAssets(ComboSearchFilter);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var combo = AssetDatabase.LoadAssetAtPath<WeaponComboDefinition>(path);
                if (combo != null && string.Equals(combo.WeaponId, weaponId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return combo;
                }
            }

            return null;
        }
    }
}
