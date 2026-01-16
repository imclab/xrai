// ARKit Hand Tracking - Fallback when HoloKit is not available
// Uses Unity.XR.Hands for cross-platform hand tracking
// Works with ARKit hand tracking in AR Foundation 6+

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

#if UNITY_XR_HANDS
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
#endif

namespace MetavidoVFX.HandTracking
{
    /// <summary>
    /// ARKit hand tracking without HoloKit dependency.
    /// Uses XR Hands subsystem when available, fallback to touch/gesture.
    /// </summary>
    public class ARKitHandTracking : MonoBehaviour
    {
        [Header("Hand Transforms")]
        [SerializeField] private Transform leftHandRoot;
        [SerializeField] private Transform rightHandRoot;
        [SerializeField] private bool createHandProxies = true;

        [Header("VFX")]
        [SerializeField] private VisualEffect leftHandVFX;
        [SerializeField] private VisualEffect rightHandVFX;

        [Header("Gesture Settings")]
        [SerializeField] private float pinchThreshold = 0.02f;
        [SerializeField] private float gestureSmoothing = 10f;

        // Hand state
        private bool _leftHandTracked;
        private bool _rightHandTracked;
        private Vector3 _leftWristPos;
        private Vector3 _rightWristPos;
        private float _leftPinchDistance = 1f;
        private float _rightPinchDistance = 1f;
        private bool _leftPinching;
        private bool _rightPinching;

        // Velocity tracking
        private Vector3 _leftPrevPos;
        private Vector3 _rightPrevPos;
        private Vector3 _leftVelocity;
        private Vector3 _rightVelocity;

#if UNITY_XR_HANDS
        private XRHandSubsystem _handSubsystem;
#endif

        public bool LeftHandTracked => _leftHandTracked;
        public bool RightHandTracked => _rightHandTracked;
        public Vector3 LeftWristPosition => _leftWristPos;
        public Vector3 RightWristPosition => _rightWristPos;
        public bool LeftPinching => _leftPinching;
        public bool RightPinching => _rightPinching;
        public Vector3 LeftVelocity => _leftVelocity;
        public Vector3 RightVelocity => _rightVelocity;

        void Start()
        {
            if (createHandProxies)
            {
                CreateHandProxies();
            }

#if UNITY_XR_HANDS
            TryInitializeHandSubsystem();
#else
            Debug.LogWarning("[ARKitHandTracking] XR Hands package not installed. Using touch fallback.");
#endif
        }

        void CreateHandProxies()
        {
            if (leftHandRoot == null)
            {
                var leftGO = new GameObject("LeftHand_Proxy");
                leftHandRoot = leftGO.transform;
                leftHandRoot.SetParent(transform);
            }

            if (rightHandRoot == null)
            {
                var rightGO = new GameObject("RightHand_Proxy");
                rightHandRoot = rightGO.transform;
                rightHandRoot.SetParent(transform);
            }
        }

#if UNITY_XR_HANDS
        void TryInitializeHandSubsystem()
        {
            var xrManager = XRGeneralSettings.Instance?.Manager;
            if (xrManager == null || !xrManager.isInitializationComplete)
            {
                // Try again later
                Invoke(nameof(TryInitializeHandSubsystem), 0.5f);
                return;
            }

            var handSubsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(handSubsystems);

            if (handSubsystems.Count > 0)
            {
                _handSubsystem = handSubsystems[0];
                _handSubsystem.updatedHands += OnHandsUpdated;
                Debug.Log("[ARKitHandTracking] XR Hands subsystem initialized");
            }
            else
            {
                Debug.LogWarning("[ARKitHandTracking] No XR Hands subsystem found");
            }
        }

        void OnHandsUpdated(XRHandSubsystem subsystem,
                           XRHandSubsystem.UpdateSuccessFlags updateFlags,
                           XRHandSubsystem.UpdateType updateType)
        {
            // Process left hand
            if ((updateFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != 0)
            {
                ProcessHand(subsystem.leftHand, true);
            }
            else
            {
                _leftHandTracked = false;
            }

            // Process right hand
            if ((updateFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != 0)
            {
                ProcessHand(subsystem.rightHand, false);
            }
            else
            {
                _rightHandTracked = false;
            }
        }

        void ProcessHand(XRHand hand, bool isLeft)
        {
            if (!hand.isTracked)
            {
                if (isLeft) _leftHandTracked = false;
                else _rightHandTracked = false;
                return;
            }

            // Get wrist position
            if (hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wristPose))
            {
                if (isLeft)
                {
                    _leftHandTracked = true;
                    _leftWristPos = wristPose.position;
                    if (leftHandRoot != null)
                    {
                        leftHandRoot.position = wristPose.position;
                        leftHandRoot.rotation = wristPose.rotation;
                    }
                }
                else
                {
                    _rightHandTracked = true;
                    _rightWristPos = wristPose.position;
                    if (rightHandRoot != null)
                    {
                        rightHandRoot.position = wristPose.position;
                        rightHandRoot.rotation = wristPose.rotation;
                    }
                }
            }

            // Calculate pinch distance
            if (hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbPose) &&
                hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
            {
                float distance = Vector3.Distance(thumbPose.position, indexPose.position);

                if (isLeft)
                {
                    _leftPinchDistance = distance;
                    bool wasPinching = _leftPinching;
                    _leftPinching = distance < pinchThreshold;

                    if (_leftPinching && !wasPinching)
                        OnPinchStart(true);
                    else if (!_leftPinching && wasPinching)
                        OnPinchEnd(true);
                }
                else
                {
                    _rightPinchDistance = distance;
                    bool wasPinching = _rightPinching;
                    _rightPinching = distance < pinchThreshold;

                    if (_rightPinching && !wasPinching)
                        OnPinchStart(false);
                    else if (!_rightPinching && wasPinching)
                        OnPinchEnd(false);
                }
            }
        }
#endif

        void Update()
        {
            UpdateVelocity();
            UpdateVFX();

#if !UNITY_XR_HANDS
            // Fallback: use touch input to simulate hand
            ProcessTouchInput();
#endif
        }

        void UpdateVelocity()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;

            if (_leftHandTracked && leftHandRoot != null)
            {
                _leftVelocity = (leftHandRoot.position - _leftPrevPos) / dt;
                _leftPrevPos = leftHandRoot.position;
            }

            if (_rightHandTracked && rightHandRoot != null)
            {
                _rightVelocity = (rightHandRoot.position - _rightPrevPos) / dt;
                _rightPrevPos = rightHandRoot.position;
            }
        }

        void UpdateVFX()
        {
            if (leftHandVFX != null && _leftHandTracked)
            {
                PushHandData(leftHandVFX, leftHandRoot, _leftVelocity, _leftPinchDistance, _leftPinching);
            }

            if (rightHandVFX != null && _rightHandTracked)
            {
                PushHandData(rightHandVFX, rightHandRoot, _rightVelocity, _rightPinchDistance, _rightPinching);
            }
        }

        void PushHandData(VisualEffect vfx, Transform hand, Vector3 velocity, float pinchDist, bool isPinching)
        {
            if (vfx == null || hand == null) return;

            if (vfx.HasVector3("HandPosition"))
                vfx.SetVector3("HandPosition", hand.position);

            if (vfx.HasVector3("HandVelocity"))
                vfx.SetVector3("HandVelocity", velocity);

            if (vfx.HasFloat("HandSpeed"))
                vfx.SetFloat("HandSpeed", velocity.magnitude);

            if (vfx.HasFloat("PinchDistance"))
                vfx.SetFloat("PinchDistance", pinchDist);

            if (vfx.HasBool("IsPinching"))
                vfx.SetBool("IsPinching", isPinching);
        }

        void ProcessTouchInput()
        {
            // Fallback: Use touch position as "hand" position
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                var cam = Camera.main;
                if (cam == null) return;

                // Project touch to world at 0.5m depth
                Vector3 touchWorld = cam.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 0.5f));

                _rightHandTracked = true;
                _rightWristPos = touchWorld;

                if (rightHandRoot != null)
                {
                    rightHandRoot.position = Vector3.Lerp(rightHandRoot.position, touchWorld, Time.deltaTime * gestureSmoothing);
                }

                // Two-finger pinch simulation
                if (Input.touchCount >= 2)
                {
                    var touch2 = Input.GetTouch(1);
                    _rightPinchDistance = Vector2.Distance(touch.position, touch2.position) / Screen.dpi * 0.0254f;
                    _rightPinching = _rightPinchDistance < 0.05f;
                }
            }
            else
            {
                _rightHandTracked = false;
            }
        }

        void OnPinchStart(bool isLeft)
        {
            var vfx = isLeft ? leftHandVFX : rightHandVFX;
            if (vfx != null)
            {
                vfx.SendEvent("OnPinchStart");
            }
            Debug.Log($"[ARKitHandTracking] {(isLeft ? "Left" : "Right")} pinch start");
        }

        void OnPinchEnd(bool isLeft)
        {
            var vfx = isLeft ? leftHandVFX : rightHandVFX;
            if (vfx != null)
            {
                vfx.SendEvent("OnPinchEnd");
            }
        }

        void OnDestroy()
        {
#if UNITY_XR_HANDS
            if (_handSubsystem != null)
            {
                _handSubsystem.updatedHands -= OnHandsUpdated;
            }
#endif
        }
    }
}
