using System.Collections.Generic;
using Client.CharacterCreation;
using Client.Player;
using TMPro;
using UnityEngine;

namespace Client.UI
{
    [DisallowMultipleComponent]
    public class ArkitectTerrainAbilityListPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform listRoot;
        [SerializeField] private GameObject entryTemplate;

        private readonly List<GameObject> _spawnedEntries = new();

        private void OnEnable()
        {
            ResolveReferences();
            RefreshList();
            PlayerAbilityUnlockState.AbilityUnlocksChanged += RefreshList;
        }

        private void OnDisable()
        {
            PlayerAbilityUnlockState.AbilityUnlocksChanged -= RefreshList;
        }

        private void ResolveReferences()
        {
            if (listRoot == null)
            {
                var found = transform.Find("AbilityList");
                if (found != null)
                {
                    listRoot = found.GetComponent<RectTransform>();
                }
            }

            if (entryTemplate == null && listRoot != null)
            {
                var template = listRoot.Find("AbilityEntryTemplate");
                if (template != null)
                {
                    entryTemplate = template.gameObject;
                }
            }
        }

        private void RefreshList()
        {
            if (listRoot == null || entryTemplate == null)
            {
                return;
            }

            foreach (var entry in _spawnedEntries)
            {
                if (entry != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(entry);
                    }
                    else
                    {
                        DestroyImmediate(entry);
                    }
                }
            }
            _spawnedEntries.Clear();

            var classId = ClassUnlockUtility.BuilderClassId;
            var abilities = ClassAbilityCatalog.GetAbilityDockEntries(classId);
            if (abilities == null || abilities.Count == 0)
            {
                return;
            }

            foreach (var ability in abilities)
            {
                var instance = Instantiate(entryTemplate, listRoot);
                instance.name = $"AbilityEntry_{ability.DisplayName}";
                instance.SetActive(true);

                var displayName = string.IsNullOrWhiteSpace(ability.DisplayName) ? "Ability" : ability.DisplayName;
                var description = string.IsNullOrWhiteSpace(ability.Description) ? ability.Tooltip : ability.Description;
                var unlockText = ability.Level > 0 ? $"Unlock: Level {ability.Level}" : "Unlock: Available";
                var unlocked = PlayerAbilityUnlockState.IsAbilityUnlocked(classId, ability.AbilityId);

                ApplyText(instance.transform, "AbilityName", displayName);
                ApplyText(instance.transform, "AbilityDescription", description);
                ApplyText(instance.transform, "AbilityUnlock", unlocked ? "Unlocked" : unlockText);

                if (instance.TryGetComponent(out CanvasGroup canvasGroup))
                {
                    canvasGroup.alpha = unlocked ? 1f : 0.65f;
                }
                else if (!unlocked)
                {
                    var group = instance.AddComponent<CanvasGroup>();
                    group.alpha = 0.65f;
                }

                _spawnedEntries.Add(instance);
            }
        }

        private static void ApplyText(Transform root, string name, string value)
        {
            if (root == null)
            {
                return;
            }

            var child = root.Find(name);
            if (child == null || !child.TryGetComponent(out TMP_Text text))
            {
                return;
            }

            text.text = value;
        }
    }
}
