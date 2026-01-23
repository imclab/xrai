// Spec011DemoSetup.cs - Runtime demo for Open Brush Integration
// Part of Spec 011: Open Brush Integration
//
// Demonstrates all 100 Open Brush brushes with keyboard controls:
// - 1-9: Select brush by index
// - Q/E: Previous/Next brush
// - R/G/B: Cycle colors
// - +/-: Adjust size
// - Space: Begin/End stroke (follows camera)
// - C: Clear all strokes
// - M: Toggle mirror mode
// - A: Toggle audio reactive preview

using System.Collections.Generic;
using UnityEngine;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// Demo controller for Spec 011: Open Brush Integration.
    /// Loads all 100 brushes and provides keyboard controls for testing.
    /// </summary>
    public class Spec011DemoSetup : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] BrushManager _brushManager;
        [SerializeField] bool _loadFullCatalog = true;
        [SerializeField] bool _enableKeyboardControls = true;

        [Header("Demo Drawing")]
        [SerializeField] Transform _drawOrigin;
        [SerializeField] float _drawDistance = 0.5f;
        [SerializeField] float _drawSpeed = 2f;

        [Header("UI Display")]
        [SerializeField] bool _showDebugUI = true;

        // State
        int _currentBrushIndex;
        int _currentColorIndex;
        bool _isDrawing;
        float _drawTime;
        List<BrushData> _catalog;

        // Preset colors
        readonly Color[] _presetColors = new[]
        {
            Color.white,
            Color.red,
            new Color(1f, 0.5f, 0f), // Orange
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            new Color(0.5f, 0f, 1f), // Purple
            Color.magenta,
            new Color(1f, 0.75f, 0.8f), // Pink
        };

        void Start()
        {
            // Find or create BrushManager
            if (_brushManager == null)
            {
                _brushManager = FindAnyObjectByType<BrushManager>();
                if (_brushManager == null)
                {
                    var go = new GameObject("BrushManager");
                    _brushManager = go.AddComponent<BrushManager>();
                }
            }

            // Load brush catalog
            LoadBrushCatalog();

            // Set draw origin to camera if not set
            if (_drawOrigin == null && Camera.main != null)
            {
                _drawOrigin = Camera.main.transform;
            }

            Debug.Log($"[Spec011Demo] Initialized with {_catalog?.Count ?? 0} brushes");
        }

        void LoadBrushCatalog()
        {
            // Create the catalog
            _catalog = _loadFullCatalog
                ? BrushCatalogFactory.CreateFullCatalog()
                : BrushCatalogFactory.CreateEssentialCatalog();

            // Inject into BrushManager via reflection (since _brushCatalog is serialized)
            var catalogField = typeof(BrushManager).GetField("_brushCatalog",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (catalogField != null)
            {
                catalogField.SetValue(_brushManager, _catalog);

                // Re-initialize the catalog
                var initMethod = typeof(BrushManager).GetMethod("InitializeCatalog",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                initMethod?.Invoke(_brushManager, null);
            }

            // Select first brush
            if (_catalog.Count > 0)
            {
                _brushManager.SetBrush(_catalog[0]);
            }

            Debug.Log($"[Spec011Demo] Loaded {_catalog.Count} brushes ({(_loadFullCatalog ? "full" : "essential")} catalog)");
        }

        void Update()
        {
            if (_enableKeyboardControls)
            {
                HandleKeyboardInput();
            }

            if (_isDrawing)
            {
                UpdateDrawing();
            }
        }

        void HandleKeyboardInput()
        {
            // Number keys 1-9 for quick brush selection
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectBrush(i);
                }
            }

            // Q/E for previous/next brush
            if (Input.GetKeyDown(KeyCode.Q))
            {
                SelectBrush(_currentBrushIndex - 1);
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                SelectBrush(_currentBrushIndex + 1);
            }

            // R/G/B for cycling colors
            if (Input.GetKeyDown(KeyCode.R))
            {
                CycleColor(1);
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                CycleColor(-1);
            }

            // +/- for size adjustment
            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.Plus))
            {
                _brushManager.AdjustSize(Time.deltaTime * 0.5f);
            }
            if (Input.GetKey(KeyCode.Minus))
            {
                _brushManager.AdjustSize(-Time.deltaTime * 0.5f);
            }

            // Space to begin/end stroke
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleDrawing();
            }

            // C to clear all strokes
            if (Input.GetKeyDown(KeyCode.C))
            {
                _brushManager.ClearAllStrokes();
                Debug.Log("[Spec011Demo] Cleared all strokes");
            }

            // M to toggle mirror mode
            if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleMirror();
            }

            // U to undo last stroke
            if (Input.GetKeyDown(KeyCode.U))
            {
                _brushManager.UndoLastStroke();
            }

            // Tab to cycle through brush categories
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                CycleBrushCategory();
            }
        }

        void SelectBrush(int index)
        {
            if (_catalog == null || _catalog.Count == 0) return;

            _currentBrushIndex = (index + _catalog.Count) % _catalog.Count;
            _brushManager.SetBrush(_catalog[_currentBrushIndex]);

            Debug.Log($"[Spec011Demo] Selected brush {_currentBrushIndex + 1}/{_catalog.Count}: {_catalog[_currentBrushIndex].DisplayName}");
        }

        void CycleColor(int direction)
        {
            _currentColorIndex = (_currentColorIndex + direction + _presetColors.Length) % _presetColors.Length;
            _brushManager.SetColor(_presetColors[_currentColorIndex]);

            Debug.Log($"[Spec011Demo] Color changed to preset {_currentColorIndex + 1}");
        }

        void ToggleDrawing()
        {
            if (_isDrawing)
            {
                _brushManager.EndStroke();
                _isDrawing = false;
                Debug.Log("[Spec011Demo] Stroke ended");
            }
            else
            {
                Vector3 startPos = GetDrawPosition();
                _brushManager.BeginStroke(startPos, Quaternion.identity);
                _isDrawing = true;
                _drawTime = 0f;
                Debug.Log("[Spec011Demo] Stroke started");
            }
        }

        void UpdateDrawing()
        {
            _drawTime += Time.deltaTime;
            Vector3 pos = GetDrawPosition();
            _brushManager.UpdateStroke(pos, Quaternion.identity);
        }

        Vector3 GetDrawPosition()
        {
            if (_drawOrigin == null) return Vector3.zero;

            // Draw in front of the camera with some motion
            Vector3 offset = _drawOrigin.forward * _drawDistance;

            // Add some circular motion for interesting strokes
            float t = _drawTime * _drawSpeed;
            offset += _drawOrigin.right * Mathf.Sin(t) * 0.1f;
            offset += _drawOrigin.up * Mathf.Cos(t * 0.7f) * 0.1f;

            return _drawOrigin.position + offset;
        }

        void ToggleMirror()
        {
            var mirror = _brushManager.MirrorHandler;
            if (mirror != null)
            {
                // Cycle through mirror modes: Off → SinglePlane → DoublePlane → Radial → Off
                mirror.CycleMode();
                Debug.Log($"[Spec011Demo] Mirror mode: {mirror.Mode}");
            }
            else
            {
                Debug.LogWarning("[Spec011Demo] BrushMirror component not found");
            }
        }

        void CycleBrushCategory()
        {
            if (_catalog == null || _catalog.Count == 0) return;

            // Get current category
            var currentCategory = _brushManager.CurrentBrush?.Category ?? BrushCategory.Basic;

            // Find next brush in different category
            int startIndex = _currentBrushIndex;
            for (int i = 1; i < _catalog.Count; i++)
            {
                int checkIndex = (startIndex + i) % _catalog.Count;
                if (_catalog[checkIndex].Category != currentCategory)
                {
                    SelectBrush(checkIndex);
                    Debug.Log($"[Spec011Demo] Switched to category: {_catalog[checkIndex].Category}");
                    return;
                }
            }
        }

        void OnGUI()
        {
            if (!_showDebugUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 350, 400));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>Spec 011: Open Brush Demo</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Space(5);

            // Current brush info
            var brush = _brushManager?.CurrentBrush;
            if (brush != null)
            {
                GUILayout.Label($"Brush: {brush.DisplayName} ({_currentBrushIndex + 1}/{_catalog?.Count ?? 0})");
                GUILayout.Label($"Category: {brush.Category}");
                GUILayout.Label($"Type: {brush.GeometryType}");
                GUILayout.Label($"Size: {_brushManager.CurrentSize:F3}m");
                GUILayout.Label($"Audio Reactive: {(brush.IsAudioReactive ? "Yes" : "No")}");
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>Controls:</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("1-9: Select brush");
            GUILayout.Label("Q/E: Prev/Next brush");
            GUILayout.Label("Tab: Cycle category");
            GUILayout.Label("R/G: Cycle colors");
            GUILayout.Label("+/-: Adjust size");
            GUILayout.Label("Space: Draw stroke");
            GUILayout.Label("M: Toggle mirror");
            GUILayout.Label("U: Undo last");
            GUILayout.Label("C: Clear all");

            GUILayout.Space(10);
            GUILayout.Label($"Strokes: {_brushManager?.Strokes?.Count ?? 0}");
            GUILayout.Label($"Drawing: {(_isDrawing ? "YES" : "no")}");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #region Editor Setup

        [ContextMenu("Setup Scene Components")]
        void SetupSceneComponents()
        {
            // Add BrushMirror if missing
            if (_brushManager != null && _brushManager.GetComponent<BrushMirror>() == null)
            {
                _brushManager.gameObject.AddComponent<BrushMirror>();
                Debug.Log("[Spec011Demo] Added BrushMirror component");
            }

            // Create stroke prefab parent if needed
            var strokesParent = GameObject.Find("Strokes");
            if (strokesParent == null)
            {
                strokesParent = new GameObject("Strokes");
                strokesParent.transform.SetParent(_brushManager?.transform);
            }
        }

        #endregion
    }
}
