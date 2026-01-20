using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.UI
{
    internal static class UiPrefabAuditUtility
    {
        private static readonly string[] HudPrefabPaths =
        {
            "Assets/UI/HUD/GameplayHud.prefab",
            "Assets/UI/HUD/MiniMapPanel.prefab",
            "Assets/UI/Maps/WorldMapOverlay.prefab",
            "Assets/UI/Shared/Dock/MasterDock.prefab",
            "Assets/UI/Arkitect/ArkitectCanvas.prefab"
        };

        internal static void ValidateHudPrefabs()
        {
            AuditPrefabs(HudPrefabPaths, removeMissingScripts: false);
        }

        internal static void CleanHudPrefabs()
        {
            AuditPrefabs(HudPrefabPaths, removeMissingScripts: true);
        }

        private static void AuditPrefabs(IEnumerable<string> prefabPaths, bool removeMissingScripts)
        {
            if (prefabPaths == null)
            {
                return;
            }

            var totalMissing = 0;
            var totalPrefabs = 0;
            foreach (var path in prefabPaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"HUD prefab not found at '{path}'.");
                    continue;
                }

                totalPrefabs++;
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var missingCount = CountMissingScripts(contents, removeMissingScripts);
                    totalMissing += missingCount;

                    if (missingCount > 0)
                    {
                        if (removeMissingScripts)
                        {
                            PrefabUtility.SaveAsPrefabAsset(contents, path);
                            Debug.Log($"{path}: removed {missingCount} missing script component(s).");
                        }
                        else
                        {
                            Debug.Log($"{path}: found {missingCount} missing script component(s).");
                        }
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            if (totalPrefabs == 0)
            {
                Debug.LogWarning("No HUD prefabs were found to audit.");
                return;
            }

            var actionLabel = removeMissingScripts ? "Removed" : "Found";
            Debug.Log($"{actionLabel} {totalMissing} missing script component(s) across {totalPrefabs} HUD prefab(s).");
        }

        private static int CountMissingScripts(GameObject root, bool removeMissingScripts)
        {
            if (root == null)
            {
                return 0;
            }

            var missingCount = 0;
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (removeMissingScripts)
                {
                    missingCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
                    continue;
                }

                var components = transform.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        missingCount++;
                    }
                }
            }

            return missingCount;
        }
    }
}
