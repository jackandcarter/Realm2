using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor
{
    [CustomEditor(typeof(StatProfileDefinition))]
    public class StatProfileDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _guidProperty;
        private SerializedProperty _displayNameProperty;
        private SerializedProperty _descriptionProperty;
        private SerializedProperty _statCurvesProperty;

        private void OnEnable()
        {
            _guidProperty = serializedObject.FindProperty("guid");
            _displayNameProperty = serializedObject.FindProperty("displayName");
            _descriptionProperty = serializedObject.FindProperty("description");
            _statCurvesProperty = serializedObject.FindProperty("statCurves");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_guidProperty);
            }

            EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_displayNameProperty);
            EditorGUILayout.PropertyField(_descriptionProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stat Curves", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Author JRPG stat behavior using reusable templates. These profiles can be shared across clas" +
                                    "ses for consistency.", MessageType.Info);
            EditorGUILayout.PropertyField(_statCurvesProperty, true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
