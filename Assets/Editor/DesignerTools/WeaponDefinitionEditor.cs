using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.DesignerTools
{
    [CustomEditor(typeof(WeaponDefinition))]
    public class WeaponDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _weaponType;
        private SerializedProperty _baseDamage;
        private SerializedProperty _lightAttack;
        private SerializedProperty _mediumAttack;
        private SerializedProperty _heavyAttack;
        private SerializedProperty _specialAttack;

        private void OnEnable()
        {
            _weaponType = serializedObject.FindProperty("weaponType");
            _baseDamage = serializedObject.FindProperty("baseDamage");
            _lightAttack = serializedObject.FindProperty("lightAttack");
            _mediumAttack = serializedObject.FindProperty("mediumAttack");
            _heavyAttack = serializedObject.FindProperty("heavyAttack");
            _specialAttack = serializedObject.FindProperty("specialAttack");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "weaponType", "baseDamage", "lightAttack", "mediumAttack", "heavyAttack", "specialAttack");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Weapon", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_weaponType);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Combat", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_baseDamage, new GUIContent("Base Damage"));

            var clamped = false;
            clamped |= DrawAttackProfile(_lightAttack, "Light Attack");
            clamped |= DrawAttackProfile(_mediumAttack, "Medium Attack");
            clamped |= DrawAttackProfile(_heavyAttack, "Heavy Attack");

            EditorGUILayout.PropertyField(_specialAttack, new GUIContent("Special Attack"));

            if (clamped)
            {
                EditorGUILayout.HelpBox("Attack profile values are clamped to valid ranges (accuracy 0-1, windup/recovery >= 0).", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static bool DrawAttackProfile(SerializedProperty property, string label)
        {
            var damageMultiplier = property.FindPropertyRelative("damageMultiplier");
            var accuracy = property.FindPropertyRelative("accuracy");
            var windup = property.FindPropertyRelative("windupSeconds");
            var recovery = property.FindPropertyRelative("recoverySeconds");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                var damageValue = EditorGUILayout.FloatField(new GUIContent("Damage Multiplier"), damageMultiplier.floatValue);
                var accuracyValue = EditorGUILayout.Slider(new GUIContent("Accuracy"), accuracy.floatValue, 0f, 1f);
                var windupValue = EditorGUILayout.FloatField(new GUIContent("Windup Seconds"), windup.floatValue);
                var recoveryValue = EditorGUILayout.FloatField(new GUIContent("Recovery Seconds"), recovery.floatValue);

                var clamped = false;
                if (EditorGUI.EndChangeCheck())
                {
                    damageMultiplier.floatValue = damageValue;

                    if (!Mathf.Approximately(accuracyValue, Mathf.Clamp01(accuracyValue)))
                    {
                        clamped = true;
                    }

                    if (windupValue < 0f || recoveryValue < 0f)
                    {
                        clamped = true;
                    }

                    accuracy.floatValue = Mathf.Clamp01(accuracyValue);
                    windup.floatValue = Mathf.Max(0f, windupValue);
                    recovery.floatValue = Mathf.Max(0f, recoveryValue);
                }
                else
                {
                    if (!Mathf.Approximately(accuracy.floatValue, Mathf.Clamp01(accuracy.floatValue))
                        || windup.floatValue < 0f
                        || recovery.floatValue < 0f)
                    {
                        accuracy.floatValue = Mathf.Clamp01(accuracy.floatValue);
                        windup.floatValue = Mathf.Max(0f, windup.floatValue);
                        recovery.floatValue = Mathf.Max(0f, recovery.floatValue);
                        clamped = true;
                    }
                }

                return clamped;
            }
        }
    }
}
