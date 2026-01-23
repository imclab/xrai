// XRRAISceneManager.cs - Scene save/load/export manager
// Part of Spec 016: XRRAI Scene Format & Cross-Platform Export
//
// Singleton manager for XRRAI scene operations:
// - Save/load scenes to local storage
// - Export to glTF/GLB for cross-platform sharing
// - Integration with BrushManager and HologramSource

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using XRRAI.BrushPainting;

namespace XRRAI.Scene
{
    /// <summary>
    /// Singleton manager for XRRAI scene save/load/export operations.
    /// </summary>
    public class XRRAISceneManager : MonoBehaviour
    {
        public static XRRAISceneManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Default save directory (relative to persistentDataPath)")]
        [SerializeField] string _saveDirectory = "XRRAIScenes";

        [Tooltip("Auto-save interval in seconds (0 = disabled)")]
        [SerializeField] float _autoSaveInterval = 0f;

        [Header("References")]
        [SerializeField] BrushManager _brushManager;

        // Events
        public event Action<XRRAIScene> OnSceneLoaded;
        public event Action<XRRAIScene> OnSceneSaved;
        public event Action<string> OnExportComplete;
        public event Action<string> OnError;

        // State
        XRRAIScene _currentScene;
        string _currentFilePath;
        float _lastAutoSaveTime;
        bool _isDirty;

        // Properties
        public XRRAIScene CurrentScene => _currentScene;
        public string CurrentFilePath => _currentFilePath;
        public bool IsDirty => _isDirty;
        public string SaveDirectory => Path.Combine(Application.persistentDataPath, _saveDirectory);

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Ensure save directory exists
            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);

            // Find references
            if (_brushManager == null)
                _brushManager = FindFirstObjectByType<BrushManager>();

            // Create new empty scene
            NewScene();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            // Auto-save check
            if (_autoSaveInterval > 0 && _isDirty && Time.time - _lastAutoSaveTime > _autoSaveInterval)
            {
                AutoSave();
            }
        }

        #region Scene Creation

        /// <summary>
        /// Create a new empty scene
        /// </summary>
        public void NewScene(string name = "Untitled Scene")
        {
            _currentScene = new XRRAIScene();
            _currentScene.scene.name = name;
            _currentScene.layers.Add(new LayerData { id = LayerData.GenerateId(), name = "Layer 1" });
            _currentFilePath = null;
            _isDirty = false;

            Debug.Log($"[XRRAISceneManager] New scene created: {name}");
        }

        /// <summary>
        /// Mark scene as modified
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
            _currentScene?.MarkModified();
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Save current scene to file
        /// </summary>
        public bool SaveScene(string filepath = null)
        {
            if (_currentScene == null)
            {
                OnError?.Invoke("No scene to save");
                return false;
            }

            filepath ??= _currentFilePath;
            if (string.IsNullOrEmpty(filepath))
            {
                filepath = Path.Combine(SaveDirectory, $"{_currentScene.scene.name}.xrrai");
            }

            try
            {
                // Collect scene data from managers
                CollectSceneData();

                // Serialize to JSON
                string json = JsonUtility.ToJson(_currentScene, true);
                File.WriteAllText(filepath, json);

                _currentFilePath = filepath;
                _isDirty = false;
                _lastAutoSaveTime = Time.time;

                OnSceneSaved?.Invoke(_currentScene);
                Debug.Log($"[XRRAISceneManager] Scene saved: {filepath}");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Save failed: {ex.Message}");
                Debug.LogError($"[XRRAISceneManager] Save failed: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Load scene from file
        /// </summary>
        public bool LoadScene(string filepath)
        {
            if (!File.Exists(filepath))
            {
                OnError?.Invoke($"File not found: {filepath}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(filepath);
                _currentScene = JsonUtility.FromJson<XRRAIScene>(json);
                _currentFilePath = filepath;
                _isDirty = false;

                // Reconstruct scene in Unity
                ReconstructScene();

                OnSceneLoaded?.Invoke(_currentScene);
                Debug.Log($"[XRRAISceneManager] Scene loaded: {filepath}");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Load failed: {ex.Message}");
                Debug.LogError($"[XRRAISceneManager] Load failed: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Auto-save to temporary file
        /// </summary>
        void AutoSave()
        {
            string autoSavePath = Path.Combine(SaveDirectory, $".autosave_{DateTime.Now:yyyyMMdd_HHmmss}.xrrai");
            if (SaveScene(autoSavePath))
            {
                Debug.Log($"[XRRAISceneManager] Auto-saved: {autoSavePath}");
            }
        }

        /// <summary>
        /// Get list of saved scenes
        /// </summary>
        public List<string> GetSavedScenes()
        {
            var scenes = new List<string>();
            if (Directory.Exists(SaveDirectory))
            {
                foreach (var file in Directory.GetFiles(SaveDirectory, "*.xrrai"))
                {
                    if (!Path.GetFileName(file).StartsWith(".")) // Skip hidden/autosave
                        scenes.Add(file);
                }
            }
            return scenes;
        }

        #endregion

        #region Data Collection

        /// <summary>
        /// Collect current scene state from all managers
        /// </summary>
        void CollectSceneData()
        {
            CollectStrokes();
            CollectHolograms();
            CollectVFXInstances();
            CollectAnchors();
            UpdateBounds();
        }

        void CollectStrokes()
        {
            if (_brushManager == null) return;

            _currentScene.strokes.Clear();
            _currentScene.brushes.Clear();

            var brushMap = new Dictionary<string, string>(); // BrushData -> brushId

            foreach (var stroke in _brushManager.Strokes)
            {
                if (stroke == null || !stroke.IsFinalized) continue;

                // Register brush if new
                var brushData = stroke.BrushData;
                if (brushData != null && !brushMap.ContainsKey(brushData.BrushId))
                {
                    var brushDef = new BrushDefinition
                    {
                        id = BrushDefinition.GenerateId(),
                        name = brushData.DisplayName,
                        guid = brushData.BrushId,
                        material = brushData.Material?.name ?? "Default",
                        geometry = brushData.GeometryType.ToString().ToLower()
                    };

                    if (brushData.IsAudioReactive)
                    {
                        brushDef.audioReactive = new AudioReactiveSettings
                        {
                            enabled = true,
                            sizeMultiplier = brushData.AudioParams.SizeMultiplierRange.y,
                            frequencyBand = brushData.AudioParams.FrequencyBand
                        };
                    }

                    _currentScene.brushes.Add(brushDef);
                    brushMap[brushData.BrushId] = brushDef.id;
                }

                // Create stroke data
                var strokeData = ConvertToXRRAIStroke(stroke.ToData());
                strokeData.id = XRRAIStrokeData.GenerateId();
                strokeData.brushId = brushMap.GetValueOrDefault(brushData?.BrushId ?? "", "");
                strokeData.layerId = _currentScene.layers.Count > 0 ? _currentScene.layers[0].id : "";

                _currentScene.strokes.Add(strokeData);
            }

            Debug.Log($"[XRRAISceneManager] Collected {_currentScene.strokes.Count} strokes, {_currentScene.brushes.Count} brushes");
        }

        void CollectHolograms()
        {
            _currentScene.holograms.Clear();

            // Find all HologramSource components
            var holoSources = FindObjectsByType<Hologram.HologramSource>(FindObjectsSortMode.None);
            foreach (var source in holoSources)
            {
                var holoData = new HologramData
                {
                    id = HologramData.GenerateId(),
                    type = "live",
                    nodeId = GetOrCreateNodeId(source.transform),
                    source = new HologramSource
                    {
                        type = "ARDepthSource"
                    },
                    quality = "medium"
                };
                _currentScene.holograms.Add(holoData);
            }

            Debug.Log($"[XRRAISceneManager] Collected {_currentScene.holograms.Count} holograms");
        }

        void CollectVFXInstances()
        {
            _currentScene.vfxInstances.Clear();

            var vfxEffects = FindObjectsByType<UnityEngine.VFX.VisualEffect>(FindObjectsSortMode.None);
            foreach (var vfx in vfxEffects)
            {
                var vfxData = new VFXInstanceData
                {
                    id = VFXInstanceData.GenerateId(),
                    assetPath = vfx.visualEffectAsset?.name ?? "Unknown",
                    nodeId = GetOrCreateNodeId(vfx.transform)
                };
                _currentScene.vfxInstances.Add(vfxData);
            }

            Debug.Log($"[XRRAISceneManager] Collected {_currentScene.vfxInstances.Count} VFX instances");
        }

        void CollectAnchors()
        {
            _currentScene.anchors.Clear();

#if UNITY_XR_ARFOUNDATION
            var anchorManager = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARAnchorManager>();
            if (anchorManager != null)
            {
                foreach (var anchor in anchorManager.trackables)
                {
                    var anchorData = new AnchorData
                    {
                        id = AnchorData.GenerateId(),
                        type = "plane",
                        position = new[] { anchor.transform.position.x, anchor.transform.position.y, anchor.transform.position.z },
                        rotation = new[] { anchor.transform.rotation.x, anchor.transform.rotation.y, anchor.transform.rotation.z, anchor.transform.rotation.w },
                        persistent = true,
                        nativeId = anchor.trackableId.ToString()
                    };
                    _currentScene.anchors.Add(anchorData);
                }
            }
#endif

            Debug.Log($"[XRRAISceneManager] Collected {_currentScene.anchors.Count} AR anchors");
        }

        void UpdateBounds()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            // Include strokes
            foreach (var stroke in _currentScene.strokes)
            {
                foreach (var point in stroke.points)
                {
                    var pos = point.Position;
                    if (!hasBounds)
                    {
                        bounds = new Bounds(pos, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(pos);
                    }
                }
            }

            if (hasBounds)
            {
                bounds.Expand(0.5f); // Add padding
                _currentScene.scene.bounds = BoundsData.FromUnityBounds(bounds);
            }
        }

        string GetOrCreateNodeId(Transform t)
        {
            // Simple implementation - just use object name
            // Full implementation would maintain proper hierarchy
            return $"node_{t.GetInstanceID()}";
        }

        #endregion

        #region Scene Reconstruction

        /// <summary>
        /// Reconstruct Unity scene from loaded data
        /// </summary>
        void ReconstructScene()
        {
            // Clear existing strokes
            _brushManager?.ClearAllStrokes();

            // Reconstruct strokes
            ReconstructStrokes();

            Debug.Log("[XRRAISceneManager] Scene reconstructed");
        }

        void ReconstructStrokes()
        {
            if (_brushManager == null || _currentScene == null) return;

            // Build brush lookup
            var brushLookup = new Dictionary<string, BrushData>();
            foreach (var brushDef in _currentScene.brushes)
            {
                var brushData = _brushManager.GetBrush(brushDef.guid);
                if (brushData != null)
                    brushLookup[brushDef.id] = brushData;
            }

            // Reconstruct each stroke
            foreach (var strokeData in _currentScene.strokes)
            {
                if (strokeData.points.Count == 0) continue;

                // Get brush
                BrushData brush = null;
                if (!string.IsNullOrEmpty(strokeData.brushId) && brushLookup.TryGetValue(strokeData.brushId, out var b))
                    brush = b;
                brush ??= _brushManager.CurrentBrush;

                if (brush == null) continue;

                // Set brush and color
                _brushManager.SetBrush(brush);
                _brushManager.SetColor(strokeData.GetUnityColor());
                _brushManager.SetSize(strokeData.size);

                // Recreate stroke
                var firstPoint = strokeData.points[0];
                _brushManager.BeginStroke(firstPoint.Position, firstPoint.Rotation, firstPoint.Pressure);

                for (int i = 1; i < strokeData.points.Count; i++)
                {
                    var point = strokeData.points[i];
                    _brushManager.UpdateStroke(point.Position, point.Rotation, point.Pressure);
                }

                _brushManager.EndStroke();
            }

            Debug.Log($"[XRRAISceneManager] Reconstructed {_currentScene.strokes.Count} strokes");
        }

        #endregion

        #region Export

        /// <summary>
        /// Export scene to glTF/GLB format
        /// </summary>
        public async Task<string> ExportGLTFAsync(string filepath = null)
        {
            if (_currentScene == null)
            {
                OnError?.Invoke("No scene to export");
                return null;
            }

            filepath ??= Path.Combine(SaveDirectory, $"{_currentScene.scene.name}.glb");

            try
            {
                // Collect latest data
                CollectSceneData();

                // Use GLTFExporter
                var exporter = GetComponent<GLTFExporter>() ?? gameObject.AddComponent<GLTFExporter>();
                bool success = await exporter.ExportAsync(_currentScene, filepath);

                if (success)
                {
                    OnExportComplete?.Invoke(filepath);
                    Debug.Log($"[XRRAISceneManager] Exported to glTF: {filepath}");
                    return filepath;
                }
                else
                {
                    OnError?.Invoke("glTF export failed");
                    return null;
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Export failed: {ex.Message}");
                Debug.LogError($"[XRRAISceneManager] Export failed: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Synchronous export wrapper
        /// </summary>
        public string ExportGLTF(string filepath = null)
        {
            return ExportGLTFAsync(filepath).GetAwaiter().GetResult();
        }

        #endregion

        #region Conversion

        /// <summary>
        /// Convert BrushPainting.StrokeData to XRRAIStrokeData
        /// </summary>
        XRRAIStrokeData ConvertToXRRAIStroke(BrushPainting.StrokeData source)
        {
            var result = new XRRAIStrokeData
            {
                color = source.Color,
                size = source.Size,
                points = new List<StrokePoint>()
            };

            foreach (var pt in source.Points)
            {
                result.points.Add(new StrokePoint
                {
                    p = pt.Position,
                    r = pt.Rotation,
                    s = pt.Pressure,
                    t = 0
                });
            }

            return result;
        }

        /// <summary>
        /// Convert XRRAIStrokeData to BrushPainting.StrokeData
        /// </summary>
        BrushPainting.StrokeData ConvertFromXRRAIStroke(XRRAIStrokeData source)
        {
            var result = new BrushPainting.StrokeData
            {
                BrushId = source.brushId,
                Color = source.color,
                Size = source.size,
                Points = new List<BrushPainting.ControlPointData>()
            };

            foreach (var pt in source.points)
            {
                result.Points.Add(new BrushPainting.ControlPointData
                {
                    Position = pt.p,
                    Rotation = pt.r,
                    Pressure = pt.s
                });
            }

            return result;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Set scene metadata
        /// </summary>
        public void SetSceneMetadata(string name, string description = null, List<string> tags = null)
        {
            if (_currentScene == null) return;

            _currentScene.scene.name = name;
            if (description != null)
                _currentScene.scene.description = description;
            if (tags != null)
                _currentScene.scene.tags = tags;

            MarkDirty();
        }

        /// <summary>
        /// Delete a saved scene file
        /// </summary>
        public bool DeleteScene(string filepath)
        {
            try
            {
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                    Debug.Log($"[XRRAISceneManager] Deleted: {filepath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[XRRAISceneManager] Delete failed: {ex.Message}");
            }
            return false;
        }

        #endregion
    }
}
