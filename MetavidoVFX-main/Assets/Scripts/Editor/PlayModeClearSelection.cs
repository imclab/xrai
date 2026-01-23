using UnityEditor;
using UnityEngine;

/// <summary>
/// Automatically clears selection before entering Play mode to prevent
/// Inspector state errors during domain reload.
/// DISABLED (2026-01-22): Reduces domain reload overhead. Minor convenience feature.
/// </summary>
// [InitializeOnLoad] - DISABLED: Minor convenience, not critical
public static class PlayModeClearSelection
{
    static PlayModeClearSelection()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Clear selection when about to enter or exit play mode
        if (state == PlayModeStateChange.ExitingEditMode ||
            state == PlayModeStateChange.ExitingPlayMode)
        {
            Selection.activeObject = null;
            Selection.objects = new Object[0];
        }
    }
}
