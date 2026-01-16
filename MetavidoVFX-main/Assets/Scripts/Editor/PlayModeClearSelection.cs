using UnityEditor;
using UnityEngine;

/// <summary>
/// Automatically clears selection before entering Play mode to prevent
/// Inspector state errors during domain reload.
/// </summary>
[InitializeOnLoad]
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
