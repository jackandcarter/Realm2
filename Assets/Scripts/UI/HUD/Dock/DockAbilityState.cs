using System;
using UnityEngine;

namespace Client.UI.HUD.Dock
{
    public readonly struct DockAbilityState
    {
        public readonly float CastDuration;
        public readonly float CastRemaining;
        public readonly float CooldownDuration;
        public readonly float CooldownRemaining;

        public DockAbilityState(float castDuration, float castRemaining, float cooldownDuration, float cooldownRemaining)
        {
            CastDuration = Mathf.Max(0f, castDuration);
            CastRemaining = Mathf.Max(0f, castRemaining);
            CooldownDuration = Mathf.Max(0f, cooldownDuration);
            CooldownRemaining = Mathf.Max(0f, cooldownRemaining);
        }

        public float CastProgress => CastDuration <= 0f
            ? 0f
            : Mathf.Clamp01(1f - (CastRemaining / CastDuration));

        public float CooldownProgress => CooldownDuration <= 0f
            ? 0f
            : Mathf.Clamp01(1f - (CooldownRemaining / CooldownDuration));

        public bool HasCast => CastDuration > 0f;
        public bool HasCooldown => CooldownDuration > 0f;
    }

    public interface IDockAbilityStateSource
    {
        event Action<string, DockAbilityState> AbilityStateChanged;
        bool TryGetState(string abilityId, out DockAbilityState state);
    }
}
