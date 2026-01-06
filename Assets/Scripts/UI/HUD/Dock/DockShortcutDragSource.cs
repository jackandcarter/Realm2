using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class DockShortcutDragSource : MonoBehaviour, IDockShortcutSource, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        [SerializeField] private string shortcutId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private DockShortcutActionMetadata actionMetadata;
        [SerializeField] private UnityEvent onActivate = new();
        [SerializeField] private CanvasGroup canvasGroup;

        private bool _dragging;
        private bool _entryInitialized;
        private DockShortcutEntry _entry;

        public DockShortcutEntry ShortcutEntry
        {
            get
            {
                EnsureEntryInitialized();
                return _entry;
            }
        }

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public void Configure(DockShortcutEntry entry, UnityAction activationHandler)
        {
            _entry = entry;
            _entryInitialized = true;
            shortcutId = entry.ShortcutId;
            displayName = entry.DisplayName;
            icon = entry.Icon;
            actionMetadata = entry.ActionMetadata;

            if (onActivate == null)
            {
                onActivate = new UnityEvent();
            }

            onActivate.RemoveAllListeners();
            if (activationHandler != null)
            {
                onActivate.AddListener(activationHandler);
            }
        }

        public void ActivateDockShortcut()
        {
            onActivate?.Invoke();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (canvasGroup == null)
            {
                return;
            }

            _dragging = true;
            canvasGroup.alpha = 0.7f;
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging)
            {
                return;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging || canvasGroup == null)
            {
                return;
            }

            _dragging = false;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        private void EnsureEntryInitialized()
        {
            if (_entryInitialized)
            {
                return;
            }

            _entry = new DockShortcutEntry(shortcutId, displayName, icon, actionMetadata);
            _entryInitialized = true;
        }
    }
}
