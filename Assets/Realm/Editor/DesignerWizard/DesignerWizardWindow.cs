using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.DesignerWizard
{
    internal class DesignerWizardWindow : EditorWindow
    {
        private static readonly string[] StepNames =
        {
            "Stats",
            "Classes",
            "Abilities",
            "Battle Hooks"
        };

        private static readonly string[] StepHelpText =
        {
            "Define the statistical foundations for your realm. Consider attributes, scaling curves, and derived values that will inform balance decisions.",
            "Outline available classes, their archetypes, and any prerequisites. Use this area to capture class fantasies and role expectations.",
            "Document the abilities available in combat, including resource costs, cooldowns, and synergy notes.",
            "Describe battle hooks, encounter scripts, and special triggers that should activate during combat scenarios."
        };

        private Vector2 _scrollPosition;
        private DesignerWizardState _state;

        [MenuItem("Realm/Designer Wizard")]
        private static void ShowWindow()
        {
            var window = GetWindow<DesignerWizardWindow>(false, "Designer Wizard", true);
            window.Show();
        }

        private void OnEnable()
        {
            _state = DesignerWizardState.Instance;
            EnsureStepData();
        }

        private void OnGUI()
        {
            if (_state == null)
            {
                EditorGUILayout.HelpBox("Unable to load designer wizard state.", MessageType.Error);
                return;
            }

            DrawToolbar();
            EditorGUILayout.Space();
            DrawBreadcrumbs();
            DrawProgressIndicator();
            EditorGUILayout.Space();
            DrawContentArea();
            EditorGUILayout.Space();
            DrawNavigationButtons();
        }

        private void DrawToolbar()
        {
            EditorGUI.BeginChangeCheck();
            var newIndex = GUILayout.Toolbar(_state.CurrentStepIndex, StepNames);
            if (EditorGUI.EndChangeCheck())
            {
                SetStep(newIndex);
            }
        }

        private void DrawBreadcrumbs()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (var i = 0; i < StepNames.Length; i++)
                {
                    var isCurrent = i == _state.CurrentStepIndex;
                    var content = new GUIContent($"{i + 1}. {StepNames[i]}");
                    var style = isCurrent ? EditorStyles.boldLabel : EditorStyles.label;
                    GUILayout.Label(content, style);

                    if (i < StepNames.Length - 1)
                    {
                        GUILayout.Label("›", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(12f));
                    }
                }
            }
        }

        private void DrawProgressIndicator()
        {
            var completion = _state.CompletedStepCount / (float)StepNames.Length;
            var rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, completion, $"Progress {Mathf.RoundToInt(completion * 100f)}%");
        }

        private void DrawContentArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    DrawCurrentStepContent();
                }

                EditorGUILayout.Space(8f);

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(220f)))
                {
                    DrawHelpPane();
                }
            }
        }

        private void DrawCurrentStepContent()
        {
            var stepData = _state.GetStepData(_state.CurrentStepIndex);

            EditorGUILayout.LabelField(StepNames[_state.CurrentStepIndex], EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            var isComplete = EditorGUILayout.ToggleLeft("Mark step as complete", stepData.IsComplete);
            if (EditorGUI.EndChangeCheck())
            {
                stepData.IsComplete = isComplete;
                _state.MarkDirty();
            }

            EditorGUILayout.LabelField("Notes", EditorStyles.label);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
            EditorGUI.BeginChangeCheck();
            var updatedNotes = EditorGUILayout.TextArea(stepData.Notes, GUILayout.ExpandHeight(true));
            if (EditorGUI.EndChangeCheck())
            {
                stepData.Notes = updatedNotes;
                _state.MarkDirty();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHelpPane()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Contextual Help", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(StepHelpText[_state.CurrentStepIndex], MessageType.Info);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Step Summary", EditorStyles.miniBoldLabel);
                var summary = _state.GetStepData(_state.CurrentStepIndex).IsComplete
                    ? "This step is marked as complete."
                    : "This step is still in progress.";
                EditorGUILayout.LabelField(summary, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawNavigationButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_state.CurrentStepIndex <= 0))
                {
                    if (GUILayout.Button("Previous"))
                    {
                        SetStep(Mathf.Max(0, _state.CurrentStepIndex - 1));
                    }
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(_state.CurrentStepIndex >= StepNames.Length - 1))
                {
                    if (GUILayout.Button("Next"))
                    {
                        SetStep(Mathf.Min(StepNames.Length - 1, _state.CurrentStepIndex + 1));
                    }
                }
            }
        }

        private void SetStep(int newIndex)
        {
            if (newIndex == _state.CurrentStepIndex)
            {
                return;
            }

            _state.CurrentStepIndex = Mathf.Clamp(newIndex, 0, StepNames.Length - 1);
            _state.MarkDirty();
        }

        private void EnsureStepData()
        {
            if (_state == null)
            {
                return;
            }

            _state.EnsureStepData(StepNames.Length);
        }
    }

    [FilePath("Realm/DesignerWizard/DesignerWizardState.asset", FilePathAttribute.Location.PreferencesFolder)]
    internal class DesignerWizardState : ScriptableSingleton<DesignerWizardState>
    {
        [Serializable]
        internal class StepData
        {
            public bool IsComplete;
            public string Notes = string.Empty;
        }

        [SerializeField]
        private int currentStepIndex;

        [SerializeField]
        private List<StepData> steps = new List<StepData>();

        internal static DesignerWizardState Instance => instance;

        internal int CurrentStepIndex
        {
            get => Mathf.Clamp(currentStepIndex, 0, Mathf.Max(0, steps.Count - 1));
            set => currentStepIndex = value;
        }

        internal int CompletedStepCount
        {
            get
            {
                var completed = 0;
                foreach (var step in steps)
                {
                    if (step.IsComplete)
                    {
                        completed++;
                    }
                }

                return completed;
            }
        }

        internal StepData GetStepData(int index)
        {
            if (index < 0 || index >= steps.Count)
            {
                return new StepData();
            }

            return steps[index];
        }

        internal void EnsureStepData(int length)
        {
            while (steps.Count < length)
            {
                steps.Add(new StepData());
            }

            if (steps.Count > length)
            {
                steps.RemoveRange(length, steps.Count - length);
            }

            CurrentStepIndex = Mathf.Clamp(CurrentStepIndex, 0, length - 1);
            SaveIfDirty();
        }

        internal void MarkDirty()
        {
            SaveIfDirty();
        }

        private void SaveIfDirty()
        {
            Save(true);
        }
    }
}
