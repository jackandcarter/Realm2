#if UNITY_EDITOR
using System.Collections.Generic;
using Building;
using Client.Terrain;
using Digger.Modules.Core.Sources;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditorInternal;

namespace Building.Editor
{
    public class BuildableZoneTool : EditorWindow
    {
        private const float DefaultZoneHeight = 6f;
        private const float DefaultSurfaceOffset = 1f;

        private static readonly Color ExistingZoneColor = new Color(0.1f, 0.7f, 0.2f, 0.65f);
        private static readonly Color PreviewOutlineColor = new Color(0.0f, 0.6f, 1f, 0.85f);
        private static readonly Color PreviewFillColor = new Color(0.0f, 0.6f, 1f, 0.15f);

        private BuildableZoneAsset _targetAsset;
        private DiggerSystem _targetDigger;
        private float _zoneHeight = DefaultZoneHeight;
        private float _surfaceOffset = DefaultSurfaceOffset;
        private LayerMask _placementMask = ~0;
        private bool _isPlacing;
        private bool _isDragging;
        private Vector3 _dragStart;
        private Vector3 _dragCurrent;

        [MenuItem("Tools/Realm/Buildable Zone Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<BuildableZoneTool>("Buildable Zones");
            window.minSize = new Vector2(320f, 280f);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DuringSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGui;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Outline buildable zones directly in the Scene view. Drag to define an area, then release to save it into the asset.", MessageType.Info);
            _targetAsset = (BuildableZoneAsset)EditorGUILayout.ObjectField(new GUIContent("Zone Asset", "BuildableZoneAsset that stores all authored zones."), _targetAsset, typeof(BuildableZoneAsset), false);
            _targetDigger = (DiggerSystem)EditorGUILayout.ObjectField(new GUIContent("Target Digger", "Optional terrain system used to project placement onto the surface."), _targetDigger, typeof(DiggerSystem), true);
            _zoneHeight = EditorGUILayout.FloatField(new GUIContent("Zone Height", "Vertical height of the buildable volume in meters."), Mathf.Max(0.1f, _zoneHeight));
            _surfaceOffset = EditorGUILayout.FloatField(new GUIContent("Surface Offset", "Offset applied above the sampled surface when placing zones."), _surfaceOffset);
            _placementMask = LayerMaskField(new GUIContent("Placement Mask", "Layer mask used when raycasting the terrain for placement."), _placementMask);

            if (_targetAsset != null && GUILayout.Button(new GUIContent("Sync Scene Metadata", "Store the current scene name and metadata on the zone asset.")))
            {
                SyncSceneMetadata();
            }

            EditorGUI.BeginDisabledGroup(_targetAsset == null);
            if (GUILayout.Button(new GUIContent(_isPlacing ? "Cancel Placement" : "Start Placement", "Toggle scene placement mode to draw a new zone.")))
            {
                TogglePlacement();
            }

            if (GUILayout.Button(new GUIContent("Clear All Zones", "Remove every zone from the current asset.")))
            {
                ClearZones();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            DrawZoneList();
        }

        private void DrawZoneList()
        {
            if (_targetAsset == null)
            {
                EditorGUILayout.HelpBox("Assign a Buildable Zone Asset to begin.", MessageType.Info);
                return;
            }

            var zones = _targetAsset.Zones;
            if (zones == null || zones.Count == 0)
            {
                EditorGUILayout.HelpBox("No zones defined. Use Start Placement to outline a zone in the Scene view.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Defined Zones", EditorStyles.boldLabel);
            for (var i = 0; i < zones.Count; i++)
            {
                var bounds = zones[i].ToBounds();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Zone {i + 1}");
                EditorGUILayout.LabelField("Center", bounds.center.ToString("F2"));
                EditorGUILayout.LabelField("Size", bounds.size.ToString("F2"));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Focus", "Frame the selected zone in the Scene view.")))
                {
                    SceneView.lastActiveSceneView?.Frame(bounds, false);
                }

                if (GUILayout.Button(new GUIContent("Remove", "Delete this zone from the asset.")))
                {
                    RemoveZoneAt(i);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        private void DuringSceneGui(SceneView sceneView)
        {
            if (_targetAsset == null)
            {
                return;
            }

            DrawExistingZones();

            if (!_isPlacing)
            {
                return;
            }

            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current;

            switch (e.type)
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlId);
                    break;
                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt)
                    {
                        if (TryGetTerrainPoint(e.mousePosition, out _dragStart))
                        {
                            _dragCurrent = _dragStart;
                            _isDragging = true;
                            GUIUtility.hotControl = controlId;
                            e.Use();
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    if (_isDragging && GUIUtility.hotControl == controlId && TryGetTerrainPoint(e.mousePosition, out var dragPoint))
                    {
                        _dragCurrent = dragPoint;
                        sceneView.Repaint();
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId && e.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        if (_isDragging)
                        {
                            FinalizeZone();
                        }

                        _isDragging = false;
                        e.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Escape)
                    {
                        _isDragging = false;
                        GUIUtility.hotControl = 0;
                        TogglePlacement();
                        e.Use();
                    }
                    break;
                case EventType.Repaint:
                    if (_isDragging)
                    {
                        DrawPreview();
                    }
                    break;
            }
        }

        private void DrawExistingZones()
        {
            Handles.color = ExistingZoneColor;
            var zones = _targetAsset.Zones;
            if (zones == null)
            {
                return;
            }

            foreach (var serializableBounds in zones)
            {
                var bounds = serializableBounds.ToBounds();
                Handles.DrawWireCube(bounds.center, bounds.size);
                var corners = GetTopRectangle(bounds);
                Handles.DrawSolidRectangleWithOutline(corners, ExistingZoneColor, Color.green);
            }
        }

        private void DrawPreview()
        {
            Handles.color = PreviewOutlineColor;
            var min = Vector3.Min(_dragStart, _dragCurrent);
            var max = Vector3.Max(_dragStart, _dragCurrent);

            var size = new Vector3(Mathf.Max(0.1f, max.x - min.x), Mathf.Max(0.1f, _zoneHeight), Mathf.Max(0.1f, max.z - min.z));
            var center = new Vector3((min.x + max.x) * 0.5f, Mathf.Min(min.y, max.y) + _surfaceOffset + size.y * 0.5f, (min.z + max.z) * 0.5f);

            Handles.DrawWireCube(center, size);
            var topRect = GetRectangle(min, max, center.y + size.y * 0.5f);
            Handles.DrawSolidRectangleWithOutline(topRect, PreviewFillColor, PreviewOutlineColor);
        }

        private void FinalizeZone()
        {
            if (_targetAsset == null)
            {
                return;
            }

            var min = Vector3.Min(_dragStart, _dragCurrent);
            var max = Vector3.Max(_dragStart, _dragCurrent);
            if (Mathf.Approximately(min.x, max.x) || Mathf.Approximately(min.z, max.z))
            {
                return;
            }

            var size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Max(0.1f, _zoneHeight), Mathf.Abs(max.z - min.z));
            var center = new Vector3((min.x + max.x) * 0.5f, Mathf.Min(min.y, max.y) + _surfaceOffset + size.y * 0.5f, (min.z + max.z) * 0.5f);
            var bounds = new Bounds(center, size);

            Undo.RecordObject(_targetAsset, "Add Buildable Zone");
            _targetAsset.AddZone(bounds);
            EditorUtility.SetDirty(_targetAsset);
            SceneView.RepaintAll();
        }

        private void TogglePlacement()
        {
            _isPlacing = !_isPlacing;
            if (!_isPlacing)
            {
                _isDragging = false;
                GUIUtility.hotControl = 0;
            }
        }

        private void ClearZones()
        {
            if (_targetAsset == null)
            {
                return;
            }

            Undo.RecordObject(_targetAsset, "Clear Buildable Zones");
            _targetAsset.SetZones(new List<Bounds>());
            EditorUtility.SetDirty(_targetAsset);
            SceneView.RepaintAll();
        }

        private void RemoveZoneAt(int index)
        {
            if (_targetAsset == null)
            {
                return;
            }

            Undo.RecordObject(_targetAsset, "Remove Buildable Zone");
            _targetAsset.RemoveAt(index);
            EditorUtility.SetDirty(_targetAsset);
            SceneView.RepaintAll();
        }

        private void SyncSceneMetadata()
        {
            if (_targetAsset == null)
            {
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            Undo.RecordObject(_targetAsset, "Sync Buildable Zone Metadata");
            _targetAsset.SetSceneName(activeScene.name);
            _targetAsset.SetRegionId(activeScene.path);
            EditorUtility.SetDirty(_targetAsset);
        }

        private bool TryGetTerrainPoint(Vector2 mousePosition, out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            if (Physics.Raycast(ray, out var hit, 5000f, _placementMask))
            {
                worldPoint = hit.point;
                return true;
            }

            var up = Vector3.up;
            var origin = Vector3.zero;
            if (_targetDigger != null)
            {
                up = _targetDigger.transform.up;
                origin = _targetDigger.transform.position;
            }

            var plane = new Plane(up, origin);
            if (plane.Raycast(ray, out var enter))
            {
                worldPoint = ray.origin + ray.direction * enter;
                return true;
            }

            return false;
        }

        private static Vector3[] GetTopRectangle(Bounds bounds)
        {
            return GetRectangle(bounds.min, bounds.max, bounds.max.y);
        }

        private static Vector3[] GetRectangle(Vector3 min, Vector3 max, float y)
        {
            return new[]
            {
                new Vector3(min.x, y, min.z),
                new Vector3(min.x, y, max.z),
                new Vector3(max.x, y, max.z),
                new Vector3(max.x, y, min.z)
            };
        }

        private static LayerMask LayerMaskField(GUIContent label, LayerMask mask)
        {
            var layers = InternalEditorUtility.layers;
            var layerNumbers = new List<int>();
            for (var i = 0; i < layers.Length; i++)
            {
                layerNumbers.Add(LayerMask.NameToLayer(layers[i]));
            }

            var maskWithoutEmpty = 0;
            for (var i = 0; i < layerNumbers.Count; i++)
            {
                if ((mask.value & (1 << layerNumbers[i])) != 0)
                {
                    maskWithoutEmpty |= 1 << i;
                }
            }

            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);
            var newMask = 0;
            for (var i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) != 0)
                {
                    newMask |= 1 << layerNumbers[i];
                }
            }

            mask.value = newMask;
            return mask;
        }
    }
}
#endif
