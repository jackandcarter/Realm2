using System.Collections;
using UnityEngine;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class DockItemAnimator : MonoBehaviour
    {
        [Header("Hover")]
        [SerializeField] private float hoverSmoothSpeed = 12f;

        [Header("Appear/Disappear")]
        [SerializeField] private float appearScaleFrom = 0.7f;
        [SerializeField] private float appearDuration = 0.18f;
        [SerializeField] private float disappearDuration = 0.12f;

        [Header("Ability Timing")]
        [SerializeField] private float abilityLift = 18f;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Vector2 _basePosition;
        private Vector3 _baseScale;

        private float _hoverScaleTarget;
        private float _hoverLiftTarget;
        private float _hoverScaleCurrent;
        private float _hoverLiftCurrent;

        private float _appearWeight = 1f;
        private bool _isExiting;
        private Coroutine _exitRoutine;

        private float _castDuration;
        private float _castEndTime;
        private float _cooldownDuration;
        private float _cooldownEndTime;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            _basePosition = _rectTransform.anchoredPosition;
            _baseScale = transform.localScale;
            _appearWeight = Mathf.Clamp01(appearScaleFrom);
            _isExiting = false;
        }

        private void Update()
        {
            UpdateHover();
            UpdateAbilityLift();
            UpdateAppear();
        }

        public void SetHoverInfluence(float scaleAmount, float liftAmount)
        {
            _hoverScaleTarget = Mathf.Max(0f, scaleAmount);
            _hoverLiftTarget = Mathf.Max(0f, liftAmount);
        }

        public void ClearHoverInfluence()
        {
            _hoverScaleTarget = 0f;
            _hoverLiftTarget = 0f;
        }

        public void SetAbilityTiming(float castDuration, float castRemaining, float cooldownDuration, float cooldownRemaining)
        {
            _castDuration = Mathf.Max(0f, castDuration);
            _cooldownDuration = Mathf.Max(0f, cooldownDuration);

            var now = Time.unscaledTime;
            _castEndTime = now + Mathf.Max(0f, castRemaining);
            _cooldownEndTime = now + Mathf.Max(0f, cooldownRemaining);
        }

        public void ClearAbilityTiming()
        {
            _castDuration = 0f;
            _castEndTime = 0f;
            _cooldownDuration = 0f;
            _cooldownEndTime = 0f;
        }

        public IEnumerator PlayExit()
        {
            if (_isExiting)
            {
                yield break;
            }

            _isExiting = true;
            if (_exitRoutine != null)
            {
                StopCoroutine(_exitRoutine);
            }

            var duration = Mathf.Max(0.01f, disappearDuration);
            var startWeight = _appearWeight;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _appearWeight = Mathf.Lerp(startWeight, 0f, elapsed / duration);
                yield return null;
            }

            _appearWeight = 0f;
        }

        private void UpdateHover()
        {
            _hoverScaleCurrent = Mathf.Lerp(
                _hoverScaleCurrent,
                _hoverScaleTarget,
                1f - Mathf.Exp(-hoverSmoothSpeed * Time.unscaledDeltaTime));

            _hoverLiftCurrent = Mathf.Lerp(
                _hoverLiftCurrent,
                _hoverLiftTarget,
                1f - Mathf.Exp(-hoverSmoothSpeed * Time.unscaledDeltaTime));
        }

        private void UpdateAbilityLift()
        {
            var now = Time.unscaledTime;
            var lift = 0f;

            if (_castDuration > 0f && now < _castEndTime)
            {
                var remaining = Mathf.Max(0f, _castEndTime - now);
                lift = abilityLift * Mathf.Clamp01(1f - (remaining / _castDuration));
            }
            else if (_cooldownDuration > 0f && now < _cooldownEndTime)
            {
                var remaining = Mathf.Max(0f, _cooldownEndTime - now);
                lift = abilityLift * Mathf.Clamp01(remaining / _cooldownDuration);
            }

            var targetPosition = _basePosition + new Vector2(0f, _hoverLiftCurrent + lift);
            _rectTransform.anchoredPosition = Vector2.Lerp(
                _rectTransform.anchoredPosition,
                targetPosition,
                1f - Mathf.Exp(-hoverSmoothSpeed * Time.unscaledDeltaTime));
        }

        private void UpdateAppear()
        {
            if (!_isExiting)
            {
                var duration = Mathf.Max(0.01f, appearDuration);
                _appearWeight = Mathf.MoveTowards(_appearWeight, 1f, Time.unscaledDeltaTime / duration);
            }

            var scaleFactor = Mathf.Max(0.0001f, _appearWeight) * (1f + _hoverScaleCurrent);
            transform.localScale = _baseScale * scaleFactor;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = _appearWeight;
            }
        }
    }
}
