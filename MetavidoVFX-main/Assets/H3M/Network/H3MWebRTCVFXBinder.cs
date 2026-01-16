// H3MWebRTCVFXBinder - VFX Property Binder for WebRTC hologram streams
// Binds ColorMap, DepthMap, InverseView, and RayParams from remote peer
//
// Based on Rcam3-WebRTC WebRTCRcamBinder

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace MetavidoVFX.H3M.Network
{
    /// <summary>
    /// VFX property binder that connects H3MWebRTCReceiver output to VFX properties.
    /// Binds color, depth, camera matrices for remote hologram visualization.
    /// </summary>
    [AddComponentMenu("VFX/Property Binders/H3M WebRTC Binder")]
    [VFXBinder("H3M/WebRTC Stream Binder")]
    public sealed class H3MWebRTCVFXBinder : VFXBinderBase
    {
        #region VFX Properties

        [VFXPropertyBinding("UnityEngine.Texture2D")]
        public ExposedProperty ColorMapProperty = "ColorMap";

        [VFXPropertyBinding("UnityEngine.Texture2D")]
        public ExposedProperty DepthMapProperty = "DepthMap";

        [VFXPropertyBinding("UnityEngine.Vector4")]
        public ExposedProperty RayParamsProperty = "RayParams";

        [VFXPropertyBinding("UnityEngine.Matrix4x4")]
        public ExposedProperty InverseViewProperty = "InverseView";

        [VFXPropertyBinding("UnityEngine.Vector2")]
        public ExposedProperty DepthRangeProperty = "DepthRange";

        #endregion

        #region Configuration

        [Header("Source")]
        [Tooltip("The WebRTC receiver to bind from")]
        public H3MWebRTCReceiver Target;

        [Header("Options")]
        [Tooltip("Only bind when connected")]
        public bool RequireConnection = true;

        [Tooltip("Show debug info")]
        public bool DebugLogging = false;

        #endregion

        #region VFXBinderBase Implementation

        public override bool IsValid(VisualEffect component)
        {
            if (Target == null) return false;
            if (RequireConnection && !Target.IsConnected) return false;

            return component.HasTexture(ColorMapProperty) &&
                   component.HasTexture(DepthMapProperty);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            if (Target == null) return;
            if (RequireConnection && !Target.IsConnected) return;

            // Get textures from receiver
            var colorTex = Target.ColorTexture;
            var depthTex = Target.DepthTexture;

            if (colorTex == null || depthTex == null) return;

            // Get metadata
            var metadata = Target.Metadata;

            // Calculate inverse projection/ray params
            var rayParams = metadata.GetRayParams();
            var inverseView = metadata.GetInverseView();
            var depthRange = metadata.DepthRange;

            // Bind textures
            if (component.HasTexture(ColorMapProperty))
                component.SetTexture(ColorMapProperty, colorTex);

            if (component.HasTexture(DepthMapProperty))
                component.SetTexture(DepthMapProperty, depthTex);

            // Bind matrices/vectors
            if (component.HasVector4(RayParamsProperty))
                component.SetVector4(RayParamsProperty, rayParams);

            if (component.HasMatrix4x4(InverseViewProperty))
                component.SetMatrix4x4(InverseViewProperty, inverseView);

            if (component.HasVector2(DepthRangeProperty))
                component.SetVector2(DepthRangeProperty, depthRange);

            if (DebugLogging)
            {
                Debug.Log($"[H3MWebRTCBinder] Bound to {component.name}: pos={metadata.CameraPosition}");
            }
        }

        public override string ToString()
        {
            var targetName = Target != null ? Target.name : "None";
            var connected = Target != null && Target.IsConnected ? "Connected" : "Disconnected";
            return $"H3M WebRTC [{targetName}] ({connected})";
        }

        #endregion

        #region Editor Support

        private void Reset()
        {
            // Try to find receiver on same GameObject or in scene
            Target = GetComponent<H3MWebRTCReceiver>();
            if (Target == null)
            {
                Target = FindFirstObjectByType<H3MWebRTCReceiver>();
            }
        }

        #endregion
    }
}
