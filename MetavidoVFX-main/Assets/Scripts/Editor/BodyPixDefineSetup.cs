// Auto-setup BODYPIX_AVAILABLE scripting define when BodyPixSentis package is present
// Run: H3M > Body Segmentation > Setup BodyPix Defines
// Provides 24-part body segmentation for VFX

using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MetavidoVFX.Editor
{
    [InitializeOnLoad]
    public static class BodyPixDefineSetup
    {
        private const string BODYPIX_DEFINE = "BODYPIX_AVAILABLE";
        private const string BODYPIX_PACKAGE = "jp.keijiro.bodypix";

        static BodyPixDefineSetup()
        {
            // Auto-setup on domain reload
            EditorApplication.delayCall += AutoSetupDefines;
        }

        static void AutoSetupDefines()
        {
            bool bodyPixInstalled = IsPackageInstalled(BODYPIX_PACKAGE);
            bool bodyPixDefined = HasDefine(BODYPIX_DEFINE);

            if (bodyPixInstalled && !bodyPixDefined)
            {
                AddDefine(BODYPIX_DEFINE);
                Debug.Log("[BodyPix Setup] Added BODYPIX_AVAILABLE define - 24-part body segmentation enabled");
            }
            else if (!bodyPixInstalled && bodyPixDefined)
            {
                RemoveDefine(BODYPIX_DEFINE);
                Debug.Log("[BodyPix Setup] Removed BODYPIX_AVAILABLE define (package not found)");
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

        [MenuItem("H3M/Body Segmentation/Setup BodyPix Defines")]
        public static void ManualSetup()
        {
            AutoSetupDefines();

            bool bodyPixDefined = HasDefine(BODYPIX_DEFINE);
            bool bodyPixInstalled = IsPackageInstalled(BODYPIX_PACKAGE);

            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("   BodyPix Segmentation Status");
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log($"  Package (jp.keijiro.bodypix): {(bodyPixInstalled ? "✓ Installed" : "✗ Not installed")}");
            Debug.Log($"  BODYPIX_AVAILABLE define: {(bodyPixDefined ? "✓ Defined" : "✗ Not defined")}");
            Debug.Log("═══════════════════════════════════════════════════════════");

            if (bodyPixInstalled && bodyPixDefined)
            {
                Debug.Log("[BodyPix Setup] ✓ 24-part body segmentation is ready to use");
                Debug.Log("[BodyPix Setup] Add BodyPartSegmenter component and assign ResourceSet from package");
            }
            else if (!bodyPixInstalled)
            {
                Debug.LogWarning("[BodyPix Setup] Package not installed. Add to Packages/manifest.json:");
                Debug.LogWarning("  \"jp.keijiro.bodypix\": \"4.0.0\"");
            }
        }

        [MenuItem("H3M/Body Segmentation/Force Enable BodyPix")]
        public static void ForceEnableBodyPix()
        {
            AddDefine(BODYPIX_DEFINE);
            Debug.Log("[BodyPix Setup] Force enabled BODYPIX_AVAILABLE");
            Debug.Log("[BodyPix Setup] Warning: Will cause compile errors if package not installed");
        }

        [MenuItem("H3M/Body Segmentation/Disable BodyPix")]
        public static void DisableBodyPix()
        {
            RemoveDefine(BODYPIX_DEFINE);
            Debug.Log("[BodyPix Setup] Disabled BODYPIX_AVAILABLE");
        }

        [MenuItem("H3M/Body Segmentation/Add BodyPartSegmenter to Scene")]
        public static void AddBodyPartSegmenterToScene()
        {
            if (!HasDefine(BODYPIX_DEFINE))
            {
                Debug.LogError("[BodyPix Setup] BODYPIX_AVAILABLE not defined. Run 'Setup BodyPix Defines' first.");
                return;
            }

            // Find existing or create new
            var existing = Object.FindFirstObjectByType<Segmentation.BodyPartSegmenter>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[BodyPix Setup] BodyPartSegmenter already exists in scene");
                return;
            }

            // Create on AR Session Origin or new GameObject
            var arSessionOrigin = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARSessionOrigin>();
            GameObject parent = arSessionOrigin != null ? arSessionOrigin.gameObject : null;

            GameObject go;
            if (parent != null)
            {
                go = new GameObject("BodyPartSegmenter");
                go.transform.SetParent(parent.transform);
            }
            else
            {
                go = new GameObject("BodyPartSegmenter");
            }

            var segmenter = go.AddComponent<Segmentation.BodyPartSegmenter>();
            Selection.activeGameObject = go;

            Debug.Log("[BodyPix Setup] Created BodyPartSegmenter");
            Debug.Log("[BodyPix Setup] IMPORTANT: Assign ResourceSet from Packages/jp.keijiro.bodypix/Resources/");

            EditorGUIUtility.PingObject(go);
        }
    }
}
