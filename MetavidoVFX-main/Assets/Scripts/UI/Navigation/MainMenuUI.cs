// MainMenuUI.cs - Main menu for spec scene selection
// Displays grid of spec demo buttons

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MetavidoVFX.UI.Navigation
{
    /// <summary>
    /// Main menu UI for selecting spec demo scenes.
    /// Auto-generates buttons for all spec scenes.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Layout")]
        [SerializeField] private Transform _buttonContainer;
        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private int _columns = 2;

        [Header("Styling")]
        [SerializeField] private Color _buttonColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        [SerializeField] private Color _buttonHoverColor = new Color(0.3f, 0.5f, 0.9f, 1f);
        [SerializeField] private float _buttonHeight = 60f;
        [SerializeField] private float _buttonSpacing = 10f;

        [Header("References")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _versionText;

        #endregion

        #region MonoBehaviour

        private void Start()
        {
            // Set title
            if (_titleText != null)
                _titleText.text = "MetavidoVFX Spec Demos";

            // Set version
            if (_versionText != null)
                _versionText.text = $"Unity {Application.unityVersion}";

            // Generate buttons
            GenerateSpecButtons();
        }

        #endregion

        #region Button Generation

        private void GenerateSpecButtons()
        {
            if (_buttonContainer == null)
            {
                Debug.LogError("[MainMenuUI] Button container not assigned");
                return;
            }

            // Clear existing buttons
            foreach (Transform child in _buttonContainer)
            {
                Destroy(child.gameObject);
            }

            // Create button for each spec scene
            for (int i = 0; i < SceneNavigator.SpecScenes.Length; i++)
            {
                CreateSpecButton(i);
            }

            // Setup grid layout if using GridLayoutGroup
            var gridLayout = _buttonContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.constraintCount = _columns;
                gridLayout.cellSize = new Vector2(
                    (gridLayout.GetComponent<RectTransform>().rect.width - _buttonSpacing * (_columns + 1)) / _columns,
                    _buttonHeight);
                gridLayout.spacing = new Vector2(_buttonSpacing, _buttonSpacing);
            }
        }

        private void CreateSpecButton(int specIndex)
        {
            GameObject buttonObj;

            if (_buttonPrefab != null)
            {
                buttonObj = Instantiate(_buttonPrefab, _buttonContainer);
            }
            else
            {
                // Create button from scratch
                buttonObj = new GameObject($"Button_Spec{specIndex:D3}");
                buttonObj.transform.SetParent(_buttonContainer, false);

                // Add Image
                var image = buttonObj.AddComponent<Image>();
                image.color = _buttonColor;

                // Add Button
                var button = buttonObj.AddComponent<Button>();
                var colors = button.colors;
                colors.normalColor = _buttonColor;
                colors.highlightedColor = _buttonHoverColor;
                colors.pressedColor = _buttonColor * 0.8f;
                button.colors = colors;

                // Add Text
                var textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);

                var text = textObj.AddComponent<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 16;
                text.color = Color.white;

                var textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10, 5);
                textRect.offsetMax = new Vector2(-10, -5);
            }

            // Configure button
            var btn = buttonObj.GetComponent<Button>();
            var txt = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (txt != null)
            {
                txt.text = SceneNavigator.SpecDisplayNames[specIndex];
            }

            // Add click handler
            int index = specIndex; // Capture for closure
            btn.onClick.AddListener(() => OnSpecButtonClick(index));
        }

        private void OnSpecButtonClick(int specIndex)
        {
            Debug.Log($"[MainMenuUI] Loading spec {specIndex}: {SceneNavigator.SpecDisplayNames[specIndex]}");
            SceneNavigator.Instance.LoadSpecScene(specIndex);
        }

        #endregion
    }
}
