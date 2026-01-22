// StrokeManager.cs - Manages stroke recording, persistence, and manipulation (spec-012)
// Handles undo/redo, save/load, and stroke selection

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MetavidoVFX.Painting
{
    /// <summary>
    /// Manages all recorded strokes in the session.
    /// Provides undo/redo, save/load, and stroke manipulation.
    /// </summary>
    public class StrokeManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Capacity")]
        [SerializeField] private int _maxStrokes = 100;
        [SerializeField] private int _maxPointsPerStroke = 1000;
        [SerializeField] private int _undoStackSize = 20;

        [Header("Storage")]
        [SerializeField] private string _saveDirectory = "BrushStrokes";

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region Events

        public event Action<Stroke> OnStrokeAdded;
        public event Action<Stroke> OnStrokeRemoved;
        public event Action<Stroke> OnStrokeSelected;
        public event Action OnStrokesCleared;
        public event Action OnUndoPerformed;
        public event Action OnRedoPerformed;

        #endregion

        #region Private State

        private readonly List<Stroke> _strokes = new List<Stroke>();
        private readonly Stack<StrokeAction> _undoStack = new Stack<StrokeAction>();
        private readonly Stack<StrokeAction> _redoStack = new Stack<StrokeAction>();

        private Stroke _selectedStroke;
        private GraphicsBuffer _strokeBuffer;
        private int _totalPointCount;

        #endregion

        #region Stroke Class

        [Serializable]
        public class Stroke
        {
            public string Id;
            public List<StrokePoint> Points;
            public BrushController.BrushType BrushType;
            public float CreatedTime;
            public Bounds Bounds;

            public Stroke()
            {
                Id = Guid.NewGuid().ToString();
                Points = new List<StrokePoint>();
                CreatedTime = Time.time;
            }

            public void RecalculateBounds()
            {
                if (Points.Count == 0)
                {
                    Bounds = new Bounds(Vector3.zero, Vector3.zero);
                    return;
                }

                Vector3 min = Points[0].Position;
                Vector3 max = Points[0].Position;

                foreach (var point in Points)
                {
                    min = Vector3.Min(min, point.Position);
                    max = Vector3.Max(max, point.Position);
                }

                Bounds = new Bounds((min + max) * 0.5f, max - min);
            }
        }

        private enum StrokeActionType { Add, Remove }

        private struct StrokeAction
        {
            public StrokeActionType Type;
            public Stroke Stroke;
        }

        #endregion

        #region Properties

        public int StrokeCount => _strokes.Count;
        public int TotalPointCount => _totalPointCount;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public Stroke SelectedStroke => _selectedStroke;
        public IReadOnlyList<Stroke> Strokes => _strokes;

        #endregion

        #region MonoBehaviour

        private void OnDestroy()
        {
            _strokeBuffer?.Release();
            _strokeBuffer = null;
        }

        #endregion

        #region Public API - Stroke Management

        /// <summary>
        /// Add a new stroke from a list of points.
        /// </summary>
        public Stroke AddStroke(List<StrokePoint> points, BrushController.BrushType brushType)
        {
            if (points == null || points.Count == 0)
                return null;

            if (_strokes.Count >= _maxStrokes)
            {
                Debug.LogWarning($"[StrokeManager] Max strokes ({_maxStrokes}) reached");
                return null;
            }

            // Limit points per stroke
            if (points.Count > _maxPointsPerStroke)
            {
                points = points.GetRange(0, _maxPointsPerStroke);
            }

            var stroke = new Stroke
            {
                Points = new List<StrokePoint>(points),
                BrushType = brushType
            };
            stroke.RecalculateBounds();

            _strokes.Add(stroke);
            _totalPointCount += stroke.Points.Count;

            // Record for undo
            PushUndo(new StrokeAction { Type = StrokeActionType.Add, Stroke = stroke });

            OnStrokeAdded?.Invoke(stroke);

            if (_debugMode)
                Debug.Log($"[StrokeManager] Added stroke: {stroke.Id} ({stroke.Points.Count} points)");

            return stroke;
        }

        /// <summary>
        /// Remove a stroke by ID.
        /// </summary>
        public bool RemoveStroke(string strokeId)
        {
            var stroke = _strokes.Find(s => s.Id == strokeId);
            if (stroke == null)
                return false;

            return RemoveStroke(stroke);
        }

        /// <summary>
        /// Remove a stroke.
        /// </summary>
        public bool RemoveStroke(Stroke stroke)
        {
            if (!_strokes.Remove(stroke))
                return false;

            _totalPointCount -= stroke.Points.Count;

            if (_selectedStroke == stroke)
                _selectedStroke = null;

            // Record for undo
            PushUndo(new StrokeAction { Type = StrokeActionType.Remove, Stroke = stroke });

            OnStrokeRemoved?.Invoke(stroke);

            if (_debugMode)
                Debug.Log($"[StrokeManager] Removed stroke: {stroke.Id}");

            return true;
        }

        /// <summary>
        /// Clear all strokes.
        /// </summary>
        public void ClearStrokes()
        {
            // Push all strokes to undo
            foreach (var stroke in _strokes)
            {
                PushUndo(new StrokeAction { Type = StrokeActionType.Remove, Stroke = stroke });
            }

            _strokes.Clear();
            _totalPointCount = 0;
            _selectedStroke = null;

            OnStrokesCleared?.Invoke();

            if (_debugMode)
                Debug.Log("[StrokeManager] All strokes cleared");
        }

        /// <summary>
        /// Select a stroke for manipulation.
        /// </summary>
        public void SelectStroke(Stroke stroke)
        {
            _selectedStroke = stroke;
            OnStrokeSelected?.Invoke(stroke);
        }

        /// <summary>
        /// Deselect the current stroke.
        /// </summary>
        public void DeselectStroke()
        {
            _selectedStroke = null;
            OnStrokeSelected?.Invoke(null);
        }

        /// <summary>
        /// Find stroke at world position (for selection).
        /// </summary>
        public Stroke FindStrokeAtPosition(Vector3 position, float maxDistance = 0.1f)
        {
            Stroke closest = null;
            float closestDist = maxDistance;

            foreach (var stroke in _strokes)
            {
                // Quick bounds check
                float boundsDist = stroke.Bounds.SqrDistance(position);
                if (boundsDist > closestDist * closestDist)
                    continue;

                // Check individual points
                foreach (var point in stroke.Points)
                {
                    float dist = Vector3.Distance(point.Position, position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = stroke;
                    }
                }
            }

            return closest;
        }

        #endregion

        #region Undo/Redo

        private void PushUndo(StrokeAction action)
        {
            _undoStack.Push(action);

            // Limit stack size
            while (_undoStack.Count > _undoStackSize)
            {
                // Remove oldest (convert to array, rebuild)
                var actions = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = 0; i < actions.Length - 1; i++)
                {
                    _undoStack.Push(actions[i]);
                }
            }

            // Clear redo on new action
            _redoStack.Clear();
        }

        /// <summary>
        /// Undo the last stroke action.
        /// </summary>
        public void Undo()
        {
            if (_undoStack.Count == 0)
                return;

            var action = _undoStack.Pop();

            // Reverse the action
            switch (action.Type)
            {
                case StrokeActionType.Add:
                    _strokes.Remove(action.Stroke);
                    _totalPointCount -= action.Stroke.Points.Count;
                    OnStrokeRemoved?.Invoke(action.Stroke);
                    break;

                case StrokeActionType.Remove:
                    _strokes.Add(action.Stroke);
                    _totalPointCount += action.Stroke.Points.Count;
                    OnStrokeAdded?.Invoke(action.Stroke);
                    break;
            }

            _redoStack.Push(action);
            OnUndoPerformed?.Invoke();

            if (_debugMode)
                Debug.Log($"[StrokeManager] Undo: {action.Type} stroke {action.Stroke.Id}");
        }

        /// <summary>
        /// Redo the last undone action.
        /// </summary>
        public void Redo()
        {
            if (_redoStack.Count == 0)
                return;

            var action = _redoStack.Pop();

            // Re-apply the action
            switch (action.Type)
            {
                case StrokeActionType.Add:
                    _strokes.Add(action.Stroke);
                    _totalPointCount += action.Stroke.Points.Count;
                    OnStrokeAdded?.Invoke(action.Stroke);
                    break;

                case StrokeActionType.Remove:
                    _strokes.Remove(action.Stroke);
                    _totalPointCount -= action.Stroke.Points.Count;
                    OnStrokeRemoved?.Invoke(action.Stroke);
                    break;
            }

            _undoStack.Push(action);
            OnRedoPerformed?.Invoke();

            if (_debugMode)
                Debug.Log($"[StrokeManager] Redo: {action.Type} stroke {action.Stroke.Id}");
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Save all strokes to a file.
        /// </summary>
        public void SaveStrokes(string filename)
        {
            string directory = Path.Combine(Application.persistentDataPath, _saveDirectory);
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, filename + ".json");

            var saveData = new StrokeSaveData
            {
                Version = 1,
                Strokes = _strokes
            };

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(path, json);

            if (_debugMode)
                Debug.Log($"[StrokeManager] Saved {_strokes.Count} strokes to {path}");
        }

        /// <summary>
        /// Load strokes from a file.
        /// </summary>
        public void LoadStrokes(string filename)
        {
            string path = Path.Combine(Application.persistentDataPath, _saveDirectory, filename + ".json");

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[StrokeManager] File not found: {path}");
                return;
            }

            string json = File.ReadAllText(path);
            var saveData = JsonUtility.FromJson<StrokeSaveData>(json);

            if (saveData.Strokes != null)
            {
                ClearStrokes();

                foreach (var stroke in saveData.Strokes)
                {
                    _strokes.Add(stroke);
                    _totalPointCount += stroke.Points.Count;
                    OnStrokeAdded?.Invoke(stroke);
                }

                if (_debugMode)
                    Debug.Log($"[StrokeManager] Loaded {_strokes.Count} strokes from {path}");
            }
        }

        /// <summary>
        /// List available save files.
        /// </summary>
        public string[] ListSaveFiles()
        {
            string directory = Path.Combine(Application.persistentDataPath, _saveDirectory);

            if (!Directory.Exists(directory))
                return Array.Empty<string>();

            var files = Directory.GetFiles(directory, "*.json");
            var names = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                names[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            return names;
        }

        [Serializable]
        private class StrokeSaveData
        {
            public int Version;
            public List<Stroke> Strokes;
        }

        #endregion

        #region GPU Buffer

        /// <summary>
        /// Get or create a GraphicsBuffer containing all stroke points.
        /// Useful for VFX Graph rendering.
        /// </summary>
        public GraphicsBuffer GetStrokeBuffer()
        {
            if (_totalPointCount == 0)
                return null;

            // Recreate buffer if size changed
            if (_strokeBuffer == null || _strokeBuffer.count < _totalPointCount)
            {
                _strokeBuffer?.Release();
                _strokeBuffer = new GraphicsBuffer(
                    GraphicsBuffer.Target.Structured,
                    _totalPointCount,
                    StrokePoint.Stride);
            }

            // Flatten all points into array
            var allPoints = new StrokePoint[_totalPointCount];
            int offset = 0;

            foreach (var stroke in _strokes)
            {
                stroke.Points.CopyTo(allPoints, offset);
                offset += stroke.Points.Count;
            }

            _strokeBuffer.SetData(allPoints);
            return _strokeBuffer;
        }

        #endregion
    }
}
