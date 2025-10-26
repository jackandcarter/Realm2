using System;
using System.Collections.Generic;
using Client.CharacterCreation;
using Client.Player;
using UnityEngine;

namespace Client.UI.HUD
{
    [DisallowMultipleComponent]
    public class GameplayHudController : MonoBehaviour
    {
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private RectTransform classDockAnchor;
        [SerializeField] private List<ClassUiModuleBinding> classUiModules = new();

        private string _activeClassId;
        private IClassUiModule _activeModule;
        private GameObject _activeInstance;
        private bool _ownsActiveInstance;
        private bool _initialized;

        [Serializable]
        private class ClassUiModuleBinding
        {
            public string classId;
            public GameObject prefab;
        }

        private void Awake()
        {
            if (mainCanvas == null)
            {
                mainCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (mainCanvas == null)
            {
                mainCanvas = GetComponent<Canvas>();
            }

            if (classDockAnchor == null && mainCanvas != null)
            {
                var rectTransforms = mainCanvas.GetComponentsInChildren<RectTransform>(true);
                foreach (var rect in rectTransforms)
                {
                    if (string.Equals(rect.gameObject.name, "ClassDockAnchor", StringComparison.Ordinal))
                    {
                        classDockAnchor = rect;
                        break;
                    }
                }
            }
        }

        private void OnEnable()
        {
            PlayerClassStateManager.ActiveClassChanged += OnActiveClassChanged;
            PlayerClassStateManager.ArkitectAvailabilityChanged += OnArkitectAvailabilityChanged;

            if (!_initialized)
            {
                AdoptExistingModule(PlayerClassStateManager.ActiveClassId);
                _initialized = true;
            }

            LoadClassDock(PlayerClassStateManager.ActiveClassId);
        }

        private void OnDisable()
        {
            PlayerClassStateManager.ActiveClassChanged -= OnActiveClassChanged;
            PlayerClassStateManager.ArkitectAvailabilityChanged -= OnArkitectAvailabilityChanged;
        }

        private void OnDestroy()
        {
            PlayerClassStateManager.ActiveClassChanged -= OnActiveClassChanged;
            PlayerClassStateManager.ArkitectAvailabilityChanged -= OnArkitectAvailabilityChanged;
        }

        private void OnActiveClassChanged(string classId)
        {
            LoadClassDock(classId);
        }

        private void OnArkitectAvailabilityChanged(bool available)
        {
            _activeModule?.OnAbilityStateChanged(ClassUnlockUtility.BuilderClassId, available);
        }

        private void LoadClassDock(string classId)
        {
            if (string.Equals(_activeClassId, classId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            UnloadActiveModule();

            if (!AdoptExistingModule(classId) && TryGetBinding(classId, out var binding))
            {
                CreateModuleInstance(binding);
            }
        }

        private void CreateModuleInstance(ClassUiModuleBinding binding)
        {
            if (binding?.prefab == null || classDockAnchor == null)
            {
                return;
            }

            var instance = Instantiate(binding.prefab);
            var component = FindModuleComponent(instance);
            if (component == null)
            {
                DestroyInstance(instance);
                return;
            }

            var module = (IClassUiModule)component;
            module.Mount(classDockAnchor);

            _activeModule = module;
            _activeInstance = component.gameObject;
            _activeClassId = ResolveClassId(module, binding.classId?.Trim());
            _ownsActiveInstance = true;
        }

        private bool AdoptExistingModule(string classId)
        {
            if (classDockAnchor == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(classId))
            {
                return false;
            }

            foreach (Transform child in classDockAnchor)
            {
                var component = FindModuleComponent(child.gameObject);
                if (component == null)
                {
                    continue;
                }

                var module = (IClassUiModule)component;
                var moduleClassId = ResolveClassId(module, null);
                if (!string.IsNullOrWhiteSpace(moduleClassId) &&
                    string.Equals(moduleClassId, classId, StringComparison.OrdinalIgnoreCase))
                {
                    module.Mount(classDockAnchor);
                    _activeModule = module;
                    _activeInstance = component.gameObject;
                    _activeClassId = moduleClassId;
                    _ownsActiveInstance = false;
                    return true;
                }
            }

            return false;
        }

        private void UnloadActiveModule()
        {
            if (_activeModule != null)
            {
                _activeModule.Unmount();
            }

            if (_ownsActiveInstance && _activeInstance != null)
            {
                DestroyInstance(_activeInstance);
            }

            _activeModule = null;
            _activeInstance = null;
            _activeClassId = null;
            _ownsActiveInstance = false;
        }

        private static MonoBehaviour FindModuleComponent(GameObject root)
        {
            if (root == null)
            {
                return null;
            }

            var components = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var component in components)
            {
                if (component is IClassUiModule)
                {
                    return component;
                }
            }

            return null;
        }

        private static string ResolveClassId(IClassUiModule module, string fallback)
        {
            if (module == null)
            {
                return fallback;
            }

            var classId = module.ClassId;
            return string.IsNullOrWhiteSpace(classId) ? fallback : classId;
        }

        private bool TryGetBinding(string classId, out ClassUiModuleBinding binding)
        {
            binding = null;

            if (string.IsNullOrWhiteSpace(classId))
            {
                return false;
            }

            foreach (var candidate in classUiModules)
            {
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.classId))
                {
                    continue;
                }

                var candidateId = candidate.classId.Trim();
                if (string.Equals(candidateId, classId, StringComparison.OrdinalIgnoreCase))
                {
                    binding = candidate;
                    return true;
                }
            }

            return false;
        }

        private void DestroyInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }
    }
}
