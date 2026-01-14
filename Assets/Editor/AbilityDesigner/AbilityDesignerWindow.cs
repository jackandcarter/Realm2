using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Client.CharacterCreation;
using Realm.Editor.DesignerTools;
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
        private SerializedProperty _hitboxProperty;
        private SerializedProperty _comboProperty;
        private SerializedProperty _comboStagesProperty;
        private SerializedProperty _effectsProperty;
        private ReorderableList _effectsList;
        private ReorderableList _comboStagesList;
        private readonly List<(string message, MessageType type)> _validationMessages = new();
        private Vector2 _scrollPosition;
        private GUIStyle _wrapStyle;
        private ClassAbilityProgression _progressionAsset;
        private string[] _progressionClassOptions = Array.Empty<string>();
        private string _progressionClassId = string.Empty;
        private int _progressionLevel = 1;
        private bool _registerInClassProgression;

        [MenuItem("Tools/Designer/Ability Designer", priority = 130)]
        public static void Open()
        {
            var window = GetWindow<AbilityDesignerWindow>("Ability Designer");
            window.minSize = new Vector2(640f, 600f);
        }

        private void OnEnable()
        {
            CreateWorkingCopy();
            EnsureProgressionAsset();
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
                _workingCopy.hideFlags = HideFlags.HideInHierarchy
                                          | HideFlags.DontSaveInEditor
                                          | HideFlags.DontSaveInBuild
                                          | HideFlags.DontUnloadUnusedAsset;
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
            _hitboxProperty = _serializedAbility.FindProperty("Hitbox");
            _comboProperty = _serializedAbility.FindProperty("Combo");
            _comboStagesProperty = _comboProperty.FindPropertyRelative("Stages");
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

            _comboStagesList = new ReorderableList(_serializedAbility, _comboStagesProperty, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Combo Stages"),
                elementHeightCallback = index => EditorGUIUtility.singleLineHeight * 7.5f + 16f,
                onAddCallback = list =>
                {
                    var index = _comboStagesProperty.arraySize;
                    _comboStagesProperty.InsertArrayElementAtIndex(index);
                    var element = _comboStagesProperty.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("StageId").stringValue = $"stage-{index + 1}";
                    element.FindPropertyRelative("DisplayName").stringValue = $"Stage {index + 1}";
                    element.FindPropertyRelative("DamageMultiplier").floatValue = 1f;
                    element.FindPropertyRelative("WindowSeconds").floatValue = 0.6f;
                    element.FindPropertyRelative("AnimationTrigger").stringValue = string.Empty;
                },
                onRemoveCallback = list =>
                {
                    if (list.index >= 0 && list.index < _comboStagesProperty.arraySize)
                    {
                        _comboStagesProperty.DeleteArrayElementAtIndex(list.index);
                    }
                }
            };

            _comboStagesList.drawElementCallback = (rect, index, active, focused) =>
            {
                var element = _comboStagesProperty.GetArrayElementAtIndex(index);
                var line = rect;
                line.height = EditorGUIUtility.singleLineHeight;
                line.y += 2f;

                var left = new Rect(line.x, line.y, line.width * 0.5f - 4f, line.height);
                var right = new Rect(line.x + line.width * 0.5f + 4f, line.y, line.width * 0.5f - 4f, line.height);

                EditorGUI.PropertyField(left, element.FindPropertyRelative("StageId"), GUIContent.none);
                EditorGUI.PropertyField(right, element.FindPropertyRelative("DisplayName"), GUIContent.none);

                line.y += line.height + 2f;
                EditorGUI.PropertyField(new Rect(line.x, line.y, line.width * 0.5f - 4f, line.height), element.FindPropertyRelative("DamageMultiplier"), new GUIContent("Damage Multiplier"));
                EditorGUI.PropertyField(new Rect(line.x + line.width * 0.5f + 4f, line.y, line.width * 0.5f - 4f, line.height), element.FindPropertyRelative("WindowSeconds"), new GUIContent("Window (s)"));

                line.y += line.height + 2f;
                EditorGUI.PropertyField(new Rect(line.x, line.y, line.width, line.height), element.FindPropertyRelative("AnimationTrigger"), new GUIContent("Animation Trigger"));

                line.y += line.height + 2f;
                EditorGUI.PropertyField(new Rect(line.x, line.y, line.width, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("HitboxOverride"), new GUIContent("Hitbox Override"), true);
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
            DrawDeliverySection();
            EditorGUILayout.Space(12f);
            DrawComboSection();
            EditorGUILayout.Space(12f);
            DrawEffectsSection();
            EditorGUILayout.Space(12f);
            DrawClassProgressionSection();
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
                _selectedAsset = (AbilityDefinition)EditorGUILayout.ObjectField(new GUIContent("Ability Asset", "AbilityDefinition asset to load into the editor."), _selectedAsset, typeof(AbilityDefinition), false, GUILayout.Width(250f));

                if (GUILayout.Button(new GUIContent("Load", "Load the selected ability asset into the working copy."), EditorStyles.toolbarButton))
                {
                    LoadFromAsset(_selectedAsset);
                }

                if (GUILayout.Button(new GUIContent("New", "Reset the editor to a fresh working copy."), EditorStyles.toolbarButton))
                {
                    ResetWorkingCopy();
                }

                if (GUILayout.Button(new GUIContent("Create Asset", "Create a new AbilityDefinition asset in the default abilities folder."), EditorStyles.toolbarButton))
                {
                    SaveAsNewAsset(_registerInClassProgression, false, GetDefaultAbilityAssetPath());
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(_selectedAsset == null))
                {
                    if (GUILayout.Button(new GUIContent("Apply To Asset", "Overwrite the selected asset with the working copy data."), EditorStyles.toolbarButton))
                    {
                        SaveToExistingAsset();
                    }
                }

                if (GUILayout.Button(new GUIContent("Save As...", "Create a new AbilityDefinition asset from the working copy."), EditorStyles.toolbarButton))
                {
                    SaveAsNewAsset(_registerInClassProgression);
                }
            }
        }

        private void DrawGeneralSection()
        {
            EditorGUILayout.LabelField("Ability Overview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Define the player-facing name, description, and icon for this ability.", MessageType.Info);
            EditorGUILayout.PropertyField(_serializedAbility.FindProperty("AbilityName"), new GUIContent("Name", "Display name shown in the UI."));
            EditorGUILayout.PropertyField(_serializedAbility.FindProperty("Description"), new GUIContent("Description", "Short summary used for tooltips and logs."));
            EditorGUILayout.PropertyField(_serializedAbility.FindProperty("Icon"), new GUIContent("Icon", "Sprite used in ability lists and hotbars."));
        }

        private void DrawTargetingSection()
        {
            EditorGUILayout.LabelField("Targeting", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure how the ability selects targets and whether it uses an area shape.", MessageType.Info);

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
            EditorGUILayout.HelpBox("Set resource costs and cooldowns used by the runtime ability system.", MessageType.Info);

            var typeProp = _resourceProperty.FindPropertyRelative("ResourceType");
            var costProp = _resourceProperty.FindPropertyRelative("Cost");
            var percentProp = _resourceProperty.FindPropertyRelative("PercentageCost");
            var castProp = _resourceProperty.FindPropertyRelative("CastSeconds");
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

            EditorGUILayout.PropertyField(castProp, new GUIContent("Cast Time (s)"));
            EditorGUILayout.PropertyField(cooldownProp, new GUIContent("Cooldown (s)"));
            EditorGUILayout.PropertyField(gcdProp, new GUIContent("Global Cooldown (s)"));
        }

        private void DrawExecutionSection()
        {
            EditorGUILayout.LabelField("Execution Conditions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Gate ability usage with movement, targeting, or buff requirements.", MessageType.Info);

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

        private void DrawClassProgressionSection()
        {
            EditorGUILayout.LabelField("Class Progression", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Optionally register this ability into the class progression asset at a specific level.", MessageType.Info);

            var updatedProgression = (ClassAbilityProgression)EditorGUILayout.ObjectField(
                new GUIContent("Progression Asset", "ClassAbilityProgression asset to update when registering this ability."),
                _progressionAsset,
                typeof(ClassAbilityProgression),
                false);

            if (updatedProgression != _progressionAsset)
            {
                _progressionAsset = updatedProgression;
                RefreshProgressionOptions();
            }

            if (_progressionAsset == null)
            {
                EditorGUILayout.HelpBox("No class progression asset selected. Assign one to enable registration.", MessageType.Warning);
                return;
            }

            if (_progressionClassOptions.Length > 0)
            {
                var options = _progressionClassOptions.Concat(new[] { "Custom..." }).ToArray();
                var selectedIndex = Array.FindIndex(_progressionClassOptions,
                    option => string.Equals(option, _progressionClassId, StringComparison.OrdinalIgnoreCase));
                if (selectedIndex < 0)
                {
                    selectedIndex = _progressionClassOptions.Length;
                }

                selectedIndex = EditorGUILayout.Popup(new GUIContent("Class Id", "Class identifier to register this ability under."),
                    selectedIndex, options);

                if (selectedIndex >= 0 && selectedIndex < _progressionClassOptions.Length)
                {
                    _progressionClassId = _progressionClassOptions[selectedIndex];
                }
                else
                {
                    _progressionClassId = EditorGUILayout.TextField(new GUIContent("Custom Class Id"), _progressionClassId);
                }
            }
            else
            {
                _progressionClassId = EditorGUILayout.TextField(new GUIContent("Class Id", "Class identifier to register this ability under."), _progressionClassId);
            }

            _progressionLevel = EditorGUILayout.IntField(new GUIContent("Unlock Level", "Level at which this ability becomes available."), _progressionLevel);

            _registerInClassProgression = EditorGUILayout.ToggleLeft(
                new GUIContent("Register in Class Progression on Save As", "Add this ability to the class progression asset when saving a new ability asset."),
                _registerInClassProgression);

            using (new EditorGUI.DisabledScope(_selectedAsset == null))
            {
                if (GUILayout.Button(new GUIContent("Register Selected Ability", "Add the selected ability asset to the class progression asset now.")))
                {
                    if (TryRegisterAbilityInProgression(_selectedAsset, out var feedback))
                    {
                        ShowNotification(new GUIContent(feedback));
                        RefreshValidation();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Register Ability", feedback, "OK");
                    }
                }
            }
        }

        private void DrawDeliverySection()
        {
            EditorGUILayout.LabelField("Hitbox Delivery", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Define the hitbox shape, size, and timing used when the ability executes.", MessageType.Info);
            if (_hitboxProperty == null)
            {
                EditorGUILayout.HelpBox("Hitbox configuration is unavailable.", MessageType.Warning);
                return;
            }

            EditorGUILayout.PropertyField(_hitboxProperty.FindPropertyRelative("Shape"));
            EditorGUILayout.PropertyField(_hitboxProperty.FindPropertyRelative("Size"), new GUIContent("Size (m)"));
            EditorGUILayout.PropertyField(_hitboxProperty.FindPropertyRelative("Radius"), new GUIContent("Radius (m)"));
            EditorGUILayout.PropertyField(_hitboxProperty.FindPropertyRelative("Length"), new GUIContent("Length (m)"));
            EditorGUILayout.PropertyField(_hitboxProperty.FindPropertyRelative("Offset"), new GUIContent("Offset"));
            EditorGUILayout.PropertyField(_hitboxProperty.FindPropertyRelative("UseCasterFacing"), new GUIContent("Use Caster Facing"));
            EditorGUILayout.PropertyField(_hitboxProperty.FindPropertyRelative("ActiveSeconds"), new GUIContent("Active Window (s)"));
            EditorGUILayout.PropertyField(_hitboxProperty.FindPropertyRelative("RequiresContact"), new GUIContent("Requires Contact"));
        }

        private void DrawComboSection()
        {
            EditorGUILayout.LabelField("Combo Chain", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Enable and configure chained stages that reward timing-based sequences.", MessageType.Info);

            var enabledProp = _comboProperty.FindPropertyRelative("Enabled");
            var resetProp = _comboProperty.FindPropertyRelative("ResetSeconds");

            EditorGUILayout.PropertyField(enabledProp, new GUIContent("Enabled"));
            EditorGUILayout.PropertyField(resetProp, new GUIContent("Reset After (s)"));

            using (new EditorGUI.DisabledScope(!enabledProp.boolValue))
            {
                _comboStagesList.DoLayoutList();
            }
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
            EditorGUILayout.HelpBox("Preview the summary text generated for in-game tooltips.", MessageType.Info);
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
            if (_workingCopy.Hitbox == null)
            {
                _workingCopy.Hitbox = new AbilityHitboxConfig();
            }
            if (_workingCopy.Combo == null)
            {
                _workingCopy.Combo = new AbilityComboChain();
            }
            if (_workingCopy.Combo.Stages == null)
            {
                _workingCopy.Combo.Stages = new List<AbilityComboStage>();
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

        private void SaveAsNewAsset(bool registerInProgression, bool showDialogOnFailure = false)
        {
            SaveAsNewAsset(registerInProgression, showDialogOnFailure, null);
        }

        private void SaveAsNewAsset(bool registerInProgression, bool showDialogOnFailure, string pathOverride)
        {
            var defaultName = GetDefaultAbilityAssetName();
            var path = pathOverride ?? EditorUtility.SaveFilePanelInProject("Save Ability", defaultName, "asset", "Choose a location for the ability definition asset.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var asset = CreateInstance<AbilityDefinition>();
            EditorUtility.CopySerialized(_workingCopy, asset);
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadFromAsset(asset);
            EditorGUIUtility.PingObject(asset);

            if (registerInProgression)
            {
                if (!TryRegisterAbilityInProgression(asset, out var feedback))
                {
                    if (showDialogOnFailure)
                    {
                        EditorUtility.DisplayDialog("Register Ability", feedback, "OK");
                    }
                    else
                    {
                        Debug.LogWarning($"Unable to register ability in class progression: {feedback}");
                    }
                }
                else
                {
                    ShowNotification(new GUIContent(feedback));
                }
            }
            else
            {
                UpdateProgressionAbilityEntries(asset);
            }
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
            UpdateProgressionAbilityEntries(_selectedAsset);
        }

        private void RefreshValidation()
        {
            _validationMessages.Clear();

            if (_workingCopy == null)
            {
                return;
            }

            EnsureProgressionAsset();

            var targeting = _workingCopy.Targeting;
            var execution = _workingCopy.Execution;
            var resource = _workingCopy.Resource;
            var abilityGuid = GetAbilityGuid();

            if (string.IsNullOrWhiteSpace(abilityGuid))
            {
                _validationMessages.Add(("Ability GUID is missing. Save the asset to generate a GUID.", MessageType.Warning));
            }

            if (_workingCopy.Icon == null)
            {
                _validationMessages.Add(("Assign an icon so the UI can display this ability without manual icon folder setup.", MessageType.Warning));
            }

            if (!string.IsNullOrWhiteSpace(abilityGuid) && _progressionAsset != null && !IsAbilityInProgression(abilityGuid))
            {
                _validationMessages.Add(("Ability GUID is not registered in the class progression asset.", MessageType.Warning));
            }

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

            if (resource.CastSeconds < 0f)
            {
                _validationMessages.Add(("Cast time cannot be negative.", MessageType.Error));
            }

            if (execution.OnlyWhileMoving && execution.OnlyWhileStationary)
            {
                _validationMessages.Add(("An ability cannot require both movement and being stationary.", MessageType.Error));
            }

            if (execution.RequiresBuffActive && string.IsNullOrWhiteSpace(execution.RequiredBuffName))
            {
                _validationMessages.Add(("Specify the buff name when 'Requires Buff Active' is enabled.", MessageType.Warning));
            }

            if (_workingCopy.Combo != null && _workingCopy.Combo.Enabled)
            {
                if (_workingCopy.Combo.Stages == null || _workingCopy.Combo.Stages.Count == 0)
                {
                    _validationMessages.Add(("Combo chain enabled but no stages were defined.", MessageType.Error));
                }
                else if (_workingCopy.Combo.Stages.Count < 2)
                {
                    _validationMessages.Add(("Combo chain has fewer than two stages.", MessageType.Warning));
                }
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

        private void EnsureProgressionAsset()
        {
            if (_progressionAsset != null)
            {
                return;
            }

            var guids = AssetDatabase.FindAssets("t:ClassAbilityProgression");
            if (guids == null || guids.Length == 0)
            {
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _progressionAsset = AssetDatabase.LoadAssetAtPath<ClassAbilityProgression>(path);
            RefreshProgressionOptions();
        }

        private void RefreshProgressionOptions()
        {
            _progressionClassOptions = Array.Empty<string>();
            if (_progressionAsset == null)
            {
                return;
            }

            var tracked = _progressionAsset.GetTrackedClassIds();
            if (tracked == null || tracked.Count == 0)
            {
                return;
            }

            _progressionClassOptions = tracked
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (_progressionClassOptions.Length == 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_progressionClassId))
            {
                _progressionClassId = _progressionClassOptions[0];
            }
        }

        private bool TryRegisterAbilityInProgression(AbilityDefinition ability, out string feedback)
        {
            if (ability == null)
            {
                feedback = "Select an ability asset before registering.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ability.Guid))
            {
                feedback = "Ability GUID is missing. Save the asset to generate a GUID.";
                return false;
            }

            if (_progressionAsset == null)
            {
                feedback = "No class progression asset is assigned.";
                return false;
            }

            var classId = _progressionClassId?.Trim();
            if (string.IsNullOrWhiteSpace(classId))
            {
                feedback = "Provide a class identifier to register the ability.";
                return false;
            }

            var level = Mathf.Max(1, _progressionLevel);
            var serializedProgression = new SerializedObject(_progressionAsset);
            var tracksProperty = serializedProgression.FindProperty("classAbilityTracks");
            if (tracksProperty == null || !tracksProperty.isArray)
            {
                feedback = "Progression asset does not expose class ability tracks.";
                return false;
            }

            SerializedProperty trackProperty = null;
            for (var i = 0; i < tracksProperty.arraySize; i++)
            {
                var element = tracksProperty.GetArrayElementAtIndex(i);
                var classIdProperty = element.FindPropertyRelative("ClassId");
                if (classIdProperty == null)
                {
                    continue;
                }

                if (string.Equals(classIdProperty.stringValue?.Trim(), classId, StringComparison.OrdinalIgnoreCase))
                {
                    trackProperty = element;
                    break;
                }
            }

            if (trackProperty == null)
            {
                tracksProperty.arraySize += 1;
                trackProperty = tracksProperty.GetArrayElementAtIndex(tracksProperty.arraySize - 1);
                var classIdProperty = trackProperty.FindPropertyRelative("ClassId");
                if (classIdProperty != null)
                {
                    classIdProperty.stringValue = classId;
                }

                var levelsProperty = trackProperty.FindPropertyRelative("Levels");
                if (levelsProperty != null && levelsProperty.isArray)
                {
                    levelsProperty.arraySize = 0;
                }
            }

            if (trackProperty == null)
            {
                feedback = "Unable to create or locate a class track for the progression asset.";
                return false;
            }

            var levelsList = trackProperty.FindPropertyRelative("Levels");
            if (levelsList == null || !levelsList.isArray)
            {
                feedback = "Progression asset class track does not expose level entries.";
                return false;
            }

            SerializedProperty levelEntry = null;
            for (var i = 0; i < levelsList.arraySize; i++)
            {
                var entry = levelsList.GetArrayElementAtIndex(i);
                var levelProperty = entry.FindPropertyRelative("Level");
                if (levelProperty != null && levelProperty.intValue == level)
                {
                    levelEntry = entry;
                    break;
                }
            }

            if (levelEntry == null)
            {
                levelsList.arraySize += 1;
                levelEntry = levelsList.GetArrayElementAtIndex(levelsList.arraySize - 1);
                var levelProperty = levelEntry.FindPropertyRelative("Level");
                if (levelProperty != null)
                {
                    levelProperty.intValue = level;
                }

                var abilitiesProperty = levelEntry.FindPropertyRelative("Abilities");
                if (abilitiesProperty != null && abilitiesProperty.isArray)
                {
                    abilitiesProperty.arraySize = 0;
                }
            }

            if (levelEntry == null)
            {
                feedback = "Unable to locate or create the level entry for registration.";
                return false;
            }

            var abilitiesList = levelEntry.FindPropertyRelative("Abilities");
            if (abilitiesList == null || !abilitiesList.isArray)
            {
                feedback = "Progression level entry does not expose ability definitions.";
                return false;
            }

            SerializedProperty abilityEntry = null;
            for (var i = 0; i < abilitiesList.arraySize; i++)
            {
                var entry = abilitiesList.GetArrayElementAtIndex(i);
                var guidProperty = entry.FindPropertyRelative("abilityGuid");
                var abilityProperty = entry.FindPropertyRelative("ability");
                if (guidProperty != null && string.Equals(guidProperty.stringValue, ability.Guid, StringComparison.OrdinalIgnoreCase))
                {
                    abilityEntry = entry;
                    break;
                }

                if (abilityProperty != null && abilityProperty.objectReferenceValue == ability)
                {
                    abilityEntry = entry;
                    break;
                }
            }

            if (abilityEntry == null)
            {
                abilitiesList.arraySize += 1;
                abilityEntry = abilitiesList.GetArrayElementAtIndex(abilitiesList.arraySize - 1);
            }

            PopulateAbilityEntry(abilityEntry, ability);

            serializedProgression.ApplyModifiedProperties();
            EditorUtility.SetDirty(_progressionAsset);
            AssetDatabase.SaveAssets();

            feedback = $"Registered {ability.AbilityName} for {classId} at level {level}.";
            return true;
        }

        private void PopulateAbilityEntry(SerializedProperty abilityEntry, AbilityDefinition ability)
        {
            if (abilityEntry == null || ability == null)
            {
                return;
            }

            var guidProperty = abilityEntry.FindPropertyRelative("abilityGuid");
            if (guidProperty != null)
            {
                guidProperty.stringValue = ability.Guid;
            }

            var abilityProperty = abilityEntry.FindPropertyRelative("ability");
            if (abilityProperty != null)
            {
                abilityProperty.objectReferenceValue = ability;
            }

            var displayNameProperty = abilityEntry.FindPropertyRelative("DisplayName");
            if (displayNameProperty != null)
            {
                displayNameProperty.stringValue = ability.AbilityName;
            }

            var descriptionProperty = abilityEntry.FindPropertyRelative("Description");
            if (descriptionProperty != null)
            {
                descriptionProperty.stringValue = ability.Description;
            }

            var tooltipProperty = abilityEntry.FindPropertyRelative("Tooltip");
            if (tooltipProperty != null)
            {
                tooltipProperty.stringValue = ability.BuildSummary();
            }
        }

        private void UpdateProgressionAbilityEntries(AbilityDefinition ability)
        {
            if (_progressionAsset == null || ability == null || string.IsNullOrWhiteSpace(ability.Guid))
            {
                return;
            }

            var serializedProgression = new SerializedObject(_progressionAsset);
            var tracksProperty = serializedProgression.FindProperty("classAbilityTracks");
            if (tracksProperty == null || !tracksProperty.isArray)
            {
                return;
            }

            var updated = false;
            for (var i = 0; i < tracksProperty.arraySize; i++)
            {
                var track = tracksProperty.GetArrayElementAtIndex(i);
                var levelsProperty = track.FindPropertyRelative("Levels");
                if (levelsProperty == null || !levelsProperty.isArray)
                {
                    continue;
                }

                for (var j = 0; j < levelsProperty.arraySize; j++)
                {
                    var levelEntry = levelsProperty.GetArrayElementAtIndex(j);
                    var abilitiesProperty = levelEntry.FindPropertyRelative("Abilities");
                    if (abilitiesProperty == null || !abilitiesProperty.isArray)
                    {
                        continue;
                    }

                    for (var k = 0; k < abilitiesProperty.arraySize; k++)
                    {
                        var abilityEntry = abilitiesProperty.GetArrayElementAtIndex(k);
                        var guidProperty = abilityEntry.FindPropertyRelative("abilityGuid");
                        var abilityProperty = abilityEntry.FindPropertyRelative("ability");
                        var matchesGuid = guidProperty != null
                            && string.Equals(guidProperty.stringValue, ability.Guid, StringComparison.OrdinalIgnoreCase);
                        var matchesReference = abilityProperty != null && abilityProperty.objectReferenceValue == ability;
                        if (!matchesGuid && !matchesReference)
                        {
                            continue;
                        }

                        PopulateAbilityEntry(abilityEntry, ability);
                        updated = true;
                    }
                }
            }

            if (!updated)
            {
                return;
            }

            serializedProgression.ApplyModifiedProperties();
            EditorUtility.SetDirty(_progressionAsset);
            AssetDatabase.SaveAssets();
        }

        private bool IsAbilityInProgression(string abilityGuid)
        {
            if (_progressionAsset == null || string.IsNullOrWhiteSpace(abilityGuid))
            {
                return false;
            }

            var classIds = _progressionAsset.GetTrackedClassIds();
            foreach (var classId in classIds)
            {
                var entries = _progressionAsset.GetProgression(classId);
                if (entries == null)
                {
                    continue;
                }

                foreach (var entry in entries)
                {
                    if (entry?.Abilities == null)
                    {
                        continue;
                    }

                    foreach (var ability in entry.Abilities)
                    {
                        if (ability == null || string.IsNullOrWhiteSpace(ability.AbilityGuid))
                        {
                            continue;
                        }

                        if (string.Equals(ability.AbilityGuid.Trim(), abilityGuid, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private string GetAbilityGuid()
        {
            if (_selectedAsset != null && !string.IsNullOrWhiteSpace(_selectedAsset.Guid))
            {
                return _selectedAsset.Guid;
            }

            return _workingCopy != null ? _workingCopy.Guid : string.Empty;
        }

        private string GetDefaultAbilityAssetName()
        {
            var rawName = _workingCopy != null ? _workingCopy.AbilityName : string.Empty;
            if (string.IsNullOrWhiteSpace(rawName))
            {
                rawName = "NewAbilityDefinition";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(rawName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "NewAbilityDefinition" : sanitized;
        }

        private string GetDefaultAbilityAssetPath()
        {
            var profile = DesignerToolkitProfile.Instance;
            var folder = profile != null ? profile.AbilityDefinitionsFolder : "Assets/ScriptableObjects/Abilities";
            if (string.IsNullOrWhiteSpace(folder))
            {
                folder = "Assets";
            }

            if (!folder.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                folder = "Assets";
            }

            EnsureFolderExists(folder);
            var defaultName = GetDefaultAbilityAssetName();
            var path = $"{folder.TrimEnd('/')}/{defaultName}.asset";
            return AssetDatabase.GenerateUniqueAssetPath(path);
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parts = folderPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
