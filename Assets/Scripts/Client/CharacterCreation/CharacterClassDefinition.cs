using System;
using UnityEngine;

namespace Client.CharacterCreation
{
    [Serializable]
    public class CharacterClassDefinition
    {
        public string Id;
        public string DisplayName;
        public string RoleSummary;
        public string Description;
        public Color CrystalColor = Color.white;
        public string CrystalSymbol;
    }
}
