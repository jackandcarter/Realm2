using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    public class DockShortcutSection : MonoBehaviour, IDropHandler
    {
        [SerializeField] private DockShortcutItem itemPrefab;

        private readonly List<string> _order = new();
        private readonly List<DockShortcutItem> _items = new();
        private readonly Dictionary<string, DockShortcutItem> _itemLookup =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IDockShortcutSource> _sourceLookup =
            new(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            EnsureLayout();
        }

        private void OnEnable()
        {
            Rebuild();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            var dragObject = eventData.pointerDrag;
            if (dragObject == null)
            {
                return;
            }

            var draggedItem = dragObject.GetComponent<DockShortcutItem>();
            if (draggedItem != null && draggedItem.Owner == this)
            {
                MoveToEnd(draggedItem);
                return;
            }

            var source = ResolveSource(dragObject);
            if (source != null)
            {
                AddShortcut(source);
            }
        }

        internal bool IsDropTarget(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            return target.GetComponentInParent<DockShortcutSection>() == this;
        }

        internal void ActivateShortcut(string shortcutId)
        {
            if (string.IsNullOrWhiteSpace(shortcutId))
            {
                return;
            }

            if (_sourceLookup.TryGetValue(shortcutId, out var source))
            {
                source.ActivateDockShortcut();
            }
        }

        internal void RequestSwap(DockShortcutItem source, DockShortcutItem target)
        {
            if (source == null || target == null)
            {
                return;
            }

            var sourceId = source.ShortcutId;
            var targetId = target.ShortcutId;
            if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(targetId))
            {
                return;
            }

            var sourceIndex = _order.FindIndex(id => string.Equals(id, sourceId, StringComparison.OrdinalIgnoreCase));
            var targetIndex = _order.FindIndex(id => string.Equals(id, targetId, StringComparison.OrdinalIgnoreCase));
            if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
            {
                return;
            }

            (_order[sourceIndex], _order[targetIndex]) = (_order[targetIndex], _order[sourceIndex]);
            source.transform.SetSiblingIndex(targetIndex);
            target.transform.SetSiblingIndex(sourceIndex);
            PersistLayout();
        }

        internal void RemoveShortcut(string shortcutId)
        {
            if (string.IsNullOrWhiteSpace(shortcutId))
            {
                return;
            }

            if (_itemLookup.TryGetValue(shortcutId, out var item))
            {
                _items.Remove(item);
                _itemLookup.Remove(shortcutId);
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            _order.RemoveAll(id => string.Equals(id, shortcutId, StringComparison.OrdinalIgnoreCase));
            PersistLayout();
        }

        internal void AddShortcutFromSource(IDockShortcutSource source)
        {
            AddShortcut(source);
        }

        private void AddShortcut(IDockShortcutSource source)
        {
            if (source == null)
            {
                return;
            }

            var entry = source.ShortcutEntry;
            if (string.IsNullOrWhiteSpace(entry.ShortcutId))
            {
                return;
            }

            var shortcutId = entry.ShortcutId.Trim();
            if (_itemLookup.ContainsKey(shortcutId))
            {
                return;
            }

            _sourceLookup[shortcutId] = source;
            _order.Add(shortcutId);

            if (itemPrefab != null)
            {
                var item = Instantiate(itemPrefab, transform);
                item.Initialize(this);
                item.Bind(entry);
                _items.Add(item);
                _itemLookup[shortcutId] = item;
            }
            else
            {
                Debug.LogWarning("DockShortcutSection is missing a DockShortcutItem prefab.", this);
            }

            PersistLayout();
        }

        private void MoveToEnd(DockShortcutItem item)
        {
            if (item == null)
            {
                return;
            }

            var shortcutId = item.ShortcutId;
            if (string.IsNullOrWhiteSpace(shortcutId))
            {
                return;
            }

            var index = _order.FindIndex(id => string.Equals(id, shortcutId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return;
            }

            _order.RemoveAt(index);
            _order.Add(shortcutId);
            item.transform.SetSiblingIndex(_order.Count - 1);
            PersistLayout();
        }

        private void Rebuild()
        {
            RefreshSources();
            _order.Clear();

            if (_sourceLookup.Count == 0)
            {
                ClearItems();
                return;
            }

            var defaultOrder = new List<string>(_sourceLookup.Keys);
            var storedOrder = DockShortcutLayoutStore.GetLayout(defaultOrder);
            var finalOrder = MergeOrders(storedOrder, defaultOrder, _sourceLookup.Keys);
            _order.AddRange(finalOrder);

            RebuildItems();
        }

        private void RefreshSources()
        {
            _sourceLookup.Clear();

#if UNITY_2023_1_OR_NEWER
            var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
#endif
            foreach (var behaviour in behaviours)
            {
                if (behaviour is not IDockShortcutSource source)
                {
                    continue;
                }

                var entry = source.ShortcutEntry;
                if (string.IsNullOrWhiteSpace(entry.ShortcutId))
                {
                    continue;
                }

                var shortcutId = entry.ShortcutId.Trim();
                if (!_sourceLookup.ContainsKey(shortcutId))
                {
                    _sourceLookup.Add(shortcutId, source);
                }
            }
        }

        private void RebuildItems()
        {
            ClearItems();

            if (itemPrefab == null)
            {
                Debug.LogWarning("DockShortcutSection is missing a DockShortcutItem prefab.", this);
                return;
            }

            foreach (var shortcutId in _order)
            {
                if (!_sourceLookup.TryGetValue(shortcutId, out var source))
                {
                    continue;
                }

                var item = Instantiate(itemPrefab, transform);
                item.Initialize(this);
                item.Bind(source.ShortcutEntry);
                _items.Add(item);
                _itemLookup[shortcutId] = item;
            }
        }

        private void ClearItems()
        {
            foreach (var item in _items)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            _items.Clear();
            _itemLookup.Clear();
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

        private void PersistLayout()
        {
            DockShortcutLayoutStore.SaveLayout(_order);
        }

        private void EnsureLayout()
        {
            if (!TryGetComponent(out HorizontalLayoutGroup layout))
            {
                layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 8f;
        }

        private static IDockShortcutSource ResolveSource(GameObject dragObject)
        {
            if (dragObject == null)
            {
                return null;
            }

            var components = dragObject.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component is IDockShortcutSource source)
                {
                    return source;
                }
            }

            return null;
        }
    }
}
