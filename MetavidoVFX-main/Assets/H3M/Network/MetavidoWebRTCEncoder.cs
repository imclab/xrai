// MetavidoWebRTCEncoder - Encodes AR frames in Metavido format for WebRTC streaming
// Uses keijiro's Metavido encoding: color + depth + camera pose burnt into single frame
//
// Usage:
// 1. Add to scene with ARCameraWebRTCCapture
// 2. Assign XRDataProvider or configure to use ARDepthSource
// 3. Call GetEncodedFrame() to get the multiplexed texture for WebRTC

using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Metavido.Common;

namespace MetavidoVFX.H3M.Network
{
    /// <summary>
    /// Encodes AR frames in Metavido format for WebRTC streaming.
    /// Produces a single 1920x1080 frame containing:
    /// - Color (left half)
    /// - Depth (right bottom quadrant)
    /// - Camera pose (burnt-in barcode)
    /// </summary>
    public class MetavidoWebRTCEncoder : MonoBehaviour
    {
        #region Serialized Fields

        [Header("AR Sources")]
        [Tooltip("AR Camera Manager for camera textures")]
        [SerializeField] private ARCameraManager _cameraManager;

        [Tooltip("AR Occlusion Manager for depth/stencil")]
        [SerializeField] private AROcclusionManager _occlusionManager;

        [Tooltip("Camera for transform and projection")]
        [SerializeField] private Camera _arCamera;

        [Header("Encoding Settings")]
        [SerializeField] private float _minDepth = 0.1f;
        [SerializeField] private float _maxDepth = 5f;
        [SerializeField] private int _outputWidth = 1920;
        [SerializeField] private int _outputHeight = 1080;

        [Header("Shader References")]
        [SerializeField] private Shader _encoderShader;
        [SerializeField] private ComputeShader _metadataShader;

        [Header("Debug")]
        [SerializeField] private bool _logDebugInfo = false;

        #endregion

        #region Public Properties

        /// <summary>The encoded Metavido frame ready for WebRTC streaming.</summary>
        public RenderTexture EncodedTexture => _encodedTexture;

        /// <summary>Current camera metadata (position, rotation, FOV, depth range).</summary>
        public Metadata CurrentMetadata => _currentMetadata;

        /// <summary>Whether encoding is active and producing frames.</summary>
        public bool IsEncoding => _isEncoding;

        /// <summary>Frames encoded since start.</summary>
        public int FrameCount => _frameCount;

        #endregion

        #region Events

        /// <summary>Fired when a new encoded frame is ready.</summary>
        public event Action<RenderTexture, Metadata> OnFrameEncoded;

        #endregion

        #region Private Fields

        private RenderTexture _encodedTexture;
        private RenderTexture _colorTexture;
        private RenderTexture _depthTexture;
        private Material _encoderMaterial;
        private GraphicsBuffer _metadataBuffer;
        private Metadata[] _metadataArray = new Metadata[1];
        private Metadata _currentMetadata;
        private bool _isEncoding;
        private int _frameCount;

        // Cached reference to avoid expensive FindFirstObjectByType every frame
        private ARDepthSource _cachedARDepthSource;

        // Shader property IDs
        private static readonly int _TextureY = Shader.PropertyToID("_TextureY");
        private static readonly int _TextureCbCr = Shader.PropertyToID("_TextureCbCr");
        private static readonly int _ColorTex = Shader.PropertyToID("_ColorTex");
        private static readonly int _DepthTex = Shader.PropertyToID("_DepthTex");
        private static readonly int _StencilTex = Shader.PropertyToID("_StencilTex");
        private static readonly int _DepthRange = Shader.PropertyToID("_DepthRange");
        private static readonly int _AspectFix = Shader.PropertyToID("_AspectFix");
        private static readonly int _Metadata = Shader.PropertyToID("_Metadata");

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            // Find components if not assigned
            if (_cameraManager == null)
                _cameraManager = FindFirstObjectByType<ARCameraManager>();
            if (_occlusionManager == null)
                _occlusionManager = FindFirstObjectByType<AROcclusionManager>();
            if (_arCamera == null && _cameraManager != null)
                _arCamera = _cameraManager.GetComponent<Camera>();
        }

        private void Start()
        {
            InitializeEncoder();
        }

        private void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }

        private void OnDestroy()
        {
            CleanupResources();
        }

        #endregion

        #region Initialization

        private void InitializeEncoder()
        {
            // Load shader if not assigned
            if (_encoderShader == null)
            {
                _encoderShader = Shader.Find("Hidden/Metavido/WebRTCEncoder");
                if (_encoderShader == null)
                {
                    // Fallback to simple blit shader
                    _encoderShader = Shader.Find("Hidden/Blit");
                    LogWarning("MetavidoWebRTCEncoder shader not found, using fallback");
                }
            }

            // Create encoder material
            if (_encoderShader != null)
            {
                _encoderMaterial = new Material(_encoderShader);
            }

            // Create output render texture
            _encodedTexture = new RenderTexture(_outputWidth, _outputHeight, 0, RenderTextureFormat.ARGB32);
            _encodedTexture.name = "MetavidoEncodedFrame";
            _encodedTexture.Create();

            // Create intermediate textures
            _colorTexture = new RenderTexture(_outputWidth / 2, _outputHeight, 0, RenderTextureFormat.ARGB32);
            _colorTexture.Create();

            _depthTexture = new RenderTexture(_outputWidth / 2, _outputHeight / 2, 0, RenderTextureFormat.RHalf);
            _depthTexture.Create();

            // Create metadata buffer (12 floats)
            _metadataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 12 * sizeof(float));

            _isEncoding = true;
            Log("Encoder initialized");
        }

        private void CleanupResources()
        {
            _isEncoding = false;

            if (_encoderMaterial != null)
            {
                Destroy(_encoderMaterial);
                _encoderMaterial = null;
            }

            if (_encodedTexture != null)
            {
                _encodedTexture.Release();
                Destroy(_encodedTexture);
                _encodedTexture = null;
            }

            if (_colorTexture != null)
            {
                _colorTexture.Release();
                Destroy(_colorTexture);
                _colorTexture = null;
            }

            if (_depthTexture != null)
            {
                _depthTexture.Release();
                Destroy(_depthTexture);
                _depthTexture = null;
            }

            if (_metadataBuffer != null)
            {
                _metadataBuffer.Dispose();
                _metadataBuffer = null;
            }
        }

        #endregion

        #region Encoding

        private void OnBeforeRender()
        {
            if (!_isEncoding) return;

            EncodeFrame();
        }

        /// <summary>
        /// Encode the current AR frame into Metavido format.
        /// </summary>
        public void EncodeFrame()
        {
            if (!ValidateInputs()) return;

            // Get AR textures
            var colorTex = GetColorTexture();
            var depthTex = GetDepthTexture();
            var stencilTex = GetStencilTexture();

            if (colorTex == null)
            {
                LogWarning("No color texture available");
                return;
            }

            // Create metadata from camera
            var projMatrix = _arCamera.projectionMatrix;
            var depthRange = new Vector2(_minDepth, _maxDepth);
            _currentMetadata = new Metadata(_arCamera.transform, projMatrix, depthRange);

            // Upload metadata to GPU
            _metadataArray[0] = _currentMetadata;
            _metadataBuffer.SetData(_metadataArray);

            // Encode frame using shader
            if (_encoderMaterial != null)
            {
                // Set textures
                _encoderMaterial.SetTexture(_ColorTex, colorTex);
                if (depthTex != null)
                    _encoderMaterial.SetTexture(_DepthTex, depthTex);
                if (stencilTex != null)
                    _encoderMaterial.SetTexture(_StencilTex, stencilTex);

                // Set parameters
                _encoderMaterial.SetVector(_DepthRange, depthRange);
                _encoderMaterial.SetFloat(_AspectFix, 9f / 16f * colorTex.width / colorTex.height);
                _encoderMaterial.SetBuffer(_Metadata, _metadataBuffer);

                // Blit to output
                Graphics.Blit(null, _encodedTexture, _encoderMaterial);
            }
            else
            {
                // Fallback: just blit color
                Graphics.Blit(colorTex, _encodedTexture);
            }

            _frameCount++;
            OnFrameEncoded?.Invoke(_encodedTexture, _currentMetadata);
        }

        private bool ValidateInputs()
        {
            if (_arCamera == null)
            {
                LogWarning("AR Camera not assigned");
                return false;
            }

            return true;
        }

        private Texture GetColorTexture()
        {
            // Use cached ARDepthSource to avoid expensive FindFirstObjectByType every frame
            if (_cachedARDepthSource == null)
                _cachedARDepthSource = FindFirstObjectByType<ARDepthSource>();

            if (_cachedARDepthSource != null && _cachedARDepthSource.ColorMap != null)
            {
                return _cachedARDepthSource.ColorMap;
            }

            // Try camera manager
            if (_cameraManager != null)
            {
                // Get CPU image and convert (for platforms without direct texture access)
                // This is a fallback - prefer ARDepthSource
            }

            return null;
        }

        private Texture GetDepthTexture()
        {
            // Use cached ARDepthSource
            if (_cachedARDepthSource == null)
                _cachedARDepthSource = FindFirstObjectByType<ARDepthSource>();

            if (_cachedARDepthSource != null && _cachedARDepthSource.DepthMap != null)
            {
                return _cachedARDepthSource.DepthMap;
            }

            // Try occlusion manager
            if (_occlusionManager != null)
            {
                try
                {
                    return _occlusionManager.environmentDepthTexture;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private Texture GetStencilTexture()
        {
            // Use cached ARDepthSource
            if (_cachedARDepthSource == null)
                _cachedARDepthSource = FindFirstObjectByType<ARDepthSource>();

            if (_cachedARDepthSource != null && _cachedARDepthSource.StencilMap != null)
            {
                return _cachedARDepthSource.StencilMap;
            }

            // Try occlusion manager
            if (_occlusionManager != null)
            {
                try
                {
                    return _occlusionManager.humanStencilTexture;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the encoded frame as raw bytes for WebRTC transmission.
        /// </summary>
        public byte[] GetEncodedFrameBytes()
        {
            if (_encodedTexture == null) return null;

            // Read pixels from render texture
            var tex2D = new Texture2D(_encodedTexture.width, _encodedTexture.height, TextureFormat.RGBA32, false);
            var prevRT = RenderTexture.active;
            RenderTexture.active = _encodedTexture;
            tex2D.ReadPixels(new Rect(0, 0, _encodedTexture.width, _encodedTexture.height), 0, 0);
            tex2D.Apply();
            RenderTexture.active = prevRT;

            var bytes = tex2D.GetRawTextureData();
            Destroy(tex2D);

            return bytes;
        }

        /// <summary>
        /// Set depth range for encoding.
        /// </summary>
        public void SetDepthRange(float min, float max)
        {
            _minDepth = min;
            _maxDepth = max;
        }

        /// <summary>
        /// Pause encoding.
        /// </summary>
        public void Pause()
        {
            _isEncoding = false;
        }

        /// <summary>
        /// Resume encoding.
        /// </summary>
        public void Resume()
        {
            _isEncoding = true;
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_logDebugInfo)
                Debug.Log($"[MetavidoWebRTCEncoder] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[MetavidoWebRTCEncoder] {message}");
        }

        #endregion
    }
}
