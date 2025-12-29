using System.IO;
using Client.UI.HUD;
using Client.UI.Maps;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Realm.Editor.UI
{
    public static class GameplayHudGenerator
    {
        private const string PrefabPath = "Assets/UI/HUD/GameplayHud.prefab";
        private const string MiniMapPrefabPath = "Assets/UI/HUD/MiniMapPanel.prefab";
        private const string WorldMapPrefabPath = "Assets/UI/Maps/WorldMapOverlay.prefab";
        private const string MenuRoot = "Tools/Realm/UI";

        [MenuItem(MenuRoot + "/Generate Gameplay HUD", priority = 110)]
        public static void GenerateGameplayHud()
        {
            var controller = FindOrCreateHudRoot();
            var root = controller.gameObject;

            EnsureEventSystem();
            ConfigureCanvas(root.GetComponent<Canvas>(), root.GetComponent<RectTransform>(), root.GetComponent<CanvasScaler>());

            var classDockAnchor = FindOrCreateChildRect(root.transform, "ClassDockAnchor");
            ConfigureClassDockAnchor(classDockAnchor);

            var worldMap = EnsurePrefabChild(root.transform, WorldMapPrefabPath, "WorldMapOverlay");
            var miniMap = EnsurePrefabChild(root.transform, MiniMapPrefabPath, "MiniMapPanel");

            BindMapReferences(miniMap, worldMap);
            ApplyControllerBindings(controller, root.GetComponent<Canvas>(), classDockAnchor);

            SavePrefab(root);
            Selection.activeGameObject = root;
        }

        private static GameplayHudController FindOrCreateHudRoot()
        {
            var existing = Object.FindFirstObjectByType<GameplayHudController>();
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("GameplayHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameplayHudController));
            Undo.RegisterCreatedObjectUndo(root, "Create Gameplay HUD");
            ApplyUiLayer(root);
            return root.GetComponent<GameplayHudController>();
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                return;
            }

            var system = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(system, "Create EventSystem");
        }

        private static void ConfigureCanvas(Canvas canvas, RectTransform rectTransform, CanvasScaler scaler)
        {
            if (canvas == null || rectTransform == null || scaler == null)
            {
                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        private static RectTransform FindOrCreateChildRect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing as RectTransform;
            }

            var child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, "Create HUD Element");
            child.transform.SetParent(parent, false);
            ApplyUiLayer(child);
            return child.GetComponent<RectTransform>();
        }

        private static void ConfigureClassDockAnchor(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 24f);
            rect.sizeDelta = new Vector2(640f, 180f);
        }

        private static GameObject EnsurePrefabChild(Transform parent, string prefabPath, string fallbackName)
        {
            var existing = parent.Find(fallbackName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                return null;
            }

            instance.name = prefab.name;
            ApplyUiLayer(instance);
            return instance;
        }

        private static void BindMapReferences(GameObject miniMapObject, GameObject worldMapObject)
        {
            if (miniMapObject == null || worldMapObject == null)
            {
                return;
            }

            if (!miniMapObject.TryGetComponent(out MiniMapController miniMap))
            {
                return;
            }

            if (!worldMapObject.TryGetComponent(out WorldMapOverlayController worldMap))
            {
                return;
            }

            var serialized = new SerializedObject(miniMap);
            serialized.FindProperty("worldMapOverlay").objectReferenceValue = worldMap;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyControllerBindings(GameplayHudController controller, Canvas canvas, RectTransform classDockAnchor)
        {
            if (controller == null)
            {
                return;
            }

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("mainCanvas").objectReferenceValue = canvas;
            serialized.FindProperty("classDockAnchor").objectReferenceValue = classDockAnchor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
