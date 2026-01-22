// Auto-setup HOLOKIT_AVAILABLE scripting define when HoloKit package is present
// Run: H3M > HoloKit > Setup HoloKit Defines

using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MetavidoVFX.Editor
{
    [InitializeOnLoad]
    public static class HoloKitDefineSetup
    {
        private const string HOLOKIT_DEFINE = "HOLOKIT_AVAILABLE";
        private const string HOLOKIT_PACKAGE = "io.holokit.unity-sdk";
        private const string XR_HANDS_DEFINE = "UNITY_XR_HANDS";
        private const string XR_HANDS_PACKAGE = "com.unity.xr.hands";

        static HoloKitDefineSetup()
        {
            // Auto-setup on domain reload
            EditorApplication.delayCall += AutoSetupDefines;
        }

        static void AutoSetupDefines()
        {
            bool holoKitInstalled = IsPackageInstalled(HOLOKIT_PACKAGE);
            bool xrHandsInstalled = IsPackageInstalled(XR_HANDS_PACKAGE);

            bool holoKitDefined = HasDefine(HOLOKIT_DEFINE);
            bool xrHandsDefined = HasDefine(XR_HANDS_DEFINE);

            bool needsUpdate = false;

            if (holoKitInstalled && !holoKitDefined)
            {
                AddDefine(HOLOKIT_DEFINE);
                needsUpdate = true;
                Debug.Log("[HoloKit Setup] Added HOLOKIT_AVAILABLE define");
            }
            else if (!holoKitInstalled && holoKitDefined)
            {
                RemoveDefine(HOLOKIT_DEFINE);
                needsUpdate = true;
                Debug.Log("[HoloKit Setup] Removed HOLOKIT_AVAILABLE define (package not found)");
            }

            if (xrHandsInstalled && !xrHandsDefined)
            {
                AddDefine(XR_HANDS_DEFINE);
                needsUpdate = true;
                Debug.Log("[HoloKit Setup] Added UNITY_XR_HANDS define");
            }

            if (needsUpdate)
            {
                Debug.Log("[HoloKit Setup] Scripting defines updated - scripts will recompile");
            }
        }

        static bool IsPackageInstalled(string packageName)
        {
            string manifestPath = "Packages/manifest.json";
            if (!System.IO.File.Exists(manifestPath)) return false;

            string manifest = System.IO.File.ReadAllText(manifestPath);
            return manifest.Contains($"\"{packageName}\"");
        }

        static bool HasDefine(string define)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            return defines.Split(';').Contains(define);
        }

        static void AddDefine(string define)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

            if (!defines.Split(';').Contains(define))
            {
                defines = string.IsNullOrEmpty(defines) ? define : defines + ";" + define;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
            }
        }

        static void RemoveDefine(string define)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

            var definesList = defines.Split(';').Where(d => d != define);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", definesList));
        }

        [MenuItem("H3M/HoloKit/Setup HoloKit Defines")]
        public static void ManualSetup()
        {
            AutoSetupDefines();

            bool holoKitDefined = HasDefine(HOLOKIT_DEFINE);
            bool xrHandsDefined = HasDefine(XR_HANDS_DEFINE);

            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("   HoloKit Define Status");
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log($"  HOLOKIT_AVAILABLE: {(holoKitDefined ? "✓ Defined" : "✗ Not defined")}");
            Debug.Log($"  UNITY_XR_HANDS: {(xrHandsDefined ? "✓ Defined" : "✗ Not defined")}");
            Debug.Log("═══════════════════════════════════════════════════════════");

            if (!holoKitDefined || !xrHandsDefined)
            {
                Debug.LogWarning("[HoloKit Setup] Some defines missing - check package installation");
            }
        }

        [MenuItem("H3M/HoloKit/Force Enable HoloKit")]
        public static void ForceEnableHoloKit()
        {
            AddDefine(HOLOKIT_DEFINE);
            AddDefine(XR_HANDS_DEFINE);
            Debug.Log("[HoloKit Setup] Force enabled HOLOKIT_AVAILABLE and UNITY_XR_HANDS");
        }
    }
}
