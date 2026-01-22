// ColorPicker.cs - Palm-projected color wheel for brush painting (spec-012)
// Provides HSB color selection via hand position over palm

using System;
using UnityEngine;
using MetavidoVFX.HandTracking;

namespace MetavidoVFX.Painting
{
    /// <summary>
    /// Palm-projected color wheel for selecting brush colors.
    /// Pinch position over palm determines hue/saturation, second hand controls brightness.
    /// </summary>
    public class ColorPicker : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Hand Configuration")]
        [SerializeField] private Hand _paletteHand = Hand.Left;
        [SerializeField] private float _wheelRadius = 0.08f;
        [SerializeField] private float _activationDistance = 0.15f;

        [Header("Color Settings")]
        [SerializeField] private Color _currentColor = Color.cyan;
        [SerializeField] private float _brightness = 1f;
        [SerializeField] private float _saturation = 1f;
        [SerializeField] private float _hue = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject _colorWheelPrefab;
        [SerializeField] private float _wheelScale = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region Events

        public event Action<Color> OnColorChanged;
        public event Action OnPickerActivated;
        public event Action OnPickerDeactivated;

        #endregion

        #region Private State

        private IHandTrackingProvider _provider;
        private bool _isActive;
        private GameObject _wheelInstance;
        private Vector3 _palmPosition;
        private Vector3 _palmNormal;

        private Hand _selectionHand;

        #endregion

        #region Properties

        public Color CurrentColor => _currentColor;
        public float Hue => _hue;
        public float Saturation => _saturation;
        public float Brightness => _brightness;
        public bool IsActive => _isActive;

        #endregion

        #region MonoBehaviour

        private void Start()
        {
            _provider = HandTrackingProviderManager.Instance?.ActiveProvider;
            _selectionHand = _paletteHand == Hand.Left ? Hand.Right : Hand.Left;
        }

        private void Update()
        {
            if (_provider == null)
            {
                _provider = HandTrackingProviderManager.Instance?.ActiveProvider;
                if (_provider == null) return;
            }

            UpdatePaletteHandTracking();

            if (_isActive)
            {
                UpdateColorSelection();
                UpdateVisual();
            }
        }

        private void OnDestroy()
        {
            if (_wheelInstance != null)
            {
                Destroy(_wheelInstance);
            }
        }

        #endregion

        #region Private Methods

        private void UpdatePaletteHandTracking()
        {
            if (!_provider.IsHandTracked(_paletteHand))
            {
                if (_isActive)
                {
                    Deactivate();
                }
                return;
            }

            // Get palm position and orientation
            _palmPosition = _provider.GetJointPosition(_paletteHand, HandJointID.Palm);

            // Calculate palm normal from wrist to middle finger
            Vector3 wrist = _provider.GetJointPosition(_paletteHand, HandJointID.Wrist);
            Vector3 middleTip = _provider.GetJointPosition(_paletteHand, HandJointID.MiddleTip);
            _palmNormal = (middleTip - wrist).normalized;
        }

        private void UpdateColorSelection()
        {
            if (!_provider.IsHandTracked(_selectionHand))
                return;

            // Get selection hand fingertip (index)
            Vector3 indexTip = _provider.GetJointPosition(_selectionHand, HandJointID.IndexTip);

            // Project onto palm plane
            Vector3 toPalm = indexTip - _palmPosition;
            float distanceToPlane = Vector3.Dot(toPalm, _palmNormal);

            // Only select if close to palm plane
            if (Mathf.Abs(distanceToPlane) > _activationDistance)
                return;

            // Project point onto palm plane
            Vector3 projectedPoint = indexTip - _palmNormal * distanceToPlane;
            Vector3 localOffset = projectedPoint - _palmPosition;

            // Calculate polar coordinates for hue/saturation
            float distance = localOffset.magnitude;
            float normalizedDistance = Mathf.Clamp01(distance / _wheelRadius);

            if (distance > 0.001f)
            {
                // Calculate angle for hue (0-1)
                Vector3 palmRight = Vector3.Cross(_palmNormal, Vector3.up).normalized;
                if (palmRight.magnitude < 0.1f)
                    palmRight = Vector3.Cross(_palmNormal, Vector3.forward).normalized;

                Vector3 palmUp = Vector3.Cross(palmRight, _palmNormal);

                float x = Vector3.Dot(localOffset, palmRight);
                float y = Vector3.Dot(localOffset, palmUp);

                float angle = Mathf.Atan2(y, x);
                _hue = (angle + Mathf.PI) / (2f * Mathf.PI);

                // Distance from center for saturation
                _saturation = normalizedDistance;
            }

            // Use second hand height for brightness
            Vector3 thumbTip = _provider.GetJointPosition(_selectionHand, HandJointID.ThumbTip);
            float heightDiff = thumbTip.y - indexTip.y;
            _brightness = Mathf.Clamp01(0.5f + heightDiff * 5f);

            // Convert HSB to RGB
            _currentColor = Color.HSVToRGB(_hue, _saturation, _brightness);

            OnColorChanged?.Invoke(_currentColor);

            if (_debugMode)
                Debug.Log($"[ColorPicker] H:{_hue:F2} S:{_saturation:F2} B:{_brightness:F2}");
        }

        private void UpdateVisual()
        {
            if (_wheelInstance == null || !_isActive)
                return;

            // Position wheel above palm
            _wheelInstance.transform.position = _palmPosition + _palmNormal * 0.02f;
            _wheelInstance.transform.rotation = Quaternion.LookRotation(_palmNormal);
            _wheelInstance.transform.localScale = Vector3.one * _wheelScale;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Activate the color picker (show wheel).
        /// </summary>
        public void Activate()
        {
            if (_isActive) return;

            _isActive = true;

            // Spawn visual if prefab assigned
            if (_colorWheelPrefab != null && _wheelInstance == null)
            {
                _wheelInstance = Instantiate(_colorWheelPrefab);
            }

            if (_wheelInstance != null)
            {
                _wheelInstance.SetActive(true);
            }

            OnPickerActivated?.Invoke();

            if (_debugMode)
                Debug.Log("[ColorPicker] Activated");
        }

        /// <summary>
        /// Deactivate the color picker (hide wheel).
        /// </summary>
        public void Deactivate()
        {
            if (!_isActive) return;

            _isActive = false;

            if (_wheelInstance != null)
            {
                _wheelInstance.SetActive(false);
            }

            OnPickerDeactivated?.Invoke();

            if (_debugMode)
                Debug.Log("[ColorPicker] Deactivated");
        }

        /// <summary>
        /// Toggle the color picker.
        /// </summary>
        public void Toggle()
        {
            if (_isActive)
                Deactivate();
            else
                Activate();
        }

        /// <summary>
        /// Set color directly (bypasses picker).
        /// </summary>
        public void SetColor(Color color)
        {
            _currentColor = color;
            Color.RGBToHSV(color, out _hue, out _saturation, out _brightness);
            OnColorChanged?.Invoke(_currentColor);
        }

        /// <summary>
        /// Set hue directly (0-1).
        /// </summary>
        public void SetHue(float hue)
        {
            _hue = Mathf.Clamp01(hue);
            _currentColor = Color.HSVToRGB(_hue, _saturation, _brightness);
            OnColorChanged?.Invoke(_currentColor);
        }

        /// <summary>
        /// Set saturation directly (0-1).
        /// </summary>
        public void SetSaturation(float saturation)
        {
            _saturation = Mathf.Clamp01(saturation);
            _currentColor = Color.HSVToRGB(_hue, _saturation, _brightness);
            OnColorChanged?.Invoke(_currentColor);
        }

        /// <summary>
        /// Set brightness directly (0-1).
        /// </summary>
        public void SetBrightness(float brightness)
        {
            _brightness = Mathf.Clamp01(brightness);
            _currentColor = Color.HSVToRGB(_hue, _saturation, _brightness);
            OnColorChanged?.Invoke(_currentColor);
        }

        /// <summary>
        /// Swap which hand holds the palette.
        /// </summary>
        public void SwapHands()
        {
            _paletteHand = _paletteHand == Hand.Left ? Hand.Right : Hand.Left;
            _selectionHand = _paletteHand == Hand.Left ? Hand.Right : Hand.Left;
        }

        #endregion
    }
}
