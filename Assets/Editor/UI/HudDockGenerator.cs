using System.IO;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.UI
{
    public static class HudDockGenerator
    {
        private const string PrefabPath = DockGeneratorUtility.MasterDockPrefabPath;
        private const string MenuRoot = "Tools/Realm/UI";

        [MenuItem(MenuRoot + "/Generate HUD Dock", priority = 120)]
        public static void GenerateHudDock()
        {
            var root = FindOrCreateDockRoot();
            DockGeneratorUtility.ConfigureMasterDockLayout(root, out _, out _, out _);

            SavePrefab(root);
            Selection.activeGameObject = root;
        }

        private static GameObject FindOrCreateDockRoot()
        {
            var existing = GameObject.Find("MasterDock");
            if (existing != null)
            {
                return existing;
            }

            return DockGeneratorUtility.InstantiateDockRoot("MasterDock", "Create HUD Dock");
        }

        private static void SavePrefab(GameObject root)
        {
            var directory = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        }
    }
}
