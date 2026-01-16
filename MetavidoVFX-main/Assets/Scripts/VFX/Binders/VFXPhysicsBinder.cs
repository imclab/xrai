// VFXPhysicsBinder - Optional velocity-driven input and physics/gravity for VFX
// Attach to any VFX GameObject along with VFXPropertyBinder
// Provides toggleable velocity and gravity bindings with configurable ranges

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace MetavidoVFX.VFX.Binders
{
    /// <summary>
    /// Binds optional velocity-driven input and physics/gravity to VFX properties.
    ///
    /// Velocity Properties (bind when enabled):
    /// - Velocity / ReferenceVelocity (Vector3): Motion direction and speed
    /// - Speed (float): Velocity magnitude
    ///
    /// Physics Properties (bind when enabled):
    /// - Gravity / Gravity Vector (Vector3): Gravity force direction
    /// - GravityStrength (float): Gravity magnitude (-20 to 20)
    /// </summary>
    [VFXBinder("Physics/Physics Data")]
    public class VFXPhysicsBinder : VFXBinderBase
    {
        [Header("Velocity Settings")]
        [Tooltip("Enable velocity-driven VFX binding")]
        public bool enableVelocity = true;

        [Tooltip("Source for velocity data")]
        public VelocitySource velocitySource = VelocitySource.CameraMovement;

        [Tooltip("Manual velocity when using ManualInput source")]
        public Vector3 manualVelocity = Vector3.zero;

        [Tooltip("Velocity scale multiplier")]
        [Range(0.1f, 10f)]
        public float velocityScale = 1f;

        [Tooltip("Smooth velocity changes (0 = instant, 1 = very smooth)")]
        [Range(0f, 0.99f)]
        public float velocitySmoothing = 0.5f;

        [Header("Physics/Gravity Settings")]
        [Tooltip("Enable gravity/physics binding")]
        public bool enableGravity = false;

        [Tooltip("Gravity strength (Y-axis, negative = down)")]
        [Range(-20f, 20f)]
        public float gravityStrength = -9.81f;

        [Tooltip("Custom gravity direction (normalized, then scaled by gravityStrength)")]
        public Vector3 gravityDirection = Vector3.down;

        [Tooltip("Use world gravity direction (ignores gravityDirection)")]
        public bool useWorldGravity = true;

        [Header("Property Names")]
        [VFXPropertyBinding("UnityEngine.Vector3")]
        public ExposedProperty velocityProperty = "Velocity";

        [VFXPropertyBinding("UnityEngine.Vector3")]
        public ExposedProperty referenceVelocityProperty = "ReferenceVelocity";

        [VFXPropertyBinding("float")]
        public ExposedProperty speedProperty = "Speed";

        [VFXPropertyBinding("UnityEngine.Vector3")]
        public ExposedProperty gravityProperty = "Gravity";

        [VFXPropertyBinding("UnityEngine.Vector3")]
        public ExposedProperty gravityVectorProperty = "Gravity Vector";

        [VFXPropertyBinding("float")]
        public ExposedProperty gravityStrengthProperty = "GravityStrength";

        [Header("Debug")]
        public bool verboseLogging = false;

        public enum VelocitySource
        {
            CameraMovement,     // Derive from camera position delta
            VelocityMap,        // Sample from VelocityMap texture center
            ManualInput,        // Use manualVelocity field
            Transform           // Derive from this object's movement
        }

        // Internal state
        private Camera _arCamera;
        private Vector3 _lastCameraPosition;
        private Vector3 _lastPosition;
        private Vector3 _smoothedVelocity;
        private float _smoothedSpeed;
        private bool _initialized;

        // VelocityMap sampling
        private RenderTexture _velocityMapRT;
        private Texture2D _readbackTexture;

        protected override void Awake()
        {
            base.Awake();
            _arCamera = Camera.main;
        }

        void Start()
        {
            if (_arCamera != null)
            {
                _lastCameraPosition = _arCamera.transform.position;
            }
            _lastPosition = transform.position;
            _initialized = true;
        }

        public override bool IsValid(VisualEffect component)
        {
            // Valid if at least one velocity or gravity property exists
            return component.HasVector3(velocityProperty) ||
                   component.HasVector3(referenceVelocityProperty) ||
                   component.HasFloat(speedProperty) ||
                   component.HasVector3(gravityProperty) ||
                   component.HasVector3(gravityVectorProperty) ||
                   component.HasFloat(gravityStrengthProperty);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            if (!_initialized) return;

            // Update and bind velocity
            if (enableVelocity)
            {
                UpdateVelocity();
                BindVelocity(component);
            }

            // Bind gravity
            if (enableGravity)
            {
                BindGravity(component);
            }
        }

        void UpdateVelocity()
        {
            Vector3 rawVelocity = Vector3.zero;

            switch (velocitySource)
            {
                case VelocitySource.CameraMovement:
                    if (_arCamera != null)
                    {
                        Vector3 cameraDelta = _arCamera.transform.position - _lastCameraPosition;
                        rawVelocity = cameraDelta / Time.deltaTime;
                        _lastCameraPosition = _arCamera.transform.position;
                    }
                    break;

                case VelocitySource.Transform:
                    Vector3 positionDelta = transform.position - _lastPosition;
                    rawVelocity = positionDelta / Time.deltaTime;
                    _lastPosition = transform.position;
                    break;

                case VelocitySource.ManualInput:
                    rawVelocity = manualVelocity;
                    break;

                case VelocitySource.VelocityMap:
                    rawVelocity = SampleVelocityMap();
                    break;
            }

            // Apply scale
            rawVelocity *= velocityScale;

            // Smooth velocity
            float smoothFactor = 1f - velocitySmoothing;
            _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, rawVelocity, smoothFactor);
            _smoothedSpeed = _smoothedVelocity.magnitude;

            if (verboseLogging && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[VFXPhysicsBinder] Velocity: {_smoothedVelocity:F2} Speed: {_smoothedSpeed:F2}");
            }
        }

        Vector3 SampleVelocityMap()
        {
            // Try to find VelocityMap from VFXBinderManager or VFXARDataBinder
            if (_velocityMapRT == null)
            {
                // Look for existing velocity map in scene
                var manager = FindFirstObjectByType<VFXBinderManager>();
                if (manager != null)
                {
                    // Access private _velocityMapRT via reflection (or make it public)
                    var field = typeof(VFXBinderManager).GetField("_velocityMapRT",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        _velocityMapRT = field.GetValue(manager) as RenderTexture;
                    }
                }
            }

            // Validate velocity texture exists and is usable
            if (_velocityMapRT == null || !_velocityMapRT.IsCreated() ||
                _velocityMapRT.width <= 0 || _velocityMapRT.height <= 0)
            {
                return Vector3.zero;
            }

            // Initialize readback texture
            if (_readbackTexture == null)
            {
                _readbackTexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
            }

            // Sample center of velocity map with bounds validation
            int sampleX = Mathf.Clamp(_velocityMapRT.width / 2, 0, _velocityMapRT.width - 1);
            int sampleY = Mathf.Clamp(_velocityMapRT.height / 2, 0, _velocityMapRT.height - 1);

            // Double-check bounds before ReadPixels
            if (sampleX < 0 || sampleY < 0 || sampleX >= _velocityMapRT.width || sampleY >= _velocityMapRT.height)
            {
                return Vector3.zero;
            }

            RenderTexture.active = _velocityMapRT;
            _readbackTexture.ReadPixels(new Rect(sampleX, sampleY, 1, 1), 0, 0, false);
            _readbackTexture.Apply();
            RenderTexture.active = null;

            Color velocityColor = _readbackTexture.GetPixel(0, 0);
            return new Vector3(velocityColor.r, velocityColor.g, velocityColor.b);
        }

        void BindVelocity(VisualEffect component)
        {
            // Bind Velocity to all possible property names
            if (component.HasVector3(velocityProperty))
                component.SetVector3(velocityProperty, _smoothedVelocity);

            if (component.HasVector3(referenceVelocityProperty))
                component.SetVector3(referenceVelocityProperty, _smoothedVelocity);

            // Also try "Initial Velocity" which some VFX use
            if (component.HasVector3("Initial Velocity"))
                component.SetVector3("Initial Velocity", _smoothedVelocity);

            // Bind speed
            if (component.HasFloat(speedProperty))
                component.SetFloat(speedProperty, _smoothedSpeed);

            // VelocityMagnitude is another common name
            if (component.HasFloat("VelocityMagnitude"))
                component.SetFloat("VelocityMagnitude", _smoothedSpeed);
        }

        void BindGravity(VisualEffect component)
        {
            // Calculate gravity vector
            Vector3 gravityVector;
            if (useWorldGravity)
            {
                gravityVector = new Vector3(0f, gravityStrength, 0f);
            }
            else
            {
                gravityVector = gravityDirection.normalized * Mathf.Abs(gravityStrength);
                if (gravityStrength < 0f)
                    gravityVector = -gravityVector;
            }

            // Bind Gravity to all possible property names
            if (component.HasVector3(gravityProperty))
                component.SetVector3(gravityProperty, gravityVector);

            if (component.HasVector3(gravityVectorProperty))
                component.SetVector3(gravityVectorProperty, gravityVector);

            // Bind gravity strength as scalar
            if (component.HasFloat(gravityStrengthProperty))
                component.SetFloat(gravityStrengthProperty, gravityStrength);

            // GravityY is used by some VFX for simple Y-axis gravity
            if (component.HasFloat("GravityY"))
                component.SetFloat("GravityY", gravityStrength);

            if (verboseLogging && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[VFXPhysicsBinder] Gravity: {gravityVector:F2} Strength: {gravityStrength:F2}");
            }
        }

        void OnDestroy()
        {
            if (_readbackTexture != null)
            {
                Destroy(_readbackTexture);
                _readbackTexture = null;
            }
        }

        public override string ToString()
        {
            string status = "";
            if (enableVelocity) status += $"Vel({velocitySource})";
            if (enableGravity) status += (status.Length > 0 ? " + " : "") + $"Grav({gravityStrength:F1})";
            return $"Physics : {status}";
        }

        // ========== PUBLIC API ==========

        /// <summary>
        /// Set velocity enable state at runtime
        /// </summary>
        public void SetVelocityEnabled(bool enabled)
        {
            enableVelocity = enabled;
        }

        /// <summary>
        /// Set gravity enable state at runtime
        /// </summary>
        public void SetGravityEnabled(bool enabled)
        {
            enableGravity = enabled;
        }

        /// <summary>
        /// Set gravity strength at runtime (-20 to 20)
        /// </summary>
        public void SetGravityStrength(float strength)
        {
            gravityStrength = Mathf.Clamp(strength, -20f, 20f);
        }

        /// <summary>
        /// Set manual velocity (only used when velocitySource is ManualInput)
        /// </summary>
        public void SetManualVelocity(Vector3 velocity)
        {
            manualVelocity = velocity;
        }
    }
}
