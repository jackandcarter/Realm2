using Client.UI;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.UI
{
    [CustomEditor(typeof(ArkitectUIManager))]
    public class ArkitectUIManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate UI Foundation"))
            {
                ArkitectUiFoundationGenerator.GenerateFoundation();
                GUIUtility.ExitGUI();
            }
        }
    }
}
