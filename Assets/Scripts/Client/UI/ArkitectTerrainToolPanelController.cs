using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI
{
    [DisallowMultipleComponent]
    public class ArkitectTerrainToolPanelController : MonoBehaviour
    {
        [SerializeField] private RectTransform toolButtonRoot;
        [SerializeField] private RectTransform toolPanelRoot;
        [SerializeField] private string buttonSuffix = "Tool";
        [SerializeField] private string panelSuffix = "Panel";

        private readonly Dictionary<Button, GameObject> _panelLookup = new();
        private readonly Dictionary<Button, UnityEngine.Events.UnityAction> _handlers = new();
        private GameObject _activePanel;

        private void OnEnable()
        {
            ResolveReferences();
            WireButtons();
            ShowFirstPanelIfNeeded();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        public void Configure(RectTransform buttonRoot, RectTransform panelRoot)
        {
            toolButtonRoot = buttonRoot;
            toolPanelRoot = panelRoot;
        }

        private void ResolveReferences()
        {
            if (toolButtonRoot == null)
            {
                var found = transform.Find("TerrainLayout/ToolsColumn/ToolsSidebar");
                if (found != null)
                {
                    toolButtonRoot = found.GetComponent<RectTransform>();
                }
            }

            if (toolPanelRoot == null)
            {
                var found = transform.Find("TerrainLayout/InfoColumn/TerrainToolsCard/ToolDetailPanels");
                if (found != null)
                {
                    toolPanelRoot = found.GetComponent<RectTransform>();
                }
            }
        }

        private void WireButtons()
        {
            _panelLookup.Clear();
            _activePanel = null;

            if (toolButtonRoot == null || toolPanelRoot == null)
            {
                return;
            }

            var buttons = toolButtonRoot.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                var panelName = ResolvePanelName(button.name);
                var panelTransform = toolPanelRoot.Find(panelName);
                if (panelTransform == null)
                {
                    continue;
                }

                var panelObject = panelTransform.gameObject;
                _panelLookup[button] = panelObject;

                if (_handlers.TryGetValue(button, out var existing))
                {
                    button.onClick.RemoveListener(existing);
                }

                UnityEngine.Events.UnityAction handler = () => HandleButtonClicked(button);
                _handlers[button] = handler;
                button.onClick.AddListener(handler);
            }
        }

        private void UnwireButtons()
        {
            foreach (var pair in _panelLookup)
            {
                if (pair.Key == null)
                {
                    continue;
                }

                if (_handlers.TryGetValue(pair.Key, out var handler))
                {
                    pair.Key.onClick.RemoveListener(handler);
                }
            }

            _panelLookup.Clear();
            _handlers.Clear();
        }

        private void HandleButtonClicked(Button button)
        {
            if (button == null || !_panelLookup.TryGetValue(button, out var panel))
            {
                return;
            }

            if (_activePanel == panel)
            {
                panel.SetActive(false);
                _activePanel = null;
                return;
            }

            foreach (var entry in _panelLookup.Values)
            {
                if (entry != null)
                {
                    entry.SetActive(false);
                }
            }

            panel.SetActive(true);
            _activePanel = panel;
        }

        private void ShowFirstPanelIfNeeded()
        {
            if (_activePanel != null || _panelLookup.Count == 0)
            {
                return;
            }

            foreach (var panel in _panelLookup.Values)
            {
                if (panel == null)
                {
                    continue;
                }

                panel.SetActive(true);
                _activePanel = panel;
                break;
            }
        }

        private string ResolvePanelName(string buttonName)
        {
            if (string.IsNullOrWhiteSpace(buttonName))
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(buttonSuffix) && buttonName.EndsWith(buttonSuffix, StringComparison.Ordinal))
            {
                return buttonName[..^buttonSuffix.Length] + panelSuffix;
            }

            return buttonName + panelSuffix;
        }
    }
}
