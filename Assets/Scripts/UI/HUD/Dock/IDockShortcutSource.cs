namespace Client.UI.HUD.Dock
{
    public interface IDockShortcutSource
    {
        DockShortcutEntry ShortcutEntry { get; }
        void ActivateDockShortcut();
    }
}
