using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.DesignerTools
{
    [FilePath("ProjectSettings/DesignerToolkitProfile.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class DesignerToolkitProfile : ScriptableSingleton<DesignerToolkitProfile>
    {
        [SerializeField]
        private StatRegistry statRegistry;

        [SerializeField]
        private string statDefinitionsFolder = "Assets/ScriptableObjects/Stats";

        [SerializeField]
        private string statCategoriesFolder = "Assets/ScriptableObjects/Stats";

        [SerializeField]
        private string statProfilesFolder = "Assets/ScriptableObjects/Stats";

        [SerializeField]
        private string classDefinitionsFolder = "Assets/ScriptableObjects/Classes";

        [SerializeField]
        private string abilityDefinitionsFolder = "Assets/ScriptableObjects/Abilities";

        [SerializeField]
        private string weaponTypeDefinitionsFolder = "Assets/ScriptableObjects/Equipment/WeaponTypes";

        [SerializeField]
        private string armorTypeDefinitionsFolder = "Assets/ScriptableObjects/Equipment/ArmorTypes";

        [SerializeField]
        private string weaponDefinitionsFolder = "Assets/ScriptableObjects/Equipment/Weapons";

        [SerializeField]
        private string armorDefinitionsFolder = "Assets/ScriptableObjects/Equipment/Armors";

        [SerializeField]
        private string registryAssetPath = "Assets/ScriptableObjects/Stats/StatRegistry.asset";

        internal static DesignerToolkitProfile Instance => instance;

        internal StatRegistry StatRegistry
        {
            get => statRegistry;
            set => statRegistry = value;
        }

        internal string StatDefinitionsFolder
        {
            get => statDefinitionsFolder;
            set => statDefinitionsFolder = value;
        }

        internal string StatCategoriesFolder
        {
            get => statCategoriesFolder;
            set => statCategoriesFolder = value;
        }

        internal string StatProfilesFolder
        {
            get => statProfilesFolder;
            set => statProfilesFolder = value;
        }

        internal string ClassDefinitionsFolder
        {
            get => classDefinitionsFolder;
            set => classDefinitionsFolder = value;
        }

        internal string AbilityDefinitionsFolder
        {
            get => abilityDefinitionsFolder;
            set => abilityDefinitionsFolder = value;
        }

        internal string WeaponTypeDefinitionsFolder
        {
            get => weaponTypeDefinitionsFolder;
            set => weaponTypeDefinitionsFolder = value;
        }

        internal string ArmorTypeDefinitionsFolder
        {
            get => armorTypeDefinitionsFolder;
            set => armorTypeDefinitionsFolder = value;
        }

        internal string WeaponDefinitionsFolder
        {
            get => weaponDefinitionsFolder;
            set => weaponDefinitionsFolder = value;
        }

        internal string ArmorDefinitionsFolder
        {
            get => armorDefinitionsFolder;
            set => armorDefinitionsFolder = value;
        }

        internal string RegistryAssetPath
        {
            get => registryAssetPath;
            set => registryAssetPath = value;
        }

        internal void SaveProfile()
        {
            Save(true);
        }
    }
}
