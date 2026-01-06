using UnityEngine;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    public class DockShortcutId : MonoBehaviour
    {
        [SerializeField] private string shortcutId;

        public string ShortcutId => shortcutId;
    }
}
