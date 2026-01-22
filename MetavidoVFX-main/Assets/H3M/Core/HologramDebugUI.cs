// HologramDebugUI - UI Toolkit panel for hologram debug info
// Can embed into VFXToggleUI or any UI Toolkit panel
//
// Usage:
// 1. Add to GameObject with UIDocument
// 2. Or reference external UIDocument and specify container name

using UnityEngine;
using UnityEngine.UIElements;

namespace XRRAI.Hologram
{
    /// <summary>
    /// UI Toolkit debug panel for HologramRenderer status.
    /// Can embed into existing UI panels (like VFXToggleUI/HUD-UI-K).
    /// </summary>
    public class HologramDebugUI : MonoBehaviour
    {
        public enum UIMode
        {
            Auto,           // Auto-detect: embed if external doc available, else standalone
            Standalone,     // Create own UIDocument
            Embedded        // Inject into external UIDocument's container
        }

        [Header("UI Mode")]
        [SerializeField] private UIMode uiMode = UIMode.Auto;
        [Tooltip("External UIDocument to embed into")]
        [SerializeField] private UIDocument externalDocument;
        [Tooltip("Container element name to inject into")]
        [SerializeField] private string containerElementName = "hologram-debug-container";

        [Header("References")]
        [SerializeField] private HologramRenderer hologramRenderer;
        [SerializeField] private HologramSource hologramSource;
        [SerializeField] private bool autoFindReferences = true;

        [Header("Settings")]
        [SerializeField] private bool startVisible = true;
        [SerializeField] private float updateInterval = 0.1f;

        [Header("Position (Standalone mode)")]
        [SerializeField] private bool positionRight = false;
        [SerializeField] private bool positionTop = true;
        [SerializeField] private float panelWidth = 280f;
        [SerializeField] private float panelMargin = 8f;

        // UI Elements
        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _panel;
        private Label _titleLabel;
        private Label _vfxStatusLabel;
        private Label _particlesLabel;
        private Label _posMapLabel;
        private Label _colorMapLabel;
        private Label _spawnLabel;
        private Label _sourceStatusLabel;
        private Label _anchorLabel;
        private Label _frameLabel;
        private Button _toggleButton;

        private bool _isInitialized;
        private float _updateTimer;
        private bool _isVisible;

        void Start()
        {
            if (autoFindReferences)
            {
                if (hologramRenderer == null)
                    hologramRenderer = FindFirstObjectByType<HologramRenderer>();
                if (hologramSource == null)
                    hologramSource = FindFirstObjectByType<HologramSource>();
            }

            InitializeUI();
            _isVisible = startVisible;
            if (_panel != null)
                _panel.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void Update()
        {
            if (!_isInitialized || !_isVisible) return;

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= updateInterval)
            {
                _updateTimer = 0f;
                UpdateDebugInfo();
            }
        }

        void InitializeUI()
        {
            // Determine mode
            UIMode actualMode = uiMode;
            if (actualMode == UIMode.Auto)
            {
                actualMode = (externalDocument != null) ? UIMode.Embedded : UIMode.Standalone;
            }

            if (actualMode == UIMode.Embedded && externalDocument != null)
            {
                _root = externalDocument.rootVisualElement;
                var container = _root.Q<VisualElement>(containerElementName);
                if (container != null)
                {
                    _panel = CreatePanel();
                    container.Add(_panel);
                    _isInitialized = true;
                    Debug.Log($"[HologramDebugUI] Embedded into '{containerElementName}'");
                }
                else
                {
                    Debug.LogWarning($"[HologramDebugUI] Container '{containerElementName}' not found, creating standalone");
                    CreateStandalone();
                }
            }
            else
            {
                CreateStandalone();
            }
        }

        void CreateStandalone()
        {
            _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                _document = gameObject.AddComponent<UIDocument>();
                _document.panelSettings = Resources.Load<PanelSettings>("DefaultPanelSettings");

                // If no panel settings found, create minimal one
                if (_document.panelSettings == null)
                {
                    Debug.LogWarning("[HologramDebugUI] No PanelSettings found, UI may not display correctly");
                }
            }

            _root = new VisualElement();
            _root.style.position = Position.Absolute;
            _root.style.width = Length.Auto();
            _root.style.height = Length.Auto();
            _root.pickingMode = PickingMode.Ignore; // Don't block touches on empty space

            _panel = CreatePanel();

            // Position root container (panel will fill it)
            if (positionRight)
                _root.style.right = panelMargin;
            else
                _root.style.left = panelMargin;

            if (positionTop)
                _root.style.top = panelMargin;
            else
                _root.style.bottom = panelMargin;

            _root.Add(_panel);

            if (_document.rootVisualElement != null)
            {
                _document.rootVisualElement.Clear();
                _document.rootVisualElement.Add(_root);
            }

            _isInitialized = true;
            Debug.Log("[HologramDebugUI] Standalone panel created");
        }

        VisualElement CreatePanel()
        {
            var panel = new VisualElement();
            panel.name = "hologram-debug-panel";
            panel.style.width = panelWidth;
            panel.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.9f);
            panel.style.borderTopLeftRadius = 6;
            panel.style.borderTopRightRadius = 6;
            panel.style.borderBottomLeftRadius = 6;
            panel.style.borderBottomRightRadius = 6;
            panel.style.paddingTop = 8;
            panel.style.paddingBottom = 8;
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;

            // Title row
            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.justifyContent = Justify.SpaceBetween;
            titleRow.style.marginBottom = 6;

            _titleLabel = new Label("H3M Hologram Debug");
            _titleLabel.style.fontSize = 14;
            _titleLabel.style.color = new Color(0.4f, 0.8f, 1f);
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleRow.Add(_titleLabel);

            _toggleButton = new Button(() => ToggleVisibility());
            _toggleButton.text = "-";
            _toggleButton.style.width = 24;
            _toggleButton.style.height = 24;
            titleRow.Add(_toggleButton);

            panel.Add(titleRow);

            // Divider
            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
            divider.style.marginBottom = 6;
            panel.Add(divider);

            // VFX Section
            _vfxStatusLabel = CreateLabel("VFX: --");
            _particlesLabel = CreateLabel("Particles: --");
            _posMapLabel = CreateLabel("PositionMap: --");
            _colorMapLabel = CreateLabel("ColorMap: --");
            _spawnLabel = CreateLabel("Spawn: --");

            panel.Add(_vfxStatusLabel);
            panel.Add(_particlesLabel);
            panel.Add(_posMapLabel);
            panel.Add(_colorMapLabel);
            panel.Add(_spawnLabel);

            // Source Section
            var sourceHeader = CreateLabel("Source");
            sourceHeader.style.color = new Color(0.4f, 0.8f, 1f);
            sourceHeader.style.marginTop = 6;
            panel.Add(sourceHeader);

            _sourceStatusLabel = CreateLabel("Status: --");
            _anchorLabel = CreateLabel("Anchor: --");

            panel.Add(_sourceStatusLabel);
            panel.Add(_anchorLabel);

            // Frame counter
            _frameLabel = CreateLabel("Frame: 0");
            _frameLabel.style.marginTop = 6;
            _frameLabel.style.color = new Color(0.5f, 0.5f, 0.55f);
            panel.Add(_frameLabel);

            return panel;
        }

        Label CreateLabel(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 12;
            label.style.color = new Color(0.85f, 0.85f, 0.85f);
            label.style.marginBottom = 2;
            return label;
        }

        void UpdateDebugInfo()
        {
            if (hologramRenderer != null)
            {
                var info = hologramRenderer.GetDebugInfo();

                _vfxStatusLabel.text = info.HasVFX ? "VFX: OK" : "VFX: NULL";
                _vfxStatusLabel.style.color = info.HasVFX ? Color.green : Color.red;

                _particlesLabel.text = $"Particles: {info.AliveParticles}";

                _posMapLabel.text = $"PositionMap: {(info.HasPositionMap ? "YES" : "NO")} ({info.PositionMapSize})";
                _posMapLabel.style.color = info.HasPositionMap ? Color.white : Color.yellow;

                _colorMapLabel.text = $"ColorMap: {(info.HasColorMap ? "YES" : "NO")}";
                _spawnLabel.text = $"Spawn: {(info.HasSpawnProperty ? "YES" : "NO")}";

                _sourceStatusLabel.text = $"PosMap: {info.PositionMapSize} | Stencil: {info.StencilSize}";

                if (info.HasAnchor)
                    _anchorLabel.text = $"Anchor: {info.AnchorPosition:F2} | Scale: {info.HologramScale:F2}";
                else
                    _anchorLabel.text = "Anchor: Not Placed";

                _frameLabel.text = $"Frame: {info.FrameCount}";
            }
            else if (hologramSource != null)
            {
                // Fallback: show source info only
                _vfxStatusLabel.text = "VFX: No Renderer";
                _vfxStatusLabel.style.color = Color.yellow;

                var posMap = hologramSource.PositionMap;
                var stencil = hologramSource.StencilTexture;

                _posMapLabel.text = posMap != null
                    ? $"PositionMap: {posMap.width}x{posMap.height}"
                    : "PositionMap: NULL";

                _sourceStatusLabel.text = stencil != null
                    ? $"Stencil: {stencil.width}x{stencil.height}"
                    : "Stencil: NULL";
            }
            else
            {
                _vfxStatusLabel.text = "No Hologram Components Found";
                _vfxStatusLabel.style.color = Color.red;
            }
        }

        public void ToggleVisibility()
        {
            _isVisible = !_isVisible;
            if (_panel != null)
            {
                _panel.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_panel != null)
            {
                _panel.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
