using System;
using System.Collections.Generic;
using Client.Combat;
using Realm.Abilities;
using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.Combat
{
    public class WeaponComboDefinitionEditorWindow : EditorWindow
    {
        private const string NoneOption = "<None>";

        private readonly List<string> _weaponIds = new();
        private readonly List<string> _weaponLabels = new();
        private readonly List<string> _abilityIds = new();
        private readonly List<string> _abilityLabels = new();
        private readonly List<WeaponComboDefinition> _comboDefinitions = new();

        private WeaponComboDefinition _definition;
        private SerializedObject _serializedDefinition;
        private SerializedProperty _weaponIdProperty;
        private SerializedProperty _comboSequenceProperty;
        private SerializedProperty _specialAbilityIdProperty;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Realm/Combat/Weapon Combo Editor", priority = 12)]
        public static void ShowWindow()
        {
            var window = GetWindow<WeaponComboDefinitionEditorWindow>("Weapon Combo Editor");
            window.minSize = new Vector2(420f, 360f);
        }

        private void OnEnable()
        {
            RefreshOptions();
            SyncDefinitionSelection();
        }

        private void OnFocus()
        {
            RefreshOptions();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            DrawToolbar();
            EditorGUILayout.Space(6f);

            _definition = (WeaponComboDefinition)EditorGUILayout.ObjectField(
                "Combo Definition",
                _definition,
                typeof(WeaponComboDefinition),
                allowSceneObjects: false);

            if (_definition == null)
            {
                EditorGUILayout.HelpBox("Select a WeaponComboDefinition asset to edit.", MessageType.Info);
                return;
            }

            EnsureSerialized();

            _serializedDefinition.Update();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawWeaponIdField();
            EditorGUILayout.Space(6f);
            DrawComboSequence();
            EditorGUILayout.Space(6f);
            DrawSpecialAbilityField();

            EditorGUILayout.EndScrollView();

            _serializedDefinition.ApplyModifiedProperties();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh Lists", GUILayout.Width(120f)))
            {
                RefreshOptions();
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Known Combos", GUILayout.Width(96f));
                var selectedIndex = GetSelectedDefinitionIndex();
                var labels = GetComboLabels();
                var nextIndex = EditorGUILayout.Popup(selectedIndex, labels);
                if (nextIndex != selectedIndex && nextIndex >= 0 && nextIndex < _comboDefinitions.Count)
                {
                    _definition = _comboDefinitions[nextIndex];
                    SyncDefinitionSelection();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawWeaponIdField()
        {
            EditorGUILayout.LabelField("Weapon", EditorStyles.boldLabel);

            var currentId = _weaponIdProperty.stringValue ?? string.Empty;
            var options = GetOptions(_weaponLabels);
            var ids = GetOptions(_weaponIds);
            var selectedIndex = GetOptionIndex(currentId, ids);
            var nextIndex = EditorGUILayout.Popup("Weapon ID", selectedIndex, options);

            if (nextIndex != selectedIndex)
            {
                _weaponIdProperty.stringValue = ids[nextIndex];
            }

            if (IsCustomSelection(currentId, ids))
            {
                EditorGUILayout.HelpBox("Weapon ID is not linked to a known WeaponDefinition asset.", MessageType.Warning);
                _weaponIdProperty.stringValue = EditorGUILayout.TextField("Custom Weapon ID", currentId);
            }
        }

        private void DrawComboSequence()
        {
            EditorGUILayout.LabelField("Combo Sequence", EditorStyles.boldLabel);

            var length = _comboSequenceProperty.arraySize;
            var nextLength = Mathf.Max(1, EditorGUILayout.IntField("Sequence Length", length));
            if (nextLength != length)
            {
                ResizeComboSequence(nextLength);
            }

            for (var i = 0; i < _comboSequenceProperty.arraySize; i++)
            {
                var entry = _comboSequenceProperty.GetArrayElementAtIndex(i);
                var value = (WeaponComboInputType)entry.enumValueIndex;
                var nextValue = (WeaponComboInputType)EditorGUILayout.EnumPopup($"Step {i + 1}", value);
                if (nextValue != value)
                {
                    entry.enumValueIndex = (int)nextValue;
                }
            }

            if (_comboSequenceProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Combo sequence must contain at least one input.", MessageType.Error);
            }
        }

        private void DrawSpecialAbilityField()
        {
            EditorGUILayout.LabelField("Special Ability", EditorStyles.boldLabel);

            var currentId = _specialAbilityIdProperty.stringValue ?? string.Empty;
            var options = GetOptions(_abilityLabels);
            var ids = GetOptions(_abilityIds);
            var selectedIndex = GetOptionIndex(currentId, ids);
            var nextIndex = EditorGUILayout.Popup("Ability ID", selectedIndex, options);

            if (nextIndex != selectedIndex)
            {
                _specialAbilityIdProperty.stringValue = ids[nextIndex];
            }

            if (IsCustomSelection(currentId, ids))
            {
                EditorGUILayout.HelpBox("Special ability ID is not linked to a known AbilityDefinition asset.", MessageType.Warning);
                _specialAbilityIdProperty.stringValue = EditorGUILayout.TextField("Custom Ability ID", currentId);
            }
        }

        private void ResizeComboSequence(int targetSize)
        {
            targetSize = Mathf.Max(0, targetSize);

            while (_comboSequenceProperty.arraySize < targetSize)
            {
                _comboSequenceProperty.InsertArrayElementAtIndex(_comboSequenceProperty.arraySize);
                var entry = _comboSequenceProperty.GetArrayElementAtIndex(_comboSequenceProperty.arraySize - 1);
                entry.enumValueIndex = (int)WeaponComboInputType.Light;
            }

            while (_comboSequenceProperty.arraySize > targetSize)
            {
                _comboSequenceProperty.DeleteArrayElementAtIndex(_comboSequenceProperty.arraySize - 1);
            }
        }

        private void RefreshOptions()
        {
            _weaponIds.Clear();
            _weaponLabels.Clear();
            _abilityIds.Clear();
            _abilityLabels.Clear();
            _comboDefinitions.Clear();

            _weaponIds.Add(string.Empty);
            _weaponLabels.Add(NoneOption);
            _abilityIds.Add(string.Empty);
            _abilityLabels.Add(NoneOption);

            LoadWeaponDefinitions();
            LoadAbilityDefinitions();
            LoadComboDefinitions();
        }

        private void LoadWeaponDefinitions()
        {
            var guids = AssetDatabase.FindAssets("t:WeaponDefinition");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
                if (asset == null || string.IsNullOrWhiteSpace(asset.Guid))
                {
                    continue;
                }

                _weaponIds.Add(asset.Guid);
                _weaponLabels.Add($"{asset.DisplayName} ({asset.Guid})");
            }
        }

        private void LoadAbilityDefinitions()
        {
            var guids = AssetDatabase.FindAssets("t:AbilityDefinition");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
                if (asset == null || string.IsNullOrWhiteSpace(asset.Guid))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(asset.AbilityName) ? asset.name : asset.AbilityName;
                _abilityIds.Add(asset.Guid);
                _abilityLabels.Add($"{label} ({asset.Guid})");
            }
        }

        private void LoadComboDefinitions()
        {
            var guids = AssetDatabase.FindAssets("t:WeaponComboDefinition");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<WeaponComboDefinition>(path);
                if (asset != null)
                {
                    _comboDefinitions.Add(asset);
                }
            }
        }

        private string[] GetComboLabels()
        {
            if (_comboDefinitions.Count == 0)
            {
                return new[] { "<None Found>" };
            }

            var labels = new string[_comboDefinitions.Count];
            for (var i = 0; i < _comboDefinitions.Count; i++)
            {
                labels[i] = _comboDefinitions[i] != null ? _comboDefinitions[i].name : "<Missing>";
            }

            return labels;
        }

        private void EnsureSerialized()
        {
            if (_serializedDefinition != null && _serializedDefinition.targetObject == _definition)
            {
                return;
            }

            _serializedDefinition = new SerializedObject(_definition);
            _weaponIdProperty = _serializedDefinition.FindProperty("weaponId");
            _comboSequenceProperty = _serializedDefinition.FindProperty("comboSequence");
            _specialAbilityIdProperty = _serializedDefinition.FindProperty("specialAttackAbilityId");
        }

        private void SyncDefinitionSelection()
        {
            if (_definition != null)
            {
                EnsureSerialized();
                return;
            }

            if (_comboDefinitions.Count > 0)
            {
                _definition = _comboDefinitions[0];
                EnsureSerialized();
            }
        }

        private int GetSelectedDefinitionIndex()
        {
            if (_definition == null || _comboDefinitions.Count == 0)
            {
                return 0;
            }

            return Mathf.Max(0, _comboDefinitions.IndexOf(_definition));
        }

        private static string[] GetOptions(List<string> values)
        {
            return values.Count == 0 ? new[] { NoneOption } : values.ToArray();
        }

        private static int GetOptionIndex(string currentId, string[] options)
        {
            if (options == null || options.Length == 0)
            {
                return 0;
            }

            var index = Array.IndexOf(options, currentId);
            return index >= 0 ? index : 0;
        }

        private static bool IsCustomSelection(string currentId, string[] options)
        {
            if (string.IsNullOrWhiteSpace(currentId))
            {
                return false;
            }

            return Array.IndexOf(options, currentId) < 0;
        }
    }
}
