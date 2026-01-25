using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Client
{
    [DisallowMultipleComponent]
    public class SceneTransitionController : MonoBehaviour
    {
        [Header("Fade")]
        [SerializeField] private float fadeDuration = 0.6f;
        [SerializeField] private float holdDuration = 0.15f;
        [SerializeField] private Color fadeColor = Color.black;

        [Header("Particles")]
        [SerializeField] private ParticleSystem transitionParticles;
        [SerializeField] private float particleDuration = 1.4f;
        [SerializeField] private Mesh sourceMesh;
        [SerializeField] private string targetMeshName;
        [SerializeField] private bool emitTargetBurst = true;

        [Header("Scene")]
        [SerializeField] private bool unloadPreviousScene = true;
        [SerializeField] private bool destroyAfterTransition = true;

        private CanvasGroup _fadeGroup;
        private Image _fadeImage;
        private bool _isTransitioning;
        private string _originSceneName;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _originSceneName = SceneManager.GetActiveScene().name;

            EnsureFadeCanvas();
            EnsureParticles();
        }

        public void TransitionToScene(string sceneName)
        {
            if (_isTransitioning || string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            StartCoroutine(TransitionRoutine(sceneName));
        }

        private IEnumerator TransitionRoutine(string sceneName)
        {
            _isTransitioning = true;

            if (_fadeGroup != null)
            {
                yield return FadeTo(1f);
            }

            TryApplyMeshShape(sourceMesh);
            PlayParticles();

            var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (loadOp != null)
            {
                while (!loadOp.isDone)
                {
                    yield return null;
                }
            }

            var loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid())
            {
                SceneManager.SetActiveScene(loadedScene);
            }

            if (emitTargetBurst && loadedScene.IsValid())
            {
                var targetMesh = FindMeshInScene(loadedScene, targetMeshName);
                if (targetMesh != null)
                {
                    TryApplyMeshShape(targetMesh);
                    PlayParticles();
                }
            }

            yield return new WaitForSeconds(holdDuration);

            if (_fadeGroup != null)
            {
                yield return FadeTo(0f);
            }

            if (unloadPreviousScene && !string.IsNullOrWhiteSpace(_originSceneName))
            {
                var previousScene = SceneManager.GetSceneByName(_originSceneName);
                if (previousScene.IsValid())
                {
                    SceneManager.UnloadSceneAsync(previousScene);
                }
            }

            _isTransitioning = false;

            if (destroyAfterTransition)
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            if (_fadeGroup == null)
            {
                yield break;
            }

            _fadeGroup.blocksRaycasts = true;
            _fadeGroup.interactable = true;

            var startAlpha = _fadeGroup.alpha;
            var time = 0f;
            while (time < fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(time / Mathf.Max(0.01f, fadeDuration));
                _fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            _fadeGroup.alpha = targetAlpha;
            _fadeGroup.blocksRaycasts = targetAlpha > 0.01f;
            _fadeGroup.interactable = targetAlpha > 0.01f;
        }

        private void EnsureFadeCanvas()
        {
            var canvasObject = new GameObject("SceneTransitionCanvas");
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            canvasObject.AddComponent<CanvasScaler>();

            _fadeGroup = canvasObject.AddComponent<CanvasGroup>();
            _fadeGroup.alpha = 0f;
            _fadeGroup.blocksRaycasts = false;
            _fadeGroup.interactable = false;

            var imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(canvasObject.transform, false);
            var rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            _fadeImage = imageObject.AddComponent<Image>();
            _fadeImage.color = fadeColor;
        }

        private void EnsureParticles()
        {
            if (transitionParticles != null)
            {
                return;
            }

            var particleObject = new GameObject("SceneTransitionParticles");
            particleObject.transform.SetParent(transform, false);
            transitionParticles = particleObject.AddComponent<ParticleSystem>();

            var main = transitionParticles.main;
            main.duration = particleDuration;
            main.loop = false;
            main.startLifetime = 1.5f;
            main.startSpeed = 1.8f;
            main.startSize = 0.08f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.8f, 0.9f, 1f, 0.9f),
                new Color(0.4f, 0.6f, 1f, 0.9f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = transitionParticles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 320),
                new ParticleSystem.Burst(0.2f, 180)
            });

            var shape = transitionParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.6f;

            var velocity = transitionParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalY = 2.5f;

            var colorOverLifetime = transitionParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
                new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(new Color(0.9f, 0.95f, 1f), 0f),
                        new GradientColorKey(new Color(0.5f, 0.7f, 1f), 1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(1f, 0.2f),
                        new GradientAlphaKey(0f, 1f)
                    }
                });
        }

        private void TryApplyMeshShape(Mesh mesh)
        {
            if (transitionParticles == null)
            {
                return;
            }

            var shape = transitionParticles.shape;
            if (mesh == null)
            {
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.6f;
                return;
            }

            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.mesh = mesh;
        }

        private static Mesh FindMeshInScene(Scene scene, string meshName)
        {
            if (string.IsNullOrWhiteSpace(meshName))
            {
                return null;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                var filters = root.GetComponentsInChildren<MeshFilter>(true);
                foreach (var filter in filters)
                {
                    if (filter != null && filter.sharedMesh != null &&
                        string.Equals(filter.sharedMesh.name, meshName, StringComparison.OrdinalIgnoreCase))
                    {
                        return filter.sharedMesh;
                    }
                }
            }

            return null;
        }

        private void PlayParticles()
        {
            if (transitionParticles == null)
            {
                return;
            }

            var anchorPosition = Vector3.zero;
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                anchorPosition = mainCamera.transform.position + mainCamera.transform.forward * 1.5f;
            }

            transitionParticles.transform.position = anchorPosition;
            transitionParticles.Play();
        }
    }
}
