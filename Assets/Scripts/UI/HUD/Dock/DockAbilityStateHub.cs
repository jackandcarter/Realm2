using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    public class DockAbilityStateHub : MonoBehaviour, IDockAbilityStateSource
    {
        private readonly Dictionary<string, DockAbilityState> _states =
            new Dictionary<string, DockAbilityState>(StringComparer.OrdinalIgnoreCase);

        public event Action<string, DockAbilityState> AbilityStateChanged;

        public bool TryGetState(string abilityId, out DockAbilityState state)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                state = default;
                return false;
            }

            return _states.TryGetValue(abilityId.Trim(), out state);
        }

        public void SetAbilityState(string abilityId, DockAbilityState state)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return;
            }

            var key = abilityId.Trim();
            _states[key] = state;
            AbilityStateChanged?.Invoke(key, state);
        }

        public void ClearAbilityState(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return;
            }

            var key = abilityId.Trim();
            if (_states.Remove(key))
            {
                AbilityStateChanged?.Invoke(key, default);
            }
        }

        public void ClearAll()
        {
            _states.Clear();
        }
    }
}
