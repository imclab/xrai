// SpecSceneUI.cs - UI overlay for spec demo scenes
// Shows scene title and back button to main menu

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MetavidoVFX.UI.Navigation
{
    /// <summary>
    /// UI overlay for spec demo scenes.
    /// Displays scene title at top and back button.
    /// </summary>
    public class SpecSceneUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _backButton;
        [SerializeField] private DebugStatsHUD _debugHUD;

        [Header("Auto-Setup")]
        [SerializeField] private bool _autoSetTitle = true;
        [SerializeField] private bool _autoCreateDebugHUD = true;

        #endregion

        #region MonoBehaviour

        private void Start()
        {
            // Auto-set title from scene name
            if (_autoSetTitle && _titleText != null)
            {
                _titleText.text = SceneNavigator.Instance.GetCurrentSceneDisplayName();
            }

            // Setup back button
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackButtonClick);
            }

            // Auto-create debug HUD if not present
            if (_autoCreateDebugHUD && _debugHUD == null)
            {
                _debugHUD = FindAnyObjectByType<DebugStatsHUD>();
                if (_debugHUD == null)
                {
                    var hudObj = new GameObject("[DebugStatsHUD]");
                    _debugHUD = hudObj.AddComponent<DebugStatsHUD>();
                }
            }
        }

        private void Update()
        {
            // Keyboard shortcut to go back (Escape)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnBackButtonClick();
            }
        }

        #endregion

        #region Button Handlers

        private void OnBackButtonClick()
        {
            Debug.Log("[SpecSceneUI] Returning to main menu");
            SceneNavigator.Instance.LoadMainMenu();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the scene title manually.
        /// </summary>
        public void SetTitle(string title)
        {
            if (_titleText != null)
                _titleText.text = title;
        }

        /// <summary>
        /// Toggle debug HUD visibility.
        /// </summary>
        public void ToggleDebugHUD()
        {
            if (_debugHUD != null)
                _debugHUD.Toggle();
        }

        #endregion
    }
}
