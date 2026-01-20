using UnityEditor;

public static class DisableAutoRecompile
{
    [MenuItem("H3M/Editor/Disable Auto-Recompile")]
    public static void DisableAuto()
    {
        // Stop compilation when scripts change
        EditorApplication.LockReloadAssemblies();
        EditorPrefs.SetInt("kAutoRefresh", 0);
        UnityEngine.Debug.Log("[H3M] Auto-recompile disabled. Use H3M > Editor > Force Recompile when ready.");
    }

    [MenuItem("H3M/Editor/Enable Auto-Recompile")]
    public static void EnableAuto()
    {
        EditorApplication.UnlockReloadAssemblies();
        EditorPrefs.SetInt("kAutoRefresh", 1);
        UnityEngine.Debug.Log("[H3M] Auto-recompile enabled.");
    }

    [MenuItem("H3M/Editor/Force Recompile Now")]
    public static void ForceRecompile()
    {
        EditorApplication.UnlockReloadAssemblies();
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("[H3M] Force recompile triggered.");
    }
}
