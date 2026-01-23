#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Suppresses the "Reading pixels out of bounds" error from AR Foundation Remote.
/// The error occurs in EditorViewSender.cs when screen dimensions don't match render target.
/// This is harmless but spams the console.
/// DISABLED (2026-01-22): Filter doesn't actually work. Use menu items manually.
/// </summary>
// [InitializeOnLoad] - DISABLED: Filter ineffective, menu items work manually
public static class ARRemoteReadPixelsFix
{
    private const string PREF_KEY = "ARRemote_SuppressReadPixels";
    private static bool _isSubscribed = false;

    static ARRemoteReadPixelsFix()
    {
        // Auto-enable suppression if previously set
        if (EditorPrefs.GetBool(PREF_KEY, true)) // Default to true
        {
            EnableSuppression();
        }

        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool(PREF_KEY, true))
        {
            EnableSuppression();
        }
    }

    static void EnableSuppression()
    {
        if (!_isSubscribed)
        {
            Application.logMessageReceived += FilterLogMessage;
            _isSubscribed = true;
        }
    }

    static void DisableSuppression()
    {
        if (_isSubscribed)
        {
            Application.logMessageReceived -= FilterLogMessage;
            _isSubscribed = false;
        }
    }

    static void FilterLogMessage(string condition, string stackTrace, LogType type)
    {
        // We can't actually prevent the log, but we can clear it immediately
        // This is a workaround since Unity doesn't allow suppressing specific errors
        if (type == LogType.Error && condition.Contains("Reading pixels out of bounds"))
        {
            // The error has already been logged, but we can mark it as "known issue"
            // by doing nothing here - at least we've identified the source
        }
    }

    [MenuItem("H3M/AR Remote/Suppress ReadPixels Warnings (Toggle)")]
    static void ToggleSuppression()
    {
        var current = EditorPrefs.GetBool(PREF_KEY, true);
        EditorPrefs.SetBool(PREF_KEY, !current);

        if (!current)
        {
            EnableSuppression();
            Debug.Log("[ARRemoteFix] ReadPixels warning filter enabled.");
        }
        else
        {
            DisableSuppression();
            Debug.Log("[ARRemoteFix] ReadPixels warning filter disabled.");
        }
    }

    [MenuItem("H3M/AR Remote/Fix: Disable AR Remote Editor View")]
    static void DisableARRemoteEditorView()
    {
        // Find and disable the EditorViewSender component
        var senders = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var sender in senders)
        {
            if (sender.GetType().Name == "EditorViewSender")
            {
                sender.enabled = false;
                Debug.Log("[ARRemoteFix] Disabled EditorViewSender - ReadPixels errors should stop.");
                return;
            }
        }
        Debug.Log("[ARRemoteFix] EditorViewSender not found. It may start when entering Play mode.");
    }

    [MenuItem("H3M/AR Remote/Open AR Remote Settings")]
    static void OpenARRemoteSettings()
    {
        EditorApplication.ExecuteMenuItem("Window/XR/AR Remote/Connection");
    }
}
#endif
