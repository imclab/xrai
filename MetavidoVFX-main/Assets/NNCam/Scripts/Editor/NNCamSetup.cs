// NNCam Scene Setup Editor
// Quick setup utilities for NNCam VFX with BodyPartSegmenter

using UnityEngine;
using UnityEngine.VFX;
using UnityEditor;

namespace MetavidoVFX.NNCam.Editor
{
    /// <summary>
    /// Editor utilities for setting up NNCam VFX in a scene.
    /// </summary>
    public static class NNCamSetup
    {
        [MenuItem("H3M/NNCam/Setup NNCam VFX Scene", priority = 100)]
        public static void SetupNNCamScene()
        {
            // Find or create NNCam parent
            GameObject nncamRoot = GameObject.Find("NNCam");
            if (nncamRoot == null)
            {
                nncamRoot = new GameObject("NNCam");
                Undo.RegisterCreatedObjectUndo(nncamRoot, "Create NNCam Root");
            }

            // Create VFX container
            GameObject vfxContainer = new GameObject("VFX");
            vfxContainer.transform.SetParent(nncamRoot.transform);
            Undo.RegisterCreatedObjectUndo(vfxContainer, "Create VFX Container");

            // Add VFX Switcher
            var switcher = nncamRoot.AddComponent<NNCamVFXSwitcher>();
            Undo.RegisterCreatedObjectUndo(switcher, "Add NNCam VFX Switcher");

            // Load NNCam2 VFX assets from Resources or specific path
            string[] vfxPaths = new string[]
            {
                "Assets/VFX/NNCam2/eyes_any_nncam2.vfx",
                "Assets/VFX/NNCam2/joints_any_nncam2.vfx",
                "Assets/VFX/NNCam2/electrify_any_nncam2.vfx",
                "Assets/VFX/NNCam2/mosaic_any_nncam2.vfx",
                "Assets/VFX/NNCam2/spikes_any_nncam2.vfx",
                "Assets/VFX/NNCam2/petals_any_nncam2.vfx",
                "Assets/VFX/NNCam2/tentacles_any_nncam2.vfx",
                "Assets/VFX/NNCam2/particle_any_nncam2.vfx",
                "Assets/VFX/NNCam2/symbols_any_nncam2.vfx"
            };

            // Create VFX GameObjects
            foreach (string path in vfxPaths)
            {
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                if (vfxAsset == null)
                {
                    Debug.LogWarning($"[NNCamSetup] VFX not found: {path}");
                    continue;
                }

                string vfxName = System.IO.Path.GetFileNameWithoutExtension(path);
                GameObject vfxGO = new GameObject(vfxName);
                vfxGO.transform.SetParent(vfxContainer.transform);

                var vfx = vfxGO.AddComponent<VisualEffect>();
                vfx.visualEffectAsset = vfxAsset;
                vfx.enabled = false; // Start disabled

                // Add VFXPropertyBinder for the keypoint binder
                var propertyBinder = vfxGO.AddComponent<UnityEngine.VFX.Utility.VFXPropertyBinder>();

                // Add NNCam Keypoint Binder
                var keypointBinder = vfxGO.AddComponent<NNCamKeypointBinder>();

                // Register with switcher
                switcher.AddVFX(vfx);

                Undo.RegisterCreatedObjectUndo(vfxGO, $"Create {vfxName}");
            }

            // Select the first VFX
            if (switcher.VFXCount > 0)
            {
                switcher.SelectVFX(0);
            }

            Selection.activeGameObject = nncamRoot;
            Debug.Log($"[NNCamSetup] Created NNCam scene with {switcher.VFXCount} VFX effects");
        }

        [MenuItem("H3M/NNCam/Add Keypoint Binder to Selected VFX", priority = 101)]
        public static void AddKeypointBinderToSelected()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                var vfx = go.GetComponent<VisualEffect>();
                if (vfx == null)
                {
                    Debug.LogWarning($"[NNCamSetup] {go.name} has no VisualEffect component");
                    continue;
                }

                // Check if it has KeypointBuffer property
                if (!vfx.HasGraphicsBuffer("KeypointBuffer"))
                {
                    Debug.LogWarning($"[NNCamSetup] {go.name} VFX has no KeypointBuffer property");
                    continue;
                }

                // Add property binder if missing
                var propertyBinder = go.GetComponent<UnityEngine.VFX.Utility.VFXPropertyBinder>();
                if (propertyBinder == null)
                {
                    propertyBinder = go.AddComponent<UnityEngine.VFX.Utility.VFXPropertyBinder>();
                    Undo.RegisterCreatedObjectUndo(propertyBinder, "Add VFXPropertyBinder");
                }

                // Add keypoint binder if missing
                var keypointBinder = go.GetComponent<NNCamKeypointBinder>();
                if (keypointBinder == null)
                {
                    keypointBinder = go.AddComponent<NNCamKeypointBinder>();
                    Undo.RegisterCreatedObjectUndo(keypointBinder, "Add NNCamKeypointBinder");
                    Debug.Log($"[NNCamSetup] Added NNCamKeypointBinder to {go.name}");
                }
                else
                {
                    Debug.Log($"[NNCamSetup] {go.name} already has NNCamKeypointBinder");
                }
            }
        }

        [MenuItem("H3M/NNCam/Create Single Eyes VFX", priority = 102)]
        public static void CreateSingleEyesVFX()
        {
            string path = "Assets/VFX/NNCam2/eyes_any_nncam2.vfx";
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);

            if (vfxAsset == null)
            {
                Debug.LogError($"[NNCamSetup] Eyes VFX not found at {path}");
                return;
            }

            GameObject vfxGO = new GameObject("NNCam_Eyes");

            var vfx = vfxGO.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = vfxAsset;

            var propertyBinder = vfxGO.AddComponent<UnityEngine.VFX.Utility.VFXPropertyBinder>();
            var keypointBinder = vfxGO.AddComponent<NNCamKeypointBinder>();

            Undo.RegisterCreatedObjectUndo(vfxGO, "Create NNCam Eyes VFX");
            Selection.activeGameObject = vfxGO;

            Debug.Log("[NNCamSetup] Created NNCam Eyes VFX - ensure BodyPartSegmenter is in scene");
        }
    }
}
