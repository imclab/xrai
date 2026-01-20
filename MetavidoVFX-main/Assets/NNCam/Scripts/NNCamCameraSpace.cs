// NNCam Camera Space Helper
// Positions NNCam VFX in camera space so UV coordinates (0-1) map to visible screen
// The VFX is scaled and positioned to create a virtual screen in front of the camera

using UnityEngine;

namespace MetavidoVFX.NNCam
{
    /// <summary>
    /// Positions NNCam VFX in camera space so that UV-based particle positions
    /// appear at correct screen locations. The VFX is positioned as a virtual
    /// screen in front of the camera.
    /// </summary>
    [ExecuteAlways]
    public class NNCamCameraSpace : MonoBehaviour
    {
        [Header("Camera Reference")]
        [Tooltip("The AR camera to track. Auto-found if null.")]
        public Camera targetCamera;

        [Header("Virtual Screen Settings")]
        [Tooltip("Distance from camera to virtual screen (meters)")]
        [Range(0.1f, 5f)]
        public float screenDistance = 1f;

        [Tooltip("Height of virtual screen (meters). Width calculated from aspect.")]
        [Range(0.1f, 3f)]
        public float screenHeight = 1f;

        [Tooltip("Offset to center UV (0.5, 0.5) in screen center")]
        public bool centerUV = true;

        [Header("Debug")]
        public bool showDebugGizmos = false;

        void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        void LateUpdate()
        {
            if (targetCamera == null) return;

            // Position VFX at camera location, offset forward by screenDistance
            Vector3 camPos = targetCamera.transform.position;
            Vector3 camForward = targetCamera.transform.forward;
            Vector3 camRight = targetCamera.transform.right;
            Vector3 camUp = targetCamera.transform.up;

            // Calculate screen dimensions based on aspect ratio
            float aspect = targetCamera.aspect;
            float screenWidth = screenHeight * aspect;

            // Position the VFX origin
            // UV (0,0) should be at bottom-left of screen
            // UV (1,1) should be at top-right of screen

            Vector3 screenCenter = camPos + camForward * screenDistance;

            if (centerUV)
            {
                // Center the UV range so (0.5, 0.5) is at screen center
                // VFX origin becomes the bottom-left corner
                transform.position = screenCenter - camRight * (screenWidth * 0.5f) - camUp * (screenHeight * 0.5f);
            }
            else
            {
                // UV (0,0) at screen center
                transform.position = screenCenter;
            }

            // Orient to face camera (particles rendered on this virtual plane)
            transform.rotation = targetCamera.transform.rotation;

            // Scale so that UV 0-1 maps to screen dimensions
            // NNCam VFX output positions like (0.3, 0.7, -1)
            // We want x=0.3 to map to 0.3 * screenWidth meters to the right
            // y=0.7 to map to 0.7 * screenHeight meters up
            // z=-1 to map to -1 meter (into the screen, which is fine)
            transform.localScale = new Vector3(screenWidth, screenHeight, 1f);
        }

        void OnDrawGizmos()
        {
            if (!showDebugGizmos || targetCamera == null) return;

            float aspect = targetCamera.aspect;
            float screenWidth = screenHeight * aspect;

            Vector3 camPos = targetCamera.transform.position;
            Vector3 camForward = targetCamera.transform.forward;
            Vector3 camRight = targetCamera.transform.right;
            Vector3 camUp = targetCamera.transform.up;

            Vector3 screenCenter = camPos + camForward * screenDistance;
            Vector3 bottomLeft = screenCenter - camRight * (screenWidth * 0.5f) - camUp * (screenHeight * 0.5f);
            Vector3 bottomRight = screenCenter + camRight * (screenWidth * 0.5f) - camUp * (screenHeight * 0.5f);
            Vector3 topLeft = screenCenter - camRight * (screenWidth * 0.5f) + camUp * (screenHeight * 0.5f);
            Vector3 topRight = screenCenter + camRight * (screenWidth * 0.5f) + camUp * (screenHeight * 0.5f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);

            // Draw center cross
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(screenCenter - camRight * 0.1f, screenCenter + camRight * 0.1f);
            Gizmos.DrawLine(screenCenter - camUp * 0.1f, screenCenter + camUp * 0.1f);
        }
    }
}
