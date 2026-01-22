// Editor utilities for VFXAutoOptimizer

using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using XRRAI.Performance;

namespace XRRAI.Performance.Editor
{
    public static class VFXAutoOptimizerEditor
    {
        [MenuItem("H3M/Performance/Profile All VFX")]
        public static void ProfileVFX()
        {
            var profiler = Object.FindFirstObjectByType<VFXProfiler>();
            if (profiler == null)
            {
                var go = new GameObject("VFXProfiler");
                profiler = go.AddComponent<VFXProfiler>();
            }

            profiler.ProfileAllVFX();
            Selection.activeGameObject = profiler.gameObject;
        }

        [MenuItem("H3M/Performance/Add All Performance Components")]
        public static void AddAllPerformanceComponents()
        {
            // Add optimizer
            AddOptimizer();

            // Add profiler
            var profiler = Object.FindFirstObjectByType<VFXProfiler>();
            if (profiler == null)
            {
                var optimizerGO = Object.FindFirstObjectByType<VFXAutoOptimizer>()?.gameObject;
                if (optimizerGO != null)
                {
                    profiler = optimizerGO.AddComponent<VFXProfiler>();
                }
            }

            Debug.Log("[VFXOptimizer] Added VFXAutoOptimizer + VFXProfiler");
        }

        [MenuItem("H3M/Performance/Add LOD Controllers to All VFX")]
        public static void AddLODToAllVFX()
        {
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            int added = 0;

            foreach (var vfx in allVFX)
            {
                if (vfx.GetComponent<VFXLODController>() == null)
                {
                    Undo.AddComponent<VFXLODController>(vfx.gameObject);
                    added++;
                }
            }

            Debug.Log($"[VFXOptimizer] Added VFXLODController to {added} VFX objects");
        }

        [MenuItem("H3M/Performance/Cleanup Sample VFX Files")]
        public static void CleanupSampleVFX()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Cleanup Sample VFX",
                "This will DELETE sample VFX files from:\n\n" +
                "• Assets/Samples/Visual Effect Graph/*/Learning Templates/\n\n" +
                "This can save ~100MB and reduce import time.\n\n" +
                "Continue?",
                "Delete Samples",
                "Cancel"
            );

            if (!confirmed) return;

            string[] pathsToDelete = {
                "Assets/Samples/Visual Effect Graph/17.2.0/Learning Templates"
            };

            int deleted = 0;
            foreach (var path in pathsToDelete)
            {
                if (AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.DeleteAsset(path);
                    deleted++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[VFXOptimizer] Deleted {deleted} sample folders");
        }


        [MenuItem("H3M/Performance/Add VFX Auto-Optimizer")]
        public static void AddOptimizer()
        {
            var existing = Object.FindFirstObjectByType<VFXAutoOptimizer>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[VFXOptimizer] Already exists in scene - selected it");
                return;
            }

            var go = new GameObject("VFXAutoOptimizer");
            var optimizer = go.AddComponent<VFXAutoOptimizer>();
            Undo.RegisterCreatedObjectUndo(go, "Add VFX Optimizer");
            Selection.activeGameObject = go;

            Debug.Log("[VFXOptimizer] Added to scene - will auto-optimize VFX to maintain 60fps");
        }

        [MenuItem("H3M/Performance/Show Performance Stats")]
        public static void ShowPerformanceStats()
        {
            var optimizer = Object.FindFirstObjectByType<VFXAutoOptimizer>();
            if (optimizer == null)
            {
                Debug.Log("[VFXOptimizer] No optimizer in scene. Add one via H3M > Performance > Add VFX Auto-Optimizer");
                return;
            }

            Debug.Log($"═══════════════════════════════════════════════════════════");
            Debug.Log($"   VFX Performance Stats");
            Debug.Log($"═══════════════════════════════════════════════════════════");
            Debug.Log($"  FPS: {optimizer.AverageFPS:F1} (min: {optimizer.MinFPS:F0}, max: {optimizer.MaxFPS:F0})");
            Debug.Log($"  Total Particles: {optimizer.TotalParticleCount:N0}");
            Debug.Log($"  Active VFX: {optimizer.ActiveVFXCount}");
            Debug.Log($"  Quality Multiplier: {optimizer.QualityMultiplier:P0}");
            Debug.Log($"  State: {optimizer.State}");
            Debug.Log($"═══════════════════════════════════════════════════════════");
        }

        [MenuItem("H3M/Performance/Reset Quality to Max")]
        public static void ResetQuality()
        {
            var optimizer = Object.FindFirstObjectByType<VFXAutoOptimizer>();
            if (optimizer == null)
            {
                Debug.LogWarning("[VFXOptimizer] No optimizer in scene");
                return;
            }

            optimizer.ResetToMaxQuality();
            Debug.Log("[VFXOptimizer] Reset to maximum quality");
        }

        [MenuItem("H3M/Performance/Refresh VFX List")]
        public static void RefreshVFXList()
        {
            var optimizer = Object.FindFirstObjectByType<VFXAutoOptimizer>();
            if (optimizer == null)
            {
                Debug.LogWarning("[VFXOptimizer] No optimizer in scene");
                return;
            }

            optimizer.RefreshVFXList();
        }
    }

    [CustomEditor(typeof(VFXAutoOptimizer))]
    public class VFXAutoOptimizerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            VFXAutoOptimizer optimizer = (VFXAutoOptimizer)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Stats", EditorStyles.boldLabel);

            GUI.enabled = false;
            EditorGUILayout.FloatField("Current FPS", optimizer.CurrentFPS);
            EditorGUILayout.FloatField("Average FPS", optimizer.AverageFPS);
            EditorGUILayout.IntField("Total Particles", optimizer.TotalParticleCount);
            EditorGUILayout.IntField("Active VFX", optimizer.ActiveVFXCount);
            EditorGUILayout.Slider("Quality", optimizer.QualityMultiplier, 0f, 1f);
            EditorGUILayout.EnumPopup("State", optimizer.State);
            GUI.enabled = true;

            EditorGUILayout.Space(5);

            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh VFX List"))
                {
                    optimizer.RefreshVFXList();
                }
                if (GUILayout.Button("Reset Quality"))
                {
                    optimizer.ResetToMaxQuality();
                }
                EditorGUILayout.EndHorizontal();
            }

            // Repaint while playing for live updates
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
