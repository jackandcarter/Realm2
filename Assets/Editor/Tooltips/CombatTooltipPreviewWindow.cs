using System.Collections.Generic;
using Realm.Abilities;
using Realm.Combat.Data;
using Realm.Data;
using Realm.UI.Tooltips;
using UnityEditor;
using UnityEngine;

namespace Realm.EditorTools.Tooltips
{
    public sealed class CombatTooltipPreviewWindow : EditorWindow
    {
        private enum PreviewMode
        {
            StatusEffect,
            Ability,
            Equipment
        }

        private PreviewMode _mode = PreviewMode.StatusEffect;
        private StatusEffectDefinition _statusDefinition;
        private AbilityDefinition _abilityDefinition;
        private EquipmentDefinition _equipmentDefinition;
        private Vector2 _scroll;

        [MenuItem("Tools/Designer/Tooltip Preview", priority = 135)]
        public static void ShowWindow()
        {
            var window = GetWindow<CombatTooltipPreviewWindow>("Tooltip Preview");
            window.minSize = new Vector2(420f, 520f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Combat Tooltip Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Preview tooltip payloads sourced from status, ability, or equipment assets.", MessageType.Info);

            _mode = (PreviewMode)EditorGUILayout.EnumPopup("Preview Mode", _mode);

            switch (_mode)
            {
                case PreviewMode.StatusEffect:
                    _statusDefinition = (StatusEffectDefinition)EditorGUILayout.ObjectField(
                        new GUIContent("Status Effect", "StatusEffectDefinition asset to preview."),
                        _statusDefinition,
                        typeof(StatusEffectDefinition),
                        false);
                    break;
                case PreviewMode.Ability:
                    _abilityDefinition = (AbilityDefinition)EditorGUILayout.ObjectField(
                        new GUIContent("Ability", "AbilityDefinition asset to preview."),
                        _abilityDefinition,
                        typeof(AbilityDefinition),
                        false);
                    break;
                case PreviewMode.Equipment:
                    _equipmentDefinition = (EquipmentDefinition)EditorGUILayout.ObjectField(
                        new GUIContent("Equipment", "EquipmentDefinition asset to preview."),
                        _equipmentDefinition,
                        typeof(EquipmentDefinition),
                        false);
                    break;
            }

            var payload = BuildPayload();
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;
                DrawPayload(payload);
            }
        }

        private CombatTooltipPayload BuildPayload()
        {
            return _mode switch
            {
                PreviewMode.StatusEffect => CombatTooltipDataBuilder.BuildFromStatusEffect(_statusDefinition),
                PreviewMode.Ability => CombatTooltipDataBuilder.BuildFromAbility(_abilityDefinition),
                PreviewMode.Equipment => CombatTooltipDataBuilder.BuildFromItem(_equipmentDefinition),
                _ => default
            };
        }

        private static void DrawPayload(CombatTooltipPayload payload)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Payload", EditorStyles.boldLabel);

            if (payload.Icon != null)
            {
                var texture = AssetPreview.GetAssetPreview(payload.Icon) ?? payload.Icon.texture;
                if (texture != null)
                {
                    GUILayout.Label(texture, GUILayout.Width(64f), GUILayout.Height(64f));
                }
            }

            DrawLabel("Title", payload.Title);
            DrawLabel("Description", payload.Description);
            DrawLabel("Duration", !string.IsNullOrWhiteSpace(payload.DurationLabel)
                ? payload.DurationLabel
                : payload.DurationSeconds > 0f ? $"{payload.DurationSeconds:0.##}s" : string.Empty);
            DrawLabel("Max Stacks", payload.MaxStacks > 1 ? payload.MaxStacks.ToString() : string.Empty);
            DrawLabel("Refresh Rule", payload.RefreshRule);
            DrawLabel("Dispel Type", payload.DispelType);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Stat Modifiers", EditorStyles.boldLabel);
            if (payload.StatModifiers == null)
            {
                EditorGUILayout.LabelField("None");
                return;
            }

            foreach (var modifier in payload.StatModifiers)
            {
                DrawModifier(modifier);
            }

            if (payload.StatModifiers.Count == 0)
            {
                EditorGUILayout.LabelField("None");
            }
        }

        private static void DrawLabel(string label, string value)
        {
            EditorGUILayout.LabelField(label, string.IsNullOrWhiteSpace(value) ? "—" : value);
        }

        private static void DrawModifier(CombatTooltipStatModifier modifier)
        {
            var parts = new List<string>
            {
                modifier.StatId
            };

            if (modifier.FlatDelta != 0f)
            {
                parts.Add(modifier.FlatDelta.ToString("+0.##;-0.##;0"));
            }

            if (modifier.PercentDelta != 0f)
            {
                parts.Add(modifier.PercentDelta.ToString("+0.##%;-0.##%;0%"));
            }

            if (!string.IsNullOrWhiteSpace(modifier.SourceLabel))
            {
                parts.Add($"({modifier.SourceLabel})");
            }

            EditorGUILayout.LabelField(string.Join(" ", parts));
        }
    }
}
