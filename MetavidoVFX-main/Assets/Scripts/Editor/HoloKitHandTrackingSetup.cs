// Editor utility to set up HoloKit camera rig with hand tracking
// Menu: H3M > HoloKit > Setup Complete HoloKit Rig (disables XR Rig)
// Based on HoloKit SDK GazeGestureInteraction sample pattern

using UnityEngine;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;
using UnityEngine.InputSystem.XR;

#if HOLOKIT_AVAILABLE
using HoloKit;
using HoloKit.iOS;
#endif

namespace XRRAI.Editor
{
    public static class HoloKitHandTrackingSetup
    {
        [MenuItem("H3M/HoloKit/Setup Complete HoloKit Rig (disables XR Rig)")]
        public static void SetupCompleteHoloKitRig()
        {
            Debug.Log("[HoloKit Setup] Setting up complete HoloKit rig...");

            // 1. Find and disable existing XR Rig (don't delete, just disable)
            DisableExistingXRRig();

            // 2. Find or create AR Session
            EnsureARSession();

            // 3. Create HoloKit Camera Rig
            CreateHoloKitCameraRig();

            // 4. Setup hand tracking
            SetupHandTracking();

            // 5. Setup VFX Gallery
            SetupVFXGallery();

            Debug.Log("[HoloKit Setup] ✓ Complete HoloKit rig setup finished!");
        }

        static void DisableExistingXRRig()
        {
            // Look for common XR rig patterns
            string[] xrRigNames = {
                "XR Origin", "XR Rig", "XROrigin", "XRRig",
                "AR Session Origin", "ARSessionOrigin"
            };

            foreach (string rigName in xrRigNames)
            {
                var rig = GameObject.Find(rigName);
                if (rig != null && rig.activeSelf)
                {
                    // Check if it's not our new HoloKit rig
                    if (!rig.name.Contains("HoloKit"))
                    {
                        Undo.RecordObject(rig, "Disable XR Rig");
                        rig.SetActive(false);
                        Debug.Log($"[HoloKit Setup] Disabled existing rig: {rigName}");
                    }
                }
            }

            // Also look for XROrigin component
            var xrOrigins = Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None);
            foreach (var origin in xrOrigins)
            {
                if (!origin.gameObject.name.Contains("HoloKit") && origin.gameObject.activeSelf)
                {
                    Undo.RecordObject(origin.gameObject, "Disable XR Origin");
                    origin.gameObject.SetActive(false);
                    Debug.Log($"[HoloKit Setup] Disabled XROrigin: {origin.gameObject.name}");
                }
            }
        }

        static void EnsureARSession()
        {
            var arSession = Object.FindFirstObjectByType<ARSession>();
            if (arSession == null)
            {
                var sessionObj = new GameObject("AR Session");
                arSession = sessionObj.AddComponent<ARSession>();
                sessionObj.AddComponent<ARInputManager>();
                Undo.RegisterCreatedObjectUndo(sessionObj, "Create AR Session");
                Debug.Log("[HoloKit Setup] ✓ Created AR Session");
            }
            else
            {
                // Make sure it's enabled
                if (!arSession.gameObject.activeSelf)
                {
                    Undo.RecordObject(arSession.gameObject, "Enable AR Session");
                    arSession.gameObject.SetActive(true);
                }
                Debug.Log("[HoloKit Setup] ✓ AR Session exists");
            }
        }

        static void CreateHoloKitCameraRig()
        {
            // Check if HoloKit rig already exists
            var existingRig = GameObject.Find("HoloKit Camera Rig");
            if (existingRig != null)
            {
                Debug.Log("[HoloKit Setup] ✓ HoloKit Camera Rig already exists");
                Selection.activeGameObject = existingRig;
                return;
            }

            // Create the rig hierarchy
            var rigRoot = new GameObject("HoloKit Camera Rig");
            Undo.RegisterCreatedObjectUndo(rigRoot, "Create HoloKit Rig");

            // Add XROrigin
            var xrOrigin = rigRoot.AddComponent<Unity.XR.CoreUtils.XROrigin>();

            // Create Camera Offset
            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(rigRoot.transform);
            cameraOffset.transform.localPosition = Vector3.zero;
            xrOrigin.CameraFloorOffsetObject = cameraOffset;

            // Create AR Camera
            var cameraObj = new GameObject("AR Camera");
            cameraObj.transform.SetParent(cameraOffset.transform);
            cameraObj.transform.localPosition = Vector3.zero;
            cameraObj.tag = "MainCamera";

            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            // Add AR components
            cameraObj.AddComponent<ARCameraManager>();
            cameraObj.AddComponent<ARCameraBackground>();
            cameraObj.AddComponent<TrackedPoseDriver>();

            // Add occlusion manager for depth/stencil
            var occlusionManager = cameraObj.AddComponent<AROcclusionManager>();
            occlusionManager.requestedHumanStencilMode = UnityEngine.XR.ARSubsystems.HumanSegmentationStencilMode.Best;
            occlusionManager.requestedEnvironmentDepthMode = UnityEngine.XR.ARSubsystems.EnvironmentDepthMode.Best;

            // Set camera reference
            xrOrigin.Camera = camera;

#if HOLOKIT_AVAILABLE
            // Add HoloKit components
            var holoKitCameraManager = cameraObj.AddComponent<HoloKitCameraManager>();
            Debug.Log("[HoloKit Setup] ✓ Added HoloKitCameraManager");
#else
            Debug.LogWarning("[HoloKit Setup] HOLOKIT_AVAILABLE not defined - HoloKitCameraManager not added");
#endif

            // Add AR Plane Manager for desk detection
            var planeManager = rigRoot.AddComponent<ARPlaneManager>();

            // Add AR Raycast Manager for placement
            rigRoot.AddComponent<ARRaycastManager>();

            Debug.Log("[HoloKit Setup] ✓ Created HoloKit Camera Rig with AR components");

            Selection.activeGameObject = rigRoot;
        }

        static void SetupHandTracking()
        {
#if HOLOKIT_AVAILABLE
            // Create Hand Gesture Recognition Manager
            var handGestureManager = Object.FindFirstObjectByType<HandGestureRecognitionManager>();
            GameObject handTrackingObj;
            if (handGestureManager == null)
            {
                handTrackingObj = new GameObject("Hand Tracking");
                handGestureManager = handTrackingObj.AddComponent<HandGestureRecognitionManager>();

                // Add Hand Tracking Manager
                handTrackingObj.AddComponent<HandTrackingManager>();

                Undo.RegisterCreatedObjectUndo(handTrackingObj, "Create Hand Tracking");
                Debug.Log("[HoloKit Setup] ✓ Created HandGestureRecognitionManager + HandTrackingManager");
            }
            else
            {
                handTrackingObj = handGestureManager.gameObject;
            }

            // Create Gaze Interaction system
            var gazeRaycastInteractor = Object.FindFirstObjectByType<GazeRaycastInteractor>();
            if (gazeRaycastInteractor == null)
            {
                var gazeObj = new GameObject("Gaze Interaction");
                gazeRaycastInteractor = gazeObj.AddComponent<GazeRaycastInteractor>();
                gazeObj.AddComponent<GazeGestureInteractor>();
                Undo.RegisterCreatedObjectUndo(gazeObj, "Create Gaze Interaction");
                Debug.Log("[HoloKit Setup] ✓ Created GazeRaycastInteractor + GazeGestureInteractor");
            }

            // Setup Hand VFX Controller
            SetupHandVFXController(handTrackingObj);
#else
            Debug.LogWarning("[HoloKit Setup] HOLOKIT_AVAILABLE not defined - hand tracking components not added");
            Debug.Log("[HoloKit Setup] To enable HoloKit, add 'HOLOKIT_AVAILABLE' to Scripting Define Symbols");
#endif
        }

        static void SetupHandVFXController(GameObject handTrackingObj)
        {
            // Check if HandVFXController already exists
            var existingController = Object.FindFirstObjectByType<HandTracking.HandVFXController>();
            if (existingController != null)
            {
                Debug.Log("[HoloKit Setup] ✓ HandVFXController already exists");
                return;
            }

            // Create Hand VFX root object
            var handVFXRoot = new GameObject("Hand VFX");
            Undo.RegisterCreatedObjectUndo(handVFXRoot, "Create Hand VFX");

            // Create left hand
            var leftHand = new GameObject("Left Hand");
            leftHand.transform.SetParent(handVFXRoot.transform);
            var leftVFX = leftHand.AddComponent<VisualEffect>();

            // Create right hand
            var rightHand = new GameObject("Right Hand");
            rightHand.transform.SetParent(handVFXRoot.transform);
            var rightVFX = rightHand.AddComponent<VisualEffect>();

            // Load default VFX from Resources
            var defaultVFX = Resources.Load<VisualEffectAsset>("VFX/Star");
            if (defaultVFX == null)
            {
                // Try other common VFX names
                defaultVFX = Resources.Load<VisualEffectAsset>("VFX/ParticleLine");
            }

            if (defaultVFX != null)
            {
                leftVFX.visualEffectAsset = defaultVFX;
                rightVFX.visualEffectAsset = defaultVFX;
                Debug.Log($"[HoloKit Setup] ✓ Assigned VFX: {defaultVFX.name}");
            }
            else
            {
                Debug.LogWarning("[HoloKit Setup] No VFX found in Resources/VFX - assign manually");
            }

            // Add HandVFXController
            var controller = handVFXRoot.AddComponent<HandTracking.HandVFXController>();

            // Configure via SerializedObject
            var so = new SerializedObject(controller);
            so.FindProperty("leftHandRoot").objectReferenceValue = leftHand.transform;
            so.FindProperty("rightHandRoot").objectReferenceValue = rightHand.transform;
            so.FindProperty("leftHandVFX").objectReferenceValue = leftVFX;
            so.FindProperty("rightHandVFX").objectReferenceValue = rightVFX;
            so.FindProperty("useHoloKitHandTracking").boolValue = true;

            // Load all available VFX assets
            var allVFX = Resources.LoadAll<VisualEffectAsset>("VFX");
            if (allVFX.Length > 0)
            {
                var vfxArrayProp = so.FindProperty("availableVFXAssets");
                vfxArrayProp.arraySize = allVFX.Length;
                for (int i = 0; i < allVFX.Length; i++)
                {
                    vfxArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = allVFX[i];
                }
                Debug.Log($"[HoloKit Setup] ✓ Loaded {allVFX.Length} VFX assets");
            }

            // Try to find AudioBridge (preferred)
            var audioBridge = Object.FindFirstObjectByType<AudioBridge>();
            if (audioBridge != null)
            {
                so.FindProperty("audioBridge").objectReferenceValue = audioBridge;
                Debug.Log("[HoloKit Setup] ✓ Connected AudioBridge");
            }
            else
            {
                Debug.LogWarning("[HoloKit Setup] AudioBridge not found - add via H3M > VFX Pipeline Master > Create AudioBridge");
            }

            // Legacy fallback: EnhancedAudioProcessor
            var audioProcessor = Object.FindFirstObjectByType<Audio.EnhancedAudioProcessor>();
            if (audioProcessor != null)
            {
                so.FindProperty("enhancedAudioProcessor").objectReferenceValue = audioProcessor;
                Debug.Log("[HoloKit Setup] ✓ Connected EnhancedAudioProcessor (legacy)");
            }

            so.ApplyModifiedProperties();

            Debug.Log("[HoloKit Setup] ✓ Created HandVFXController with Left/Right hand VFX");
        }

        static void SetupVFXGallery()
        {
            // Check if VFX Gallery exists
            var gallery = Object.FindFirstObjectByType<UI.VFXGalleryUI>();
            if (gallery != null)
            {
                Debug.Log("[HoloKit Setup] ✓ VFX Gallery already exists");
                return;
            }

            // Load VFX assets from Resources
            var vfxAssets = Resources.LoadAll<VisualEffectAsset>("VFX");
            if (vfxAssets.Length == 0)
            {
                Debug.LogWarning("[HoloKit Setup] No VFX assets in Resources/VFX - VFX Gallery not created");
                Debug.Log("[HoloKit Setup] Run 'Metavido > Copy More VFX to Resources' first");
                return;
            }

            // Clean up ANY existing VFX containers first
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t != null && t.name == "SpawnControlVFX_Container")
                {
                    Debug.Log($"[HoloKit Setup] Removing existing container");
                    Undo.DestroyObjectImmediate(t.gameObject);
                }
            }

            // Create VFX Gallery
            var galleryObj = new GameObject("VFXGalleryUI");
            gallery = galleryObj.AddComponent<UI.VFXGalleryUI>();
            Undo.RegisterCreatedObjectUndo(galleryObj, "Create VFX Gallery");

            // Configure gallery to auto-populate (don't create instances here)
            SerializedObject so = new SerializedObject(gallery);
            so.FindProperty("autoPopulateFromResources").boolValue = true;
            so.FindProperty("useSpawnControlMode").boolValue = true;
            so.FindProperty("vfxResourceFolder").stringValue = "VFX";

            // Set VFX assets array only (let runtime populate instances)
            var vfxAssetsProp = so.FindProperty("vfxAssets");
            vfxAssetsProp.arraySize = vfxAssets.Length;
            for (int i = 0; i < vfxAssets.Length; i++)
            {
                vfxAssetsProp.GetArrayElementAtIndex(i).objectReferenceValue = vfxAssets[i];
            }
            so.ApplyModifiedProperties();

            Debug.Log($"[HoloKit Setup] ✓ Created VFX Gallery with {vfxAssets.Length} effects (instances created at runtime)");
        }

        [MenuItem("H3M/HoloKit/Setup Camera Rig with Hand Tracking")]
        public static void SetupHoloKitWithHandTracking()
        {
            Debug.Log("[HoloKit Setup] Setting up HoloKit camera rig with hand tracking...");

#if HOLOKIT_AVAILABLE
            // Find AR Camera
            var arCameraManager = Object.FindFirstObjectByType<ARCameraManager>();
            if (arCameraManager == null)
            {
                Debug.LogError("[HoloKit Setup] No ARCameraManager found! Please set up AR Foundation first.");
                return;
            }

            GameObject cameraObj = arCameraManager.gameObject;

            // 1. Add HoloKitCameraManager
            var holoKitCameraManager = cameraObj.GetComponent<HoloKitCameraManager>();
            if (holoKitCameraManager == null)
            {
                holoKitCameraManager = Undo.AddComponent<HoloKitCameraManager>(cameraObj);
                Debug.Log("[HoloKit Setup] ✓ Added HoloKitCameraManager");
            }

            // 2. Add HandGestureRecognitionManager
            var handGestureManager = Object.FindFirstObjectByType<HandGestureRecognitionManager>();
            if (handGestureManager == null)
            {
                GameObject handTrackingObj = new GameObject("HandGestureRecognitionManager");
                handGestureManager = handTrackingObj.AddComponent<HandGestureRecognitionManager>();
                Undo.RegisterCreatedObjectUndo(handTrackingObj, "Create Hand Gesture Manager");
                Debug.Log("[HoloKit Setup] ✓ Created HandGestureRecognitionManager");
            }

            // 3. Add GazeRaycastInteractor
            var gazeRaycastInteractor = Object.FindFirstObjectByType<GazeRaycastInteractor>();
            if (gazeRaycastInteractor == null)
            {
                GameObject gazeObj = new GameObject("GazeInteraction");
                gazeRaycastInteractor = gazeObj.AddComponent<GazeRaycastInteractor>();
                gazeObj.AddComponent<GazeGestureInteractor>();
                Undo.RegisterCreatedObjectUndo(gazeObj, "Create Gaze Raycast Interactor");
                Debug.Log("[HoloKit Setup] ✓ Created GazeRaycastInteractor + GazeGestureInteractor");
            }

            Debug.Log("[HoloKit Setup] ✓ Hand tracking setup complete!");
#else
            Debug.LogError("[HoloKit Setup] HOLOKIT_AVAILABLE not defined. Please install HoloKit SDK.");
            Debug.Log("[HoloKit Setup] Add 'HOLOKIT_AVAILABLE' to Scripting Define Symbols in Player Settings.");
#endif
        }

        [MenuItem("H3M/HoloKit/Validate Hand Tracking Setup")]
        public static void ValidateHandTrackingSetup()
        {
            Debug.Log("[HoloKit Setup] Validating hand tracking setup...");

            bool allGood = true;

            // Check AR components
            var arSession = Object.FindFirstObjectByType<ARSession>();
            Log("ARSession", arSession != null);
            allGood &= arSession != null;

            var arCameraManager = Object.FindFirstObjectByType<ARCameraManager>();
            Log("ARCameraManager", arCameraManager != null);
            allGood &= arCameraManager != null;

            var occlusionManager = Object.FindFirstObjectByType<AROcclusionManager>();
            Log("AROcclusionManager", occlusionManager != null);

#if HOLOKIT_AVAILABLE
            var holoKitCameraManager = Object.FindFirstObjectByType<HoloKitCameraManager>();
            var handGestureManager = Object.FindFirstObjectByType<HandGestureRecognitionManager>();
            var gazeRaycastInteractor = Object.FindFirstObjectByType<GazeRaycastInteractor>();
            var gazeGestureInteractor = Object.FindFirstObjectByType<GazeGestureInteractor>();

            Log("HoloKitCameraManager", holoKitCameraManager != null);
            Log("HandGestureRecognitionManager", handGestureManager != null);
            Log("GazeRaycastInteractor", gazeRaycastInteractor != null);
            Log("GazeGestureInteractor", gazeGestureInteractor != null);

            // Check HandVFXController
            var handVFXController = Object.FindFirstObjectByType<HandTracking.HandVFXController>();
            Log("HandVFXController", handVFXController != null);

            allGood &= holoKitCameraManager != null && handGestureManager != null &&
                       gazeRaycastInteractor != null && gazeGestureInteractor != null &&
                       handVFXController != null;
#else
            Debug.LogWarning("[HoloKit Setup] HOLOKIT_AVAILABLE not defined - HoloKit components not checked");
            allGood = false;
#endif

            // Check VFX Gallery
            var gallery = Object.FindFirstObjectByType<UI.VFXGalleryUI>();
            Log("VFXGalleryUI", gallery != null);

            if (allGood)
            {
                Debug.Log("[HoloKit Setup] ✓ All components present and configured!");
            }
            else
            {
                Debug.LogWarning("[HoloKit Setup] Some components missing. Run 'Setup Complete HoloKit Rig' to fix.");
            }
        }

        [MenuItem("H3M/HoloKit/Re-enable Original XR Rig")]
        public static void ReEnableOriginalXRRig()
        {
            string[] xrRigNames = {
                "XR Origin", "XR Rig", "XROrigin", "XRRig",
                "AR Session Origin", "ARSessionOrigin"
            };

            foreach (string rigName in xrRigNames)
            {
                var rig = GameObject.Find(rigName);
                if (rig != null && !rig.activeSelf)
                {
                    Undo.RecordObject(rig, "Enable XR Rig");
                    rig.SetActive(true);
                    Debug.Log($"[HoloKit Setup] Re-enabled: {rigName}");
                }
            }

            // Disable HoloKit rig
            var holoKitRig = GameObject.Find("HoloKit Camera Rig");
            if (holoKitRig != null)
            {
                Undo.RecordObject(holoKitRig, "Disable HoloKit Rig");
                holoKitRig.SetActive(false);
                Debug.Log("[HoloKit Setup] Disabled HoloKit Camera Rig");
            }
        }

        [MenuItem("H3M/HoloKit/Add VFX Gallery with Hand Tracking")]
        public static void AddVFXGalleryWithHandTracking()
        {
            SetupHoloKitWithHandTracking();
            UI.Editor.VFXGallerySetup.SetupCompleteVFXSystem();
            Debug.Log("[HoloKit Setup] ✓ VFX Gallery with hand tracking ready!");
        }

        static void Log(string component, bool found)
        {
            string status = found ? "✓" : "✗ MISSING";
            Debug.Log($"  [{status}] {component}");
        }
    }
}
