using System;
using System.Collections.Generic;
using Realm.Combat.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.EditorTools
{
    internal static class AbilityDesignerStatusEffectStubs
    {
        // Task Stub 12: Extend Ability Designer UI to surface refresh rules, max stacks, dispel types.
        public static void DrawStatusEffectMetadata(SerializedProperty applyStatusEffectProperty)
        {
            if (applyStatusEffectProperty == null)
            {
                return;
            }

            var statusIdProperty = applyStatusEffectProperty.FindPropertyRelative("StatusId")
                                   ?? applyStatusEffectProperty.FindPropertyRelative("StateName");
            if (statusIdProperty == null)
            {
                return;
            }

            var line = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            DrawStatusEffectMetadata(line, statusIdProperty);
        }

        public static void DrawStatusEffectMetadata(Rect rect, SerializedProperty statusIdProperty)
        {
            if (statusIdProperty == null)
            {
                return;
            }

            var statusId = statusIdProperty.stringValue;
            var line = rect;
            line.height = EditorGUIUtility.singleLineHeight;

            if (string.IsNullOrWhiteSpace(statusId))
            {
                EditorGUI.LabelField(line, "Status Metadata", "No status assigned");
                return;
            }

            var definition = ResolveStatusDefinition(statusId.Trim());
            if (definition == null)
            {
                EditorGUI.LabelField(line, "Status Metadata", "Status asset not found");
                return;
            }

            var metadata = $"Refresh: {definition.RefreshRule} | Max: {Mathf.Max(1, definition.MaxStacks)} | Dispel: {definition.DispelType}";
            EditorGUI.LabelField(line, "Status Metadata", metadata);

            if (definition.MaxStacks > 1 && definition.RefreshRule != StatusRefreshRule.AddStacks)
            {
                line.y += line.height + 2f;
                EditorGUI.LabelField(line, "Warning", "MaxStacks > 1 but RefreshRule is not AddStacks");
            }
        }

        private static StatusEffectDefinition ResolveStatusDefinition(string statusId)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                return null;
            }

            var lookup = StatusEffectCache.Lookup;
            if (lookup.Count == 0)
            {
                StatusEffectCache.Refresh();
                lookup = StatusEffectCache.Lookup;
            }

            return lookup.TryGetValue(statusId, out var definition) ? definition : null;
        }

        private static class StatusEffectCache
        {
            private static readonly Dictionary<string, StatusEffectDefinition> Cache = new(StringComparer.OrdinalIgnoreCase);
            public static IReadOnlyDictionary<string, StatusEffectDefinition> Lookup => Cache;

            public static void Refresh()
            {
                Cache.Clear();
                var guids = AssetDatabase.FindAssets("t:StatusEffectDefinition");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>(path);
                    if (asset == null || string.IsNullOrWhiteSpace(asset.StatusId))
                    {
                        continue;
                    }

                    Cache[asset.StatusId.Trim()] = asset;
                }
            }
        }
    }
}
