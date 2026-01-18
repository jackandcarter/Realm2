using UnityEngine;

namespace Realm.UI.Tooltips
{
    public sealed class CombatTooltipController : MonoBehaviour
    {
        [SerializeField] private CombatTooltipView tooltipView;
        [SerializeField] private RectTransform tooltipRoot;
        [SerializeField] private Canvas tooltipCanvas;
        [SerializeField] private Vector2 cursorOffset = new(16f, -16f);

        public void ShowTooltip(CombatTooltipPayload payload)
        {
            // Task Stub 6: Hook into UI anchors and show tooltip at cursor/target position.
            if (tooltipView != null)
            {
                tooltipView.Bind(payload);
                tooltipView.gameObject.SetActive(true);
            }
        }

        public void ShowTooltip(CombatTooltipPayload payload, Vector2 screenPosition)
        {
            ShowTooltip(payload);
            UpdatePosition(screenPosition);
        }

        public void HideTooltip()
        {
            if (tooltipView != null)
            {
                tooltipView.gameObject.SetActive(false);
            }
        }

        public void UpdatePosition(Vector2 screenPosition)
        {
            if (tooltipView == null)
            {
                return;
            }

            var root = tooltipRoot != null ? tooltipRoot : tooltipView.transform as RectTransform;
            if (root == null)
            {
                return;
            }

            var anchoredTarget = screenPosition + cursorOffset;

            if (tooltipCanvas != null && tooltipCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                var canvasRect = tooltipCanvas.transform as RectTransform;
                if (canvasRect != null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        anchoredTarget,
                        tooltipCanvas.worldCamera,
                        out var localPoint);
                    root.anchoredPosition = localPoint;
                }
            }
            else
            {
                root.position = anchoredTarget;
            }
        }
    }
}
