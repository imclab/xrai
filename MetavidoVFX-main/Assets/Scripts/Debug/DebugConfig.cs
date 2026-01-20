// DebugConfig.cs - ScriptableObject for debug settings
// Create via: Assets > Create > MetavidoVFX > Debug Config

using UnityEngine;

using Metavido.Diagnostics;

namespace Metavido.Diagnostics
{
    /// <summary>
    /// Runtime-configurable debug settings.
    /// Create an instance at Assets/Settings/DebugConfig.asset
    /// </summary>
    [CreateAssetMenu(fileName = "DebugConfig", menuName = "MetavidoVFX/Debug Config", order = 100)]
    public class DebugConfig : ScriptableObject
    {
        [Header("Category Muting")]
        [Tooltip("Mute all debug output")]
        public bool muteAll = false;

        [Tooltip("Mute tracking debug logs")]
        public bool muteTracking = false;

        [Tooltip("Mute voice debug logs")]
        public bool muteVoice = false;

        [Tooltip("Mute VFX debug logs")]
        public bool muteVFX = false;

        [Tooltip("Mute network debug logs")]
        public bool muteNetwork = false;

        [Tooltip("Mute system debug logs")]
        public bool muteSystem = false;

        [Header("Log Options")]
        [Tooltip("Include timestamp in log messages")]
        public bool includeTimestamp = false;

        [Tooltip("Include stack trace for errors")]
        public bool includeStackTrace = true;

        [Tooltip("Log to file (uses PlayerLogDumper)")]
        public bool logToFile = true;

        [Header("Visual Debug")]
        [Tooltip("Show VFXPipelineDashboard on start")]
        public bool showDashboardOnStart = true;

        [Tooltip("Show performance warnings as screen overlay")]
        public bool showPerformanceWarnings = true;

        [Header("Editor Mock Data")]
        [Tooltip("Use webcam as mock AR input in Editor")]
        public bool useWebcamMock = true;

        [Tooltip("Preferred webcam index (0 = default)")]
        public int preferredWebcamIndex = 0;

        [Tooltip("Mock depth texture resolution")]
        public Vector2Int mockResolution = new Vector2Int(256, 192);

        /// <summary>
        /// Apply settings to DebugFlags at runtime.
        /// Call this from a MonoBehaviour's Awake or Start.
        /// </summary>
        public void ApplySettings()
        {
            DebugFlags.MuteAll = muteAll;
            DebugFlags.MuteTracking = muteTracking;
            DebugFlags.MuteVoice = muteVoice;
            DebugFlags.MuteVFX = muteVFX;
            DebugFlags.MuteNetwork = muteNetwork;
            DebugFlags.MuteSystem = muteSystem;
        }

        #region Singleton Access

        private static DebugConfig _instance;

        /// <summary>
        /// Get the default DebugConfig from Resources.
        /// Place DebugConfig.asset in Resources folder or use explicit reference.
        /// </summary>
        public static DebugConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<DebugConfig>("DebugConfig");
                    if (_instance == null)
                    {
                        // Create runtime defaults if no asset exists
                        _instance = CreateInstance<DebugConfig>();
                        _instance.name = "DebugConfig (Runtime Default)";
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Set a specific instance as the active config.
        /// </summary>
        public static void SetInstance(DebugConfig config)
        {
            _instance = config;
            config?.ApplySettings();
        }

        #endregion
    }
}
