using UnityEngine;

namespace Client.UI.HUD
{
    public interface IClassUiModule
    {
        string ClassId { get; }

        void Mount(Transform parent);

        void Unmount();

        void OnAbilityStateChanged(string abilityId, bool enabled);
    }
}
