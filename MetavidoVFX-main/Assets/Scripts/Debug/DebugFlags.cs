// DebugFlags.cs - Compile-time conditional debug logging
// Uses [Conditional] attributes so debug code is stripped from production builds
//
// Usage:
//   DebugFlags.LogTracking("Hand position updated", this);
//   DebugFlags.LogVFX("VFX spawned: Fire", this);
//
// Setup:
//   Add to Player Settings > Scripting Define Symbols:
//   DEBUG_TRACKING;DEBUG_VOICE;DEBUG_VFX;DEBUG_NETWORK;DEBUG_SYSTEM

using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace XRRAI.Debugging
{
    /// <summary>
    /// Category-based debug logging with compile-time stripping.
    /// Each category uses [Conditional] attributes - calls are completely removed
    /// in builds without the corresponding scripting define symbol.
    /// </summary>
    public static class DebugFlags
    {
        // Runtime muting (for categories that ARE compiled in)
        public static bool MuteTracking { get; set; }
        public static bool MuteVoice { get; set; }
        public static bool MuteVFX { get; set; }
        public static bool MuteNetwork { get; set; }
        public static bool MuteSystem { get; set; }
        public static bool MuteAll { get; set; }

        /// <summary>
        /// Check if any debug category is compiled in.
        /// Returns false in production builds where all defines are removed.
        /// </summary>
        public static bool IsDebugBuild
        {
            get
            {
#if DEBUG_TRACKING || DEBUG_VOICE || DEBUG_VFX || DEBUG_NETWORK || DEBUG_SYSTEM
                return true;
#else
                return false;
#endif
            }
        }

        #region Tracking Category

        [Conditional("DEBUG_TRACKING")]
        public static void LogTracking(string message, UnityEngine.Object context = null)
        {
            if (MuteAll || MuteTracking) return;
            Debug.Log($"[TRACKING] {message}", context);
        }

        [Conditional("DEBUG_TRACKING")]
        public static void LogTrackingWarning(string message, UnityEngine.Object context = null)
        {
            if (MuteAll || MuteTracking) return;
            Debug.LogWarning($"[TRACKING] {message}", context);
        }

        [Conditional("DEBUG_TRACKING")]
        public static void LogTrackingError(string message, UnityEngine.Object context = null)
        {
            // Errors are never muted
            Debug.LogError($"[TRACKING] {message}", context);
        }

        #endregion

        #region Voice Category

        [Conditional("DEBUG_VOICE")]
        public static void LogVoice(string message, UnityEngine.Object context = null)
        {
            if (MuteAll || MuteVoice) return;
            Debug.Log($"[VOICE] {message}", context);
        }

        [Conditional("DEBUG_VOICE")]
        public static void LogVoiceWarning(string message, UnityEngine.Object context = null)
        {
            if (MuteAll || MuteVoice) return;
            Debug.LogWarning($"[VOICE] {message}", context);
        }

        [Conditional("DEBUG_VOICE")]
        public static void LogVoiceError(string message, UnityEngine.Object context = null)
        {
            Debug.LogError($"[VOICE] {message}", context);
        }

        #endregion

        #region VFX Category

        [Conditional("DEBUG_VFX")]
        public static void LogVFX(string message, UnityEngine.Object context = null)
        {
            if (MuteAll || MuteVFX) return;
            Debug.Log($"[VFX] {message}", context);
        }

        [Conditional("DEBUG_VFX")]
        public static void LogVFXWarning(string message, UnityEngine.Object context = null)
        {
            if (MuteAll || MuteVFX) return;
            Debug.LogWarning($"[VFX] {message}", context);
        }

        [Conditional("DEBUG_VFX")]
        public static void LogVFXError(string message, UnityEngine.Object context = null)
        {
            Debug.LogError($"[VFX] {message}", context);
        }

        #endregion

        #region Network Category

        [Conditional("DEBUG_NETWORK")]
        public static void LogNetwork(string message, UnityEngine.Object context = null)
        {
            if (MuteAll || MuteNetwork) return;
            Debug.Log($"[NETWORK] {message}", context);
        }

        [Conditional("DEBUG_NETWORK")]
        public static void LogNetworkWarning(string message, UnityEngine.Object context = null)
        {
            if (MuteAll || MuteNetwork) return;
            Debug.LogWarning($"[NETWORK] {message}", context);
        }

        [Conditional("DEBUG_NETWORK")]
        public static void LogNetworkError(string message, UnityEngine.Object context = null)
        {
            Debug.LogError($"[NETWORK] {message}", context);
        }

        #endregion

        #region System Category

        [Conditional("DEBUG_SYSTEM")]
        public static void LogSystem(string message, UnityEngine.Object context = null)
        {
            if (MuteAll || MuteSystem) return;
            Debug.Log($"[SYSTEM] {message}", context);
        }

        [Conditional("DEBUG_SYSTEM")]
        public static void LogSystemWarning(string message, UnityEngine.Object context = null)
        {
            if (MuteAll || MuteSystem) return;
            Debug.LogWarning($"[SYSTEM] {message}", context);
        }

        [Conditional("DEBUG_SYSTEM")]
        public static void LogSystemError(string message, UnityEngine.Object context = null)
        {
            Debug.LogError($"[SYSTEM] {message}", context);
        }

        #endregion

        #region Performance Logging (always compiled, but can be muted)

        /// <summary>
        /// Log with timing info. Always compiled - use for critical performance tracking.
        /// </summary>
        public static void LogTimed(string category, string message, float timeMs, UnityEngine.Object context = null)
        {
            if (MuteAll) return;
            Debug.Log($"[{category}] [{timeMs:F2}ms] {message}", context);
        }

        /// <summary>
        /// Log a frame-specific event (uses Time.frameCount).
        /// </summary>
        [Conditional("DEBUG_TRACKING"), Conditional("DEBUG_VFX"), Conditional("DEBUG_SYSTEM")]
        public static void LogFrame(string category, string message, UnityEngine.Object context = null)
        {
            if (MuteAll) return;
            Debug.Log($"[{category}] [F{Time.frameCount}] {message}", context);
        }

        #endregion

        #region Mute Control

        /// <summary>
        /// Mute a specific category at runtime.
        /// </summary>
        public static void SetMuted(LogCategory category, bool muted)
        {
            switch (category)
            {
                case LogCategory.Tracking: MuteTracking = muted; break;
                case LogCategory.Voice: MuteVoice = muted; break;
                case LogCategory.VFX: MuteVFX = muted; break;
                case LogCategory.Network: MuteNetwork = muted; break;
                case LogCategory.System: MuteSystem = muted; break;
                case LogCategory.All: MuteAll = muted; break;
            }
        }

        /// <summary>
        /// Solo a category (mute all others).
        /// </summary>
        public static void Solo(LogCategory category)
        {
            MuteTracking = category != LogCategory.Tracking && category != LogCategory.All;
            MuteVoice = category != LogCategory.Voice && category != LogCategory.All;
            MuteVFX = category != LogCategory.VFX && category != LogCategory.All;
            MuteNetwork = category != LogCategory.Network && category != LogCategory.All;
            MuteSystem = category != LogCategory.System && category != LogCategory.All;
            MuteAll = false;
        }

        /// <summary>
        /// Unmute all categories.
        /// </summary>
        public static void UnmuteAll()
        {
            MuteTracking = false;
            MuteVoice = false;
            MuteVFX = false;
            MuteNetwork = false;
            MuteSystem = false;
            MuteAll = false;
        }

        #endregion
    }

    public enum LogCategory
    {
        Tracking,
        Voice,
        VFX,
        Network,
        System,
        All
    }
}
