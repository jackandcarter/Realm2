using System;
using Client.UI.HUD.Dock;
using Realm.Abilities;
using UnityEngine;

namespace Client.Combat
{
    [DisallowMultipleComponent]
    public class AbilityActivationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private WeaponAttackController attackController;
        [SerializeField] private DockAbilityStateHub abilityStateHub;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        public bool TryActivate(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            var normalized = abilityId.Trim();
            if (!TryResolveAbility(normalized, out var definition))
            {
                Debug.LogWarning($"AbilityActivationController could not resolve ability '{normalized}'.", this);
                return false;
            }

            if (!RequestExecution(normalized))
            {
                return false;
            }

            return true;
        }

        private void ResolveReferences()
        {
            if (combatManager == null)
            {
#if UNITY_2023_1_OR_NEWER
                combatManager = UnityEngine.Object.FindFirstObjectByType<CombatManager>(FindObjectsInactive.Include);
#else
                combatManager = FindObjectOfType<CombatManager>(true);
#endif
            }

            if (attackController == null)
            {
#if UNITY_2023_1_OR_NEWER
                attackController = UnityEngine.Object.FindFirstObjectByType<WeaponAttackController>(FindObjectsInactive.Include);
#else
                attackController = FindObjectOfType<WeaponAttackController>(true);
#endif
            }

            if (abilityStateHub == null)
            {
#if UNITY_2023_1_OR_NEWER
                abilityStateHub = UnityEngine.Object.FindFirstObjectByType<DockAbilityStateHub>(FindObjectsInactive.Include);
#else
                abilityStateHub = FindObjectOfType<DockAbilityStateHub>(true);
#endif
            }
        }

        private bool TryResolveAbility(string abilityId, out AbilityDefinition definition)
        {
            if (combatManager != null && combatManager.TryGetAbilityDefinition(abilityId, out definition))
            {
                return true;
            }

            return AbilityRegistry.TryGetAbility(abilityId, out definition);
        }

        private bool RequestExecution(string abilityId)
        {
            if (attackController == null)
            {
                Debug.LogWarning($"AbilityActivationController has no WeaponAttackController for '{abilityId}'.", this);
                return false;
            }

            attackController.HandleSpecial(abilityId);
            return true;
        }

    }
}
