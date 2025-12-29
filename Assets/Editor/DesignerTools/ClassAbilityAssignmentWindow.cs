using System;
using System.Collections.Generic;
using System.Linq;
using Realm.EditorTools;
using UnityEditor;
using UnityEngine;
using AbilityUnlockConditionType = Realm.Data.AbilityUnlockConditionType;
using ClassDefinition = Realm.Data.ClassDefinition;

namespace Realm.Editor.DesignerTools
{
    public class ClassAbilityAssignmentWindow : EditorWindow
    {
        private readonly List<ClassDefinition> _classes = new();
        private readonly List<Realm.Abilities.AbilityDefinition> _abilities = new();
        private SerializedObject _serializedClass;
        private int _selectedClassIndex = -1;
        private Vector2 _classScroll;
        private Vector2 _detailsScroll;
        private string _classSearch = string.Empty;
        private string _abilitySearch = string.Empty;
        private GUIStyle _wrapStyle;

        [MenuItem("Tools/Designer/Class Ability Planner", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<ClassAbilityAssignmentWindow>("Class Ability Planner");
            window.minSize = new Vector2(920f, 560f);
            window.RefreshData();
        }

        private void OnEnable()
        {
            RefreshData();
        }

        private void RefreshData()
        {
            _classes.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Realm.Data.ClassDefinition"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<ClassDefinition>(path);
                if (definition != null)
                {
                    _classes.Add(definition);
                }
            }

            _classes.Sort((a, b) => string.Compare(a?.DisplayName ?? a?.name, b?.DisplayName ?? b?.name, StringComparison.OrdinalIgnoreCase));

            _abilities.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Realm.Abilities.AbilityDefinition"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ability = AssetDatabase.LoadAssetAtPath<Realm.Abilities.AbilityDefinition>(path);
                if (ability != null)
                {
                    _abilities.Add(ability);
                }
            }

            _abilities.Sort((a, b) => string.Compare(ResolveAbilityLabel(a), ResolveAbilityLabel(b), StringComparison.OrdinalIgnoreCase));

            if (_selectedClassIndex >= _classes.Count)
            {
                _selectedClassIndex = _classes.Count - 1;
            }

            LoadSelectedClass();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_classes.Count == 0)
            {
                EditorGUILayout.HelpBox("No class definitions found. Create a ClassDefinition asset to begin planning abilities.", MessageType.Info);
                return;
            }

            _wrapStyle ??= new GUIStyle(EditorStyles.wordWrappedLabel) { wordWrap = true };

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawClassSelection();
                DrawClassDetails();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    RefreshData();
                }

                GUILayout.Space(8f);

                EditorGUILayout.LabelField("Class Filter", GUILayout.Width(80f));
                _classSearch = GUILayout.TextField(_classSearch, GUILayout.Width(160f));

                GUILayout.Space(12f);

                EditorGUILayout.LabelField("Ability Filter", GUILayout.Width(90f));
                _abilitySearch = GUILayout.TextField(_abilitySearch, GUILayout.Width(160f));

                GUILayout.FlexibleSpace();

                if (_selectedClassIndex >= 0 && _selectedClassIndex < _classes.Count)
                {
                    using (new EditorGUI.DisabledScope(_serializedClass == null))
                    {
                        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                        {
                            if (_serializedClass != null)
                            {
                                _serializedClass.ApplyModifiedProperties();
                                EditorUtility.SetDirty(_classes[_selectedClassIndex]);
                                AssetDatabase.SaveAssets();
                            }
                        }
                    }
                }
            }
        }

        private void DrawClassSelection()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(260f)))
            {
                EditorGUILayout.LabelField("Classes", EditorStyles.boldLabel);

                using (var scroll = new EditorGUILayout.ScrollViewScope(_classScroll))
                {
                    _classScroll = scroll.scrollPosition;

                    for (var i = 0; i < _classes.Count; i++)
                    {
                        var definition = _classes[i];
                        if (!Matches(definition.DisplayName, _classSearch))
                        {
                            continue;
                        }

                        var label = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.name : definition.DisplayName;
                        if (GUILayout.Toggle(_selectedClassIndex == i, label, EditorStyles.toolbarButton))
                        {
                            if (_selectedClassIndex != i)
                            {
                                _selectedClassIndex = i;
                                LoadSelectedClass();
                            }
                        }
                    }
                }
            }
        }

        private void DrawClassDetails()
        {
            if (_serializedClass == null || _selectedClassIndex < 0 || _selectedClassIndex >= _classes.Count)
            {
                EditorGUILayout.HelpBox("Select a class to configure its ability progression.", MessageType.Info);
                return;
            }

            _serializedClass.Update();

            using (new EditorGUILayout.VerticalScope())
            {
                var classDefinition = _classes[_selectedClassIndex];
                EditorGUILayout.LabelField(classDefinition.DisplayName, EditorStyles.boldLabel);
                if (!string.IsNullOrWhiteSpace(classDefinition.Description))
                {
                    EditorGUILayout.LabelField(classDefinition.Description, _wrapStyle);
                }
                EditorGUILayout.Space();

                using (var scroll = new EditorGUILayout.ScrollViewScope(_detailsScroll))
                {
                    _detailsScroll = scroll.scrollPosition;

                    var unlocksProperty = _serializedClass.FindProperty("abilityUnlocks");
                    if (unlocksProperty != null)
                    {
                        for (var i = 0; i < unlocksProperty.arraySize; i++)
                        {
                            DrawUnlockEntry(unlocksProperty, i);
                        }
                    }

                    EditorGUILayout.Space(10f);
                    DrawAddAbilityControls(unlocksProperty);
                }

                if (_serializedClass.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(classDefinition);
                }
            }
        }

        private void DrawUnlockEntry(SerializedProperty unlocksProperty, int index)
        {
            var element = unlocksProperty.GetArrayElementAtIndex(index);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var abilityProp = element.FindPropertyRelative("ability");
                    var ability = abilityProp?.objectReferenceValue as Realm.Abilities.AbilityDefinition;
                    var displayName = ResolveAbilityLabel(ability);
                    EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
                    if (ability != null && GUILayout.Button("Ability Designer", GUILayout.Width(130f)))
                    {
                        AbilityDesignerWindow.Open();
                        Selection.activeObject = ability;
                        EditorGUIUtility.PingObject(ability);
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        unlocksProperty.DeleteArrayElementAtIndex(index);
                        return;
                    }
                }

                EditorGUILayout.PropertyField(element.FindPropertyRelative("ability"));

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

                var unlockData = _classes[_selectedClassIndex].AbilityUnlocks.ElementAtOrDefault(index);
                if (unlockData != null)
                {
                    EditorGUILayout.LabelField("Summary", unlockData.DescribeCondition(), EditorStyles.miniLabel);
                }
            }
        }

        private void DrawAddAbilityControls(SerializedProperty unlocksProperty)
        {
            if (unlocksProperty == null)
            {
                EditorGUILayout.HelpBox("Unable to access ability unlock data on the selected class.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Add Ability", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select an ability from the library and assign how it unlocks for this class.", MessageType.Info);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Realm.Abilities.AbilityDefinition selectedAbility = null;
                foreach (var ability in _abilities)
                {
                    var label = ResolveAbilityLabel(ability);
                    if (!Matches(label, _abilitySearch))
                    {
                        continue;
                    }

                    if (GUILayout.Button(label))
                    {
                        selectedAbility = ability;
                    }
                }

                if (selectedAbility != null)
                {
                    var index = unlocksProperty.arraySize;
                    unlocksProperty.InsertArrayElementAtIndex(index);
                    var element = unlocksProperty.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("ability").objectReferenceValue = selectedAbility;
                    element.FindPropertyRelative("conditionType").enumValueIndex = (int)AbilityUnlockConditionType.Level;
                    element.FindPropertyRelative("requiredLevel").intValue = 1;
                    element.FindPropertyRelative("questId").stringValue = string.Empty;
                    element.FindPropertyRelative("itemId").stringValue = string.Empty;
                    element.FindPropertyRelative("notes").stringValue = string.Empty;
                }
            }
        }

        private void LoadSelectedClass()
        {
            if (_selectedClassIndex >= 0 && _selectedClassIndex < _classes.Count)
            {
                _serializedClass = new SerializedObject(_classes[_selectedClassIndex]);
            }
            else
            {
                _serializedClass = null;
            }
        }

        private static bool Matches(string value, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            return value != null && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ResolveAbilityLabel(Realm.Abilities.AbilityDefinition ability)
        {
            if (ability == null)
            {
                return "Unassigned";
            }

            var label = string.IsNullOrWhiteSpace(ability.AbilityName)
                ? ability.name
                : ability.AbilityName;

            return string.IsNullOrWhiteSpace(label) ? "Unnamed Ability" : label;
        }
    }
}
