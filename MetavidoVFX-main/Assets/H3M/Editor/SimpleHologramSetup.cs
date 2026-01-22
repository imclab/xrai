using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;
using XRRAI.Hologram;

namespace H3M.Editor
{
    public class SimpleHologramSetup : EditorWindow
    {
        [MenuItem("H3M/Setup Simple Human Hologram")]
        public static void Setup()
        {
            // Find the Rcam4 VFX GameObject (or any with VisualEffect)
            var vfxObjects = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);

            if (vfxObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No VisualEffect found in scene!", "OK");
                return;
            }

            // Find AR Camera with AROcclusionManager
            var occlusionManager = Object.FindFirstObjectByType<AROcclusionManager>();
            if (occlusionManager == null)
            {
                EditorUtility.DisplayDialog("Error", "No AROcclusionManager found in scene!", "OK");
                return;
            }

            var arCamera = occlusionManager.GetComponent<Camera>();

            int setupCount = 0;
            foreach (var vfx in vfxObjects)
            {
                // Skip if already has SimpleHumanHologram
                if (vfx.GetComponent<SimpleHumanHologram>() != null)
                {
                    Debug.Log($"[H3M Setup] {vfx.name} already has SimpleHumanHologram");
                    continue;
                }

                // Add SimpleHumanHologram
                var hologram = vfx.gameObject.AddComponent<SimpleHumanHologram>();

                // Configure via SerializedObject
                var so = new SerializedObject(hologram);
                so.FindProperty("occlusionManager").objectReferenceValue = occlusionManager;
                so.FindProperty("arCamera").objectReferenceValue = arCamera;
                so.FindProperty("useHumanDepth").boolValue = true;
                so.FindProperty("useLiDARDepth").boolValue = true;
                so.ApplyModifiedProperties();

                Debug.Log($"[H3M Setup] Added SimpleHumanHologram to {vfx.name}");
                setupCount++;
            }

            EditorUtility.DisplayDialog("Setup Complete",
                $"Added SimpleHumanHologram to {setupCount} VFX objects.\n\n" +
                "Ready to test!", "OK");
        }

        [MenuItem("H3M/Quick Test - Enter Play Mode")]
        public static void QuickTest()
        {
            Setup();
            EditorApplication.isPlaying = true;
        }
    }
}
