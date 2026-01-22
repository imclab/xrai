// Hand-Driven VFX Controller
// Attaches VFX to hands with velocity, audio, and gesture control
// Supports pinch-based parameter control and physics collision

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;
using MetavidoVFX.Segmentation;

#if HOLOKIT_AVAILABLE
using HoloKit.iOS;
#endif

namespace MetavidoVFX.HandTracking
{
    /// <summary>
    /// Controls VFX effects attached to hands with support for:
    /// - Velocity-driven particle behavior
    /// - Audio-reactive parameters
    /// - Gesture-based VFX switching and parameter control
    /// - Physics collision with AR mesh
    /// </summary>
    public class HandVFXController : MonoBehaviour
    {
        void Log(string msg)
        {
            if (suppressLogs) return;
            Debug.Log(msg);
        }

        [Header("Hand Tracking")]
        [SerializeField] private Transform leftHandRoot;
        [SerializeField] private Transform rightHandRoot;
        [SerializeField] private bool useHoloKitHandTracking = true;

        [Header("VFX")]
        [SerializeField] private VisualEffect leftHandVFX;
        [SerializeField] private VisualEffect rightHandVFX;
        [SerializeField] private VisualEffectAsset[] availableVFXAssets;

        [Header("Audio")]
        [SerializeField] private AudioBridge audioBridge;
        [SerializeField] private AudioProcessor audioProcessor;
        [SerializeField] private Audio.EnhancedAudioProcessor enhancedAudioProcessor;
        [SerializeField] private bool audioReactive = true;

        [Header("Debug")]
        [SerializeField] private bool suppressLogs = false;

        [Header("Gesture Thresholds")]
        [SerializeField] private float pinchStartDistance = 0.02f;
        [SerializeField] private float pinchEndDistance = 0.03f;
        [SerializeField] private float grabStartDistance = 0.04f;
        [SerializeField] private float grabEndDistance = 0.06f;

        [Header("BodyPix Fallback")]
        [Tooltip("Use BodyPartSegmenter wrist keypoints when HoloKit unavailable")]
        [SerializeField] private bool useBodyPixFallback = true;
        [SerializeField] private BodyPartSegmenter bodyPartSegmenter;
        [Tooltip("Minimum confidence for keypoint tracking (0-1)")]
        [SerializeField] private float keypointMinConfidence = 0.5f;

        [Header("Physics")]
        [SerializeField] private bool enablePhysicsCollision = true;
        [SerializeField] private ARMeshManager arMeshManager;
        [SerializeField] private float collisionBounciness = 0.5f;

        [Header("VFX Parameter Mapping")]
        [SerializeField] private float velocityToTrailLength = 0.5f;
        [SerializeField] private float pinchToBrushWidth = 0.1f;
        [SerializeField] private float audioToEmissionRate = 100f;

        [Header("Runtime Status (Read-Only)")]
        [SerializeField, Tooltip("Left hand tracking active")]
        private bool _leftHandActiveDisplay = false;
        [SerializeField, Tooltip("Right hand tracking active")]
        private bool _rightHandActiveDisplay = false;
        [SerializeField, Tooltip("Left hand velocity magnitude")]
        private float _leftHandSpeedDisplay = 0f;
        [SerializeField, Tooltip("Right hand velocity magnitude")]
        private float _rightHandSpeedDisplay = 0f;
        [SerializeField, Tooltip("Left hand pinching")]
        private bool _leftPinchingDisplay = false;
        [SerializeField, Tooltip("Right hand pinching")]
        private bool _rightPinchingDisplay = false;
        [SerializeField, Tooltip("Current tracking source")]
        private string _trackingSourceDisplay = "None";

        // Runtime state
        private Vector3 leftHandPrevPos;
        private Vector3 rightHandPrevPos;
        private Vector3 leftHandVelocity;
        private Vector3 rightHandVelocity;

        private bool leftPinching;
        private bool rightPinching;
        private bool leftGrabbing;
        private bool rightGrabbing;

        private float leftPinchDistance;
        private float rightPinchDistance;

        private int currentVFXIndex = 0;

#if HOLOKIT_AVAILABLE
        private HandTrackingManager handTrackingManager;
        private HandGestureRecognitionManager gestureManager;
#endif

        void Start()
        {
#if HOLOKIT_AVAILABLE
            if (useHoloKitHandTracking)
            {
                handTrackingManager = FindFirstObjectByType<HandTrackingManager>();
                gestureManager = FindFirstObjectByType<HandGestureRecognitionManager>();

                if (gestureManager != null)
                {
                    gestureManager.OnHandGestureChanged += OnHandGestureChanged;
                }
            }
#endif
            // Find audio sources if not assigned (AudioBridge preferred)
            if (audioBridge == null)
            {
                audioBridge = AudioBridge.Instance;
                if (audioBridge == null)
                {
                    audioBridge = FindFirstObjectByType<AudioBridge>();
                }
            }

            // Find audio processors if not assigned (legacy fallback)
            if (enhancedAudioProcessor == null)
            {
                enhancedAudioProcessor = FindFirstObjectByType<Audio.EnhancedAudioProcessor>();
            }
            if (audioProcessor == null)
            {
                audioProcessor = FindFirstObjectByType<AudioProcessor>();
            }

            // Find BodyPartSegmenter for fallback hand tracking
            if (useBodyPixFallback && bodyPartSegmenter == null)
            {
                bodyPartSegmenter = FindFirstObjectByType<BodyPartSegmenter>();
            }

            // Initialize hand positions
            if (leftHandRoot != null) leftHandPrevPos = leftHandRoot.position;
            if (rightHandRoot != null) rightHandPrevPos = rightHandRoot.position;
        }

        void Update()
        {
            UpdateHandTracking();
            UpdateVelocity();
            UpdateGestures();
            PushVFXParameters();
            UpdateRuntimeStatus();
        }

        void UpdateRuntimeStatus()
        {
            _leftHandActiveDisplay = leftHandRoot != null && leftHandRoot.gameObject.activeInHierarchy;
            _rightHandActiveDisplay = rightHandRoot != null && rightHandRoot.gameObject.activeInHierarchy;
            _leftHandSpeedDisplay = leftHandVelocity.magnitude;
            _rightHandSpeedDisplay = rightHandVelocity.magnitude;
            _leftPinchingDisplay = leftPinching;
            _rightPinchingDisplay = rightPinching;

            // Determine tracking source
            #if HOLOKIT_AVAILABLE && !UNITY_EDITOR
            if (useHoloKitHandTracking && handTrackingManager != null && handTrackingManager.HandCount > 0)
                _trackingSourceDisplay = "HoloKit";
            else
            #endif
            if (useBodyPixFallback && bodyPartSegmenter != null && bodyPartSegmenter.IsReady)
                _trackingSourceDisplay = "BodyPix";
            else
                _trackingSourceDisplay = "None";
        }

        void UpdateHandTracking()
        {
            bool holoKitHandled = false;

#if HOLOKIT_AVAILABLE && !UNITY_EDITOR
            if (useHoloKitHandTracking && handTrackingManager != null)
            {
                // Get hand positions from HoloKit (only on device - native plugin required)
                if (handTrackingManager.HandCount > 0)
                {
                    // Use wrist as hand root
                    Vector3 wristPos = handTrackingManager.GetHandJointPosition(0, JointName.Wrist);
                    if (leftHandRoot != null)
                    {
                        leftHandRoot.position = wristPos;
                    }

                    // Calculate pinch distance
                    Vector3 thumbTip = handTrackingManager.GetHandJointPosition(0, JointName.ThumbTip);
                    Vector3 indexTip = handTrackingManager.GetHandJointPosition(0, JointName.IndexTip);
                    leftPinchDistance = Vector3.Distance(thumbTip, indexTip);
                    holoKitHandled = true;
                }

                if (handTrackingManager.HandCount > 1)
                {
                    Vector3 wristPos = handTrackingManager.GetHandJointPosition(1, JointName.Wrist);
                    if (rightHandRoot != null)
                    {
                        rightHandRoot.position = wristPos;
                    }

                    Vector3 thumbTip = handTrackingManager.GetHandJointPosition(1, JointName.ThumbTip);
                    Vector3 indexTip = handTrackingManager.GetHandJointPosition(1, JointName.IndexTip);
                    rightPinchDistance = Vector3.Distance(thumbTip, indexTip);
                }
            }
#endif

            // BodyPix fallback when HoloKit isn't available or didn't find hands
            if (!holoKitHandled && useBodyPixFallback && bodyPartSegmenter != null && bodyPartSegmenter.IsReady)
            {
                UpdateHandTrackingFromBodyPix();
            }
        }

        void UpdateHandTrackingFromBodyPix()
        {
            // LeftWrist = keypoint index 9, RightWrist = keypoint index 10
            float leftScore = bodyPartSegmenter.GetKeypointScore(KeypointIndex.LeftWrist);
            float rightScore = bodyPartSegmenter.GetKeypointScore(KeypointIndex.RightWrist);

            if (leftScore >= keypointMinConfidence)
            {
                Vector3 screenPos = bodyPartSegmenter.GetKeypointPosition(KeypointIndex.LeftWrist);
                // Convert screen position to world position (simplified - at fixed depth)
                if (leftHandRoot != null && Camera.main != null)
                {
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 1f));
                    leftHandRoot.position = worldPos;
                }
            }

            if (rightScore >= keypointMinConfidence)
            {
                Vector3 screenPos = bodyPartSegmenter.GetKeypointPosition(KeypointIndex.RightWrist);
                if (rightHandRoot != null && Camera.main != null)
                {
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 1f));
                    rightHandRoot.position = worldPos;
                }
            }

            // BodyPix doesn't provide finger positions for pinch detection
            // Use fixed large pinch distance (no pinch detected)
            leftPinchDistance = 0.1f;
            rightPinchDistance = 0.1f;
        }

        void UpdateVelocity()
        {
            if (leftHandRoot != null)
            {
                leftHandVelocity = (leftHandRoot.position - leftHandPrevPos) / Time.deltaTime;
                leftHandPrevPos = leftHandRoot.position;
            }

            if (rightHandRoot != null)
            {
                rightHandVelocity = (rightHandRoot.position - rightHandPrevPos) / Time.deltaTime;
                rightHandPrevPos = rightHandRoot.position;
            }
        }

        void UpdateGestures()
        {
            // Pinch detection with hysteresis
            if (!leftPinching && leftPinchDistance < pinchStartDistance)
            {
                leftPinching = true;
                OnPinchStart(true);
            }
            else if (leftPinching && leftPinchDistance > pinchEndDistance)
            {
                leftPinching = false;
                OnPinchEnd(true);
            }

            if (!rightPinching && rightPinchDistance < pinchStartDistance)
            {
                rightPinching = true;
                OnPinchStart(false);
            }
            else if (rightPinching && rightPinchDistance > pinchEndDistance)
            {
                rightPinching = false;
                OnPinchEnd(false);
            }
        }

        void PushVFXParameters()
        {
            // Left hand VFX
            if (leftHandVFX != null)
            {
                PushHandParameters(leftHandVFX, leftHandRoot, leftHandVelocity, leftPinchDistance, leftPinching);
            }

            // Right hand VFX
            if (rightHandVFX != null)
            {
                PushHandParameters(rightHandVFX, rightHandRoot, rightHandVelocity, rightPinchDistance, rightPinching);
            }
        }

        void PushHandParameters(VisualEffect vfx, Transform handRoot, Vector3 velocity, float pinchDist, bool isPinching)
        {
            if (vfx == null || handRoot == null) return;

            // Position
            if (vfx.HasVector3("HandPosition"))
                vfx.SetVector3("HandPosition", handRoot.position);

            // Velocity
            if (vfx.HasVector3("HandVelocity"))
                vfx.SetVector3("HandVelocity", velocity);

            float speed = velocity.magnitude;
            if (vfx.HasFloat("HandSpeed"))
                vfx.SetFloat("HandSpeed", speed);

            // Trail length based on velocity
            if (vfx.HasFloat("TrailLength"))
                vfx.SetFloat("TrailLength", speed * velocityToTrailLength);

            // Pinch-controlled brush width
            float brushWidth = Mathf.Lerp(0.01f, 0.5f, Mathf.InverseLerp(pinchStartDistance, 0.15f, pinchDist));
            if (vfx.HasFloat("BrushWidth"))
                vfx.SetFloat("BrushWidth", brushWidth * pinchToBrushWidth);

            // Pinch state
            if (vfx.HasBool("IsPinching"))
                vfx.SetBool("IsPinching", isPinching);

            // Audio-reactive parameters - prefer AudioBridge, then EnhancedAudioProcessor
            if (audioReactive)
            {
                if (audioBridge != null)
                {
                    if (vfx.HasFloat("AudioVolume"))
                        vfx.SetFloat("AudioVolume", audioBridge.Volume);

                    if (vfx.HasFloat("AudioBass"))
                        vfx.SetFloat("AudioBass", audioBridge.Bass);

                    if (vfx.HasFloat("AudioMid"))
                        vfx.SetFloat("AudioMid", audioBridge.Mids);

                    if (vfx.HasFloat("AudioTreble"))
                        vfx.SetFloat("AudioTreble", audioBridge.Treble);

                    if (vfx.HasFloat("AudioSubBass"))
                        vfx.SetFloat("AudioSubBass", audioBridge.SubBass);

                    if (vfx.HasFloat("BeatPulse"))
                        vfx.SetFloat("BeatPulse", audioBridge.BeatPulse);

                    if (vfx.HasFloat("BeatIntensity"))
                        vfx.SetFloat("BeatIntensity", audioBridge.BeatIntensity);

                    if (vfx.HasFloat("EmissionRate"))
                        vfx.SetFloat("EmissionRate", audioBridge.Bass * audioToEmissionRate);
                }
                else if (enhancedAudioProcessor != null)
                {
                    // Use enhanced audio with bass/mid/treble
                    enhancedAudioProcessor.PushToVFX(vfx);

                    // Emission rate modulated by bass for punchy response
                    if (vfx.HasFloat("EmissionRate"))
                        vfx.SetFloat("EmissionRate", enhancedAudioProcessor.AudioBass * audioToEmissionRate);
                }
                else if (audioProcessor != null)
                {
                    // Fallback to basic audio
                    if (vfx.HasFloat("AudioVolume"))
                        vfx.SetFloat("AudioVolume", audioProcessor.AudioVolume);

                    if (vfx.HasFloat("AudioPitch"))
                        vfx.SetFloat("AudioPitch", audioProcessor.AudioPitch);

                    if (vfx.HasFloat("EmissionRate"))
                        vfx.SetFloat("EmissionRate", audioProcessor.AudioVolume * audioToEmissionRate);
                }
            }

            // Physics collision plane (simplified - uses floor plane)
            if (enablePhysicsCollision)
            {
                if (vfx.HasVector3("CollisionPlanePosition"))
                    vfx.SetVector3("CollisionPlanePosition", Vector3.zero);

                if (vfx.HasVector3("CollisionPlaneNormal"))
                    vfx.SetVector3("CollisionPlaneNormal", Vector3.up);

                if (vfx.HasFloat("CollisionBounciness"))
                    vfx.SetFloat("CollisionBounciness", collisionBounciness);
            }
        }

#if HOLOKIT_AVAILABLE
        void OnHandGestureChanged(HandGesture gesture)
        {
            switch (gesture)
            {
                case HandGesture.Pinched:
                    // Already handled in UpdateGestures
                    break;

                case HandGesture.Five:
                    // Open palm - switch to next VFX
                    CycleVFX();
                    break;
            }
        }
#endif

        void OnPinchStart(bool isLeftHand)
        {
            VisualEffect vfx = isLeftHand ? leftHandVFX : rightHandVFX;
            if (vfx != null)
            {
                vfx.SendEvent("OnPinchStart");
            }
            Log($"[HandVFX] {(isLeftHand ? "Left" : "Right")} pinch started");
        }

        void OnPinchEnd(bool isLeftHand)
        {
            VisualEffect vfx = isLeftHand ? leftHandVFX : rightHandVFX;
            if (vfx != null)
            {
                vfx.SendEvent("OnPinchEnd");
            }
            Log($"[HandVFX] {(isLeftHand ? "Left" : "Right")} pinch ended");
        }

        /// <summary>
        /// Switch to next VFX in the available list
        /// </summary>
        public void CycleVFX()
        {
            if (availableVFXAssets == null || availableVFXAssets.Length == 0) return;

            currentVFXIndex = (currentVFXIndex + 1) % availableVFXAssets.Length;
            SetVFX(availableVFXAssets[currentVFXIndex]);
        }

        /// <summary>
        /// Set VFX for both hands
        /// </summary>
        public void SetVFX(VisualEffectAsset asset)
        {
            if (leftHandVFX != null)
            {
                leftHandVFX.visualEffectAsset = asset;
                leftHandVFX.Reinit();
            }

            if (rightHandVFX != null)
            {
                rightHandVFX.visualEffectAsset = asset;
                rightHandVFX.Reinit();
            }

            Log($"[HandVFX] Switched to: {asset?.name}");
        }

        /// <summary>
        /// Set VFX by index
        /// </summary>
        public void SetVFXByIndex(int index)
        {
            if (availableVFXAssets == null || index < 0 || index >= availableVFXAssets.Length) return;

            currentVFXIndex = index;
            SetVFX(availableVFXAssets[index]);
        }

        void OnDestroy()
        {
#if HOLOKIT_AVAILABLE
            if (gestureManager != null)
            {
                gestureManager.OnHandGestureChanged -= OnHandGestureChanged;
            }
#endif
        }
    }
}
