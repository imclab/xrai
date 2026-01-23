// BrushTestRunner.cs - Comprehensive test for brush material switching
// Runs automatically when selected from menu

using UnityEngine;
using UnityEditor;
using XRRAI.BrushPainting;
using System.Collections.Generic;

namespace XRRAI.Editor
{
    public static class BrushTestRunner
    {
        [MenuItem("H3M/Testing/Test Brush Types (Play Mode)")]
        public static void TestBrushTypes()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[BrushTest] Must be in Play mode!");
                return;
            }

            var manager = Object.FindAnyObjectByType<BrushManager>();
            if (manager == null)
            {
                Debug.LogError("[BrushTest] BrushManager not found!");
                return;
            }

            Debug.Log("[BrushTest] Starting brush type test...");

            // Test different geometry types
            TestBrushByGeometryType(manager, BrushGeometryType.Flat);
            TestBrushByGeometryType(manager, BrushGeometryType.Tube);
            TestBrushByGeometryType(manager, BrushGeometryType.Hull);
            TestBrushByGeometryType(manager, BrushGeometryType.Particle);
            TestBrushByGeometryType(manager, BrushGeometryType.Spray);
            TestBrushByGeometryType(manager, BrushGeometryType.Slice);

            Debug.Log("[BrushTest] Test complete! Check logs above for results.");
        }

        [MenuItem("H3M/Testing/Test ALL Brushes (Play Mode)")]
        public static void TestAllBrushes()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[BrushTest] Must be in Play mode!");
                return;
            }

            var manager = Object.FindAnyObjectByType<BrushManager>();
            if (manager == null)
            {
                Debug.LogError("[BrushTest] BrushManager not found!");
                return;
            }

            Debug.Log($"[BrushTest] ========== TESTING ALL {manager.BrushCatalog.Count} BRUSHES ==========");

            int passed = 0;
            int failed = 0;
            var failures = new List<string>();
            var shaderCounts = new Dictionary<string, int>();

            foreach (var brush in manager.BrushCatalog)
            {
                bool success = TestSingleBrush(manager, brush, out string error);

                string shaderName = brush.Material?.shader?.name ?? "NULL";
                if (!shaderCounts.ContainsKey(shaderName))
                    shaderCounts[shaderName] = 0;
                shaderCounts[shaderName]++;

                if (success)
                {
                    passed++;
                }
                else
                {
                    failed++;
                    failures.Add($"{brush.DisplayName}: {error}");
                }
            }

            // Summary
            Debug.Log($"[BrushTest] ========== RESULTS ==========");
            Debug.Log($"[BrushTest] Total: {manager.BrushCatalog.Count}, Passed: {passed}, Failed: {failed}");

            // Shader distribution
            string shaderSummary = "";
            foreach (var kv in shaderCounts)
            {
                shaderSummary += $"\n  {kv.Key}: {kv.Value}";
            }
            Debug.Log($"[BrushTest] Shader distribution:{shaderSummary}");

            // List failures
            if (failures.Count > 0)
            {
                Debug.LogError($"[BrushTest] FAILURES ({failures.Count}):");
                foreach (var f in failures)
                {
                    Debug.LogError($"  - {f}");
                }
            }
            else
            {
                Debug.Log($"[BrushTest] ✓ ALL BRUSHES PASSED!");
            }

            // Clean up strokes
            manager.ClearAllStrokes();
            Debug.Log($"[BrushTest] Cleaned up test strokes");
        }

        static bool TestSingleBrush(BrushManager manager, BrushData brush, out string error)
        {
            error = null;

            // Check material
            if (brush.Material == null)
            {
                error = "Material is NULL";
                return false;
            }

            if (brush.Material.shader == null)
            {
                error = "Shader is NULL";
                return false;
            }

            if (brush.Material.shader.name == "Hidden/InternalErrorShader")
            {
                error = "Using error shader";
                return false;
            }

            // Set brush and draw
            manager.SetBrush(brush);
            manager.SetColor(new Color(Random.value, Random.value, Random.value));
            manager.SetSize(brush.DefaultSize);

            Vector3 start = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;
            start += new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.2f), 0);

            var stroke = manager.BeginStroke(start, Quaternion.identity);
            if (stroke == null)
            {
                error = "BeginStroke returned null";
                return false;
            }

            // Add points
            for (int i = 1; i <= 5; i++)
            {
                Vector3 pos = start + Camera.main.transform.right * (i * 0.01f);
                manager.UpdateStroke(pos, Quaternion.identity);
            }

            manager.EndStroke();

            // Check stroke has mesh
            var meshFilter = stroke.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                error = "No mesh generated";
                return false;
            }

            if (meshFilter.sharedMesh.vertexCount == 0)
            {
                error = "Mesh has 0 vertices";
                return false;
            }

            return true;
        }

        static void TestBrushByGeometryType(BrushManager manager, BrushGeometryType geomType)
        {
            // Find a brush with this geometry type
            BrushData brush = null;
            foreach (var b in manager.BrushCatalog)
            {
                if (b.GeometryType == geomType)
                {
                    brush = b;
                    break;
                }
            }

            if (brush == null)
            {
                Debug.LogWarning($"[BrushTest] No brush found with GeometryType={geomType}");
                return;
            }

            // Set this brush
            manager.SetBrush(brush);
            manager.SetColor(Color.cyan);
            manager.SetSize(0.02f);

            // Draw a simple stroke
            Vector3 start = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;

            var stroke = manager.BeginStroke(start, Quaternion.identity);
            if (stroke == null)
            {
                Debug.LogError($"[BrushTest] Failed to begin stroke for {brush.DisplayName}");
                return;
            }

            // Add some points
            for (int i = 1; i <= 10; i++)
            {
                Vector3 pos = start + Camera.main.transform.right * (i * 0.02f);
                manager.UpdateStroke(pos, Quaternion.identity);
            }

            manager.EndStroke();

            Debug.Log($"[BrushTest] ✓ {geomType}: {brush.DisplayName}, Material={brush.Material?.shader?.name}");
        }
    }
}
