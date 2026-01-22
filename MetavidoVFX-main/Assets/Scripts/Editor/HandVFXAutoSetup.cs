// Auto-setup script for HandVFXController
// Menu: H3M/Hand VFX/Auto Wire References

using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;

namespace MetavidoVFX.Editor
{
    public static class HandVFXAutoSetup
    {
        [MenuItem("H3M/Hand VFX/Auto Wire References")]
        public static void AutoWireReferences()
        {
            // Find HandVFXController
            var controller = Object.FindFirstObjectByType<HandTracking.HandVFXController>();
            if (controller == null)
            {
                Debug.LogError("[HandVFX Setup] No HandVFXController found in scene!");
                return;
            }

            Debug.Log("[HandVFX Setup] Found HandVFXController, wiring references...");

            // Get SerializedObject for the controller
            var so = new SerializedObject(controller);

            // Find hand tracking transforms (Wrist joints from Hand Tracking Manager)
            var leftWrist = GameObject.Find("Hand Tracking Manager/Hand 0/Wrist");
            var rightWrist = GameObject.Find("Hand Tracking Manager/Hand 1/Wrist");

            if (leftWrist != null)
            {
                so.FindProperty("leftHandRoot").objectReferenceValue = leftWrist.transform;
                Debug.Log("[HandVFX Setup] ✓ Assigned Left Hand Root (Wrist)");
            }
            else
            {
                Debug.LogWarning("[HandVFX Setup] Could not find Hand 0/Wrist");
            }

            if (rightWrist != null)
            {
                so.FindProperty("rightHandRoot").objectReferenceValue = rightWrist.transform;
                Debug.Log("[HandVFX Setup] ✓ Assigned Right Hand Root (Wrist)");
            }
            else
            {
                Debug.LogWarning("[HandVFX Setup] Could not find Hand 1/Wrist");
            }

            // Find VFX children
            var leftVFX = controller.transform.Find("LeftHandVFX")?.GetComponent<VisualEffect>();
            var rightVFX = controller.transform.Find("RightHandVFX")?.GetComponent<VisualEffect>();

            if (leftVFX != null)
            {
                so.FindProperty("leftHandVFX").objectReferenceValue = leftVFX;
                Debug.Log("[HandVFX Setup] ✓ Assigned Left Hand VFX");
            }

            if (rightVFX != null)
            {
                so.FindProperty("rightHandVFX").objectReferenceValue = rightVFX;
                Debug.Log("[HandVFX Setup] ✓ Assigned Right Hand VFX");
            }

            // Load VFX assets from Resources
            var vfxAssets = Resources.LoadAll<VisualEffectAsset>("VFX");
            if (vfxAssets.Length > 0)
            {
                var vfxArrayProp = so.FindProperty("availableVFXAssets");
                vfxArrayProp.arraySize = vfxAssets.Length;
                for (int i = 0; i < vfxAssets.Length; i++)
                {
                    vfxArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = vfxAssets[i];
                }
                Debug.Log($"[HandVFX Setup] ✓ Loaded {vfxAssets.Length} VFX assets");

                // Assign first VFX to both hands
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
                Debug.Log($"[HandVFX Setup] ✓ Assigned VFX: {vfxAssets[0].name} to both hands");
            }
            else
            {
                Debug.LogWarning("[HandVFX Setup] No VFX assets found in Resources/VFX");
            }

            // Find and assign AudioBridge (preferred)
            var audioBridge = Object.FindFirstObjectByType<AudioBridge>();
            if (audioBridge != null)
            {
                so.FindProperty("audioBridge").objectReferenceValue = audioBridge;
                Debug.Log("[HandVFX Setup] ✓ Connected AudioBridge");
            }
            else
            {
                Debug.LogWarning("[HandVFX Setup] AudioBridge not found - add via H3M > VFX Pipeline Master > Create AudioBridge");
            }

            // Legacy fallback: EnhancedAudioProcessor
            var audioProcessor = Object.FindFirstObjectByType<Audio.EnhancedAudioProcessor>();
            if (audioProcessor != null)
            {
                so.FindProperty("enhancedAudioProcessor").objectReferenceValue = audioProcessor;
                Debug.Log("[HandVFX Setup] ✓ Connected EnhancedAudioProcessor (legacy)");
            }

            // Enable HoloKit hand tracking
            so.FindProperty("useHoloKitHandTracking").boolValue = true;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);

            Debug.Log("[HandVFX Setup] ✓ Hand VFX setup complete!");
        }

        [MenuItem("H3M/Hand VFX/Validate Setup")]
        public static void ValidateSetup()
        {
            var controller = Object.FindFirstObjectByType<HandTracking.HandVFXController>();
            if (controller == null)
            {
                Debug.LogError("[HandVFX] ✗ No HandVFXController in scene");
                return;
            }

            var so = new SerializedObject(controller);

            bool allGood = true;

            Check("leftHandRoot", so, ref allGood);
            Check("rightHandRoot", so, ref allGood);
            Check("leftHandVFX", so, ref allGood);
            Check("rightHandVFX", so, ref allGood);

            var vfxArray = so.FindProperty("availableVFXAssets");
            if (vfxArray.arraySize > 0)
                Debug.Log($"[HandVFX] ✓ availableVFXAssets ({vfxArray.arraySize} assets)");
            else
            {
                Debug.LogWarning("[HandVFX] ✗ availableVFXAssets is empty");
                allGood = false;
            }

            if (allGood)
                Debug.Log("[HandVFX] ✓ All references configured correctly!");
            else
                Debug.LogWarning("[HandVFX] Some references missing. Run 'Auto Wire References'.");
        }

        static void Check(string propName, SerializedObject so, ref bool allGood)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && prop.objectReferenceValue != null)
                Debug.Log($"[HandVFX] ✓ {propName}: {prop.objectReferenceValue.name}");
            else
            {
                Debug.LogWarning($"[HandVFX] ✗ {propName} is not assigned");
                allGood = false;
            }
        }
    }
}
