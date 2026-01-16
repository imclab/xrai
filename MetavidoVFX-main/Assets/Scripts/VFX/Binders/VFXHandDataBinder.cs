// VFXHandDataBinder - Binds hand tracking data to VFX
// Uses Transform-based tracking with velocity computation

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace MetavidoVFX.VFX.Binders
{
    [VFXBinder("Hand Tracking/Hand Data")]
    public class VFXHandDataBinder : VFXBinderBase
    {
        [Header("Hand Transform (drag hand/wrist here)")]
        public Transform handTransform;

        [Header("Which Hand")]
        public bool useLeftHand = true;

        [Header("Property Names")]
        [VFXPropertyBinding("UnityEngine.Vector3")]
        public ExposedProperty positionProperty = "HandPosition";
        [VFXPropertyBinding("UnityEngine.Vector3")]
        public ExposedProperty velocityProperty = "HandVelocity";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty speedProperty = "HandSpeed";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty brushWidthProperty = "BrushWidth";
        [VFXPropertyBinding("System.Boolean")]
        public ExposedProperty isPinchingProperty = "IsPinching";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty trailLengthProperty = "TrailLength";

        [Header("Settings")]
        [Range(0.01f, 0.5f)]
        public float defaultBrushWidth = 0.05f;
        [Range(0f, 2f)]
        public float velocityMultiplier = 1f;

        // Velocity computation
        private Vector3 _lastPosition;
        private Vector3 _velocity;
        private float _lastTime;

        public override bool IsValid(VisualEffect component)
        {
            return component.HasVector3(positionProperty) ||
                   component.HasVector3(velocityProperty) ||
                   component.HasFloat(speedProperty) ||
                   component.HasFloat(brushWidthProperty);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            Vector3 position = handTransform != null ? handTransform.position : Vector3.zero;

            // Compute velocity from position delta
            float deltaTime = Time.time - _lastTime;
            if (deltaTime > 0.001f)
            {
                _velocity = (position - _lastPosition) / deltaTime;
            }
            _lastPosition = position;
            _lastTime = Time.time;

            float speed = _velocity.magnitude;

            // Bind to VFX
            if (component.HasVector3(positionProperty))
                component.SetVector3(positionProperty, position);

            if (component.HasVector3(velocityProperty))
                component.SetVector3(velocityProperty, _velocity * velocityMultiplier);

            if (component.HasFloat(speedProperty))
                component.SetFloat(speedProperty, speed * velocityMultiplier);

            if (component.HasFloat(brushWidthProperty))
                component.SetFloat(brushWidthProperty, defaultBrushWidth);

            if (component.HasBool(isPinchingProperty))
                component.SetBool(isPinchingProperty, false);

            if (component.HasFloat(trailLengthProperty))
                component.SetFloat(trailLengthProperty, speed * velocityMultiplier * 0.1f);
        }

        public override string ToString()
        {
            string hand = useLeftHand ? "Left" : "Right";
            return $"Hand Data ({hand}) : {positionProperty}, {velocityProperty}";
        }
    }
}
