using UnityEngine;
using System.IO;

namespace Metavido {
    public class PlayerLogDumper : MonoBehaviour {
        private string logPath;
        private StreamWriter writer;

        void Awake() {
            logPath = Path.Combine(Application.persistentDataPath, "player_console.log");
            writer = new StreamWriter(logPath, false);
            writer.AutoFlush = true;
            writer.WriteLine($"=== PLAYER CONSOLE LOG STARTED {System.DateTime.Now} ===");
            Application.logMessageReceived += HandleLog;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[PlayerLogDumper] Logging to: {logPath}");
        }

        void HandleLog(string logString, string stackTrace, LogType type) {
            if (writer == null) return;
            string prefix = $"[{type}] [{Time.frameCount}] ";
            writer.WriteLine(prefix + logString);
            if (type == LogType.Error || type == LogType.Exception) {
                writer.WriteLine(stackTrace);
            }
        }

        void OnDestroy() {
            Application.logMessageReceived -= HandleLog;
            writer?.Close();
        }
    }
}
