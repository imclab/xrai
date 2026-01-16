// H3MWebRTCReceiver - Receives hologram video streams via WebRTC
// Decodes color/depth textures from remote peer for hologram visualization
//
// Based on Rcam3-WebRTC SimpleWebRTCReceiver

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

#if UNITY_WEBRTC_AVAILABLE
using Unity.WebRTC;
#endif

namespace MetavidoVFX.H3M.Network
{
    /// <summary>
    /// WebRTC receiver for hologram video streams.
    /// Decodes multiplexed color/depth video into separate textures for VFX binding.
    ///
    /// Requires: com.unity.webrtc package
    /// Add "UNITY_WEBRTC_AVAILABLE" to scripting defines to enable.
    /// </summary>
    [RequireComponent(typeof(H3MSignalingClient))]
    public class H3MWebRTCReceiver : MonoBehaviour
    {
        #region Serialized Fields

        [Header("WebRTC Settings")]
        [SerializeField] private string _roomName = "h3m-room";
        [SerializeField] private bool _useStunServer = true;
        [SerializeField] private string _stunServerUrl = "stun:stun.l.google.com:19302";

        [Header("Decoding")]
        [SerializeField] private Material _colorDecoderMaterial;
        [SerializeField] private Material _depthDecoderMaterial;
        [SerializeField] private Texture3D _colorLUT;

        [Header("Output Dimensions")]
        [SerializeField] private int _outputWidth = 1280;
        [SerializeField] private int _outputHeight = 720;

        [Header("Debug")]
        [SerializeField] private bool _logDebugInfo = true;

        #endregion

        #region Public Properties

        /// <summary>Decoded color texture for VFX binding.</summary>
        public RenderTexture ColorTexture => _decoded.color;

        /// <summary>Decoded depth texture for VFX binding.</summary>
        public RenderTexture DepthTexture => _decoded.depth;

        /// <summary>Stream metadata (camera position, projection, etc).</summary>
        public ref readonly H3MStreamMetadata Metadata => ref _metadata;

        /// <summary>True when connected to remote peer.</summary>
        public bool IsConnected => _isConnected;

        /// <summary>ID of connected peer.</summary>
        public string ConnectedPeerId => _connectedPeerId;

        #endregion

        #region Events

        public event Action OnStreamStarted;
        public event Action OnStreamEnded;
        public event Action<H3MStreamMetadata> OnMetadataReceived;

        #endregion

        #region Private Members

        private H3MSignalingClient _signalingClient;
        private bool _isConnected = false;
        private string _connectedPeerId = "";

        #if UNITY_WEBRTC_AVAILABLE
        private RTCPeerConnection _peerConnection;
        private RTCDataChannel _dataChannel;
        private VideoStreamTrack _videoTrack;
        private Dictionary<string, RTCIceCandidate> _pendingCandidates =
            new Dictionary<string, RTCIceCandidate>();
        #endif

        private RenderTexture _remoteTexture;
        private (RenderTexture color, RenderTexture depth) _decoded;
        private H3MStreamMetadata _metadata = H3MStreamMetadata.Default;
        private string _pendingMetadata = null;

        // Shader property IDs
        static class ShaderID
        {
            public static readonly int LutTexture = Shader.PropertyToID("_LutTexture");
            public static readonly int DepthRange = Shader.PropertyToID("_DepthRange");
        }

        #endregion

        #region MonoBehaviour

        private void Start()
        {
            _signalingClient = GetComponent<H3MSignalingClient>();
            AllocateTextures();
            SetupSignalingCallbacks();

            // Connect to room
            _signalingClient.Connect(_roomName);

            Log("H3MWebRTCReceiver initialized");
        }

        private void OnDestroy()
        {
            CleanupConnection();
            ReleaseTextures();
        }

        private void Update()
        {
            #if UNITY_WEBRTC_AVAILABLE
            // Process WebRTC events
            StartCoroutine(WebRTC.Update());
            #endif

            // Process pending metadata
            ProcessPendingMetadata();
        }

        #endregion

        #region Initialization

        private void AllocateTextures()
        {
            // Remote video buffer
            _remoteTexture = new RenderTexture(_outputWidth, _outputHeight, 0, RenderTextureFormat.ARGB32);
            _remoteTexture.wrapMode = TextureWrapMode.Clamp;
            _remoteTexture.Create();

            // Decoded color (full resolution)
            _decoded.color = new RenderTexture(_outputWidth / 2, _outputHeight, 0, RenderTextureFormat.ARGB32);
            _decoded.color.wrapMode = TextureWrapMode.Clamp;
            _decoded.color.Create();

            // Decoded depth (half resolution, single channel)
            _decoded.depth = new RenderTexture(_outputWidth / 2, _outputHeight / 2, 0, RenderTextureFormat.RHalf);
            _decoded.depth.wrapMode = TextureWrapMode.Clamp;
            _decoded.depth.Create();
        }

        private void ReleaseTextures()
        {
            if (_remoteTexture != null) { _remoteTexture.Release(); Destroy(_remoteTexture); }
            if (_decoded.color != null) { _decoded.color.Release(); Destroy(_decoded.color); }
            if (_decoded.depth != null) { _decoded.depth.Release(); Destroy(_decoded.depth); }
        }

        private void SetupSignalingCallbacks()
        {
            _signalingClient.OnPeerConnected += OnPeerConnected;
            _signalingClient.OnPeerDisconnected += OnPeerDisconnected;
            _signalingClient.OnSignalingOffer += OnSignalingOffer;
            _signalingClient.OnSignalingAnswer += OnSignalingAnswer;

            #if UNITY_WEBRTC_AVAILABLE
            _signalingClient.OnSignalingCandidate += OnSignalingCandidate;
            #endif
        }

        #endregion

        #region WebRTC Setup

        #if UNITY_WEBRTC_AVAILABLE
        private RTCConfiguration CreateRTCConfiguration()
        {
            var config = default(RTCConfiguration);

            if (_useStunServer)
            {
                config.iceServers = new RTCIceServer[]
                {
                    new RTCIceServer { urls = new string[] { _stunServerUrl } }
                };
            }

            return config;
        }

        private void CreatePeerConnection(string remotePeerId)
        {
            var config = CreateRTCConfiguration();
            _peerConnection = new RTCPeerConnection(ref config);

            _peerConnection.OnIceCandidate = candidate =>
            {
                _signalingClient.SendCandidate(remotePeerId, candidate);
            };

            _peerConnection.OnConnectionStateChange = state =>
            {
                Log($"Connection state: {state}");
                bool wasConnected = _isConnected;
                _isConnected = (state == RTCPeerConnectionState.Connected);

                if (_isConnected && !wasConnected)
                {
                    _connectedPeerId = remotePeerId;
                    OnStreamStarted?.Invoke();
                }
                else if (!_isConnected && wasConnected)
                {
                    OnStreamEnded?.Invoke();
                }
            };

            _peerConnection.OnTrack = e =>
            {
                if (e.Track is VideoStreamTrack videoTrack)
                {
                    Log("Received video track");
                    _videoTrack = videoTrack;
                    _videoTrack.OnVideoReceived += OnVideoFrameReceived;
                }
            };

            _peerConnection.OnDataChannel = channel =>
            {
                Log($"Received data channel: {channel.Label}");
                _dataChannel = channel;
                _dataChannel.OnMessage = OnDataChannelMessage;
            };

            // Add any pending ICE candidates
            if (_pendingCandidates.ContainsKey(remotePeerId))
            {
                _peerConnection.AddIceCandidate(_pendingCandidates[remotePeerId]);
                _pendingCandidates.Remove(remotePeerId);
            }
        }
        #endif

        private void CleanupConnection()
        {
            #if UNITY_WEBRTC_AVAILABLE
            if (_dataChannel != null)
            {
                _dataChannel.Close();
                _dataChannel = null;
            }

            if (_videoTrack != null)
            {
                _videoTrack.OnVideoReceived -= OnVideoFrameReceived;
                _videoTrack.Dispose();
                _videoTrack = null;
            }

            if (_peerConnection != null)
            {
                _peerConnection.Close();
                _peerConnection = null;
            }
            #endif

            _isConnected = false;
            _connectedPeerId = "";
        }

        #endregion

        #region Signaling Handlers

        private void OnPeerConnected(string peerId)
        {
            Log($"Peer connected: {peerId}");
        }

        private void OnPeerDisconnected(string peerId)
        {
            Log($"Peer disconnected: {peerId}");
            if (peerId == _connectedPeerId)
            {
                CleanupConnection();
            }
        }

        private void OnSignalingOffer(string peerId, string sdp)
        {
            Log($"Received offer from {peerId}");

            #if UNITY_WEBRTC_AVAILABLE
            if (_peerConnection == null)
            {
                CreatePeerConnection(peerId);
            }

            var desc = new RTCSessionDescription { type = RTCSdpType.Offer, sdp = sdp };
            StartCoroutine(HandleOffer(desc, peerId));
            #endif
        }

        private void OnSignalingAnswer(string peerId, string sdp)
        {
            Log($"Received answer from {peerId}");

            #if UNITY_WEBRTC_AVAILABLE
            var desc = new RTCSessionDescription { type = RTCSdpType.Answer, sdp = sdp };
            StartCoroutine(SetRemoteDescription(desc));
            #endif
        }

        #if UNITY_WEBRTC_AVAILABLE
        private void OnSignalingCandidate(string peerId, RTCIceCandidateInit candidateInit)
        {
            var candidate = new RTCIceCandidate(candidateInit);

            if (_peerConnection != null)
            {
                Log($"Adding ICE candidate from {peerId}");
                _peerConnection.AddIceCandidate(candidate);
            }
            else
            {
                Log($"Storing ICE candidate from {peerId}");
                _pendingCandidates[peerId] = candidate;
            }
        }
        #endif

        #endregion

        #region WebRTC Coroutines

        #if UNITY_WEBRTC_AVAILABLE
        private IEnumerator HandleOffer(RTCSessionDescription offer, string peerId)
        {
            // Set remote description
            var setRemoteOp = _peerConnection.SetRemoteDescription(ref offer);
            yield return setRemoteOp;

            if (setRemoteOp.IsError)
            {
                LogError($"Failed to set remote description: {setRemoteOp.Error.message}");
                yield break;
            }

            // Create answer
            var createAnswerOp = _peerConnection.CreateAnswer();
            yield return createAnswerOp;

            if (createAnswerOp.IsError)
            {
                LogError($"Failed to create answer: {createAnswerOp.Error.message}");
                yield break;
            }

            // Set local description
            var answer = createAnswerOp.Desc;
            var setLocalOp = _peerConnection.SetLocalDescription(ref answer);
            yield return setLocalOp;

            if (setLocalOp.IsError)
            {
                LogError($"Failed to set local description: {setLocalOp.Error.message}");
                yield break;
            }

            // Send answer
            _signalingClient.SendAnswer(peerId, answer.sdp);
        }

        private IEnumerator SetRemoteDescription(RTCSessionDescription desc)
        {
            var op = _peerConnection.SetRemoteDescription(ref desc);
            yield return op;

            if (op.IsError)
            {
                LogError($"Failed to set remote description: {op.Error.message}");
            }
        }
        #endif

        #endregion

        #region Video Processing

        private void OnVideoFrameReceived(Texture texture)
        {
            // Copy to buffer
            Graphics.CopyTexture(texture, _remoteTexture);

            // Decode multiplexed frame
            DecodeFrame();
        }

        private void DecodeFrame()
        {
            if (_remoteTexture == null) return;

            // Decode color
            if (_colorDecoderMaterial != null)
            {
                if (_colorLUT != null)
                    _colorDecoderMaterial.SetTexture(ShaderID.LutTexture, _colorLUT);

                Graphics.Blit(_remoteTexture, _decoded.color, _colorDecoderMaterial);
            }
            else
            {
                // Simple copy if no decoder
                Graphics.Blit(_remoteTexture, _decoded.color);
            }

            // Decode depth
            if (_depthDecoderMaterial != null)
            {
                _depthDecoderMaterial.SetVector(ShaderID.DepthRange, _metadata.DepthRange);
                Graphics.Blit(_remoteTexture, _decoded.depth, _depthDecoderMaterial);
            }
        }

        #endregion

        #region Metadata Processing

        private void OnDataChannelMessage(byte[] bytes)
        {
            // Store for main thread processing
            _pendingMetadata = Encoding.UTF8.GetString(bytes);
        }

        private void ProcessPendingMetadata()
        {
            if (_pendingMetadata == null) return;

            try
            {
                _metadata = H3MStreamMetadata.Deserialize(_pendingMetadata);
                _pendingMetadata = null;
                OnMetadataReceived?.Invoke(_metadata);
            }
            catch (Exception e)
            {
                LogError($"Failed to parse metadata: {e.Message}");
                _pendingMetadata = null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Send message to connected peer via data channel.
        /// </summary>
        public void SendMessage(string message)
        {
            #if UNITY_WEBRTC_AVAILABLE
            if (_dataChannel != null && _dataChannel.ReadyState == RTCDataChannelState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                _dataChannel.Send(bytes);
            }
            else
            {
                LogWarning("Cannot send message - data channel not ready");
            }
            #endif
        }

        /// <summary>
        /// Manually connect to a different room.
        /// </summary>
        public void ConnectToRoom(string roomName)
        {
            CleanupConnection();
            _roomName = roomName;
            _signalingClient.Disconnect();
            _signalingClient.Connect(roomName);
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_logDebugInfo)
                Debug.Log($"[H3MWebRTCReceiver] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[H3MWebRTCReceiver] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[H3MWebRTCReceiver] {message}");
        }

        #endregion
    }
}
