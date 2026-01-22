// GestureConfig - ScriptableObject for gesture detection thresholds (spec-012)
// Create via Assets > Create > MetavidoVFX > Gestures > Gesture Config

using UnityEngine;

namespace MetavidoVFX.HandTracking.Gestures
{
    /// <summary>
    /// Configurable gesture detection thresholds.
    /// Create instances for different use cases (precise drawing, casual interaction, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "GestureConfig", menuName = "MetavidoVFX/Gestures/Gesture Config")]
    public class GestureConfig : ScriptableObject
    {
        [Header("Pinch Detection")]
        [Tooltip("Distance (m) at which pinch gesture starts")]
        [Range(0.01f, 0.05f)]
        public float PinchStartThreshold = 0.02f;

        [Tooltip("Distance (m) at which pinch gesture ends (hysteresis)")]
        [Range(0.02f, 0.08f)]
        public float PinchEndThreshold = 0.04f;

        [Header("Grab Detection")]
        [Tooltip("Angle (degrees) at which a finger is considered curled")]
        [Range(30f, 90f)]
        public float FingerCurlAngle = 60f;

        [Tooltip("Minimum curled fingers for grab (1-4, excluding thumb)")]
        [Range(1, 4)]
        public int GrabMinCurledFingers = 4;

        [Header("Hold Detection")]
        [Tooltip("Time (s) before OnGestureHold fires")]
        [Range(0.1f, 2f)]
        public float HoldDuration = 0.5f;

        [Header("Two-Hand Gestures")]
        [Tooltip("Maximum distance (m) between hands for two-hand gestures")]
        [Range(0.1f, 0.5f)]
        public float TwoHandMaxDistance = 0.3f;

        [Tooltip("Time (s) both hands must hold gesture for two-hand activation")]
        [Range(0.1f, 1f)]
        public float TwoHandHoldTime = 0.3f;

        [Header("Velocity Thresholds")]
        [Tooltip("Minimum hand speed (m/s) for 'moving' state")]
        [Range(0.01f, 0.2f)]
        public float MinMoveSpeed = 0.05f;

        [Tooltip("Speed (m/s) considered 'fast' movement")]
        [Range(0.2f, 1f)]
        public float FastMoveSpeed = 0.5f;

        /// <summary>
        /// Apply this config to a GestureDetector.
        /// </summary>
        public void ApplyTo(GestureDetector detector)
        {
            if (detector == null) return;

            detector.PinchStartThreshold = PinchStartThreshold;
            detector.PinchEndThreshold = PinchEndThreshold;
            detector.FingerCurlAngle = FingerCurlAngle;
            detector.HoldDuration = HoldDuration;
        }

        /// <summary>
        /// Create a GestureDetector configured with these settings.
        /// </summary>
        public GestureDetector CreateDetector(Hand hand)
        {
            var detector = new GestureDetector(hand, PinchStartThreshold, PinchEndThreshold, FingerCurlAngle);
            detector.HoldDuration = HoldDuration;
            return detector;
        }

        #region Presets

        /// <summary>
        /// Preset for precise brush painting (tight thresholds).
        /// </summary>
        public static GestureConfig CreatePrecisePreset()
        {
            var config = CreateInstance<GestureConfig>();
            config.PinchStartThreshold = 0.015f;
            config.PinchEndThreshold = 0.025f;
            config.FingerCurlAngle = 50f;
            config.HoldDuration = 0.3f;
            config.TwoHandMaxDistance = 0.2f;
            return config;
        }

        /// <summary>
        /// Preset for casual interaction (forgiving thresholds).
        /// </summary>
        public static GestureConfig CreateCasualPreset()
        {
            var config = CreateInstance<GestureConfig>();
            config.PinchStartThreshold = 0.03f;
            config.PinchEndThreshold = 0.06f;
            config.FingerCurlAngle = 70f;
            config.HoldDuration = 0.5f;
            config.TwoHandMaxDistance = 0.4f;
            return config;
        }

        #endregion
    }
}
