using Client.Terrain;
using UnityEditor;
using UnityEngine;

namespace Client.Editor
{
    [CustomEditor(typeof(TerrainRegionManager))]
    public class TerrainRegionManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var manager = (TerrainRegionManager)target;
            GUILayout.Space(8f);

            if (GUILayout.Button("Refresh Regions From Scene"))
            {
                manager.RefreshRegions();
                EditorUtility.SetDirty(manager);
            }
        }
    }
}
