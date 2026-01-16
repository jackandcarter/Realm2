using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.Combat.Pipeline
{
    [Serializable]
    public struct CombatAbilityRequest
    {
        public string AbilityId;
        public string CasterId;
        public List<string> TargetIds;
        public Vector3 TargetPoint;
        public float ClientTime;
    }

    [Serializable]
    public struct CombatAbilityConfirmation
    {
        public string AbilityId;
        public string CasterId;
        public List<string> TargetIds;
        public float ServerTime;
    }

    [DisallowMultipleComponent]
    public class CombatServerBridge : MonoBehaviour
    {
        [SerializeField] private bool autoConfirm = true;

        public event Action<CombatAbilityRequest> AbilityRequested;
        public event Action<CombatAbilityConfirmation> AbilityConfirmed;

        public void RequestAbilityExecution(CombatAbilityRequest request)
        {
            AbilityRequested?.Invoke(request);

            if (!autoConfirm)
            {
                return;
            }

            var confirmation = new CombatAbilityConfirmation
            {
                AbilityId = request.AbilityId,
                CasterId = request.CasterId,
                TargetIds = request.TargetIds,
                ServerTime = Time.time
            };

            AbilityConfirmed?.Invoke(confirmation);
        }
    }
}
