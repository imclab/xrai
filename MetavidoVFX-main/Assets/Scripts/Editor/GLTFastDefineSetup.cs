// GLTFastDefineSetup.cs - Auto-add GLTFAST_AVAILABLE scripting define
// Part of Spec 009: Icosa & Sketchfab Integration
//
// DISABLED (2026-01-22): Consolidated into ScriptingDefineManager.cs
// [InitializeOnLoad] removed to prevent recompile storms from multiple scripts
// calling SetScriptingDefineSymbols on every domain reload.
// Manual menu items retained for debugging/verification.

using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.Linq;

namespace XRRAI.Editor
{
    /// <summary>
    /// Automatically adds GLTFAST_AVAILABLE scripting define when glTFast package is installed.
    /// NOTE: Auto-sync disabled - use ScriptingDefineManager instead.
    /// </summary>
    // [InitializeOnLoad] - DISABLED: Consolidated into ScriptingDefineManager.cs
    public static class GLTFastDefineSetup
    {
        private const string DEFINE_SYMBOL = "GLTFAST_AVAILABLE";
        private const string GLTFAST_TYPE = "GLTFast.GltfImport, glTFast";

        static GLTFastDefineSetup()
        {
            CheckAndSetDefine();
        }

        [MenuItem("H3M/Packages/Setup GLTFast Defines", priority = 500)]
        public static void SetupDefines()
        {
            CheckAndSetDefine();
            Debug.Log("[GLTFastDefineSetup] Checked GLTFast availability");
        }

        [MenuItem("H3M/Packages/Verify GLTFast Setup", priority = 501)]
        public static void VerifySetup()
        {
            bool hasPackage = IsGLTFastAvailable();
            bool hasDefine = HasDefineSymbol();

            var report = new System.Text.StringBuilder();
            report.AppendLine("=== GLTFast Setup Report ===\n");
            report.AppendLine($"GLTFast Package: {(hasPackage ? "INSTALLED" : "NOT FOUND")}");
            report.AppendLine($"GLTFAST_AVAILABLE Define: {(hasDefine ? "SET" : "NOT SET")}");

            if (hasPackage && hasDefine)
            {
                report.AppendLine("\n Status: READY for runtime glTF loading");
            }
            else if (hasPackage && !hasDefine)
            {
                report.AppendLine("\n Status: Package installed but define missing. Run Setup.");
            }
            else
            {
                report.AppendLine("\n Status: Add com.unity.cloud.gltfast to Packages/manifest.json");
            }

            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("GLTFast Setup", report.ToString(), "OK");
        }

        private static void CheckAndSetDefine()
        {
            bool hasPackage = IsGLTFastAvailable();
            bool hasDefine = HasDefineSymbol();

            if (hasPackage && !hasDefine)
            {
                AddDefineSymbol();
                Debug.Log("[GLTFastDefineSetup] Added GLTFAST_AVAILABLE define");
            }
            else if (!hasPackage && hasDefine)
            {
                RemoveDefineSymbol();
                Debug.Log("[GLTFastDefineSetup] Removed GLTFAST_AVAILABLE define (package not found)");
            }
        }

        private static bool IsGLTFastAvailable()
        {
            // Check if GLTFast type exists
            var type = System.Type.GetType(GLTFAST_TYPE);
            if (type != null) return true;

            // Fallback: check for any GLTFast assembly
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.Any(a => a.GetName().Name.Contains("glTFast") || a.GetName().Name.Contains("GLTFast"));
        }

        private static bool HasDefineSymbol()
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] defines);
            return defines.Contains(DEFINE_SYMBOL);
        }

        private static void AddDefineSymbol()
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] defines);

            if (!defines.Contains(DEFINE_SYMBOL))
            {
                var newDefines = defines.Append(DEFINE_SYMBOL).ToArray();
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newDefines);
            }
        }

        private static void RemoveDefineSymbol()
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] defines);

            if (defines.Contains(DEFINE_SYMBOL))
            {
                var newDefines = defines.Where(d => d != DEFINE_SYMBOL).ToArray();
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newDefines);
            }
        }
    }
}
