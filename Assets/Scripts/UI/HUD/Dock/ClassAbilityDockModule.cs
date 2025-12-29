using System;
using System.Collections.Generic;
using System.Linq;
using Client.CharacterCreation;
using Client.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ClassAbilityDockModule : MonoBehaviour, IClassUiModule
    {
        [Header("Class")]
        [SerializeField] private string classId;

        [Header("UI")]
        [SerializeField] private RectTransform itemContainer;
        [SerializeField] private AbilityDockItem itemPrefab;
        [SerializeField] private Sprite fallbackIcon;
        [SerializeField] private string iconResourceFolder = "UI/AbilityIcons";

        [Header("Behaviour")]
        [SerializeField] private bool rebuildOnEnable = true;

        private readonly List<AbilityDockItem> _items = new();
        private readonly Dictionary<string, Sprite> _iconCache = new(StringComparer.OrdinalIgnoreCase);

        private RectTransform _selfRect;
        private AbilityDockItem _dragSource;
        private string _resolvedClassId;
        private bool _mounted;

        public string ClassId => string.IsNullOrWhiteSpace(classId) ? _resolvedClassId : classId.Trim();

        public void Mount(Transform parent)
        {
            EnsureContainer();
            if (_selfRect == null)
            {
                return;
            }
            _selfRect.SetParent(parent, false);
            gameObject.SetActive(true);
            _mounted = true;
            PlayerClassStateManager.ActiveClassChanged += OnActiveClassChanged;
            Rebind();
        }

        public void Unmount()
        {
            PlayerClassStateManager.ActiveClassChanged -= OnActiveClassChanged;
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
                Rebind();
            }
        }

        private void OnDisable()
        {
            if (!_mounted)
            {
                PlayerClassStateManager.ActiveClassChanged -= OnActiveClassChanged;
            }
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
                itemContainer = _selfRect;
            }
        }

        private void RebuildItems(IReadOnlyList<ClassAbilityCatalog.ClassAbilityDockEntry> entries)
        {
            ClearItems();

            if (entries == null || entries.Count == 0)
            {
                AbilityDockLayoutStore.SaveLayout(_resolvedClassId, Array.Empty<string>());
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
                AbilityDockLayoutStore.SaveLayout(_resolvedClassId, Array.Empty<string>());
                return;
            }

            var sorted = lookup.Values
                .OrderBy(e => e.Level)
                .ThenBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(e => e.AbilityId)
                .ToList();

            var stored = AbilityDockLayoutStore.GetLayout(_resolvedClassId, sorted);
            var finalOrder = MergeOrders(stored, sorted, lookup.Keys);

            foreach (var abilityId in finalOrder)
            {
                if (!lookup.TryGetValue(abilityId, out var entry))
                {
                    continue;
                }

                var item = CreateItem();
                if (item == null)
                {
                    continue;
                }
                item.Bind(entry, ResolveIcon(entry.AbilityId));
                _items.Add(item);
            }

            ApplyItemOrder();
            PersistCurrentLayout();
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
            _dragSource = null;
        }

        private List<string> MergeOrders(IReadOnlyList<string> stored, IReadOnlyList<string> sorted, ICollection<string> validIds)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (stored != null)
            {
                foreach (var abilityId in stored)
                {
                    if (string.IsNullOrWhiteSpace(abilityId))
                    {
                        continue;
                    }

                    if (!validIds.Contains(abilityId) || !seen.Add(abilityId))
                    {
                        continue;
                    }

                    result.Add(abilityId);
                }
            }

            if (sorted != null)
            {
                foreach (var abilityId in sorted)
                {
                    if (string.IsNullOrWhiteSpace(abilityId))
                    {
                        continue;
                    }

                    if (!validIds.Contains(abilityId) || !seen.Add(abilityId))
                    {
                        continue;
                    }

                    result.Add(abilityId);
                }
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
                if (item == null || string.IsNullOrWhiteSpace(item.AbilityId))
                {
                    continue;
                }

                order.Add(item.AbilityId.Trim());
            }

            AbilityDockLayoutStore.SaveLayout(_resolvedClassId, order);
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
            if (!string.IsNullOrWhiteSpace(iconResourceFolder))
            {
                var path = $"{iconResourceFolder.TrimEnd('/')}/{abilityId}";
                sprite = Resources.Load<Sprite>(path);
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
