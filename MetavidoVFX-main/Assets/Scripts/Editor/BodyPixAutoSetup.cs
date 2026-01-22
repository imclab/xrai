// Auto-setup for BodyPartSegmenter - assigns ResourceSet and cleans duplicates
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace XRRAI.Editor
{
    public static class BodyPixAutoSetup
    {
        private const string RESOURCESET_GUID = "2f407a69dfb2e1e48a0b559630c28676";

        [MenuItem("H3M/Body Segmentation/Auto-Setup BodyPartSegmenter")]
        public static void AutoSetup()
        {
            // Find all BodyPartSegmenters in scene
            var segmenters = Object.FindObjectsByType<MetavidoVFX.Segmentation.BodyPartSegmenter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (segmenters.Length == 0)
            {
                Debug.LogWarning("[BodyPix AutoSetup] No BodyPartSegmenter found in scene");
                return;
            }

            Debug.Log($"[BodyPix AutoSetup] Found {segmenters.Length} BodyPartSegmenter(s)");

            // Load ResourceSet from package
            string path = AssetDatabase.GUIDToAssetPath(RESOURCESET_GUID);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[BodyPix AutoSetup] ResourceSet not found. Is jp.keijiro.bodypix installed?");
                return;
            }

            var resourceSet = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (resourceSet == null)
            {
                Debug.LogError($"[BodyPix AutoSetup] Failed to load ResourceSet from {path}");
                return;
            }

            Debug.Log($"[BodyPix AutoSetup] Loaded ResourceSet: {path}");

            // Find the best segmenter (prefer one with AR camera assigned)
            MetavidoVFX.Segmentation.BodyPartSegmenter bestSegmenter = null;
            foreach (var seg in segmenters)
            {
                var so = new SerializedObject(seg);
                var camProp = so.FindProperty("_arCamera");
                if (camProp != null && camProp.objectReferenceValue != null)
                {
                    bestSegmenter = seg;
                    break;
                }
            }

            // Fallback to first active one
            if (bestSegmenter == null)
            {
                bestSegmenter = segmenters.FirstOrDefault(s => s.gameObject.activeInHierarchy)
                             ?? segmenters[0];
            }

            // Assign ResourceSet
            var bestSO = new SerializedObject(bestSegmenter);
            var resourceSetProp = bestSO.FindProperty("_resourceSet");
            if (resourceSetProp != null)
            {
                resourceSetProp.objectReferenceValue = resourceSet;
                bestSO.ApplyModifiedProperties();
                Debug.Log($"[BodyPix AutoSetup] ✓ Assigned ResourceSet to {bestSegmenter.gameObject.name}");
            }

            // Delete duplicates
            int deleted = 0;
            foreach (var seg in segmenters)
            {
                if (seg != bestSegmenter)
                {
                    Debug.Log($"[BodyPix AutoSetup] Removing duplicate: {seg.gameObject.name}");
                    Undo.DestroyObjectImmediate(seg.gameObject);
                    deleted++;
                }
            }

            if (deleted > 0)
                Debug.Log($"[BodyPix AutoSetup] ✓ Removed {deleted} duplicate(s)");

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[BodyPix AutoSetup] ✓ Setup complete! Save scene to persist changes.");
        }

        [MenuItem("H3M/Body Segmentation/Verify Setup")]
        public static void VerifySetup()
        {
            var segmenters = Object.FindObjectsByType<MetavidoVFX.Segmentation.BodyPartSegmenter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("   BodyPartSegmenter Verification");
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log($"  Count: {segmenters.Length}");

            foreach (var seg in segmenters)
            {
                var so = new SerializedObject(seg);
                var resProp = so.FindProperty("_resourceSet");
                var camProp = so.FindProperty("_arCamera");
                var bgProp = so.FindProperty("_arCameraBackground");

                bool hasResource = resProp?.objectReferenceValue != null;
                bool hasCam = camProp?.objectReferenceValue != null;
                bool hasBg = bgProp?.objectReferenceValue != null;

                string status = hasResource && hasCam && hasBg ? "✓ OK" : "✗ INCOMPLETE";
                Debug.Log($"  [{status}] {seg.gameObject.name} (active: {seg.gameObject.activeInHierarchy})");
                Debug.Log($"      ResourceSet: {(hasResource ? "✓" : "✗")}");
                Debug.Log($"      AR Camera: {(hasCam ? "✓" : "✗")}");
                Debug.Log($"      AR Background: {(hasBg ? "✓" : "✗")}");
            }
            Debug.Log("═══════════════════════════════════════════════════════════");
        }
    }
}
