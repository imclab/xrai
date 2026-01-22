// Spec007TestSetup - Editor utility to set up Spec 007 test scenes
// Menu: H3M > Testing > Spec 007 > Setup Audio Test Scene / Setup Physics Test Scene

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

namespace XRRAI.Editor
{
    public static class Spec007TestSetup
    {
        private const string TestScenesPath = "Assets/Scenes/SpecDemos/Tests/";

        [MenuItem("H3M/Testing/Spec 007/Setup Audio Test Scene", priority = 200)]
        public static void SetupAudioTestScene()
        {
            // Create or load scene
            string scenePath = TestScenesPath + "Spec007_Audio_Test.unity";
            Scene scene;

            if (System.IO.File.Exists(scenePath))
            {
                scene = EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // Clear existing objects
            foreach (var go in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(go);
            }

            // Create Main Camera
            var cameraGO = new GameObject("Main Camera");
            var camera = cameraGO.AddComponent<Camera>();
            cameraGO.AddComponent<AudioListener>();
            cameraGO.tag = "MainCamera";
            cameraGO.transform.position = new Vector3(0, 1, -5);

            // Create ARDepthSource
            var depthSourceGO = new GameObject("[ARDepthSource]");
            depthSourceGO.AddComponent<ARDepthSource>();

            // Create AudioBridge with AudioSource
            var audioBridgeGO = new GameObject("[AudioBridge]");
            var audioBridge = audioBridgeGO.AddComponent<AudioBridge>();
            var audioSource = audioBridgeGO.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;

            // Create AudioMonitor
            var monitorGO = new GameObject("[AudioMonitor]");
            monitorGO.AddComponent<AudioMonitor>();

            // Create VFXPipelineDashboard
            var dashboardGO = new GameObject("[VFXPipelineDashboard]");
            dashboardGO.AddComponent<VFXPipelineDashboard>();

            // Create test harness
            var harnessGO = new GameObject("VFX_Audio_Tests");
            harnessGO.AddComponent<XRRAI.Testing.AudioTestHarness>();

            // Create directional light
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[Spec007TestSetup] Audio test scene created at: {scenePath}");
            Debug.Log("[Spec007TestSetup] Press Play to start. Space=Cycle Audio, Tab=Dashboard, M=AudioMonitor");
        }

        [MenuItem("H3M/Testing/Spec 007/Setup Physics Test Scene", priority = 201)]
        public static void SetupPhysicsTestScene()
        {
            // Create or load scene
            string scenePath = TestScenesPath + "Spec007_Physics_Test.unity";
            Scene scene;

            if (System.IO.File.Exists(scenePath))
            {
                scene = EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // Clear existing objects
            foreach (var go in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(go);
            }

            // Create Main Camera
            var cameraGO = new GameObject("Main Camera");
            var camera = cameraGO.AddComponent<Camera>();
            cameraGO.AddComponent<AudioListener>();
            cameraGO.tag = "MainCamera";
            cameraGO.transform.position = new Vector3(0, 2, -8);
            cameraGO.transform.rotation = Quaternion.Euler(15, 0, 0);

            // Create ARDepthSource
            var depthSourceGO = new GameObject("[ARDepthSource]");
            depthSourceGO.AddComponent<ARDepthSource>();

            // Create VFXPipelineDashboard
            var dashboardGO = new GameObject("[VFXPipelineDashboard]");
            dashboardGO.AddComponent<VFXPipelineDashboard>();

            // Create test harness
            var harnessGO = new GameObject("VFX_Physics_Tests");
            harnessGO.AddComponent<XRRAI.Testing.PhysicsTestHarness>();

            // Create ground plane for reference
            var groundGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGO.name = "Ground";
            groundGO.transform.position = Vector3.zero;
            groundGO.transform.localScale = new Vector3(2, 1, 2);
            Object.DestroyImmediate(groundGO.GetComponent<MeshCollider>());

            // Create directional light
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[Spec007TestSetup] Physics test scene created at: {scenePath}");
            Debug.Log("[Spec007TestSetup] Press Play to start. WASD=Move, G=Gravity, V=Velocity, Tab=Dashboard");
        }

        [MenuItem("H3M/Testing/Spec 007/Validate Test Scenes", priority = 210)]
        public static void ValidateTestScenes()
        {
            Debug.Log("=== Spec 007 Test Scene Validation ===");

            ValidateScene("Spec007_Audio_Test", new[]
            {
                "ARDepthSource",
                "AudioBridge",
                "AudioMonitor",
                "VFXPipelineDashboard",
                "AudioTestHarness"
            });

            ValidateScene("Spec007_Physics_Test", new[]
            {
                "ARDepthSource",
                "VFXPipelineDashboard",
                "PhysicsTestHarness"
            });

            Debug.Log("=== Validation Complete ===");
        }

        private static void ValidateScene(string sceneName, string[] requiredComponents)
        {
            string scenePath = TestScenesPath + sceneName + ".unity";

            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"[Validate] {sceneName}: Scene not found at {scenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            foreach (var componentName in requiredComponents)
            {
                bool found = false;
                foreach (var go in scene.GetRootGameObjects())
                {
                    var component = go.GetComponent(componentName);
                    if (component != null)
                    {
                        found = true;
                        break;
                    }

                    // Check children
                    var childComponent = go.GetComponentInChildren(System.Type.GetType(componentName) ??
                        System.Type.GetType("XRRAI.Testing." + componentName) ??
                        System.Type.GetType("MetavidoVFX.Audio." + componentName));

                    if (childComponent != null)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    Debug.Log($"[Validate] {sceneName}: ✓ {componentName}");
                else
                    Debug.LogWarning($"[Validate] {sceneName}: ✗ {componentName} - MISSING");
            }

            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
#endif
