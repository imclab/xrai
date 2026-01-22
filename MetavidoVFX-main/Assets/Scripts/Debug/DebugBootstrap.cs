// DebugBootstrap.cs - Simple initialization for debug infrastructure
// Add to scene to auto-configure debug settings on start

using UnityEngine;

using XRRAI.Debugging;

namespace XRRAI.Debugging
{
    /// <summary>
    /// Bootstraps debug infrastructure at scene start.
    /// Applies DebugConfig settings and shows dashboard if configured.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [AddComponentMenu("MetavidoVFX/Debug/Debug Bootstrap")]
    public class DebugBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Optional explicit config. If null, uses DebugConfig.Instance")]
        [SerializeField] private DebugConfig _config;

        [Header("Quick Settings")]
        [Tooltip("Show VFXPipelineDashboard on start (Tab to toggle)")]
        [SerializeField] private bool _showDashboardOnStart = true;

        [Tooltip("Solo a specific category (mute all others)")]
        [SerializeField] private LogCategory _soloCategory = LogCategory.All;

        void Awake()
        {
            // Apply config settings
            var config = _config != null ? _config : DebugConfig.Instance;
            config?.ApplySettings();

            // Override with solo if set
            if (_soloCategory != LogCategory.All)
            {
                DebugFlags.Solo(_soloCategory);
            }

            DebugFlags.LogSystem($"DebugBootstrap initialized. IsDebugBuild={DebugFlags.IsDebugBuild}", this);
        }

        void Start()
        {
            // Show/hide dashboard
            var dashboard = FindFirstObjectByType<VFXPipelineDashboard>();
            if (dashboard != null)
            {
                if (_showDashboardOnStart)
                    dashboard.Show();
                else
                    dashboard.Hide();
            }
        }

        #region Runtime API

        /// <summary>
        /// Mute a category at runtime.
        /// </summary>
        public void Mute(LogCategory category) => DebugFlags.SetMuted(category, true);

        /// <summary>
        /// Unmute a category at runtime.
        /// </summary>
        public void Unmute(LogCategory category) => DebugFlags.SetMuted(category, false);

        /// <summary>
        /// Solo a category (mute all others).
        /// </summary>
        public void Solo(LogCategory category)
        {
            _soloCategory = category;
            DebugFlags.Solo(category);
        }

        /// <summary>
        /// Unmute all categories.
        /// </summary>
        public void UnmuteAll()
        {
            _soloCategory = LogCategory.All;
            DebugFlags.UnmuteAll();
        }

        #endregion

        #region Editor

        [ContextMenu("Solo Tracking")]
        void SoloTracking() => Solo(LogCategory.Tracking);

        [ContextMenu("Solo VFX")]
        void SoloVFX() => Solo(LogCategory.VFX);

        [ContextMenu("Unmute All")]
        void EditorUnmuteAll() => UnmuteAll();

        #endregion
    }
}
