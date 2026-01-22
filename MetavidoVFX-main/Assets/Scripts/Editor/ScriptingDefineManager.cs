// ScriptingDefineManager.cs - Unified scripting define management
// Consolidates: BodyPixDefineSetup, HoloKitDefineSetup, EnableUnsafeCode
// Auto-detects packages and manages defines on domain reload

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace XRRAI.Editor
{
    /// <summary>
    /// Manages scripting defines based on installed packages.
    /// Runs automatically on domain reload.
    /// Menu: H3M > Setup > Scripting Defines
    /// </summary>
    [InitializeOnLoad]
    public static class ScriptingDefineManager
    {
        #region Package-to-Define Mappings

        /// <summary>
        /// Maps package names to scripting defines they enable.
        /// </summary>
        private static readonly Dictionary<string, string[]> PackageDefines = new()
        {
            // HoloKit hand tracking
            { "io.holokit.unity-sdk", new[] { "HOLOKIT_AVAILABLE" } },

            // XR Hands (Unity's hand tracking subsystem)
            { "com.unity.xr.hands", new[] { "UNITY_XR_HANDS" } },

            // BodyPix 24-part body segmentation
            { "jp.keijiro.bodypix", new[] { "BODYPIX_AVAILABLE" } },

            // Unity WebRTC (note: conflicts with WebRtcVideoChat plugin)
            { "com.unity.webrtc", new[] { "UNITY_WEBRTC_AVAILABLE" } },

            // Icosa 3D model API (requires unsafe code)
            { "com.icosa.icosa-api-client-unity", new[] { "ICOSA_AVAILABLE" } },
        };

        /// <summary>
        /// Packages that require unsafe code compilation.
        /// </summary>
        private static readonly string[] UnsafeCodePackages = new[]
        {
            "com.icosa.icosa-api-client-unity"
        };

        /// <summary>
        /// Debug defines (not auto-enabled, use menu).
        /// </summary>
        private static readonly string[] DebugDefines = new[]
        {
            "DEBUG_TRACKING",
            "DEBUG_VOICE",
            "DEBUG_VFX",
            "DEBUG_NETWORK",
            "DEBUG_SYSTEM"
        };

        #endregion

        #region Initialization

        static ScriptingDefineManager()
        {
            EditorApplication.delayCall += SyncAllDefines;
        }

        #endregion

        #region Core Logic

        /// <summary>
        /// Syncs all scripting defines based on installed packages.
        /// </summary>
        public static void SyncAllDefines()
        {
            var namedTarget = GetCurrentNamedBuildTarget();
            var currentDefines = GetDefines(namedTarget);
            var newDefines = new HashSet<string>(currentDefines);
            bool changed = false;

            // Check each package mapping
            foreach (var kvp in PackageDefines)
            {
                bool packageInstalled = IsPackageInstalled(kvp.Key);

                foreach (var define in kvp.Value)
                {
                    bool hasDefine = currentDefines.Contains(define);

                    if (packageInstalled && !hasDefine)
                    {
                        newDefines.Add(define);
                        changed = true;
                        Debug.Log($"[ScriptingDefineManager] Added {define} (package: {kvp.Key})");
                    }
                    else if (!packageInstalled && hasDefine)
                    {
                        newDefines.Remove(define);
                        changed = true;
                        Debug.Log($"[ScriptingDefineManager] Removed {define} (package not found: {kvp.Key})");
                    }
                }
            }

            // Auto-enable unsafe code if required packages present
            foreach (var package in UnsafeCodePackages)
            {
                if (IsPackageInstalled(package) && !PlayerSettings.allowUnsafeCode)
                {
                    PlayerSettings.allowUnsafeCode = true;
                    Debug.Log($"[ScriptingDefineManager] Enabled unsafe code (required by {package})");
                }
            }

            if (changed)
            {
                SetDefines(namedTarget, newDefines);
            }
        }

        #endregion

        #region Menu Items

        [MenuItem("H3M/Setup/Scripting Defines/Sync All Defines", priority = 100)]
        public static void MenuSyncDefines()
        {
            SyncAllDefines();
            ShowStatus();
        }

        [MenuItem("H3M/Setup/Scripting Defines/Show Status", priority = 101)]
        public static void ShowStatus()
        {
            var namedTarget = GetCurrentNamedBuildTarget();
            var currentDefines = GetDefines(namedTarget);

            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("   Scripting Defines Status");
            Debug.Log("═══════════════════════════════════════════════════════════");

            Debug.Log("\n  Package Defines:");
            foreach (var kvp in PackageDefines)
            {
                bool installed = IsPackageInstalled(kvp.Key);
                foreach (var define in kvp.Value)
                {
                    bool active = currentDefines.Contains(define);
                    Debug.Log($"    {(active ? "✓" : "✗")} {define} ({(installed ? "installed" : "not installed")})");
                }
            }

            Debug.Log("\n  Debug Defines:");
            foreach (var define in DebugDefines)
            {
                bool active = currentDefines.Contains(define);
                Debug.Log($"    {(active ? "✓" : "✗")} {define}");
            }

            Debug.Log($"\n  Unsafe Code: {(PlayerSettings.allowUnsafeCode ? "✓ ENABLED" : "✗ DISABLED")}");
            Debug.Log("═══════════════════════════════════════════════════════════");
        }

        [MenuItem("H3M/Setup/Scripting Defines/Enable Debug Defines", priority = 110)]
        public static void EnableDebugDefines()
        {
            var namedTarget = GetCurrentNamedBuildTarget();
            var currentDefines = GetDefines(namedTarget);
            var newDefines = new HashSet<string>(currentDefines);

            foreach (var define in DebugDefines)
            {
                newDefines.Add(define);
            }

            SetDefines(namedTarget, newDefines);
            Debug.Log($"[ScriptingDefineManager] Enabled debug defines: {string.Join(", ", DebugDefines)}");
        }

        [MenuItem("H3M/Setup/Scripting Defines/Disable Debug Defines", priority = 111)]
        public static void DisableDebugDefines()
        {
            var namedTarget = GetCurrentNamedBuildTarget();
            var currentDefines = GetDefines(namedTarget);
            var newDefines = new HashSet<string>(currentDefines);

            foreach (var define in DebugDefines)
            {
                newDefines.Remove(define);
            }

            SetDefines(namedTarget, newDefines);
            Debug.Log("[ScriptingDefineManager] Disabled debug defines");
        }

        [MenuItem("H3M/Setup/Scripting Defines/Enable Unsafe Code", priority = 120)]
        public static void EnableUnsafeCode()
        {
            PlayerSettings.allowUnsafeCode = true;
            Debug.Log("[ScriptingDefineManager] Unsafe code enabled");
        }

        #endregion

        #region Helpers

        private static bool IsPackageInstalled(string packageName)
        {
            string manifestPath = "Packages/manifest.json";
            if (!File.Exists(manifestPath)) return false;

            string manifest = File.ReadAllText(manifestPath);
            return manifest.Contains($"\"{packageName}\"");
        }

        private static HashSet<string> GetDefines(NamedBuildTarget target)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbols(target);
            return new HashSet<string>(defines.Split(';').Where(d => !string.IsNullOrEmpty(d)));
        }

        private static void SetDefines(NamedBuildTarget target, HashSet<string> defines)
        {
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
        }

        private static NamedBuildTarget GetCurrentNamedBuildTarget()
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

            return targetGroup switch
            {
                BuildTargetGroup.Standalone => NamedBuildTarget.Standalone,
                BuildTargetGroup.iOS => NamedBuildTarget.iOS,
                BuildTargetGroup.Android => NamedBuildTarget.Android,
                BuildTargetGroup.WebGL => NamedBuildTarget.WebGL,
                BuildTargetGroup.VisionOS => NamedBuildTarget.VisionOS,
                _ => NamedBuildTarget.FromBuildTargetGroup(targetGroup)
            };
        }

        #endregion
    }
}
#endif
