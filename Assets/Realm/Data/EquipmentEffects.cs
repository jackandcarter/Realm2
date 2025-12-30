using System;
using System.Collections.Generic;
using Realm.Abilities;
using UnityEngine;

namespace Realm.Data
{
    public enum EquipmentEquipEffectType
    {
        StatModifier,
        GrantAbility,
        Custom
    }

    [Serializable]
    public class EquipmentEquipEffect
    {
        public string Label = "New Effect";
        [TextArea(2, 4)]
        public string Description;
        public EquipmentEquipEffectType EffectType = EquipmentEquipEffectType.StatModifier;
        public StatDefinition Stat;
        public float FlatModifier;
        public float PercentModifier;
        public AbilityDefinition GrantedAbility;
        public bool AddToAbilityDock;
        public Sprite DockIconOverride;
        public string CustomPayload;

        public string BuildSummary()
        {
            var summaryParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Label))
            {
                summaryParts.Add(Label.Trim());
            }

            if (!string.IsNullOrWhiteSpace(Description))
            {
                summaryParts.Add(Description.Trim());
            }

            return string.Join(" • ", summaryParts);
        }
    }
}
