// VFXHandBinder - Binds unified hand tracking to VFX (spec-012)
// Uses HandTrackingProviderManager for cross-platform hand data

using UnityEngine;
using UnityEngine.VFX;
using XRRAI.HandTracking;

namespace XRRAI.VFXBinders
{
    /// <summary>
    /// Binds hand tracking data from unified provider to VFX Graph.
    /// Supports both hands with velocity smoothing.
    /// </summary>
    [RequireComponent(typeof(VisualEffect))]
    public class VFXHandBinder : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] Hand _targetHand = Hand.Right;
        [SerializeField] HandJointID _primaryJoint = HandJointID.Wrist;

        [Header("Velocity Smoothing")]
        [SerializeField] float _velocitySmoothTime = 0.1f;
        [SerializeField] float _maxVelocity = 10f;

        [Header("Parameter Mapping")]
        [SerializeField] float _speedToParticleRate = 100f;
        [SerializeField] float _pinchToWidth = 0.1f;

        VisualEffect _vfx;
        HandTrackingProviderManager _manager;

        Vector3 _prevPosition;
        Vector3 _velocity;
        Vector3 _smoothVelocity;

        // Cached property IDs
        static readonly int _idHandPosition = Shader.PropertyToID("HandPosition");
        static readonly int _idHandVelocity = Shader.PropertyToID("HandVelocity");
        static readonly int _idHandSpeed = Shader.PropertyToID("HandSpeed");
        static readonly int _idIsPinching = Shader.PropertyToID("IsPinching");
        static readonly int _idPinchStrength = Shader.PropertyToID("PinchStrength");
        static readonly int _idBrushWidth = Shader.PropertyToID("BrushWidth");
        static readonly int _idEmissionRate = Shader.PropertyToID("EmissionRate");
        static readonly int _idTrailLength = Shader.PropertyToID("TrailLength");

        // Additional property IDs (aliases)
        static readonly int _idPosition = Shader.PropertyToID("Position");
        static readonly int _idVelocity = Shader.PropertyToID("Velocity");
        static readonly int _idSpeed = Shader.PropertyToID("Speed");

        void Awake()
        {
            _vfx = GetComponent<VisualEffect>();
        }

        void Start()
        {
            _manager = HandTrackingProviderManager.Instance;
            if (_manager == null)
            {
                _manager = FindFirstObjectByType<HandTrackingProviderManager>();
            }

            if (_manager != null)
            {
                _manager.OnGestureStart += HandleGestureStart;
                _manager.OnGestureEnd += HandleGestureEnd;
            }
        }

        void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnGestureStart -= HandleGestureStart;
                _manager.OnGestureEnd -= HandleGestureEnd;
            }
        }

        void Update()
        {
            if (_vfx == null || _manager == null || !_manager.IsTracking) return;

            if (!_manager.IsHandTracked(_targetHand)) return;

            var position = _manager.GetJointPosition(_targetHand, _primaryJoint);
            UpdateVelocity(position);
            PushParameters(position);
        }

        void UpdateVelocity(Vector3 currentPos)
        {
            if (_prevPosition != Vector3.zero)
            {
                var instantVelocity = (currentPos - _prevPosition) / Time.deltaTime;
                instantVelocity = Vector3.ClampMagnitude(instantVelocity, _maxVelocity);
                _velocity = Vector3.SmoothDamp(_velocity, instantVelocity, ref _smoothVelocity, _velocitySmoothTime);
            }
            _prevPosition = currentPos;
        }

        void PushParameters(Vector3 position)
        {
            float speed = _velocity.magnitude;
            float pinchStrength = _manager.GetPinchStrength(_targetHand);
            bool isPinching = _manager.IsGestureActive(_targetHand, GestureType.Pinch);

            // Position
            TrySetVector3(_idHandPosition, position);
            TrySetVector3(_idPosition, position);

            // Velocity
            TrySetVector3(_idHandVelocity, _velocity);
            TrySetVector3(_idVelocity, _velocity);

            // Speed
            TrySetFloat(_idHandSpeed, speed);
            TrySetFloat(_idSpeed, speed);

            // Pinch state
            TrySetBool(_idIsPinching, isPinching);
            TrySetFloat(_idPinchStrength, pinchStrength);

            // Derived parameters
            float brushWidth = Mathf.Lerp(0.01f, _pinchToWidth, pinchStrength);
            TrySetFloat(_idBrushWidth, brushWidth);

            float emissionRate = speed * _speedToParticleRate;
            TrySetFloat(_idEmissionRate, emissionRate);

            float trailLength = Mathf.Lerp(0.1f, 1f, speed / 2f);
            TrySetFloat(_idTrailLength, trailLength);
        }

        void HandleGestureStart(Hand hand, GestureType gesture)
        {
            if (hand != _targetHand || _vfx == null) return;

            switch (gesture)
            {
                case GestureType.Pinch:
                    _vfx.SendEvent("OnPinchStart");
                    break;
                case GestureType.OpenPalm:
                    _vfx.SendEvent("OnOpenPalm");
                    break;
            }
        }

        void HandleGestureEnd(Hand hand, GestureType gesture)
        {
            if (hand != _targetHand || _vfx == null) return;

            if (gesture == GestureType.Pinch)
            {
                _vfx.SendEvent("OnPinchEnd");
            }
        }

        void TrySetVector3(int id, Vector3 value)
        {
            if (_vfx.HasVector3(id))
                _vfx.SetVector3(id, value);
        }

        void TrySetFloat(int id, float value)
        {
            if (_vfx.HasFloat(id))
                _vfx.SetFloat(id, value);
        }

        void TrySetBool(int id, bool value)
        {
            if (_vfx.HasBool(id))
                _vfx.SetBool(id, value);
        }

        /// <summary>
        /// Switch which hand this binder tracks.
        /// </summary>
        public void SetTargetHand(Hand hand)
        {
            _targetHand = hand;
            _prevPosition = Vector3.zero;
            _velocity = Vector3.zero;
        }
    }
}
