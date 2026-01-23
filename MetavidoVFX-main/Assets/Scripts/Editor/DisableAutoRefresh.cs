using UnityEditor;
using System.Reflection;

public static class DisableAutoRefresh
{
    [MenuItem("H3M/Quick/STOP Recompile Loop NOW")]
    public static void StopLoop()
    {
        // Disable Unity auto-refresh
        EditorPrefs.SetInt("kAutoRefresh", 0);

        // Disable script compilation on play
        EditorPrefs.SetBool("ScriptCompilationDuringPlay", false);

        // Lock reload assemblies
        EditorApplication.LockReloadAssemblies();

        // Try to disable Rider's auto-refresh via reflection
        try
        {
            var riderType = System.Type.GetType("JetBrains.Rider.Unity.Editor.RiderScriptEditor, JetBrains.Rider.Unity.Editor.Plugin.2022.3.Net46.Repacked");
            if (riderType != null)
            {
                UnityEngine.Debug.Log("[AutoRefresh] Found Rider plugin - check Rider preferences to disable 'Regenerate project files'");
            }
        }
        catch { }

        UnityEngine.Debug.Log("[AutoRefresh] STOPPED - Assembly reload locked. Run 'Unlock Assemblies' when ready.");
    }

    [MenuItem("H3M/Quick/Unlock Assemblies")]
    public static void Unlock()
    {
        EditorApplication.UnlockReloadAssemblies();
        UnityEngine.Debug.Log("[AutoRefresh] Assemblies unlocked - manual refresh with Ctrl/Cmd+R");
    }

    [MenuItem("H3M/Quick/Disable Auto Refresh")]
    public static void Disable()
    {
        EditorPrefs.SetInt("kAutoRefresh", 0);
        UnityEngine.Debug.Log("[AutoRefresh] Disabled - use Ctrl/Cmd+R to manually refresh");
    }

    [MenuItem("H3M/Quick/Enable Auto Refresh")]
    public static void Enable()
    {
        EditorPrefs.SetInt("kAutoRefresh", 1);
        EditorApplication.UnlockReloadAssemblies();
        UnityEngine.Debug.Log("[AutoRefresh] Enabled");
    }
}
