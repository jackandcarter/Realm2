using System.Collections.Generic;
using Client.CharacterCreation;
using Client.Player;
using UnityEngine;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    public class ClassAbilityDockDebugSwitcher : MonoBehaviour
    {
        [SerializeField] private List<string> classCycleOrder = new();
        [SerializeField] private KeyCode nextKey = KeyCode.RightBracket;
        [SerializeField] private KeyCode previousKey = KeyCode.LeftBracket;
        [SerializeField] private bool populateFromCatalog = true;
        [SerializeField] private float autoCycleIntervalSeconds;

        private int _index;
        private float _nextCycleTime;

        private void Awake()
        {
            PopulateClassList();
        }

        private void OnEnable()
        {
            PopulateClassList();
            _nextCycleTime = Time.unscaledTime + Mathf.Max(0f, autoCycleIntervalSeconds);
        }

        private void Update()
        {
            if (autoCycleIntervalSeconds > 0f && Time.unscaledTime >= _nextCycleTime)
            {
                Cycle(1);
                _nextCycleTime = Time.unscaledTime + autoCycleIntervalSeconds;
            }

            if (Input.GetKeyDown(nextKey))
            {
                Cycle(1);
            }
            else if (Input.GetKeyDown(previousKey))
            {
                Cycle(-1);
            }
        }

        [ContextMenu("Cycle Next Class")]
        public void CycleNext()
        {
            Cycle(1);
        }

        [ContextMenu("Cycle Previous Class")]
        public void CyclePrevious()
        {
            Cycle(-1);
        }

        private void Cycle(int delta)
        {
            if (classCycleOrder == null || classCycleOrder.Count == 0)
            {
                return;
            }

            _index = (_index + delta) % classCycleOrder.Count;
            if (_index < 0)
            {
                _index += classCycleOrder.Count;
            }

            var classId = classCycleOrder[_index];
            if (string.IsNullOrWhiteSpace(classId))
            {
                return;
            }

            if (!PlayerClassStateManager.TrySetActiveClass(classId))
            {
                Debug.LogWarning($"ClassAbilityDockDebugSwitcher could not activate class '{classId}'. Ensure the class is unlocked for the active character.", this);
            }
            else
            {
                Debug.Log($"[DockDebug] Active class set to '{classId}'.", this);
            }
        }

        private void PopulateClassList()
        {
            if (!populateFromCatalog)
            {
                return;
            }

            if (classCycleOrder != null && classCycleOrder.Count > 0)
            {
                return;
            }

            var ids = ClassAbilityCatalog.GetKnownClassIds();
            if (ids != null && ids.Count > 0)
            {
                if (classCycleOrder == null)
                {
                    classCycleOrder = new List<string>(ids);
                }
                else
                {
                    classCycleOrder.Clear();
                    classCycleOrder.AddRange(ids);
                }
            }
        }
    }
}
