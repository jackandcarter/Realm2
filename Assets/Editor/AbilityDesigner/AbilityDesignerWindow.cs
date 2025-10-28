using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Realm.Abilities;

namespace Realm.EditorTools
{
    public class AbilityDesignerWindow : EditorWindow
    {
        private AbilityDefinition _workingCopy;
        private AbilityDefinition _selectedAsset;
        private SerializedObject _serializedAbility;
        private SerializedProperty _targetingProperty;
        private SerializedProperty _resourceProperty;
        private SerializedProperty _executionProperty;
        private SerializedProperty _effectsProperty;
        private ReorderableList _effectsList;
        private readonly List<(string message, MessageType type)> _validationMessages = new();
        private Vector2 _scrollPosition;
        private GUIStyle _wrapStyle;

        [MenuItem("Realm/Ability Designer", priority = 200)]
        public static void Open()
        {
            var window = GetWindow<AbilityDesignerWindow>("Ability Designer");
            window.minSize = new Vector2(640f, 600f);
        }

        private void OnEnable()
        {
            CreateWorkingCopy();
            RefreshValidation();
        }

        private void OnDisable()
        {
            if (_workingCopy != null)
            {
                DestroyImmediate(_workingCopy);
            }
        }

        private void CreateWorkingCopy()
        {
            if (_workingCopy == null)
            {
                _workingCopy = CreateInstance<AbilityDefinition>();
                _workingCopy.hideFlags = HideFlags.HideAndDontSave;
            }

            if (_workingCopy.Effects == null)
            {
                _workingCopy.Effects = new List<AbilityEffect>();
            }

            InitialiseSerializedState();
        }

        private void InitialiseSerializedState()
        {
            _serializedAbility = new SerializedObject(_workingCopy);
            _targetingProperty = _serializedAbility.FindProperty("Targeting");
            _resourceProperty = _serializedAbility.FindProperty("Resource");
            _executionProperty = _serializedAbility.FindProperty("Execution");
            _effectsProperty = _serializedAbility.FindProperty("Effects");

            _effectsList = new ReorderableList(_serializedAbility, _effectsProperty, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Effect Composer"),
                elementHeightCallback = index => EditorGUIUtility.singleLineHeight * 6.5f + 12f,
                onAddCallback = list =>
                {
                    var index = _effectsProperty.arraySize;
                    _effectsProperty.InsertArrayElementAtIndex(index);
                    var element = _effectsProperty.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("Name").stringValue = "New Effect";
                    element.FindPropertyRelative("EffectType").enumValueIndex = (int)AbilityEffectType.Damage;
                    element.FindPropertyRelative("Magnitude").floatValue = 10f;
                    element.FindPropertyRelative("DurationSeconds").floatValue = 0f;
                    element.FindPropertyRelative("TickInterval").floatValue = 1f;
                    element.FindPropertyRelative("ScalingWithPower").boolValue = true;
                    element.FindPropertyRelative("Priority").intValue = index;
                    element.FindPropertyRelative("StateName").stringValue = string.Empty;
                    element.FindPropertyRelative("CustomSummary").stringValue = string.Empty;
                },
                onRemoveCallback = list =>
                {
                    if (list.index >= 0 && list.index < _effectsProperty.arraySize)
                    {
                        _effectsProperty.DeleteArrayElementAtIndex(list.index);
                    }
                }
            };

            _effectsList.drawElementCallback = (rect, index, active, focused) =>
            {
                var element = _effectsProperty.GetArrayElementAtIndex(index);
                var line = rect;
                line.height = EditorGUIUtility.singleLineHeight;
                line.y += 2f;

                var left = new Rect(line.x, line.y, line.width * 0.5f - 4f, line.height);
                var right = new Rect(line.x + line.width * 0.5f + 4f, line.y, line.width * 0.5f - 4f, line.height);

                EditorGUI.PropertyField(left, element.FindPropertyRelative("Name"), GUIContent.none);
                EditorGUI.PropertyField(right, element.FindPropertyRelative("EffectType"), GUIContent.none);

                line.y += line.height + 2f;
                EditorGUI.PropertyField(new Rect(line.x, line.y, line.width * 0.5f - 4f, line.height), element.FindPropertyRelative("Magnitude"), new GUIContent("Magnitude"));
                EditorGUI.PropertyField(new Rect(line.x + line.width * 0.5f + 4f, line.y, line.width * 0.5f - 4f, line.height), element.FindPropertyRelative("DurationSeconds"), new GUIContent("Duration"));

                line.y += line.height + 2f;
                EditorGUI.PropertyField(new Rect(line.x, line.y, line.width * 0.5f - 4f, line.height), element.FindPropertyRelative("TickInterval"), new GUIContent("Tick Interval"));
                EditorGUI.PropertyField(new Rect(line.x + line.width * 0.5f + 4f, line.y, line.width * 0.5f - 4f, line.height), element.FindPropertyRelative("Priority"), new GUIContent("Priority"));

                line.y += line.height + 2f;
                EditorGUI.PropertyField(new Rect(line.x, line.y, line.width, line.height), element.FindPropertyRelative("StateName"), new GUIContent("State / Tag"));

                line.y += line.height + 2f;
                EditorGUI.PropertyField(new Rect(line.x, line.y, line.width, line.height), element.FindPropertyRelative("CustomSummary"), new GUIContent("Custom Summary"));

                line.y += line.height + 2f;
                EditorGUI.PropertyField(new Rect(line.x, line.y, line.width, line.height), element.FindPropertyRelative("ScalingWithPower"), new GUIContent("Scales With Power"));
            };
        }

        private void OnGUI()
        {
            if (_workingCopy == null)
            {
                CreateWorkingCopy();
            }

            _wrapStyle ??= new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            DrawToolbar();

            if (_serializedAbility == null)
            {
                InitialiseSerializedState();
            }

            _serializedAbility.Update();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawGeneralSection();
            EditorGUILayout.Space(12f);
            DrawTargetingSection();
            EditorGUILayout.Space(12f);
            DrawResourceSection();
            EditorGUILayout.Space(12f);
            DrawExecutionSection();
            EditorGUILayout.Space(12f);
            DrawEffectsSection();
            EditorGUILayout.Space(12f);
            DrawValidationSection();
            EditorGUILayout.Space(12f);
            DrawSummarySection();

            EditorGUILayout.EndScrollView();

            if (_serializedAbility.ApplyModifiedProperties())
            {
                RefreshValidation();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _selectedAsset = (AbilityDefinition)EditorGUILayout.ObjectField(_selectedAsset, typeof(AbilityDefinition), false, GUILayout.Width(250f));

                if (GUILayout.Button("Load", EditorStyles.toolbarButton))
                {
                    LoadFromAsset(_selectedAsset);
                }

                if (GUILayout.Button("New", EditorStyles.toolbarButton))
                {
                    ResetWorkingCopy();
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(_selectedAsset == null))
                {
                    if (GUILayout.Button("Apply To Asset", EditorStyles.toolbarButton))
                    {
                        SaveToExistingAsset();
                    }
                }

                if (GUILayout.Button("Save As...", EditorStyles.toolbarButton))
                {
                    SaveAsNewAsset();
                }
            }
        }

        private void DrawGeneralSection()
        {
            EditorGUILayout.LabelField("Ability Overview", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedAbility.FindProperty("AbilityName"), new GUIContent("Name"));
            EditorGUILayout.PropertyField(_serializedAbility.FindProperty("Description"));
            EditorGUILayout.PropertyField(_serializedAbility.FindProperty("Icon"));
        }

        private void DrawTargetingSection()
        {
            EditorGUILayout.LabelField("Targeting", EditorStyles.boldLabel);

            var modeProp = _targetingProperty.FindPropertyRelative("Mode");
            var areaShapeProp = _targetingProperty.FindPropertyRelative("AreaShape");
            var areaSizeProp = _targetingProperty.FindPropertyRelative("AreaSize");
            var maxTargetsProp = _targetingProperty.FindPropertyRelative("MaxTargets");
            var requiresTargetProp = _targetingProperty.FindPropertyRelative("RequiresPrimaryTarget");
            var canAffectCasterProp = _targetingProperty.FindPropertyRelative("CanAffectCaster");

            EditorGUILayout.PropertyField(modeProp);
            EditorGUILayout.PropertyField(maxTargetsProp, new GUIContent("Max Targets"));
            EditorGUILayout.PropertyField(requiresTargetProp, new GUIContent("Requires Primary Target"));
            EditorGUILayout.PropertyField(canAffectCasterProp, new GUIContent("Can Affect Caster"));

            if ((AbilityTargetingMode)modeProp.enumValueIndex == AbilityTargetingMode.Area)
            {
                EditorGUILayout.PropertyField(areaShapeProp);
                EditorGUILayout.PropertyField(areaSizeProp, new GUIContent("Area Size (m)"));
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(areaShapeProp, new GUIContent("Area Shape"));
                    EditorGUILayout.PropertyField(areaSizeProp, new GUIContent("Area Size (m)"));
                }
            }
        }

        private void DrawResourceSection()
        {
            EditorGUILayout.LabelField("Resources & Cooldowns", EditorStyles.boldLabel);

            var typeProp = _resourceProperty.FindPropertyRelative("ResourceType");
            var costProp = _resourceProperty.FindPropertyRelative("Cost");
            var percentProp = _resourceProperty.FindPropertyRelative("PercentageCost");
            var cooldownProp = _resourceProperty.FindPropertyRelative("CooldownSeconds");
            var gcdProp = _resourceProperty.FindPropertyRelative("GlobalCooldownSeconds");

            EditorGUILayout.PropertyField(typeProp, new GUIContent("Resource Type"));
            if ((AbilityResourceType)typeProp.enumValueIndex != AbilityResourceType.None)
            {
                EditorGUILayout.PropertyField(costProp, new GUIContent("Cost"));
                EditorGUILayout.PropertyField(percentProp, new GUIContent("Cost Is Percentage"));
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(costProp, new GUIContent("Cost"));
                    EditorGUILayout.PropertyField(percentProp, new GUIContent("Cost Is Percentage"));
                }
            }

            EditorGUILayout.PropertyField(cooldownProp, new GUIContent("Cooldown (s)"));
            EditorGUILayout.PropertyField(gcdProp, new GUIContent("Global Cooldown (s)"));
        }

        private void DrawExecutionSection()
        {
            EditorGUILayout.LabelField("Execution Conditions", EditorStyles.boldLabel);

            var labelProp = _executionProperty.FindPropertyRelative("Label");
            var descProp = _executionProperty.FindPropertyRelative("Description");
            var requiresLoSProp = _executionProperty.FindPropertyRelative("RequiresLoS");
            var requiresGroundProp = _executionProperty.FindPropertyRelative("RequiresGroundTarget");
            var movingProp = _executionProperty.FindPropertyRelative("OnlyWhileMoving");
            var stationaryProp = _executionProperty.FindPropertyRelative("OnlyWhileStationary");
            var requiresBuffProp = _executionProperty.FindPropertyRelative("RequiresBuffActive");
            var buffNameProp = _executionProperty.FindPropertyRelative("RequiredBuffName");
            var healthThresholdProp = _executionProperty.FindPropertyRelative("HealthThreshold");
            var comboProp = _executionProperty.FindPropertyRelative("RequiresComboWindow");

            EditorGUILayout.PropertyField(labelProp, new GUIContent("Label"));
            EditorGUILayout.PropertyField(descProp, new GUIContent("Description"));
            EditorGUILayout.PropertyField(requiresLoSProp, new GUIContent("Requires Line of Sight"));
            EditorGUILayout.PropertyField(requiresGroundProp, new GUIContent("Requires Ground Target"));
            EditorGUILayout.PropertyField(movingProp, new GUIContent("Only While Moving"));
            EditorGUILayout.PropertyField(stationaryProp, new GUIContent("Only While Stationary"));
            EditorGUILayout.PropertyField(comboProp, new GUIContent("Requires Combo Window"));
            EditorGUILayout.PropertyField(requiresBuffProp, new GUIContent("Requires Buff Active"));
            if (requiresBuffProp.boolValue)
            {
                EditorGUILayout.PropertyField(buffNameProp, new GUIContent("Buff Name"));
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(buffNameProp, new GUIContent("Buff Name"));
                }
            }

            EditorGUILayout.Slider(healthThresholdProp, 0f, 1f, new GUIContent("Health Threshold (0-1)"));
        }

        private void DrawEffectsSection()
        {
            EditorGUILayout.LabelField("Effect Composer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Reorder effects to control execution priority.", MessageType.None);
            _effectsList.DoLayoutList();
        }

        private void DrawValidationSection()
        {
            if (_validationMessages.Count == 0)
            {
                EditorGUILayout.HelpBox("No validation issues detected.", MessageType.Info);
                return;
            }

            foreach (var (message, type) in _validationMessages)
            {
                EditorGUILayout.HelpBox(message, type);
            }
        }

        private void DrawSummarySection()
        {
            EditorGUILayout.LabelField("Tooltip Preview", EditorStyles.boldLabel);
            var summary = _workingCopy.BuildSummary();
            EditorGUILayout.LabelField(summary, _wrapStyle);
        }

        private void LoadFromAsset(AbilityDefinition asset)
        {
            if (asset == null)
            {
                ResetWorkingCopy();
                return;
            }

            _selectedAsset = asset;
            EditorUtility.CopySerialized(asset, _workingCopy);
            if (_workingCopy.Targeting == null)
            {
                _workingCopy.Targeting = new AbilityTargetingConfig();
            }
            if (_workingCopy.Resource == null)
            {
                _workingCopy.Resource = new AbilityResourceConfig();
            }
            if (_workingCopy.Execution == null)
            {
                _workingCopy.Execution = new AbilityExecutionCondition();
            }
            if (_workingCopy.Effects == null)
            {
                _workingCopy.Effects = new List<AbilityEffect>();
            }
            InitialiseSerializedState();
            RefreshValidation();
        }

        private void ResetWorkingCopy()
        {
            if (_workingCopy != null)
            {
                DestroyImmediate(_workingCopy);
            }

            _selectedAsset = null;
            CreateWorkingCopy();
            RefreshValidation();
        }

        private void SaveAsNewAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject("Save Ability", "NewAbilityDefinition", "asset", "Choose a location for the ability definition asset.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var asset = CreateInstance<AbilityDefinition>();
            EditorUtility.CopySerialized(_workingCopy, asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _selectedAsset = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void SaveToExistingAsset()
        {
            if (_selectedAsset == null)
            {
                return;
            }

            EditorUtility.CopySerialized(_workingCopy, _selectedAsset);
            EditorUtility.SetDirty(_selectedAsset);
            AssetDatabase.SaveAssets();
        }

        private void RefreshValidation()
        {
            _validationMessages.Clear();

            if (_workingCopy == null)
            {
                return;
            }

            var targeting = _workingCopy.Targeting;
            var execution = _workingCopy.Execution;
            var resource = _workingCopy.Resource;

            if (_workingCopy.Effects == null || _workingCopy.Effects.Count == 0)
            {
                _validationMessages.Add(("Add at least one effect to the ability.", MessageType.Error));
            }

            if (targeting.Mode != AbilityTargetingMode.Area)
            {
                if (targeting.AreaShape != AbilityAreaShape.None)
                {
                    _validationMessages.Add(("Area shape is only valid when targeting mode is set to Area.", MessageType.Error));
                }

                if (targeting.AreaSize > 0f)
                {
                    _validationMessages.Add(("Area size will be ignored unless the ability targets an Area.", MessageType.Warning));
                }
            }
            else
            {
                if (targeting.AreaShape == AbilityAreaShape.None)
                {
                    _validationMessages.Add(("Select an area shape for area-targeted abilities.", MessageType.Error));
                }

                if (targeting.MaxTargets <= 1)
                {
                    _validationMessages.Add(("Area abilities typically affect more than one target.", MessageType.Warning));
                }

                if (targeting.RequiresPrimaryTarget)
                {
                    _validationMessages.Add(("Area abilities should not require a single primary target.", MessageType.Error));
                }
            }

            if (targeting.Mode == AbilityTargetingMode.Self && targeting.RequiresPrimaryTarget)
            {
                _validationMessages.Add(("Self-target abilities cannot require a primary target.", MessageType.Error));
            }

            if (resource.ResourceType == AbilityResourceType.None && resource.Cost > 0f)
            {
                _validationMessages.Add(("Cost is set but resource type is None. Consider disabling cost or selecting a resource type.", MessageType.Warning));
            }

            if (resource.CooldownSeconds < 0f)
            {
                _validationMessages.Add(("Cooldown cannot be negative.", MessageType.Error));
            }

            if (execution.OnlyWhileMoving && execution.OnlyWhileStationary)
            {
                _validationMessages.Add(("An ability cannot require both movement and being stationary.", MessageType.Error));
            }

            if (execution.RequiresBuffActive && string.IsNullOrWhiteSpace(execution.RequiredBuffName))
            {
                _validationMessages.Add(("Specify the buff name when 'Requires Buff Active' is enabled.", MessageType.Warning));
            }

            foreach (var effect in _workingCopy.Effects)
            {
                if (effect.EffectType == AbilityEffectType.Damage && effect.Magnitude <= 0f)
                {
                    _validationMessages.Add(($"Damage effect '{effect.Name}' has no magnitude.", MessageType.Warning));
                }

                if ((effect.EffectType == AbilityEffectType.Buff || effect.EffectType == AbilityEffectType.Debuff || effect.EffectType == AbilityEffectType.StateChange) && string.IsNullOrWhiteSpace(effect.StateName))
                {
                    _validationMessages.Add(($"Effect '{effect.Name}' requires a state or tag name.", MessageType.Warning));
                }

                if (effect.DurationSeconds < 0f)
                {
                    _validationMessages.Add(($"Effect '{effect.Name}' has a negative duration.", MessageType.Error));
                }

                if (effect.DurationSeconds > 0f && effect.TickInterval <= 0f)
                {
                    _validationMessages.Add(($"Effect '{effect.Name}' has duration but no tick interval.", MessageType.Warning));
                }
            }
        }
    }
}
