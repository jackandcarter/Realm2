using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Client.CharacterCreation;
using Client.Player;
using Client.UI;
using Client.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Client
{
    [DisallowMultipleComponent]
    public class WorldPreviewTransitionManager : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string defaultWorldSceneName = "SampleScene";
        [SerializeField] private bool unloadMenuSceneOnEnter = true;
        [SerializeField] private bool disableMenuCameraDuringPreview = true;

        [Header("Menu UI")]
        [SerializeField] private List<CanvasGroup> menuPanels = new();
        [SerializeField] private string[] menuPanelNames = { "LoginCanvas", "CreateAccountCanvas", "RealmCanvas", "CharacterCanvas" };
        [SerializeField] private float menuFadeDuration = 0.35f;

        [Header("World HUD")]
        [SerializeField] private CanvasGroup worldHudGroup;
        [SerializeField] private string worldHudName = "ArkitectCanvas";
        [SerializeField] private WorldUITransitionController worldUiTransitionController;

        [Header("Preview Avatar")]
        [SerializeField] private GameObject previewAvatarPrefab;
        [SerializeField] private CharacterPrefabCatalog characterPrefabCatalog;
        [SerializeField] private Vector3 fallbackSpawnPosition = new(0f, 2f, 0f);
        [SerializeField] private bool disableAvatarInputDuringPreview = true;

        private string _menuSceneName;
        private string _loadedWorldSceneName;
        private Scene _worldScene;
        private GameObject _previewAvatar;
        private Camera _menuCamera;
        private CharacterInfo _previewCharacter;
        private bool _enteringWorld;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _menuSceneName = SceneManager.GetActiveScene().name;
            _menuCamera = Camera.main;
            ResolveMenuPanels();
        }

        public void PreviewCharacter(CharacterInfo character, string worldSceneName)
        {
            if (character == null)
            {
                return;
            }

            var targetSceneName = string.IsNullOrWhiteSpace(worldSceneName) ? defaultWorldSceneName : worldSceneName;
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                return;
            }

            if (_previewCharacter != null && string.Equals(_previewCharacter.id, character.id, StringComparison.Ordinal))
            {
                return;
            }

            _previewCharacter = character;
            StartCoroutine(PreviewRoutine(targetSceneName, character));
        }

        public void EnterWorld(CharacterInfo character, string worldSceneName)
        {
            if (_enteringWorld)
            {
                return;
            }

            var targetSceneName = string.IsNullOrWhiteSpace(worldSceneName) ? defaultWorldSceneName : worldSceneName;
            StartCoroutine(EnterWorldRoutine(targetSceneName, character));
        }

        private IEnumerator PreviewRoutine(string worldSceneName, CharacterInfo character)
        {
            yield return EnsureWorldSceneLoaded(worldSceneName);

            if (!_worldScene.IsValid())
            {
                yield break;
            }

            if (disableMenuCameraDuringPreview && _menuCamera != null)
            {
                _menuCamera.enabled = false;
            }

            EnsureWorldHudHidden();
            if (worldUiTransitionController != null)
            {
                worldUiTransitionController.PrepareForPreview();
            }

            var spawnPosition = ResolveCharacterSpawnPosition(character);
            SpawnOrMovePreviewAvatar(spawnPosition);
        }

        private IEnumerator EnterWorldRoutine(string worldSceneName, CharacterInfo character)
        {
            _enteringWorld = true;
            _previewCharacter = character;

            yield return EnsureWorldSceneLoaded(worldSceneName);

            if (_worldScene.IsValid() && disableMenuCameraDuringPreview && _menuCamera != null)
            {
                _menuCamera.enabled = false;
            }

            var spawnPosition = ResolveCharacterSpawnPosition(character);
            SpawnOrMovePreviewAvatar(spawnPosition);
            ToggleAvatarInput(true);

            yield return FadeMenuPanels(0f, disableInteraction: true);
            EnsureWorldHudVisible();
            if (worldUiTransitionController != null)
            {
                worldUiTransitionController.EnterWorld();
            }

            if (unloadMenuSceneOnEnter && !string.IsNullOrWhiteSpace(_menuSceneName))
            {
                var menuScene = SceneManager.GetSceneByName(_menuSceneName);
                if (menuScene.IsValid())
                {
                    SceneManager.UnloadSceneAsync(menuScene);
                }
            }

            _enteringWorld = false;
            Destroy(gameObject);
        }

        private IEnumerator EnsureWorldSceneLoaded(string worldSceneName)
        {
            if (_worldScene.IsValid() && string.Equals(_loadedWorldSceneName, worldSceneName, StringComparison.Ordinal))
            {
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(_loadedWorldSceneName))
            {
                var existing = SceneManager.GetSceneByName(_loadedWorldSceneName);
                if (existing.IsValid())
                {
                    SceneManager.UnloadSceneAsync(existing);
                }
            }

            _loadedWorldSceneName = worldSceneName;
            var loadOp = SceneManager.LoadSceneAsync(worldSceneName, LoadSceneMode.Additive);
            if (loadOp != null)
            {
                while (!loadOp.isDone)
                {
                    yield return null;
                }
            }

            _worldScene = SceneManager.GetSceneByName(worldSceneName);
            if (_worldScene.IsValid())
            {
                SceneManager.SetActiveScene(_worldScene);
            }

            ResolveWorldUiTransitionController();
        }

        private Vector3 ResolveCharacterSpawnPosition(CharacterInfo character)
        {
            if (character != null && TryParseLocation(character.lastKnownLocation, out var position))
            {
                return position;
            }

            return PlayerSpawnService.ResolveSpawnPosition(fallbackSpawnPosition);
        }

        private void SpawnOrMovePreviewAvatar(Vector3 spawnPosition)
        {
            if (_previewAvatar == null)
            {
                var resolvedPrefab = ResolvePreviewPrefab(_previewCharacter);
                if (resolvedPrefab != null)
                {
                    _previewAvatar = Instantiate(resolvedPrefab, spawnPosition, Quaternion.identity);
                }
                else
                {
                    _previewAvatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    _previewAvatar.name = "PreviewAvatar";
                    _previewAvatar.transform.position = spawnPosition;
                    _previewAvatar.transform.rotation = Quaternion.identity;
                    var controller = _previewAvatar.AddComponent<CharacterController>();
                    controller.height = 1.8f;
                    controller.radius = 0.3f;
                    controller.center = new Vector3(0f, 0.9f, 0f);
                    _previewAvatar.AddComponent<PlayerAvatarController>();
                }

                if (_worldScene.IsValid())
                {
                    SceneManager.MoveGameObjectToScene(_previewAvatar, _worldScene);
                }

                ApplyAppearanceToAvatar(_previewAvatar, _previewCharacter);
            }
            else
            {
                _previewAvatar.transform.position = spawnPosition;
                ApplyAppearanceToAvatar(_previewAvatar, _previewCharacter);
            }

            ToggleAvatarInput(!disableAvatarInputDuringPreview);
        }

        private void ToggleAvatarInput(bool enabled)
        {
            if (_previewAvatar == null)
            {
                return;
            }

            var controller = _previewAvatar.GetComponent<PlayerAvatarController>();
            if (controller != null)
            {
                controller.enabled = enabled;
            }
        }

        private void EnsureWorldHudHidden()
        {
            var group = ResolveWorldHudGroup();
            if (group == null)
            {
                return;
            }

            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void EnsureWorldHudVisible()
        {
            var group = ResolveWorldHudGroup();
            if (group == null)
            {
                return;
            }

            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
        }

        private CanvasGroup ResolveWorldHudGroup()
        {
            if (worldHudGroup != null)
            {
                return worldHudGroup;
            }

            if (!_worldScene.IsValid() || string.IsNullOrWhiteSpace(worldHudName))
            {
                return null;
            }

            foreach (var root in _worldScene.GetRootGameObjects())
            {
                var found = root.transform.Find(worldHudName);
                if (found != null)
                {
                    worldHudGroup = EnsureCanvasGroup(found.gameObject);
                    return worldHudGroup;
                }

                if (string.Equals(root.name, worldHudName, StringComparison.OrdinalIgnoreCase))
                {
                    worldHudGroup = EnsureCanvasGroup(root);
                    return worldHudGroup;
                }
            }

            return null;
        }

        private void ResolveWorldUiTransitionController()
        {
            if (worldUiTransitionController != null || !_worldScene.IsValid())
            {
                return;
            }

            foreach (var root in _worldScene.GetRootGameObjects())
            {
                worldUiTransitionController = root.GetComponentInChildren<WorldUITransitionController>(true);
                if (worldUiTransitionController != null)
                {
                    if (worldUiTransitionController != null)
                    {
                        worldUiTransitionController.Configure(ResolveWorldHudGroup(), FindArkitectManager());
                    }
                    return;
                }
            }

            var hudGroup = ResolveWorldHudGroup();
            if (hudGroup != null)
            {
                worldUiTransitionController = hudGroup.gameObject.AddComponent<WorldUITransitionController>();
                worldUiTransitionController.Configure(hudGroup, FindArkitectManager());
            }
        }

        private ArkitectUIManager FindArkitectManager()
        {
            if (!_worldScene.IsValid())
            {
                return null;
            }

            foreach (var root in _worldScene.GetRootGameObjects())
            {
                var manager = root.GetComponentInChildren<ArkitectUIManager>(true);
                if (manager != null)
                {
                    return manager;
                }
            }

            return null;
        }

        private void ResolveMenuPanels()
        {
            menuPanels.RemoveAll(panel => panel == null);
            if (menuPanels.Count > 0)
            {
                return;
            }

            foreach (var name in menuPanelNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var root = GameObject.Find(name);
                if (root != null)
                {
                    menuPanels.Add(EnsureCanvasGroup(root));
                }
            }
        }

        private IEnumerator FadeMenuPanels(float targetAlpha, bool disableInteraction)
        {
            if (menuPanels.Count == 0)
            {
                yield break;
            }

            var startAlpha = menuPanels[0].alpha;
            var time = 0f;
            while (time < menuFadeDuration)
            {
                time += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(time / Mathf.Max(0.01f, menuFadeDuration));
                var alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                foreach (var panel in menuPanels)
                {
                    if (panel != null)
                    {
                        panel.alpha = alpha;
                    }
                }

                yield return null;
            }

            foreach (var panel in menuPanels)
            {
                if (panel == null)
                {
                    continue;
                }

                panel.alpha = targetAlpha;
                panel.blocksRaycasts = !disableInteraction && targetAlpha > 0.01f;
                panel.interactable = !disableInteraction && targetAlpha > 0.01f;
            }
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject root)
        {
            var group = root.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = root.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private static bool TryParseLocation(string location, out Vector3 position)
        {
            position = Vector3.zero;
            if (string.IsNullOrWhiteSpace(location))
            {
                return false;
            }

            var tokens = location.Split(new[] { ',', '|', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 3)
            {
                return false;
            }

            if (!TryParseFloat(tokens[0], out var x) ||
                !TryParseFloat(tokens[1], out var y) ||
                !TryParseFloat(tokens[2], out var z))
            {
                return false;
            }

            position = new Vector3(x, y, z);
            return true;
        }

        private static bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private GameObject ResolvePreviewPrefab(CharacterInfo character)
        {
            if (characterPrefabCatalog != null)
            {
                var resolved = characterPrefabCatalog.ResolvePrefab(character?.raceId, character?.classId);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            return previewAvatarPrefab;
        }

        private void ApplyAppearanceToAvatar(GameObject avatar, CharacterInfo character)
        {
            if (avatar == null || character?.appearance == null)
            {
                return;
            }

            if (!RaceCatalog.TryGetRace(character.raceId, out var race))
            {
                return;
            }

            var morphController = avatar.GetComponentInChildren<Realm.CharacterCustomization.CharacterMorphController>();
            if (morphController == null)
            {
                return;
            }

            var heightNormalized = NormalizeSelectionValue(race.Customization?.Height, character.appearance.height);
            var buildNormalized = NormalizeSelectionValue(race.Customization?.Build, character.appearance.build);

            foreach (var region in morphController.Regions)
            {
                if (region == null)
                {
                    continue;
                }

                region.heightNormalized = heightNormalized;
                region.widthNormalized = buildNormalized;
            }

            morphController.ApplyAll();
        }

        private static float NormalizeSelectionValue(FloatRange? range, float value)
        {
            if (!range.HasValue)
            {
                return 0.5f;
            }

            var min = range.Value.Min;
            var max = range.Value.Max;
            if (Mathf.Approximately(min, max))
            {
                return 0.5f;
            }

            return Mathf.InverseLerp(min, max, value);
        }
    }
}
