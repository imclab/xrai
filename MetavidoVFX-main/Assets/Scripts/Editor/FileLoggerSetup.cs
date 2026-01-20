using UnityEngine;
using UnityEditor;

public static class FileLoggerSetup
{
    [MenuItem("H3M/Debug/Add File Logger + Server")]
    public static void AddFileLogger()
    {
        // Check if already exists
        var existing = Object.FindFirstObjectByType<FileLogger>();
        if (existing != null)
        {
            Debug.Log("[FileLoggerSetup] FileLogger already exists in scene");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Create GameObject
        var go = new GameObject("FileLogger");
        go.AddComponent<FileLogger>();
        go.AddComponent<LogServer>();

        Undo.RegisterCreatedObjectUndo(go, "Add File Logger");
        Selection.activeGameObject = go;

        Debug.Log("[FileLoggerSetup] Added FileLogger and LogServer to scene");
        Debug.Log("[FileLoggerSetup] Access logs at http://[device-ip]:8085/logs after build");
    }

    [MenuItem("H3M/Debug/Show Log Server Info")]
    public static void ShowLogServerInfo()
    {
        Debug.Log(@"=== Log Server Usage ===

After deploying to device, access logs via:
  curl http://[device-ip]:8085/logs      # Full logs
  curl http://[device-ip]:8085/recent    # Recent logs
  curl http://[device-ip]:8085/errors    # Errors only
  curl http://[device-ip]:8085/status    # App status

Or use the fetch script:
  ./fetch_logs.sh [device-ip]

Find device IP: Settings > Wi-Fi > tap network > IP Address
");
    }
}
