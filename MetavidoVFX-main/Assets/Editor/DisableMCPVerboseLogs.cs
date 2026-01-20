using UnityEditor;

public static class DisableMCPVerboseLogs
{
    [MenuItem("H3M/Debug/Disable MCP Verbose Logs")]
    static void DisableVerbose()
    {
        EditorPrefs.SetBool("MCPForUnity.DebugLogs", false);
        UnityEngine.Debug.Log("MCP verbose logs DISABLED");
    }

    [MenuItem("H3M/Debug/Enable MCP Verbose Logs")]
    static void EnableVerbose()
    {
        EditorPrefs.SetBool("MCPForUnity.DebugLogs", true);
        UnityEngine.Debug.Log("MCP verbose logs ENABLED");
    }
}
