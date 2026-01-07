using Client.Combat;
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

        [Header("Runtime")]
        [SerializeField] private WeaponComboTracker comboTracker;
        [SerializeField] private WeaponAttackController attackController;

        private string _equippedWeaponId;

        private void Reset()
        {
            CacheButtons();
        }

        private void Awake()
        {
            CacheButtons();
            ResolveComboTracker();
            ResolveAttackController();
            SetSpecialIndicator(false);
        }

        private void OnEnable()
        {
            BindButtons();
            PlayerEquipmentStateManager.EquipmentChanged += OnEquipmentChanged;
            PlayerClassStateManager.ActiveClassChanged += OnActiveClassChanged;

            ResolveComboTracker();
            if (comboTracker != null)
            {
                comboTracker.SpecialReadyChanged += OnSpecialReadyChanged;
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
            if (comboTracker == null)
            {
                return;
            }

            if (comboTracker.TryConsumeSpecialReady())
            {
                attackController?.HandleSpecial();
            }
        }

        private void RegisterAttack(WeaponComboInputType inputType)
        {
            if (comboTracker == null || string.IsNullOrWhiteSpace(_equippedWeaponId))
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

            if (comboTracker != null)
            {
                comboTracker.SetEquippedWeaponId(_equippedWeaponId);
                SetSpecialIndicator(comboTracker.IsSpecialReady);
            }
            else
            {
                SetSpecialIndicator(false);
            }

            var canAttack = !string.IsNullOrWhiteSpace(_equippedWeaponId);
            SetButtonsInteractable(canAttack);
            UpdateSpecialButtonState();
        }

        private void OnSpecialReadyChanged(bool isReady)
        {
            SetSpecialIndicator(isReady);
            UpdateSpecialButtonState();
        }

        private void SetSpecialIndicator(bool active)
        {
            if (specialReadyIndicator == null)
            {
                return;
            }

            specialReadyIndicator.enabled = active;
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

            var ready = comboTracker != null && comboTracker.IsSpecialReady;
            specialButton.interactable = !string.IsNullOrWhiteSpace(_equippedWeaponId) && ready;
        }
    }
}
