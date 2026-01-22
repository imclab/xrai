// DebugLogger.cs - Runtime log filtering and history
// Works with DebugFlags to provide runtime control over logging

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace XRRAI.Debugging
{
    public enum LogLevel { Verbose, Info, Warning, Error }

    public struct LogEntry
    {
        public LogCategory Category;
        public LogLevel Level;
        public string Message;
        public float Time;

        public LogEntry(LogCategory category, LogLevel level, string message, float time)
        {
            Category = category;
            Level = level;
            Message = message;
            Time = time;
        }
    }

    /// <summary>
    /// Centralized logger with history and filtering.
    /// Can be used by DebugOverlay to show logs in-game.
    /// </summary>
    public static class DebugLogger
    {
        private static DebugConfig _config;
        private static List<LogEntry> _history = new();
        private static int _maxHistory = 1000;

        public static void Log(LogCategory category, LogLevel level, string message)
        {
            if (_config == null) _config = DebugConfig.Instance;

            // Check config filters
            if (_config != null)
            {
                // Skip if muted in config
                if (_config.muteAll) return;

                switch (category)
                {
                    case LogCategory.Tracking: if (_config.muteTracking) return; break;
                    case LogCategory.Voice: if (_config.muteVoice) return; break;
                    case LogCategory.VFX: if (_config.muteVFX) return; break;
                    case LogCategory.Network: if (_config.muteNetwork) return; break;
                    case LogCategory.System: if (_config.muteSystem) return; break;
                }
            }

            // Add to history
            var entry = new LogEntry(category, level, message, Time.time);
            _history.Add(entry);
            if (_history.Count > _maxHistory)
                _history.RemoveAt(0);

            // Forward to Unity console with prefix
            string prefix = _config != null && _config.includeTimestamp ? $"[{Time.time:F2}] " : "";
            string catStr = $"[{category}]";

            switch (level)
            {
                case LogLevel.Verbose:
                case LogLevel.Info:
                    Debug.Log($"{prefix}{catStr} {message}");
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning($"{prefix}{catStr} {message}");
                    break;
                case LogLevel.Error:
                    Debug.LogError($"{prefix}{catStr} {message}");
                    break;
            }
        }

        public static IReadOnlyList<LogEntry> GetHistory() => _history;
        public static void Clear() => _history.Clear();

        public static void SetConfig(DebugConfig config)
        {
            _config = config;
        }
    }
}
