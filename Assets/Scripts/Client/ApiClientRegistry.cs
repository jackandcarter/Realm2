using UnityEngine;

namespace Client
{
    public static class ApiClientRegistry
    {
        public static string BaseUrl { get; private set; }
        public static bool UseMockServices { get; private set; }

        public static void Configure(string baseUrl, bool useMocks)
        {
            BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.TrimEnd('/');
            UseMockServices = useMocks;
        }

        public static bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);

        public static void EnsureConfigured(string fallbackUrl)
        {
            if (IsConfigured)
            {
                return;
            }

            BaseUrl = string.IsNullOrWhiteSpace(fallbackUrl) ? null : fallbackUrl.TrimEnd('/');
            UseMockServices = false;
            if (!string.IsNullOrWhiteSpace(BaseUrl))
            {
                Debug.LogWarning(
                    $"ApiClientRegistry was not configured. Falling back to {BaseUrl}."
                );
            }
        }
    }
}
