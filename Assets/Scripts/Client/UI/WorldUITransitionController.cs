using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client.UI
{
    [DisallowMultipleComponent]
    public class WorldUITransitionController : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private CanvasGroup hudGroup;
        [SerializeField] private List<GameObject> panelsVisibleOnEnter = new();
        [SerializeField] private List<GameObject> panelsHiddenOnEnter = new();
        [SerializeField] private float hudFadeDuration = 0.35f;
        [SerializeField] private bool hideHudOnAwake = true;

        [Header("Arkitect UI")]
        [SerializeField] private ArkitectUIManager arkitectUiManager;

        private Coroutine _fadeRoutine;

        private void Awake()
        {
            if (hideHudOnAwake)
            {
                HideHudImmediate();
            }
        }

        public void Configure(CanvasGroup hud, ArkitectUIManager arkitectManager)
        {
            if (hudGroup == null)
            {
                hudGroup = hud;
            }

            if (arkitectUiManager == null)
            {
                arkitectUiManager = arkitectManager;
            }

            if (hideHudOnAwake)
            {
                HideHudImmediate();
            }
        }

        public void PrepareForPreview()
        {
            HideHudImmediate();
            ApplyPanelVisibility(false);

            if (arkitectUiManager != null)
            {
                arkitectUiManager.HideAllPanels();
            }
        }

        public void EnterWorld()
        {
            ApplyPanelVisibility(true);
            if (arkitectUiManager != null)
            {
                arkitectUiManager.HideAllPanels();
            }
            FadeHud(1f);
        }

        public void HideHudImmediate()
        {
            if (hudGroup == null)
            {
                return;
            }

            hudGroup.alpha = 0f;
            hudGroup.blocksRaycasts = false;
            hudGroup.interactable = false;
        }

        private void FadeHud(float targetAlpha)
        {
            if (hudGroup == null)
            {
                return;
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeHudRoutine(targetAlpha));
        }

        private IEnumerator FadeHudRoutine(float targetAlpha)
        {
            var startAlpha = hudGroup.alpha;
            var time = 0f;
            while (time < hudFadeDuration)
            {
                time += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(time / Mathf.Max(0.01f, hudFadeDuration));
                hudGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            hudGroup.alpha = targetAlpha;
            hudGroup.blocksRaycasts = targetAlpha > 0.01f;
            hudGroup.interactable = targetAlpha > 0.01f;
        }

        private void ApplyPanelVisibility(bool enteringWorld)
        {
            foreach (var panel in panelsVisibleOnEnter)
            {
                if (panel != null)
                {
                    panel.SetActive(enteringWorld);
                }
            }

            foreach (var panel in panelsHiddenOnEnter)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
        }
    }
}
