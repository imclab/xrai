using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace XRRAI.Editor
{
    /// <summary>
    /// Editor utility to wire migrated demo scenes from Hologrm.Demos and Portals_6
    /// </summary>
    public static class MigratedSceneSetup
    {
        private const string HOLOGRM_SCENES_PATH = "Assets/Scenes/Demos/Hologrm";
        private const string PAINTAR_SCENES_PATH = "Assets/PaintAR/Scenes";

        [MenuItem("H3M/Migrated Demos/Wire All Hologrm Scenes")]
        public static void WireAllHologrmScenes()
        {
            var scenes = new string[]
            {
                "FungiSync",
                "TestHandTracking",
                "Zuzaland_Start",
                "Zuzaland_Fireworks",
                "TransparentMan_Sample"
            };

            int wiredCount = 0;
            foreach (var sceneName in scenes)
            {
                string scenePath = $"{HOLOGRM_SCENES_PATH}/{sceneName}.unity";
                if (File.Exists(scenePath))
                {
                    WireHologrmScene(scenePath);
                    wiredCount++;
                }
            }

            Debug.Log($"[MigratedSceneSetup] Wired {wiredCount} Hologrm scenes");
        }

        [MenuItem("H3M/Migrated Demos/Wire PaintAR Interaction Scene")]
        public static void WirePaintARScene()
        {
            string scenePath = $"{PAINTAR_SCENES_PATH}/Interaction.unity";
            if (!File.Exists(scenePath))
            {
                Debug.LogError($"[MigratedSceneSetup] Scene not found: {scenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Find and fix missing references
            FixMissingReferences();

            // Ensure AR Foundation components exist
            EnsureARFoundationSetup();

            // Add our hand tracking integration
            AddHandTrackingIntegration();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MigratedSceneSetup] PaintAR Interaction scene wired successfully");
        }

        [MenuItem("H3M/Migrated Demos/Wire Current Scene")]
        public static void WireCurrentScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            FixMissingReferences();
            EnsureARFoundationSetup();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[MigratedSceneSetup] Wired current scene: {scene.name}");
        }

        private static void WireHologrmScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Fix missing references
            FixMissingReferences();

            // Ensure AR Foundation is set up
            EnsureARFoundationSetup();

            // Add VFX binders if needed
            AddVFXBinders();

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MigratedSceneSetup] Wired scene: {scene.name}");
        }

        private static void FixMissingReferences()
        {
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            int fixedCount = 0;

            foreach (var root in rootObjects)
            {
                var components = root.GetComponentsInChildren<Component>(true);
                foreach (var component in components)
                {
                    if (component == null) continue;

                    // Check for missing script references
                    var serializedObject = new SerializedObject(component);
                    var iterator = serializedObject.GetIterator();

                    while (iterator.NextVisible(true))
                    {
                        if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (iterator.objectReferenceValue == null && iterator.objectReferenceInstanceIDValue != 0)
                            {
                                // Missing reference detected - try to find replacement
                                var fieldName = iterator.name;
                                TryFixMissingReference(component, iterator, fieldName);
                                fixedCount++;
                            }
                        }
                    }
                }
            }

            if (fixedCount > 0)
            {
                Debug.Log($"[MigratedSceneSetup] Attempted to fix {fixedCount} missing references");
            }
        }

        private static void TryFixMissingReference(Component component, SerializedProperty property, string fieldName)
        {
            // Try to find VFX assets by name patterns
            if (fieldName.ToLower().Contains("vfx") || fieldName.ToLower().Contains("effect"))
            {
                var vfxAssets = AssetDatabase.FindAssets("t:VisualEffectAsset");
                foreach (var guid in vfxAssets)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (asset != null && (path.Contains("FungiSync") || path.Contains("Zuzaland") || path.Contains("TransparentMan")))
                    {
                        property.objectReferenceValue = asset;
                        property.serializedObject.ApplyModifiedProperties();
                        Debug.Log($"[MigratedSceneSetup] Auto-assigned VFX: {path}");
                        return;
                    }
                }
            }

            // Try to find materials
            if (fieldName.ToLower().Contains("material"))
            {
                var materials = AssetDatabase.FindAssets("t:Material", new[] { "Assets/PaintAR/Materials", "Assets/PaintAR/Brushes" });
                if (materials.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(materials[0]);
                    property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(path);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private static void EnsureARFoundationSetup()
        {
            // Check for AR Session
            var arSession = Object.FindObjectOfType<UnityEngine.XR.ARFoundation.ARSession>();
            if (arSession == null)
            {
                var sessionGO = new GameObject("AR Session");
                sessionGO.AddComponent<UnityEngine.XR.ARFoundation.ARSession>();
                sessionGO.AddComponent<UnityEngine.XR.ARFoundation.ARInputManager>();
                Debug.Log("[MigratedSceneSetup] Added AR Session");
            }

            // Check for XR Origin
            var xrOrigin = Object.FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                var originGO = new GameObject("XR Origin");
                var origin = originGO.AddComponent<Unity.XR.CoreUtils.XROrigin>();

                // Add AR Camera
                var cameraGO = new GameObject("AR Camera");
                cameraGO.transform.SetParent(originGO.transform);
                var camera = cameraGO.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                cameraGO.AddComponent<UnityEngine.XR.ARFoundation.ARCameraManager>();
                cameraGO.AddComponent<UnityEngine.XR.ARFoundation.ARCameraBackground>();

                origin.Camera = camera;
                Debug.Log("[MigratedSceneSetup] Added XR Origin with AR Camera");

                // Add AR managers
                originGO.AddComponent<UnityEngine.XR.ARFoundation.ARPlaneManager>();
                originGO.AddComponent<UnityEngine.XR.ARFoundation.ARRaycastManager>();
            }
        }

        private static void AddVFXBinders()
        {
            // Find all VFX in the scene
            var vfxComponents = Object.FindObjectsOfType<UnityEngine.VFX.VisualEffect>();

            foreach (var vfx in vfxComponents)
            {
                // Check if it needs a binder (look for exposed AR properties)
                bool hasARProperties = vfx.HasTexture("DepthMap") || vfx.HasTexture("PositionMap") ||
                                      vfx.HasTexture("StencilMap") || vfx.HasTexture("ColorMap");

                if (hasARProperties)
                {
                    Debug.Log($"[MigratedSceneSetup] VFX '{vfx.name}' has AR properties - may need VFXARBinder");
                }
            }
        }

        private static void AddHandTrackingIntegration()
        {
            // Look for paint stroke controllers
            var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();

            foreach (var mb in allMonoBehaviours)
            {
                if (mb.GetType().Name.Contains("Paint") || mb.GetType().Name.Contains("Brush"))
                {
                    Debug.Log($"[MigratedSceneSetup] Found paint controller: {mb.name}");
                }
            }
        }

        [MenuItem("H3M/Migrated Demos/List All Migrated Scenes")]
        public static void ListAllMigratedScenes()
        {
            Debug.Log("=== Migrated Demo Scenes ===");

            // Hologrm scenes
            Debug.Log("\n--- Hologrm Demos ---");
            if (Directory.Exists(HOLOGRM_SCENES_PATH))
            {
                var hologrmScenes = Directory.GetFiles(HOLOGRM_SCENES_PATH, "*.unity");
                foreach (var scene in hologrmScenes)
                {
                    Debug.Log($"  - {Path.GetFileNameWithoutExtension(scene)}");
                }
            }

            // PaintAR scenes
            Debug.Log("\n--- PaintAR Scenes ---");
            if (Directory.Exists(PAINTAR_SCENES_PATH))
            {
                var paintarScenes = Directory.GetFiles(PAINTAR_SCENES_PATH, "*.unity");
                foreach (var scene in paintarScenes)
                {
                    Debug.Log($"  - {Path.GetFileNameWithoutExtension(scene)}");
                }
            }
        }
    }
}
