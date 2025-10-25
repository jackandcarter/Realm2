using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_2023_1_OR_NEWER
using UnityEditor.Build;
#endif

namespace Digger.Modules.Core.Editor
{
    [InitializeOnLoad]
    public class DiggerDefines
    {
        private const string DiggerDefine = "__DIGGER__";

        static DiggerDefines()
        {
            InitDefine(DiggerDefine);
        }

        public static void InitDefine(string def)
        {
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
#if UNITY_2023_1_OR_NEWER
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(target);
            var defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
#else
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
#endif
            if (defines.Contains(def))
                return;

            if (string.IsNullOrEmpty(defines)) {
#if UNITY_2023_1_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, def);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, def);
#endif
            }
            else {
                if (!defines[defines.Length - 1].Equals(';')) {
                    defines += ';';
                }

                defines += def;
#if UNITY_2023_1_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
#endif
            }
        }

        [PostProcessScene(0)]
        public static void OnPostprocessScene()
        {
            InitDefine(DiggerDefine);
        }
    }
}