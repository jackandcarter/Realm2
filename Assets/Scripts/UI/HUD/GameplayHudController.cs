using System;
using System.Collections.Generic;
using Client.CharacterCreation;
using Client.Player;
using Client.UI;
using Client.UI.HUD.Dock;
using UnityEngine;

namespace Client.UI.HUD
{
    [DisallowMultipleComponent]
    public class GameplayHudController : MonoBehaviour
    {
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private RectTransform classDockAnchor;
        [SerializeField] private GameObject masterDockPrefab;
        [SerializeField] private List<ClassUiModuleBinding> classUiModules = new();
        [SerializeField] private ClassAbilityDockModule abilityDockModule;
        [SerializeField] private List<MonoBehaviour> lockedAbilityPanels = new();

        private string _activeClassId;
        private IClassUiModule _activeModule;
        private GameObject _activeInstance;
        private bool _ownsActiveInstance;
        private RectTransform _masterDockRoot;
        private RectTransform _centerSection;
        private RectTransform _rightSection;
        private bool _initialized;

        [Serializable]
        private class ClassUiModuleBinding
        {
            public string classId;
            public GameObject prefab;
            public MonoBehaviour sceneModule;
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
            PlayerAbilityUnlockState.AbilityUnlocksChanged += OnAbilityUnlocksChanged;

            if (!_initialized)
            {
                EnsureArkitectModuleBinding();
                AdoptExistingModule(PlayerClassStateManager.ActiveClassId);
                _initialized = true;
            }

            LoadClassDock(PlayerClassStateManager.ActiveClassId);
        }

        private void OnDisable()
        {
            PlayerClassStateManager.ActiveClassChanged -= OnActiveClassChanged;
            PlayerClassStateManager.ArkitectAvailabilityChanged -= OnArkitectAvailabilityChanged;
            PlayerAbilityUnlockState.AbilityUnlocksChanged -= OnAbilityUnlocksChanged;
        }

        private void OnDestroy()
        {
            PlayerClassStateManager.ActiveClassChanged -= OnActiveClassChanged;
            PlayerClassStateManager.ArkitectAvailabilityChanged -= OnArkitectAvailabilityChanged;
            PlayerAbilityUnlockState.AbilityUnlocksChanged -= OnAbilityUnlocksChanged;
        }

        private void OnActiveClassChanged(string classId)
        {
            LoadClassDock(classId);
        }

        private void OnArkitectAvailabilityChanged(bool _)
        {
            ApplyArkitectAvailabilityIfNeeded();
        }

        private void OnAbilityUnlocksChanged()
        {
            RefreshLockedAbilityPanels(PlayerClassStateManager.ActiveClassId);
        }

        private void LoadClassDock(string classId)
        {
            EnsureMasterDock();

            if (string.Equals(_activeClassId, classId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            UnloadActiveModule();

            if (!AdoptExistingModule(classId) && TryGetBinding(classId, out var binding))
            {
                if (!TryMountSceneModule(binding))
                {
                    CreateModuleInstance(binding);
                }
            }

            RefreshLockedAbilityPanels(classId);
            ApplyArkitectAvailabilityIfNeeded();
        }

        private void CreateModuleInstance(ClassUiModuleBinding binding)
        {
            if (binding?.prefab == null)
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
            module.Mount(GetModuleContainer());

            _activeModule = module;
            _activeInstance = component.gameObject;
            _activeClassId = ResolveClassId(module, binding.classId?.Trim());
            _ownsActiveInstance = true;
        }

        private bool TryMountSceneModule(ClassUiModuleBinding binding)
        {
            if (binding?.sceneModule == null)
            {
                return false;
            }

            if (binding.sceneModule is not IClassUiModule module)
            {
                return false;
            }

            var moduleTransform = binding.sceneModule.transform;
            module.Mount(GetModuleContainer());

            _activeModule = module;
            _activeInstance = moduleTransform.gameObject;
            _activeClassId = ResolveClassId(module, binding.classId?.Trim());
            _ownsActiveInstance = false;
            return true;
        }

        private bool AdoptExistingModule(string classId)
        {
            EnsureMasterDock();
            var moduleContainer = GetModuleContainer();

            if (string.IsNullOrWhiteSpace(classId))
            {
                return false;
            }

            if (moduleContainer == null)
            {
                return false;
            }

            foreach (Transform child in moduleContainer)
            {
                var component = FindModuleComponent(child.gameObject);
                if (component == null)
                {
                    continue;
                }

                if (component is ClassAbilityDockModule)
                {
                    continue;
                }

                var module = (IClassUiModule)component;
                var moduleClassId = ResolveClassId(module, null);
                if (!string.IsNullOrWhiteSpace(moduleClassId) &&
                    string.Equals(moduleClassId, classId, StringComparison.OrdinalIgnoreCase))
                {
                    module.Mount(moduleContainer);
                    _activeModule = module;
                    _activeInstance = component.gameObject;
                    _activeClassId = moduleClassId;
                    _ownsActiveInstance = false;
                    return true;
                }
            }

            return false;
        }

        private Transform GetModuleContainer()
        {
            return _centerSection != null ? _centerSection : classDockAnchor;
        }

        private void EnsureMasterDock()
        {
            if (classDockAnchor == null)
            {
                return;
            }

            if (_masterDockRoot != null)
            {
                return;
            }

            foreach (Transform child in classDockAnchor)
            {
                var center = FindSection(child, "CenterSection");
                if (center != null)
                {
                    _masterDockRoot = child as RectTransform;
                    _centerSection = center;
                    _rightSection = FindSection(child, "RightSection");
                    InitializeShortcutSection();
                    EnsureAbilityDockModule();
                    return;
                }
            }

            if (masterDockPrefab == null)
            {
                return;
            }

            var instance = Instantiate(masterDockPrefab, classDockAnchor);
            _masterDockRoot = instance.GetComponent<RectTransform>();
            _centerSection = FindSection(instance.transform, "CenterSection");
            _rightSection = FindSection(instance.transform, "RightSection");
            InitializeShortcutSection();
            EnsureAbilityDockModule();
        }

        private void EnsureArkitectModuleBinding()
        {
            if (TryGetBinding(ClassUnlockUtility.BuilderClassId, out var existing))
            {
                if (existing.sceneModule == null && existing.prefab == null)
                {
                    existing.sceneModule = FindFirstObjectByType<ArkitectUIManager>();
                }

                return;
            }

            var manager = FindFirstObjectByType<ArkitectUIManager>();
            if (manager == null)
            {
                return;
            }

            classUiModules.Add(new ClassUiModuleBinding
            {
                classId = ClassUnlockUtility.BuilderClassId,
                sceneModule = manager
            });
        }

        private static RectTransform FindSection(Transform root, string name)
        {
            if (root == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return root.Find(name) as RectTransform;
        }

        private void InitializeShortcutSection()
        {
            if (_rightSection == null)
            {
                return;
            }

            var lookup = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            var defaultOrder = new List<string>();
            foreach (Transform child in _rightSection)
            {
                var shortcutId = ResolveShortcutId(child);
                if (string.IsNullOrWhiteSpace(shortcutId))
                {
                    continue;
                }

                var normalized = shortcutId.Trim();
                if (lookup.ContainsKey(normalized))
                {
                    continue;
                }

                lookup.Add(normalized, child);
                defaultOrder.Add(normalized);
            }

            if (defaultOrder.Count == 0)
            {
                return;
            }

            var storedOrder = DockShortcutLayoutStore.GetLayout(defaultOrder);
            var finalOrder = MergeOrders(storedOrder, defaultOrder, lookup.Keys);
            ApplyShortcutOrder(finalOrder, lookup);
        }

        private void EnsureAbilityDockModule()
        {
            if (_centerSection == null)
            {
                return;
            }

            if (abilityDockModule == null)
            {
                abilityDockModule = _centerSection.GetComponentInChildren<ClassAbilityDockModule>(true);
            }

            if (abilityDockModule == null)
            {
                return;
            }

            abilityDockModule.Mount(_centerSection);
        }

        private static string ResolveShortcutId(Transform shortcutTransform)
        {
            if (shortcutTransform == null)
            {
                return null;
            }

            if (shortcutTransform.TryGetComponent(out DockShortcutId shortcutId) &&
                !string.IsNullOrWhiteSpace(shortcutId.ShortcutId))
            {
                return shortcutId.ShortcutId;
            }

            return shortcutTransform.gameObject.name;
        }

        private static List<string> MergeOrders(
            IReadOnlyList<string> stored,
            IReadOnlyList<string> defaults,
            ICollection<string> validIds)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (stored != null)
            {
                foreach (var shortcutId in stored)
                {
                    if (string.IsNullOrWhiteSpace(shortcutId))
                    {
                        continue;
                    }

                    if (!validIds.Contains(shortcutId) || !seen.Add(shortcutId))
                    {
                        continue;
                    }

                    result.Add(shortcutId);
                }
            }

            if (defaults != null)
            {
                foreach (var shortcutId in defaults)
                {
                    if (string.IsNullOrWhiteSpace(shortcutId))
                    {
                        continue;
                    }

                    if (!validIds.Contains(shortcutId) || !seen.Add(shortcutId))
                    {
                        continue;
                    }

                    result.Add(shortcutId);
                }
            }

            return result;
        }

        private static void ApplyShortcutOrder(IReadOnlyList<string> order, Dictionary<string, Transform> lookup)
        {
            if (order == null || lookup == null)
            {
                return;
            }

            for (var i = 0; i < order.Count; i++)
            {
                if (!lookup.TryGetValue(order[i], out var shortcut) || shortcut == null)
                {
                    continue;
                }

                shortcut.SetSiblingIndex(i);
            }
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

        private void ApplyArkitectAvailabilityIfNeeded()
        {
            if (_activeModule == null)
            {
                return;
            }

            if (!string.Equals(_activeModule.ClassId, ClassUnlockUtility.BuilderClassId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _activeModule.OnAbilityStateChanged(
                ClassUnlockUtility.BuilderClassId,
                PlayerClassStateManager.IsArkitectAvailable);
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

        private void RefreshLockedAbilityPanels(string classId)
        {
            if (lockedAbilityPanels == null || lockedAbilityPanels.Count == 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(classId))
            {
                return;
            }

            var classKey = classId.Trim();
            var entries = ClassAbilityCatalog.GetAbilityDockEntries(classKey);
            var lockedEntries = new List<ClassAbilityCatalog.ClassAbilityDockEntry>();
            foreach (var entry in entries)
            {
                if (!entry.IsValid)
                {
                    continue;
                }

                if (!PlayerAbilityUnlockState.IsAbilityUnlocked(classKey, entry.AbilityId))
                {
                    lockedEntries.Add(entry);
                }
            }

            foreach (var panel in lockedAbilityPanels)
            {
                if (panel is IClassLockedAbilityPanel lockedPanel)
                {
                    lockedPanel.SetLockedAbilities(classKey, lockedEntries);
                }
            }
        }
    }
}
