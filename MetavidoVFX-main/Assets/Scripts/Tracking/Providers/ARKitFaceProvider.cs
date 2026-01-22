// ARKitFaceProvider - Native 52-blendshape face tracking via AR Foundation (spec-008)
// PRIORITY: P1 - Native face tracking for expressions and VFX
// iOS: TrueDepth camera (iPhone X+) or Neural Engine (A12+, iOS 14+)
// ~3ms latency, supports front/rear camera modes

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace XRRAI.ARTracking
{
    /// <summary>
    /// ARKit native face tracking provider (52 blendshapes + mesh).
    /// Uses ARFaceManager for real-time facial expression tracking.
    ///
    /// Key features:
    /// - 52 ARKit blendshapes (eyes, brows, mouth, jaw, cheeks, nose)
    /// - 1220 vertex face mesh with topology
    /// - ~3ms latency
    /// - Works with front camera (TrueDepth) or rear camera (iOS 13+)
    ///
    /// Blendshape categories:
    /// - Eyes (12): blink, look, squint, wide
    /// - Brows (4): down, inner up, outer up
    /// - Mouth (24): smile, frown, pucker, funnel, press, stretch, roll
    /// - Jaw (4): forward, left, right, open
    /// - Cheeks (2): puff, squint
    /// - Nose (2): sneer left/right
    /// - Tongue (1): out
    /// </summary>
    [TrackingProvider("arkit-face", priority: 85)]
    public class ARKitFaceProvider : ITrackingProvider
    {
        public string Id => "arkit-face";
        public int Priority => 85;  // High priority - native tracking
        public Platform SupportedPlatforms => Platform.iOS | Platform.Editor;
        public TrackingCap Capabilities => TrackingCap.Face;

        private ARFaceManager _faceManager;
        private FaceData _cachedData;
        private bool _initialized;
        private bool _wasTracking;

        // Cached blendshape array (52 ARKit blendshapes)
        private float[] _blendshapes = new float[52];

        public bool IsAvailable
        {
            get
            {
#if UNITY_IOS || UNITY_EDITOR
                if (!_initialized) return false;
                if (_faceManager == null) return false;

                var subsystem = _faceManager.subsystem;
                return subsystem != null && subsystem.running;
#else
                return false;
#endif
            }
        }

        public event Action<TrackingCap> OnCapabilitiesChanged;
        public event Action OnTrackingLost;
        public event Action OnTrackingFound;

        public void Initialize()
        {
            _faceManager = UnityEngine.Object.FindAnyObjectByType<ARFaceManager>();

            if (_faceManager == null)
            {
                Debug.LogWarning("[ARKitFaceProvider] ARFaceManager not found in scene. " +
                    "Add it to enable native face tracking.");
            }
            else
            {
                _faceManager.facesChanged += OnFacesChanged;
                Debug.Log("[ARKitFaceProvider] Initialized - 52 blendshape face tracking enabled");
            }

            _initialized = true;
        }

        public void Update()
        {
            if (!IsAvailable) return;

            // Data is updated via event callback
            bool isTracking = _cachedData.IsTracked;

            if (isTracking && !_wasTracking)
            {
                OnTrackingFound?.Invoke();
            }
            else if (!isTracking && _wasTracking)
            {
                OnTrackingLost?.Invoke();
            }

            _wasTracking = isTracking;
        }

        private void OnFacesChanged(ARFacesChangedEventArgs args)
        {
            // Process first detected face
            if (args.added.Count > 0)
            {
                ProcessFace(args.added[0]);
            }
            else if (args.updated.Count > 0)
            {
                ProcessFace(args.updated[0]);
            }
            else if (args.removed.Count > 0)
            {
                _cachedData.IsTracked = false;
            }
        }

        private void ProcessFace(ARFace face)
        {
            if (face == null) return;

            // Extract blendshapes
#if UNITY_IOS && !UNITY_EDITOR
            using (var blendShapeCoefficients = face.GetBlendShapeCoefficients(Unity.Collections.Allocator.Temp))
            {
                for (int i = 0; i < Mathf.Min(blendShapeCoefficients.Length, 52); i++)
                {
                    var coeff = blendShapeCoefficients[i];
                    int index = MapARKitBlendShapeToIndex(coeff.blendShapeLocation);
                    if (index >= 0 && index < 52)
                    {
                        _blendshapes[index] = coeff.coefficient;
                    }
                }
            }
#else
            // Editor fallback - simulate some expression
            SimulateBlendshapes();
#endif

            _cachedData.Blendshapes = _blendshapes;

            // Extract mesh
            if (face.vertices.Length > 0)
            {
                _cachedData.Vertices = face.vertices.ToArray();
                _cachedData.Indices = face.indices.ToArray();
            }

            // Transform
            _cachedData.Position = face.transform.position;
            _cachedData.Rotation = face.transform.rotation;
            _cachedData.IsTracked = true;
            _cachedData.Timestamp = Time.time;
        }

#if UNITY_IOS && !UNITY_EDITOR
        private int MapARKitBlendShapeToIndex(UnityEngine.XR.ARKit.ARKitBlendShapeLocation location)
        {
            // Map ARKit blendshape locations to array indices
            return location switch
            {
                // Eyes
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeBlinkLeft => 0,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeBlinkRight => 1,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeLookDownLeft => 2,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeLookDownRight => 3,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeLookInLeft => 4,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeLookInRight => 5,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeLookOutLeft => 6,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeLookOutRight => 7,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeLookUpLeft => 8,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeLookUpRight => 9,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeSquintLeft => 10,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeSquintRight => 11,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeWideLeft => 12,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.EyeWideRight => 13,
                // Brows
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.BrowDownLeft => 14,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.BrowDownRight => 15,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.BrowInnerUp => 16,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.BrowOuterUpLeft => 17,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.BrowOuterUpRight => 18,
                // Jaw
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.JawForward => 19,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.JawLeft => 20,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.JawRight => 21,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.JawOpen => 22,
                // Mouth
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthClose => 23,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthFunnel => 24,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthPucker => 25,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthLeft => 26,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthRight => 27,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthSmileLeft => 28,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthSmileRight => 29,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthFrownLeft => 30,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthFrownRight => 31,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthDimpleLeft => 32,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthDimpleRight => 33,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthStretchLeft => 34,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthStretchRight => 35,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthRollLower => 36,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthRollUpper => 37,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthShrugLower => 38,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthShrugUpper => 39,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthPressLeft => 40,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthPressRight => 41,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthLowerDownLeft => 42,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthLowerDownRight => 43,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthUpperUpLeft => 44,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.MouthUpperUpRight => 45,
                // Cheeks & Nose
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.CheekPuff => 46,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.CheekSquintLeft => 47,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.CheekSquintRight => 48,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.NoseSneerLeft => 49,
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.NoseSneerRight => 50,
                // Tongue
                UnityEngine.XR.ARKit.ARKitBlendShapeLocation.TongueOut => 51,
                _ => -1
            };
        }
#endif

        private void SimulateBlendshapes()
        {
            // Subtle idle animation for Editor testing
            float time = Time.time;

            // Simulate subtle blinks
            float blinkPhase = Mathf.Sin(time * 0.3f);
            if (blinkPhase > 0.95f)
            {
                _blendshapes[0] = _blendshapes[1] = Mathf.InverseLerp(0.95f, 1f, blinkPhase);
            }
            else
            {
                _blendshapes[0] = _blendshapes[1] = 0f;
            }

            // Subtle smile variation
            _blendshapes[28] = _blendshapes[29] = 0.1f + Mathf.Sin(time * 0.5f) * 0.05f;
        }

        public void Shutdown()
        {
            if (_faceManager != null)
            {
                _faceManager.facesChanged -= OnFacesChanged;
            }
            _initialized = false;
        }

        public bool TryGetData<T>(out T data) where T : struct, ITrackingData
        {
            if (typeof(T) == typeof(FaceData) && _cachedData.IsTracked)
            {
                data = (T)(object)_cachedData;
                return true;
            }
            data = default;
            return false;
        }

        /// <summary>
        /// Get a specific blendshape value by index (0-51).
        /// </summary>
        public float GetBlendshape(int index)
        {
            if (index < 0 || index >= 52) return 0f;
            return _blendshapes[index];
        }

        /// <summary>
        /// Get blendshape value by name (ARKit naming convention).
        /// </summary>
        public float GetBlendshape(string name)
        {
            int index = GetBlendshapeIndex(name);
            return index >= 0 ? _blendshapes[index] : 0f;
        }

        /// <summary>
        /// Get blendshape index by name.
        /// </summary>
        public static int GetBlendshapeIndex(string name)
        {
            return name.ToLower() switch
            {
                "eyeblinkleft" => 0,
                "eyeblinkright" => 1,
                "eyelookdownleft" => 2,
                "eyelookdownright" => 3,
                "eyelookinleft" => 4,
                "eyelookinright" => 5,
                "eyelookoutleft" => 6,
                "eyelookoutright" => 7,
                "eyelookupleft" => 8,
                "eyelookupright" => 9,
                "eyesquintleft" => 10,
                "eyesquintright" => 11,
                "eyewideleft" => 12,
                "eyewideright" => 13,
                "browdownleft" => 14,
                "browdownright" => 15,
                "browinnerup" => 16,
                "browouterupleft" => 17,
                "browouterupright" => 18,
                "jawforward" => 19,
                "jawleft" => 20,
                "jawright" => 21,
                "jawopen" => 22,
                "mouthclose" => 23,
                "mouthfunnel" => 24,
                "mouthpucker" => 25,
                "mouthleft" => 26,
                "mouthright" => 27,
                "mouthsmileleft" => 28,
                "mouthsmileright" => 29,
                "mouthfrownleft" => 30,
                "mouthfrownright" => 31,
                "mouthdimpleleft" => 32,
                "mouthdimpleright" => 33,
                "mouthstretchleft" => 34,
                "mouthstretchright" => 35,
                "mouthrolllower" => 36,
                "mouthrollupper" => 37,
                "mouthshruglower" => 38,
                "mouthshrugupper" => 39,
                "mouthpressleft" => 40,
                "mouthpressright" => 41,
                "mouthlowerdownleft" => 42,
                "mouthlowerdownright" => 43,
                "mouthupperupleft" => 44,
                "mouthupperupright" => 45,
                "cheekpuff" => 46,
                "cheeksquintleft" => 47,
                "cheeksquintright" => 48,
                "nosesneerleft" => 49,
                "nosesneerright" => 50,
                "tongueout" => 51,
                _ => -1
            };
        }

        /// <summary>
        /// Get all blendshape names for reference.
        /// </summary>
        public static string[] GetBlendshapeNames()
        {
            return new string[]
            {
                "EyeBlinkLeft", "EyeBlinkRight", "EyeLookDownLeft", "EyeLookDownRight",
                "EyeLookInLeft", "EyeLookInRight", "EyeLookOutLeft", "EyeLookOutRight",
                "EyeLookUpLeft", "EyeLookUpRight", "EyeSquintLeft", "EyeSquintRight",
                "EyeWideLeft", "EyeWideRight", "BrowDownLeft", "BrowDownRight",
                "BrowInnerUp", "BrowOuterUpLeft", "BrowOuterUpRight", "JawForward",
                "JawLeft", "JawRight", "JawOpen", "MouthClose",
                "MouthFunnel", "MouthPucker", "MouthLeft", "MouthRight",
                "MouthSmileLeft", "MouthSmileRight", "MouthFrownLeft", "MouthFrownRight",
                "MouthDimpleLeft", "MouthDimpleRight", "MouthStretchLeft", "MouthStretchRight",
                "MouthRollLower", "MouthRollUpper", "MouthShrugLower", "MouthShrugUpper",
                "MouthPressLeft", "MouthPressRight", "MouthLowerDownLeft", "MouthLowerDownRight",
                "MouthUpperUpLeft", "MouthUpperUpRight", "CheekPuff", "CheekSquintLeft",
                "CheekSquintRight", "NoseSneerLeft", "NoseSneerRight", "TongueOut"
            };
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
