// MediaPipeDefineSetup.cs - Auto-add MEDIAPIPE_AVAILABLE scripting define
// Part of Spec 008: Cross-Platform Multimodal ML Foundations
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
    /// Automatically adds MEDIAPIPE_AVAILABLE scripting define when MediaPipe package is installed.
    /// NOTE: Auto-sync disabled - use ScriptingDefineManager instead.
    /// </summary>
    // [InitializeOnLoad] - DISABLED: Consolidated into ScriptingDefineManager.cs
    public static class MediaPipeDefineSetup
    {
        private const string DEFINE_SYMBOL = "MEDIAPIPE_AVAILABLE";
        private const string MEDIAPIPE_TYPE = "Mediapipe.Unity.ImageSource, MediaPipe.Runtime";

        static MediaPipeDefineSetup()
        {
            CheckAndSetDefine();
        }

        [MenuItem("H3M/Packages/Setup MediaPipe Defines", priority = 502)]
        public static void SetupDefines()
        {
            CheckAndSetDefine();
            Debug.Log("[MediaPipeDefineSetup] Checked MediaPipe availability");
        }

        [MenuItem("H3M/Packages/Verify MediaPipe Setup", priority = 503)]
        public static void VerifySetup()
        {
            bool hasPackage = IsMediaPipeAvailable();
            bool hasDefine = HasDefineSymbol();

            var report = new System.Text.StringBuilder();
            report.AppendLine("=== MediaPipe Setup Report ===\n");
            report.AppendLine($"MediaPipe Package: {(hasPackage ? "INSTALLED" : "NOT FOUND")}");
            report.AppendLine($"MEDIAPIPE_AVAILABLE Define: {(hasDefine ? "SET" : "NOT SET")}");

            if (hasPackage && hasDefine)
            {
                report.AppendLine("\nStatus: READY for MediaPipe tracking");
            }
            else if (!hasPackage)
            {
                report.AppendLine("\nStatus: Install MediaPipe Unity Plugin:");
                report.AppendLine("  1. Clone github.com/homuler/MediaPipeUnityPlugin");
                report.AppendLine("  2. Follow setup instructions for your platform");
                report.AppendLine("  3. Run this setup again");
            }

            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("MediaPipe Setup", report.ToString(), "OK");
        }

        private static void CheckAndSetDefine()
        {
            bool hasPackage = IsMediaPipeAvailable();
            bool hasDefine = HasDefineSymbol();

            if (hasPackage && !hasDefine)
            {
                AddDefineSymbol();
                Debug.Log("[MediaPipeDefineSetup] Added MEDIAPIPE_AVAILABLE define");
            }
            else if (!hasPackage && hasDefine)
            {
                RemoveDefineSymbol();
                Debug.Log("[MediaPipeDefineSetup] Removed MEDIAPIPE_AVAILABLE define (package not found)");
            }
        }

        private static bool IsMediaPipeAvailable()
        {
            // Check if MediaPipe type exists
            var type = System.Type.GetType(MEDIAPIPE_TYPE);
            if (type != null) return true;

            // Fallback: check for MediaPipe assemblies
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.Any(a =>
                a.GetName().Name.Contains("MediaPipe") ||
                a.GetName().Name.Contains("Mediapipe"));
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
