#if AR_FOUNDATION_5_0_OR_NEWER && INPUT_SYSTEM_INSTALLED && ENABLE_INPUT_SYSTEM
    #define ENABLE_NEW_INPUT_SYSTEM_CAMERA_TRACKING
#endif
#if ENABLE_NEW_INPUT_SYSTEM_CAMERA_TRACKING
    using ARPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;
    using UnityEngine.InputSystem;
#endif
#if AR_FOUNDATION_5_0_OR_NEWER
    using ARSessionOrigin = Unity.XR.CoreUtils.XROrigin;
#endif
#if !ARFOUNDATION_4_0_2_OR_NEWER
    using XRCpuImage = UnityEngine.XR.ARSubsystems.XRCameraImage;
#endif
#if ARFOUNDATION_4_0_OR_NEWER
    using System;
#endif
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace ARFoundationRemoteExamples {
    public class SetupARFoundationVersionSpecificComponents : MonoBehaviour {
        [SerializeField] ARSessionOrigin origin = null;
        [SerializeField] bool isUserFacing = false;
        [SerializeField] bool autofocus = false;
        [SerializeField] bool enableCameraManager = true;
        [SerializeField] bool addCameraBackground = true;
        [SerializeField] [CanBeNull] Material customCameraMaterial = null;
        #pragma warning disable 414
        [SerializeField] ARSession arSession = null;
        [SerializeField] TrackingModeWrapper trackingMode = TrackingModeWrapper.DontCare;
        #pragma warning restore
        [SerializeField] bool addPoseDriver = true;

        [CanBeNull] ARCameraManager _cameraManager = null;
        [CanBeNull] ARCameraBackground _cameraBackground = null;
        bool initialized;

        
        [NotNull]
        public ARCameraManager cameraManager {
            get {
                if (_cameraManager == null) {
                    init();
                }

                Assert.IsNotNull(_cameraManager);
                return _cameraManager;
            }
        }

        public ARCameraBackground cameraBackground {
            get {
                initIfNeeded();
                return _cameraBackground;
            }
        }

        void Awake() {
            initIfNeeded();
        }

        void initIfNeeded() {
            if (!initialized) {
                init();
            }
        }

        void init() {
            Assert.IsFalse(initialized);
            initialized = true;
            var cameraGameObject = origin.GetCamera().gameObject;
            cameraGameObject.SetActive(false);
            if (addPoseDriver) {
                var poseDriver = cameraGameObject.AddComponent<ARPoseDriver>();
                setupPoseDriverARF5(poseDriver);
                if (ExamplesUtils.AR_FOUNDATION_5_0_OR_NEWER && origin.gameObject.activeSelf) {
                    Debug.LogWarning($"Please disable session origin's gameobject. The SetupARFoundationVersionSpecificComponents.cs script will automatically add the ARPoseDriver/TrackedPoseDriver and will enable the session origin's gameobject.\n\n" +
                         "Explanation: in AR Foundation 5, XROrigin checks if TrackedPoseDriver is present on camera. If it's not, it displays a warning. To get rid of the warning, the SetupARFoundationVersionSpecificComponents.cs adds the pose driver before enabling the XROrigin.\n");
                }
                
                origin.gameObject.SetActive(true);
            }
            
            var camManager = cameraGameObject.AddComponent<ARCameraManager>();
            camManager.enabled = enableCameraManager;
            _cameraManager = camManager;
            if (addCameraBackground) {
                var bg = cameraGameObject.AddComponent<ARCameraBackground>();
                _cameraBackground = bg;
                if (customCameraMaterial != null) {
                    bg.useCustomMaterial = true;
                    bg.customMaterial = customCameraMaterial;
                }
            }

            if (isUserFacing) {
                #if ARFOUNDATION_4_0_OR_NEWER
                    camManager.requestedFacingDirection = CameraFacingDirection.User;
                #endif
            }
            
            camManager.SetCameraAutoFocus(autofocus);

            #if ARFOUNDATION_4_0_OR_NEWER
                TrackingMode toTrackingMode(TrackingModeWrapper mode) {
                    switch (mode) {
                        case TrackingModeWrapper.DontCare:
                            return TrackingMode.DontCare;
                        case TrackingModeWrapper.RotationOnly:
                            return TrackingMode.RotationOnly;
                        case TrackingModeWrapper.PositionAndRotation:
                            return TrackingMode.PositionAndRotation;
                        default:
                            throw new Exception();
                    }
                }
                
                if (trackingMode != TrackingModeWrapper.DontSetup) {
                    var requestedTrackingMode = toTrackingMode(trackingMode);
                    if (getSerializedTrackingMode(arSession) != requestedTrackingMode) {
                        Debug.LogWarning($"{nameof(SetupARFoundationVersionSpecificComponents)} setting requestedTrackingMode to {requestedTrackingMode}");
                        arSession.requestedTrackingMode = requestedTrackingMode;
                    }
                }
                
                TrackingMode getSerializedTrackingMode(ARSession s) {
                    var f = s.GetType().GetField("m_TrackingMode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (f == null) {
                        Debug.LogError("ARSession.m_TrackingMode field not found.");
                        return TrackingMode.DontCare;
                    }

                    return (TrackingMode) f.GetValue(s);
                }
            #endif
            
            cameraGameObject.SetActive(true);
            origin.gameObject.SetActive(true);
        }

        /// see UnityEditor.XR.ARFoundation.XROriginCreateUtil
        void setupPoseDriverARF5(ARPoseDriver poseDriver) {
            // ReSharper disable once UnusedParameter.Local
            #if ENABLE_NEW_INPUT_SYSTEM_CAMERA_TRACKING
                var positionAction = new InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
                positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");
                var rotationAction = new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
                rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
                poseDriver.positionInput = new InputActionProperty(positionAction);
                poseDriver.rotationInput = new InputActionProperty(rotationAction);
                #if INPUT_SYSTEM_1_5_0_OR_NEWER
                    poseDriver.ignoreTrackingState = true;
                #endif
            #endif
        }
    }


    internal static class ARCameraManagerExtensions {
        public static void SetCameraAutoFocus(this ARCameraManager cameraManager, bool auto) {
            #if ARFOUNDATION_4_0_OR_NEWER
            cameraManager.autoFocusRequested = auto;
            #else
            cameraManager.focusMode = auto ? UnityEngine.XR.ARSubsystems.CameraFocusMode.Auto : UnityEngine.XR.ARSubsystems.CameraFocusMode.Fixed;
            #endif
        }

        public static bool TryAcquireLatestCpuImageVersionAgnostic(this ARCameraManager cameraManager, out XRCpuImage cameraImage) {
            return cameraManager.
                #if ARFOUNDATION_4_0_2_OR_NEWER
                    TryAcquireLatestCpuImage
                #else 
                    TryGetLatestImage
                #endif
                    (out cameraImage);
        }
    }


    internal enum TrackingModeWrapper {
        DontCare,
        RotationOnly,
        PositionAndRotation,
        DontSetup
    }
}
