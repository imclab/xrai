// IcosaDefineSetup.cs - Auto-add ICOSA_AVAILABLE scripting define
// Part of Spec 009: Icosa & Sketchfab 3D Model Integration
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
    /// Automatically adds ICOSA_AVAILABLE scripting define when Icosa Toolkit is installed.
    /// NOTE: Auto-sync disabled - use ScriptingDefineManager instead.
    /// </summary>
    // [InitializeOnLoad] - DISABLED: Consolidated into ScriptingDefineManager.cs
    public static class IcosaDefineSetup
    {
        private const string DEFINE_SYMBOL = "ICOSA_AVAILABLE";
        private const string ICOSA_TYPE = "Icosa.IcosaAsset, Icosa.Core";

        static IcosaDefineSetup()
        {
            CheckAndSetDefine();
        }

        [MenuItem("H3M/Packages/Setup Icosa Defines", priority = 504)]
        public static void SetupDefines()
        {
            CheckAndSetDefine();
            Debug.Log("[IcosaDefineSetup] Checked Icosa Toolkit availability");
        }

        [MenuItem("H3M/Packages/Verify Icosa Setup", priority = 505)]
        public static void VerifySetup()
        {
            bool hasPackage = IsIcosaAvailable();
            bool hasDefine = HasDefineSymbol();

            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Icosa Toolkit Setup Report ===\n");
            report.AppendLine($"Icosa Toolkit Package: {(hasPackage ? "INSTALLED" : "NOT FOUND")}");
            report.AppendLine($"ICOSA_AVAILABLE Define: {(hasDefine ? "SET" : "NOT SET")}");

            if (hasPackage && hasDefine)
            {
                report.AppendLine("\nStatus: READY for Icosa Gallery integration");
            }
            else if (!hasPackage)
            {
                report.AppendLine("\nStatus: Add to Packages/manifest.json:");
                report.AppendLine("  \"org.icosa.toolkit\": \"https://github.com/icosa-foundation/icosa-toolkit-unity.git\"");
            }

            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("Icosa Setup", report.ToString(), "OK");
        }

        private static void CheckAndSetDefine()
        {
            bool hasPackage = IsIcosaAvailable();
            bool hasDefine = HasDefineSymbol();

            if (hasPackage && !hasDefine)
            {
                AddDefineSymbol();
                Debug.Log("[IcosaDefineSetup] Added ICOSA_AVAILABLE define");
            }
            else if (!hasPackage && hasDefine)
            {
                RemoveDefineSymbol();
                Debug.Log("[IcosaDefineSetup] Removed ICOSA_AVAILABLE define (package not found)");
            }
        }

        private static bool IsIcosaAvailable()
        {
            // Check if Icosa type exists
            var type = System.Type.GetType(ICOSA_TYPE);
            if (type != null) return true;

            // Fallback: check for Icosa assemblies
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.Any(a =>
                a.GetName().Name.Contains("Icosa") ||
                a.GetName().Name.Contains("icosa"));
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
