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
            if (percentDelta != 0f)
            {
                return percentDelta.ToString("+0.##%;-0.##%;0%") ;
            }

            return flatDelta.ToString("+0.##;-0.##;0");
        }
    }
}
