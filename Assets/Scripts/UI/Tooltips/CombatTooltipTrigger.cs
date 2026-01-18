using UnityEngine;
using UnityEngine.EventSystems;

namespace Realm.UI.Tooltips
{
    public enum CombatTooltipSourceType
    {
        Ability,
        Item,
        Equipment,
        Status
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CombatTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] private CombatTooltipController tooltipController;
        [SerializeField] private CombatTooltipSourceType sourceType;
        [SerializeField] private Object definition;

        private CombatTooltipPayload _payload;
        private bool _hasPayload;

        private void Awake()
        {
            ResolveController();
            BuildPayload();
        }

        private void OnEnable()
        {
            BuildPayload();
        }

        private void OnValidate()
        {
            BuildPayload();
        }

        public void Configure(CombatTooltipController controller, CombatTooltipSourceType type, Object sourceDefinition)
        {
            tooltipController = controller;
            sourceType = type;
            definition = sourceDefinition;
            ResolveController();
            BuildPayload();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_hasPayload)
            {
                return;
            }

            ResolveController();
            if (tooltipController == null)
            {
                return;
            }

            tooltipController.ShowTooltip(_payload, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipController != null)
            {
                tooltipController.HideTooltip();
            }
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (tooltipController != null)
            {
                tooltipController.UpdatePosition(eventData.position);
            }
        }

        private void BuildPayload()
        {
            _payload = sourceType switch
            {
                CombatTooltipSourceType.Ability => CombatTooltipDataBuilder.BuildFromAbility(definition),
                CombatTooltipSourceType.Item => CombatTooltipDataBuilder.BuildFromItem(definition),
                CombatTooltipSourceType.Equipment => CombatTooltipDataBuilder.BuildFromItem(definition),
                CombatTooltipSourceType.Status => CombatTooltipDataBuilder.BuildFromStatusEffect(definition),
                _ => default
            };
            _hasPayload = !string.IsNullOrWhiteSpace(_payload.Title) || !string.IsNullOrWhiteSpace(_payload.Description);
        }

        private void ResolveController()
        {
            if (tooltipController != null)
            {
                return;
            }

            tooltipController = FindFirstObjectByType<CombatTooltipController>(FindObjectsInactive.Include);
        }
    }
}
