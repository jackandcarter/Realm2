using System;
using System.Collections;
using System.Text;
using Client;
using Client.Combat.Pipeline;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.Combat
{
    public class CombatApiClient
    {
        private readonly string _baseUrl;

        public CombatApiClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public IEnumerator ExecuteAbility(
            CombatAbilityRequest request,
            Action<CombatAbilityConfirmation> onSuccess,
            Action<ApiError> onError)
        {
            var payload = JsonUtility.ToJson(request);

            using var webRequest = new UnityWebRequest($"{_baseUrl}/combat/execute", UnityWebRequest.kHttpVerbPOST);
            var bodyRaw = Encoding.UTF8.GetBytes(payload);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            AttachAuthHeader(webRequest);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<CombatAbilityConfirmation>(webRequest.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(webRequest));
            }
        }

        private static void AttachAuthHeader(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(SessionManager.AuthToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {SessionManager.AuthToken}");
            }
        }
    }
}
