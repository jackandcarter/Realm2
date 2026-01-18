using System.Collections.Generic;
using System.Linq;
using System.Text;
using Client.Combat.StatusEffects;
using Realm.Abilities;
using Realm.Combat.Data;
using Realm.Data;
using UnityEngine;

namespace Realm.UI.Tooltips
{
    public static class CombatTooltipDataBuilder
    {
        public static CombatTooltipPayload BuildFromStatusEffect(Object statusDefinition)
        {
            if (statusDefinition is not StatusEffectDefinition definition)
            {
                return BuildEmptyPayload();
            }

            var modifiers = new List<CombatTooltipStatModifier>();
            if (definition.Modifiers != null)
            {
                foreach (var modifier in definition.Modifiers)
                {
                    if (modifier == null || string.IsNullOrWhiteSpace(modifier.StatId))
                    {
                        continue;
                    }

                    modifiers.Add(new CombatTooltipStatModifier
                    {
                        StatId = ResolveStatLabel(modifier.StatId.Trim()),
                        FlatDelta = modifier.Value,
                        PercentDelta = 0f,
                        SourceLabel = definition.Type.ToString()
                    });
                }
            }

            var durationLabel = string.IsNullOrWhiteSpace(definition.DurationModelId)
                ? string.Empty
                : $"Model {definition.DurationModelId.Trim()}";

            return new CombatTooltipPayload
            {
                Title = ResolveTitle(definition.DisplayName, definition.name, definition.StatusId),
                Description = BuildStatusDescription(definition),
                Icon = definition.Icon,
                StatModifiers = modifiers,
                DurationSeconds = 0f,
                DurationLabel = durationLabel,
                MaxStacks = definition.MaxStacks,
                RefreshRule = definition.RefreshRule.ToString(),
                DispelType = definition.DispelType.ToString()
            };
        }

        public static CombatTooltipPayload BuildFromAbility(Object abilityDefinition)
        {
            if (abilityDefinition is not AbilityDefinition ability)
            {
                return BuildEmptyPayload();
            }

            var modifiers = new List<CombatTooltipStatModifier>();
            var statusDefinitions = new List<StatusEffectDefinition>();
            var duration = 0f;

            if (ability.Effects != null)
            {
                foreach (var effect in ability.Effects)
                {
                    if (effect == null)
                    {
                        continue;
                    }

                    if (effect.DurationSeconds > duration)
                    {
                        duration = effect.DurationSeconds;
                    }

                    if (string.IsNullOrWhiteSpace(effect.StateName))
                    {
                        continue;
                    }

                    if (StatusEffectRegistry.TryGetStatus(effect.StateName.Trim(), out var statusDefinition))
                    {
                        statusDefinitions.Add(statusDefinition);
                        AppendStatusModifiers(statusDefinition, modifiers);
                    }
                }
            }

            var statusMetadata = ResolveStatusMetadata(statusDefinitions);

            return new CombatTooltipPayload
            {
                Title = ResolveTitle(ability.AbilityName, ability.name, ability.Guid),
                Description = ability.BuildSummary(),
                Icon = ability.Icon,
                StatModifiers = modifiers,
                DurationSeconds = duration,
                DurationLabel = string.Empty,
                MaxStacks = statusMetadata.MaxStacks,
                RefreshRule = statusMetadata.RefreshRule,
                DispelType = statusMetadata.DispelType
            };
        }

        public static CombatTooltipPayload BuildFromItem(Object itemDefinition)
        {
            if (itemDefinition is not EquipmentDefinition equipment)
            {
                return BuildEmptyPayload();
            }

            var modifiers = new List<CombatTooltipStatModifier>();
            var descriptionSegments = new List<string>();

            if (!string.IsNullOrWhiteSpace(equipment.Description))
            {
                descriptionSegments.Add(equipment.Description.Trim());
            }

            if (equipment.EquipEffects != null)
            {
                foreach (var effect in equipment.EquipEffects)
                {
                    if (effect == null)
                    {
                        continue;
                    }

                    if (effect.EffectType == EquipmentEquipEffectType.StatModifier)
                    {
                        var statName = ResolveStatLabel(effect.Stat != null && !string.IsNullOrWhiteSpace(effect.Stat.DisplayName)
                            ? effect.Stat.DisplayName
                            : effect.Stat != null ? effect.Stat.name : string.Empty);
                        var resolvedStat = string.IsNullOrWhiteSpace(statName)
                            ? ResolveStatLabel(effect.Stat != null ? effect.Stat.name : string.Empty)
                            : statName;

                        modifiers.Add(new CombatTooltipStatModifier
                        {
                            StatId = string.IsNullOrWhiteSpace(resolvedStat) ? "Stat" : resolvedStat,
                            FlatDelta = effect.FlatModifier,
                            PercentDelta = effect.PercentModifier,
                            SourceLabel = string.IsNullOrWhiteSpace(effect.Label) ? "Equipment" : effect.Label.Trim()
                        });
                        continue;
                    }

                    if (effect.EffectType == EquipmentEquipEffectType.GrantAbility && effect.GrantedAbility != null)
                    {
                        descriptionSegments.Add($"Grants ability: {ResolveTitle(effect.GrantedAbility.AbilityName, effect.GrantedAbility.name, effect.GrantedAbility.Guid)}");
                        continue;
                    }

                    var summary = effect.BuildSummary();
                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        descriptionSegments.Add(summary);
                    }
                }
            }

            return new CombatTooltipPayload
            {
                Title = ResolveTitle(equipment.DisplayName, equipment.name, equipment.Guid),
                Description = string.Join(" \u2022 ", descriptionSegments),
                Icon = equipment.InventoryIcon,
                StatModifiers = modifiers,
                DurationSeconds = 0f,
                DurationLabel = string.Empty,
                MaxStacks = 0,
                RefreshRule = string.Empty,
                DispelType = string.Empty
            };
        }

        private static CombatTooltipPayload BuildEmptyPayload()
        {
            return new CombatTooltipPayload
            {
                Title = string.Empty,
                Description = string.Empty,
                Icon = null,
                StatModifiers = new List<CombatTooltipStatModifier>(),
                DurationSeconds = 0f,
                DurationLabel = string.Empty,
                MaxStacks = 0,
                RefreshRule = string.Empty,
                DispelType = string.Empty
            };
        }

        private static void AppendStatusModifiers(StatusEffectDefinition definition, List<CombatTooltipStatModifier> modifiers)
        {
            if (definition?.Modifiers == null || modifiers == null)
            {
                return;
            }

            foreach (var modifier in definition.Modifiers)
            {
                if (modifier == null || string.IsNullOrWhiteSpace(modifier.StatId))
                {
                    continue;
                }

                modifiers.Add(new CombatTooltipStatModifier
                {
                    StatId = ResolveStatLabel(modifier.StatId.Trim()),
                    FlatDelta = modifier.Value,
                    PercentDelta = 0f,
                    SourceLabel = string.IsNullOrWhiteSpace(definition.DisplayName)
                        ? definition.Type.ToString()
                        : definition.DisplayName.Trim()
                });
            }
        }

        private static string ResolveTitle(string primary, string fallbackName, string fallbackId)
        {
            if (!string.IsNullOrWhiteSpace(primary))
            {
                return primary.Trim();
            }

            if (!string.IsNullOrWhiteSpace(fallbackName))
            {
                return fallbackName.Trim();
            }

            return fallbackId?.Trim() ?? string.Empty;
        }

        private static (int MaxStacks, string RefreshRule, string DispelType) ResolveStatusMetadata(
            IReadOnlyList<StatusEffectDefinition> definitions)
        {
            if (definitions == null)
            {
                return (0, string.Empty, string.Empty);
            }

            var distinct = definitions.Where(definition => definition != null).Distinct().ToList();
            if (distinct.Count != 1)
            {
                return distinct.Count > 1
                    ? (0, "Multiple", "Multiple")
                    : (0, string.Empty, string.Empty);
            }

            var definition = distinct[0];
            return (definition.MaxStacks, definition.RefreshRule.ToString(), definition.DispelType.ToString());
        }

        private static string BuildStatusDescription(StatusEffectDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            var segments = new List<string>();
            segments.Add(definition.Type.ToString());

            var restrictions = definition.ActionRestrictions;
            if (restrictions != null)
            {
                if (restrictions.BlocksAllActions)
                {
                    segments.Add("Blocks all actions");
                }
                else if (restrictions.BlocksAbilities)
                {
                    segments.Add("Blocks abilities");
                }
            }

            var periodic = definition.PeriodicEffects;
            if (periodic != null && periodic.TickRateSeconds > 0f)
            {
                segments.Add($"Ticks every {periodic.TickRateSeconds:0.##}s");
            }

            return string.Join(" \u2022 ", segments);
        }

        private static string ResolveStatLabel(string statId)
        {
            if (string.IsNullOrWhiteSpace(statId))
            {
                return string.Empty;
            }

            var trimmed = statId.Trim();
            var segments = trimmed.Split('.');
            var raw = segments.Length > 0 ? segments[^1] : trimmed;
            raw = raw.Replace("_", " ");

            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(raw.Length + 8);
            builder.Append(char.ToUpperInvariant(raw[0]));

            for (var i = 1; i < raw.Length; i++)
            {
                var current = raw[i];
                if (char.IsUpper(current) && raw[i - 1] != ' ')
                {
                    builder.Append(' ');
                }

                builder.Append(current);
            }

            return builder.ToString();
        }
    }
}
