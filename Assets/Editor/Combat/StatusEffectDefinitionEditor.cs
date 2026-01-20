using Realm.Combat.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.EditorTools.Combat
{
    [CustomEditor(typeof(StatusEffectDefinition))]
    public class StatusEffectDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Task Stub 11: Surface refresh rule, max stacks, and dispel type with validation hints.
            EditorGUILayout.PropertyField(serializedObject.FindProperty("statusId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("type"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("refreshRule"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStacks"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dispelType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("durationModelId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stackingRuleId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("modifiers"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("actionRestrictions"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("periodicEffects"));

            var maxStacksProperty = serializedObject.FindProperty("maxStacks");
            var refreshRuleProperty = serializedObject.FindProperty("refreshRule");
            if (maxStacksProperty != null && refreshRuleProperty != null)
            {
                var maxStacks = maxStacksProperty.intValue;
                var refreshRule = (StatusRefreshRule)refreshRuleProperty.enumValueIndex;
                if (maxStacks > 1 && refreshRule != StatusRefreshRule.AddStacks)
                {
                    EditorGUILayout.HelpBox(
                        "MaxStacks > 1 but RefreshRule is not AddStacks.",
                        MessageType.Warning);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
