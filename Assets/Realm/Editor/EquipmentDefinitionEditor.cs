using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor
{
    [CustomEditor(typeof(EquipmentDefinition), true)]
    public class EquipmentDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _guidProperty;
        private SerializedProperty _displayNameProperty;
        private SerializedProperty _descriptionProperty;
        private SerializedProperty _iconProperty;
        private SerializedProperty _inventoryIconProperty;
        private SerializedProperty _dockIconProperty;
        private SerializedProperty _slotProperty;
        private SerializedProperty _requiredClassIdsProperty;
        private SerializedProperty _requiredClassesProperty;
        private SerializedProperty _equipEffectsProperty;
        private SerializedProperty _weaponTypeProperty;
        private SerializedProperty _armorTypeProperty;

        private void OnEnable()
        {
            _guidProperty = serializedObject.FindProperty("guid");
            _displayNameProperty = serializedObject.FindProperty("displayName");
            _descriptionProperty = serializedObject.FindProperty("description");
            _iconProperty = serializedObject.FindProperty("icon");
            _inventoryIconProperty = serializedObject.FindProperty("inventoryIcon");
            _dockIconProperty = serializedObject.FindProperty("dockIcon");
            _slotProperty = serializedObject.FindProperty("slot");
            _requiredClassIdsProperty = serializedObject.FindProperty("requiredClassIds");
            _requiredClassesProperty = serializedObject.FindProperty("requiredClasses");
            _equipEffectsProperty = serializedObject.FindProperty("equipEffects");
            _weaponTypeProperty = serializedObject.FindProperty("weaponType");
            _armorTypeProperty = serializedObject.FindProperty("armorType");
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
            EditorGUILayout.PropertyField(_inventoryIconProperty, new GUIContent("Inventory Icon"));
            EditorGUILayout.PropertyField(_dockIconProperty, new GUIContent("Dock Icon"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Equipment Details", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_slotProperty);
            if (_weaponTypeProperty != null)
            {
                EditorGUILayout.PropertyField(_weaponTypeProperty);
            }

            if (_armorTypeProperty != null)
            {
                EditorGUILayout.PropertyField(_armorTypeProperty);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Class Restrictions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use class ids for runtime enforcement. Optional asset references can help designers stay aligned with class definitions.", MessageType.Info);
            EditorGUILayout.PropertyField(_requiredClassIdsProperty, true);
            EditorGUILayout.PropertyField(_requiredClassesProperty, true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("On-Equip Behaviors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_equipEffectsProperty, true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
