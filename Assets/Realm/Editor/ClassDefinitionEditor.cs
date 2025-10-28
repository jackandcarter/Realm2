using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor
{
    [CustomEditor(typeof(ClassDefinition))]
    public class ClassDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _guidProperty;
        private SerializedProperty _displayNameProperty;
        private SerializedProperty _descriptionProperty;
        private SerializedProperty _iconProperty;
        private SerializedProperty _statCategoriesProperty;
        private SerializedProperty _baseCurvesProperty;
        private SerializedProperty _growthModifiersProperty;
        private SerializedProperty _startingAbilitiesProperty;
        private SerializedProperty _unlockableAbilitiesProperty;

        private void OnEnable()
        {
            _guidProperty = serializedObject.FindProperty("guid");
            _displayNameProperty = serializedObject.FindProperty("displayName");
            _descriptionProperty = serializedObject.FindProperty("description");
            _iconProperty = serializedObject.FindProperty("icon");
            _statCategoriesProperty = serializedObject.FindProperty("statCategories");
            _baseCurvesProperty = serializedObject.FindProperty("baseStatCurves");
            _growthModifiersProperty = serializedObject.FindProperty("growthModifiers");
            _startingAbilitiesProperty = serializedObject.FindProperty("startingAbilities");
            _unlockableAbilitiesProperty = serializedObject.FindProperty("unlockableAbilities");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_guidProperty);
            }

            EditorGUILayout.LabelField("Presentation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_displayNameProperty);
            EditorGUILayout.PropertyField(_descriptionProperty);
            EditorGUILayout.PropertyField(_iconProperty);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Stat Focus", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_statCategoriesProperty, true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Stat Progression", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure per-stat base curves, growth modifiers, soft caps, jitter ranges, and JRPG formula coefficients.", MessageType.Info);
            EditorGUILayout.PropertyField(_baseCurvesProperty, new GUIContent("Base Stat Profiles"), true);
            EditorGUILayout.PropertyField(_growthModifiersProperty, new GUIContent("Growth Modifiers"), true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Abilities", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_startingAbilitiesProperty, true);
            EditorGUILayout.PropertyField(_unlockableAbilitiesProperty, true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
