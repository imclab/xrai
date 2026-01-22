using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace XRRAI.Debugging
{
    /// <summary>
    /// Runtime VFX property inspector for debugging.
    /// Shows all exposed properties, their types, and current values.
    /// Toggle with F1 key. Select VFX with 1-9 keys.
    /// </summary>
    public class VFXPropertyInspector : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private bool _visible = false;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
        [SerializeField] private KeyCode _cycleKey = KeyCode.F2;

        [Header("Style")]
        [SerializeField] private float _width = 400f;
        [SerializeField] private float _height = 500f;
        [SerializeField] private int _fontSize = 12;
        [SerializeField] private Color _backgroundColor = new Color(0, 0, 0, 0.9f);
        [SerializeField] private Color _headerColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color _boundColor = new Color(0.3f, 0.9f, 0.3f);
        [SerializeField] private Color _unboundColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _errorColor = new Color(0.9f, 0.3f, 0.3f);

        [Header("Runtime Status (Read-Only)")]
        [SerializeField, Tooltip("Total VFX count in scene")]
        private int _totalVFXDisplay = 0;
        [SerializeField, Tooltip("Currently selected VFX index")]
        private int _selectedIndexDisplay = 0;
        [SerializeField, Tooltip("Selected VFX name")]
        private string _selectedVFXNameDisplay = "None";
        [SerializeField, Tooltip("Property count for selected VFX")]
        private int _propertyCountDisplay = 0;
        [SerializeField, Tooltip("Bound properties count")]
        private int _boundPropertiesDisplay = 0;

        private VisualEffect[] _allVFX;
        private int _selectedIndex = 0;
        private Vector2 _scrollPosition;
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private Texture2D _backgroundTex;

        // Property cache
        private List<PropertyInfo> _propertyCache = new List<PropertyInfo>();
        private float _lastRefreshTime;
        private const float RefreshInterval = 0.5f;

        private struct PropertyInfo
        {
            public string Name;
            public string Type;
            public string Value;
            public bool IsBound;
            public bool HasError;
        }

        private void Update()
        {
            // Toggle visibility
            if (Input.GetKeyDown(_toggleKey))
            {
                _visible = !_visible;
                if (_visible) RefreshVFXList();
            }

            // Cycle VFX
            if (_visible && Input.GetKeyDown(_cycleKey))
            {
                _selectedIndex = (_selectedIndex + 1) % Mathf.Max(1, _allVFX?.Length ?? 0);
                RefreshProperties();
            }

            // Number keys to select VFX
            if (_visible)
            {
                for (int i = 0; i < 9; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i) && _allVFX != null && i < _allVFX.Length)
                    {
                        _selectedIndex = i;
                        RefreshProperties();
                    }
                }
            }

            // Periodic refresh
            if (_visible && Time.time - _lastRefreshTime > RefreshInterval)
            {
                RefreshProperties();
                _lastRefreshTime = Time.time;
            }

            UpdateRuntimeStatus();
        }

        private void UpdateRuntimeStatus()
        {
            _totalVFXDisplay = _allVFX?.Length ?? 0;
            _selectedIndexDisplay = _selectedIndex;
            _selectedVFXNameDisplay = (_allVFX != null && _selectedIndex < _allVFX.Length && _allVFX[_selectedIndex] != null)
                ? _allVFX[_selectedIndex].name
                : "None";
            _propertyCountDisplay = _propertyCache.Count;
            _boundPropertiesDisplay = _propertyCache.Count(p => p.IsBound);
        }

        private void RefreshVFXList()
        {
            _allVFX = FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            if (_selectedIndex >= _allVFX.Length)
                _selectedIndex = 0;
            RefreshProperties();
        }

        private void RefreshProperties()
        {
            _propertyCache.Clear();
            if (_allVFX == null || _allVFX.Length == 0 || _selectedIndex >= _allVFX.Length)
                return;

            var vfx = _allVFX[_selectedIndex];
            if (vfx == null || vfx.visualEffectAsset == null) return;

            // Standard properties to check
            var standardProps = new (string name, string type)[]
            {
                ("ColorMap", "Texture2D"),
                ("DepthMap", "Texture2D"),
                ("StencilMap", "Texture2D"),
                ("PositionMap", "Texture2D"),
                ("VelocityMap", "Texture2D"),
                ("NormalMap", "Texture2D"),
                ("RayParams", "Vector4"),
                ("InverseView", "Matrix4x4"),
                ("InverseProjection", "Matrix4x4"),
                ("DepthRange", "Vector2"),
                ("ParticleCount", "UInt"),
                ("ParticleSize", "Float"),
                ("Throttle", "Float"),
                ("Intensity", "Float"),
                ("ColorSaturation", "Float"),
                ("ColorBrightness", "Float"),
                ("AudioVolume", "Float"),
                ("AudioBass", "Float"),
                ("AudioMid", "Float"),
                ("AudioTreble", "Float"),
                ("HandPosition", "Vector3"),
                ("HandVelocity", "Vector3"),
                ("Velocity", "Vector3"),
                ("Gravity", "Vector3"),
            };

            foreach (var (name, type) in standardProps)
            {
                bool hasProperty = false;
                string value = "N/A";
                bool hasError = false;

                try
                {
                    switch (type)
                    {
                        case "Texture2D":
                            hasProperty = vfx.HasTexture(name);
                            if (hasProperty)
                            {
                                var tex = vfx.GetTexture(name);
                                value = tex != null ? $"{tex.width}x{tex.height}" : "null";
                            }
                            break;
                        case "Vector4":
                            hasProperty = vfx.HasVector4(name);
                            if (hasProperty)
                            {
                                var v = vfx.GetVector4(name);
                                value = $"({v.x:F2}, {v.y:F2}, {v.z:F2}, {v.w:F2})";
                            }
                            break;
                        case "Vector3":
                            hasProperty = vfx.HasVector3(name);
                            if (hasProperty)
                            {
                                var v = vfx.GetVector3(name);
                                value = $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
                            }
                            break;
                        case "Vector2":
                            hasProperty = vfx.HasVector2(name);
                            if (hasProperty)
                            {
                                var v = vfx.GetVector2(name);
                                value = $"({v.x:F2}, {v.y:F2})";
                            }
                            break;
                        case "Matrix4x4":
                            hasProperty = vfx.HasMatrix4x4(name);
                            if (hasProperty) value = "(matrix)";
                            break;
                        case "Float":
                            hasProperty = vfx.HasFloat(name);
                            if (hasProperty) value = vfx.GetFloat(name).ToString("F3");
                            break;
                        case "UInt":
                            hasProperty = vfx.HasUInt(name);
                            if (hasProperty) value = vfx.GetUInt(name).ToString("N0");
                            break;
                    }
                }
                catch (Exception e)
                {
                    hasError = true;
                    value = e.Message.Length > 20 ? e.Message.Substring(0, 20) + "..." : e.Message;
                }

                if (hasProperty)
                {
                    _propertyCache.Add(new PropertyInfo
                    {
                        Name = name,
                        Type = type,
                        Value = value,
                        IsBound = !string.IsNullOrEmpty(value) && value != "null" && value != "N/A",
                        HasError = hasError
                    });
                }
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;

            InitStyles();

            // Draw window
            float x = Screen.width - _width - 20;
            float y = 20;
            Rect windowRect = new Rect(x, y, _width, _height);

            GUI.Box(windowRect, GUIContent.none, _boxStyle);

            GUILayout.BeginArea(new Rect(x + 10, y + 10, _width - 20, _height - 20));

            // Header
            GUILayout.Label("VFX Property Inspector", _headerStyle);
            GUILayout.Label($"[F1] Toggle | [F2] Cycle | [1-9] Select", _labelStyle);
            GUILayout.Space(5);

            // VFX selector
            if (_allVFX != null && _allVFX.Length > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("VFX:", _labelStyle, GUILayout.Width(40));
                for (int i = 0; i < Mathf.Min(_allVFX.Length, 9); i++)
                {
                    var style = i == _selectedIndex ? _headerStyle : _labelStyle;
                    if (GUILayout.Button($"{i + 1}", style, GUILayout.Width(25)))
                    {
                        _selectedIndex = i;
                        RefreshProperties();
                    }
                }
                GUILayout.EndHorizontal();

                if (_selectedIndex < _allVFX.Length && _allVFX[_selectedIndex] != null)
                {
                    var vfx = _allVFX[_selectedIndex];
                    GUILayout.Label($"Selected: {vfx.name}", _valueStyle);
                    GUILayout.Label($"Asset: {vfx.visualEffectAsset?.name ?? "None"}", _labelStyle);
                    GUILayout.Label($"Alive: {vfx.aliveParticleCount:N0}", _labelStyle);
                }
            }
            else
            {
                GUILayout.Label("No VFX found in scene", _labelStyle);
            }

            GUILayout.Space(10);

            // Property list
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            foreach (var prop in _propertyCache)
            {
                GUILayout.BeginHorizontal();

                // Status indicator
                Color statusColor = prop.HasError ? _errorColor : (prop.IsBound ? _boundColor : _unboundColor);
                var oldColor = GUI.color;
                GUI.color = statusColor;
                GUILayout.Label(prop.IsBound ? "\u25CF" : "\u25CB", GUILayout.Width(15));
                GUI.color = oldColor;

                // Property name and type
                GUILayout.Label($"{prop.Name}", _labelStyle, GUILayout.Width(130));
                GUILayout.Label($"[{prop.Type}]", _labelStyle, GUILayout.Width(80));

                // Value
                GUILayout.Label(prop.Value, _valueStyle);

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void InitStyles()
        {
            if (_boxStyle != null) return;

            _backgroundTex = new Texture2D(1, 1);
            _backgroundTex.SetPixel(0, 0, _backgroundColor);
            _backgroundTex.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = _backgroundTex;

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = _fontSize + 2;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = _headerColor;

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = _fontSize;
            _labelStyle.normal.textColor = Color.white;

            _valueStyle = new GUIStyle(GUI.skin.label);
            _valueStyle.fontSize = _fontSize;
            _valueStyle.fontStyle = FontStyle.Bold;
            _valueStyle.normal.textColor = Color.cyan;
        }
    }
}
