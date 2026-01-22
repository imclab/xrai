using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace XRRAI.VoiceToObject
{
    /// <summary>
    /// UI Toolkit interface for searching and placing 3D models from Icosa/Sketchfab.
    /// Spec-009: Manual Search and Browse (P1)
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ModelSearchUI : MonoBehaviour
    {
        [SerializeField] UnifiedModelSearch _searchManager;
        [SerializeField] ModelPlacer _placer;

        UIDocument _document;
        TextField _searchField;
        ScrollView _resultsContainer;
        VisualElement _previewPanel;
        Label _statusLabel;
        Button _searchButton;

        UnifiedSearchResult _selectedResult;
        bool _isLoading;

        void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_searchManager == null)
                _searchManager = FindObjectOfType<UnifiedModelSearch>();
            if (_placer == null)
                _placer = FindObjectOfType<ModelPlacer>();
        }

        void OnEnable()
        {
            CreateUI();
            BindEvents();
        }

        void OnDisable()
        {
            UnbindEvents();
        }

        void CreateUI()
        {
            var root = _document.rootVisualElement;
            root.Clear();

            // Main container
            var container = new VisualElement();
            container.style.position = Position.Absolute;
            container.style.top = 80;
            container.style.right = 20;
            container.style.width = 320;
            container.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            container.style.borderTopLeftRadius = 12;
            container.style.borderTopRightRadius = 12;
            container.style.borderBottomLeftRadius = 12;
            container.style.borderBottomRightRadius = 12;
            container.style.paddingTop = 16;
            container.style.paddingBottom = 16;
            container.style.paddingLeft = 16;
            container.style.paddingRight = 16;
            root.Add(container);

            // Title
            var title = new Label("3D Model Search");
            title.style.fontSize = 18;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 12;
            container.Add(title);

            // Search row
            var searchRow = new VisualElement();
            searchRow.style.flexDirection = FlexDirection.Row;
            searchRow.style.marginBottom = 12;
            container.Add(searchRow);

            _searchField = new TextField();
            _searchField.style.flexGrow = 1;
            _searchField.style.marginRight = 8;
            _searchField.value = "";
            searchRow.Add(_searchField);

            _searchButton = new Button(OnSearchClicked) { text = "Search" };
            _searchButton.style.width = 70;
            searchRow.Add(_searchButton);

            // Status
            _statusLabel = new Label("Enter keywords to search");
            _statusLabel.style.fontSize = 12;
            _statusLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            _statusLabel.style.marginBottom = 8;
            container.Add(_statusLabel);

            // Results scroll view
            _resultsContainer = new ScrollView(ScrollViewMode.Vertical);
            _resultsContainer.style.height = 300;
            _resultsContainer.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.8f);
            _resultsContainer.style.borderTopLeftRadius = 8;
            _resultsContainer.style.borderTopRightRadius = 8;
            _resultsContainer.style.borderBottomLeftRadius = 8;
            _resultsContainer.style.borderBottomRightRadius = 8;
            container.Add(_resultsContainer);

            // Preview panel (hidden by default)
            _previewPanel = new VisualElement();
            _previewPanel.style.display = DisplayStyle.None;
            _previewPanel.style.marginTop = 12;
            container.Add(_previewPanel);
        }

        void BindEvents()
        {
            if (_searchManager != null)
            {
                _searchManager.OnSearchComplete += OnSearchComplete;
                _searchManager.OnSearchError += OnSearchError;
            }
            _searchField.RegisterCallback<KeyDownEvent>(OnSearchKeyDown);
        }

        void UnbindEvents()
        {
            if (_searchManager != null)
            {
                _searchManager.OnSearchComplete -= OnSearchComplete;
                _searchManager.OnSearchError -= OnSearchError;
            }
        }

        void OnSearchKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                OnSearchClicked();
        }

        async void OnSearchClicked()
        {
            var query = _searchField.value?.Trim();
            if (string.IsNullOrEmpty(query)) return;
            if (_isLoading) return;

            _isLoading = true;
            _statusLabel.text = $"Searching for \"{query}\"...";
            _searchButton.SetEnabled(false);
            _resultsContainer.Clear();

            await _searchManager.SearchAsync(query);
        }

        void OnSearchComplete(List<UnifiedSearchResult> results)
        {
            _isLoading = false;
            _searchButton.SetEnabled(true);

            _resultsContainer.Clear();

            if (results == null || results.Count == 0)
            {
                _statusLabel.text = "No results found";
                return;
            }

            _statusLabel.text = $"Found {results.Count} models";

            foreach (var result in results)
            {
                var item = CreateResultItem(result);
                _resultsContainer.Add(item);
            }
        }

        void OnSearchError(string error)
        {
            _isLoading = false;
            _searchButton.SetEnabled(true);
            _statusLabel.text = $"Error: {error}";
        }

        VisualElement CreateResultItem(UnifiedSearchResult result)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.paddingTop = 8;
            item.style.paddingBottom = 8;
            item.style.paddingLeft = 8;
            item.style.paddingRight = 8;
            item.style.borderBottomWidth = 1;
            item.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            // Thumbnail placeholder
            var thumb = new VisualElement();
            thumb.style.width = 60;
            thumb.style.height = 60;
            thumb.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            thumb.style.marginRight = 12;
            item.Add(thumb);

            // Info column
            var info = new VisualElement();
            info.style.flexGrow = 1;

            var nameLabel = new Label(result.DisplayName);
            nameLabel.style.color = Color.white;
            nameLabel.style.fontSize = 14;
            info.Add(nameLabel);

            var sourceLabel = new Label(result.Source.ToString());
            sourceLabel.style.color = result.Source == ModelSource.Icosa
                ? new Color(0.4f, 0.8f, 0.4f)
                : new Color(0.4f, 0.6f, 1f);
            sourceLabel.style.fontSize = 11;
            info.Add(sourceLabel);

            var authorLabel = new Label($"by {result.AuthorName}");
            authorLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            authorLabel.style.fontSize = 11;
            info.Add(authorLabel);

            item.Add(info);

            // Place button
            var placeBtn = new Button(() => OnPlaceClicked(result)) { text = "Place" };
            placeBtn.style.height = 40;
            placeBtn.style.alignSelf = Align.Center;
            item.Add(placeBtn);

            return item;
        }

        async void OnPlaceClicked(UnifiedSearchResult result)
        {
            _selectedResult = result;
            _statusLabel.text = $"Loading {result.DisplayName}...";

            if (_placer != null)
            {
                await _placer.PlaceModelAsync(result);
                _statusLabel.text = $"Placed: {result.DisplayName}";
            }
            else
            {
                _statusLabel.text = "ModelPlacer not configured";
            }
        }
    }
}
