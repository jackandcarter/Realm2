using UnityEditor;
using UnityEngine;

namespace Realm.EditorTools
{
    internal static class AbilityDesignerStatusEffectStubs
    {
        // Task Stub 12: Extend Ability Designer UI to surface refresh rules, max stacks, dispel types.
        public static void DrawStatusEffectMetadata(SerializedProperty applyStatusEffectProperty)
        {
            // TODO: Locate StatusEffectDefinition reference in the property and draw metadata fields.
            if (applyStatusEffectProperty == null)
            {
                return;
            }

            EditorGUILayout.HelpBox("Status effect metadata UI stub. Implement in AbilityDesignerWindow.", MessageType.Info);
        }
    }
}
