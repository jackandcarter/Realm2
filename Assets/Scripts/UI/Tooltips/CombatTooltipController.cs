using UnityEngine;

namespace Realm.UI.Tooltips
{
    public sealed class CombatTooltipController : MonoBehaviour
    {
        [SerializeField] private CombatTooltipView tooltipView;

        public void ShowTooltip(CombatTooltipPayload payload)
        {
            // Task Stub 6: Hook into UI anchors and show tooltip at cursor/target position.
            if (tooltipView != null)
            {
                tooltipView.Bind(payload);
                tooltipView.gameObject.SetActive(true);
            }
        }

        public void HideTooltip()
        {
            if (tooltipView != null)
            {
                tooltipView.gameObject.SetActive(false);
            }
        }
    }
}
