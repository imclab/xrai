// InitializeOnLoad script to automatically wire HandVFXController
// This runs once when Unity loads/recompiles

using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;

namespace MetavidoVFX.Editor
{
    [InitializeOnLoad]
    public static class HandVFXInitialSetup
    {
        static HandVFXInitialSetup()
        {
            // Delay execution to ensure scene is fully loaded
            EditorApplication.delayCall += OnDelayedInit;
        }

        static void OnDelayedInit()
        {
            // Only run once
            EditorApplication.delayCall -= OnDelayedInit;

            // Check if we need to setup
            var controller = Object.FindFirstObjectByType<HandTracking.HandVFXController>();
            if (controller == null)
            {
                Debug.Log("[HandVFX InitSetup] No HandVFXController found - skipping auto-setup");
                return;
            }

            // Check if already configured
            var so = new SerializedObject(controller);
            var leftRoot = so.FindProperty("leftHandRoot");
            if (leftRoot != null && leftRoot.objectReferenceValue != null)
            {
                Debug.Log("[HandVFX InitSetup] HandVFXController already configured - skipping");
                return;
            }

            Debug.Log("[HandVFX InitSetup] Auto-configuring HandVFXController...");
            DoSetup(controller);
        }

        public static void DoSetup(HandTracking.HandVFXController controller)
        {
            var so = new SerializedObject(controller);

            // Find hand tracking transforms
            var leftWrist = GameObject.Find("Hand Tracking Manager/Hand 0/Wrist");
            var rightWrist = GameObject.Find("Hand Tracking Manager/Hand 1/Wrist");

            if (leftWrist != null)
            {
                so.FindProperty("leftHandRoot").objectReferenceValue = leftWrist.transform;
                Debug.Log("[HandVFX InitSetup] ✓ Left Hand Root assigned");
            }

            if (rightWrist != null)
            {
                so.FindProperty("rightHandRoot").objectReferenceValue = rightWrist.transform;
                Debug.Log("[HandVFX InitSetup] ✓ Right Hand Root assigned");
            }

            // Find VFX children
            var leftVFX = controller.transform.Find("LeftHandVFX")?.GetComponent<VisualEffect>();
            var rightVFX = controller.transform.Find("RightHandVFX")?.GetComponent<VisualEffect>();

            if (leftVFX != null)
            {
                so.FindProperty("leftHandVFX").objectReferenceValue = leftVFX;
                Debug.Log("[HandVFX InitSetup] ✓ Left Hand VFX assigned");
            }

            if (rightVFX != null)
            {
                so.FindProperty("rightHandVFX").objectReferenceValue = rightVFX;
                Debug.Log("[HandVFX InitSetup] ✓ Right Hand VFX assigned");
            }

            // Load VFX assets
            var vfxAssets = Resources.LoadAll<VisualEffectAsset>("VFX");
            if (vfxAssets.Length > 0)
            {
                var vfxArrayProp = so.FindProperty("availableVFXAssets");
                vfxArrayProp.arraySize = vfxAssets.Length;
                for (int i = 0; i < vfxAssets.Length; i++)
                {
                    vfxArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = vfxAssets[i];
                }

                // Assign first VFX to hands
                if (leftVFX != null)
                {
                    leftVFX.visualEffectAsset = vfxAssets[0];
                    EditorUtility.SetDirty(leftVFX);
                }
                if (rightVFX != null)
                {
                    rightVFX.visualEffectAsset = vfxAssets[0];
                    EditorUtility.SetDirty(rightVFX);
                }
                Debug.Log($"[HandVFX InitSetup] ✓ {vfxAssets.Length} VFX assets loaded, assigned: {vfxAssets[0].name}");
            }

            // Find audio processor
            var audioProcessor = Object.FindFirstObjectByType<Audio.EnhancedAudioProcessor>();
            if (audioProcessor != null)
            {
                so.FindProperty("enhancedAudioProcessor").objectReferenceValue = audioProcessor;
                Debug.Log("[HandVFX InitSetup] ✓ EnhancedAudioProcessor connected");
            }

            so.FindProperty("useHoloKitHandTracking").boolValue = true;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);

            Debug.Log("[HandVFX InitSetup] ✓ HandVFXController setup complete!");
        }
    }
}
