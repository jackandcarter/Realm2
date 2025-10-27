using Client.Map;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI.Maps
{
    /// <summary>
    /// Runtime controller responsible for rendering map tooltip content and sizing the panel to match the
    /// supplied text entries.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapTooltipView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image background;
        [SerializeField] private Text titleLabel;
        [SerializeField] private RectTransform titleRect;
        [SerializeField] private GameObject subtitleContainer;
        [SerializeField] private Text subtitleLabel;
        [SerializeField] private RectTransform subtitleRect;
        [SerializeField] private GameObject separatorContainer;
        [SerializeField] private RectTransform separatorRect;
        [SerializeField] private GameObject descriptionContainer;
        [SerializeField] private Text descriptionLabel;
        [SerializeField] private RectTransform descriptionRect;
        [SerializeField] private float horizontalPadding = 16f;
        [SerializeField] private float verticalPadding = 12f;
        [SerializeField] private float spacing = 6f;
        [SerializeField] private float separatorThickness = 1.5f;
        [SerializeField] private float minimumWidth = 200f;
        [SerializeField] private float maximumWidth = 360f;

        private RectTransform _rectTransform;

        /// <summary>
        /// Provides direct access to the tooltip RectTransform for layout calculations.
        /// </summary>
        public RectTransform RectTransform => _rectTransform != null ? _rectTransform : (_rectTransform = GetComponent<RectTransform>());

        private void Awake()
        {
            _rectTransform = RectTransform;

            if (root == null)
            {
                root = _rectTransform;
            }

            if (titleRect == null && titleLabel != null)
            {
                titleRect = titleLabel.rectTransform;
            }

            if (subtitleRect == null && subtitleLabel != null)
            {
                subtitleRect = subtitleLabel.rectTransform;
            }

            if (descriptionRect == null && descriptionLabel != null)
            {
                descriptionRect = descriptionLabel.rectTransform;
            }

            if (separatorRect == null && separatorContainer != null)
            {
                separatorRect = separatorContainer.GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// Populates the tooltip labels with the provided data. Empty segments are collapsed automatically.
        /// </summary>
        public void SetContent(MapPinData pin)
        {
            if (pin == null)
            {
                ApplyContent(string.Empty, string.Empty, string.Empty);
                return;
            }

            ApplyContent(pin.DisplayName, pin.DisplaySubTitle, pin.Tooltip);
        }

        /// <summary>
        /// Populates the tooltip with raw text values. Empty segments are collapsed automatically.
        /// </summary>
        public void SetContent(string title, string subtitle, string description)
        {
            ApplyContent(title, subtitle, description);
        }

        /// <summary>
        /// Enables or disables the tooltip visuals without destroying the component hierarchy.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (background != null)
            {
                background.enabled = visible;
            }

            if (_rectTransform != null)
            {
                _rectTransform.gameObject.SetActive(visible);
            }
        }

        private void ApplyContent(string title, string subtitle, string description)
        {
            if (titleLabel != null)
            {
                titleLabel.text = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
            }

            bool hasSubtitle = !string.IsNullOrWhiteSpace(subtitle);
            if (subtitleLabel != null)
            {
                subtitleLabel.text = hasSubtitle ? subtitle.Trim() : string.Empty;
            }

            if (subtitleContainer != null)
            {
                subtitleContainer.SetActive(hasSubtitle);
            }

            bool hasDescription = !string.IsNullOrWhiteSpace(description);
            if (descriptionLabel != null)
            {
                descriptionLabel.text = hasDescription ? description.Trim() : string.Empty;
            }

            if (descriptionContainer != null)
            {
                descriptionContainer.SetActive(hasDescription);
            }

            if (separatorContainer != null)
            {
                separatorContainer.SetActive(hasSubtitle && hasDescription);
            }

            RecalculateLayout();
        }

        /// <summary>
        /// Forces the tooltip rect transform to resize to match the preferred content dimensions.
        /// </summary>
        public void RecalculateLayout()
        {
            var rect = RectTransform;
            if (rect == null)
            {
                return;
            }

            float minInnerWidth = Mathf.Max(0f, minimumWidth - (horizontalPadding * 2f));
            float maxInnerWidth = Mathf.Max(minInnerWidth, maximumWidth - (horizontalPadding * 2f));

            float titleWidth = titleRect != null ? LayoutUtility.GetPreferredWidth(titleRect) : 0f;
            float subtitleWidth = subtitleContainer != null && subtitleContainer.activeSelf && subtitleRect != null
                ? LayoutUtility.GetPreferredWidth(subtitleRect)
                : 0f;
            float descriptionWidth = descriptionContainer != null && descriptionContainer.activeSelf && descriptionRect != null
                ? LayoutUtility.GetPreferredWidth(descriptionRect)
                : 0f;

            float innerWidth = Mathf.Clamp(Mathf.Max(minInnerWidth, titleWidth, subtitleWidth, descriptionWidth), minInnerWidth, maxInnerWidth);

            if (titleRect != null)
            {
                titleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerWidth);
            }

            if (subtitleRect != null)
            {
                subtitleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerWidth);
            }

            if (descriptionRect != null)
            {
                descriptionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerWidth);
            }

            float height = verticalPadding * 2f;
            float currentTop = -verticalPadding;

            if (titleRect != null)
            {
                float titleHeight = LayoutUtility.GetPreferredHeight(titleRect);
                PositionElement(titleRect, horizontalPadding, currentTop, innerWidth, titleHeight);
                currentTop -= titleHeight;
                height += titleHeight;
            }

            if (subtitleContainer != null && subtitleContainer.activeSelf && subtitleRect != null)
            {
                currentTop -= spacing;
                height += spacing;

                float subtitleHeight = LayoutUtility.GetPreferredHeight(subtitleRect);
                PositionElement(subtitleRect, horizontalPadding, currentTop, innerWidth, subtitleHeight);
                currentTop -= subtitleHeight;
                height += subtitleHeight;
            }

            if (separatorContainer != null && separatorContainer.activeSelf && separatorRect != null)
            {
                currentTop -= spacing;
                height += spacing;

                PositionElement(separatorRect, horizontalPadding, currentTop, innerWidth, separatorThickness);
                currentTop -= separatorThickness;
                height += separatorThickness;
            }

            if (descriptionContainer != null && descriptionContainer.activeSelf && descriptionRect != null)
            {
                currentTop -= spacing;
                height += spacing;

                float descriptionHeight = LayoutUtility.GetPreferredHeight(descriptionRect);
                PositionElement(descriptionRect, horizontalPadding, currentTop, innerWidth, descriptionHeight);
                currentTop -= descriptionHeight;
                height += descriptionHeight;
            }

            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerWidth + (horizontalPadding * 2f));
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private static void PositionElement(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, top);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
