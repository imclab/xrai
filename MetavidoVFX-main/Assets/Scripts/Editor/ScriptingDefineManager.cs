// ScriptingDefineManager.cs - Unified scripting define management
// Consolidates ALL define setup scripts into single source of truth:
// - BodyPixDefineSetup, HoloKitDefineSetup, EnableUnsafeCode (original)
// - IcosaDefineSetup, GLTFastDefineSetup, MediaPipeDefineSetup (consolidated 2026-01-22)
//
// IMPORTANT: This is the ONLY script that should call SetScriptingDefineSymbols.
// Other *DefineSetup.cs scripts have been disabled to prevent recompile storms.
//
// Recompile Storm Prevention:
// - Caches last known defines to avoid redundant SetScriptingDefineSymbols calls
// - Uses delayCall to ensure single execution per domain reload
// - Logs all changes for debugging

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
    /// Runs automatically on domain reload with recompile storm prevention.
    /// Menu: H3M > Setup > Scripting Defines
    /// </summary>
    [InitializeOnLoad]
    public static class ScriptingDefineManager
    {
        #region Package-to-Define Mappings

        /// <summary>
        /// Package detection config - supports both manifest.json and type-based detection.
        /// </summary>
        private struct PackageConfig
        {
            public string PackageName;      // Package name in manifest.json
            public string TypeName;         // Full type name for assembly-based detection
            public string[] AssemblyHints;  // Partial assembly name matches
            public string[] Defines;        // Scripting defines to set
            public bool RequiresUnsafe;     // Whether package needs unsafe code
        }

        /// <summary>
        /// All package-to-define mappings. Single source of truth.
        /// </summary>
        private static readonly PackageConfig[] PackageConfigs = new[]
        {
            // HoloKit hand tracking
            new PackageConfig
            {
                PackageName = "io.holokit.unity-sdk",
                TypeName = "HoloKit.HoloKitCameraRig, HoloKit.Runtime",
                AssemblyHints = new[] { "HoloKit" },
                Defines = new[] { "HOLOKIT_AVAILABLE" },
                RequiresUnsafe = false
            },

            // XR Hands (Unity's hand tracking subsystem)
            new PackageConfig
            {
                PackageName = "com.unity.xr.hands",
                TypeName = "UnityEngine.XR.Hands.XRHandSubsystem, Unity.XR.Hands",
                AssemblyHints = new[] { "Unity.XR.Hands" },
                Defines = new[] { "UNITY_XR_HANDS" },
                RequiresUnsafe = false
            },

            // BodyPix 24-part body segmentation
            new PackageConfig
            {
                PackageName = "jp.keijiro.bodypix",
                TypeName = "BodyPix.BodyPixRuntime, BodyPix.Runtime",
                AssemblyHints = new[] { "BodyPix" },
                Defines = new[] { "BODYPIX_AVAILABLE" },
                RequiresUnsafe = false
            },

            // Unity WebRTC (note: conflicts with WebRtcVideoChat plugin)
            new PackageConfig
            {
                PackageName = "com.unity.webrtc",
                TypeName = "Unity.WebRTC.WebRTC, Unity.WebRTC",
                AssemblyHints = new[] { "Unity.WebRTC" },
                Defines = new[] { "UNITY_WEBRTC_AVAILABLE" },
                RequiresUnsafe = false
            },

            // Icosa 3D model API
            new PackageConfig
            {
                PackageName = "com.icosa.icosa-api-client-unity",
                TypeName = "Icosa.IcosaAsset, Icosa.Core",
                AssemblyHints = new[] { "Icosa" },
                Defines = new[] { "ICOSA_AVAILABLE" },
                RequiresUnsafe = true
            },

            // GLTFast runtime glTF loading (consolidated from GLTFastDefineSetup.cs)
            new PackageConfig
            {
                PackageName = "com.unity.cloud.gltfast",
                TypeName = "GLTFast.GltfImport, glTFast",
                AssemblyHints = new[] { "glTFast", "GLTFast" },
                Defines = new[] { "GLTFAST_AVAILABLE" },
                RequiresUnsafe = false
            },

            // MediaPipe hand/pose tracking (consolidated from MediaPipeDefineSetup.cs)
            new PackageConfig
            {
                PackageName = "com.github.homuler.mediapipe",
                TypeName = "Mediapipe.Unity.ImageSource, MediaPipe.Runtime",
                AssemblyHints = new[] { "MediaPipe", "Mediapipe" },
                Defines = new[] { "MEDIAPIPE_AVAILABLE" },
                RequiresUnsafe = false
            },
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

        #region Recompile Storm Prevention

        // EditorPrefs key to cache last known define state
        private const string LAST_DEFINES_KEY = "XRRAI_LastDefinesHash";
        private static bool _syncScheduled = false;

        #endregion

        #region Initialization

        static ScriptingDefineManager()
        {
            // Prevent multiple scheduled syncs in same domain reload
            if (!_syncScheduled)
            {
                _syncScheduled = true;
                EditorApplication.delayCall += () =>
                {
                    _syncScheduled = false;
                    SyncAllDefines();
                };
            }
        }

        #endregion

        #region Core Logic

        /// <summary>
        /// Syncs all scripting defines based on installed packages.
        /// Uses recompile storm prevention to avoid redundant SetScriptingDefineSymbols calls.
        /// </summary>
        public static void SyncAllDefines()
        {
            var namedTarget = GetCurrentNamedBuildTarget();
            var currentDefines = GetDefines(namedTarget);
            var newDefines = new HashSet<string>(currentDefines);
            var addedDefines = new List<string>();
            var removedDefines = new List<string>();

            // Check each package config using both manifest and type detection
            foreach (var config in PackageConfigs)
            {
                bool packageAvailable = IsPackageAvailable(config);

                foreach (var define in config.Defines)
                {
                    bool hasDefine = currentDefines.Contains(define);

                    if (packageAvailable && !hasDefine)
                    {
                        newDefines.Add(define);
                        addedDefines.Add($"{define} ({config.PackageName})");
                    }
                    else if (!packageAvailable && hasDefine)
                    {
                        newDefines.Remove(define);
                        removedDefines.Add($"{define} ({config.PackageName})");
                    }
                }

                // Auto-enable unsafe code if required
                if (packageAvailable && config.RequiresUnsafe && !PlayerSettings.allowUnsafeCode)
                {
                    PlayerSettings.allowUnsafeCode = true;
                    Debug.Log($"[ScriptingDefineManager] Enabled unsafe code (required by {config.PackageName})");
                }
            }

            // Recompile storm prevention: only update if defines actually changed
            string newDefinesHash = string.Join(";", newDefines.OrderBy(d => d));
            string lastDefinesHash = EditorPrefs.GetString(LAST_DEFINES_KEY, "");

            if (newDefinesHash != lastDefinesHash)
            {
                // Log changes
                foreach (var added in addedDefines)
                    Debug.Log($"[ScriptingDefineManager] Added: {added}");
                foreach (var removed in removedDefines)
                    Debug.Log($"[ScriptingDefineManager] Removed: {removed}");

                if (addedDefines.Count > 0 || removedDefines.Count > 0)
                {
                    // Actually changed - update defines
                    SetDefines(namedTarget, newDefines);
                    EditorPrefs.SetString(LAST_DEFINES_KEY, newDefinesHash);
                    Debug.Log($"[ScriptingDefineManager] Updated {addedDefines.Count} added, {removedDefines.Count} removed defines");
                }
                else
                {
                    // Just cache update (first run or external change)
                    EditorPrefs.SetString(LAST_DEFINES_KEY, newDefinesHash);
                }
            }
            // else: No changes needed, skip SetScriptingDefineSymbols to prevent recompile storm
        }

        /// <summary>
        /// Detects if a package is available using both manifest.json and type-based detection.
        /// Type-based detection is more reliable when package is installed but not in manifest.
        /// </summary>
        private static bool IsPackageAvailable(PackageConfig config)
        {
            // Method 1: Check manifest.json
            if (IsPackageInManifest(config.PackageName))
                return true;

            // Method 2: Check if type exists (handles git packages, local packages)
            if (!string.IsNullOrEmpty(config.TypeName))
            {
                var type = System.Type.GetType(config.TypeName);
                if (type != null)
                    return true;
            }

            // Method 3: Check for assemblies by name hint
            if (config.AssemblyHints != null && config.AssemblyHints.Length > 0)
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var hint in config.AssemblyHints)
                {
                    if (assemblies.Any(a => a.GetName().Name.Contains(hint)))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Force sync regardless of cache. Use for manual menu trigger.
        /// </summary>
        public static void ForceSyncAllDefines()
        {
            EditorPrefs.DeleteKey(LAST_DEFINES_KEY);
            SyncAllDefines();
        }

        #endregion

        #region Menu Items

        [MenuItem("H3M/Setup/Scripting Defines/Sync All Defines", priority = 100)]
        public static void MenuSyncDefines()
        {
            SyncAllDefines();
            ShowStatus();
        }

        [MenuItem("H3M/Setup/Scripting Defines/Force Sync (Clear Cache)", priority = 101)]
        public static void MenuForceSyncDefines()
        {
            ForceSyncAllDefines();
            ShowStatus();
        }

        [MenuItem("H3M/Setup/Scripting Defines/Show Status", priority = 102)]
        public static void ShowStatus()
        {
            var namedTarget = GetCurrentNamedBuildTarget();
            var currentDefines = GetDefines(namedTarget);

            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("   Scripting Defines Status (ScriptingDefineManager)");
            Debug.Log("═══════════════════════════════════════════════════════════");

            Debug.Log("\n  Package Defines:");
            foreach (var config in PackageConfigs)
            {
                bool available = IsPackageAvailable(config);
                foreach (var define in config.Defines)
                {
                    bool active = currentDefines.Contains(define);
                    string status = active ? "✓" : "✗";
                    string pkgStatus = available ? "available" : "not found";
                    Debug.Log($"    {status} {define} ({pkgStatus})");
                }
            }

            Debug.Log("\n  Debug Defines:");
            foreach (var define in DebugDefines)
            {
                bool active = currentDefines.Contains(define);
                Debug.Log($"    {(active ? "✓" : "✗")} {define}");
            }

            Debug.Log($"\n  Unsafe Code: {(PlayerSettings.allowUnsafeCode ? "✓ ENABLED" : "✗ DISABLED")}");
            Debug.Log($"  Storm Prevention: Hash cached in EditorPrefs");
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

        /// <summary>
        /// Checks if package is listed in manifest.json.
        /// </summary>
        private static bool IsPackageInManifest(string packageName)
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
