// IHandTrackingProvider - Unified hand tracking interface (spec-012)
// Abstract over HoloKit, XR Hands, BodyPix, and Touch fallbacks

using System;
using UnityEngine;

namespace XRRAI.HandTracking
{
    /// <summary>
    /// Unified hand tracking interface for all backends.
    /// Implementations: HoloKit, XR Hands, BodyPix, Touch.
    /// </summary>
    public interface IHandTrackingProvider : IDisposable
    {
        string Id { get; }
        int Priority { get; }
        bool IsAvailable { get; }

        void Initialize();
        void Update();
        void Shutdown();

        bool IsHandTracked(Hand hand);
        Vector3 GetJointPosition(Hand hand, HandJointID joint);
        Quaternion GetJointRotation(Hand hand, HandJointID joint);
        float GetJointRadius(Hand hand, HandJointID joint);

        bool IsGestureActive(Hand hand, GestureType gesture);
        float GetPinchStrength(Hand hand);
        float GetGrabStrength(Hand hand);

        event Action<Hand> OnHandTrackingGained;
        event Action<Hand> OnHandTrackingLost;
        event Action<Hand, GestureType> OnGestureStart;
        event Action<Hand, GestureType> OnGestureEnd;
    }

    public enum Hand
    {
        Left = 0,
        Right = 1
    }

    public enum GestureType
    {
        None = 0,
        Pinch = 1,      // Thumb + Index touch
        Grab = 2,       // All fingers curled
        Point = 3,      // Index extended, others curled
        OpenPalm = 4,   // All fingers extended
        ThumbsUp = 5    // Thumb extended, others curled
    }

    /// <summary>
    /// Unified joint IDs mapping HoloKit (21) and XR Hands (26) joints.
    /// </summary>
    public enum HandJointID
    {
        // Wrist
        Wrist = 0,
        Palm = 1,

        // Thumb (4 joints)
        ThumbMetacarpal = 2,
        ThumbProximal = 3,
        ThumbDistal = 4,
        ThumbTip = 5,

        // Index (5 joints)
        IndexMetacarpal = 6,
        IndexProximal = 7,
        IndexIntermediate = 8,
        IndexDistal = 9,
        IndexTip = 10,

        // Middle (5 joints)
        MiddleMetacarpal = 11,
        MiddleProximal = 12,
        MiddleIntermediate = 13,
        MiddleDistal = 14,
        MiddleTip = 15,

        // Ring (5 joints)
        RingMetacarpal = 16,
        RingProximal = 17,
        RingIntermediate = 18,
        RingDistal = 19,
        RingTip = 20,

        // Pinky (5 joints)
        PinkyMetacarpal = 21,
        PinkyProximal = 22,
        PinkyIntermediate = 23,
        PinkyDistal = 24,
        PinkyTip = 25,

        Count = 26
    }

    /// <summary>
    /// Mark a class as a hand tracking provider for auto-discovery.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class HandTrackingProviderAttribute : Attribute
    {
        public string Id { get; }
        public int Priority { get; }

        public HandTrackingProviderAttribute(string id, int priority = 0)
        {
            Id = id;
            Priority = priority;
        }
    }
}
