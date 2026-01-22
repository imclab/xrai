// HoloKitJointMapper - Maps HoloKit JointName to unified HandJointID (spec-012)
// HoloKit has 21 joints, HandJointID has 26 (superset with XR Hands)

using System.Collections.Generic;

#if HOLOKIT_AVAILABLE
using HoloKit.iOS;
#endif

namespace XRRAI.HandTracking
{
    /// <summary>
    /// Maps HoloKit JointName enum values to unified HandJointID.
    /// HoloKit provides 21 joints (no Palm or Metacarpal for fingers).
    /// </summary>
    public static class HoloKitJointMapper
    {
#if HOLOKIT_AVAILABLE
        private static readonly Dictionary<JointName, HandJointID> _holoKitToUnified = new()
        {
            { JointName.Wrist, HandJointID.Wrist },

            // Thumb (HoloKit uses ThumbMP, not ThumbMCP)
            { JointName.ThumbCMC, HandJointID.ThumbMetacarpal },
            { JointName.ThumbMP, HandJointID.ThumbProximal },
            { JointName.ThumbIP, HandJointID.ThumbDistal },
            { JointName.ThumbTip, HandJointID.ThumbTip },

            // Index
            { JointName.IndexMCP, HandJointID.IndexProximal },
            { JointName.IndexPIP, HandJointID.IndexIntermediate },
            { JointName.IndexDIP, HandJointID.IndexDistal },
            { JointName.IndexTip, HandJointID.IndexTip },

            // Middle
            { JointName.MiddleMCP, HandJointID.MiddleProximal },
            { JointName.MiddlePIP, HandJointID.MiddleIntermediate },
            { JointName.MiddleDIP, HandJointID.MiddleDistal },
            { JointName.MiddleTip, HandJointID.MiddleTip },

            // Ring
            { JointName.RingMCP, HandJointID.RingProximal },
            { JointName.RingPIP, HandJointID.RingIntermediate },
            { JointName.RingDIP, HandJointID.RingDistal },
            { JointName.RingTip, HandJointID.RingTip },

            // Pinky (HoloKit uses "Little" instead of "Pinky")
            { JointName.LittleMCP, HandJointID.PinkyProximal },
            { JointName.LittlePIP, HandJointID.PinkyIntermediate },
            { JointName.LittleDIP, HandJointID.PinkyDistal },
            { JointName.LittleTip, HandJointID.PinkyTip },
        };

        private static readonly Dictionary<HandJointID, JointName> _unifiedToHoloKit;

        static HoloKitJointMapper()
        {
            _unifiedToHoloKit = new Dictionary<HandJointID, JointName>();
            foreach (var kvp in _holoKitToUnified)
            {
                _unifiedToHoloKit[kvp.Value] = kvp.Key;
            }
        }

        /// <summary>
        /// Convert HoloKit JointName to unified HandJointID.
        /// </summary>
        public static HandJointID ToHandJointID(JointName holoKitJoint)
        {
            return _holoKitToUnified.TryGetValue(holoKitJoint, out var result)
                ? result
                : HandJointID.Wrist; // Fallback
        }

        /// <summary>
        /// Convert unified HandJointID to HoloKit JointName.
        /// Returns JointName.Wrist if no mapping exists (e.g., Palm, Metacarpals).
        /// </summary>
        public static JointName ToHoloKitJoint(HandJointID handJoint)
        {
            return _unifiedToHoloKit.TryGetValue(handJoint, out var result)
                ? result
                : JointName.Wrist; // Fallback for unmapped joints
        }

        /// <summary>
        /// Check if a HandJointID has a HoloKit equivalent.
        /// Palm and finger Metacarpals are not available in HoloKit.
        /// </summary>
        public static bool HasHoloKitEquivalent(HandJointID handJoint)
        {
            return _unifiedToHoloKit.ContainsKey(handJoint);
        }

        /// <summary>
        /// Get all HandJointIDs supported by HoloKit.
        /// </summary>
        public static IEnumerable<HandJointID> GetSupportedJoints()
        {
            return _unifiedToHoloKit.Keys;
        }
#else
        // Stub methods when HoloKit is not available
        public static HandJointID ToHandJointID(int holoKitJoint) => HandJointID.Wrist;
        public static int ToHoloKitJoint(HandJointID handJoint) => 0;
        public static bool HasHoloKitEquivalent(HandJointID handJoint) => false;
        public static IEnumerable<HandJointID> GetSupportedJoints() => System.Array.Empty<HandJointID>();
#endif
    }
}
