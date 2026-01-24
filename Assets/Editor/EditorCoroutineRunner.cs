#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace EditorUtilities
{
    public static class EditorCoroutineRunner
    {
        private static readonly List<IEnumerator> Routines = new();
        private static bool _isRunning;

        public static void Start(IEnumerator routine)
        {
            if (routine == null)
            {
                return;
            }

            Routines.Add(routine);
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            for (var i = Routines.Count - 1; i >= 0; i--)
            {
                var routine = Routines[i];
                if (routine == null || !routine.MoveNext())
                {
                    Routines.RemoveAt(i);
                }
            }

            if (Routines.Count == 0)
            {
                _isRunning = false;
                EditorApplication.update -= Tick;
            }
        }
    }
}
#endif
