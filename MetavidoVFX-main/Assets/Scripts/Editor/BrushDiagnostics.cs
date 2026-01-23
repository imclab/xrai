// BrushDiagnostics.cs - Deep diagnostic tool for brush issues
using UnityEngine;
using UnityEditor;
using XRRAI.BrushPainting;
using System.Collections.Generic;

namespace XRRAI.Editor
{
    public static class BrushDiagnostics
    {
        [MenuItem("H3M/Testing/Diagnose Brush System (Play Mode)")]
        public static void DiagnoseBrushSystem()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[Diagnostics] Must be in Play mode!");
                return;
            }

            var manager = Object.FindAnyObjectByType<BrushManager>();
            if (manager == null)
            {
                Debug.LogError("[Diagnostics] BrushManager not found!");
                return;
            }

            Debug.Log("=== BRUSH SYSTEM DIAGNOSTICS ===\n");

            // 1. Check catalog
            Debug.Log($"[1] CATALOG: {manager.BrushCatalog.Count} brushes loaded");

            // Count by geometry type
            var geomCounts = new Dictionary<BrushGeometryType, int>();
            var matCounts = new Dictionary<string, int>();
            var nullMaterials = new List<string>();

            foreach (var brush in manager.BrushCatalog)
            {
                // Geometry type count
                if (!geomCounts.ContainsKey(brush.GeometryType))
                    geomCounts[brush.GeometryType] = 0;
                geomCounts[brush.GeometryType]++;

                // Material check
                if (brush.Material == null)
                {
                    nullMaterials.Add(brush.DisplayName);
                }
                else
                {
                    string shaderName = brush.Material.shader?.name ?? "NULL_SHADER";
                    if (!matCounts.ContainsKey(shaderName))
                        matCounts[shaderName] = 0;
                    matCounts[shaderName]++;
                }
            }

            Debug.Log("[2] GEOMETRY TYPES:");
            foreach (var kv in geomCounts)
                Debug.Log($"    {kv.Key}: {kv.Value}");

            Debug.Log("[3] SHADER DISTRIBUTION:");
            foreach (var kv in matCounts)
                Debug.Log($"    {kv.Key}: {kv.Value}");

            if (nullMaterials.Count > 0)
            {
                Debug.LogError($"[4] NULL MATERIALS ({nullMaterials.Count}):");
                foreach (var name in nullMaterials)
                    Debug.LogError($"    - {name}");
            }
            else
            {
                Debug.Log("[4] All brushes have materials ✓");
            }

            // 2. Check current brush
            var currentBrush = manager.CurrentBrush;
            if (currentBrush != null)
            {
                Debug.Log($"\n[5] CURRENT BRUSH: {currentBrush.DisplayName}");
                Debug.Log($"    GeometryType: {currentBrush.GeometryType}");
                Debug.Log($"    Material: {(currentBrush.Material != null ? "SET" : "NULL")}");
                Debug.Log($"    Shader: {currentBrush.Material?.shader?.name ?? "NULL"}");
                Debug.Log($"    Category: {currentBrush.Category}");
            }
            else
            {
                Debug.LogError("[5] No current brush selected!");
            }

            // Camera info
            if (Camera.main != null)
            {
                Debug.Log($"\n[6] CAMERA INFO:");
                Debug.Log($"    Position: {Camera.main.transform.position}");
                Debug.Log($"    Forward: {Camera.main.transform.forward}");
                Debug.Log($"    Near/Far: {Camera.main.nearClipPlane}/{Camera.main.farClipPlane}");
            }

            // 3. Test drawing with each geometry type
            Debug.Log("\n[7] DRAWING TEST - Testing each geometry type:");

            var geomTypes = new[] {
                BrushGeometryType.Flat,
                BrushGeometryType.Tube,
                BrushGeometryType.Hull,
                BrushGeometryType.Particle,
                BrushGeometryType.Spray,
                BrushGeometryType.Slice
            };

            foreach (var geomType in geomTypes)
            {
                // Find brush with this type
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
                    Debug.LogWarning($"    {geomType}: No brush found");
                    continue;
                }

                // Set brush
                manager.SetBrush(brush);

                // Verify it was set
                if (manager.CurrentBrush != brush)
                {
                    Debug.LogError($"    {geomType}: SetBrush FAILED - brush didn't change!");
                    continue;
                }

                // Draw stroke
                manager.SetColor(Color.cyan);
                Vector3 start = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;
                start += Random.insideUnitSphere * 0.2f;

                var stroke = manager.BeginStroke(start, Quaternion.identity);
                if (stroke == null)
                {
                    Debug.LogError($"    {geomType}: BeginStroke returned NULL!");
                    continue;
                }

                // Check stroke's brush data
                if (stroke.BrushData != brush)
                {
                    Debug.LogError($"    {geomType}: Stroke has WRONG brush! Expected {brush.DisplayName}, got {stroke.BrushData?.DisplayName}");
                }

                // Add points
                for (int i = 0; i < 10; i++)
                {
                    Vector3 pos = start + Camera.main.transform.right * (i * 0.02f);
                    manager.UpdateStroke(pos, Quaternion.identity);
                }

                manager.EndStroke();

                // Check mesh
                var meshFilter = stroke.GetComponent<MeshFilter>();
                var meshRenderer = stroke.GetComponent<MeshRenderer>();
                var mesh = meshFilter?.sharedMesh;

                string meshInfo = mesh != null ? $"Verts={mesh.vertexCount}, Tris={mesh.triangles.Length/3}" : "NO MESH";
                string matInfo = meshRenderer?.material?.shader?.name ?? "NO MATERIAL";

                bool hasGeometry = mesh != null && mesh.vertexCount > 0;
                bool hasMaterial = meshRenderer?.material != null;

                string status = hasGeometry && hasMaterial ? "✓" : "✗";
                Debug.Log($"    {geomType}: {status} {brush.DisplayName} | {meshInfo} | {matInfo}");

                // DEEP RENDER CHECK
                if (meshRenderer != null)
                {
                    var mat = meshRenderer.material;
                    Debug.Log($"        Renderer.enabled: {meshRenderer.enabled}");
                    Debug.Log($"        GameObject.active: {stroke.gameObject.activeInHierarchy}");
                    Debug.Log($"        Layer: {LayerMask.LayerToName(stroke.gameObject.layer)} ({stroke.gameObject.layer})");
                    Debug.Log($"        Position: {stroke.transform.position}");
                    Debug.Log($"        Bounds: {meshRenderer.bounds}");
                    if (mat != null)
                    {
                        Debug.Log($"        Material.color: {(mat.HasProperty("_Color") ? mat.color.ToString() : "N/A")}");
                        Debug.Log($"        Material.renderQueue: {mat.renderQueue}");
                        Debug.Log($"        Shader.isSupported: {mat.shader.isSupported}");
                    }
                    // Camera check
                    if (Camera.main != null)
                    {
                        Vector3 toStroke = meshRenderer.bounds.center - Camera.main.transform.position;
                        float dotProduct = Vector3.Dot(Camera.main.transform.forward, toStroke.normalized);
                        bool inFront = dotProduct > 0;
                        Debug.Log($"        Camera.cullingMask includes layer: {((Camera.main.cullingMask & (1 << stroke.gameObject.layer)) != 0)}");
                        Debug.Log($"        Distance to camera: {toStroke.magnitude:F2}m");
                        Debug.Log($"        IN FRONT of camera: {inFront} (dot={dotProduct:F2})");
                        Debug.Log($"        Stroke center: {meshRenderer.bounds.center}");
                    }
                }

                if (!hasGeometry)
                    Debug.LogError($"        ERROR: No geometry generated!");
                if (!hasMaterial)
                    Debug.LogError($"        ERROR: No material on renderer!");
            }

            manager.ClearAllStrokes();
            Debug.Log("\n=== DIAGNOSTICS COMPLETE ===");
        }
    }
}
