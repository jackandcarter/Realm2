using System.Collections;
using UnityEngine;

namespace Client.Progression
{
    public static class ProgressionCoroutineRunner
    {
        private const string RunnerName = "ProgressionCoroutineRunner";
        private static CoroutineHost _host;

        public static void Run(IEnumerator routine)
        {
            if (routine == null)
            {
                return;
            }

            EnsureHost();
            _host.StartCoroutine(routine);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureHost()
        {
            if (_host != null)
            {
                return;
            }

            var existing = GameObject.Find(RunnerName);
            if (existing != null && existing.TryGetComponent(out CoroutineHost host))
            {
                _host = host;
                return;
            }

            var runner = new GameObject(RunnerName);
            Object.DontDestroyOnLoad(runner);
            _host = runner.AddComponent<CoroutineHost>();
        }

        private sealed class CoroutineHost : MonoBehaviour
        {
        }
    }
}
