// EchoVision Scene Setup - Migrates EchoVision components to main scene
// Menu: H3M > EchoVision > Setup EchoVision Components

using UnityEngine;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;
using UnityEngine.Rendering;
using UnityEngine.InputSystem.XR;
using Audio = MetavidoVFX.Audio;
using VFX = MetavidoVFX.VFX;

namespace MetavidoVFX.Editor
{
    public static class EchovisionSetup
    {
        [MenuItem("H3M/EchoVision/Setup All EchoVision Components")]
        public static void SetupAllEchovisionComponents()
        {
            Debug.Log("[EchoVision Setup] Setting up EchoVision components...");

            // 1. Setup AR Session
            EnsureARSession();

            // 2. Setup AR Mesh Manager
            SetupARMeshManager();

            // 3. Setup Audio Input
            SetupAudioInput();

            // 4. Setup MeshVFX
            SetupMeshVFX();

            // 5. Setup Post-Processing
            PostProcessingSetup.SetupPostProcessing();

            // 6. Enable camera post-processing
            PostProcessingSetup.EnableCameraPostProcessing();

            Debug.Log("[EchoVision Setup] ✓ Complete! All EchoVision components set up.");
        }

        [MenuItem("H3M/EchoVision/Setup AR Mesh Manager")]
        public static void SetupARMeshManager()
        {
            var meshManager = Object.FindFirstObjectByType<ARMeshManager>();
            if (meshManager != null)
            {
                Debug.Log("[EchoVision Setup] ✓ ARMeshManager already exists");
                return;
            }

            // Find XR Origin or create mesh manager object
            var xrOrigin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            GameObject parent = xrOrigin != null ? xrOrigin.gameObject : null;

            var meshManagerObj = new GameObject("MeshManager");
            if (parent != null)
            {
                meshManagerObj.transform.SetParent(parent.transform);
            }

            meshManager = meshManagerObj.AddComponent<ARMeshManager>();
            Undo.RegisterCreatedObjectUndo(meshManagerObj, "Create ARMeshManager");

            Debug.Log("[EchoVision Setup] ✓ Created ARMeshManager");
        }

        [MenuItem("H3M/EchoVision/Setup Audio Input")]
        public static void SetupAudioInput()
        {
            // Check if AudioProcessor exists (legacy)
            var audioProcessor = Object.FindFirstObjectByType<AudioProcessor>();
            if (audioProcessor != null)
            {
                Debug.Log("[EchoVision Setup] ✓ AudioProcessor (legacy) already exists");
            }
            else
            {
                var audioObj = new GameObject("AudioInput");
                audioProcessor = audioObj.AddComponent<AudioProcessor>();

                // Add microphone if available
                var micAPI = audioObj.AddComponent<MicrophoneAPI>();

                Undo.RegisterCreatedObjectUndo(audioObj, "Create AudioInput");
                Debug.Log("[EchoVision Setup] ✓ Created AudioProcessor with MicrophoneAPI");
            }

            // Also check/add EnhancedAudioProcessor for VFXBinderManager compatibility
            var enhancedAudio = Object.FindFirstObjectByType<Audio.EnhancedAudioProcessor>();
            if (enhancedAudio == null && audioProcessor != null)
            {
                enhancedAudio = audioProcessor.gameObject.AddComponent<Audio.EnhancedAudioProcessor>();
                Undo.RegisterCreatedObjectUndo(enhancedAudio, "Add EnhancedAudioProcessor");
                Debug.Log("[EchoVision Setup] ✓ Added EnhancedAudioProcessor for VFXBinderManager");
            }
            else if (enhancedAudio != null)
            {
                Debug.Log("[EchoVision Setup] ✓ EnhancedAudioProcessor already exists");
            }

            // Check if SoundWaveEmitter exists
            var soundWaveEmitter = Object.FindFirstObjectByType<SoundWaveEmitter>();
            if (soundWaveEmitter != null)
            {
                Debug.Log("[EchoVision Setup] ✓ SoundWaveEmitter already exists");
                return;
            }

            // Create SoundWaveEmitter
            var emitterObj = new GameObject("SoundWaveEmitter");
            soundWaveEmitter = emitterObj.AddComponent<SoundWaveEmitter>();
            Undo.RegisterCreatedObjectUndo(emitterObj, "Create SoundWaveEmitter");

            // Wire up references
            SerializedObject so = new SerializedObject(soundWaveEmitter);

            // Find TrackedPoseDriver
            var trackedPoseDriver = Object.FindFirstObjectByType<TrackedPoseDriver>();
            if (trackedPoseDriver != null)
            {
                so.FindProperty("trackedPoseDriver").objectReferenceValue = trackedPoseDriver;
            }

            // Find AudioProcessor
            so.FindProperty("audioProcessor").objectReferenceValue = audioProcessor;

            // Find DepthImageProcessor
            var depthProcessor = Object.FindFirstObjectByType<DepthImageProcessor>();
            if (depthProcessor != null)
            {
                so.FindProperty("depthImageProcessor").objectReferenceValue = depthProcessor;
            }

            // Find VFX
            var vfx = Object.FindFirstObjectByType<VisualEffect>();
            if (vfx != null)
            {
                so.FindProperty("vfx").objectReferenceValue = vfx;
            }

            so.ApplyModifiedProperties();

            Debug.Log("[EchoVision Setup] ✓ Created SoundWaveEmitter with references");
        }

        [MenuItem("H3M/EchoVision/Setup MeshVFX")]
        public static void SetupMeshVFX()
        {
            var meshVFX = Object.FindFirstObjectByType<Echovision.MeshVFX>();
            if (meshVFX != null)
            {
                Debug.Log("[EchoVision Setup] ✓ MeshVFX already exists");
                return;
            }

            // Create MeshVFX object
            var meshVFXObj = new GameObject("MeshVFX");
            meshVFX = meshVFXObj.AddComponent<Echovision.MeshVFX>();
            Undo.RegisterCreatedObjectUndo(meshVFXObj, "Create MeshVFX");

            // Wire up references
            SerializedObject so = new SerializedObject(meshVFX);

            // Find ARMeshManager
            var meshManager = Object.FindFirstObjectByType<ARMeshManager>();
            if (meshManager != null)
            {
                so.FindProperty("meshManager").objectReferenceValue = meshManager;
            }

            // Find TrackedPoseDriver
            var trackedPoseDriver = Object.FindFirstObjectByType<TrackedPoseDriver>();
            if (trackedPoseDriver != null)
            {
                so.FindProperty("trackedPoseDriver").objectReferenceValue = trackedPoseDriver;
            }

            // Find VFX
            var vfx = Object.FindFirstObjectByType<VisualEffect>();
            if (vfx != null)
            {
                so.FindProperty("vfx").objectReferenceValue = vfx;
            }

            so.ApplyModifiedProperties();

            Debug.Log("[EchoVision Setup] ✓ Created MeshVFX with references");
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
                Debug.Log("[EchoVision Setup] ✓ Created AR Session");
            }
            else
            {
                Debug.Log("[EchoVision Setup] ✓ AR Session exists");
            }
        }

        [MenuItem("H3M/EchoVision/Validate Setup")]
        public static void ValidateSetup()
        {
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("   EchoVision Component Validation");
            Debug.Log("═══════════════════════════════════════════════════════════");

            LogComponent<ARSession>("ARSession");
            LogComponent<ARCameraManager>("ARCameraManager");
            LogComponent<AROcclusionManager>("AROcclusionManager");
            LogComponent<ARMeshManager>("ARMeshManager");
            LogComponent<TrackedPoseDriver>("TrackedPoseDriver");
            LogComponent<AudioProcessor>("AudioProcessor (Legacy)");
            LogComponent<Audio.EnhancedAudioProcessor>("EnhancedAudioProcessor");
            LogComponent<SoundWaveEmitter>("SoundWaveEmitter");
            LogComponent<DepthImageProcessor>("DepthImageProcessor");
            LogComponent<Echovision.MeshVFX>("MeshVFX");
            LogComponent<VFX.VFXBinderManager>("VFXBinderManager");

            // Check for VFX
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            Debug.Log($"  VisualEffects in scene: {allVFX.Length}");

            // Check for Volume
            var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            int globalCount = 0;
            foreach (var v in volumes)
            {
                if (v.isGlobal) globalCount++;
            }
            Debug.Log($"  Volumes: {volumes.Length} total, {globalCount} global");

            Debug.Log("═══════════════════════════════════════════════════════════");
        }

        static void LogComponent<T>(string name) where T : Component
        {
            var component = Object.FindFirstObjectByType<T>();
            string status = component != null ? "✓" : "✗";
            string objName = component != null ? $" ({component.gameObject.name})" : "";
            Debug.Log($"  [{status}] {name}{objName}");
        }

        [MenuItem("H3M/EchoVision/Setup DepthImageProcessor")]
        public static void SetupDepthImageProcessor()
        {
            var depthProcessor = Object.FindFirstObjectByType<DepthImageProcessor>();
            if (depthProcessor != null)
            {
                Debug.Log("[EchoVision Setup] ✓ DepthImageProcessor already exists");
                return;
            }

            // Find camera with AROcclusionManager
            var occlusionManager = Object.FindFirstObjectByType<AROcclusionManager>();
            GameObject parent = occlusionManager != null ? occlusionManager.gameObject : null;

            if (parent == null)
            {
                Debug.LogWarning("[EchoVision Setup] No AROcclusionManager found - create one first");
                return;
            }

            depthProcessor = parent.AddComponent<DepthImageProcessor>();
            Undo.RegisterCreatedObjectUndo(depthProcessor, "Add DepthImageProcessor");

            // Wire up AROcclusionManager
            SerializedObject so = new SerializedObject(depthProcessor);
            var occlusionProp = so.FindProperty("occlusionManager");
            if (occlusionProp != null)
            {
                occlusionProp.objectReferenceValue = occlusionManager;
                so.ApplyModifiedProperties();
            }

            Debug.Log("[EchoVision Setup] ✓ Created DepthImageProcessor");
        }

        [MenuItem("H3M/EchoVision/Copy VFX from EchoVision Scene")]
        public static void CopyVFXFromEchovision()
        {
            // Load EchoVision VFX assets
            string[] echoVFXPaths = new[]
            {
                "Assets/Echovision/VFX",
                "Assets/Echovision/Effects"
            };

            int copied = 0;
            foreach (string path in echoVFXPaths)
            {
                string[] guids = AssetDatabase.FindAssets("t:VisualEffectAsset", new[] { path });
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = System.IO.Path.GetFileName(assetPath);
                    string destPath = $"Assets/Resources/VFX/{fileName}";

                    if (!System.IO.File.Exists(destPath))
                    {
                        AssetDatabase.CopyAsset(assetPath, destPath);
                        copied++;
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[EchoVision Setup] Copied {copied} VFX assets to Resources/VFX");
        }
    }
}
