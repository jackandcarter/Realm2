using System;
using System.Collections;
using System.Collections.Generic;
using Client;
using Client.Combat;
using UnityEngine;

namespace Client.Combat.Pipeline
{
    [Serializable]
    public struct CombatAbilityRequest
    {
        public string requestId;
        public string abilityId;
        public string casterId;
        public string primaryTargetId;
        public List<string> targetIds;
        public Vector3 targetPoint;
        public float clientTime;
    }

    [Serializable]
    public struct CombatAbilityConfirmation
    {
        public string requestId;
        public string abilityId;
        public string casterId;
        public List<string> targetIds;
        public float serverTime;
        public List<CombatAbilityEvent> events;
    }

    [Serializable]
    public struct CombatAbilityEvent
    {
        public string kind;
        public string targetId;
        public float amount;
        public string stateId;
        public float durationSeconds;
    }

    [DisallowMultipleComponent]
    public class CombatServerBridge : MonoBehaviour
    {
        [SerializeField] private ApiEnvironmentConfig environmentConfig;
        [SerializeField] private string fallbackBaseApiUrl = "http://localhost:3000";
        [SerializeField] private bool useMockServer;
        [SerializeField] private bool autoConfirm;
        [SerializeField] private bool fallbackToLocalOnError;
        [SerializeField] private bool forceServerAuthority = true;

        private CombatApiClient _apiClient;

        public event Action<CombatAbilityRequest> AbilityRequested;
        public event Action<CombatAbilityConfirmation> AbilityConfirmed;

        private void Awake()
        {
            InitializeClient();
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(fallbackBaseApiUrl))
            {
                fallbackBaseApiUrl = "http://localhost:3000";
            }
        }

        public void RequestAbilityExecution(CombatAbilityRequest request)
        {
            AbilityRequested?.Invoke(request);

            var allowAutoConfirm = autoConfirm && useMockServer && !forceServerAuthority;
            if (!allowAutoConfirm)
            {
                StartCoroutine(SendAbilityRequest(request));
                return;
            }

            var confirmation = new CombatAbilityConfirmation
            {
                requestId = request.requestId,
                abilityId = request.abilityId,
                casterId = request.casterId,
                targetIds = request.targetIds,
                serverTime = Time.time
            };

            ReceiveServerConfirmation(confirmation);
        }

        public void ReceiveServerConfirmation(CombatAbilityConfirmation confirmation)
        {
            AbilityConfirmed?.Invoke(confirmation);
        }

        private IEnumerator SendAbilityRequest(CombatAbilityRequest request)
        {
            if (_apiClient == null)
            {
                yield break;
            }

            CombatAbilityConfirmation confirmation = default;
            ApiError requestError = null;

            yield return _apiClient.ExecuteAbility(
                request,
                result => confirmation = result,
                error => requestError = error);

            if (requestError != null)
            {
                Debug.LogWarning($"CombatServerBridge failed to send ability request: {requestError.Message}", this);
                if (fallbackToLocalOnError && useMockServer && !forceServerAuthority)
                {
                    ReceiveServerConfirmation(new CombatAbilityConfirmation
                    {
                        requestId = request.requestId,
                        abilityId = request.abilityId,
                        casterId = request.casterId,
                        targetIds = request.targetIds,
                        serverTime = Time.time
                    });
                }

                yield break;
            }

            ReceiveServerConfirmation(confirmation);
        }

        private void InitializeClient()
        {
            var baseUrl = environmentConfig != null && !string.IsNullOrWhiteSpace(environmentConfig.BaseApiUrl)
                ? environmentConfig.BaseApiUrl
                : fallbackBaseApiUrl;
            _apiClient = new CombatApiClient(baseUrl, useMockServer);
        }
    }
}
