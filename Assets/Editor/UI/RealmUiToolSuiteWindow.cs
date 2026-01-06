using UnityEditor;
using UnityEngine;

namespace Realm.Editor.UI
{
    public class RealmUiToolSuiteWindow : EditorWindow
    {
        [MenuItem("Tools/Realm/UI/Realm UI Tool Suite", priority = 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<RealmUiToolSuiteWindow>("Realm UI Tool Suite");
            window.minSize = new Vector2(360f, 280f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("UI Generators", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            DrawSection("Core Foundations", () =>
            {
                if (GUILayout.Button("Generate Gameplay HUD"))
                {
                    GameplayHudGenerator.GenerateGameplayHud();
                }

                if (GUILayout.Button("Generate Arkitect UI Foundation"))
                {
                    ArkitectUiFoundationGenerator.GenerateFoundation();
                }
            });

            DrawSection("Dock Modules", () =>
            {
                if (GUILayout.Button("Generate HUD Dock"))
                {
                    HudDockGenerator.GenerateHudDock();
                }

                if (GUILayout.Button("Generate Class Ability Dock"))
                {
                    ClassAbilityDockGenerator.GenerateClassDock();
                }

                if (GUILayout.Button("Generate Arkitect Dock"))
                {
                    ArkitectDockGenerator.GenerateDock();
                }
            });

            DrawSection("Arkitect Extensions", () =>
            {
                if (GUILayout.Button("Generate Arkitect Terrain Tools UI"))
                {
                    ArkitectTerrainUiGenerator.GenerateTerrainUi();
                }
            });
        }

        private static void DrawSection(string title, System.Action drawContent)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            drawContent?.Invoke();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(8f);
        }
    }
}
