using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Client.Biomes.Editor
{
    [CustomEditor(typeof(BiomePreset))]
    public class BiomePresetEditor : UnityEditor.Editor
    {
        private SerializedProperty layersProperty;
        private SerializedProperty cavesProperty;
        private ReorderableList layerList;
        private TerrainLayer[] cachedTerrainLayers = Array.Empty<TerrainLayer>();

        private void OnEnable()
        {
            layersProperty = serializedObject.FindProperty("layers");
            cavesProperty = serializedObject.FindProperty("caves");
            layerList = new ReorderableList(serializedObject, layersProperty, true, true, true, true)
            {
                drawHeaderCallback = DrawLayersHeader,
                drawElementCallback = DrawLayerElement,
                onAddCallback = AddLayer,
                elementHeightCallback = _ => GetElementHeight()
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            cachedTerrainLayers = FindTerrainLayers();

            if (cachedTerrainLayers.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No Terrain layers found in the active scene. Add Terrain Layers to a Unity Terrain to enable selection.",
                    MessageType.Warning);
            }

            layerList.DoLayoutList();
            EditorGUILayout.PropertyField(cavesProperty, true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLayersHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Biome Layers");
        }

        private void DrawLayerElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = layersProperty.GetArrayElementAtIndex(index);
            var textureIndex = element.FindPropertyRelative("textureIndex");
            var minHeight = element.FindPropertyRelative("minHeight");
            var maxHeight = element.FindPropertyRelative("maxHeight");
            var heightFalloff = element.FindPropertyRelative("heightFalloff");
            var minSlope = element.FindPropertyRelative("minSlope");
            var maxSlope = element.FindPropertyRelative("maxSlope");
            var noiseAmplitude = element.FindPropertyRelative("noiseAmplitude");
            var noiseFrequency = element.FindPropertyRelative("noiseFrequency");

            rect.height = EditorGUIUtility.singleLineHeight;
            var lineRect = rect;

            using (new EditorGUI.DisabledScope(cachedTerrainLayers.Length == 0))
            {
                textureIndex.intValue = DrawTextureSelector(lineRect, textureIndex.intValue);
            }

            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            DrawMinMaxFields(lineRect, "Height", minHeight, maxHeight);

            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            DrawMinMaxFields(lineRect, "Slope", minSlope, maxSlope);

            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            heightFalloff.floatValue = EditorGUI.FloatField(lineRect, "Height Falloff", heightFalloff.floatValue);

            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            noiseAmplitude.floatValue = EditorGUI.FloatField(lineRect, "Noise Amplitude", noiseAmplitude.floatValue);

            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            noiseFrequency.floatValue = EditorGUI.FloatField(lineRect, "Noise Frequency", noiseFrequency.floatValue);
        }

        private float GetElementHeight()
        {
            var lineCount = 6f;
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * lineCount;
        }

        private void AddLayer(ReorderableList list)
        {
            layersProperty.arraySize++;
            var newElement = layersProperty.GetArrayElementAtIndex(layersProperty.arraySize - 1);
            newElement.FindPropertyRelative("textureIndex").intValue = cachedTerrainLayers.Length > 0 ? 0 : -1;
            newElement.FindPropertyRelative("minHeight").floatValue = 0f;
            newElement.FindPropertyRelative("maxHeight").floatValue = 1f;
            newElement.FindPropertyRelative("heightFalloff").floatValue = 0.1f;
            newElement.FindPropertyRelative("minSlope").floatValue = 0f;
            newElement.FindPropertyRelative("maxSlope").floatValue = 45f;
            newElement.FindPropertyRelative("noiseAmplitude").floatValue = 0f;
            newElement.FindPropertyRelative("noiseFrequency").floatValue = 0f;
        }

        private int DrawTextureSelector(Rect rect, int currentIndex)
        {
            var options = BuildTerrainLayerOptions();
            if (options.Count == 0)
            {
                EditorGUI.LabelField(rect, "Terrain Layer", "No layers available");
                return currentIndex;
            }

            var clampedIndex = Mathf.Clamp(currentIndex, 0, options.Count - 1);
            return EditorGUI.Popup(rect, "Terrain Layer", clampedIndex, options.ToArray());
        }

        private static void DrawMinMaxFields(Rect rect, string label, SerializedProperty min, SerializedProperty max)
        {
            var half = rect.width * 0.5f;
            var left = new Rect(rect.x, rect.y, half - 2f, rect.height);
            var right = new Rect(rect.x + half + 2f, rect.y, half - 2f, rect.height);
            EditorGUI.PropertyField(left, min, new GUIContent($"{label} Min"));
            EditorGUI.PropertyField(right, max, new GUIContent($"{label} Max"));
        }

        private List<string> BuildTerrainLayerOptions()
        {
            var options = new List<string>(cachedTerrainLayers.Length);
            for (var i = 0; i < cachedTerrainLayers.Length; i++)
            {
                var layer = cachedTerrainLayers[i];
                options.Add(layer ? $"{i}: {layer.name}" : $"{i}: Missing Layer");
            }

            return options;
        }

        private static TerrainLayer[] FindTerrainLayers()
        {
            var terrains = Terrain.activeTerrains;
            if (terrains == null || terrains.Length == 0)
            {
                return Array.Empty<TerrainLayer>();
            }

            foreach (var terrain in terrains)
            {
                if (!terrain || !terrain.terrainData)
                {
                    continue;
                }

                var layers = terrain.terrainData.terrainLayers;
                if (layers != null && layers.Length > 0)
                {
                    return layers;
                }
            }

            return Array.Empty<TerrainLayer>();
        }
    }
}
