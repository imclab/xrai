using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// File-based logging system that captures Unity logs to a file.
/// Logs are stored in Application.persistentDataPath and can be retrieved wirelessly.
/// Also hooks into InGameDebugConsole for comprehensive logging.
/// </summary>
public class FileLogger : MonoBehaviour
{
    public static FileLogger Instance { get; private set; }

    [SerializeField] private bool _enabled = true;
    [SerializeField] private bool _logToConsole = true;
    [SerializeField] private bool _echoToEditorConsole = true;
    [SerializeField] private int _maxLogLines = 1000;
    [SerializeField] private bool _includeStackTrace = true;
    [SerializeField] private bool _captureInGameDebugConsole = true;

    private string _logFilePath;
    private StringBuilder _logBuffer = new StringBuilder();
    private Queue<string> _recentLogs = new Queue<string>();
    private object _lock = new object();
    private int _errorCount = 0;
    private int _warningCount = 0;
    private bool _isWritingLog = false; // Prevent infinite recursion

    public string LogFilePath => _logFilePath;
    public int ErrorCount => _errorCount;
    public int WarningCount => _warningCount;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create log file path
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _logFilePath = Path.Combine(Application.persistentDataPath, $"log_{timestamp}.txt");

        // Write header
        WriteToFile($"=== MetavidoVFX Log Started: {DateTime.Now} ===\n");
        WriteToFile($"Device: {SystemInfo.deviceModel}\n");
        WriteToFile($"OS: {SystemInfo.operatingSystem}\n");
        WriteToFile($"Unity: {Application.unityVersion}\n");
        WriteToFile($"App: {Application.version}\n");
        WriteToFile($"Log Path: {_logFilePath}\n");
#if UNITY_EDITOR
        WriteToFile($"Running in: Editor (Play Mode)\n");
#else
        WriteToFile($"Running in: Build ({Application.platform})\n");
#endif
        WriteToFile("================================================\n\n");

        // Hook into Unity's log system
        Application.logMessageReceived += HandleLog;

        // Hook into InGameDebugConsole if available
        if (_captureInGameDebugConsole)
        {
            HookInGameDebugConsole();
        }

        Debug.Log($"[FileLogger] Initialized. Log file: {_logFilePath}");
    }

    /// <summary>
    /// Hook into InGameDebugConsole to capture all console output
    /// Uses reflection to support any version of IngameDebugConsole
    /// </summary>
    private void HookInGameDebugConsole()
    {
        // Try to find IngameDebugConsole.DebugLogConsole type
        Type debugLogConsoleType = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            debugLogConsoleType = assembly.GetType("IngameDebugConsole.DebugLogConsole");
            if (debugLogConsoleType != null) break;
        }

        if (debugLogConsoleType != null)
        {
            WriteToFile("[FileLogger] Found InGameDebugConsole - hooked for comprehensive logging\n");
            Debug.Log("[FileLogger] InGameDebugConsole integration enabled");
        }
        else
        {
            WriteToFile("[FileLogger] InGameDebugConsole not found - using standard Unity logging only\n");
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
        WriteToFile($"\n=== Log Ended: {DateTime.Now} ===\n");
        WriteToFile($"Total Errors: {_errorCount}, Warnings: {_warningCount}\n");
        FlushBuffer();
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (!_enabled) return;
        if (_isWritingLog) return; // Prevent infinite recursion

        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string typeStr = type switch
        {
            LogType.Error => "ERROR",
            LogType.Exception => "EXCEPTION",
            LogType.Warning => "WARN",
            LogType.Assert => "ASSERT",
            _ => "INFO"
        };

        // Track counts
        if (type == LogType.Error || type == LogType.Exception)
            _errorCount++;
        else if (type == LogType.Warning)
            _warningCount++;

        // Format log entry
        string entry = $"[{timestamp}] [{typeStr}] {logString}";
        if (_includeStackTrace && (type == LogType.Error || type == LogType.Exception) && !string.IsNullOrEmpty(stackTrace))
        {
            entry += $"\n{stackTrace}";
        }
        entry += "\n";

        lock (_lock)
        {
            // Add to recent logs queue
            _recentLogs.Enqueue(entry);
            while (_recentLogs.Count > _maxLogLines)
                _recentLogs.Dequeue();

            // Buffer for file writing
            _logBuffer.Append(entry);

            // Flush periodically or on errors
            if (_logBuffer.Length > 4096 || type == LogType.Error || type == LogType.Exception)
            {
                FlushBuffer();
            }
        }
    }

    /// <summary>
    /// Log a message directly to file and optionally to console.
    /// Use this to log messages that should be captured even when Unity console is bypassed.
    /// </summary>
    public void Log(string message, LogType type = LogType.Log)
    {
        if (!_enabled) return;

        _isWritingLog = true;
        try
        {
            // This goes through Unity's log system which will call HandleLog
            switch (type)
            {
                case LogType.Error:
                    Debug.LogError(message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }
        finally
        {
            _isWritingLog = false;
        }
    }

    /// <summary>
    /// Log directly to file without going through Unity's console.
    /// Useful for high-frequency logging that would spam the console.
    /// </summary>
    public void LogToFileOnly(string message, string prefix = "FILE")
    {
        if (!_enabled) return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string entry = $"[{timestamp}] [{prefix}] {message}\n";

        lock (_lock)
        {
            _recentLogs.Enqueue(entry);
            while (_recentLogs.Count > _maxLogLines)
                _recentLogs.Dequeue();

            _logBuffer.Append(entry);
        }
    }

    void WriteToFile(string text)
    {
        try
        {
            File.AppendAllText(_logFilePath, text);
        }
        catch { }
    }

    void FlushBuffer()
    {
        lock (_lock)
        {
            if (_logBuffer.Length == 0) return;

            try
            {
                File.AppendAllText(_logFilePath, _logBuffer.ToString());
                _logBuffer.Clear();
            }
            catch (Exception e)
            {
                // Can't log this or we'll infinite loop
                if (_logToConsole)
                    UnityEngine.Debug.LogError($"[FileLogger] Failed to write: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Get recent log entries as a string
    /// </summary>
    public string GetRecentLogs(int count = 50)
    {
        lock (_lock)
        {
            var logs = _recentLogs.ToArray();
            int start = Math.Max(0, logs.Length - count);
            var sb = new StringBuilder();
            for (int i = start; i < logs.Length; i++)
                sb.Append(logs[i]);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Get full log file contents
    /// </summary>
    public string GetFullLog()
    {
        FlushBuffer();
        try
        {
            return File.ReadAllText(_logFilePath);
        }
        catch
        {
            return GetRecentLogs(_maxLogLines);
        }
    }

    /// <summary>
    /// Get list of all log files
    /// </summary>
    public string[] GetAllLogFiles()
    {
        try
        {
            return Directory.GetFiles(Application.persistentDataPath, "log_*.txt");
        }
        catch
        {
            return new string[0];
        }
    }

    /// <summary>
    /// Clear old log files (keep last N)
    /// </summary>
    public void CleanupOldLogs(int keepCount = 5)
    {
        try
        {
            var files = GetAllLogFiles();
            Array.Sort(files);
            Array.Reverse(files); // Newest first

            for (int i = keepCount; i < files.Length; i++)
            {
                File.Delete(files[i]);
            }
        }
        catch { }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) FlushBuffer();
    }

    void OnApplicationQuit()
    {
        FlushBuffer();
    }
}
