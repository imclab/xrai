// TrackingData - Common data structures for tracking providers (spec-008)

using UnityEngine;

namespace XRRAI.ARTracking
{
    /// <summary>
    /// Body pose data (17-33 keypoints depending on provider).
    /// </summary>
    public struct BodyPoseData : ITrackingData
    {
        public Vector3[] Keypoints;       // World positions
        public float[] Confidences;       // Per-joint confidence 0-1
        public bool IsTracked;
        public float Timestamp;

        // Standard keypoint indices (COCO 17-point format)
        public const int Nose = 0;
        public const int LeftEye = 1;
        public const int RightEye = 2;
        public const int LeftEar = 3;
        public const int RightEar = 4;
        public const int LeftShoulder = 5;
        public const int RightShoulder = 6;
        public const int LeftElbow = 7;
        public const int RightElbow = 8;
        public const int LeftWrist = 9;
        public const int RightWrist = 10;
        public const int LeftHip = 11;
        public const int RightHip = 12;
        public const int LeftKnee = 13;
        public const int RightKnee = 14;
        public const int LeftAnkle = 15;
        public const int RightAnkle = 16;
    }

    /// <summary>
    /// Hand tracking data (21 joints per hand).
    /// </summary>
    public struct HandData : ITrackingData
    {
        public Vector3[] Joints;          // 21 joint positions
        public Quaternion[] Rotations;    // Joint rotations
        public float[] Confidences;
        public bool IsTracked;
        public bool IsLeft;
        public float PinchStrength;       // 0-1 pinch gesture
        public float GrabStrength;        // 0-1 grab gesture
        public float Timestamp;

        // Joint indices (XR Hands standard)
        public const int Wrist = 0;
        public const int ThumbMetacarpal = 1;
        public const int ThumbProximal = 2;
        public const int ThumbDistal = 3;
        public const int ThumbTip = 4;
        public const int IndexMetacarpal = 5;
        public const int IndexProximal = 6;
        public const int IndexIntermediate = 7;
        public const int IndexDistal = 8;
        public const int IndexTip = 9;
        // ... (middle, ring, pinky follow same pattern)
    }

    /// <summary>
    /// Face tracking data (blendshapes + mesh).
    /// </summary>
    public struct FaceData : ITrackingData
    {
        public float[] Blendshapes;       // ARKit 52 blendshapes
        public Vector3[] Vertices;        // Face mesh vertices
        public int[] Indices;             // Face mesh triangles
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsTracked;
        public float Timestamp;
    }

    /// <summary>
    /// Depth data (environment or human).
    /// </summary>
    public struct DepthData : ITrackingData
    {
        public Texture DepthTexture;      // R32Float or R16
        public Texture StencilTexture;    // Human stencil (optional)
        public Texture ColorTexture;      // Camera color
        public Matrix4x4 InverseView;
        public Matrix4x4 InverseProjection;
        public Vector4 RayParams;
        public Vector2 DepthRange;        // Near/far
        public int Width;
        public int Height;
        public bool IsValid;
        public float Timestamp;
    }

    /// <summary>
    /// Body segmentation data (24-part).
    /// </summary>
    public struct SegmentationData : ITrackingData
    {
        public Texture MaskTexture;       // R channel = part index
        public GraphicsBuffer KeypointBuffer;
        public int PartCount;             // 24 for BodyPix
        public bool IsValid;
        public float Timestamp;
    }

    /// <summary>
    /// Audio data for audio-reactive VFX.
    /// </summary>
    public struct AudioData : ITrackingData
    {
        public float Volume;              // RMS 0-1
        public Vector4 Bands;             // Bass, Mids, Treble, SubBass
        public float BeatPulse;           // 1→0 on beat
        public float BeatIntensity;
        public float[] Spectrum;          // Full FFT spectrum
        public bool IsValid;
        public float Timestamp;
    }
}
