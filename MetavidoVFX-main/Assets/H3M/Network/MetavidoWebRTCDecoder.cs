// MetavidoWebRTCDecoder - Decodes Metavido-encoded WebRTC frames
// Uses keijiro's Metavido decoding: separates color, depth, and camera pose from single frame
//
// Usage:
// 1. Add to remote hologram GameObject
// 2. Feed incoming WebRTC frames via DecodeFrame()
// 3. Access ColorTexture, DepthTexture, and Metadata for VFX binding

using System;
using UnityEngine;
using UnityEngine.Rendering;
using Metavido.Common;

namespace XRRAI.Hologram
{
    /// <summary>
    /// Decodes Metavido-encoded WebRTC frames.
    /// Extracts:
    /// - Color texture (left half of frame)
    /// - Depth texture (right bottom quadrant)
    /// - Camera metadata (burnt-in barcode)
    /// </summary>
    public class MetavidoWebRTCDecoder : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Shader References")]
        [SerializeField] private Shader _demuxShader;
        [SerializeField] private ComputeShader _metadataDecoderShader;

        [Header("Settings")]
        [SerializeField, Range(0, 8)] private int _margin = 1;
        [SerializeField] private bool _useAsyncReadback = true;

        [Header("Debug")]
        [SerializeField] private bool _logDebugInfo = false;

        #endregion

        #region Public Properties

        /// <summary>Decoded color texture.</summary>
        public RenderTexture ColorTexture => _colorTexture;

        /// <summary>Decoded depth texture.</summary>
        public RenderTexture DepthTexture => _depthTexture;

        /// <summary>Decoded camera metadata.</summary>
        public Metadata Metadata => _currentMetadata;

        /// <summary>Whether metadata is valid (barcode successfully decoded).</summary>
        public bool IsMetadataValid => _currentMetadata.IsValid;

        /// <summary>Number of frames decoded.</summary>
        public int DecodeCount => _decodeCount;

        /// <summary>Time since last frame decode.</summary>
        public float TimeSinceLastFrame => Time.time - _lastDecodeTime;

        #endregion

        #region Events

        /// <summary>Fired when a frame is successfully decoded.</summary>
        public event Action<RenderTexture, RenderTexture, Metadata> OnFrameDecoded;

        /// <summary>Fired when metadata is decoded (may be async).</summary>
        public event Action<Metadata> OnMetadataDecoded;

        #endregion

        #region Private Fields

        private RenderTexture _colorTexture;
        private RenderTexture _depthTexture;
        private Material _demuxMaterial;
        private GraphicsBuffer _decodeBuffer;
        private Metadata[] _readbackArray = new Metadata[1];
        private Metadata _currentMetadata;
        private int _decodeCount;
        private float _lastDecodeTime;
        private bool _isInitialized;

        // Shader property IDs
        private static readonly int _Margin = Shader.PropertyToID("_Margin");
        private static readonly int _DepthRange = Shader.PropertyToID("_DepthRange");

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // Load demux shader if not assigned
            if (_demuxShader == null)
            {
                _demuxShader = Shader.Find("Hidden/Metavido/Demux");
                if (_demuxShader == null)
                {
                    LogWarning("Metavido Demux shader not found");
                }
            }

            // Load metadata decoder shader if not assigned
            if (_metadataDecoderShader == null)
            {
                _metadataDecoderShader = Resources.Load<ComputeShader>("MetadataDecoder");
            }

            // Create demux material
            if (_demuxShader != null)
            {
                _demuxMaterial = new Material(_demuxShader);
            }

            // Create metadata decode buffer (12 floats)
            _decodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 12 * sizeof(float));

            _isInitialized = true;
            Log("Decoder initialized");
        }

        private void Cleanup()
        {
            if (_demuxMaterial != null)
            {
                Destroy(_demuxMaterial);
                _demuxMaterial = null;
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

            if (_decodeBuffer != null)
            {
                _decodeBuffer.Dispose();
                _decodeBuffer = null;
            }
        }

        #endregion

        #region Decoding

        /// <summary>
        /// Decode a Metavido-encoded frame.
        /// </summary>
        /// <param name="encodedFrame">The multiplexed Metavido frame from WebRTC</param>
        public void DecodeFrame(Texture encodedFrame)
        {
            if (!_isInitialized || encodedFrame == null) return;

            // Ensure output textures are sized correctly
            EnsureOutputTextures(encodedFrame.width, encodedFrame.height);

            // Demux color and depth
            DemuxTextures(encodedFrame);

            // Decode metadata
            if (_useAsyncReadback)
            {
                DecodeMetadataAsync(encodedFrame);
            }
            else
            {
                DecodeMetadataSync(encodedFrame);
            }

            _decodeCount++;
            _lastDecodeTime = Time.time;

            OnFrameDecoded?.Invoke(_colorTexture, _depthTexture, _currentMetadata);
        }

        /// <summary>
        /// Decode from raw byte array (WebRTC frame data).
        /// </summary>
        public void DecodeFrame(byte[] frameData, int width, int height)
        {
            if (frameData == null || frameData.Length == 0) return;

            // Create temporary texture from bytes
            var tempTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tempTex.LoadRawTextureData(frameData);
            tempTex.Apply();

            DecodeFrame(tempTex);

            // Cleanup temporary texture to prevent memory leak
            Destroy(tempTex);
        }

        private void EnsureOutputTextures(int sourceWidth, int sourceHeight)
        {
            int colorWidth = sourceWidth / 2;
            int colorHeight = sourceHeight;
            int depthWidth = sourceWidth / 2;
            int depthHeight = sourceHeight / 2;

            // Create or resize color texture
            if (_colorTexture == null ||
                _colorTexture.width != colorWidth ||
                _colorTexture.height != colorHeight)
            {
                if (_colorTexture != null)
                {
                    _colorTexture.Release();
                    Destroy(_colorTexture);
                }

                _colorTexture = new RenderTexture(colorWidth, colorHeight, 0, RenderTextureFormat.ARGB32);
                _colorTexture.name = "DecodedColor";
                _colorTexture.Create();

                Log($"Created color texture: {colorWidth}x{colorHeight}");
            }

            // Create or resize depth texture
            if (_depthTexture == null ||
                _depthTexture.width != depthWidth ||
                _depthTexture.height != depthHeight)
            {
                if (_depthTexture != null)
                {
                    _depthTexture.Release();
                    Destroy(_depthTexture);
                }

                _depthTexture = new RenderTexture(depthWidth, depthHeight, 0, RenderTextureFormat.RHalf);
                _depthTexture.name = "DecodedDepth";
                _depthTexture.Create();

                Log($"Created depth texture: {depthWidth}x{depthHeight}");
            }
        }

        private void DemuxTextures(Texture source)
        {
            if (_demuxMaterial == null)
            {
                // Fallback: just copy to color
                Graphics.Blit(source, _colorTexture);
                return;
            }

            _demuxMaterial.SetInteger(_Margin, _margin);
            _demuxMaterial.SetVector(_DepthRange, _currentMetadata.DepthRange);

            // Pass 0: Extract color (left half)
            Graphics.Blit(source, _colorTexture, _demuxMaterial, 0);

            // Pass 1: Extract depth (right bottom quadrant)
            Graphics.Blit(source, _depthTexture, _demuxMaterial, 1);
        }

        private void DecodeMetadataSync(Texture source)
        {
            if (_metadataDecoderShader == null)
            {
                // Use fallback metadata
                _currentMetadata = CreateFallbackMetadata();
                return;
            }

            // Dispatch compute shader
            _metadataDecoderShader.SetTexture(0, "Source", source);
            _metadataDecoderShader.SetBuffer(0, "Output", _decodeBuffer);
            _metadataDecoderShader.Dispatch(0, 1, 1, 1);

            // Synchronized readback (slow but immediate)
            _decodeBuffer.GetData(_readbackArray);
            _currentMetadata = _readbackArray[0];

            OnMetadataDecoded?.Invoke(_currentMetadata);
        }

        private void DecodeMetadataAsync(Texture source)
        {
            if (_metadataDecoderShader == null)
            {
                _currentMetadata = CreateFallbackMetadata();
                OnMetadataDecoded?.Invoke(_currentMetadata);
                return;
            }

            // Dispatch compute shader
            _metadataDecoderShader.SetTexture(0, "Source", source);
            _metadataDecoderShader.SetBuffer(0, "Output", _decodeBuffer);
            _metadataDecoderShader.Dispatch(0, 1, 1, 1);

            // Async readback (non-blocking)
            AsyncGPUReadback.Request(_decodeBuffer, OnReadbackComplete);
        }

        private void OnReadbackComplete(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                LogWarning("Async GPU readback failed");
                return;
            }

            var data = request.GetData<Metadata>();
            if (data.Length > 0)
            {
                _currentMetadata = data[0];
                OnMetadataDecoded?.Invoke(_currentMetadata);
            }
        }

        private Metadata CreateFallbackMetadata()
        {
            // Create metadata from main camera as fallback
            var cam = Camera.main;
            if (cam != null)
            {
                return new Metadata(cam.transform, cam.projectionMatrix, new Vector2(0.1f, 5f));
            }

            return default;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get inverse view matrix from decoded metadata.
        /// </summary>
        public Matrix4x4 GetInverseViewMatrix()
        {
            if (!_currentMetadata.IsValid) return Matrix4x4.identity;

            var viewMatrix = Matrix4x4.TRS(
                _currentMetadata.CameraPosition,
                _currentMetadata.CameraRotation,
                Vector3.one
            );

            return viewMatrix;
        }

        /// <summary>
        /// Get ray parameters for depth-to-world conversion.
        /// </summary>
        public Vector4 GetRayParams()
        {
            if (!_currentMetadata.IsValid)
                return new Vector4(0, 0, 1, 1);

            float tanHalfFov = Mathf.Tan(_currentMetadata.FieldOfView / 2);
            float aspect = 16f / 9f; // Metavido uses 16:9

            return new Vector4(0, 0, tanHalfFov * aspect, tanHalfFov);
        }

        /// <summary>
        /// Bind decoded textures to a VFX graph.
        /// </summary>
        public void BindToVFX(UnityEngine.VFX.VisualEffect vfx)
        {
            if (vfx == null) return;

            if (_colorTexture != null && vfx.HasTexture("ColorMap"))
                vfx.SetTexture("ColorMap", _colorTexture);

            if (_depthTexture != null && vfx.HasTexture("DepthMap"))
                vfx.SetTexture("DepthMap", _depthTexture);

            if (_currentMetadata.IsValid)
            {
                if (vfx.HasMatrix4x4("InverseView"))
                    vfx.SetMatrix4x4("InverseView", GetInverseViewMatrix());

                if (vfx.HasVector4("RayParams"))
                    vfx.SetVector4("RayParams", GetRayParams());

                if (vfx.HasVector2("DepthRange"))
                    vfx.SetVector2("DepthRange", _currentMetadata.DepthRange);
            }
        }

        /// <summary>
        /// Reset decoder state.
        /// </summary>
        public void Reset()
        {
            _decodeCount = 0;
            _currentMetadata = default;
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_logDebugInfo)
                Debug.Log($"[MetavidoWebRTCDecoder] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[MetavidoWebRTCDecoder] {message}");
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        [ContextMenu("Log Current Metadata")]
        private void LogCurrentMetadata()
        {
            Debug.Log($"[MetavidoWebRTCDecoder] Metadata Valid: {_currentMetadata.IsValid}");
            Debug.Log($"  Position: {_currentMetadata.CameraPosition}");
            Debug.Log($"  Rotation: {_currentMetadata.CameraRotation.eulerAngles}");
            Debug.Log($"  FOV: {_currentMetadata.FieldOfView * Mathf.Rad2Deg}°");
            Debug.Log($"  Depth Range: {_currentMetadata.DepthRange}");
        }
#endif

        #endregion
    }
}
