// TouchInputHandTrackingProvider - Mouse/touch fallback for Editor testing (spec-012)
// Priority 10 - lowest priority, used when no other providers available

using System;
using UnityEngine;

namespace XRRAI.HandTracking
{
    /// <summary>
    /// Touch/mouse input provider for Editor testing.
    /// Simulates single hand at cursor position.
    /// Click = pinch gesture.
    /// </summary>
    [HandTrackingProvider("touch", priority: 10)]
    public class TouchInputHandTrackingProvider : IHandTrackingProvider
    {
        public string Id => "touch";
        public int Priority => 10;

        bool _initialized;
        Vector3 _handPosition;
        bool _isPinching;
        float _pinchStrength;

        // Screen depth for hand position
        float _handDepth = 0.5f;

        public bool IsAvailable => _initialized;

        public event Action<Hand> OnHandTrackingGained;
        public event Action<Hand> OnHandTrackingLost;
        public event Action<Hand, GestureType> OnGestureStart;
        public event Action<Hand, GestureType> OnGestureEnd;

        public void Initialize()
        {
            _initialized = true;
            OnHandTrackingGained?.Invoke(Hand.Right);
        }

        public void Update()
        {
            if (!_initialized || Camera.main == null) return;

            // Convert mouse position to world position
            var mousePos = Input.mousePosition;
            mousePos.z = _handDepth;
            _handPosition = Camera.main.ScreenToWorldPoint(mousePos);

            // Scroll wheel adjusts depth
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _handDepth = Mathf.Clamp(_handDepth + scroll * 0.1f, 0.2f, 3f);
            }

            // Mouse click = pinch
            bool wasPressed = _isPinching;
            bool isPressed = Input.GetMouseButton(0);

            if (isPressed && !wasPressed)
            {
                _isPinching = true;
                OnGestureStart?.Invoke(Hand.Right, GestureType.Pinch);
            }
            else if (!isPressed && wasPressed)
            {
                _isPinching = false;
                OnGestureEnd?.Invoke(Hand.Right, GestureType.Pinch);
            }

            _pinchStrength = _isPinching ? 1f : 0f;
        }

        public void Shutdown()
        {
            if (_initialized)
            {
                OnHandTrackingLost?.Invoke(Hand.Right);
            }
            _initialized = false;
        }

        public bool IsHandTracked(Hand hand) => hand == Hand.Right && _initialized;

        public Vector3 GetJointPosition(Hand hand, HandJointID joint)
        {
            if (hand != Hand.Right) return Vector3.zero;

            // All joints return same position (no finger tracking)
            return _handPosition;
        }

        public Quaternion GetJointRotation(Hand hand, HandJointID joint)
        {
            // Face camera
            if (Camera.main != null)
            {
                return Quaternion.LookRotation(_handPosition - Camera.main.transform.position);
            }
            return Quaternion.identity;
        }

        public float GetJointRadius(Hand hand, HandJointID joint) => 0.01f;

        public bool IsGestureActive(Hand hand, GestureType gesture)
        {
            if (hand != Hand.Right) return false;
            if (gesture == GestureType.Pinch) return _isPinching;
            return false;
        }

        public float GetPinchStrength(Hand hand) => hand == Hand.Right ? _pinchStrength : 0f;
        public float GetGrabStrength(Hand hand) => 0f;

        public void Dispose() => Shutdown();
    }
}
