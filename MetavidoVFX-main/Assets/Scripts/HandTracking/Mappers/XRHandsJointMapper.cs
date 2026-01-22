// XRHandsJointMapper - Maps XR Hands XRHandJointID to unified HandJointID (spec-012)
// XR Hands has 26 joints (full 1:1 mapping with HandJointID)

using System.Collections.Generic;

#if XR_HANDS_1_1_OR_NEWER
using UnityEngine.XR.Hands;
#endif

namespace MetavidoVFX.HandTracking.Mappers
{
    /// <summary>
    /// Maps Unity XR Hands XRHandJointID enum values to unified HandJointID.
    /// XR Hands provides 26 joints (1:1 mapping with HandJointID).
    /// </summary>
    public static class XRHandsJointMapper
    {
#if XR_HANDS_1_1_OR_NEWER
        private static readonly Dictionary<XRHandJointID, HandJointID> _xrHandsToUnified = new()
        {
            { XRHandJointID.Wrist, HandJointID.Wrist },
            { XRHandJointID.Palm, HandJointID.Palm },

            // Thumb
            { XRHandJointID.ThumbMetacarpal, HandJointID.ThumbMetacarpal },
            { XRHandJointID.ThumbProximal, HandJointID.ThumbProximal },
            { XRHandJointID.ThumbDistal, HandJointID.ThumbDistal },
            { XRHandJointID.ThumbTip, HandJointID.ThumbTip },

            // Index
            { XRHandJointID.IndexMetacarpal, HandJointID.IndexMetacarpal },
            { XRHandJointID.IndexProximal, HandJointID.IndexProximal },
            { XRHandJointID.IndexIntermediate, HandJointID.IndexIntermediate },
            { XRHandJointID.IndexDistal, HandJointID.IndexDistal },
            { XRHandJointID.IndexTip, HandJointID.IndexTip },

            // Middle
            { XRHandJointID.MiddleMetacarpal, HandJointID.MiddleMetacarpal },
            { XRHandJointID.MiddleProximal, HandJointID.MiddleProximal },
            { XRHandJointID.MiddleIntermediate, HandJointID.MiddleIntermediate },
            { XRHandJointID.MiddleDistal, HandJointID.MiddleDistal },
            { XRHandJointID.MiddleTip, HandJointID.MiddleTip },

            // Ring
            { XRHandJointID.RingMetacarpal, HandJointID.RingMetacarpal },
            { XRHandJointID.RingProximal, HandJointID.RingProximal },
            { XRHandJointID.RingIntermediate, HandJointID.RingIntermediate },
            { XRHandJointID.RingDistal, HandJointID.RingDistal },
            { XRHandJointID.RingTip, HandJointID.RingTip },

            // Pinky
            { XRHandJointID.LittleMetacarpal, HandJointID.PinkyMetacarpal },
            { XRHandJointID.LittleProximal, HandJointID.PinkyProximal },
            { XRHandJointID.LittleIntermediate, HandJointID.PinkyIntermediate },
            { XRHandJointID.LittleDistal, HandJointID.PinkyDistal },
            { XRHandJointID.LittleTip, HandJointID.PinkyTip },
        };

        private static readonly Dictionary<HandJointID, XRHandJointID> _unifiedToXRHands;

        static XRHandsJointMapper()
        {
            _unifiedToXRHands = new Dictionary<HandJointID, XRHandJointID>();
            foreach (var kvp in _xrHandsToUnified)
            {
                _unifiedToXRHands[kvp.Value] = kvp.Key;
            }
        }

        /// <summary>
        /// Convert XR Hands XRHandJointID to unified HandJointID.
        /// </summary>
        public static HandJointID ToHandJointID(XRHandJointID xrHandJoint)
        {
            return _xrHandsToUnified.TryGetValue(xrHandJoint, out var result)
                ? result
                : HandJointID.Wrist; // Fallback
        }

        /// <summary>
        /// Convert unified HandJointID to XR Hands XRHandJointID.
        /// </summary>
        public static XRHandJointID ToXRHandJointID(HandJointID handJoint)
        {
            return _unifiedToXRHands.TryGetValue(handJoint, out var result)
                ? result
                : XRHandJointID.Wrist; // Fallback
        }

        /// <summary>
        /// Check if a HandJointID has an XR Hands equivalent.
        /// All 26 joints should have equivalents.
        /// </summary>
        public static bool HasXRHandsEquivalent(HandJointID handJoint)
        {
            return _unifiedToXRHands.ContainsKey(handJoint);
        }

        /// <summary>
        /// Get all HandJointIDs supported by XR Hands.
        /// </summary>
        public static IEnumerable<HandJointID> GetSupportedJoints()
        {
            return _unifiedToXRHands.Keys;
        }

        /// <summary>
        /// Get the fingertip joint ID for a given finger index (0=Thumb, 1=Index, etc.)
        /// </summary>
        public static XRHandJointID GetFingertipJoint(int fingerIndex)
        {
            return fingerIndex switch
            {
                0 => XRHandJointID.ThumbTip,
                1 => XRHandJointID.IndexTip,
                2 => XRHandJointID.MiddleTip,
                3 => XRHandJointID.RingTip,
                4 => XRHandJointID.LittleTip,
                _ => XRHandJointID.Wrist
            };
        }
#else
        // Stub methods when XR Hands is not available
        public static HandJointID ToHandJointID(int xrHandJoint) => HandJointID.Wrist;
        public static int ToXRHandJointID(HandJointID handJoint) => 0;
        public static bool HasXRHandsEquivalent(HandJointID handJoint) => false;
        public static IEnumerable<HandJointID> GetSupportedJoints() => System.Array.Empty<HandJointID>();
        public static int GetFingertipJoint(int fingerIndex) => 0;
#endif
    }
}
