using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.Builder
{
    [CreateAssetMenu(menuName = "Realm/Builder/Ability Set", fileName = "BuilderAbilities")]
    public class BuilderAbilitySet : ScriptableObject
    {
        [SerializeField] private List<BuilderAbilityDefinition> abilities = new List<BuilderAbilityDefinition>();

        private readonly Dictionary<string, BuilderAbilityDefinition> _lookup =
            new Dictionary<string, BuilderAbilityDefinition>(StringComparer.OrdinalIgnoreCase);

        private bool _initialized;

        public IReadOnlyList<BuilderAbilityDefinition> Abilities
        {
            get
            {
                EnsureInitialized();
                return abilities;
            }
        }

        public bool TryGetAbility(string abilityId, out BuilderAbilityDefinition definition)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                definition = null;
                return false;
            }

            return _lookup.TryGetValue(abilityId.Trim(), out definition);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _lookup.Clear();
            if (abilities != null)
            {
                foreach (var ability in abilities)
                {
                    if (ability == null || string.IsNullOrWhiteSpace(ability.AbilityId))
                    {
                        continue;
                    }

                    var id = ability.AbilityId.Trim();
                    if (_lookup.ContainsKey(id))
                    {
                        Debug.LogWarning($"Duplicate builder ability id detected: {id}. Only the first entry will be used.");
                        continue;
                    }

                    _lookup[id] = ability;
                }
            }

            _initialized = true;
        }
    }

    [Serializable]
    public class BuilderAbilityDefinition
    {
        [SerializeField] private string abilityId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private string iconResourcePath;
        [SerializeField] private float cooldownSeconds = 1f;
        [SerializeField] private BuilderAbilityKind abilityKind = BuilderAbilityKind.Custom;
        [SerializeField] private bool requiresBuilderClass = true;

        [NonSerialized] private Sprite _cachedIcon;

        public string AbilityId => abilityId;
        public string DisplayName => displayName;
        public string Description => description;
        public float CooldownSeconds => Mathf.Max(0f, cooldownSeconds);
        public BuilderAbilityKind AbilityKind => abilityKind;
        public bool RequiresBuilderClass => requiresBuilderClass;

        public string IconResourcePath
        {
            get => iconResourcePath;
            set => iconResourcePath = value;
        }

        public Sprite GetIcon()
        {
            if (_cachedIcon != null)
            {
                return _cachedIcon;
            }

            if (string.IsNullOrWhiteSpace(iconResourcePath))
            {
                return null;
            }

            _cachedIcon = Resources.Load<Sprite>(iconResourcePath);
            if (_cachedIcon == null)
            {
                Debug.LogWarning($"Builder ability icon could not be loaded from Resources at path '{iconResourcePath}'.");
            }

            return _cachedIcon;
        }
    }

    public enum BuilderAbilityKind
    {
        Custom = 0,
        SpawnBlueprint,
        FloatSelection,
        PlaceSelection
    }
}
