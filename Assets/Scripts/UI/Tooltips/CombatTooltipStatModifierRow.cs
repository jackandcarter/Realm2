using TMPro;
using UnityEngine;

namespace Realm.UI.Tooltips
{
    public sealed class CombatTooltipStatModifierRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text statLabel;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private TMP_Text sourceLabel;

        public void Bind(CombatTooltipStatModifier modifier)
        {
            // Task Stub 5: Format stat modifier values (flat/percent) and apply sign coloring.
            if (statLabel != null)
            {
                statLabel.text = modifier.StatId;
            }

            if (valueLabel != null)
            {
                valueLabel.text = FormatModifier(modifier.FlatDelta, modifier.PercentDelta);
            }

            if (sourceLabel != null)
            {
                sourceLabel.text = modifier.SourceLabel;
            }
        }

        private static string FormatModifier(float flatDelta, float percentDelta)
        {
            var value = percentDelta != 0f
                ? percentDelta.ToString("+0.##%;-0.##%;0%")
                : flatDelta.ToString("+0.##;-0.##;0");

            var delta = percentDelta != 0f ? percentDelta : flatDelta;
            if (delta > 0f)
            {
                return $"<color=#7BE082>{value}</color>";
            }

            if (delta < 0f)
            {
                return $"<color=#F26D6D>{value}</color>";
            }

            return value;
        }
    }
}
