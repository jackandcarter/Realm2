using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    public class DockShortcutFolder : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Folder Dock")]
        [SerializeField] private RectTransform subDockRoot;
        [SerializeField] private DockShortcutSection subSection;
        [SerializeField] private string layoutKeyPrefix = "dock-folder";

        [Header("Hover Behavior")]
        [SerializeField] private bool openOnHover = true;
        [SerializeField] private float hoverOpenDelay = 0.12f;
        [SerializeField] private float hoverCloseDelay = 0.18f;

        [Header("Click Behavior")]
        [SerializeField] private bool toggleOnClick = true;

        private Coroutine _hoverRoutine;
        private bool _isOpen;

        private void Awake()
        {
            ResolveSection();
            ApplyOpenState(_isOpen);
        }

        public void ConfigureLayoutKey(string shortcutId)
        {
            if (string.IsNullOrWhiteSpace(shortcutId) || subSection == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(layoutKeyPrefix))
            {
                layoutKeyPrefix = "dock-folder";
            }

            subSection.name = $"{layoutKeyPrefix}-{shortcutId}";
            var section = subSection.gameObject;
            if (section.TryGetComponent(out DockShortcutSection sectionComponent))
            {
                sectionComponent.SetLayoutKey($"{layoutKeyPrefix}-{shortcutId}");
            }
        }

        public bool HandleClick()
        {
            if (!toggleOnClick)
            {
                return false;
            }

            ToggleOpen();
            return true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!openOnHover)
            {
                return;
            }

            StartHoverRoutine(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!openOnHover)
            {
                return;
            }

            StartHoverRoutine(false);
        }

        public void OnDrop(PointerEventData eventData)
        {
            ResolveSection();
            if (subSection == null || eventData?.pointerDrag == null)
            {
                return;
            }

            var draggedItem = eventData.pointerDrag.GetComponent<DockShortcutItem>();
            if (draggedItem != null && draggedItem.Owner != null)
            {
                MoveItemToSubSection(draggedItem);
                return;
            }

            subSection.OnDrop(eventData);
        }

        public void ToggleOpen()
        {
            SetOpen(!_isOpen);
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;
            ApplyOpenState(open);
        }

        private void ApplyOpenState(bool open)
        {
            if (subDockRoot != null)
            {
                subDockRoot.gameObject.SetActive(open);
            }
        }

        private void ResolveSection()
        {
            if (subSection == null && subDockRoot != null)
            {
                subSection = subDockRoot.GetComponentInChildren<DockShortcutSection>(true);
            }
        }

        private void StartHoverRoutine(bool open)
        {
            if (_hoverRoutine != null)
            {
                StopCoroutine(_hoverRoutine);
            }

            _hoverRoutine = StartCoroutine(HoverRoutine(open));
        }

        private IEnumerator HoverRoutine(bool open)
        {
            var delay = Mathf.Max(0f, open ? hoverOpenDelay : hoverCloseDelay);
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            SetOpen(open);
        }

        private void MoveItemToSubSection(DockShortcutItem draggedItem)
        {
            ResolveSection();
            if (subSection == null || draggedItem == null)
            {
                return;
            }

            var sourceSection = draggedItem.Owner;
            if (sourceSection == null || sourceSection == subSection)
            {
                return;
            }

            if (!sourceSection.TryGetSource(draggedItem.ShortcutId, out var source))
            {
                return;
            }

            sourceSection.RemoveShortcut(draggedItem.ShortcutId);
            subSection.AddShortcutFromSource(source);
        }
    }
}
