// SpecSceneUI.cs - UI Toolkit overlay for spec demo scenes
// Shows scene title, back button, and debug stats

using UnityEngine;
using UnityEngine.UIElements;

namespace XRRAI.UI.Navigation
{
    /// <summary>
    /// UI Toolkit overlay for spec demo scenes.
    /// Displays title (top-center), back button (top-left), debug stats (bottom-left).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SpecSceneUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Auto-Setup")]
        [SerializeField] private bool _autoSetTitle = true;
        [SerializeField] private bool _showDebugStats = true;

        #endregion

        #region Private Fields

        private UIDocument _document;
        private Label _titleLabel;
        private VisualElement _debugPanel;

        // Debug stats
        private Label _fpsLabel;
        private Label _sceneLabel;
        private float _fps;
        private float[] _fpsBuffer = new float[30];
        private int _fpsIndex;
        private float _updateTimer;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void Start()
        {
            CreateUI();
        }

        private void Update()
        {
            // Escape to go back
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnBackButtonClick();
            }

            // Tab to toggle debug stats
            if (Input.GetKeyDown(KeyCode.Tab) && _debugPanel != null)
            {
                bool visible = _debugPanel.style.display == DisplayStyle.Flex;
                _debugPanel.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // Update FPS
            _fpsBuffer[_fpsIndex] = 1f / Time.unscaledDeltaTime;
            _fpsIndex = (_fpsIndex + 1) % _fpsBuffer.Length;
            _fps = 0;
            foreach (var f in _fpsBuffer) _fps += f;
            _fps /= _fpsBuffer.Length;

            // Update stats
            _updateTimer -= Time.unscaledDeltaTime;
            if (_updateTimer <= 0 && _fpsLabel != null)
            {
                _fpsLabel.text = $"FPS: {_fps:F0}";
                _fpsLabel.style.color = _fps >= 55 ? Color.white : (_fps >= 30 ? Color.yellow : Color.red);
                _updateTimer = 0.5f;
            }
        }

        #endregion

        #region UI Creation

        private void CreateUI()
        {
            if (_document == null || _document.rootVisualElement == null) return;

            var root = _document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;

            // Top bar
            var topBar = new VisualElement { name = "top-bar" };
            topBar.style.position = Position.Absolute;
            topBar.style.top = 0;
            topBar.style.left = 0;
            topBar.style.right = 0;
            topBar.style.height = 50;
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.alignItems = Align.Center;
            topBar.style.paddingLeft = topBar.style.paddingRight = 10;
            topBar.style.backgroundColor = new Color(0, 0, 0, 0.6f);

            // Back button
            var backBtn = new Button(OnBackButtonClick);
            backBtn.text = "← Back";
            backBtn.style.width = 80;
            backBtn.style.height = 36;
            backBtn.style.fontSize = 13;
            backBtn.style.color = Color.white;
            backBtn.style.backgroundColor = new Color(0.25f, 0.25f, 0.3f);
            backBtn.style.borderTopWidth = backBtn.style.borderBottomWidth =
                backBtn.style.borderLeftWidth = backBtn.style.borderRightWidth = 0;
            backBtn.style.borderTopLeftRadius = backBtn.style.borderTopRightRadius =
                backBtn.style.borderBottomLeftRadius = backBtn.style.borderBottomRightRadius = 4;
            topBar.Add(backBtn);

            // Title (centered)
            _titleLabel = new Label();
            _titleLabel.style.flexGrow = 1;
            _titleLabel.style.fontSize = 18;
            _titleLabel.style.color = Color.white;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            if (_autoSetTitle)
            {
                _titleLabel.text = SceneNavigator.Instance?.GetCurrentSceneDisplayName() ??
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }
            topBar.Add(_titleLabel);

            // Spacer (to balance back button width)
            var spacer = new VisualElement();
            spacer.style.width = 80;
            topBar.Add(spacer);

            root.Add(topBar);

            // Debug stats panel (bottom-left)
            if (_showDebugStats)
            {
                _debugPanel = new VisualElement { name = "debug-panel" };
                _debugPanel.style.position = Position.Absolute;
                _debugPanel.style.left = 10;
                _debugPanel.style.bottom = 10;
                _debugPanel.style.width = 140;
                _debugPanel.style.backgroundColor = new Color(0, 0, 0, 0.75f);
                _debugPanel.style.borderTopLeftRadius = _debugPanel.style.borderTopRightRadius =
                    _debugPanel.style.borderBottomLeftRadius = _debugPanel.style.borderBottomRightRadius = 6;
                _debugPanel.style.paddingTop = _debugPanel.style.paddingBottom = 8;
                _debugPanel.style.paddingLeft = _debugPanel.style.paddingRight = 10;

                _fpsLabel = CreateLabel("FPS: --", 11, Color.white);
                _debugPanel.Add(_fpsLabel);

                _sceneLabel = CreateLabel(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    10, new Color(1, 1, 1, 0.6f));
                _debugPanel.Add(_sceneLabel);

                var hint = CreateLabel("[Tab] hide | [Esc] back", 9, new Color(1, 1, 1, 0.4f));
                hint.style.marginTop = 4;
                _debugPanel.Add(hint);

                root.Add(_debugPanel);
            }
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

        #region Handlers

        private void OnBackButtonClick()
        {
            Debug.Log("[SpecSceneUI] Returning to main menu");
            SceneNavigator.Instance.LoadMainMenu();
        }

        #endregion

        #region Public API

        public void SetTitle(string title)
        {
            if (_titleLabel != null)
                _titleLabel.text = title;
        }

        public void ToggleDebugPanel()
        {
            if (_debugPanel != null)
            {
                bool visible = _debugPanel.style.display == DisplayStyle.Flex;
                _debugPanel.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        #endregion
    }
}
