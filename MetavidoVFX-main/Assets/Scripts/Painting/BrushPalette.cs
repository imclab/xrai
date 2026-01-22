// BrushPalette.cs - Circular brush selection palette for hand painting (spec-012)
// Displays brush options above non-dominant palm with point-to-select interaction

using System;
using System.Collections.Generic;
using UnityEngine;
using MetavidoVFX.HandTracking;

namespace MetavidoVFX.Painting
{
    /// <summary>
    /// Circular palette for brush selection.
    /// Appears above non-dominant palm when activated.
    /// Point with dominant hand index finger to select.
    /// </summary>
    public class BrushPalette : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Hand Configuration")]
        [SerializeField] private Hand _paletteHand = Hand.Left;
        [SerializeField] private float _activationDistance = 0.1f;

        [Header("Layout")]
        [SerializeField] private float _radius = 0.06f;
        [SerializeField] private float _heightAbovePalm = 0.05f;
        [SerializeField] private int _brushCount = 8;

        [Header("Brushes")]
        [SerializeField] private List<BrushOption> _brushOptions = new List<BrushOption>();

        [Header("Visual")]
        [SerializeField] private GameObject _paletteRootPrefab;
        [SerializeField] private GameObject _brushIconPrefab;
        [SerializeField] private GameObject _selectionHighlightPrefab;
        [SerializeField] private float _iconScale = 0.02f;
        [SerializeField] private float _animationSpeed = 5f;

        [Header("Selection")]
        [SerializeField] private int _selectedIndex = 0;
        [SerializeField] private int _hoveredIndex = -1;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region Nested Types

        [Serializable]
        public class BrushOption
        {
            public string Name;
            public BrushController.BrushType BrushType;
            public Sprite Icon;
            public Color PreviewColor = Color.white;
        }

        #endregion

        #region Events

        public event Action<BrushController.BrushType> OnBrushSelected;
        public event Action<int> OnBrushHovered;
        public event Action OnPaletteOpened;
        public event Action OnPaletteClosed;

        #endregion

        #region Private State

        private IHandTrackingProvider _provider;
        private bool _isActive;
        private GameObject _paletteRoot;
        private List<GameObject> _iconInstances = new List<GameObject>();
        private GameObject _highlightInstance;

        private Vector3 _palmPosition;
        private Vector3 _palmNormal;
        private float _openProgress;

        private Hand _selectionHand;

        #endregion

        #region Properties

        public bool IsActive => _isActive;
        public int SelectedIndex => _selectedIndex;
        public int HoveredIndex => _hoveredIndex;
        public BrushController.BrushType SelectedBrush =>
            _selectedIndex >= 0 && _selectedIndex < _brushOptions.Count
                ? _brushOptions[_selectedIndex].BrushType
                : BrushController.BrushType.ParticleTrail;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            // Initialize default brush options if empty
            if (_brushOptions.Count == 0)
            {
                InitializeDefaultBrushes();
            }
        }

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
                UpdateSelection();
                UpdateVisuals();
            }

            // Animate open/close
            float targetProgress = _isActive ? 1f : 0f;
            _openProgress = Mathf.Lerp(_openProgress, targetProgress, Time.deltaTime * _animationSpeed);
        }

        private void OnDestroy()
        {
            DestroyVisuals();
        }

        #endregion

        #region Private Methods

        private void InitializeDefaultBrushes()
        {
            _brushOptions = new List<BrushOption>
            {
                new BrushOption { Name = "Trail", BrushType = BrushController.BrushType.ParticleTrail, PreviewColor = Color.cyan },
                new BrushOption { Name = "Ribbon", BrushType = BrushController.BrushType.Ribbon, PreviewColor = Color.magenta },
                new BrushOption { Name = "Spray", BrushType = BrushController.BrushType.Spray, PreviewColor = Color.yellow },
                new BrushOption { Name = "Glow", BrushType = BrushController.BrushType.Glow, PreviewColor = Color.white },
                new BrushOption { Name = "Fire", BrushType = BrushController.BrushType.Fire, PreviewColor = new Color(1f, 0.5f, 0f) },
                new BrushOption { Name = "Sparkle", BrushType = BrushController.BrushType.Sparkle, PreviewColor = Color.yellow },
                new BrushOption { Name = "Tube", BrushType = BrushController.BrushType.Tube, PreviewColor = Color.green },
                new BrushOption { Name = "Smoke", BrushType = BrushController.BrushType.Smoke, PreviewColor = Color.gray }
            };
        }

        private void UpdatePaletteHandTracking()
        {
            if (!_provider.IsHandTracked(_paletteHand))
            {
                return;
            }

            // Get palm position and orientation
            _palmPosition = _provider.GetJointPosition(_paletteHand, HandJointID.Palm);

            // Calculate palm normal (pointing up from palm)
            Vector3 wrist = _provider.GetJointPosition(_paletteHand, HandJointID.Wrist);
            Vector3 middleMCP = _provider.GetJointPosition(_paletteHand, HandJointID.MiddleProximal);
            Vector3 indexMCP = _provider.GetJointPosition(_paletteHand, HandJointID.IndexProximal);

            Vector3 forward = (middleMCP - wrist).normalized;
            Vector3 side = (indexMCP - middleMCP).normalized;
            _palmNormal = Vector3.Cross(forward, side).normalized;

            // Flip if needed (palm should face up)
            if (_paletteHand == Hand.Right)
                _palmNormal = -_palmNormal;
        }

        private void UpdateSelection()
        {
            if (!_provider.IsHandTracked(_selectionHand))
            {
                _hoveredIndex = -1;
                return;
            }

            // Get pointing finger position
            Vector3 indexTip = _provider.GetJointPosition(_selectionHand, HandJointID.IndexTip);

            // Check distance to palette center
            Vector3 paletteCenter = _palmPosition + _palmNormal * _heightAbovePalm;
            float distanceToCenter = Vector3.Distance(indexTip, paletteCenter);

            if (distanceToCenter > _radius * 2f)
            {
                _hoveredIndex = -1;
                return;
            }

            // Find closest brush icon
            int closestIndex = -1;
            float closestDist = _activationDistance;

            for (int i = 0; i < _brushOptions.Count; i++)
            {
                Vector3 iconPos = GetIconPosition(i);
                float dist = Vector3.Distance(indexTip, iconPos);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }

            if (_hoveredIndex != closestIndex)
            {
                _hoveredIndex = closestIndex;
                OnBrushHovered?.Invoke(_hoveredIndex);

                if (_debugMode && _hoveredIndex >= 0)
                    Debug.Log($"[BrushPalette] Hovering: {_brushOptions[_hoveredIndex].Name}");
            }

            // Check for pinch to select
            float pinchStrength = _provider.GetPinchStrength(_selectionHand);
            if (pinchStrength > 0.8f && _hoveredIndex >= 0 && _hoveredIndex != _selectedIndex)
            {
                SelectBrush(_hoveredIndex);
            }
        }

        private Vector3 GetIconPosition(int index)
        {
            float angle = (float)index / _brushOptions.Count * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * _radius;
            float z = Mathf.Sin(angle) * _radius;

            Vector3 paletteCenter = _palmPosition + _palmNormal * _heightAbovePalm;

            // Create local coordinate system
            Vector3 palmRight = Vector3.Cross(_palmNormal, Vector3.up).normalized;
            if (palmRight.magnitude < 0.1f)
                palmRight = Vector3.Cross(_palmNormal, Vector3.forward).normalized;
            Vector3 palmForward = Vector3.Cross(palmRight, _palmNormal);

            return paletteCenter + palmRight * x + palmForward * z;
        }

        private void UpdateVisuals()
        {
            if (_paletteRoot == null) return;

            // Update root position
            Vector3 targetPos = _palmPosition + _palmNormal * _heightAbovePalm;
            _paletteRoot.transform.position = Vector3.Lerp(
                _palmPosition, targetPos, _openProgress);
            _paletteRoot.transform.rotation = Quaternion.LookRotation(_palmNormal);
            _paletteRoot.transform.localScale = Vector3.one * _openProgress;

            // Update icon positions
            for (int i = 0; i < _iconInstances.Count && i < _brushOptions.Count; i++)
            {
                var icon = _iconInstances[i];
                if (icon == null) continue;

                Vector3 localPos = GetIconPosition(i) - _paletteRoot.transform.position;
                icon.transform.localPosition = localPos;

                // Scale based on hover/selection
                float scale = _iconScale;
                if (i == _selectedIndex) scale *= 1.3f;
                else if (i == _hoveredIndex) scale *= 1.15f;

                icon.transform.localScale = Vector3.one * scale;
            }

            // Update highlight
            if (_highlightInstance != null && _selectedIndex >= 0)
            {
                _highlightInstance.transform.position = GetIconPosition(_selectedIndex);
            }
        }

        private void CreateVisuals()
        {
            // Create root
            if (_paletteRootPrefab != null)
            {
                _paletteRoot = Instantiate(_paletteRootPrefab);
            }
            else
            {
                _paletteRoot = new GameObject("BrushPalette_Root");
            }

            // Create icons
            _iconInstances.Clear();
            for (int i = 0; i < _brushOptions.Count; i++)
            {
                GameObject icon;
                if (_brushIconPrefab != null)
                {
                    icon = Instantiate(_brushIconPrefab, _paletteRoot.transform);
                }
                else
                {
                    // Create simple sphere as placeholder
                    icon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    icon.transform.SetParent(_paletteRoot.transform);
                    icon.transform.localScale = Vector3.one * _iconScale;

                    // Set color
                    var renderer = icon.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        renderer.material.color = _brushOptions[i].PreviewColor;
                    }

                    // Remove collider
                    var collider = icon.GetComponent<Collider>();
                    if (collider != null) Destroy(collider);
                }

                icon.name = $"Brush_{_brushOptions[i].Name}";
                _iconInstances.Add(icon);
            }

            // Create highlight
            if (_selectionHighlightPrefab != null)
            {
                _highlightInstance = Instantiate(_selectionHighlightPrefab, _paletteRoot.transform);
            }
            else
            {
                // Create simple ring as placeholder
                _highlightInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _highlightInstance.transform.SetParent(_paletteRoot.transform);
                _highlightInstance.transform.localScale = new Vector3(_iconScale * 1.5f, 0.001f, _iconScale * 1.5f);

                var renderer = _highlightInstance.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    renderer.material.color = Color.white;
                }

                var collider = _highlightInstance.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }
        }

        private void DestroyVisuals()
        {
            foreach (var icon in _iconInstances)
            {
                if (icon != null) Destroy(icon);
            }
            _iconInstances.Clear();

            if (_highlightInstance != null)
            {
                Destroy(_highlightInstance);
                _highlightInstance = null;
            }

            if (_paletteRoot != null)
            {
                Destroy(_paletteRoot);
                _paletteRoot = null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Open the brush palette.
        /// </summary>
        public void Open()
        {
            if (_isActive) return;

            _isActive = true;
            _openProgress = 0f;

            if (_paletteRoot == null)
            {
                CreateVisuals();
            }
            _paletteRoot.SetActive(true);

            OnPaletteOpened?.Invoke();

            if (_debugMode)
                Debug.Log("[BrushPalette] Opened");
        }

        /// <summary>
        /// Close the brush palette.
        /// </summary>
        public void Close()
        {
            if (!_isActive) return;

            _isActive = false;
            _hoveredIndex = -1;

            if (_paletteRoot != null)
            {
                _paletteRoot.SetActive(false);
            }

            OnPaletteClosed?.Invoke();

            if (_debugMode)
                Debug.Log("[BrushPalette] Closed");
        }

        /// <summary>
        /// Toggle the palette.
        /// </summary>
        public void Toggle()
        {
            if (_isActive)
                Close();
            else
                Open();
        }

        /// <summary>
        /// Select a brush by index.
        /// </summary>
        public void SelectBrush(int index)
        {
            if (index < 0 || index >= _brushOptions.Count)
                return;

            _selectedIndex = index;
            OnBrushSelected?.Invoke(_brushOptions[index].BrushType);

            if (_debugMode)
                Debug.Log($"[BrushPalette] Selected: {_brushOptions[index].Name}");
        }

        /// <summary>
        /// Select a brush by type.
        /// </summary>
        public void SelectBrush(BrushController.BrushType brushType)
        {
            for (int i = 0; i < _brushOptions.Count; i++)
            {
                if (_brushOptions[i].BrushType == brushType)
                {
                    SelectBrush(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Select next brush in the palette.
        /// </summary>
        public void NextBrush()
        {
            int next = (_selectedIndex + 1) % _brushOptions.Count;
            SelectBrush(next);
        }

        /// <summary>
        /// Select previous brush in the palette.
        /// </summary>
        public void PreviousBrush()
        {
            int prev = (_selectedIndex - 1 + _brushOptions.Count) % _brushOptions.Count;
            SelectBrush(prev);
        }

        /// <summary>
        /// Swap which hand holds the palette.
        /// </summary>
        public void SwapHands()
        {
            _paletteHand = _paletteHand == Hand.Left ? Hand.Right : Hand.Left;
            _selectionHand = _paletteHand == Hand.Left ? Hand.Right : Hand.Left;
        }

        /// <summary>
        /// Add a custom brush option.
        /// </summary>
        public void AddBrushOption(BrushOption option)
        {
            _brushOptions.Add(option);

            // Recreate visuals if active
            if (_isActive)
            {
                DestroyVisuals();
                CreateVisuals();
            }
        }

        #endregion
    }
}
