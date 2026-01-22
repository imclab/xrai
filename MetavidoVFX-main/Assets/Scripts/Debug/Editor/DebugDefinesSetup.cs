// DebugDefinesSetup.cs - Editor tool for managing debug scripting defines

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System.Collections.Generic;

using XRRAI.Debugging;

namespace Metavido.Diagnostics.Editor
{
    /// <summary>
    /// Editor utilities for managing debug scripting defines.
    /// H3M > Debug > Setup Debug Defines
    /// </summary>
    public static class DebugDefinesSetup
    {
        private static readonly string[] DebugDefines = new[]
        {
            "DEBUG_TRACKING",
            "DEBUG_VOICE",
            "DEBUG_VFX",
            "DEBUG_NETWORK",
            "DEBUG_SYSTEM"
        };

        [MenuItem("H3M/Debug/Setup Debug Defines (Development)", priority = 500)]
        public static void AddDebugDefines()
        {
            var namedTarget = GetCurrentNamedBuildTarget();
            var defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            var defineList = new List<string>(defines.Split(';'));

            bool changed = false;
            foreach (var define in DebugDefines)
            {
                if (!defineList.Contains(define))
                {
                    defineList.Add(define);
                    changed = true;
                }
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", defineList));
                Debug.Log($"[DebugDefinesSetup] Added debug defines for {namedTarget}: {string.Join(", ", DebugDefines)}");
            }
            else
            {
                Debug.Log("[DebugDefinesSetup] All debug defines already present");
            }
        }

        [MenuItem("H3M/Debug/Remove Debug Defines (Production)", priority = 501)]
        public static void RemoveDebugDefines()
        {
            var namedTarget = GetCurrentNamedBuildTarget();
            var defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            var defineList = new List<string>(defines.Split(';'));

            bool changed = false;
            foreach (var define in DebugDefines)
            {
                if (defineList.Contains(define))
                {
                    defineList.Remove(define);
                    changed = true;
                }
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", defineList));
                Debug.Log($"[DebugDefinesSetup] Removed debug defines for {namedTarget}");
            }
            else
            {
                Debug.Log("[DebugDefinesSetup] No debug defines to remove");
            }
        }

        [MenuItem("H3M/Debug/Check Debug Defines", priority = 502)]
        public static void CheckDebugDefines()
        {
            var namedTarget = GetCurrentNamedBuildTarget();
            var defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            var defineList = new HashSet<string>(defines.Split(';'));

            Debug.Log($"=== Debug Defines Status ({namedTarget}) ===");
            foreach (var define in DebugDefines)
            {
                bool present = defineList.Contains(define);
                Debug.Log($"  {(present ? "✓" : "✗")} {define}");
            }
        }

        /// <summary>
        /// Convert the selected build target group to NamedBuildTarget (Unity 2021.2+ API)
        /// </summary>
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
                BuildTargetGroup.WSA => NamedBuildTarget.WindowsStoreApps,
                BuildTargetGroup.PS4 => NamedBuildTarget.PS4,
                BuildTargetGroup.PS5 => NamedBuildTarget.PS5,
                BuildTargetGroup.XboxOne => NamedBuildTarget.XboxOne,
                BuildTargetGroup.tvOS => NamedBuildTarget.tvOS,
                BuildTargetGroup.LinuxHeadlessSimulation => NamedBuildTarget.LinuxHeadlessSimulation,
                BuildTargetGroup.EmbeddedLinux => NamedBuildTarget.EmbeddedLinux,
                BuildTargetGroup.QNX => NamedBuildTarget.QNX,
                BuildTargetGroup.VisionOS => NamedBuildTarget.VisionOS,
                _ => NamedBuildTarget.FromBuildTargetGroup(targetGroup)
            };
        }

        [MenuItem("H3M/Debug/Add DebugBootstrap to Scene", priority = 510)]
        public static void AddDebugBootstrap()
        {
            // Check if already exists
            var existing = Object.FindFirstObjectByType<DebugBootstrap>();
            if (existing != null)
            {
                Debug.Log($"[DebugDefinesSetup] DebugBootstrap already exists: {existing.gameObject.name}");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Create new
            var go = new GameObject("DebugBootstrap");
            go.AddComponent<DebugBootstrap>();

            // Add under Debug parent if it exists
            var debugParent = GameObject.Find("Debug");
            if (debugParent != null)
            {
                go.transform.SetParent(debugParent.transform);
            }

            Undo.RegisterCreatedObjectUndo(go, "Add DebugBootstrap");
            Selection.activeGameObject = go;
            Debug.Log("[DebugDefinesSetup] Created DebugBootstrap");
        }

        [MenuItem("H3M/Debug/Create Debug Test Scene", priority = 520)]
        public static void CreateDebugTestScene()
        {
            // Create new scene
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // === Camera ===
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0, 1, -3);
                mainCam.transform.rotation = Quaternion.identity;
                mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }

            // === Debug Hierarchy ===
            var debugParent = new GameObject("Debug");

            var bootstrap = new GameObject("DebugBootstrap");
            bootstrap.transform.SetParent(debugParent.transform);
            bootstrap.AddComponent<DebugBootstrap>();

            // === Pipeline Hierarchy ===
            var pipelineParent = new GameObject("Pipeline");

            // ARDepthSource (will use mock data in Editor)
            var depthSource = new GameObject("ARDepthSource");
            depthSource.transform.SetParent(pipelineParent.transform);
            depthSource.AddComponent<ARDepthSource>();

            // VFXPipelineDashboard
            var dashboard = new GameObject("VFXPipelineDashboard");
            dashboard.transform.SetParent(pipelineParent.transform);
            dashboard.AddComponent<VFXPipelineDashboard>();

            // VFXTestHarness
            var harness = new GameObject("VFXTestHarness");
            harness.transform.SetParent(pipelineParent.transform);
            harness.AddComponent<VFXTestHarness>();

            // VFXProfiler
            var profiler = new GameObject("VFXProfiler");
            profiler.transform.SetParent(pipelineParent.transform);
            profiler.AddComponent<XRRAI.Performance.VFXProfiler>();

            // === Test VFX (placeholder cube) ===
            var testVFX = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testVFX.name = "TestCube (Replace with VFX)";
            testVFX.transform.position = Vector3.zero;
            testVFX.transform.localScale = Vector3.one * 0.5f;

            // Add light
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Save scene
            string scenePath = "Assets/Scenes/Testing/DebugTestScene.unity";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(
                System.IO.Path.Combine(Application.dataPath, "../", scenePath)));
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[DebugDefinesSetup] Created Debug Test Scene at {scenePath}");
            Debug.Log("  Contents: DebugBootstrap, ARDepthSource (mock), Dashboard, TestHarness, Profiler");
            Debug.Log("  Press Tab to toggle dashboard, 1-9/Space/C/A for VFX shortcuts");
        }
    }
}
#endif
