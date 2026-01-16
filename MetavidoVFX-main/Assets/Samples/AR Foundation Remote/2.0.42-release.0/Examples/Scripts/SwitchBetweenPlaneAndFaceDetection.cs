using UnityEngine;
using UnityEngine.XR.ARFoundation;


namespace ARFoundationRemoteExamples {
    public class SwitchBetweenPlaneAndFaceDetection : MonoBehaviour {
        #pragma warning disable 414
        [SerializeField] SetupARFoundationVersionSpecificComponents setupCameraManager = null;
        ARCameraManager ARCameraManager => setupCameraManager.cameraManager;
        [SerializeField] ARSession arSession = null;
        [SerializeField] ARPlaneManager planeManager = null;
        [SerializeField] ARFaceManager faceManager = null;
        [SerializeField] bool isPlaneTracking = true;
        [SerializeField] int fontSize = 35;
        #pragma warning restore 414


        void Awake() {
            updateTrackingMode();
        }

        void OnGUI() {
            var style = new GUIStyle(GUI.skin.button) {fontSize = fontSize};
            var width = 400;
            var height = 200;
            if (GUI.Button(new Rect(0, 0, width, height), $"{(isPlaneTracking ? "Plane Tracking" : "Face Tracking")}", style)) {
                isPlaneTracking = !isPlaneTracking;
                updateTrackingMode();
            }

            if (GUI.Button(new Rect(width, 0, width, height), $"AR Session: {(arSession.enabled ? "Running" : "Paused")}", style)) {
                if (arSession.enabled) {
                    arSession.enabled = false;
                    arSession.Reset();
                } else {
                    arSession.enabled = true;
                }
            }
        }

        void updateTrackingMode() {
            if (isPlaneTracking) {
                planeManager.enabled = true;
                faceManager.enabled = false;
                #if ARFOUNDATION_4_0_OR_NEWER
                ARCameraManager.requestedFacingDirection = CameraFacingDirection.World;
                arSession.requestedTrackingMode = TrackingMode.PositionAndRotation;
                #endif
            } else {
                planeManager.enabled = false;
                faceManager.enabled = true;
                #if ARFOUNDATION_4_0_OR_NEWER
                arSession.requestedTrackingMode = TrackingMode.RotationOnly;
                ARCameraManager.requestedFacingDirection = CameraFacingDirection.User;
                #endif
            }
        }
    }
}
