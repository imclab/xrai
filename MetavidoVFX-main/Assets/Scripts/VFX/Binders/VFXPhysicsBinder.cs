// VFXPhysicsBinder - Velocity, gravity, AR mesh collision, and hand velocity for VFX
// Updated for spec-007: Added mesh collision support, AR-relative gravity, hand velocity
// Attach to any VFX GameObject along with VFXPropertyBinder

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using MetavidoVFX.HandTracking;

namespace MetavidoVFX.VFX.Binders
{
    /// <summary>
    /// Binds physics data to VFX properties including:
    /// - Camera/hand velocity
    /// - Gravity with AR-relative direction
    /// - AR mesh collision buffers (spec-007)
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

        [Header("Hand Velocity (spec-007)")]
        [Tooltip("Enable hand velocity binding from HandVFXController")]
        public bool enableHandVelocity = false;

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

        [Tooltip("AR-relative gravity: gravity follows device orientation")]
        public bool useARRelativeGravity = false;

        [Header("AR Mesh Collision (spec-007)")]
        [Tooltip("Enable AR mesh collision - requires MeshVFX in scene")]
        public bool enableMeshCollision = false;

        [Tooltip("Bounce factor when particles hit AR mesh (0 = stop, 1 = full bounce)")]
        [Range(0f, 1f)]
        public float bounceFactor = 0.5f;

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

        [Header("Hand Velocity Properties (spec-007)")]
        [VFXPropertyBinding("UnityEngine.Vector3")]
        public ExposedProperty handVelocityProperty = "HandVelocity";

        [VFXPropertyBinding("float")]
        public ExposedProperty handSpeedProperty = "HandSpeed";

        [Header("Mesh Collision Properties (spec-007)")]
        [VFXPropertyBinding("float")]
        public ExposedProperty bounceFactorProperty = "BounceFactor";

        [VFXPropertyBinding("int")]
        public ExposedProperty meshPointCountProperty = "MeshPointCount";

        [Header("Debug")]
        public bool verboseLogging = false;

        public enum VelocitySource
        {
            CameraMovement,     // Derive from camera position delta
            VelocityMap,        // Sample from VelocityMap texture center
            ManualInput,        // Use manualVelocity field
            Transform,          // Derive from this object's movement
            HandTracking        // Use hand velocity from HandVFXController (spec-007)
        }

        // Internal state
        private Camera _arCamera;
        private Vector3 _lastCameraPosition;
        private Vector3 _lastPosition;
        private Vector3 _smoothedVelocity;
        private float _smoothedSpeed;
        private bool _initialized;

        // Hand velocity state (spec-007)
        private Vector3 _handVelocity;
        private float _handSpeed;

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

            // Update and bind hand velocity (spec-007)
            if (enableHandVelocity)
            {
                UpdateHandVelocity();
                BindHandVelocity(component);
            }

            // Bind gravity
            if (enableGravity)
            {
                BindGravity(component);
            }

            // Bind mesh collision (spec-007)
            if (enableMeshCollision)
            {
                BindMeshCollision(component);
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

            if (useARRelativeGravity && _arCamera != null)
            {
                // AR-relative gravity: "down" based on device orientation (spec-007)
                // This makes gravity point toward the floor even when device is tilted
                gravityVector = -_arCamera.transform.up * Mathf.Abs(gravityStrength);
                if (gravityStrength > 0f)
                    gravityVector = -gravityVector;
            }
            else if (useWorldGravity)
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
                Debug.Log($"[VFXPhysicsBinder] Gravity: {gravityVector:F2} Strength: {gravityStrength:F2} AR-Relative: {useARRelativeGravity}");
            }
        }

        #region Hand Velocity (spec-007)

        void UpdateHandVelocity()
        {
            // Try to get hand velocity from HandVFXController
            var handController = FindFirstObjectByType<HandTracking.HandVFXController>();
            if (handController != null)
            {
                // Use reflection or public property to get hand velocity
                // HandVFXController has HandVelocity property
                var velocityProp = handController.GetType().GetProperty("HandVelocity");
                if (velocityProp != null)
                {
                    _handVelocity = (Vector3)velocityProp.GetValue(handController);
                    _handSpeed = _handVelocity.magnitude;
                }
            }
            else
            {
                _handVelocity = Vector3.zero;
                _handSpeed = 0f;
            }
        }

        void BindHandVelocity(VisualEffect component)
        {
            if (component.HasVector3(handVelocityProperty))
                component.SetVector3(handVelocityProperty, _handVelocity);

            if (component.HasFloat(handSpeedProperty))
                component.SetFloat(handSpeedProperty, _handSpeed);

            // Common property name variants
            if (component.HasVector3("Hand Velocity"))
                component.SetVector3("Hand Velocity", _handVelocity);

            if (verboseLogging && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[VFXPhysicsBinder] Hand Velocity: {_handVelocity:F2} Speed: {_handSpeed:F2}");
            }
        }

        #endregion

        #region Mesh Collision (spec-007)

        void BindMeshCollision(VisualEffect component)
        {
            // MeshVFX sets global buffers (_MeshPointCache, _MeshNormalCache) and per-VFX properties
            // We just need to bind the bounce factor and mesh point count

            // Bind bounce factor
            if (component.HasFloat(bounceFactorProperty))
                component.SetFloat(bounceFactorProperty, bounceFactor);

            if (component.HasFloat("Bounce"))
                component.SetFloat("Bounce", bounceFactor);

            // MeshPointCount is typically set by MeshVFX, but we can also access global
            int meshPointCount = Shader.GetGlobalInt("_MeshPointCount");
            if (meshPointCount > 0)
            {
                if (component.HasInt(meshPointCountProperty))
                    component.SetInt(meshPointCountProperty, meshPointCount);
            }

            // Note: GraphicsBuffers (MeshPointCache, MeshNormalCache) are set by MeshVFX directly
            // VFX Graph accesses them via global shader buffers or direct binding from MeshVFX

            if (verboseLogging && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[VFXPhysicsBinder] Mesh Collision: Points={meshPointCount}, Bounce={bounceFactor:F2}");
            }
        }

        #endregion

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
            if (enableHandVelocity) status += (status.Length > 0 ? " + " : "") + "Hand";
            if (enableGravity) status += (status.Length > 0 ? " + " : "") + $"Grav({gravityStrength:F1})";
            if (enableMeshCollision) status += (status.Length > 0 ? " + " : "") + $"Mesh(B:{bounceFactor:F1})";
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

        /// <summary>
        /// Enable/disable hand velocity binding (spec-007)
        /// </summary>
        public void SetHandVelocityEnabled(bool enabled)
        {
            enableHandVelocity = enabled;
        }

        /// <summary>
        /// Enable/disable mesh collision binding (spec-007)
        /// </summary>
        public void SetMeshCollisionEnabled(bool enabled)
        {
            enableMeshCollision = enabled;
        }

        /// <summary>
        /// Set bounce factor for mesh collision (spec-007)
        /// </summary>
        public void SetBounceFactor(float bounce)
        {
            bounceFactor = Mathf.Clamp01(bounce);
        }

        /// <summary>
        /// Enable AR-relative gravity (spec-007)
        /// </summary>
        public void SetARRelativeGravity(bool enabled)
        {
            useARRelativeGravity = enabled;
        }
    }
}
