// HologramConferenceManager - High-level manager for hologram video conferencing
// Coordinates signaling, WebRTC connections, and hologram rendering for multiple peers

using UnityEngine;
using UnityEngine.VFX;
using System;
using System.Collections.Generic;

#if UNITY_WEBRTC_AVAILABLE
using Unity.WebRTC;
#endif

namespace MetavidoVFX.H3M.Network
{
    /// <summary>
    /// High-level manager for hologram video conferencing.
    /// Handles multi-user connections where each remote peer appears as a hologram.
    ///
    /// Features:
    /// - Automatic peer discovery via signaling
    /// - One hologram VFX per remote peer
    /// - Local capture via ARCameraWebRTCCapture
    /// - Metadata sync (camera pose, depth range)
    ///
    /// Usage:
    /// 1. Add to scene with H3MSignalingClient and ARCameraWebRTCCapture
    /// 2. Assign hologram VFX prefab
    /// 3. Call JoinRoom(roomName) to start conferencing
    /// </summary>
    public class HologramConferenceManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Signaling")]
        [SerializeField] private H3MSignalingClient _signalingClient;
        [SerializeField] private string _roomName = "h3m-room";
        [SerializeField] private bool _autoJoinOnStart = false;

        [Header("Local Capture")]
        [SerializeField] private ARCameraWebRTCCapture _localCapture;
        [SerializeField] private bool _includeDepth = true;

        [Header("Remote Holograms")]
        [Tooltip("Prefab with VisualEffect + H3MWebRTCVFXBinder")]
        [SerializeField] private GameObject _hologramPrefab;
        [SerializeField] private Transform _hologramContainer;
        [SerializeField] private float _hologramScale = 0.15f;
        [SerializeField] private float _hologramSpacing = 1.5f;

        [Header("WebRTC Settings")]
        [SerializeField] private bool _useStunServer = true;
        [SerializeField] private string _stunServerUrl = "stun:stun.l.google.com:19302";

        [Header("Debug")]
        [SerializeField] private bool _logDebugInfo = true;
        [SerializeField] private bool _showDebugGUI = false;

        #endregion

        #region Public Properties

        /// <summary>True when connected to signaling and room joined.</summary>
        public bool IsInRoom => _signalingClient?.IsConnected ?? false;

        /// <summary>Current room name.</summary>
        public string CurrentRoom => _roomName;

        /// <summary>Number of remote peers (holograms).</summary>
        public int PeerCount => _remoteHolograms.Count;

        /// <summary>Local peer ID.</summary>
        public string LocalPeerId => _localPeerId;

        #endregion

        #region Events

        public event Action<string> OnPeerJoined;
        public event Action<string> OnPeerLeft;
        public event Action OnRoomJoined;
        public event Action OnRoomLeft;

        #endregion

        #region Private Members

        private string _localPeerId;
        private Dictionary<string, RemoteHologram> _remoteHolograms = new Dictionary<string, RemoteHologram>();

        #if UNITY_WEBRTC_AVAILABLE
        private Dictionary<string, RTCPeerConnection> _peerConnections = new Dictionary<string, RTCPeerConnection>();
        #endif

        // Remote hologram data
        private class RemoteHologram
        {
            public string peerId;
            public GameObject gameObject;
            public VisualEffect vfx;
            public H3MWebRTCVFXBinder binder;
            public RenderTexture colorTexture;
            public RenderTexture depthTexture;
            public H3MStreamMetadata metadata;
        }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            // Generate unique peer ID
            _localPeerId = $"peer-{System.Guid.NewGuid().ToString().Substring(0, 8)}";

            if (_signalingClient == null)
                _signalingClient = GetComponent<H3MSignalingClient>();

            if (_hologramContainer == null)
                _hologramContainer = transform;
        }

        private void Start()
        {
            SetupSignalingCallbacks();

            if (_autoJoinOnStart)
            {
                JoinRoom(_roomName);
            }
        }

        private void OnDestroy()
        {
            LeaveRoom();
            CleanupAllHolograms();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Join a conference room.
        /// </summary>
        public void JoinRoom(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                LogError("Room name cannot be empty");
                return;
            }

            _roomName = roomName;
            Log($"Joining room: {roomName} as {_localPeerId}");
            _signalingClient.Connect(roomName);
        }

        /// <summary>
        /// Leave current room.
        /// </summary>
        public void LeaveRoom()
        {
            Log("Leaving room");
            _signalingClient.Disconnect();
            CleanupAllHolograms();
            OnRoomLeft?.Invoke();
        }

        /// <summary>
        /// Get remote hologram by peer ID.
        /// </summary>
        public GameObject GetRemoteHologram(string peerId)
        {
            return _remoteHolograms.TryGetValue(peerId, out var hologram) ? hologram.gameObject : null;
        }

        #endregion

        #region Signaling Callbacks

        private void SetupSignalingCallbacks()
        {
            _signalingClient.OnConnected += OnSignalingConnected;
            _signalingClient.OnDisconnected += OnSignalingDisconnected;
            _signalingClient.OnPeerConnected += OnRemotePeerJoined;
            _signalingClient.OnPeerDisconnected += OnRemotePeerLeft;
            _signalingClient.OnSignalingOffer += OnSignalingOffer;
            _signalingClient.OnSignalingAnswer += OnSignalingAnswer;

            #if UNITY_WEBRTC_AVAILABLE
            _signalingClient.OnSignalingCandidate += OnSignalingCandidate;
            #endif
        }

        private void OnSignalingConnected()
        {
            Log("Connected to signaling server");
            OnRoomJoined?.Invoke();
        }

        private void OnSignalingDisconnected()
        {
            Log("Disconnected from signaling server");
        }

        private void OnRemotePeerJoined(string peerId)
        {
            Log($"Remote peer joined: {peerId}");

            // Create hologram for this peer
            CreateRemoteHologram(peerId);

            // Initiate WebRTC connection (we're the offerer)
            #if UNITY_WEBRTC_AVAILABLE
            StartCoroutine(InitiateConnection(peerId));
            #endif

            OnPeerJoined?.Invoke(peerId);
        }

        private void OnRemotePeerLeft(string peerId)
        {
            Log($"Remote peer left: {peerId}");
            DestroyRemoteHologram(peerId);
            OnPeerLeft?.Invoke(peerId);
        }

        private void OnSignalingOffer(string peerId, string sdp)
        {
            Log($"Received offer from {peerId}");
            #if UNITY_WEBRTC_AVAILABLE
            StartCoroutine(HandleOffer(peerId, sdp));
            #endif
        }

        private void OnSignalingAnswer(string peerId, string sdp)
        {
            Log($"Received answer from {peerId}");
            #if UNITY_WEBRTC_AVAILABLE
            StartCoroutine(HandleAnswer(peerId, sdp));
            #endif
        }

        #if UNITY_WEBRTC_AVAILABLE
        private void OnSignalingCandidate(string peerId, RTCIceCandidateInit candidateInit)
        {
            if (_peerConnections.TryGetValue(peerId, out var pc))
            {
                pc.AddIceCandidate(new RTCIceCandidate(candidateInit));
            }
        }
        #endif

        #endregion

        #region WebRTC Connection

        #if UNITY_WEBRTC_AVAILABLE
        private System.Collections.IEnumerator InitiateConnection(string peerId)
        {
            // Create peer connection
            var pc = CreatePeerConnection(peerId);
            _peerConnections[peerId] = pc;

            // Add local video track
            if (_localCapture != null && _localCapture.LastColorTexture != null)
            {
                // TODO: Create video track from local capture
                // var videoTrack = new VideoStreamTrack(_localCapture.LastColorTexture);
                // pc.AddTrack(videoTrack);
            }

            // Create data channel for metadata
            var dataChannel = pc.CreateDataChannel("metadata");

            // Create offer
            var offerOp = pc.CreateOffer();
            yield return offerOp;

            if (offerOp.IsError)
            {
                LogError($"Failed to create offer: {offerOp.Error.message}");
                yield break;
            }

            // Set local description
            var offer = offerOp.Desc;
            var setLocalOp = pc.SetLocalDescription(ref offer);
            yield return setLocalOp;

            if (setLocalOp.IsError)
            {
                LogError($"Failed to set local description: {setLocalOp.Error.message}");
                yield break;
            }

            // Send offer via signaling
            _signalingClient.SendOffer(peerId, offer.sdp);
        }

        private System.Collections.IEnumerator HandleOffer(string peerId, string sdp)
        {
            // Create peer connection if needed
            if (!_peerConnections.TryGetValue(peerId, out var pc))
            {
                pc = CreatePeerConnection(peerId);
                _peerConnections[peerId] = pc;

                // Create hologram if not exists
                if (!_remoteHolograms.ContainsKey(peerId))
                {
                    CreateRemoteHologram(peerId);
                }
            }

            // Set remote description
            var offer = new RTCSessionDescription { type = RTCSdpType.Offer, sdp = sdp };
            var setRemoteOp = pc.SetRemoteDescription(ref offer);
            yield return setRemoteOp;

            if (setRemoteOp.IsError)
            {
                LogError($"Failed to set remote description: {setRemoteOp.Error.message}");
                yield break;
            }

            // Create answer
            var answerOp = pc.CreateAnswer();
            yield return answerOp;

            if (answerOp.IsError)
            {
                LogError($"Failed to create answer: {answerOp.Error.message}");
                yield break;
            }

            // Set local description
            var answer = answerOp.Desc;
            var setLocalOp = pc.SetLocalDescription(ref answer);
            yield return setLocalOp;

            if (setLocalOp.IsError)
            {
                LogError($"Failed to set local description: {setLocalOp.Error.message}");
                yield break;
            }

            // Send answer via signaling
            _signalingClient.SendAnswer(peerId, answer.sdp);
        }

        private System.Collections.IEnumerator HandleAnswer(string peerId, string sdp)
        {
            if (!_peerConnections.TryGetValue(peerId, out var pc))
            {
                LogError($"No peer connection for {peerId}");
                yield break;
            }

            var answer = new RTCSessionDescription { type = RTCSdpType.Answer, sdp = sdp };
            var op = pc.SetRemoteDescription(ref answer);
            yield return op;

            if (op.IsError)
            {
                LogError($"Failed to set remote answer: {op.Error.message}");
            }
        }

        private RTCPeerConnection CreatePeerConnection(string peerId)
        {
            var config = default(RTCConfiguration);
            if (_useStunServer)
            {
                config.iceServers = new RTCIceServer[]
                {
                    new RTCIceServer { urls = new string[] { _stunServerUrl } }
                };
            }

            var pc = new RTCPeerConnection(ref config);

            pc.OnIceCandidate = candidate =>
            {
                _signalingClient.SendCandidate(peerId, candidate);
            };

            pc.OnConnectionStateChange = state =>
            {
                Log($"Connection to {peerId}: {state}");
            };

            pc.OnTrack = e =>
            {
                if (e.Track is VideoStreamTrack videoTrack)
                {
                    Log($"Received video track from {peerId}");
                    if (_remoteHolograms.TryGetValue(peerId, out var hologram))
                    {
                        videoTrack.OnVideoReceived += tex => OnRemoteVideoReceived(peerId, tex);
                    }
                }
            };

            return pc;
        }

        private void OnRemoteVideoReceived(string peerId, Texture texture)
        {
            if (!_remoteHolograms.TryGetValue(peerId, out var hologram)) return;

            // Copy to hologram's render texture
            if (hologram.colorTexture != null)
            {
                Graphics.CopyTexture(texture, hologram.colorTexture);
            }
        }
        #endif

        #endregion

        #region Hologram Management

        private void CreateRemoteHologram(string peerId)
        {
            if (_remoteHolograms.ContainsKey(peerId))
            {
                Log($"Hologram already exists for {peerId}");
                return;
            }

            if (_hologramPrefab == null)
            {
                LogError("Hologram prefab not assigned");
                return;
            }

            // Calculate position
            int index = _remoteHolograms.Count;
            var position = _hologramContainer.position + Vector3.right * (index * _hologramSpacing);

            // Instantiate hologram
            var go = Instantiate(_hologramPrefab, position, Quaternion.identity, _hologramContainer);
            go.name = $"RemoteHologram_{peerId}";
            go.transform.localScale = Vector3.one * _hologramScale;

            // Get components
            var vfx = go.GetComponent<VisualEffect>();
            var binder = go.GetComponent<H3MWebRTCVFXBinder>();

            // Create textures
            var colorTex = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
            var depthTex = new RenderTexture(640, 360, 0, RenderTextureFormat.RHalf);
            colorTex.Create();
            depthTex.Create();

            // Store hologram data
            var hologram = new RemoteHologram
            {
                peerId = peerId,
                gameObject = go,
                vfx = vfx,
                binder = binder,
                colorTexture = colorTex,
                depthTexture = depthTex,
                metadata = H3MStreamMetadata.Default
            };
            _remoteHolograms[peerId] = hologram;

            // Bind textures to VFX
            if (binder != null)
            {
                binder.SetTextures(colorTex, depthTex);
            }

            Log($"Created hologram for {peerId}");
        }

        private void DestroyRemoteHologram(string peerId)
        {
            if (!_remoteHolograms.TryGetValue(peerId, out var hologram)) return;

            // Cleanup WebRTC
            #if UNITY_WEBRTC_AVAILABLE
            if (_peerConnections.TryGetValue(peerId, out var pc))
            {
                pc.Close();
                _peerConnections.Remove(peerId);
            }
            #endif

            // Cleanup textures
            if (hologram.colorTexture != null)
            {
                hologram.colorTexture.Release();
                Destroy(hologram.colorTexture);
            }
            if (hologram.depthTexture != null)
            {
                hologram.depthTexture.Release();
                Destroy(hologram.depthTexture);
            }

            // Destroy GameObject
            if (hologram.gameObject != null)
            {
                Destroy(hologram.gameObject);
            }

            _remoteHolograms.Remove(peerId);
            Log($"Destroyed hologram for {peerId}");
        }

        private void CleanupAllHolograms()
        {
            var peerIds = new List<string>(_remoteHolograms.Keys);
            foreach (var peerId in peerIds)
            {
                DestroyRemoteHologram(peerId);
            }
        }

        #endregion

        #region Debug GUI

        private void OnGUI()
        {
            if (!_showDebugGUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>Hologram Conference Manager</b>");
            GUILayout.Label($"Local Peer: {_localPeerId}");
            GUILayout.Label($"Room: {_roomName}");
            GUILayout.Label($"Connected: {IsInRoom}");
            GUILayout.Label($"Remote Peers: {_remoteHolograms.Count}");

            GUILayout.Space(10);

            if (!IsInRoom)
            {
                if (GUILayout.Button("Join Room"))
                {
                    JoinRoom(_roomName);
                }
            }
            else
            {
                if (GUILayout.Button("Leave Room"))
                {
                    LeaveRoom();
                }
            }

            GUILayout.Space(10);

            GUILayout.Label("<b>Remote Holograms:</b>");
            foreach (var kvp in _remoteHolograms)
            {
                GUILayout.Label($"  - {kvp.Key}");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_logDebugInfo)
                Debug.Log($"[HologramConferenceManager] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[HologramConferenceManager] {message}");
        }

        #endregion
    }
}
