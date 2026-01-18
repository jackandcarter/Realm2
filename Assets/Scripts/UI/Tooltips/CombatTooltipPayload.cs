using System;
using System.Collections.Generic;
using UnityEngine;

namespace Realm.UI.Tooltips
{
    [Serializable]
    public struct CombatTooltipStatModifier
    {
        public string StatId;
        public float FlatDelta;
        public float PercentDelta;
        public string SourceLabel;
    }

    [Serializable]
    public struct CombatTooltipPayload
    {
        public string Title;
        public string Description;
        public Sprite Icon;
        public IReadOnlyList<CombatTooltipStatModifier> StatModifiers;
        public float DurationSeconds;
        public string DurationLabel;
        public int MaxStacks;
        public string RefreshRule;
        public string DispelType;
    }
}
