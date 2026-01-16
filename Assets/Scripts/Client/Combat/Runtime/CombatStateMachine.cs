using System;
using System.Collections.Generic;
using Realm.Combat.Data;
using UnityEngine;

namespace Client.Combat.Runtime
{
    public enum CombatState
    {
        Idle,
        BasicAttack,
        Recovery
    }

    public enum ActionCategory
    {
        BasicAttack,
        SpecialAbility
    }

    public struct ActionRequirements
    {
        public bool RequiresContinueWindow;
        public bool RequiresCancelableIntoAbilityWindow;
    }

    public struct BufferedAction
    {
        public ActionCategory Category;
        public ComboInputType ComboInput;
        public float RecoverySeconds;
        public float ReceivedAt;
        public float ExpiresAt;
    }

    public struct ComboStepContext
    {
        public ComboInputType Input;
        public ComboStepDefinition Step;
        public float RecoverySeconds;
    }

    public enum ActionHandleResult
    {
        Started,
        Buffered,
        Rejected
    }

    [DisallowMultipleComponent]
    public class CombatStateMachine : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AnimationCombatDriver animationDriver;

        [Header("Combo Timing")]
        [SerializeField] private float comboInputTimeoutSeconds = 1.15f;
        [SerializeField] private float bufferedInputTimeoutSeconds = 0.45f;

        [Header("Special Timing")]
        [SerializeField] private float specialInputTimeoutSeconds = 1.25f;

        private readonly ComboSystem _comboSystem = new();
        private readonly List<ComboInputType> _specialInputBuffer = new();

        private WeaponSpecialDefinition _specialDefinition;
        private CombatState _state = CombatState.Idle;
        private BufferedAction? _bufferedAction;
        private ComboStepDefinition _currentStep;
        private bool _continueWindowOpen;
        private bool _cancelIntoAbilityWindowOpen;
        private float _recoveryEndsAt = float.NegativeInfinity;
        private float _lastComboInputTime = float.NegativeInfinity;
        private float _specialReadyExpiresAt = float.NegativeInfinity;
        private float _specialCooldownEndsAt = float.NegativeInfinity;
        private int _hitCount;
        private float _hitWindowEndsAt = float.NegativeInfinity;
        private float _combatMeter;
        private float _timeInCombat;
        private bool _specialReadyActive;

        public event Action<ComboStepContext> ComboStepStarted;
        public event Action<ComboStepDefinition> ComboStepEnded;
        public event Action<bool> SpecialReadyChanged;

        public CombatState CurrentState => _state;
        public bool IsContinueWindowOpen => _continueWindowOpen;
        public bool IsCancelableIntoAbilityOpen => _cancelIntoAbilityWindowOpen;
        public bool IsSpecialReady => _specialReadyActive;
        public float SpecialCooldownSeconds => _specialDefinition?.Action?.CooldownSeconds ?? 0f;
        public float SpecialCooldownRemaining => Mathf.Max(0f, _specialCooldownEndsAt - Time.unscaledTime);
        public string CurrentSpecialAbilityId
        {
            get
            {
                if (_specialDefinition == null)
                {
                    return string.Empty;
                }

                var ability = _specialDefinition.Action?.AbilityReference;
                if (ability != null && !string.IsNullOrWhiteSpace(ability.Guid))
                {
                    return ability.Guid;
                }

                return _specialDefinition.Action?.SpecialId ?? string.Empty;
            }
        }

        private void Awake()
        {
            ResolveAnimationDriver();
            ApplyComboTimeout();
        }

        private void OnEnable()
        {
            ResolveAnimationDriver();
            BindAnimationDriver();
        }

        private void OnDisable()
        {
            UnbindAnimationDriver();
        }

        private void OnValidate()
        {
            comboInputTimeoutSeconds = Mathf.Max(0.05f, comboInputTimeoutSeconds);
            bufferedInputTimeoutSeconds = Mathf.Max(0f, bufferedInputTimeoutSeconds);
            specialInputTimeoutSeconds = Mathf.Max(0f, specialInputTimeoutSeconds);
            ApplyComboTimeout();
        }

        private void Update()
        {
            if (_state == CombatState.Recovery && Time.unscaledTime >= _recoveryEndsAt)
            {
                _state = CombatState.Idle;
                TryConsumeBufferedAction();
            }

            if (_specialReadyActive && Time.unscaledTime >= _specialReadyExpiresAt)
            {
                ResetSpecialReady();
            }
        }

        public void SetCombatDefinition(WeaponCombatDefinition definition)
        {
            _comboSystem.SetDefinition(definition);
        }

        public void SetSpecialDefinition(WeaponSpecialDefinition definition)
        {
            if (_specialDefinition == definition)
            {
                return;
            }

            _specialDefinition = definition;
            ResetSpecialTracking();
        }

        public ActionHandleResult HandleComboInput(ComboInputType input, float recoverySeconds, out ComboStepDefinition step)
        {
            step = null;
            var now = Time.unscaledTime;
            var context = new ComboInputContext
            {
                Input = input,
                HitConfirmed = false,
                Stamina = float.MaxValue
            };

            RegisterSpecialInput(input, now);

            if (_state == CombatState.BasicAttack && !_continueWindowOpen)
            {
                BufferAction(input, recoverySeconds, now);
                return ActionHandleResult.Buffered;
            }

            if (!_comboSystem.TryAdvanceCombo(context, now, out step))
            {
                if (_state == CombatState.BasicAttack || _state == CombatState.Recovery)
                {
                    BufferAction(input, recoverySeconds, now);
                    return ActionHandleResult.Buffered;
                }

                return ActionHandleResult.Rejected;
            }

            StartComboStep(input, step, recoverySeconds, now);
            return ActionHandleResult.Started;
        }

        public void RegisterComboInput(ComboInputType input)
        {
            RegisterSpecialInput(input, Time.unscaledTime);
        }

        public bool TryConsumeSpecialReady()
        {
            if (!IsSpecialReady)
            {
                return false;
            }

            ResetSpecialReady();
            _specialCooldownEndsAt = Time.unscaledTime + SpecialCooldownSeconds;
            return true;
        }

        public void RegisterHitConfirmed()
        {
            if (_specialDefinition == null || _specialDefinition.Rule == null)
            {
                return;
            }

            if (_specialDefinition.Rule.RuleType != SpecialRuleType.HitCount)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (_hitWindowEndsAt < now)
            {
                _hitCount = 0;
                _hitWindowEndsAt = now + Mathf.Max(0f, _specialDefinition.Rule.HitWindowSeconds);
            }

            _hitCount++;
            if (_specialDefinition.Rule.HitCount > 0 && _hitCount >= _specialDefinition.Rule.HitCount)
            {
                SetSpecialReady(now);
            }
        }

        public void UpdateCombatMeter(float meter)
        {
            _combatMeter = Mathf.Max(0f, meter);
            if (_specialDefinition == null || _specialDefinition.Rule == null)
            {
                return;
            }

            if (_specialDefinition.Rule.RuleType != SpecialRuleType.MeterFill)
            {
                return;
            }

            if (_combatMeter >= _specialDefinition.Rule.MeterFill)
            {
                SetSpecialReady(Time.unscaledTime);
            }
        }

        public void UpdateTimeInCombat(float seconds)
        {
            _timeInCombat = Mathf.Max(0f, seconds);
            if (_specialDefinition == null || _specialDefinition.Rule == null)
            {
                return;
            }

            if (_specialDefinition.Rule.RuleType != SpecialRuleType.TimeInCombat)
            {
                return;
            }

            if (_timeInCombat >= _specialDefinition.Rule.TimeInCombatSeconds)
            {
                SetSpecialReady(Time.unscaledTime);
            }
        }

        public void NotifyContinueWindowOpened()
        {
            _continueWindowOpen = true;
            TryConsumeBufferedAction();
        }

        public void NotifyContinueWindowClosed()
        {
            _continueWindowOpen = false;
        }

        public void NotifyCancelableIntoAbilityOpened()
        {
            _cancelIntoAbilityWindowOpen = true;
        }

        public void NotifyCancelableIntoAbilityClosed()
        {
            _cancelIntoAbilityWindowOpen = false;
        }

        public void NotifyActionAnimationComplete()
        {
            EnterRecovery();
        }

        private void StartComboStep(ComboInputType input, ComboStepDefinition step, float recoverySeconds, float now)
        {
            _state = CombatState.BasicAttack;
            _currentStep = step;
            _continueWindowOpen = false;
            _cancelIntoAbilityWindowOpen = false;
            _recoveryEndsAt = now + Mathf.Max(0f, recoverySeconds);
            _bufferedAction = null;
            ComboStepStarted?.Invoke(new ComboStepContext
            {
                Input = input,
                Step = step,
                RecoverySeconds = recoverySeconds
            });
            RegisterFinisherRule(step, now);
        }

        private void EnterRecovery()
        {
            if (_state != CombatState.BasicAttack)
            {
                return;
            }

            _state = CombatState.Recovery;
            ComboStepEnded?.Invoke(_currentStep);
            _currentStep = null;
        }

        private void BufferAction(ComboInputType input, float recoverySeconds, float now)
        {
            if (bufferedInputTimeoutSeconds <= 0f)
            {
                return;
            }

            _bufferedAction = new BufferedAction
            {
                Category = ActionCategory.BasicAttack,
                ComboInput = input,
                RecoverySeconds = recoverySeconds,
                ReceivedAt = now,
                ExpiresAt = now + bufferedInputTimeoutSeconds
            };
        }

        private void TryConsumeBufferedAction()
        {
            if (_bufferedAction == null)
            {
                return;
            }

            var buffered = _bufferedAction.Value;
            if (Time.unscaledTime > buffered.ExpiresAt)
            {
                _bufferedAction = null;
                return;
            }

            if (_state == CombatState.BasicAttack && !_continueWindowOpen)
            {
                return;
            }

            if (_comboSystem.TryAdvanceCombo(buffered.ComboInput, Time.unscaledTime, out var step))
            {
                StartComboStep(buffered.ComboInput, step, buffered.RecoverySeconds, Time.unscaledTime);
            }

            _bufferedAction = null;
        }

        private void RegisterSpecialInput(ComboInputType input, float now)
        {
            if (_specialDefinition == null || _specialDefinition.Rule == null)
            {
                return;
            }

            if (Time.unscaledTime < _specialCooldownEndsAt)
            {
                return;
            }

            if (now - _lastComboInputTime > specialInputTimeoutSeconds)
            {
                _specialInputBuffer.Clear();
            }

            _lastComboInputTime = now;
            _specialInputBuffer.Add(input);

            var rule = _specialDefinition.Rule;
            if (rule.RuleType == SpecialRuleType.SequenceMatch)
            {
                if (rule.SequenceMatch != null && rule.SequenceMatch.Count > 0 &&
                    _specialInputBuffer.Count > rule.SequenceMatch.Count)
                {
                    _specialInputBuffer.RemoveRange(
                        0,
                        _specialInputBuffer.Count - rule.SequenceMatch.Count);
                }

                if (MatchesSequence(rule.SequenceMatch, _specialInputBuffer))
                {
                    SetSpecialReady(now);
                }
            }
        }

        private void RegisterFinisherRule(ComboStepDefinition step, float now)
        {
            if (_specialDefinition == null || _specialDefinition.Rule == null)
            {
                return;
            }

            var rule = _specialDefinition.Rule;
            if (rule.RuleType != SpecialRuleType.FinisherReached)
            {
                return;
            }

            if (step == null || step.Tags == null)
            {
                return;
            }

            if (step.Tags.Contains(rule.FinisherTag))
            {
                SetSpecialReady(now);
            }
        }

        private void SetSpecialReady(float now)
        {
            if (_specialDefinition == null)
            {
                return;
            }

            if (Time.unscaledTime < _specialCooldownEndsAt)
            {
                return;
            }

            var expiresAfter = _specialDefinition.Rule != null
                ? Mathf.Max(0f, _specialDefinition.Rule.ExpiresAfterSeconds)
                : 0f;
            _specialReadyExpiresAt = expiresAfter > 0f ? now + expiresAfter : float.PositiveInfinity;

            if (_specialReadyActive)
            {
                return;
            }

            _specialReadyActive = true;
            SpecialReadyChanged?.Invoke(true);
        }

        private void ResetSpecialReady()
        {
            if (!_specialReadyActive)
            {
                _specialReadyExpiresAt = float.NegativeInfinity;
                return;
            }

            _specialReadyActive = false;
            _specialReadyExpiresAt = float.NegativeInfinity;
            SpecialReadyChanged?.Invoke(false);
        }

        private void ResetSpecialTracking()
        {
            ResetSpecialReady();
            _specialInputBuffer.Clear();
            _lastComboInputTime = float.NegativeInfinity;
            _specialCooldownEndsAt = float.NegativeInfinity;
            _hitCount = 0;
            _hitWindowEndsAt = float.NegativeInfinity;
            _combatMeter = 0f;
            _timeInCombat = 0f;
            _specialReadyActive = false;
        }

        private static bool MatchesSequence(IReadOnlyList<ComboInputType> sequence, IReadOnlyList<ComboInputType> buffer)
        {
            if (sequence == null || sequence.Count == 0 || buffer.Count < sequence.Count)
            {
                return false;
            }

            var startIndex = buffer.Count - sequence.Count;
            for (var i = 0; i < sequence.Count; i++)
            {
                if (sequence[i] != buffer[startIndex + i])
                {
                    return false;
                }
            }

            return true;
        }

        private void ResolveAnimationDriver()
        {
            if (animationDriver != null)
            {
                return;
            }

#if UNITY_2023_1_OR_NEWER
            animationDriver = FindFirstObjectByType<AnimationCombatDriver>(FindObjectsInactive.Include);
#else
            animationDriver = FindObjectOfType<AnimationCombatDriver>(true);
#endif
        }

        private void BindAnimationDriver()
        {
            if (animationDriver == null)
            {
                return;
            }

            animationDriver.ContinueWindowOpened -= NotifyContinueWindowOpened;
            animationDriver.ContinueWindowOpened += NotifyContinueWindowOpened;
            animationDriver.ContinueWindowClosed -= NotifyContinueWindowClosed;
            animationDriver.ContinueWindowClosed += NotifyContinueWindowClosed;
            animationDriver.CancelableIntoAbilityOpened -= NotifyCancelableIntoAbilityOpened;
            animationDriver.CancelableIntoAbilityOpened += NotifyCancelableIntoAbilityOpened;
            animationDriver.CancelableIntoAbilityClosed -= NotifyCancelableIntoAbilityClosed;
            animationDriver.CancelableIntoAbilityClosed += NotifyCancelableIntoAbilityClosed;
            animationDriver.ActionAnimationComplete -= NotifyActionAnimationComplete;
            animationDriver.ActionAnimationComplete += NotifyActionAnimationComplete;
            animationDriver.HitEvent -= HandleHitEvent;
            animationDriver.HitEvent += HandleHitEvent;
        }

        private void UnbindAnimationDriver()
        {
            if (animationDriver == null)
            {
                return;
            }

            animationDriver.ContinueWindowOpened -= NotifyContinueWindowOpened;
            animationDriver.ContinueWindowClosed -= NotifyContinueWindowClosed;
            animationDriver.CancelableIntoAbilityOpened -= NotifyCancelableIntoAbilityOpened;
            animationDriver.CancelableIntoAbilityClosed -= NotifyCancelableIntoAbilityClosed;
            animationDriver.ActionAnimationComplete -= NotifyActionAnimationComplete;
            animationDriver.HitEvent -= HandleHitEvent;
        }

        private void HandleHitEvent(ComboStepDefinition step, HitShapeDefinition hitShape)
        {
            RegisterHitConfirmed();
        }

        private void ApplyComboTimeout()
        {
            _comboSystem.InputTimeoutSeconds = comboInputTimeoutSeconds;
        }
    }
}
