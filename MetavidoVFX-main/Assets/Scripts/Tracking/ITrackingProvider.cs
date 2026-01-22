// ITrackingProvider - Core interface for cross-platform tracking (spec-008)
// Enables ARKit, MediaPipe, ML, and WebXR providers to share common API

using System;
using UnityEngine;

namespace XRRAI.ARTracking
{
    /// <summary>
    /// Tracking capabilities bitmask for feature detection.
    /// </summary>
    [Flags]
    public enum TrackingCap
    {
        None = 0,
        Body = 1 << 0,          // Full body pose (33 joints)
        Hands = 1 << 1,         // Hand tracking (21 joints per hand)
        Face = 1 << 2,          // Face mesh/blendshapes
        Depth = 1 << 3,         // Environment depth
        HumanDepth = 1 << 4,    // Human-only depth/stencil
        Segmentation = 1 << 5,  // Body part segmentation (24-part)
        ARMesh = 1 << 6,        // AR mesh reconstruction
        Keypoints = 1 << 7,     // Pose keypoints (17-33)
        Audio = 1 << 8,         // Audio/microphone input
        Eye = 1 << 9,           // Eye gaze tracking
        All = ~0
    }

    /// <summary>
    /// Supported platforms bitmask.
    /// </summary>
    [Flags]
    public enum Platform
    {
        None = 0,
        iOS = 1 << 0,
        Android = 1 << 1,
        Quest = 1 << 2,
        VisionPro = 1 << 3,
        WebGL = 1 << 4,
        Editor = 1 << 5,
        All = ~0
    }

    /// <summary>
    /// Marker interface for tracking data structs.
    /// </summary>
    public interface ITrackingData { }

    /// <summary>
    /// Core interface for all tracking providers.
    /// Implements pull-based data access with optional push events.
    /// </summary>
    public interface ITrackingProvider : IDisposable
    {
        // Identity
        string Id { get; }
        int Priority { get; }
        Platform SupportedPlatforms { get; }
        TrackingCap Capabilities { get; }
        bool IsAvailable { get; }

        // Lifecycle
        void Initialize();
        void Update();
        void Shutdown();

        // Data access (pull model - zero allocation)
        bool TryGetData<T>(out T data) where T : struct, ITrackingData;

        // Events (push model)
        event Action<TrackingCap> OnCapabilitiesChanged;
        event Action OnTrackingLost;
        event Action OnTrackingFound;
    }

    /// <summary>
    /// Interface for components that consume tracking data.
    /// </summary>
    public interface ITrackingConsumer
    {
        TrackingCap RequiredCapabilities { get; }
        void OnTrackingData(ITrackingProvider provider);
        void OnTrackingLost();
    }

    /// <summary>
    /// Attribute to mark tracking provider classes for auto-discovery.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TrackingProviderAttribute : Attribute
    {
        public string Id { get; }
        public int Priority { get; }
        public Platform Platforms { get; }
        public TrackingCap Capabilities { get; }

        public TrackingProviderAttribute(string id, int priority = 0)
        {
            Id = id;
            Priority = priority;
            Platforms = Platform.All;
            Capabilities = TrackingCap.None;
        }
    }

    /// <summary>
    /// Attribute to mark tracking consumer classes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TrackingConsumerAttribute : Attribute
    {
        public TrackingCap Required { get; }

        public TrackingConsumerAttribute(TrackingCap required)
        {
            Required = required;
        }
    }
}
