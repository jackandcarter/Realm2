using Client.Combat;
using Client.Combat.Runtime;
using Client.Player;
using Realm.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    public class WeaponDockController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button lightButton;
        [SerializeField] private Button mediumButton;
        [SerializeField] private Button heavyButton;
        [SerializeField] private Button specialButton;
        [SerializeField] private Image specialReadyIndicator;
        [SerializeField] private Image specialCooldownIndicator;
        [SerializeField] private Outline specialReadyOutline;
        [SerializeField] private float readyPulseScale = 1.06f;
        [SerializeField] private float readyPulseSpeed = 2.4f;
        [SerializeField] private float readyPulseMinAlpha = 0.25f;

        [Header("Runtime")]
        [SerializeField] private WeaponComboTracker comboTracker;
        [SerializeField] private WeaponAttackController attackController;
        [SerializeField] private CombatStateMachine combatStateMachine;

        private string _equippedWeaponId;
        private Coroutine _readyPulseRoutine;
        private Vector3 _readyIndicatorBaseScale;
        private Color _readyIndicatorBaseColor;
        private Color _readyOutlineBaseColor;
        private bool _specialAlwaysReady;

        private void Reset()
        {
            CacheButtons();
        }

        private void Awake()
        {
            CacheButtons();
            CacheReadyVisuals();
            ResolveComboTracker();
            ResolveAttackController();
            ResolveCombatStateMachine();
            SetSpecialIndicator(false);
            UpdateSpecialCooldownIndicator();
        }

        private void OnEnable()
        {
            BindButtons();
            PlayerEquipmentStateManager.EquipmentChanged += OnEquipmentChanged;
            PlayerClassStateManager.ActiveClassChanged += OnActiveClassChanged;

            ResolveComboTracker();
            ResolveCombatStateMachine();
            if (comboTracker != null && combatStateMachine == null)
            {
                comboTracker.SpecialReadyChanged += OnSpecialReadyChanged;
            }

            if (combatStateMachine != null)
            {
                combatStateMachine.SpecialReadyChanged += OnSpecialReadyChanged;
            }

            RefreshEquippedWeapon();
        }

        private void OnDisable()
        {
            UnbindButtons();
            PlayerEquipmentStateManager.EquipmentChanged -= OnEquipmentChanged;
            PlayerClassStateManager.ActiveClassChanged -= OnActiveClassChanged;

            if (comboTracker != null)
            {
                comboTracker.SpecialReadyChanged -= OnSpecialReadyChanged;
            }

            if (combatStateMachine != null)
            {
                combatStateMachine.SpecialReadyChanged -= OnSpecialReadyChanged;
            }

            StopReadyPulse();
        }

        private void CacheButtons()
        {
            if (lightButton == null || mediumButton == null || heavyButton == null || specialButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                foreach (var button in buttons)
                {
                    if (button == null)
                    {
                        continue;
                    }

                    switch (button.name)
                    {
                        case "LightWeaponButton":
                            lightButton = button;
                            break;
                        case "MediumWeaponButton":
                            mediumButton = button;
                            break;
                        case "HeavyWeaponButton":
                            heavyButton = button;
                            break;
                        case "SpecialWeaponButton":
                            specialButton = button;
                            break;
                    }
                }
            }

            if (specialReadyOutline == null && specialButton != null)
            {
                specialReadyOutline = specialButton.GetComponent<Outline>();
                if (specialReadyOutline == null)
                {
                    specialReadyOutline = specialButton.gameObject.AddComponent<Outline>();
                    specialReadyOutline.effectColor = new Color(0.95f, 0.78f, 0.2f, 0.85f);
                    specialReadyOutline.effectDistance = new Vector2(3f, -3f);
                    specialReadyOutline.enabled = false;
                }
            }
        }

        private void CacheReadyVisuals()
        {
            if (specialReadyIndicator != null)
            {
                _readyIndicatorBaseScale = specialReadyIndicator.rectTransform.localScale;
                _readyIndicatorBaseColor = specialReadyIndicator.color;
            }

            if (specialReadyOutline != null)
            {
                _readyOutlineBaseColor = specialReadyOutline.effectColor;
            }
        }

        private void ResolveComboTracker()
        {
            if (comboTracker != null)
            {
                return;
            }

#if UNITY_2023_1_OR_NEWER
            comboTracker = Object.FindFirstObjectByType<WeaponComboTracker>(FindObjectsInactive.Include);
#else
            comboTracker = FindObjectOfType<WeaponComboTracker>(true);
#endif
        }

        private void ResolveAttackController()
        {
            if (attackController != null)
            {
                return;
            }

#if UNITY_2023_1_OR_NEWER
            attackController = Object.FindFirstObjectByType<WeaponAttackController>(FindObjectsInactive.Include);
#else
            attackController = FindObjectOfType<WeaponAttackController>(true);
#endif
        }

        private void ResolveCombatStateMachine()
        {
            if (combatStateMachine != null)
            {
                return;
            }

#if UNITY_2023_1_OR_NEWER
            combatStateMachine = Object.FindFirstObjectByType<CombatStateMachine>(FindObjectsInactive.Include);
#else
            combatStateMachine = FindObjectOfType<CombatStateMachine>(true);
#endif
        }

        private void BindButtons()
        {
            if (lightButton != null)
            {
                lightButton.onClick.RemoveListener(HandleLightClicked);
                lightButton.onClick.AddListener(HandleLightClicked);
            }

            if (mediumButton != null)
            {
                mediumButton.onClick.RemoveListener(HandleMediumClicked);
                mediumButton.onClick.AddListener(HandleMediumClicked);
            }

            if (heavyButton != null)
            {
                heavyButton.onClick.RemoveListener(HandleHeavyClicked);
                heavyButton.onClick.AddListener(HandleHeavyClicked);
            }

            if (specialButton != null)
            {
                specialButton.onClick.RemoveListener(HandleSpecialClicked);
                specialButton.onClick.AddListener(HandleSpecialClicked);
            }
        }

        private void UnbindButtons()
        {
            if (lightButton != null)
            {
                lightButton.onClick.RemoveListener(HandleLightClicked);
            }

            if (mediumButton != null)
            {
                mediumButton.onClick.RemoveListener(HandleMediumClicked);
            }

            if (heavyButton != null)
            {
                heavyButton.onClick.RemoveListener(HandleHeavyClicked);
            }

            if (specialButton != null)
            {
                specialButton.onClick.RemoveListener(HandleSpecialClicked);
            }
        }

        private void HandleLightClicked()
        {
            RegisterAttack(WeaponComboInputType.Light);
            attackController?.HandleAttack(WeaponComboInputType.Light);
        }

        private void HandleMediumClicked()
        {
            RegisterAttack(WeaponComboInputType.Medium);
            attackController?.HandleAttack(WeaponComboInputType.Medium);
        }

        private void HandleHeavyClicked()
        {
            RegisterAttack(WeaponComboInputType.Heavy);
            attackController?.HandleAttack(WeaponComboInputType.Heavy);
        }

        private void HandleSpecialClicked()
        {
            if (_specialAlwaysReady)
            {
                attackController?.HandleSpecial();
                return;
            }

            if (combatStateMachine != null)
            {
                if (combatStateMachine.TryConsumeSpecialReady())
                {
                    attackController?.HandleSpecial();
                }

                return;
            }

            if (comboTracker != null && comboTracker.TryConsumeSpecialReady())
            {
                attackController?.HandleSpecial();
            }
        }

        private void RegisterAttack(WeaponComboInputType inputType)
        {
            if (comboTracker == null || combatStateMachine != null || string.IsNullOrWhiteSpace(_equippedWeaponId))
            {
                return;
            }

            comboTracker.RegisterAttackInput(inputType);
        }

        private void OnEquipmentChanged(EquipmentSlot slot, EquipmentDefinition definition)
        {
            if (slot != EquipmentSlot.Weapon)
            {
                return;
            }

            RefreshEquippedWeapon(definition as WeaponDefinition);
        }

        private void OnActiveClassChanged(string _)
        {
            RefreshEquippedWeapon();
        }

        private void RefreshEquippedWeapon()
        {
            var equipped = PlayerEquipmentStateManager.GetEquippedItem(EquipmentSlot.Weapon) as WeaponDefinition;
            RefreshEquippedWeapon(equipped);
        }

        private void RefreshEquippedWeapon(WeaponDefinition weapon)
        {
            _equippedWeaponId = weapon != null ? weapon.Guid : string.Empty;
            _specialAlwaysReady = weapon != null && weapon.SpecialDefinition == null && weapon.SpecialAttack != null;

            if (combatStateMachine != null)
            {
                combatStateMachine.SetCombatDefinition(weapon != null ? weapon.CombatDefinition : null);
                combatStateMachine.SetSpecialDefinition(weapon != null ? weapon.SpecialDefinition : null);
                SetSpecialIndicator(_specialAlwaysReady || combatStateMachine.IsSpecialReady);
            }
            else if (comboTracker != null)
            {
                comboTracker.SetEquippedWeaponId(_equippedWeaponId);
                SetSpecialIndicator(_specialAlwaysReady || comboTracker.IsSpecialReady);
            }
            else
            {
                SetSpecialIndicator(_specialAlwaysReady);
            }

            var canAttack = !string.IsNullOrWhiteSpace(_equippedWeaponId);
            SetButtonsInteractable(canAttack);
            UpdateSpecialButtonState();
            UpdateSpecialCooldownIndicator();
        }

        private void OnSpecialReadyChanged(bool isReady)
        {
            SetSpecialIndicator(isReady);
            UpdateSpecialButtonState();
            UpdateSpecialCooldownIndicator();
        }

        private void SetSpecialIndicator(bool active)
        {
            if (specialReadyIndicator == null)
            {
                StopReadyPulse();
                return;
            }

            specialReadyIndicator.enabled = active;

            if (specialReadyOutline != null)
            {
                specialReadyOutline.enabled = active;
            }

            if (active)
            {
                CacheReadyVisuals();
                StartReadyPulse();
            }
            else
            {
                StopReadyPulse();
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (lightButton != null)
            {
                lightButton.interactable = interactable;
            }

            if (mediumButton != null)
            {
                mediumButton.interactable = interactable;
            }

            if (heavyButton != null)
            {
                heavyButton.interactable = interactable;
            }
        }

        private void UpdateSpecialButtonState()
        {
            if (specialButton == null)
            {
                return;
            }

            var ready = _specialAlwaysReady || (combatStateMachine != null
                ? combatStateMachine.IsSpecialReady
                : comboTracker != null && comboTracker.IsSpecialReady);
            specialButton.interactable = !string.IsNullOrWhiteSpace(_equippedWeaponId) && ready;
        }

        private void Update()
        {
            UpdateSpecialCooldownIndicator();
        }

        private void UpdateSpecialCooldownIndicator()
        {
            if (specialCooldownIndicator == null)
            {
                return;
            }

            if (comboTracker == null && combatStateMachine == null)
            {
                specialCooldownIndicator.enabled = false;
                return;
            }

            if (_specialAlwaysReady)
            {
                specialCooldownIndicator.enabled = false;
                return;
            }

            var remaining = combatStateMachine != null
                ? combatStateMachine.SpecialCooldownRemaining
                : comboTracker.SpecialCooldownRemaining;
            var duration = combatStateMachine != null
                ? combatStateMachine.SpecialCooldownSeconds
                : comboTracker.SpecialCooldownSeconds;
            var show = remaining > 0f && duration > 0f;

            specialCooldownIndicator.enabled = show;

            if (show)
            {
                specialCooldownIndicator.fillAmount = Mathf.Clamp01(remaining / duration);
            }
        }

        private void StartReadyPulse()
        {
            if (_readyPulseRoutine != null)
            {
                return;
            }

            _readyPulseRoutine = StartCoroutine(PulseReadyIndicator());
        }

        private void StopReadyPulse()
        {
            if (_readyPulseRoutine != null)
            {
                StopCoroutine(_readyPulseRoutine);
                _readyPulseRoutine = null;
            }

            if (specialReadyIndicator != null)
            {
                specialReadyIndicator.rectTransform.localScale = _readyIndicatorBaseScale == Vector3.zero
                    ? Vector3.one
                    : _readyIndicatorBaseScale;
                specialReadyIndicator.color = _readyIndicatorBaseColor;
            }

            if (specialReadyOutline != null)
            {
                specialReadyOutline.effectColor = _readyOutlineBaseColor;
            }
        }

        private System.Collections.IEnumerator PulseReadyIndicator()
        {
            while (true)
            {
                var pulse = (Mathf.Sin(Time.unscaledTime * readyPulseSpeed) + 1f) * 0.5f;

                if (specialReadyIndicator != null)
                {
                    var baseScale = _readyIndicatorBaseScale == Vector3.zero ? Vector3.one : _readyIndicatorBaseScale;
                    specialReadyIndicator.rectTransform.localScale = baseScale * Mathf.Lerp(1f, readyPulseScale, pulse);

                    var color = _readyIndicatorBaseColor;
                    color.a = Mathf.Lerp(readyPulseMinAlpha, _readyIndicatorBaseColor.a, pulse);
                    specialReadyIndicator.color = color;
                }

                if (specialReadyOutline != null)
                {
                    var outlineColor = _readyOutlineBaseColor;
                    outlineColor.a = Mathf.Lerp(readyPulseMinAlpha, _readyOutlineBaseColor.a, pulse);
                    specialReadyOutline.effectColor = outlineColor;
                }

                yield return null;
            }
        }
    }
}
