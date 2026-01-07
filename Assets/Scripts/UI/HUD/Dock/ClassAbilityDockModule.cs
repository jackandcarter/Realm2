using System;
using System.Collections.Generic;
using System.Linq;
using Client.CharacterCreation;
using Client.Combat;
using Client.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ClassAbilityDockModule : MonoBehaviour, IClassUiModule
    {
        private static readonly string[] DefaultHotkeys =
        {
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "0"
        };

        [Header("Class")]
        [SerializeField] private string classId;

        [Header("UI")]
        [SerializeField] private RectTransform itemContainer;
        [SerializeField] private RectTransform mountContainer;
        [SerializeField] private AbilityDockItem itemPrefab;
        [SerializeField] private Sprite fallbackIcon;
        [SerializeField] private string iconResourceFolder = "UI/AbilityIcons";

        [Header("Ability State")]
        [SerializeField] private MonoBehaviour abilityStateSource;

        [Header("Behaviour")]
        [SerializeField] private bool rebuildOnEnable = true;
        [SerializeField] private int slotCount = 10;

        private readonly List<AbilityDockItem> _items = new();
        private readonly Dictionary<string, AbilityDockItem> _abilityLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Sprite> _iconCache = new(StringComparer.OrdinalIgnoreCase);

        private RectTransform _selfRect;
        private AbilityDockItem _dragSource;
        private string _resolvedClassId;
        private bool _mounted;
        private IDockAbilityStateSource _stateSource;

        public string ClassId => string.IsNullOrWhiteSpace(classId)
            ? (string.IsNullOrWhiteSpace(_resolvedClassId) ? PlayerClassStateManager.ActiveClassId : _resolvedClassId)
            : classId.Trim();

        public void Mount(Transform parent)
        {
            EnsureContainer();
            if (_selfRect == null)
            {
                return;
            }
            var targetParent = mountContainer != null ? mountContainer : parent;
            if (targetParent != null && !_selfRect.IsChildOf(targetParent))
            {
                _selfRect.SetParent(targetParent, false);
            }
            gameObject.SetActive(true);
            _mounted = true;
            PlayerClassStateManager.ActiveClassChanged += OnActiveClassChanged;
            PlayerAbilityUnlockState.AbilityUnlocksChanged += OnAbilityUnlocksChanged;
            AttachStateSource();
            Rebind();
        }

        public void Unmount()
        {
            PlayerClassStateManager.ActiveClassChanged -= OnActiveClassChanged;
            PlayerAbilityUnlockState.AbilityUnlocksChanged -= OnAbilityUnlocksChanged;
            DetachStateSource();
            _mounted = false;
            gameObject.SetActive(false);
            _dragSource = null;
        }

        public void OnAbilityStateChanged(string abilityId, bool enabled)
        {
        }

        private void OnEnable()
        {
            if (rebuildOnEnable && _mounted)
            {
                AttachStateSource();
                Rebind();
            }
        }

        private void OnDisable()
        {
            if (!_mounted)
            {
                PlayerClassStateManager.ActiveClassChanged -= OnActiveClassChanged;
            }

            DetachStateSource();
        }

        internal void BeginDrag(AbilityDockItem item)
        {
            _dragSource = item;
        }

        internal void EndDrag(AbilityDockItem item)
        {
            if (_dragSource == item)
            {
                ResetDraggedItemPosition(item);
                _dragSource = null;
            }
            else
            {
                ResetDraggedItemPosition(item);
            }

            ApplyItemOrder();
        }

        internal void RequestSwap(AbilityDockItem source, AbilityDockItem target)
        {
            if (source == null || target == null)
            {
                return;
            }

            if (_dragSource != null && source != _dragSource)
            {
                return;
            }

            var sourceIndex = _items.IndexOf(source);
            var targetIndex = _items.IndexOf(target);
            if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
            {
                return;
            }

            (_items[sourceIndex], _items[targetIndex]) = (_items[targetIndex], _items[sourceIndex]);
            ApplyItemOrder();
            PersistCurrentLayout();
            _dragSource = null;
        }

        private void Rebind()
        {
            var activeClass = ResolveClassId();
            var entries = ClassAbilityCatalog.GetAbilityDockEntries(activeClass);
            RebuildItems(entries);
        }

        private string ResolveClassId()
        {
            if (!string.IsNullOrWhiteSpace(classId))
            {
                _resolvedClassId = classId.Trim();
                return _resolvedClassId;
            }

            _resolvedClassId = PlayerClassStateManager.ActiveClassId;
            return _resolvedClassId;
        }

        private void OnActiveClassChanged(string activeClass)
        {
            if (!_mounted)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(classId) &&
                !string.Equals(classId, activeClass, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Rebind();
        }

        private void OnAbilityUnlocksChanged()
        {
            if (!_mounted)
            {
                return;
            }

            RefreshUnlockStates();
        }

        private void EnsureContainer()
        {
            if (_selfRect == null)
            {
                _selfRect = GetComponent<RectTransform>();
                if (_selfRect == null)
                {
                    Debug.LogError("ClassAbilityDockModule requires a RectTransform component.", this);
                    return;
                }
            }

            if (itemContainer == null)
            {
                itemContainer = mountContainer != null ? mountContainer : _selfRect;
            }

            if (itemContainer != null && !itemContainer.TryGetComponent(out DockMagnifier _))
            {
                itemContainer.gameObject.AddComponent<DockMagnifier>();
            }
        }

        private void RebuildItems(IReadOnlyList<ClassAbilityCatalog.ClassAbilityDockEntry> entries)
        {
            ClearItems();

            var effectiveSlots = Mathf.Max(1, slotCount);
            var placeholderOrder = BuildPlaceholderIds(effectiveSlots);

            if (entries == null || entries.Count == 0)
            {
                CreatePlaceholders(placeholderOrder);
                ApplyItemOrder();
                PersistCurrentLayout();
                return;
            }

            if (itemPrefab == null)
            {
                Debug.LogWarning("ClassAbilityDockModule is missing an AbilityDockItem prefab.", this);
                return;
            }

            var lookup = new Dictionary<string, ClassAbilityCatalog.ClassAbilityDockEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                if (entry.IsValid && !lookup.ContainsKey(entry.AbilityId))
                {
                    lookup.Add(entry.AbilityId, entry);
                }
            }

            if (lookup.Count == 0)
            {
                CreatePlaceholders(placeholderOrder);
                ApplyItemOrder();
                PersistCurrentLayout();
                return;
            }

            var abilityOrder = lookup.Values
                .OrderBy(e => e.Level)
                .ThenBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(e => e.AbilityId)
                .ToList();

            var stored = AbilityDockLayoutStore.GetLayout(_resolvedClassId, placeholderOrder);
            var finalOrder = NormalizeLayout(stored, placeholderOrder, abilityOrder);

            foreach (var abilityId in finalOrder)
            {
                var item = CreateItem();
                if (item == null)
                {
                    continue;
                }

                if (IsPlaceholderId(abilityId))
                {
                    item.BindPlaceholder(abilityId);
                }
                else if (lookup.TryGetValue(abilityId, out var entry))
                {
                    item.Bind(entry, ResolveIcon(entry.AbilityId));
                    item.SetLocked(!PlayerAbilityUnlockState.IsAbilityUnlocked(_resolvedClassId, entry.AbilityId));
                    _abilityLookup[entry.AbilityId] = item;
                }
                else
                {
                    item.BindPlaceholder(abilityId);
                }

                _items.Add(item);
            }

            ApplyItemOrder();
            PersistCurrentLayout();
            RefreshAbilityStates();
            RefreshUnlockStates();
        }

        private AbilityDockItem CreateItem()
        {
            AbilityDockItem item;
            if (itemPrefab != null)
            {
                item = Instantiate(itemPrefab, itemContainer);
            }
            else
            {
                Debug.LogWarning("ClassAbilityDockModule is missing an AbilityDockItem prefab.", this);
                return null;
            }

            if (!item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
            }

            item.Initialize(this);
            return item;
        }

        private void ClearItems()
        {
            foreach (var item in _items)
            {
                if (item != null)
                {
                    var go = item.gameObject;
                    if (Application.isPlaying)
                    {
                        Destroy(go);
                    }
                    else
                    {
                        DestroyImmediate(go);
                    }
                }
            }

            _items.Clear();
            _abilityLookup.Clear();
            _dragSource = null;
        }

        private static List<string> NormalizeLayout(
            IReadOnlyList<string> stored,
            IReadOnlyList<string> placeholders,
            IReadOnlyList<string> abilityOrder)
        {
            var result = new List<string>();
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var placeholderSet = new HashSet<string>(placeholders ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var abilitySet = new HashSet<string>(abilityOrder ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            if (stored != null)
            {
                foreach (var entry in stored)
                {
                    if (string.IsNullOrWhiteSpace(entry))
                    {
                        continue;
                    }

                    if (placeholderSet.Contains(entry) || abilitySet.Contains(entry))
                    {
                        if (used.Add(entry))
                        {
                            result.Add(entry);
                        }
                    }
                }
            }

            if (placeholders != null)
            {
                foreach (var placeholder in placeholders)
                {
                    if (result.Count >= placeholders.Count)
                    {
                        break;
                    }

                    if (used.Add(placeholder))
                    {
                        result.Add(placeholder);
                    }
                }
            }

            if (abilityOrder != null)
            {
                foreach (var abilityId in abilityOrder)
                {
                    if (string.IsNullOrWhiteSpace(abilityId) || used.Contains(abilityId))
                    {
                        continue;
                    }

                    var placeholderIndex = result.FindIndex(IsPlaceholderId);
                    if (placeholderIndex < 0)
                    {
                        break;
                    }

                    used.Remove(result[placeholderIndex]);
                    result[placeholderIndex] = abilityId;
                    used.Add(abilityId);
                }
            }

            if (placeholders != null && result.Count > placeholders.Count)
            {
                result.RemoveRange(placeholders.Count, result.Count - placeholders.Count);
            }

            return result;
        }

        private void ApplyItemOrder()
        {
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null)
                {
                    continue;
                }

                item.transform.SetParent(itemContainer, false);
                item.transform.SetSiblingIndex(i);
                item.SetHotkeyLabel(GetHotkeyLabel(i));
            }
        }

        private void PersistCurrentLayout()
        {
            if (string.IsNullOrWhiteSpace(_resolvedClassId))
            {
                return;
            }

            var order = new List<string>();
            foreach (var item in _items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.LayoutId))
                {
                    continue;
                }

                order.Add(item.LayoutId.Trim());
            }

            AbilityDockLayoutStore.SaveLayout(_resolvedClassId, order);
        }

        private void AttachStateSource()
        {
            DetachStateSource();

            _stateSource = ResolveAbilityStateSource();
            if (_stateSource != null)
            {
                _stateSource.AbilityStateChanged += OnAbilityStateChangedInternal;
            }
        }

        private void DetachStateSource()
        {
            if (_stateSource != null)
            {
                _stateSource.AbilityStateChanged -= OnAbilityStateChangedInternal;
                _stateSource = null;
            }
        }

        private IDockAbilityStateSource ResolveAbilityStateSource()
        {
            if (abilityStateSource is IDockAbilityStateSource configured)
            {
                return configured;
            }

#if UNITY_2023_1_OR_NEWER
            var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
#endif
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IDockAbilityStateSource source)
                {
                    return source;
                }
            }

            return null;
        }

        private void RefreshAbilityStates()
        {
            if (_stateSource == null)
            {
                return;
            }

            foreach (var pair in _abilityLookup)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                if (_stateSource.TryGetState(pair.Key, out var state))
                {
                    pair.Value.SetAbilityState(state);
                }
            }
        }

        private void RefreshUnlockStates()
        {
            if (_abilityLookup.Count == 0)
            {
                return;
            }

            foreach (var pair in _abilityLookup)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                var unlocked = PlayerAbilityUnlockState.IsAbilityUnlocked(_resolvedClassId, pair.Key);
                pair.Value.SetLocked(!unlocked);
            }
        }

        private void OnAbilityStateChangedInternal(string abilityId, DockAbilityState state)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return;
            }

            if (_abilityLookup.TryGetValue(abilityId, out var item) && item != null)
            {
                item.SetAbilityState(state);
            }
        }

        private void ResetDraggedItemPosition(AbilityDockItem item)
        {
            if (item == null)
            {
                return;
            }

            item.transform.SetParent(itemContainer, false);
            var index = _items.IndexOf(item);
            if (index < 0)
            {
                index = itemContainer.childCount - 1;
            }

            item.transform.SetSiblingIndex(Mathf.Max(0, index));
        }

        private static List<string> BuildPlaceholderIds(int count)
        {
            var results = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                results.Add($"slot-{i + 1}");
            }

            return results;
        }

        private void CreatePlaceholders(IReadOnlyList<string> placeholderIds)
        {
            if (itemPrefab == null || placeholderIds == null)
            {
                return;
            }

            foreach (var placeholderId in placeholderIds)
            {
                var item = CreateItem();
                if (item == null)
                {
                    continue;
                }

                item.BindPlaceholder(placeholderId);
                _items.Add(item);
            }
        }

        private static bool IsPlaceholderId(string abilityId)
        {
            return !string.IsNullOrWhiteSpace(abilityId) &&
                   abilityId.StartsWith("slot-", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetHotkeyLabel(int index)
        {
            return index >= 0 && index < DefaultHotkeys.Length ? DefaultHotkeys[index] : string.Empty;
        }

        private Sprite ResolveIcon(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return fallbackIcon;
            }

            if (_iconCache.TryGetValue(abilityId, out var cached) && cached != null)
            {
                return cached;
            }

            Sprite sprite = null;
            if (AbilityRegistry.TryGetAbility(abilityId, out var ability) && ability != null && ability.Icon != null)
            {
                sprite = ability.Icon;
            }

            if (!string.IsNullOrWhiteSpace(iconResourceFolder))
            {
                var path = $"{iconResourceFolder.TrimEnd('/')}/{abilityId}";
                sprite ??= Resources.Load<Sprite>(path);
            }

            if (sprite == null)
            {
                sprite = fallbackIcon;
            }

            _iconCache[abilityId] = sprite;
            return sprite;
        }
    }
}
