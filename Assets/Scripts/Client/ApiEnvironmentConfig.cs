using UnityEngine;

namespace Client
{
    [CreateAssetMenu(fileName = "ApiEnvironment", menuName = "Realm/Api Environment", order = 0)]
    public class ApiEnvironmentConfig : ScriptableObject
    {
        [SerializeField] private string environmentName = "Local";
        [SerializeField] private string baseApiUrl = "http://localhost:3000";
        [SerializeField] private bool useMockServicesInEditor = true;
        [SerializeField] private bool useMockServicesInPlayer = false;

        public string EnvironmentName => string.IsNullOrWhiteSpace(environmentName) ? "Unnamed" : environmentName;
        public string BaseApiUrl => string.IsNullOrWhiteSpace(baseApiUrl) ? "http://localhost:3000" : baseApiUrl.TrimEnd('/');

        public bool UseMockServices => Application.isEditor ? useMockServicesInEditor : useMockServicesInPlayer;
    }
}
