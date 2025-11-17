using UnityEditor;
using UnityEngine;
using Realm.CharacterCustomization;

namespace Realm.Editor.CharacterCustomization
{
    [CustomEditor(typeof(CharacterMorphController))]
    public class CharacterMorphControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _regionsProperty;

        private void OnEnable()
        {
            _regionsProperty = serializedObject.FindProperty("regions");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Assign the transforms for each limb or torso segment, then drag the sliders to morph the current prefab or scene object.",
                MessageType.Info);

            if (_regionsProperty != null)
            {
                int removeIndex = -1;
                for (int i = 0; i < _regionsProperty.arraySize; i++)
                {
                    if (DrawRegion(_regionsProperty.GetArrayElementAtIndex(i), i))
                    {
                        removeIndex = i;
                    }
                }

                if (removeIndex >= 0)
                {
                    _regionsProperty.DeleteArrayElementAtIndex(removeIndex);
                }

                if (GUILayout.Button("Add Body Region"))
                {
                    _regionsProperty.InsertArrayElementAtIndex(_regionsProperty.arraySize);
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Capture Current Scales"))
                {
                    foreach (CharacterMorphController controller in targets)
                    {
                        controller.CaptureDefaultsFromScene();
                        EditorUtility.SetDirty(controller);
                    }
                }

                if (GUILayout.Button("Apply Sliders"))
                {
                    foreach (CharacterMorphController controller in targets)
                    {
                        controller.ApplyAll();
                        EditorUtility.SetDirty(controller);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                foreach (CharacterMorphController controller in targets)
                {
                    controller.ApplyAll();
                }
            }
        }

        private static bool DrawRegion(SerializedProperty regionProperty, int index)
        {
            if (regionProperty == null)
            {
                return false;
            }

            var labelProp = regionProperty.FindPropertyRelative("label");
            var targetProp = regionProperty.FindPropertyRelative("target");
            var heightRangeProp = regionProperty.FindPropertyRelative("heightRange");
            var widthRangeProp = regionProperty.FindPropertyRelative("widthRange");
            var heightProp = regionProperty.FindPropertyRelative("heightNormalized");
            var widthProp = regionProperty.FindPropertyRelative("widthNormalized");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Region {index + 1}", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(labelProp);
            EditorGUILayout.PropertyField(targetProp);

            EditorGUILayout.PropertyField(heightRangeProp, new GUIContent("Height Range"));
            EditorGUILayout.Slider(heightProp, 0f, 1f, new GUIContent("Height"));

            EditorGUILayout.PropertyField(widthRangeProp, new GUIContent("Width Range"));
            EditorGUILayout.Slider(widthProp, 0f, 1f, new GUIContent("Width"));

            var remove = GUILayout.Button("Remove Region");
            EditorGUILayout.EndVertical();
            return remove;
        }
    }
}
