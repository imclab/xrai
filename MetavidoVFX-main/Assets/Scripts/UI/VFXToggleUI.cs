// VFXToggleUI - Flexible UI Toolkit HUD for toggling VFX on/off by category
// Can work standalone OR embed into any existing UI Toolkit panel (like HUD-UI-K)
//
// Usage modes:
// 1. Standalone: Attach to GameObject with UIDocument, uses VFXLibrary.uxml
// 2. Embedded: Reference external UIDocument, specify container element name
// 3. Auto: Creates panel programmatically, works with any UIDocument

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using MetavidoVFX.VFX;

namespace MetavidoVFX.UI
{
    /// <summary>
    /// Flexible UI Toolkit-based VFX toggle panel.
    /// Can embed into existing UIs (HUD-UI-K, etc.) or run standalone.
    /// </summary>
    public class VFXToggleUI : MonoBehaviour
    {
        public enum UIMode
        {
            Auto,           // Auto-detect: use UXML if available, else create programmatically
            Standalone,     // Use own UIDocument with VFXLibrary.uxml
            Embedded,       // Inject into external UIDocument's container
            Programmatic    // Create all UI elements in code (no UXML needed)
        }

        [Header("UI Mode")]
        [SerializeField] private UIMode uiMode = UIMode.Auto;
        [Tooltip("External UIDocument to embed into (for Embedded mode)")]
        [SerializeField] private UIDocument externalDocument;
        [Tooltip("Container element name to inject into (for Embedded mode)")]
        [SerializeField] private string containerElementName = "vfx-container";

        [Header("Library Reference")]
        [SerializeField] private VFXLibraryManager libraryManager;
        [SerializeField] private bool autoFindLibrary = true;

        [Header("UI Settings")]
#if ENABLE_INPUT_SYSTEM
        [SerializeField] private Key toggleUIKey = Key.Tab;
#else
        [SerializeField] private KeyCode toggleUIKey = KeyCode.Tab;
#endif
        [SerializeField] private bool expandAllCategories = false;
        [SerializeField] private bool startVisible = true;

        [Header("Panel Position (Programmatic mode)")]
        [SerializeField] private bool positionRight = true;
        [SerializeField] private bool positionTop = true;
        [SerializeField] private float panelWidth = 260f;
        [SerializeField] private float panelMargin = 8f;

        // UI Elements
        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _panel;
        private Label _countLabel;
        private Label _fpsLabel;
        private Label _activeLabel;
        private VisualElement _categoriesContainer;
        private Button _allOffBtn;
        private Button _expandBtn;
        private Button _collapseBtn;

        // State
        private Dictionary<VFXCategoryType, bool> _categoryExpanded = new();
        private Dictionary<VFXCategoryType, VisualElement> _categoryContainers = new();
        private bool _isInitialized;
        private bool _createdProgrammatically;

        // Stats
        private float _fps;
        private float[] _fpsBuffer = new float[30];
        private int _fpsIndex;

        // USS class names (can be customized to match host UI)
        public static class UssClasses
        {
            public const string Panel = "vfx-panel";
            public const string Header = "vfx-header";
            public const string Title = "vfx-title";
            public const string Count = "vfx-count";
            public const string StatsRow = "vfx-stats-row";
            public const string StatLabel = "vfx-stat-label";
            public const string ControlsRow = "vfx-controls-row";
            public const string ControlBtn = "vfx-control-btn";
            public const string CategoryScroll = "vfx-category-scroll";
            public const string CategoryHeader = "vfx-category-header";
            public const string CategoryContainer = "vfx-category-container";
            public const string VfxItem = "vfx-item";
            public const string VfxToggle = "vfx-toggle";
            public const string VfxName = "vfx-name";
            public const string VfxNameActive = "vfx-name-active";
            public const string Hidden = "hidden";
        }

        void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        void Start()
        {
            if (autoFindLibrary && libraryManager == null)
            {
                libraryManager = FindObjectOfType<VFXLibraryManager>();
                if (libraryManager == null)
                {
                    var allVfx = GameObject.Find("ALL_VFX");
                    if (allVfx != null)
                    {
                        libraryManager = allVfx.GetComponent<VFXLibraryManager>();
                    }
                }
            }

            // Initialize category expansion state
            foreach (VFXCategoryType cat in System.Enum.GetValues(typeof(VFXCategoryType)))
            {
                _categoryExpanded[cat] = expandAllCategories;
            }

            if (libraryManager != null)
            {
                libraryManager.OnLibraryPopulated += RefreshUI;
                libraryManager.OnVFXToggled += OnVFXToggled;
            }

            // Setup UI after a frame to ensure documents are ready
            StartCoroutine(SetupUIDelayed());
        }

        System.Collections.IEnumerator SetupUIDelayed()
        {
            yield return null; // Wait one frame
            SetupUI();
        }

        void OnDestroy()
        {
            if (libraryManager != null)
            {
                libraryManager.OnLibraryPopulated -= RefreshUI;
                libraryManager.OnVFXToggled -= OnVFXToggled;
            }

            // Clean up programmatically created panel from external document
            if (_createdProgrammatically && _panel != null && _panel.parent != null)
            {
                _panel.RemoveFromHierarchy();
            }
        }

        void Update()
        {
            // Update FPS
            _fpsBuffer[_fpsIndex] = 1f / Time.unscaledDeltaTime;
            _fpsIndex = (_fpsIndex + 1) % _fpsBuffer.Length;
            _fps = _fpsBuffer.Average();

            // Update stats labels
            if (_fpsLabel != null)
            {
                _fpsLabel.text = $"FPS: {_fps:F0}";
            }
            if (_activeLabel != null && libraryManager != null)
            {
                _activeLabel.text = $"Active: {libraryManager.ActiveCount}/{libraryManager.MaxActiveVFX}";
            }

            // Toggle UI visibility
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current[toggleUIKey].wasPressedThisFrame)
            {
                ToggleVisibility();
            }
#else
            if (Input.GetKeyDown(toggleUIKey))
            {
                ToggleVisibility();
            }
#endif
        }

        void SetupUI()
        {
            switch (uiMode)
            {
                case UIMode.Auto:
                    SetupAutoMode();
                    break;
                case UIMode.Standalone:
                    SetupStandaloneMode();
                    break;
                case UIMode.Embedded:
                    SetupEmbeddedMode();
                    break;
                case UIMode.Programmatic:
                    SetupProgrammaticMode();
                    break;
            }

            // Initial visibility
            if (_panel != null && !startVisible)
            {
                _panel.AddToClassList(UssClasses.Hidden);
            }
        }

        void SetupAutoMode()
        {
            // Try standalone first (if we have UIDocument with UXML)
            if (_document != null && _document.visualTreeAsset != null)
            {
                SetupStandaloneMode();
                if (_isInitialized) return;
            }

            // Try embedded (if external document specified)
            if (externalDocument != null)
            {
                SetupEmbeddedMode();
                if (_isInitialized) return;
            }

            // Fall back to programmatic
            SetupProgrammaticMode();
        }

        void SetupStandaloneMode()
        {
            if (_document == null || _document.rootVisualElement == null)
            {
                Debug.LogWarning("[VFXToggleUI] Standalone mode requires UIDocument with UXML");
                return;
            }

            _root = _document.rootVisualElement;
            _root.pickingMode = PickingMode.Ignore; // Allow touches to pass through empty space
            _panel = _root.Q<VisualElement>("vfx-panel");

            if (_panel == null)
            {
                Debug.LogWarning("[VFXToggleUI] vfx-panel not found in UXML, trying programmatic");
                SetupProgrammaticMode();
                return;
            }

            QueryAndWireElements();
            _isInitialized = true;
            Debug.Log("[VFXToggleUI] Initialized in Standalone mode");

            // Initial population
            if (libraryManager != null && libraryManager.AllVFX.Count > 0)
            {
                RefreshUI();
            }
        }

        void SetupEmbeddedMode()
        {
            var doc = externalDocument ?? _document;
            if (doc == null || doc.rootVisualElement == null)
            {
                Debug.LogWarning("[VFXToggleUI] Embedded mode requires UIDocument");
                return;
            }

            _root = doc.rootVisualElement;

            // Find container to inject into
            var container = _root.Q<VisualElement>(containerElementName);
            if (container == null)
            {
                Debug.Log($"[VFXToggleUI] Container '{containerElementName}' not found, creating panel at root");
                container = _root;
            }

            // Create panel programmatically and add to container
            _panel = CreatePanelElement();
            container.Add(_panel);
            _createdProgrammatically = true;

            QueryAndWireElements();
            _isInitialized = true;
            Debug.Log($"[VFXToggleUI] Initialized in Embedded mode (container: {containerElementName})");

            if (libraryManager != null && libraryManager.AllVFX.Count > 0)
            {
                RefreshUI();
            }
        }

        void SetupProgrammaticMode()
        {
            // Find any UIDocument to attach to
            var doc = externalDocument ?? _document ?? FindObjectOfType<UIDocument>();
            if (doc == null || doc.rootVisualElement == null)
            {
                Debug.LogError("[VFXToggleUI] No UIDocument found for programmatic mode");
                return;
            }

            _root = doc.rootVisualElement;
            _root.pickingMode = PickingMode.Ignore; // Allow touches to pass through empty space
            _panel = CreatePanelElement();
            _root.Add(_panel);
            _createdProgrammatically = true;

            QueryAndWireElements();
            _isInitialized = true;
            Debug.Log("[VFXToggleUI] Initialized in Programmatic mode");

            if (libraryManager != null && libraryManager.AllVFX.Count > 0)
            {
                RefreshUI();
            }
        }

        /// <summary>
        /// Creates the entire VFX panel UI programmatically (no UXML needed)
        /// </summary>
        VisualElement CreatePanelElement()
        {
            var panel = new VisualElement();
            panel.name = "vfx-panel";
            panel.AddToClassList(UssClasses.Panel);

            // Inline styles for programmatic mode (works without USS)
            panel.style.position = Position.Absolute;
            panel.style.width = panelWidth;
            panel.style.maxHeight = new Length(80, LengthUnit.Percent);
            panel.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius =
                panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 6;
            panel.style.paddingTop = panel.style.paddingBottom =
                panel.style.paddingLeft = panel.style.paddingRight = 6;

            if (positionRight)
                panel.style.right = panelMargin;
            else
                panel.style.left = panelMargin;

            if (positionTop)
                panel.style.top = panelMargin;
            else
                panel.style.bottom = panelMargin;

            // Header
            var header = new VisualElement();
            header.name = "header";
            header.AddToClassList(UssClasses.Header);
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.paddingLeft = header.style.paddingRight = 8;
            header.style.paddingTop = header.style.paddingBottom = 4;
            header.style.marginBottom = 4;
            header.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 0.9f);
            header.style.borderTopLeftRadius = header.style.borderTopRightRadius =
                header.style.borderBottomLeftRadius = header.style.borderBottomRightRadius = 4;

            var title = new Label("VFX Library");
            title.name = "title-label";
            title.AddToClassList(UssClasses.Title);
            title.style.fontSize = 14;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);

            var count = new Label("0/0");
            count.name = "count-label";
            count.AddToClassList(UssClasses.Count);
            count.style.fontSize = 12;
            count.style.color = new Color(1, 1, 1, 0.7f);
            header.Add(count);

            panel.Add(header);

            // Stats row
            var statsRow = new VisualElement();
            statsRow.name = "stats-row";
            statsRow.AddToClassList(UssClasses.StatsRow);
            statsRow.style.flexDirection = FlexDirection.Row;
            statsRow.style.justifyContent = Justify.SpaceBetween;
            statsRow.style.paddingLeft = statsRow.style.paddingRight = 4;
            statsRow.style.marginBottom = 4;

            var fpsLabel = new Label("FPS: 60");
            fpsLabel.name = "fps-label";
            fpsLabel.AddToClassList(UssClasses.StatLabel);
            fpsLabel.style.fontSize = 11;
            fpsLabel.style.color = new Color(1, 1, 1, 0.8f);
            statsRow.Add(fpsLabel);

            var activeLabel = new Label("Active: 0/3");
            activeLabel.name = "active-label";
            activeLabel.AddToClassList(UssClasses.StatLabel);
            activeLabel.style.fontSize = 11;
            activeLabel.style.color = new Color(1, 1, 1, 0.8f);
            statsRow.Add(activeLabel);

            panel.Add(statsRow);

            // Controls row
            var controlsRow = new VisualElement();
            controlsRow.name = "controls-row";
            controlsRow.AddToClassList(UssClasses.ControlsRow);
            controlsRow.style.flexDirection = FlexDirection.Row;
            controlsRow.style.justifyContent = Justify.SpaceBetween;
            controlsRow.style.marginBottom = 6;

            var allOffBtn = CreateControlButton("All Off", "all-off-btn");
            var expandBtn = CreateControlButton("Expand", "expand-btn");
            var collapseBtn = CreateControlButton("Collapse", "collapse-btn");

            controlsRow.Add(allOffBtn);
            controlsRow.Add(expandBtn);
            controlsRow.Add(collapseBtn);
            panel.Add(controlsRow);

            // Scrollable category container
            var scroll = new ScrollView();
            scroll.name = "category-scroll";
            scroll.AddToClassList(UssClasses.CategoryScroll);
            scroll.style.flexGrow = 1;
            scroll.style.maxHeight = 400;

            var categoriesContainer = new VisualElement();
            categoriesContainer.name = "categories-container";
            scroll.Add(categoriesContainer);

            panel.Add(scroll);

            return panel;
        }

        Button CreateControlButton(string text, string name)
        {
            var btn = new Button();
            btn.text = text;
            btn.name = name;
            btn.AddToClassList(UssClasses.ControlBtn);
            btn.style.fontSize = 10;
            btn.style.color = Color.white;
            btn.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);
            btn.style.borderTopWidth = btn.style.borderBottomWidth =
                btn.style.borderLeftWidth = btn.style.borderRightWidth = 0;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius =
                btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 3;
            btn.style.paddingTop = btn.style.paddingBottom = 4;
            btn.style.paddingLeft = btn.style.paddingRight = 8;
            btn.style.flexGrow = 1;
            btn.style.marginLeft = btn.style.marginRight = 2;
            return btn;
        }

        void QueryAndWireElements()
        {
            if (_panel == null) return;

            // Query elements (works for both UXML and programmatic)
            _countLabel = _panel.Q<Label>("count-label");
            _fpsLabel = _panel.Q<Label>("fps-label");
            _activeLabel = _panel.Q<Label>("active-label");
            _categoriesContainer = _panel.Q<VisualElement>("categories-container");
            _allOffBtn = _panel.Q<Button>("all-off-btn");
            _expandBtn = _panel.Q<Button>("expand-btn");
            _collapseBtn = _panel.Q<Button>("collapse-btn");

            // Wire up buttons
            if (_allOffBtn != null)
            {
                _allOffBtn.clicked += () => libraryManager?.DisableAll();
            }
            if (_expandBtn != null)
            {
                _expandBtn.clicked += ExpandAll;
            }
            if (_collapseBtn != null)
            {
                _collapseBtn.clicked += CollapseAll;
            }
        }

        void RefreshUI()
        {
            if (!_isInitialized || _categoriesContainer == null || libraryManager == null) return;

            // Clear existing content
            _categoriesContainer.Clear();
            _categoryContainers.Clear();

            // Update count label
            if (_countLabel != null)
            {
                _countLabel.text = $"{libraryManager.ActiveCount}/{libraryManager.AllVFX.Count}";
            }

            // Create categories
            foreach (VFXCategoryType category in System.Enum.GetValues(typeof(VFXCategoryType)))
            {
                var entries = libraryManager.GetCategory(category);
                if (entries.Count == 0) continue;

                CreateCategorySection(category, entries);
            }
        }

        void CreateCategorySection(VFXCategoryType category, IReadOnlyList<VFXLibraryManager.VFXEntry> entries)
        {
            int activeInCategory = entries.Count(e => e.IsActive);
            bool isExpanded = _categoryExpanded.TryGetValue(category, out bool exp) && exp;

            // Category header button
            var headerBtn = new Button();
            headerBtn.AddToClassList(UssClasses.CategoryHeader);
            headerBtn.style.fontSize = 12;
            headerBtn.style.color = new Color(0.4f, 0.63f, 1f);
            headerBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerBtn.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.8f);
            headerBtn.style.paddingTop = headerBtn.style.paddingBottom = 6;
            headerBtn.style.paddingLeft = headerBtn.style.paddingRight = 8;
            headerBtn.style.marginTop = 4;
            headerBtn.style.marginBottom = 2;
            headerBtn.style.borderTopLeftRadius = headerBtn.style.borderTopRightRadius =
                headerBtn.style.borderBottomLeftRadius = headerBtn.style.borderBottomRightRadius = 3;
            headerBtn.style.borderTopWidth = headerBtn.style.borderBottomWidth =
                headerBtn.style.borderLeftWidth = headerBtn.style.borderRightWidth = 0;
            headerBtn.style.unityTextAlign = TextAnchor.MiddleLeft;

            string expandIcon = isExpanded ? "[-]" : "[+]";
            headerBtn.text = $"{expandIcon} {category} ({activeInCategory}/{entries.Count})";
            headerBtn.clicked += () => ToggleCategory(category);
            _categoriesContainer.Add(headerBtn);

            // Category items container
            var container = new VisualElement();
            container.AddToClassList(UssClasses.CategoryContainer);
            container.style.marginLeft = 8;
            container.style.marginBottom = 4;
            if (!isExpanded)
            {
                container.style.display = DisplayStyle.None;
            }
            _categoryContainers[category] = container;

            // Add VFX items
            foreach (var entry in entries)
            {
                var item = CreateVFXItem(entry);
                container.Add(item);
            }

            _categoriesContainer.Add(container);
        }

        VisualElement CreateVFXItem(VFXLibraryManager.VFXEntry entry)
        {
            var item = new VisualElement();
            item.AddToClassList(UssClasses.VfxItem);
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;
            item.style.paddingTop = item.style.paddingBottom = 3;
            item.style.paddingLeft = item.style.paddingRight = 4;
            item.style.marginTop = item.style.marginBottom = 1;
            item.style.borderTopLeftRadius = item.style.borderTopRightRadius =
                item.style.borderBottomLeftRadius = item.style.borderBottomRightRadius = 3;

            // Toggle
            var toggle = new Toggle();
            toggle.AddToClassList(UssClasses.VfxToggle);
            toggle.style.width = 40;
            toggle.style.marginRight = 6;
            toggle.value = entry.IsActive;
            toggle.RegisterValueChangedCallback(evt =>
            {
                libraryManager?.SetVFXActive(entry, evt.newValue);
            });
            item.Add(toggle);

            // Name label
            var nameLabel = new Label();
            nameLabel.AddToClassList(UssClasses.VfxName);
            nameLabel.style.fontSize = 11;
            nameLabel.style.color = new Color(1, 1, 1, 0.85f);
            nameLabel.style.flexGrow = 1;
            nameLabel.style.overflow = Overflow.Hidden;
            nameLabel.style.textOverflow = TextOverflow.Ellipsis;

            string displayName = entry.AssetName;
            if (displayName.Length > 28)
            {
                displayName = displayName.Substring(0, 25) + "...";
            }
            nameLabel.text = displayName;

            if (entry.IsActive)
            {
                nameLabel.AddToClassList(UssClasses.VfxNameActive);
                nameLabel.style.color = new Color(0.47f, 0.86f, 0.55f);
            }
            item.Add(nameLabel);

            // Store reference for updates
            item.userData = new VFXItemData { Entry = entry, Toggle = toggle, NameLabel = nameLabel };

            return item;
        }

        void ToggleCategory(VFXCategoryType category)
        {
            if (!_categoryExpanded.ContainsKey(category)) return;

            _categoryExpanded[category] = !_categoryExpanded[category];
            bool isExpanded = _categoryExpanded[category];

            // Update container visibility
            if (_categoryContainers.TryGetValue(category, out var container))
            {
                container.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Update header text
            UpdateCategoryHeader(category);
        }

        void OnVFXToggled(VFXLibraryManager.VFXEntry entry, bool active)
        {
            // Update count label
            if (_countLabel != null && libraryManager != null)
            {
                _countLabel.text = $"{libraryManager.ActiveCount}/{libraryManager.AllVFX.Count}";
            }

            // Update category header counts
            UpdateCategoryHeader(entry.CategoryType);

            // Update item styling
            UpdateVFXItemVisual(entry, active);
        }

        void UpdateCategoryHeader(VFXCategoryType category)
        {
            var entries = libraryManager?.GetCategory(category);
            if (entries == null) return;

            int activeInCategory = entries.Count(e => e.IsActive);
            bool isExpanded = _categoryExpanded.TryGetValue(category, out bool exp) && exp;
            string expandIcon = isExpanded ? "[-]" : "[+]";

            foreach (var child in _categoriesContainer.Children())
            {
                if (child is Button btn && btn.text.Contains(category.ToString()))
                {
                    btn.text = $"{expandIcon} {category} ({activeInCategory}/{entries.Count})";
                    break;
                }
            }
        }

        void UpdateVFXItemVisual(VFXLibraryManager.VFXEntry entry, bool active)
        {
            if (!_categoryContainers.TryGetValue(entry.CategoryType, out var container)) return;

            foreach (var child in container.Children())
            {
                if (child.userData is VFXItemData data && data.Entry == entry)
                {
                    data.Toggle.SetValueWithoutNotify(active);

                    if (active)
                    {
                        data.NameLabel.AddToClassList(UssClasses.VfxNameActive);
                        data.NameLabel.style.color = new Color(0.47f, 0.86f, 0.55f);
                    }
                    else
                    {
                        data.NameLabel.RemoveFromClassList(UssClasses.VfxNameActive);
                        data.NameLabel.style.color = new Color(1, 1, 1, 0.85f);
                    }
                    break;
                }
            }
        }

        void ExpandAll()
        {
            foreach (var cat in _categoryExpanded.Keys.ToList())
            {
                _categoryExpanded[cat] = true;
            }
            RefreshUI();
        }

        void CollapseAll()
        {
            foreach (var cat in _categoryExpanded.Keys.ToList())
            {
                _categoryExpanded[cat] = false;
            }
            RefreshUI();
        }

        // ═══════════════════════════════════════════════════════════════
        // Public API
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Toggle panel visibility</summary>
        public void ToggleVisibility()
        {
            if (_panel == null) return;

            if (_panel.style.display == DisplayStyle.None)
            {
                _panel.style.display = DisplayStyle.Flex;
                _panel.RemoveFromClassList(UssClasses.Hidden);
            }
            else
            {
                _panel.style.display = DisplayStyle.None;
                _panel.AddToClassList(UssClasses.Hidden);
            }
        }

        /// <summary>Set panel visibility</summary>
        public void SetVisible(bool visible)
        {
            if (_panel == null) return;
            _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
                _panel.RemoveFromClassList(UssClasses.Hidden);
            else
                _panel.AddToClassList(UssClasses.Hidden);
        }

        /// <summary>Is panel currently visible</summary>
        public bool IsVisible => _panel != null && _panel.style.display != DisplayStyle.None;

        /// <summary>Get the root panel element (for external styling)</summary>
        public VisualElement PanelElement => _panel;

        /// <summary>Inject into an existing UIDocument at runtime</summary>
        public void InjectInto(UIDocument document, string containerName = null)
        {
            if (document == null || document.rootVisualElement == null) return;

            // Remove from old location if needed
            if (_panel != null && _panel.parent != null)
            {
                _panel.RemoveFromHierarchy();
            }

            _root = document.rootVisualElement;
            var container = string.IsNullOrEmpty(containerName)
                ? _root
                : _root.Q<VisualElement>(containerName) ?? _root;

            if (_panel == null)
            {
                _panel = CreatePanelElement();
            }

            container.Add(_panel);
            _createdProgrammatically = true;

            if (!_isInitialized)
            {
                QueryAndWireElements();
                _isInitialized = true;
            }

            if (libraryManager != null && libraryManager.AllVFX.Count > 0)
            {
                RefreshUI();
            }

            Debug.Log($"[VFXToggleUI] Injected into {document.name}" +
                     (containerName != null ? $" container '{containerName}'" : ""));
        }

        // Helper class
        private class VFXItemData
        {
            public VFXLibraryManager.VFXEntry Entry;
            public Toggle Toggle;
            public Label NameLabel;
        }
    }
}
