using System;
using System.Collections.Generic;
using Realm.Combat.Data;
using UnityEngine;

namespace Client.Combat.Runtime
{
    [DisallowMultipleComponent]
    public class AnimationCombatDriver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Fallback Timing")]
        [SerializeField] private float fallbackDurationSeconds = 1f;

        private ComboStepDefinition _activeStep;
        private readonly List<HitShapeDefinition> _pendingHits = new();
        private float _lastNormalizedTime;
        private float _elapsedFallbackTime;
        private bool _continueWindowOpen;
        private bool _cancelAbilityWindowOpen;

        public event Action<ComboStepDefinition, HitShapeDefinition> HitEvent;
        public event Action ContinueWindowOpened;
        public event Action ContinueWindowClosed;
        public event Action CancelableIntoAbilityOpened;
        public event Action CancelableIntoAbilityClosed;
        public event Action ActionAnimationComplete;

        private void Awake()
        {
            ResolveAnimator();
        }

        private void Update()
        {
            if (_activeStep == null)
            {
                return;
            }

            var normalizedTime = SampleNormalizedTime();
            if (normalizedTime < _lastNormalizedTime)
            {
                _lastNormalizedTime = 0f;
            }

            ProcessHitEvents(normalizedTime);
            ProcessContinueWindow(normalizedTime);
            ProcessCancelWindow(normalizedTime);

            if (normalizedTime >= 1f)
            {
                CompleteAnimation();
            }

            _lastNormalizedTime = normalizedTime;
        }

        public void BeginComboStep(ComboStepDefinition step)
        {
            _activeStep = step;
            _lastNormalizedTime = 0f;
            _elapsedFallbackTime = 0f;
            _continueWindowOpen = false;
            _cancelAbilityWindowOpen = false;
            CacheHitEvents(step);

            if (animator != null && step != null && !string.IsNullOrWhiteSpace(step.AnimationKey))
            {
                animator.Play(step.AnimationKey, 0, 0f);
            }
        }

        public void ClearStep()
        {
            _activeStep = null;
            _pendingHits.Clear();
            _continueWindowOpen = false;
            _cancelAbilityWindowOpen = false;
        }

        private void CacheHitEvents(ComboStepDefinition step)
        {
            _pendingHits.Clear();
            if (step == null || step.HitShapes == null)
            {
                return;
            }

            foreach (var hit in step.HitShapes)
            {
                if (hit == null)
                {
                    continue;
                }

                _pendingHits.Add(hit);
            }

            _pendingHits.Sort((a, b) => a.HitMomentNormalized.CompareTo(b.HitMomentNormalized));
        }

        private void ProcessHitEvents(float normalizedTime)
        {
            if (_pendingHits.Count == 0)
            {
                return;
            }

            for (var i = _pendingHits.Count - 1; i >= 0; i--)
            {
                var hit = _pendingHits[i];
                if (hit == null)
                {
                    _pendingHits.RemoveAt(i);
                    continue;
                }

                if (normalizedTime >= hit.HitMomentNormalized)
                {
                    HitEvent?.Invoke(_activeStep, hit);
                    _pendingHits.RemoveAt(i);
                }
            }
        }

        private void ProcessContinueWindow(float normalizedTime)
        {
            if (_activeStep == null)
            {
                return;
            }

            var start = _activeStep.ContinueWindowStartNormalized;
            var end = _activeStep.ContinueWindowEndNormalized;
            var inWindow = normalizedTime >= start && normalizedTime <= end && end > start;

            if (inWindow && !_continueWindowOpen)
            {
                _continueWindowOpen = true;
                ContinueWindowOpened?.Invoke();
            }
            else if (!inWindow && _continueWindowOpen)
            {
                _continueWindowOpen = false;
                ContinueWindowClosed?.Invoke();
            }
        }

        private void ProcessCancelWindow(float normalizedTime)
        {
            if (_activeStep == null)
            {
                return;
            }

            var start = _activeStep.CancelIntoAbilityStartNormalized;
            var end = _activeStep.CancelIntoAbilityEndNormalized;
            var inWindow = normalizedTime >= start && normalizedTime <= end && end > start;

            if (inWindow && !_cancelAbilityWindowOpen)
            {
                _cancelAbilityWindowOpen = true;
                CancelableIntoAbilityOpened?.Invoke();
            }
            else if (!inWindow && _cancelAbilityWindowOpen)
            {
                _cancelAbilityWindowOpen = false;
                CancelableIntoAbilityClosed?.Invoke();
            }
        }

        private float SampleNormalizedTime()
        {
            if (animator == null || !animator.isActiveAndEnabled)
            {
                _elapsedFallbackTime += Time.deltaTime;
                var duration = Mathf.Max(0.01f, fallbackDurationSeconds);
                return Mathf.Clamp01(_elapsedFallbackTime / duration);
            }

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.length <= 0f)
            {
                _elapsedFallbackTime += Time.deltaTime;
                var duration = Mathf.Max(0.01f, fallbackDurationSeconds);
                return Mathf.Clamp01(_elapsedFallbackTime / duration);
            }

            var normalized = stateInfo.normalizedTime;
            return Mathf.Clamp01(normalized % 1f);
        }

        private void CompleteAnimation()
        {
            if (_activeStep == null)
            {
                return;
            }

            if (_continueWindowOpen)
            {
                _continueWindowOpen = false;
                ContinueWindowClosed?.Invoke();
            }

            if (_cancelAbilityWindowOpen)
            {
                _cancelAbilityWindowOpen = false;
                CancelableIntoAbilityClosed?.Invoke();
            }

            ActionAnimationComplete?.Invoke();
            ClearStep();
        }

        private void ResolveAnimator()
        {
            if (animator != null)
            {
                return;
            }

            animator = GetComponent<Animator>();
        }
    }
}
