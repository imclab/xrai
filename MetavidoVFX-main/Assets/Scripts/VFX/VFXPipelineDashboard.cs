using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.InputSystem;
using MetavidoVFX.VFX.Binders;

/// <summary>
/// Real-time pipeline visualization dashboard.
/// Shows: FPS, pipeline flow, binding status, performance, memory.
/// Toggle with Tab key.
///
/// Goal: Full visibility of pipeline flow, easy to measure performance & debug
/// </summary>
public class VFXPipelineDashboard : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] bool _visible = true;
    [SerializeField] KeyCode _toggleKey = KeyCode.Tab;
    [SerializeField] float _updateInterval = 0.1f;

    [Header("Size & Position")]
    [SerializeField] DashboardPosition _position = DashboardPosition.TopRight;
    [SerializeField] float _width = 380f;
    [SerializeField] float _scale = 1.0f;
    [SerializeField] Vector2 _customOffset = Vector2.zero;
    [SerializeField] float _padding = 10f;

    public enum DashboardPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Custom
    }

    [Header("Style")]
    [SerializeField] int _fontSize = 14;
    [SerializeField] Color _backgroundColor = new Color(0, 0, 0, 0.85f);
    [SerializeField] Color _okColor = new Color(0.3f, 0.9f, 0.3f);
    [SerializeField] Color _warningColor = new Color(0.9f, 0.7f, 0.2f);
    [SerializeField] Color _errorColor = new Color(0.9f, 0.3f, 0.3f);

    // FPS tracking
    float[] _fpsHistory = new float[60];
    int _fpsHistoryIndex = 0;
    float _currentFps;
    float _minFps = float.MaxValue;
    float _maxFps = 0;
    float _avgFps;

    // Cached data
    float _lastUpdateTime;
    int _activeVFXCount;
    int _totalVFXCount;
    int _totalParticles;
    List<VFXARBinder> _binders = new List<VFXARBinder>();
    List<VisualEffect> _allVFX = new List<VisualEffect>();

    // Physics tracking (spec-007 T-016)
    Vector3 _cameraVelocity;
    float _cameraSpeed;
    Vector3 _lastCameraPosition;
    Vector3 _currentGravity = new Vector3(0, -9.81f, 0);
    int _meshPointCount;
    bool _hasPhysicsBinders;

    // Styles
    GUIStyle _boxStyle;
    GUIStyle _headerStyle;
    GUIStyle _labelStyle;
    GUIStyle _valueStyle;
    Texture2D _backgroundTex;

    void Update()
    {
        // Toggle visibility with Tab key (using new Input System)
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        {
            _visible = !_visible;
        }

        // Track FPS
        _fpsHistory[_fpsHistoryIndex] = 1f / Time.unscaledDeltaTime;
        _fpsHistoryIndex = (_fpsHistoryIndex + 1) % _fpsHistory.Length;

        // Update stats periodically
        if (Time.unscaledTime - _lastUpdateTime > _updateInterval)
        {
            UpdateStats();
            _lastUpdateTime = Time.unscaledTime;
        }
    }

    void UpdateStats()
    {
        // Calculate FPS stats
        _currentFps = _fpsHistory[(_fpsHistoryIndex + _fpsHistory.Length - 1) % _fpsHistory.Length];
        _minFps = Mathf.Min(_minFps, _currentFps);
        _maxFps = Mathf.Max(_maxFps, _currentFps);
        _avgFps = _fpsHistory.Average();

        // Reset min/max every 5 seconds
        if (Time.frameCount % 300 == 0)
        {
            _minFps = _currentFps;
            _maxFps = _currentFps;
        }

        // Find all VFX and binders
        _binders.Clear();
        _binders.AddRange(FindObjectsByType<VFXARBinder>(FindObjectsSortMode.None));

        _allVFX.Clear();
        _allVFX.AddRange(FindObjectsByType<VisualEffect>(FindObjectsSortMode.None).OrderBy(v => v.name));

        _totalVFXCount = _allVFX.Count;
        _activeVFXCount = _allVFX.Count(v => v.enabled && v.gameObject.activeInHierarchy);

        // Count particles
        _totalParticles = 0;
        foreach (var vfx in _allVFX)
        {
            if (vfx.enabled && vfx.gameObject.activeInHierarchy)
            {
                _totalParticles += vfx.aliveParticleCount;
            }
        }

        // Physics tracking (spec-007 T-016)
        UpdatePhysicsStats();
    }

    void UpdatePhysicsStats()
    {
        // Camera velocity
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            if (_lastCameraPosition != Vector3.zero)
            {
                Vector3 delta = mainCamera.transform.position - _lastCameraPosition;
                _cameraVelocity = delta / _updateInterval;
                _cameraSpeed = _cameraVelocity.magnitude;
            }
            _lastCameraPosition = mainCamera.transform.position;
        }

        // Get mesh point count from global shader property
        _meshPointCount = Shader.GetGlobalInt("_MeshPointCount");

        // Check for physics binders and get gravity from first active one
        _hasPhysicsBinders = false;
        var physicsBinders = FindObjectsByType<VFXPhysicsBinder>(FindObjectsSortMode.None);
        foreach (var binder in physicsBinders)
        {
            if (binder.enabled && binder.enableGravity)
            {
                _hasPhysicsBinders = true;
                _currentGravity = binder.useWorldGravity
                    ? new Vector3(0, binder.gravityStrength, 0)
                    : binder.gravityDirection.normalized * Mathf.Abs(binder.gravityStrength);
                break;
            }
        }
    }

    void OnGUI()
    {
        if (!_visible) return;

        EnsureStyles();

        // Apply scale
        float scaledFontSize = Mathf.RoundToInt(_fontSize * _scale);
        float scaledWidth = _width * _scale;
        float scaledPadding = _padding * _scale;

        // Responsive width - use min of configured width or 90% of screen width
        float width = Mathf.Min(scaledWidth, Screen.width * 0.9f);
        float height = GetPanelHeight() * _scale;

        // Calculate position based on setting
        float x, y;
        switch (_position)
        {
            case DashboardPosition.TopLeft:
                x = scaledPadding;
                y = scaledPadding;
                break;
            case DashboardPosition.TopRight:
                x = Screen.width - width - scaledPadding;
                y = scaledPadding;
                break;
            case DashboardPosition.BottomLeft:
                x = scaledPadding;
                y = Screen.height - height - scaledPadding - 10;
                break;
            case DashboardPosition.BottomRight:
                x = Screen.width - width - scaledPadding;
                y = Screen.height - height - scaledPadding - 10;
                break;
            case DashboardPosition.Custom:
            default:
                x = _customOffset.x;
                y = _customOffset.y;
                break;
        }

        // Ensure we're on screen
        x = Mathf.Clamp(x, 5, Screen.width - width - 5);
        y = Mathf.Clamp(y, 5, Screen.height - height - 5);

        // Apply GUI scale
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.identity, new Vector3(_scale, _scale, 1));
        x = 0;
        y = 0;

        // Background
        GUI.Box(new Rect(x - 5, y - 5, _width + 10, GetPanelHeight() + 10), "", _boxStyle);

        // Header
        GUI.Label(new Rect(x, y, _width, 24), "VFX Pipeline Dashboard", _headerStyle);
        y += 28;

        // Quick Stats Row
        DrawQuickStats(ref x, ref y, _width);
        y += 8;

        // Pipeline Flow Section
        DrawPipelineFlow(ref x, ref y, _width);
        y += 8;

        // Performance Section
        DrawPerformance(ref x, ref y, _width);
        y += 8;

        // Active VFX Section
        DrawActiveVFX(ref x, ref y, _width);
        y += 8;

        // Physics Section (spec-007 T-016)
        DrawPhysicsInfo(ref x, ref y, _width);

        // Restore GUI matrix
        GUI.matrix = oldMatrix;
    }

    void DrawQuickStats(ref float x, ref float y, float width)
    {
        // FPS | Active VFX | Particles
        Color fpsColor = _currentFps >= 55 ? _okColor : (_currentFps >= 30 ? _warningColor : _errorColor);

        string fpsText = $"FPS: {_currentFps:F0}";
        string vfxText = $"VFX: {_activeVFXCount}/{_totalVFXCount}";
        string particleText = $"Particles: {FormatNumber(_totalParticles)}";

        float colWidth = width / 3f;

        var oldColor = GUI.color;
        GUI.color = fpsColor;
        GUI.Label(new Rect(x, y, colWidth, 20), fpsText, _valueStyle);
        GUI.color = oldColor;

        GUI.Label(new Rect(x + colWidth, y, colWidth, 20), vfxText, _valueStyle);
        GUI.Label(new Rect(x + colWidth * 2, y, colWidth, 20), particleText, _valueStyle);
        y += 22;
    }

    void DrawPipelineFlow(ref float x, ref float y, float width)
    {
        GUI.Label(new Rect(x, y, width, 20), "Pipeline Flow", _headerStyle);
        y += 22;

        // ARDepthSource status
        var source = ARDepthSource.Instance;
        bool sourceOk = source != null && source.IsReady;
        DrawStatusLine(ref x, ref y, width, "ARDepthSource",
            sourceOk ? "Ready" : (source != null ? "No Data" : "Missing"),
            sourceOk ? _okColor : _errorColor);

        if (sourceOk)
        {
            // Sub-items
            DrawSubItem(ref x, ref y, width, "DepthMap",
                $"{source.DepthMap?.width}x{source.DepthMap?.height}",
                source.DepthMap != null);

            DrawSubItem(ref x, ref y, width, "StencilMap",
                $"{source.StencilMap?.width}x{source.StencilMap?.height}",
                source.StencilMap != null);

            DrawSubItem(ref x, ref y, width, "PositionMap",
                $"{source.PositionMap?.width}x{source.PositionMap?.height}",
                source.PositionMap != null);

            DrawSubItem(ref x, ref y, width, "RayParams",
                $"({source.RayParams.z:F2}, {source.RayParams.w:F2})",
                true);

            DrawSubItem(ref x, ref y, width, "ComputeTime",
                $"{source.LastComputeTimeMs:F2}ms",
                source.LastComputeTimeMs < 2f);
        }

        // AudioBridge status
        var audio = AudioBridge.Instance;
        bool audioOk = audio != null;
        DrawStatusLine(ref x, ref y, width, "AudioBridge",
            audioOk ? $"Vol={audio.Volume:F2}" : "Not Active",
            audioOk ? _okColor : _warningColor);

        // VFXARBinder summary
        int boundCount = _binders.Count(b => b.IsBound);
        DrawStatusLine(ref x, ref y, width, "VFXARBinder",
            $"{boundCount}/{_binders.Count} bound",
            boundCount > 0 ? _okColor : _warningColor);
    }

    void DrawPerformance(ref float x, ref float y, float width)
    {
        GUI.Label(new Rect(x, y, width, 20), "Performance", _headerStyle);
        y += 22;

        // FPS Graph (simple ASCII-style)
        string graphLine = "";
        float graphMin = _fpsHistory.Min();
        float graphMax = _fpsHistory.Max();
        float range = Mathf.Max(graphMax - graphMin, 1f);

        for (int i = 0; i < 30; i++)
        {
            int idx = (_fpsHistoryIndex + i * 2) % _fpsHistory.Length;
            float normalized = (_fpsHistory[idx] - graphMin) / range;
            int level = Mathf.Clamp((int)(normalized * 7), 0, 7);
            char[] blocks = { ' ', '_', '.', '-', '=', '#', '*', '@' };
            graphLine += blocks[level];
        }

        GUI.Label(new Rect(x, y, width, 20), $"[{graphLine}]", _labelStyle);
        y += 18;

        GUI.Label(new Rect(x, y, width, 20),
            $"Min: {_minFps:F0}  Avg: {_avgFps:F0}  Max: {_maxFps:F0}",
            _labelStyle);
        y += 20;

        // Memory estimate
        var source = ARDepthSource.Instance;
        if (source != null)
        {
            long totalBytes = 0;
            if (source.PositionMap != null)
                totalBytes += source.PositionMap.width * source.PositionMap.height * 16; // ARGBFloat
            if (source.VelocityMap != null)
                totalBytes += source.VelocityMap.width * source.VelocityMap.height * 16;

            GUI.Label(new Rect(x, y, width, 20),
                $"RenderTexture Memory: {totalBytes / 1024f:F0} KB",
                _labelStyle);
            y += 20;
        }
    }

    void DrawActiveVFX(ref float x, ref float y, float width)
    {
        GUI.Label(new Rect(x, y, width, 20), $"Active VFX ({_activeVFXCount})", _headerStyle);
        y += 22;

        // Show top 5 active VFX
        var activeVFX = _allVFX
            .Where(v => v.enabled && v.gameObject.activeInHierarchy)
            .OrderByDescending(v => v.aliveParticleCount)
            .Take(5);

        foreach (var vfx in activeVFX)
        {
            var binder = vfx.GetComponent<VFXARBinder>();
            bool hasBinder = binder != null;
            bool isBound = binder?.IsBound ?? false;

            string status = hasBinder ? (isBound ? "bound" : "no data") : "no binder";
            Color statusColor = isBound ? _okColor : (hasBinder ? _warningColor : _errorColor);

            GUI.Label(new Rect(x + 10, y, width - 120, 18), vfx.name, _labelStyle);

            var oldColor = GUI.color;
            GUI.color = statusColor;
            GUI.Label(new Rect(x + width - 110, y, 50, 18), status, _labelStyle);
            GUI.color = oldColor;

            GUI.Label(new Rect(x + width - 55, y, 50, 18),
                $"{FormatNumber(vfx.aliveParticleCount)}p",
                _labelStyle);

            y += 18;
        }

        if (_activeVFXCount > 5)
        {
            GUI.Label(new Rect(x + 10, y, width, 18), $"... and {_activeVFXCount - 5} more", _labelStyle);
            y += 18;
        }
    }

    /// <summary>
    /// Physics visualization section (spec-007 T-016)
    /// Shows: Camera velocity vector, gravity direction, collision count
    /// </summary>
    void DrawPhysicsInfo(ref float x, ref float y, float width)
    {
        GUI.Label(new Rect(x, y, width, 20), "Physics (T-016)", _headerStyle);
        y += 22;

        // Camera Velocity Vector
        string velText = $"({_cameraVelocity.x:F2}, {_cameraVelocity.y:F2}, {_cameraVelocity.z:F2})";
        Color velColor = _cameraSpeed > 0.1f ? _okColor : new Color(0.6f, 0.6f, 0.6f);
        DrawStatusLine(ref x, ref y, width, "Camera Velocity", velText, velColor);

        // Camera Speed
        DrawStatusLine(ref x, ref y, width, "Speed", $"{_cameraSpeed:F2} m/s",
            _cameraSpeed > 1f ? _warningColor : velColor);

        // Gravity Direction
        string gravText = _hasPhysicsBinders
            ? $"({_currentGravity.x:F1}, {_currentGravity.y:F1}, {_currentGravity.z:F1})"
            : "Not Active";
        DrawStatusLine(ref x, ref y, width, "Gravity", gravText,
            _hasPhysicsBinders ? _okColor : new Color(0.5f, 0.5f, 0.5f));

        // AR Mesh Collision Count
        string meshText = _meshPointCount > 0 ? $"{FormatNumber(_meshPointCount)} points" : "No Mesh";
        DrawStatusLine(ref x, ref y, width, "Mesh Collision", meshText,
            _meshPointCount > 0 ? _okColor : new Color(0.5f, 0.5f, 0.5f));
    }

    void DrawStatusLine(ref float x, ref float y, float width, string label, string value, Color color)
    {
        GUI.Label(new Rect(x, y, 150, 18), label, _labelStyle);

        var oldColor = GUI.color;
        GUI.color = color;
        GUI.Label(new Rect(x + 155, y, width - 155, 18), value, _valueStyle);
        GUI.color = oldColor;

        y += 18;
    }

    void DrawSubItem(ref float x, ref float y, float width, string label, string value, bool ok)
    {
        GUI.Label(new Rect(x + 15, y, 130, 16), $"- {label}:", _labelStyle);

        var oldColor = GUI.color;
        GUI.color = ok ? _okColor : _errorColor;
        GUI.Label(new Rect(x + 150, y, width - 150, 16), value, _labelStyle);
        GUI.color = oldColor;

        y += 16;
    }

    float GetPanelHeight()
    {
        float height = 30; // Header
        height += 25; // Quick stats
        height += 10 + 22 + (ARDepthSource.Instance?.IsReady == true ? 6 * 16 : 0) + 18 + 18 + 18; // Pipeline
        height += 10 + 22 + 20 + 20 + 20; // Performance
        height += 10 + 22 + Mathf.Min(_activeVFXCount, 5) * 18 + (_activeVFXCount > 5 ? 18 : 0); // VFX
        height += 10 + 22 + 4 * 18; // Physics (T-016): header + 4 lines
        return height;
    }

    void EnsureStyles()
    {
        if (_boxStyle != null) return;

        // Create background texture
        _backgroundTex = new Texture2D(1, 1);
        _backgroundTex.SetPixel(0, 0, _backgroundColor);
        _backgroundTex.Apply();

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _backgroundTex }
        };

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = _fontSize + 2,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = _fontSize,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        _valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = _fontSize,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
    }

    string FormatNumber(int num)
    {
        if (num >= 1000000) return $"{num / 1000000f:F1}M";
        if (num >= 1000) return $"{num / 1000f:F1}K";
        return num.ToString();
    }

    void OnDestroy()
    {
        if (_backgroundTex != null)
            Destroy(_backgroundTex);
    }

    #region Public API

    public void Show() => _visible = true;
    public void Hide() => _visible = false;
    public void Toggle() => _visible = !_visible;
    public bool IsVisible => _visible;

    // Size & Position API
    public float Width { get => _width; set => _width = Mathf.Max(200, value); }
    public float Scale { get => _scale; set => _scale = Mathf.Clamp(value, 0.5f, 2f); }
    public float Padding { get => _padding; set => _padding = Mathf.Max(0, value); }
    public DashboardPosition Position { get => _position; set => _position = value; }
    public Vector2 CustomOffset { get => _customOffset; set => _customOffset = value; }

    public void SetPosition(DashboardPosition pos, Vector2? customOffset = null)
    {
        _position = pos;
        if (customOffset.HasValue)
            _customOffset = customOffset.Value;
    }

    public void SetSize(float width, float scale = 1f)
    {
        _width = Mathf.Max(200, width);
        _scale = Mathf.Clamp(scale, 0.5f, 2f);
    }

    #endregion
}
