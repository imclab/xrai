// DebugStatsHUD.cs - Toggleable debug stats display (lower-left)
// Shows FPS, memory, scene info, tracking status

using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace MetavidoVFX.UI.Navigation
{
    /// <summary>
    /// Toggleable debug HUD showing key stats.
    /// Press Tab or tap bottom-left corner to toggle.
    /// </summary>
    public class DebugStatsHUD : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Display Settings")]
        [SerializeField] private bool _showOnStart = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.Tab;
        [SerializeField] private float _updateInterval = 0.5f;

        [Header("Style")]
        [SerializeField] private int _fontSize = 14;
        [SerializeField] private Color _backgroundColor = new Color(0, 0, 0, 0.7f);
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;

        #endregion

        #region Private Fields

        private bool _isVisible;
        private float _updateTimer;
        private StringBuilder _displayText = new StringBuilder(512);

        // Cached stats
        private float _fps;
        private float _frameTime;
        private float _memoryUsedMB;
        private float _memoryTotalMB;
        private int _vfxCount;
        private string _sceneName;
        private string _trackingStatus;

        // GUI styling
        private GUIStyle _boxStyle;
        private GUIStyle _textStyle;
        private Rect _hudRect;
        private bool _stylesInitialized;

        // Touch toggle
        private Rect _touchToggleZone;

        #endregion

        #region MonoBehaviour

        private void Start()
        {
            _isVisible = _showOnStart;
            _sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // Touch zone in bottom-left corner (100x100 pixels)
            _touchToggleZone = new Rect(0, Screen.height - 100, 100, 100);
        }

        private void Update()
        {
            // Toggle with keyboard
            if (Input.GetKeyDown(_toggleKey))
            {
                Toggle();
            }

            // Toggle with touch (tap bottom-left corner)
            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    Vector2 touchPos = new Vector2(touch.position.x, Screen.height - touch.position.y);
                    if (_touchToggleZone.Contains(touchPos))
                    {
                        Toggle();
                    }
                }
            }

            // Update stats periodically
            if (_isVisible)
            {
                _updateTimer -= Time.unscaledDeltaTime;
                if (_updateTimer <= 0)
                {
                    UpdateStats();
                    _updateTimer = _updateInterval;
                }
            }
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            InitStyles();

            // Draw background box
            GUI.Box(_hudRect, GUIContent.none, _boxStyle);

            // Draw text
            GUI.Label(new Rect(_hudRect.x + 8, _hudRect.y + 4, _hudRect.width - 16, _hudRect.height - 8),
                _displayText.ToString(), _textStyle);
        }

        #endregion

        #region Stats

        private void UpdateStats()
        {
            // FPS
            _fps = 1f / Time.unscaledDeltaTime;
            _frameTime = Time.unscaledDeltaTime * 1000f;

            // Memory
            _memoryUsedMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            _memoryTotalMB = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);

            // VFX count
            _vfxCount = FindObjectsByType<UnityEngine.VFX.VisualEffect>(FindObjectsSortMode.None).Length;

            // Scene
            _sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // Tracking status
            _trackingStatus = GetTrackingStatus();

            // Build display text
            BuildDisplayText();
        }

        private string GetTrackingStatus()
        {
#if UNITY_IOS || UNITY_ANDROID
            // Check AR session
            var arSession = FindAnyObjectByType<UnityEngine.XR.ARFoundation.ARSession>();
            if (arSession != null && arSession.enabled)
            {
                return "AR Active";
            }
#endif
            return "Editor";
        }

        private void BuildDisplayText()
        {
            _displayText.Clear();

            // FPS with color coding
            string fpsColor = _fps >= 55 ? "white" : (_fps >= 30 ? "yellow" : "red");
            _displayText.AppendLine($"<color={fpsColor}>FPS: {_fps:F0} ({_frameTime:F1}ms)</color>");

            // Memory
            string memColor = _memoryUsedMB < 200 ? "white" : (_memoryUsedMB < 400 ? "yellow" : "red");
            _displayText.AppendLine($"<color={memColor}>Mem: {_memoryUsedMB:F0}MB / {_memoryTotalMB:F0}MB</color>");

            // VFX count
            _displayText.AppendLine($"VFX: {_vfxCount} active");

            // Scene
            _displayText.AppendLine($"Scene: {_sceneName}");

            // Tracking
            _displayText.AppendLine($"Tracking: {_trackingStatus}");

            // Platform
            _displayText.AppendLine($"Platform: {Application.platform}");

            // Hint
            _displayText.Append("<color=#888888>[Tab] to hide</color>");
        }

        #endregion

        #region Styling

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            // Box style
            _boxStyle = new GUIStyle(GUI.skin.box);
            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, _backgroundColor);
            bgTex.Apply();
            _boxStyle.normal.background = bgTex;

            // Text style
            _textStyle = new GUIStyle(GUI.skin.label);
            _textStyle.fontSize = _fontSize;
            _textStyle.normal.textColor = _textColor;
            _textStyle.richText = true;
            _textStyle.wordWrap = false;

            // HUD rect (lower-left, auto-sized)
            float width = 200;
            float height = 140;
            float margin = 10;
            _hudRect = new Rect(margin, Screen.height - height - margin, width, height);

            _stylesInitialized = true;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Toggle HUD visibility.
        /// </summary>
        public void Toggle()
        {
            _isVisible = !_isVisible;
            if (_isVisible)
            {
                UpdateStats();
            }
        }

        /// <summary>
        /// Show the HUD.
        /// </summary>
        public void Show()
        {
            _isVisible = true;
            UpdateStats();
        }

        /// <summary>
        /// Hide the HUD.
        /// </summary>
        public void Hide()
        {
            _isVisible = false;
        }

        /// <summary>
        /// Check if HUD is visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        #endregion
    }
}
