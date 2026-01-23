// BrushManager.cs - Singleton brush and stroke management
// Part of Spec 011: OpenBrush Integration
//
// Central manager for brush catalog, active stroke creation, and scene management.
// Handles brush selection, color, size, and stroke lifecycle.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XRRAI.Audio;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// Singleton manager for brush painting system.
    /// Handles brush catalog, stroke creation, and scene persistence.
    /// </summary>
    public class BrushManager : MonoBehaviour
    {
        public static BrushManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Parent transform for all strokes")]
        [SerializeField] Transform _strokeParent;

        [Tooltip("Prefab for brush strokes")]
        [SerializeField] GameObject _strokePrefab;

        [Tooltip("All available brushes")]
        [SerializeField] List<BrushData> _brushCatalog = new();

        [Tooltip("Default brush on startup")]
        [SerializeField] BrushData _defaultBrush;

        [Header("Current Settings")]
        [SerializeField] Color _currentColor = Color.white;
        [SerializeField] float _currentSize = 0.02f;

        // Events
        public event Action<BrushData> OnBrushChanged;
        public event Action<Color> OnColorChanged;
        public event Action<float> OnSizeChanged;
        public event Action<BrushStroke> OnStrokeCreated;
        public event Action<BrushStroke> OnStrokeFinalized;

        // State
        BrushData _currentBrush;
        BrushStroke _activeStroke;
        List<BrushStroke> _strokes = new();
        Dictionary<string, BrushData> _brushById = new();
        BrushMirror _mirrorHandler;
        UnifiedAudioReactive _audioHandler;

        // Properties
        public BrushData CurrentBrush => _currentBrush;
        public Color CurrentColor => _currentColor;
        public float CurrentSize => _currentSize;
        public BrushStroke ActiveStroke => _activeStroke;
        public IReadOnlyList<BrushStroke> Strokes => _strokes;
        public IReadOnlyList<BrushData> BrushCatalog => _brushCatalog;
        public bool IsDrawing => _activeStroke != null;
        public BrushMirror MirrorHandler => _mirrorHandler;
        public UnifiedAudioReactive AudioHandler => _audioHandler;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeCatalog();

            if (_strokeParent == null)
            {
                var go = new GameObject("Strokes");
                go.transform.SetParent(transform);
                _strokeParent = go.transform;
            }

            _mirrorHandler = GetComponent<BrushMirror>();
            _audioHandler = UnifiedAudioReactive.Instance;

            // Set default brush
            if (_currentBrush == null && _defaultBrush != null)
                SetBrush(_defaultBrush);
            else if (_currentBrush == null && _brushCatalog.Count > 0)
                SetBrush(_brushCatalog[0]);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void InitializeCatalog()
        {
            _brushById.Clear();
            foreach (var brush in _brushCatalog)
            {
                if (brush != null && !string.IsNullOrEmpty(brush.BrushId))
                    _brushById[brush.BrushId] = brush;
            }
        }

        #region Brush Selection

        /// <summary>
        /// Set the current brush by reference
        /// </summary>
        public void SetBrush(BrushData brush)
        {
            if (brush == null) return;

            _currentBrush = brush;
            OnBrushChanged?.Invoke(brush);
            Debug.Log($"[BrushManager] Brush changed to: {brush.DisplayName}");
        }

        /// <summary>
        /// Set the current brush by ID
        /// </summary>
        public void SetBrush(string brushId)
        {
            if (_brushById.TryGetValue(brushId, out var brush))
                SetBrush(brush);
            else
                Debug.LogWarning($"[BrushManager] Brush not found: {brushId}");
        }

        /// <summary>
        /// Set the current brush by index
        /// </summary>
        public void SetBrush(int index)
        {
            if (index >= 0 && index < _brushCatalog.Count)
                SetBrush(_brushCatalog[index]);
        }

        /// <summary>
        /// Get a brush by ID
        /// </summary>
        public BrushData GetBrush(string brushId)
        {
            return _brushById.TryGetValue(brushId, out var brush) ? brush : null;
        }

        /// <summary>
        /// Get brushes by category
        /// </summary>
        public List<BrushData> GetBrushesByCategory(BrushCategory category)
        {
            return _brushCatalog.FindAll(b => b.Category == category);
        }

        #endregion

        #region Color & Size

        /// <summary>
        /// Set the current brush color
        /// </summary>
        public void SetColor(Color color)
        {
            _currentColor = color;
            OnColorChanged?.Invoke(color);
        }

        /// <summary>
        /// Set the current brush size
        /// </summary>
        public void SetSize(float size)
        {
            _currentSize = _currentBrush?.ClampSize(size) ?? size;
            OnSizeChanged?.Invoke(_currentSize);
        }

        /// <summary>
        /// Adjust size by normalized delta (0-1 range)
        /// </summary>
        public void AdjustSize(float delta01)
        {
            if (_currentBrush == null) return;

            float range = _currentBrush.SizeRange.y - _currentBrush.SizeRange.x;
            SetSize(_currentSize + delta01 * range);
        }

        #endregion

        #region Stroke Management

        /// <summary>
        /// Begin a new stroke at the given position
        /// </summary>
        public BrushStroke BeginStroke(Vector3 position, Quaternion rotation, float pressure = 1f)
        {
            if (_currentBrush == null)
            {
                Debug.LogWarning("[BrushManager] No brush selected");
                return null;
            }

            // Finalize any active stroke
            if (_activeStroke != null)
                EndStroke();

            // Create new stroke
            _activeStroke = CreateStrokeInstance();
            _activeStroke.Initialize(_currentBrush, _currentColor, _currentSize);
            _activeStroke.AddPoint(position, rotation, pressure);

            _strokes.Add(_activeStroke);
            OnStrokeCreated?.Invoke(_activeStroke);

            // Handle mirroring
            if (_mirrorHandler != null && _mirrorHandler.Enabled)
            {
                // Mirror strokes will be created when this stroke is finalized
            }

            Debug.Log($"[BrushManager] Stroke started with {_currentBrush.DisplayName}");
            return _activeStroke;
        }

        /// <summary>
        /// Update the active stroke with a new point
        /// </summary>
        public bool UpdateStroke(Vector3 position, Quaternion rotation, float pressure = 1f)
        {
            if (_activeStroke == null) return false;

            // Apply audio modulation if enabled (uses UnifiedAudioReactive singleton)
            if (_audioHandler != null && _currentBrush.IsAudioReactive)
            {
                var modulation = _audioHandler.GetBrushModulation(_currentBrush.AudioParams);
                // Apply size modulation
                if (_currentBrush.AudioParams.ModulateSize)
                {
                    float sizeMod = Mathf.Lerp(
                        _currentBrush.AudioParams.SizeMultiplierRange.x,
                        _currentBrush.AudioParams.SizeMultiplierRange.y,
                        modulation.NormalizedLevel);
                    pressure *= sizeMod;
                }
            }

            return _activeStroke.AddPoint(position, rotation, pressure);
        }

        /// <summary>
        /// End the current stroke
        /// </summary>
        public void EndStroke()
        {
            if (_activeStroke == null) return;

            _activeStroke.Finalize();
            OnStrokeFinalized?.Invoke(_activeStroke);

            // Create mirror copies if enabled
            if (_mirrorHandler != null && _mirrorHandler.Enabled)
            {
                var mirrorStrokes = _mirrorHandler.CreateMirrorStrokes(_activeStroke, _strokeParent);
                _strokes.AddRange(mirrorStrokes);
            }

            Debug.Log($"[BrushManager] Stroke ended with {_activeStroke.PointCount} points");
            _activeStroke = null;
        }

        /// <summary>
        /// Cancel the current stroke without saving
        /// </summary>
        public void CancelStroke()
        {
            if (_activeStroke == null) return;

            _strokes.Remove(_activeStroke);
            Destroy(_activeStroke.gameObject);
            _activeStroke = null;

            Debug.Log("[BrushManager] Stroke cancelled");
        }

        /// <summary>
        /// Delete a specific stroke
        /// </summary>
        public void DeleteStroke(BrushStroke stroke)
        {
            if (stroke == null) return;

            _strokes.Remove(stroke);
            Destroy(stroke.gameObject);
        }

        /// <summary>
        /// Clear all strokes
        /// </summary>
        public void ClearAllStrokes()
        {
            foreach (var stroke in _strokes)
            {
                if (stroke != null)
                    Destroy(stroke.gameObject);
            }
            _strokes.Clear();
            _activeStroke = null;

            Debug.Log("[BrushManager] All strokes cleared");
        }

        /// <summary>
        /// Undo the last stroke
        /// </summary>
        public void UndoLastStroke()
        {
            if (_strokes.Count == 0) return;

            var lastStroke = _strokes[^1];
            DeleteStroke(lastStroke);

            Debug.Log("[BrushManager] Last stroke undone");
        }

        BrushStroke CreateStrokeInstance()
        {
            GameObject go;
            if (_strokePrefab != null)
            {
                go = Instantiate(_strokePrefab, _strokeParent);
            }
            else
            {
                go = new GameObject("BrushStroke");
                go.transform.SetParent(_strokeParent);
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
            }

            var stroke = go.GetComponent<BrushStroke>();
            if (stroke == null)
                stroke = go.AddComponent<BrushStroke>();

            return stroke;
        }

        #endregion

        #region Scene Persistence

        /// <summary>
        /// Save all strokes to JSON file
        /// </summary>
        public void SaveScene(string filepath)
        {
            var sceneData = new SceneData
            {
                Version = "1.0",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                Strokes = new List<StrokeData>()
            };

            foreach (var stroke in _strokes)
            {
                if (stroke != null && stroke.IsFinalized)
                    sceneData.Strokes.Add(stroke.ToData());
            }

            string json = JsonUtility.ToJson(sceneData, true);
            File.WriteAllText(filepath, json);

            Debug.Log($"[BrushManager] Scene saved: {filepath} ({sceneData.Strokes.Count} strokes)");
        }

        /// <summary>
        /// Load strokes from JSON file
        /// </summary>
        public void LoadScene(string filepath)
        {
            if (!File.Exists(filepath))
            {
                Debug.LogWarning($"[BrushManager] File not found: {filepath}");
                return;
            }

            ClearAllStrokes();

            string json = File.ReadAllText(filepath);
            var sceneData = JsonUtility.FromJson<SceneData>(json);

            foreach (var strokeData in sceneData.Strokes)
            {
                var brush = GetBrush(strokeData.BrushId);
                if (brush == null)
                {
                    Debug.LogWarning($"[BrushManager] Brush not found for stroke: {strokeData.BrushId}");
                    continue;
                }

                var stroke = CreateStrokeInstance();
                stroke.FromData(strokeData, brush);
                _strokes.Add(stroke);
            }

            Debug.Log($"[BrushManager] Scene loaded: {filepath} ({_strokes.Count} strokes)");
        }

        #endregion

        #region Editor Helpers

        [ContextMenu("Load All Brushes From Resources")]
        void LoadBrushesFromResources()
        {
            var brushes = Resources.LoadAll<BrushData>("Brushes");
            _brushCatalog.Clear();
            _brushCatalog.AddRange(brushes);
            InitializeCatalog();
            Debug.Log($"[BrushManager] Loaded {brushes.Length} brushes from Resources");
        }

        #endregion
    }

    /// <summary>
    /// Scene data for save/load
    /// </summary>
    [Serializable]
    public class SceneData
    {
        public string Version;
        public string CreatedAt;
        public List<StrokeData> Strokes;
    }
}
