using System;
using UnityEngine;

namespace Realm.Data
{
    [CreateAssetMenu(menuName = "Realm/Abilities/Ability Definition", fileName = "AbilityDefinition")]
    public class AbilityDefinition : ScriptableObject, IGuidIdentified
    {
        [SerializeField, Tooltip("Stable unique identifier for this ability. Auto-generated if empty.")]
        private string guid;

        [SerializeField]
        private string displayName;

        [SerializeField, TextArea(2, 5)]
        private string description;

        [SerializeField]
        private Sprite icon;

        [SerializeField, Tooltip("How long the ability takes to recharge after being used.")]
        private float cooldownSeconds = 1f;

        [SerializeField, Tooltip("Resource cost to activate the ability.")]
        private int resourceCost;

        public string Guid => guid;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public float CooldownSeconds => cooldownSeconds;
        public int ResourceCost => resourceCost;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                guid = Guid.NewGuid().ToString("N");
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }

            displayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();
            description = description?.Trim();
            cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            resourceCost = Mathf.Max(0, resourceCost);
        }
    }
}
