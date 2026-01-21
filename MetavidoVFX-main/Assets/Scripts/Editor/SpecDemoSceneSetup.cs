// SpecDemoSceneSetup.cs - Editor utility to create demo scenes for each spec
// Creates minimal scenes demonstrating the functionality of each specification

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

namespace MetavidoVFX.Editor
{
    /// <summary>
    /// Editor utility to create demo scenes for each specification.
    /// Menu: H3M > Spec Demos > Create [SpecName] Demo Scene
    /// </summary>
    public static class SpecDemoSceneSetup
    {
        private const string DemoScenePath = "Assets/Scenes/SpecDemos";

        #region Menu Items

        [MenuItem("H3M/Spec Demos/Wire All Demo Scenes", priority = 1)]
        public static void WireAllDemoScenes()
        {
            var sceneFiles = new[]
            {
                "Spec002_H3M_Foundation",
                "Spec003_Hologram_Conferencing",
                "Spec004_MetavidoVFX_Systems",
                "Spec005_AR_Texture_Safety",
                "Spec006_VFX_Library_Pipeline",
                "Spec008_ML_Foundations",
                "Spec009_Icosa_Sketchfab",
                "Spec012_Hand_Tracking"
            };

            int wiredCount = 0;
            foreach (var sceneName in sceneFiles)
            {
                string scenePath = $"{DemoScenePath}/{sceneName}.unity";
                if (!System.IO.File.Exists(scenePath.Replace("Assets/", "").Insert(0, Application.dataPath + "/")))
                {
                    Debug.LogWarning($"[SpecDemo] Scene not found: {scenePath}");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                WireSceneComponents(scene, sceneName);
                EditorSceneManager.SaveScene(scene);
                wiredCount++;
            }

            Debug.Log($"[SpecDemo] Wired {wiredCount} demo scenes successfully!");
        }

        private static void WireSceneComponents(Scene scene, string sceneName)
        {
            // Find key components
            var arDepthSource = Object.FindAnyObjectByType<ARDepthSource>();
            var audioBridge = Object.FindAnyObjectByType<AudioBridge>();
            var xrOrigin = FindXROrigin();
            var mainCamera = Camera.main;

            // 1. Wire XROrigin camera reference
            if (xrOrigin != null && mainCamera != null)
            {
                var cameraProperty = xrOrigin.GetType().GetProperty("Camera");
                if (cameraProperty != null && cameraProperty.CanWrite)
                {
                    cameraProperty.SetValue(xrOrigin, mainCamera);
                    Debug.Log($"[SpecDemo] Wired XROrigin.Camera in {sceneName}");
                }
            }

            // 2. Connect all VFXARBinders to ARDepthSource
            var vfxBinders = Object.FindObjectsByType<VFXARBinder>(FindObjectsSortMode.None);
            foreach (var binder in vfxBinders)
            {
                // VFXARBinder auto-finds ARDepthSource via singleton, but verify it exists
                if (arDepthSource == null)
                {
                    // Create ARDepthSource if missing
                    var depthGO = new GameObject("ARDepthSource");
                    SceneManager.MoveGameObjectToScene(depthGO, scene);
                    arDepthSource = depthGO.AddComponent<ARDepthSource>();
                    Debug.Log($"[SpecDemo] Created ARDepthSource in {sceneName}");
                }
                EditorUtility.SetDirty(binder);
            }

            // 3. Load appropriate VFX assets for VisualEffect components without assets
            var vfxComponents = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            foreach (var vfx in vfxComponents)
            {
                if (vfx.visualEffectAsset == null)
                {
                    var vfxAsset = GetVFXAssetForScene(sceneName, vfx.gameObject.name);
                    if (vfxAsset != null)
                    {
                        vfx.visualEffectAsset = vfxAsset;
                        Debug.Log($"[SpecDemo] Assigned VFX '{vfxAsset.name}' to '{vfx.gameObject.name}' in {sceneName}");
                        EditorUtility.SetDirty(vfx);
                    }
                }
            }

            // 4. Wire AudioBridge to AudioSource
            if (audioBridge != null)
            {
                var audioSource = audioBridge.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = audioBridge.gameObject.AddComponent<AudioSource>();
                }

                // Use reflection to set the audioSource field
                var audioSourceField = typeof(AudioBridge).GetField("audioSource",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (audioSourceField != null)
                {
                    audioSourceField.SetValue(audioBridge, audioSource);
                    Debug.Log($"[SpecDemo] Wired AudioBridge.audioSource in {sceneName}");
                    EditorUtility.SetDirty(audioBridge);
                }
            }

            // 5. Scene-specific wiring
            WireSceneSpecificComponents(scene, sceneName, arDepthSource, mainCamera);
        }

        private static Component FindXROrigin()
        {
            var originType = System.Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
            if (originType != null)
            {
                return Object.FindAnyObjectByType(originType) as Component;
            }
            return null;
        }

        private static VisualEffectAsset GetVFXAssetForScene(string sceneName, string vfxObjectName)
        {
            // Map scene names to appropriate VFX assets
            // VFX naming convention: {name}_{datatype}_{category}_{source}.vfx
            VisualEffectAsset asset = null;

            switch (sceneName)
            {
                case "Spec002_H3M_Foundation":
                    asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/H3M/VFX/hologram_depth_people_metavido.vfx");
                    if (asset == null) asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/particles_depth_people_metavido.vfx");
                    break;

                case "Spec003_Hologram_Conferencing":
                    if (vfxObjectName.Contains("Local"))
                        asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/particles_depth_people_metavido.vfx");
                    else if (vfxObjectName.Contains("Remote"))
                        asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/glitch_depth_people_metavido.vfx");
                    else
                        asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/pointcloud_depth_people_metavido.vfx");
                    break;

                case "Spec004_MetavidoVFX_Systems":
                    asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/bodyparticles_depth_people_metavido.vfx");
                    break;

                case "Spec006_VFX_Library_Pipeline":
                    asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/H3M/VFX/hologram_depth_people_metavido.vfx");
                    break;

                case "Spec012_Hand_Tracking":
                    asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/NNCam2/particle_any_nncam2.vfx");
                    break;

                default:
                    // Try to find any suitable VFX
                    asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/particles_depth_people_metavido.vfx");
                    break;
            }

            // Fallback: try common paths
            if (asset == null)
            {
                var fallbackPaths = new[]
                {
                    "Assets/H3M/VFX/hologram_depth_people_metavido.vfx",
                    "Assets/VFX/particles_depth_people_metavido.vfx",
                    "Assets/VFX/pointcloud_depth_people_metavido.vfx"
                };

                foreach (var path in fallbackPaths)
                {
                    asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                    if (asset != null) break;
                }
            }

            return asset;
        }

        private static void WireSceneSpecificComponents(Scene scene, string sceneName, ARDepthSource arDepthSource, Camera mainCamera)
        {
            switch (sceneName)
            {
                case "Spec005_AR_Texture_Safety":
                    // Ensure mock data flag is set for Editor testing
                    if (arDepthSource != null)
                    {
                        var useMockField = typeof(ARDepthSource).GetField("useMockDataInEditor",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (useMockField != null)
                        {
                            useMockField.SetValue(arDepthSource, true);
                            EditorUtility.SetDirty(arDepthSource);
                        }
                    }
                    break;

                case "Spec006_VFX_Library_Pipeline":
                    // Ensure VFXLibraryManager has populated VFX list
                    var libraryMgr = Object.FindAnyObjectByType<MetavidoVFX.VFX.VFXLibraryManager>();
                    if (libraryMgr != null)
                    {
                        // Trigger population via reflection or mark dirty
                        EditorUtility.SetDirty(libraryMgr);
                    }
                    break;

                case "Spec008_ML_Foundations":
                case "Spec012_Hand_Tracking":
                    // Ensure hand tracking components are properly linked
                    // HandTrackingProviderManager auto-discovers, just mark dirty
                    var htpmType = System.Type.GetType("MetavidoVFX.HandTracking.HandTrackingProviderManager, Assembly-CSharp");
                    if (htpmType != null)
                    {
                        var htpm = Object.FindAnyObjectByType(htpmType);
                        if (htpm != null)
                        {
                            EditorUtility.SetDirty(htpm as Component);
                        }
                    }
                    break;
            }
        }

        [MenuItem("H3M/Spec Demos/Create All Demo Scenes", priority = 0)]
        public static void CreateAllDemoScenes()
        {
            if (!EditorUtility.DisplayDialog("Create All Demo Scenes",
                "This will create demo scenes for all specs. Continue?", "Create All", "Cancel"))
                return;

            EnsureDemoFolderExists();

            CreateSpec002Scene();
            CreateSpec003Scene();
            CreateSpec004Scene();
            CreateSpec005Scene();
            CreateSpec006Scene();
            CreateSpec008Scene();
            CreateSpec009Scene();
            CreateSpec012Scene();

            Debug.Log("[SpecDemo] All demo scenes created successfully!");
            AssetDatabase.Refresh();
        }

        [MenuItem("H3M/Spec Demos/002 - H3M Foundation Demo")]
        public static void CreateSpec002Scene()
        {
            EnsureDemoFolderExists();

            var scene = CreateBaseScene("Spec002_H3M_Foundation");

            // Add AR Session Origin
            CreateARSessionOrigin(scene);

            // Add Hologram prefab
            var hologramPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/H3M/Prefabs/H3M_HologramRig.prefab");
            if (hologramPrefab != null)
            {
                var hologram = (GameObject)PrefabUtility.InstantiatePrefab(hologramPrefab, scene);
                hologram.name = "HologramRig";
            }
            else
            {
                // Create minimal hologram setup
                var hologramRoot = new GameObject("Hologram");
                SceneManager.MoveGameObjectToScene(hologramRoot, scene);

                var hologramVFX = new GameObject("HologramVFX");
                hologramVFX.transform.SetParent(hologramRoot.transform);
                var vfx = hologramVFX.AddComponent<VisualEffect>();

                // Try to load a VFX asset
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/H3M/VFX/hologram_depth_people_metavido.vfx");
                if (vfxAsset == null) vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/particles_depth_people_metavido.vfx");
                if (vfxAsset != null) vfx.visualEffectAsset = vfxAsset;

                hologramVFX.AddComponent<VFXARBinder>();
            }

            // Add HologramSource
            var sourceGO = new GameObject("HologramSource");
            SceneManager.MoveGameObjectToScene(sourceGO, scene);
            var hologramSourceType = System.Type.GetType("H3M.Core.HologramSource, Assembly-CSharp");
            if (hologramSourceType != null)
            {
                sourceGO.AddComponent(hologramSourceType);
            }

            // Add info display
            AddDemoInfoUI(scene, "Spec 002: H3M Foundation Demo",
                "Demonstrates basic hologram rendering with depth-to-world conversion.\n" +
                "- HologramSource: Depth texture → Position map\n" +
                "- HologramVFX: Renders particles at world positions");

            SaveAndCloseDemoScene(scene, "Spec002_H3M_Foundation");
        }

        [MenuItem("H3M/Spec Demos/003 - Hologram Conferencing Demo")]
        public static void CreateSpec003Scene()
        {
            EnsureDemoFolderExists();

            var scene = CreateBaseScene("Spec003_Hologram_Conferencing");

            CreateARSessionOrigin(scene);

            // Add Recording system placeholder
            var recordingSystem = new GameObject("HologramRecordingSystem");
            SceneManager.MoveGameObjectToScene(recordingSystem, scene);

            // Recorder component
            var recorder = new GameObject("HologramRecorder");
            recorder.transform.SetParent(recordingSystem.transform);

            // Player component
            var player = new GameObject("HologramPlayer");
            player.transform.SetParent(recordingSystem.transform);

            // Add WebRTC setup for conferencing
            var webrtcRoot = new GameObject("WebRTC");
            SceneManager.MoveGameObjectToScene(webrtcRoot, scene);

            var signalingClient = new GameObject("SignalingClient");
            signalingClient.transform.SetParent(webrtcRoot.transform);

            var receiver = new GameObject("WebRTCReceiver");
            receiver.transform.SetParent(webrtcRoot.transform);

            // Add local hologram
            var localHologram = new GameObject("LocalHologram");
            SceneManager.MoveGameObjectToScene(localHologram, scene);
            var localVFX = localHologram.AddComponent<VisualEffect>();
            localHologram.AddComponent<VFXARBinder>();

            // Add remote hologram placeholder
            var remoteHologram = new GameObject("RemoteHologram");
            SceneManager.MoveGameObjectToScene(remoteHologram, scene);
            var remoteVFX = remoteHologram.AddComponent<VisualEffect>();
            remoteHologram.SetActive(false); // Activated when remote stream connects

            // Add info display
            AddDemoInfoUI(scene, "Spec 003: Hologram Conferencing Demo",
                "Demonstrates hologram recording, playback & multiplayer.\n" +
                "- HologramRecorder: Captures depth + color frames\n" +
                "- HologramPlayer: Plays back .hologram files\n" +
                "- WebRTC: P2P video streaming for conferencing\n" +
                "- Multiplayer: See remote users as holograms");

            SaveAndCloseDemoScene(scene, "Spec003_Hologram_Conferencing");
        }

        [MenuItem("H3M/Spec Demos/004 - MetavidoVFX Systems Demo")]
        public static void CreateSpec004Scene()
        {
            EnsureDemoFolderExists();

            var scene = CreateBaseScene("Spec004_MetavidoVFX_Systems");

            CreateARSessionOrigin(scene);

            // Add EchoVision setup
            var echovision = new GameObject("EchoVision");
            SceneManager.MoveGameObjectToScene(echovision, scene);

            // MeshVFX for AR mesh visualization
            var meshVFX = new GameObject("MeshVFX");
            meshVFX.transform.SetParent(echovision.transform);
            // MeshVFX component would be added here if available

            // Sound wave emitter
            var soundWave = new GameObject("SoundWaveEmitter");
            soundWave.transform.SetParent(echovision.transform);
            // SoundWaveEmitter component would be added here

            // Hand tracking VFX
            var handVFX = new GameObject("HandVFX");
            SceneManager.MoveGameObjectToScene(handVFX, scene);
            var vfx = handVFX.AddComponent<VisualEffect>();
            handVFX.AddComponent<VFXARBinder>();

            // Add info display
            AddDemoInfoUI(scene, "Spec 004: MetavidoVFX Systems Demo",
                "Demonstrates hand tracking + audio-reactive VFX.\n" +
                "- HandVFXController: Hand position → VFX particles\n" +
                "- AudioBridge: FFT frequency bands → global shader properties\n" +
                "- EchoVision: AR mesh → GraphicsBuffers");

            SaveAndCloseDemoScene(scene, "Spec004_MetavidoVFX_Systems");
        }

        [MenuItem("H3M/Spec Demos/005 - AR Texture Safety Demo")]
        public static void CreateSpec005Scene()
        {
            EnsureDemoFolderExists();

            var scene = CreateBaseScene("Spec005_AR_Texture_Safety");

            CreateARSessionOrigin(scene);

            // Add ARDepthSource with mock data enabled
            var depthSource = new GameObject("ARDepthSource");
            SceneManager.MoveGameObjectToScene(depthSource, scene);
            var ards = depthSource.AddComponent<ARDepthSource>();
            // Mock data will be enabled automatically in Editor

            // Add diagnostic display
            var diagnostic = new GameObject("DiagnosticOverlay");
            SceneManager.MoveGameObjectToScene(diagnostic, scene);
            var diagType = System.Type.GetType("MetavidoVFX.Debug.DiagnosticOverlay, Assembly-CSharp");
            if (diagType != null)
            {
                diagnostic.AddComponent(diagType);
            }

            // Add info display
            AddDemoInfoUI(scene, "Spec 005: AR Texture Safety Demo",
                "Demonstrates TryGetTexture pattern for safe AR texture access.\n" +
                "- ARDepthSource: Uses try/catch to avoid NullReferenceException\n" +
                "- Mock data: Works in Editor without AR device\n" +
                "- Graceful fallback when AR subsystem not ready");

            SaveAndCloseDemoScene(scene, "Spec005_AR_Texture_Safety");
        }

        [MenuItem("H3M/Spec Demos/006 - VFX Library Pipeline Demo")]
        public static void CreateSpec006Scene()
        {
            EnsureDemoFolderExists();

            var scene = CreateBaseScene("Spec006_VFX_Library_Pipeline");

            CreateARSessionOrigin(scene);

            // Add ARDepthSource
            var depthSource = new GameObject("ARDepthSource");
            SceneManager.MoveGameObjectToScene(depthSource, scene);
            depthSource.AddComponent<ARDepthSource>();

            // Add VFX Library Manager
            var libraryRoot = new GameObject("VFX_Library");
            SceneManager.MoveGameObjectToScene(libraryRoot, scene);
            var library = libraryRoot.AddComponent<MetavidoVFX.VFX.VFXLibraryManager>();

            // Add VFXToggleUI
            var uiGO = new GameObject("VFXToggleUI");
            SceneManager.MoveGameObjectToScene(uiGO, scene);
            var uiDoc = uiGO.AddComponent<UnityEngine.UIElements.UIDocument>();
            var toggleUI = uiGO.AddComponent<MetavidoVFX.UI.VFXToggleUI>();

            // Add Pipeline Dashboard (global namespace)
            var dashboardGO = new GameObject("VFXPipelineDashboard");
            SceneManager.MoveGameObjectToScene(dashboardGO, scene);
            var dashboardType = System.Type.GetType("VFXPipelineDashboard, Assembly-CSharp");
            if (dashboardType != null)
            {
                dashboardGO.AddComponent(dashboardType);
            }

            // Add Test Harness (global namespace)
            var harnessGO = new GameObject("VFXTestHarness");
            SceneManager.MoveGameObjectToScene(harnessGO, scene);
            var harnessType = System.Type.GetType("VFXTestHarness, Assembly-CSharp");
            if (harnessType != null)
            {
                harnessGO.AddComponent(harnessType);
            }

            // Add info display
            AddDemoInfoUI(scene, "Spec 006: VFX Library Pipeline Demo",
                "Demonstrates Hybrid Bridge Pipeline architecture.\n" +
                "- ARDepthSource: ONE compute dispatch for all VFX\n" +
                "- VFXARBinder: Lightweight per-VFX binding (no compute)\n" +
                "- VFXLibraryManager: Category-based VFX organization\n" +
                "- Press TAB for Pipeline Dashboard, 1-9 for VFX selection");

            SaveAndCloseDemoScene(scene, "Spec006_VFX_Library_Pipeline");
        }

        [MenuItem("H3M/Spec Demos/008 - ML Foundations Demo")]
        public static void CreateSpec008Scene()
        {
            EnsureDemoFolderExists();

            var scene = CreateBaseScene("Spec008_ML_Foundations");

            CreateARSessionOrigin(scene);

            // Add tracking provider manager
            var trackingMgr = new GameObject("TrackingProviderManager");
            SceneManager.MoveGameObjectToScene(trackingMgr, scene);

            // Try to add HandTrackingProviderManager if it exists
            var htpmType = System.Type.GetType("MetavidoVFX.HandTracking.HandTrackingProviderManager, Assembly-CSharp");
            if (htpmType != null)
            {
                trackingMgr.AddComponent(htpmType);
            }

            // Add ARDepthSource for depth tracking
            var depthSource = new GameObject("ARDepthSource");
            SceneManager.MoveGameObjectToScene(depthSource, scene);
            depthSource.AddComponent<ARDepthSource>();

            // Add audio provider setup
            var audioProvider = new GameObject("AudioProvider");
            SceneManager.MoveGameObjectToScene(audioProvider, scene);
            audioProvider.AddComponent<AudioSource>();
            audioProvider.AddComponent<AudioBridge>();

            // Add info display
            AddDemoInfoUI(scene, "Spec 008: ML Foundations Demo",
                "Demonstrates ITrackingProvider abstraction layer.\n" +
                "- HandTrackingProviderManager: Unified hand tracking API\n" +
                "- IHandTrackingProvider: Platform-agnostic interface\n" +
                "- Priority chain: HoloKit → XRHands → BodyPix → Touch\n" +
                "- ARDepthSource: Depth provider for people segmentation");

            SaveAndCloseDemoScene(scene, "Spec008_ML_Foundations");
        }

        [MenuItem("H3M/Spec Demos/009 - Icosa Sketchfab Demo")]
        public static void CreateSpec009Scene()
        {
            EnsureDemoFolderExists();

            var scene = CreateBaseScene("Spec009_Icosa_Sketchfab");

            CreateARSessionOrigin(scene);

            // Add model search system
            var modelSearchRoot = new GameObject("ModelSearchSystem");
            SceneManager.MoveGameObjectToScene(modelSearchRoot, scene);

            // Add WhisperIcosaController placeholder
            var voiceController = new GameObject("WhisperIcosaController");
            voiceController.transform.SetParent(modelSearchRoot.transform);
            voiceController.AddComponent<AudioSource>(); // For voice input

            // Add model loader
            var modelLoader = new GameObject("IcosaAssetLoader");
            modelLoader.transform.SetParent(modelSearchRoot.transform);

            // Add placement system
            var placementSystem = new GameObject("ModelPlacementSystem");
            SceneManager.MoveGameObjectToScene(placementSystem, scene);

            // Add preview sphere placeholder
            var previewSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            previewSphere.name = "ModelPreviewPlaceholder";
            previewSphere.transform.localScale = Vector3.one * 0.3f;
            previewSphere.transform.position = new Vector3(0, 1, 2);
            SceneManager.MoveGameObjectToScene(previewSphere, scene);
            var previewMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            previewMat.color = new Color(0.3f, 0.6f, 1f, 0.5f);
            previewSphere.GetComponent<Renderer>().material = previewMat;

            // Add info display
            AddDemoInfoUI(scene, "Spec 009: Icosa/Sketchfab Demo",
                "Demonstrates voice-to-object 3D model integration.\n" +
                "- WhisperIcosaController: Voice command → model search\n" +
                "- IcosaAssetLoader: glTF model import at runtime\n" +
                "- SketchfabClient: Sketchfab Download API wrapper\n" +
                "- ModelCache: LRU disk caching for downloaded models\n" +
                "- Say 'Put a cat here' to search and place models");

            SaveAndCloseDemoScene(scene, "Spec009_Icosa_Sketchfab");
        }

        [MenuItem("H3M/Spec Demos/012 - Hand Tracking Demo")]
        public static void CreateSpec012Scene()
        {
            EnsureDemoFolderExists();

            var scene = CreateBaseScene("Spec012_Hand_Tracking");

            CreateARSessionOrigin(scene);

            // Add hand tracking provider manager
            var handMgr = new GameObject("HandTrackingProviderManager");
            SceneManager.MoveGameObjectToScene(handMgr, scene);
            var htpmType = System.Type.GetType("MetavidoVFX.HandTracking.HandTrackingProviderManager, Assembly-CSharp");
            if (htpmType != null)
            {
                handMgr.AddComponent(htpmType);
            }

            // Add VFX with hand binder
            var handVFX = new GameObject("HandVFX");
            SceneManager.MoveGameObjectToScene(handVFX, scene);
            var vfx = handVFX.AddComponent<VisualEffect>();

            // Try to add VFXHandBinder
            var handBinderType = System.Type.GetType("MetavidoVFX.VFX.Binders.VFXHandBinder, Assembly-CSharp");
            if (handBinderType != null)
            {
                handVFX.AddComponent(handBinderType);
            }

            // Add touch fallback indicator
            var touchIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            touchIndicator.name = "TouchPositionIndicator";
            touchIndicator.transform.localScale = Vector3.one * 0.05f;
            SceneManager.MoveGameObjectToScene(touchIndicator, scene);
            var touchMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            touchMat.color = Color.cyan;
            touchIndicator.GetComponent<Renderer>().material = touchMat;
            touchIndicator.SetActive(false); // Hidden until touch input

            // Add info display
            AddDemoInfoUI(scene, "Spec 012: Hand Tracking Demo",
                "Demonstrates unified hand tracking with platform fallbacks.\n" +
                "- HandTrackingProviderManager: Auto-discovers providers\n" +
                "- VFXHandBinder: Binds hand data to VFX properties\n" +
                "- Touch fallback: Works in Editor with touch/mouse\n" +
                "- 26 joints: Wrist + 5 fingers × 5 joints each");

            SaveAndCloseDemoScene(scene, "Spec012_Hand_Tracking");
        }

        #endregion

        #region Helper Methods

        private static void EnsureDemoFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            if (!AssetDatabase.IsValidFolder(DemoScenePath))
            {
                AssetDatabase.CreateFolder("Assets/Scenes", "SpecDemos");
            }
        }

        private static Scene CreateBaseScene(string sceneName)
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add Main Camera with URP
            var cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            var camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            cameraGO.AddComponent<UniversalAdditionalCameraData>();
            cameraGO.transform.position = new Vector3(0, 1.5f, -2f);
            SceneManager.MoveGameObjectToScene(cameraGO, scene);

            // Add Directional Light
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            lightGO.AddComponent<UniversalAdditionalLightData>();
            SceneManager.MoveGameObjectToScene(lightGO, scene);

            // Add Volume for post-processing
            var volumeGO = new GameObject("Global Volume");
            var volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            SceneManager.MoveGameObjectToScene(volumeGO, scene);

            return scene;
        }

        private static void CreateARSessionOrigin(Scene scene)
        {
            // Create AR Session
            var sessionGO = new GameObject("AR Session");
            SceneManager.MoveGameObjectToScene(sessionGO, scene);

            var sessionType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARSession, Unity.XR.ARFoundation");
            if (sessionType != null)
            {
                sessionGO.AddComponent(sessionType);
            }

            // Create XR Origin
            var originGO = new GameObject("XR Origin");
            SceneManager.MoveGameObjectToScene(originGO, scene);

            var originType = System.Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
            if (originType != null)
            {
                originGO.AddComponent(originType);
            }

            // Add Camera Offset
            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(originGO.transform);
            cameraOffset.transform.localPosition = Vector3.zero;

            // Move main camera under offset
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.SetParent(cameraOffset.transform);
                mainCamera.transform.localPosition = Vector3.zero;
                mainCamera.transform.localRotation = Quaternion.identity;

                // Add AR Camera Manager and Background
                var arCamMgrType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARCameraManager, Unity.XR.ARFoundation");
                var arCamBgType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARCameraBackground, Unity.XR.ARFoundation");

                if (arCamMgrType != null) mainCamera.gameObject.AddComponent(arCamMgrType);
                if (arCamBgType != null) mainCamera.gameObject.AddComponent(arCamBgType);

                // Add occlusion manager
                var arOccType = System.Type.GetType("UnityEngine.XR.ARFoundation.AROcclusionManager, Unity.XR.ARFoundation");
                if (arOccType != null) mainCamera.gameObject.AddComponent(arOccType);
            }

            // Add AR Raycast Manager
            var raycastType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARRaycastManager, Unity.XR.ARFoundation");
            if (raycastType != null)
            {
                originGO.AddComponent(raycastType);
            }

            // Add AR Plane Manager
            var planeType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARPlaneManager, Unity.XR.ARFoundation");
            if (planeType != null)
            {
                originGO.AddComponent(planeType);
            }
        }

        private static void AddDemoInfoUI(Scene scene, string title, string description)
        {
            // Create a simple world-space info canvas
            var canvasGO = new GameObject("DemoInfoCanvas");
            SceneManager.MoveGameObjectToScene(canvasGO, scene);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var rectTransform = canvasGO.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(800, 400);
            rectTransform.localScale = Vector3.one * 0.002f;
            rectTransform.position = new Vector3(0, 2f, 3f);

            // Add background panel
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(canvasGO.transform);
            var panelImage = panelGO.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Add title text
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panelGO.transform);
            var titleText = titleGO.AddComponent<UnityEngine.UI.Text>();
            titleText.text = title;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.UpperCenter;
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.75f);
            titleRect.anchorMax = new Vector2(1, 0.95f);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = new Vector2(-20, 0);

            // Add description text
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(panelGO.transform);
            var descText = descGO.AddComponent<UnityEngine.UI.Text>();
            descText.text = description;
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = 18;
            descText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            descText.alignment = TextAnchor.UpperLeft;
            var descRect = descGO.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0.05f);
            descRect.anchorMax = new Vector2(1, 0.72f);
            descRect.offsetMin = new Vector2(30, 0);
            descRect.offsetMax = new Vector2(-30, 0);
        }

        private static void SaveAndCloseDemoScene(Scene scene, string sceneName)
        {
            string path = $"{DemoScenePath}/{sceneName}.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[SpecDemo] Created demo scene: {path}");
        }

        #endregion
    }
}
