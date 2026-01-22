// DebugStatsHUD.cs - UI Toolkit debug stats display (lower-left)
// Shows FPS, memory, scene info, tracking status
// Matches VFXToggleUI style for consistency

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace XRRAI.UI.Navigation
{
    /// <summary>
    /// UI Toolkit debug HUD showing key stats.
    /// Press Tab or tap bottom-left corner to toggle.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DebugStatsHUD : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Display Settings")]
        [SerializeField] private bool _showOnStart = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.Tab;
        [SerializeField] private float _updateInterval = 0.5f;

        #endregion

        #region Private Fields

        private UIDocument _document;
        private VisualElement _panel;
        private Label _fpsLabel;
        private Label _memLabel;
        private Label _vfxLabel;
        private Label _sceneLabel;
        private Label _trackingLabel;
        private Label _platformLabel;

        private bool _isVisible;
        private float _updateTimer;

        // Cached stats
        private float _fps;
        private float[] _fpsBuffer = new float[30];
        private int _fpsIndex;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void Start()
        {
            CreateUI();
            _isVisible = _showOnStart;
            SetVisible(_isVisible);
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
                if (touch.phase == TouchPhase.Began && touch.position.x < 100 && touch.position.y < 100)
                {
                    Toggle();
                }
            }

            // Update FPS buffer
            _fpsBuffer[_fpsIndex] = 1f / Time.unscaledDeltaTime;
            _fpsIndex = (_fpsIndex + 1) % _fpsBuffer.Length;
            _fps = 0;
            foreach (var f in _fpsBuffer) _fps += f;
            _fps /= _fpsBuffer.Length;

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

        #endregion

        #region UI Creation

        private void CreateUI()
        {
            if (_document == null || _document.rootVisualElement == null) return;

            var root = _document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;

            // Main panel - lower-left
            _panel = new VisualElement { name = "debug-stats-panel" };
            _panel.style.position = Position.Absolute;
            _panel.style.left = 10;
            _panel.style.bottom = 10;
            _panel.style.width = 180;
            _panel.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            _panel.style.borderTopLeftRadius = _panel.style.borderTopRightRadius =
                _panel.style.borderBottomLeftRadius = _panel.style.borderBottomRightRadius = 6;
            _panel.style.paddingTop = _panel.style.paddingBottom = 8;
            _panel.style.paddingLeft = _panel.style.paddingRight = 10;

            // Title
            var title = CreateLabel("Debug Stats", 12, new Color(0.4f, 0.63f, 1f));
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6;
            _panel.Add(title);

            // Stats labels
            _fpsLabel = CreateLabel("FPS: --", 11, Color.white);
            _panel.Add(_fpsLabel);

            _memLabel = CreateLabel("Mem: --", 11, Color.white);
            _panel.Add(_memLabel);

            _vfxLabel = CreateLabel("VFX: --", 11, Color.white);
            _panel.Add(_vfxLabel);

            _sceneLabel = CreateLabel("Scene: --", 11, Color.white);
            _panel.Add(_sceneLabel);

            _trackingLabel = CreateLabel("Tracking: --", 11, Color.white);
            _panel.Add(_trackingLabel);

            _platformLabel = CreateLabel("Platform: --", 11, new Color(1, 1, 1, 0.6f));
            _panel.Add(_platformLabel);

            // Hint
            var hint = CreateLabel("[Tab] to hide", 10, new Color(1, 1, 1, 0.4f));
            hint.style.marginTop = 6;
            _panel.Add(hint);

            root.Add(_panel);
        }

        private Label CreateLabel(string text, int fontSize, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = color;
            label.style.marginTop = label.style.marginBottom = 1;
            return label;
        }

        #endregion

        #region Stats

        private void UpdateStats()
        {
            if (_fpsLabel == null) return;

            float frameTime = Time.unscaledDeltaTime * 1000f;
            float memUsed = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            float memTotal = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
            int vfxCount = FindObjectsByType<UnityEngine.VFX.VisualEffect>(FindObjectsSortMode.None).Length;
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // FPS with color coding
            _fpsLabel.text = $"FPS: {_fps:F0} ({frameTime:F1}ms)";
            _fpsLabel.style.color = _fps >= 55 ? Color.white : (_fps >= 30 ? Color.yellow : Color.red);

            // Memory
            _memLabel.text = $"Mem: {memUsed:F0}MB / {memTotal:F0}MB";
            _memLabel.style.color = memUsed < 200 ? Color.white : (memUsed < 400 ? Color.yellow : Color.red);

            // VFX
            _vfxLabel.text = $"VFX: {vfxCount} active";

            // Scene
            _sceneLabel.text = $"Scene: {sceneName}";

            // Tracking
            _trackingLabel.text = $"Tracking: {GetTrackingStatus()}";

            // Platform
            _platformLabel.text = $"Platform: {Application.platform}";
        }

        private string GetTrackingStatus()
        {
#if UNITY_IOS || UNITY_ANDROID
            var arSession = FindAnyObjectByType<UnityEngine.XR.ARFoundation.ARSession>();
            if (arSession != null && arSession.enabled)
                return "AR Active";
#endif
            return "Editor";
        }

        #endregion

        #region Public API

        public void Toggle()
        {
            _isVisible = !_isVisible;
            SetVisible(_isVisible);
            if (_isVisible) UpdateStats();
        }

        public void Show() { _isVisible = true; SetVisible(true); UpdateStats(); }
        public void Hide() { _isVisible = false; SetVisible(false); }

        public bool IsVisible => _isVisible;

        private void SetVisible(bool visible)
        {
            if (_panel != null)
                _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        #endregion
    }
}
