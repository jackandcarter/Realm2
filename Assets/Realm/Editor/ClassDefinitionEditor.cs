using Realm.Abilities;
using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor
{
    [CustomEditor(typeof(ClassDefinition))]
    public class ClassDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _guidProperty;
        private SerializedProperty _classIdProperty;
        private SerializedProperty _displayNameProperty;
        private SerializedProperty _descriptionProperty;
        private SerializedProperty _iconProperty;
        private SerializedProperty _statCategoriesProperty;
        private SerializedProperty _statProfileProperty;
        private SerializedProperty _baseCurvesProperty;
        private SerializedProperty _growthModifiersProperty;
        private SerializedProperty _allowedArmorTypesProperty;
        private SerializedProperty _allowedWeaponTypesProperty;
        private SerializedProperty _abilityUnlocksProperty;

        private void OnEnable()
        {
            _guidProperty = serializedObject.FindProperty("guid");
            _classIdProperty = serializedObject.FindProperty("classId");
            _displayNameProperty = serializedObject.FindProperty("displayName");
            _descriptionProperty = serializedObject.FindProperty("description");
            _iconProperty = serializedObject.FindProperty("icon");
            _statCategoriesProperty = serializedObject.FindProperty("statCategories");
            _statProfileProperty = serializedObject.FindProperty("statProfile");
            _baseCurvesProperty = serializedObject.FindProperty("baseStatCurves");
            _growthModifiersProperty = serializedObject.FindProperty("growthModifiers");
            _allowedArmorTypesProperty = serializedObject.FindProperty("allowedArmorTypes");
            _allowedWeaponTypesProperty = serializedObject.FindProperty("allowedWeaponTypes");
            _abilityUnlocksProperty = serializedObject.FindProperty("abilityUnlocks");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_guidProperty);
            }

            EditorGUILayout.LabelField("Presentation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_classIdProperty, new GUIContent("Class Id", "Runtime id used for unlocks and equipment restrictions."));
            EditorGUILayout.PropertyField(_displayNameProperty);
            EditorGUILayout.PropertyField(_descriptionProperty);
            EditorGUILayout.PropertyField(_iconProperty);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Stat Focus", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_statCategoriesProperty, true);
            EditorGUILayout.PropertyField(_statProfileProperty);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Stat Progression", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure per-stat base curves, growth modifiers, soft caps, jitter ranges, and JRPG formula coefficients.", MessageType.Info);
            EditorGUILayout.PropertyField(_baseCurvesProperty, new GUIContent("Base Stat Profiles"), true);
            EditorGUILayout.PropertyField(_growthModifiersProperty, new GUIContent("Growth Modifiers"), true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Equipment Proficiencies", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_allowedArmorTypesProperty, true);
            EditorGUILayout.PropertyField(_allowedWeaponTypesProperty, true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Ability Unlocks", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Assign abilities and define whether they unlock from level progression, quest completion, or required items.", MessageType.Info);
            DrawAbilityUnlocks();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAbilityUnlocks()
        {
            if (_abilityUnlocksProperty == null)
            {
                return;
            }

            EditorGUILayout.Space();

            for (var i = 0; i < _abilityUnlocksProperty.arraySize; i++)
            {
                var element = _abilityUnlocksProperty.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var abilityProp = element.FindPropertyRelative("ability");
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var ability = abilityProp?.objectReferenceValue as AbilityDefinition;
                        var displayName = ResolveAbilityLabel(ability);
                        EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Remove", GUILayout.Width(80f)))
                        {
                            _abilityUnlocksProperty.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }

                    EditorGUILayout.PropertyField(abilityProp);

                    var conditionProp = element.FindPropertyRelative("conditionType");
                    EditorGUILayout.PropertyField(conditionProp);

                    var condition = (AbilityUnlockConditionType)conditionProp.enumValueIndex;
                    switch (condition)
                    {
                        case AbilityUnlockConditionType.Level:
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("requiredLevel"));
                            break;
                        case AbilityUnlockConditionType.Quest:
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("questId"));
                            break;
                        case AbilityUnlockConditionType.Item:
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("itemId"));
                            break;
                    }

                    EditorGUILayout.PropertyField(element.FindPropertyRelative("notes"));

                    var classDefinition = target as ClassDefinition;
                    if (classDefinition != null && i < classDefinition.AbilityUnlocks.Count)
                    {
                        var unlock = classDefinition.AbilityUnlocks[i];
                        if (unlock != null)
                        {
                            EditorGUILayout.LabelField("Condition", unlock.DescribeCondition(), EditorStyles.miniLabel);
                        }
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Ability Unlock", GUILayout.Width(200f)))
                {
                    var index = _abilityUnlocksProperty.arraySize;
                    _abilityUnlocksProperty.InsertArrayElementAtIndex(index);
                    var element = _abilityUnlocksProperty.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("ability").objectReferenceValue = null;
                    element.FindPropertyRelative("conditionType").enumValueIndex = (int)AbilityUnlockConditionType.Level;
                    element.FindPropertyRelative("requiredLevel").intValue = 1;
                    element.FindPropertyRelative("questId").stringValue = string.Empty;
                    element.FindPropertyRelative("itemId").stringValue = string.Empty;
                    element.FindPropertyRelative("notes").stringValue = string.Empty;
                }
            }
        }

        private static string ResolveAbilityLabel(AbilityDefinition ability)
        {
            if (ability == null)
            {
                return "Unassigned Ability";
            }

            var label = string.IsNullOrWhiteSpace(ability.AbilityName)
                ? ability.name
                : ability.AbilityName;

            return string.IsNullOrWhiteSpace(label) ? "Unnamed Ability" : label;
        }
    }
}
