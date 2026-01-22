#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MetavidoVFX.Editor
{
    /// <summary>
    /// Ensures Unity stays in Play mode without pausing for AR Foundation Remote testing.
    /// Prevents focus loss pausing and keeps the game running in background.
    /// </summary>
    [InitializeOnLoad]
    public static class ContinuousPlayMode
    {
        private static bool _continuousModeEnabled = false;

        static ContinuousPlayMode()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("H3M/Testing/Enable Continuous Play Mode")]
        public static void EnableContinuousPlayMode()
        {
            _continuousModeEnabled = true;
            ApplySettings();
            Debug.Log("[ContinuousPlayMode] Enabled - Editor will not pause on focus loss");
        }

        [MenuItem("H3M/Testing/Disable Continuous Play Mode")]
        public static void DisableContinuousPlayMode()
        {
            _continuousModeEnabled = false;
            Debug.Log("[ContinuousPlayMode] Disabled - Normal pause behavior restored");
        }

        [MenuItem("H3M/Testing/Toggle Continuous Play Mode")]
        public static void ToggleContinuousPlayMode()
        {
            _continuousModeEnabled = !_continuousModeEnabled;
            if (_continuousModeEnabled)
            {
                ApplySettings();
                Debug.Log("[ContinuousPlayMode] Enabled");
            }
            else
            {
                Debug.Log("[ContinuousPlayMode] Disabled");
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && _continuousModeEnabled)
            {
                ApplySettings();
            }
        }

        private static void ApplySettings()
        {
            // Prevent pausing when Editor loses focus
            EditorApplication.pauseStateChanged += OnPauseStateChanged;

            // Run in background (prevents pause when switching apps)
            Application.runInBackground = true;

            // Disable "Pause on Focus Loss" in Editor preferences
            // This is stored in EditorPrefs
            EditorPrefs.SetBool("PauseOnFocusLost", false);

            Debug.Log("[ContinuousPlayMode] Settings applied:");
            Debug.Log("  - Application.runInBackground = true");
            Debug.Log("  - PauseOnFocusLost = false");
        }

        private static void OnPauseStateChanged(PauseState state)
        {
            if (_continuousModeEnabled && state == PauseState.Paused)
            {
                // Immediately unpause if continuous mode is enabled
                EditorApplication.isPaused = false;
                Debug.Log("[ContinuousPlayMode] Auto-unpaused");
            }
        }
    }

    /// <summary>
    /// Runtime component to ensure continuous play during AR testing.
    /// Add to scene for persistent behavior.
    /// </summary>
    public class ContinuousPlayModeRuntime : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _preventPause = true;
        [SerializeField] private bool _runInBackground = true;
        [SerializeField] private bool _showStatus = true;

        private void Awake()
        {
            if (_runInBackground)
            {
                Application.runInBackground = true;
            }
        }

        private void Update()
        {
            #if UNITY_EDITOR
            if (_preventPause && EditorApplication.isPaused)
            {
                EditorApplication.isPaused = false;
            }
            #endif
        }

        private void OnGUI()
        {
            if (!_showStatus) return;

            GUILayout.BeginArea(new Rect(10, 10, 200, 60));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Continuous Play Mode");
            GUILayout.Label($"RunInBackground: {Application.runInBackground}");
            #if UNITY_EDITOR
            GUILayout.Label($"Paused: {EditorApplication.isPaused}");
            #endif
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
#endif
