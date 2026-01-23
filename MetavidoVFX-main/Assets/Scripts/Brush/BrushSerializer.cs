// BrushSerializer.cs - Scene save/load helper
// Part of Spec 011: OpenBrush Integration
//
// Handles serialization of brush scenes to/from JSON format.
// Supports layers, camera state, and efficient brush index deduplication.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// Handles save/load of brush scenes to JSON format.
    /// Matches Open Brush-inspired format from Spec 011.
    /// </summary>
    public static class BrushSerializer
    {
        private const string CURRENT_VERSION = "1.1";

        #region Save

        /// <summary>
        /// Save a brush scene to JSON file
        /// </summary>
        public static void SaveToFile(string filepath, BrushSceneData sceneData)
        {
            string json = JsonUtility.ToJson(sceneData, true);
            File.WriteAllText(filepath, json);
            Debug.Log($"[BrushSerializer] Saved {sceneData.Strokes.Count} strokes to: {filepath}");
        }

        /// <summary>
        /// Create scene data from current BrushManager state
        /// </summary>
        public static BrushSceneData CreateSceneData(
            IReadOnlyList<BrushStroke> strokes,
            Camera camera = null,
            List<BrushLayer> layers = null)
        {
            var sceneData = new BrushSceneData
            {
                Version = CURRENT_VERSION,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                BrushIndex = new List<string>(),
                Strokes = new List<BrushStrokeData>(),
                Layers = new List<BrushLayerData>(),
                Camera = null
            };

            // Build brush index (deduplicated)
            var brushToIndex = new Dictionary<string, int>();
            foreach (var stroke in strokes)
            {
                if (stroke?.BrushData == null || !stroke.IsFinalized) continue;

                string brushId = stroke.BrushData.BrushId;
                if (!brushToIndex.ContainsKey(brushId))
                {
                    brushToIndex[brushId] = sceneData.BrushIndex.Count;
                    sceneData.BrushIndex.Add(brushId);
                }
            }

            // Serialize strokes with brush index references
            int strokeIndex = 0;
            foreach (var stroke in strokes)
            {
                if (stroke?.BrushData == null || !stroke.IsFinalized) continue;

                var strokeData = new BrushStrokeData
                {
                    BrushIdx = brushToIndex[stroke.BrushData.BrushId],
                    Color = ColorToArray(stroke.Color),
                    Size = stroke.BaseSize,
                    LayerIdx = 0, // Default layer
                    Points = new List<BrushPointData>()
                };

                foreach (var cp in stroke.ControlPoints)
                {
                    strokeData.Points.Add(new BrushPointData
                    {
                        Pos = Vector3ToArray(cp.Position),
                        Rot = QuaternionToArray(cp.Rotation),
                        Pressure = cp.Pressure
                    });
                }

                sceneData.Strokes.Add(strokeData);
                strokeIndex++;
            }

            // Serialize layers
            if (layers != null && layers.Count > 0)
            {
                foreach (var layer in layers)
                {
                    sceneData.Layers.Add(new BrushLayerData
                    {
                        Name = layer.Name,
                        Visible = layer.Visible,
                        Locked = layer.Locked
                    });
                }
            }
            else
            {
                // Add default layer
                sceneData.Layers.Add(new BrushLayerData
                {
                    Name = "Layer 1",
                    Visible = true,
                    Locked = false
                });
            }

            // Serialize camera state
            if (camera != null)
            {
                sceneData.Camera = new BrushCameraData
                {
                    Position = Vector3ToArray(camera.transform.position),
                    Rotation = QuaternionToArray(camera.transform.rotation),
                    FOV = camera.fieldOfView
                };
            }

            return sceneData;
        }

        #endregion

        #region Load

        /// <summary>
        /// Load a brush scene from JSON file
        /// </summary>
        public static BrushSceneData LoadFromFile(string filepath)
        {
            if (!File.Exists(filepath))
            {
                Debug.LogWarning($"[BrushSerializer] File not found: {filepath}");
                return null;
            }

            string json = File.ReadAllText(filepath);
            var sceneData = JsonUtility.FromJson<BrushSceneData>(json);

            Debug.Log($"[BrushSerializer] Loaded {sceneData.Strokes.Count} strokes from: {filepath}");
            return sceneData;
        }

        /// <summary>
        /// Restore strokes from scene data into BrushManager
        /// </summary>
        public static List<BrushStroke> RestoreStrokes(
            BrushSceneData sceneData,
            BrushManager manager,
            Transform parent)
        {
            var restoredStrokes = new List<BrushStroke>();

            if (sceneData == null || manager == null) return restoredStrokes;

            foreach (var strokeData in sceneData.Strokes)
            {
                // Get brush by index
                if (strokeData.BrushIdx < 0 || strokeData.BrushIdx >= sceneData.BrushIndex.Count)
                {
                    Debug.LogWarning($"[BrushSerializer] Invalid brush index: {strokeData.BrushIdx}");
                    continue;
                }

                string brushId = sceneData.BrushIndex[strokeData.BrushIdx];
                var brush = manager.GetBrush(brushId);

                if (brush == null)
                {
                    Debug.LogWarning($"[BrushSerializer] Brush not found: {brushId}");
                    continue;
                }

                // Create stroke GameObject
                var go = new GameObject("BrushStroke");
                go.transform.SetParent(parent);
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                var stroke = go.AddComponent<BrushStroke>();

                // Convert to legacy format for FromData
                var legacyData = new StrokeData
                {
                    BrushId = brushId,
                    Color = strokeData.Color,
                    Size = strokeData.Size,
                    Points = strokeData.Points.Select(p => new ControlPointData
                    {
                        Position = p.Pos,
                        Rotation = p.Rot,
                        Pressure = p.Pressure
                    }).ToList()
                };

                stroke.FromData(legacyData, brush);
                restoredStrokes.Add(stroke);
            }

            return restoredStrokes;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get save file path in persistent data
        /// </summary>
        public static string GetSavePath(string filename)
        {
            string directory = Path.Combine(Application.persistentDataPath, "BrushScenes");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return Path.Combine(directory, filename + ".json");
        }

        /// <summary>
        /// List all saved scenes
        /// </summary>
        public static string[] GetSavedScenes()
        {
            string directory = Path.Combine(Application.persistentDataPath, "BrushScenes");
            if (!Directory.Exists(directory))
                return Array.Empty<string>();

            return Directory.GetFiles(directory, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray();
        }

        /// <summary>
        /// Delete a saved scene
        /// </summary>
        public static bool DeleteScene(string filename)
        {
            string filepath = GetSavePath(filename);
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
                return true;
            }
            return false;
        }

        // Conversion helpers
        private static float[] Vector3ToArray(Vector3 v) => new[] { v.x, v.y, v.z };
        private static float[] QuaternionToArray(Quaternion q) => new[] { q.x, q.y, q.z, q.w };
        private static float[] ColorToArray(Color c) => new[] { c.r, c.g, c.b, c.a };

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Complete scene data for serialization (Spec 011 format)
    /// </summary>
    [Serializable]
    public class BrushSceneData
    {
        public string Version;
        public string CreatedAt;
        public List<string> BrushIndex;
        public List<BrushStrokeData> Strokes;
        public List<BrushLayerData> Layers;
        public BrushCameraData Camera;
    }

    /// <summary>
    /// Stroke data with brush index reference
    /// </summary>
    [Serializable]
    public class BrushStrokeData
    {
        public int BrushIdx;
        public float[] Color;
        public float Size;
        public int LayerIdx;
        public List<BrushPointData> Points;
    }

    /// <summary>
    /// Compact control point data
    /// </summary>
    [Serializable]
    public class BrushPointData
    {
        public float[] Pos;
        public float[] Rot;
        public float Pressure;
    }

    /// <summary>
    /// Layer data for scene organization
    /// </summary>
    [Serializable]
    public class BrushLayerData
    {
        public string Name;
        public bool Visible;
        public bool Locked;
    }

    /// <summary>
    /// Camera state for scene restoration
    /// </summary>
    [Serializable]
    public class BrushCameraData
    {
        public float[] Position;
        public float[] Rotation;
        public float FOV;
    }

    /// <summary>
    /// Runtime layer representation
    /// </summary>
    public class BrushLayer
    {
        public string Name;
        public bool Visible = true;
        public bool Locked;
        public List<BrushStroke> Strokes = new();
    }

    #endregion
}
