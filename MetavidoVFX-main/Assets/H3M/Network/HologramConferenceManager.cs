// HologramConferenceManager - High-level manager for hologram video conferencing
// Uses Byn.Awrtc (WebRtcVideoChat) for WebRTC conferencing with built-in signaling

using UnityEngine;
using UnityEngine.VFX;
using System;
using System.Collections.Generic;
using Byn.Awrtc;
using Byn.Awrtc.Unity;
using Byn.Unity.Examples;
using XRRAI.Hologram;

namespace XRRAI.Hologram
{
    /// <summary>
    /// High-level manager for hologram video conferencing.
    /// Handles multi-user connections where each remote peer appears as a hologram.
    ///
    /// Uses WebRtcVideoChat (Byn.Awrtc) for WebRTC with built-in signaling.
    /// No separate signaling server needed - uses wss://s.y-not.app/conferenceapp
    ///
    /// Features:
    /// - Automatic peer discovery via built-in signaling
    /// - One hologram VFX per remote peer
    /// - Local capture via ARCameraWebRTCCapture
    /// - Metadata sync via data channel
    ///
    /// Usage:
    /// 1. Add to scene with ARCameraWebRTCCapture
    /// 2. Assign hologram VFX prefab
    /// 3. Call JoinRoom(roomName) to start conferencing
    /// </summary>
    public class HologramConferenceManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Conference Settings")]
        [SerializeField] private string _roomName = "h3m-hologram";
        [SerializeField] private bool _autoJoinOnStart = false;
        [SerializeField] private bool _enableVideo = true;
        [SerializeField] private bool _enableAudio = false;

        [Header("Local Capture")]
        [SerializeField] private ARCameraWebRTCCapture _localCapture;
        [SerializeField] private bool _includeDepth = true;

        [Header("Remote Holograms")]
        [Tooltip("Prefab with VisualEffect + H3MWebRTCVFXBinder")]
        [SerializeField] private GameObject _hologramPrefab;
        [SerializeField] private Transform _hologramContainer;
        [SerializeField] private float _hologramScale = 0.15f;
        [SerializeField] private float _hologramSpacing = 1.5f;

        [Header("Debug")]
        [SerializeField] private bool _logDebugInfo = true;
        [SerializeField] private bool _showDebugGUI = false;

        [Header("Runtime Status (Read-Only)")]
        [SerializeField, Tooltip("Currently in a room")]
        private bool _isInRoomDisplay = false;
        [SerializeField, Tooltip("Current room name")]
        private string _currentRoomDisplay = "None";
        [SerializeField, Tooltip("Number of connected peers")]
        private int _peerCountDisplay = 0;
        [SerializeField, Tooltip("Local peer ID")]
        private string _localPeerIdDisplay = "";
        [SerializeField, Tooltip("WebRTC initialized")]
        private bool _isInitializedDisplay = false;

        #endregion

        #region Public Properties

        /// <summary>True when call is active and connected to room.</summary>
        public bool IsInRoom => _call != null;

        /// <summary>Current room name.</summary>
        public string CurrentRoom => _roomName;

        /// <summary>Number of remote peers (holograms).</summary>
        public int PeerCount => _remoteHolograms.Count;

        /// <summary>Local peer ID.</summary>
        public string LocalPeerId => _localPeerId;

        /// <summary>Connection count (remote peers).</summary>
        public int ConnectionCount => _remoteHolograms.Count;

        #endregion

        #region Events

        public event Action<ConnectionId> OnPeerJoined;
        public event Action<ConnectionId> OnPeerLeft;
        public event Action OnRoomJoined;
        public event Action OnRoomLeft;

        #endregion

        #region Private Members

        private string _localPeerId;
        private ICall _call;
        private NetworkConfig _networkConfig;
        private MediaConfig _mediaConfig;
        private bool _isInitialized = false;

        private Dictionary<ConnectionId, RemoteHologram> _remoteHolograms =
            new Dictionary<ConnectionId, RemoteHologram>();

        // Remote hologram data
        private class RemoteHologram
        {
            public ConnectionId connectionId;
            public GameObject gameObject;
            public VisualEffect vfx;
            public H3MWebRTCVFXBinder binder;
            public MetavidoWebRTCDecoder decoder;  // Metavido frame decoder
            public Texture2D videoTexture;
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

            if (_hologramContainer == null)
                _hologramContainer = transform;
        }

        private void Start()
        {
            // Initialize WebRTC factory
            UnityCallFactory.EnsureInit(OnCallFactoryReady, OnCallFactoryFailed);
        }

        private void Update()
        {
            // Update call to process WebRTC events on main thread
            if (_call != null)
            {
                _call.Update();
            }

            UpdateRuntimeStatus();
        }

        private void UpdateRuntimeStatus()
        {
            _isInRoomDisplay = IsInRoom;
            _currentRoomDisplay = IsInRoom ? _roomName : "None";
            _peerCountDisplay = _remoteHolograms.Count;
            _localPeerIdDisplay = _localPeerId ?? "";
            _isInitializedDisplay = _isInitialized;
        }

        private void OnDestroy()
        {
            LeaveRoom();
            CleanupAllHolograms();
        }

        #endregion

        #region Initialization

        private void OnCallFactoryReady()
        {
            Log("WebRTC factory initialized");
            UnityCallFactory.Instance.RequestLogLevel(UnityCallFactory.LogLevel.Info);

            // Setup network config for conference mode
            _networkConfig = new NetworkConfig();
            _networkConfig.SignalingUrl = ExampleGlobals.SignalingConference; // wss://s.y-not.app/conferenceapp
            _networkConfig.IsConference = true;
            _networkConfig.KeepSignalingAlive = true;
            _networkConfig.MaxIceRestart = 5;
            _networkConfig.IceServers.Add(ExampleGlobals.DefaultIceServer);

            // Setup media config
            _mediaConfig = new MediaConfig();
            _mediaConfig.Video = _enableVideo;
            _mediaConfig.Audio = _enableAudio;

            // Use default camera if video enabled
            if (_enableVideo)
            {
                _mediaConfig.VideoDeviceName = UnityCallFactory.Instance.GetDefaultVideoDevice();
            }

            _isInitialized = true;
            Log($"Conference mode ready. Signaling: {_networkConfig.SignalingUrl}");

            if (_autoJoinOnStart)
            {
                JoinRoom(_roomName);
            }
        }

        private void OnCallFactoryFailed(string error)
        {
            LogError($"WebRTC factory failed: {error}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Join a conference room.
        /// </summary>
        public void JoinRoom(string roomName)
        {
            if (!_isInitialized)
            {
                LogError("WebRTC not initialized yet");
                return;
            }

            if (string.IsNullOrEmpty(roomName))
            {
                LogError("Room name cannot be empty");
                return;
            }

            if (_call != null)
            {
                Log("Already in a room, leaving first...");
                LeaveRoom();
            }

            _roomName = roomName;
            Log($"Joining room: {roomName} as {_localPeerId}");

            // Create the call
            _call = UnityCallFactory.Instance.Create(_networkConfig);
            if (_call == null)
            {
                LogError("Failed to create call");
                return;
            }

            _call.CallEvent += OnCallEvent;

            // Configure media
            _call.Configure(_mediaConfig);
        }

        /// <summary>
        /// Leave current room.
        /// </summary>
        public void LeaveRoom()
        {
            if (_call != null)
            {
                Log("Leaving room");
                _call.Dispose();
                _call = null;
            }

            CleanupAllHolograms();
            OnRoomLeft?.Invoke();
        }

        /// <summary>
        /// Get remote hologram by connection ID.
        /// </summary>
        public GameObject GetRemoteHologram(ConnectionId connectionId)
        {
            return _remoteHolograms.TryGetValue(connectionId, out var hologram)
                ? hologram.gameObject : null;
        }

        /// <summary>
        /// Send a text message to all peers.
        /// </summary>
        public void SendMessage(string message)
        {
            if (_call != null && !string.IsNullOrEmpty(message))
            {
                _call.Send(message);
            }
        }

        #endregion

        #region Call Events

        private void OnCallEvent(object sender, CallEventArgs e)
        {
            switch (e.Type)
            {
                case CallEventType.ConfigurationComplete:
                    OnConfigurationComplete();
                    break;

                case CallEventType.ConfigurationFailed:
                    OnConfigurationFailed(e as ErrorEventArgs);
                    break;

                case CallEventType.WaitForIncomingCall:
                    OnWaitingForCall(e as WaitForIncomingCallEventArgs);
                    break;

                case CallEventType.CallAccepted:
                    OnNewConnection(e as CallAcceptedEventArgs);
                    break;

                case CallEventType.CallEnded:
                    OnConnectionEnded(e as CallEndedEventArgs);
                    break;

                case CallEventType.FrameUpdate:
                    OnFrameUpdate(e as FrameUpdateEventArgs);
                    break;

                case CallEventType.Message:
                    OnMessageReceived(e as MessageEventArgs);
                    break;

                case CallEventType.ListeningFailed:
                case CallEventType.ConnectionFailed:
                    OnError(e as ErrorEventArgs);
                    break;
            }
        }

        private void OnConfigurationComplete()
        {
            Log("Configuration complete, listening on room: " + _roomName);

            // Start listening for connections on this room address
            _call.Listen(_roomName);
        }

        private void OnConfigurationFailed(ErrorEventArgs args)
        {
            LogError($"Configuration failed: {args?.Info?.ToString() ?? "Unknown error"}");
            LeaveRoom();
        }

        private void OnWaitingForCall(WaitForIncomingCallEventArgs args)
        {
            Log($"Waiting for connections on: {args.Address}");
            OnRoomJoined?.Invoke();
        }

        private void OnNewConnection(CallAcceptedEventArgs args)
        {
            Log($"New peer connected: {args.ConnectionId}");

            // Create hologram for this peer
            CreateRemoteHologram(args.ConnectionId);

            // Send our peer ID as first message
            _call.Send(_localPeerId);

            OnPeerJoined?.Invoke(args.ConnectionId);
        }

        private void OnConnectionEnded(CallEndedEventArgs args)
        {
            Log($"Peer disconnected: {args.ConnectionId}");
            DestroyRemoteHologram(args.ConnectionId);
            OnPeerLeft?.Invoke(args.ConnectionId);
        }

        private void OnFrameUpdate(FrameUpdateEventArgs args)
        {
            // Skip local frames
            if (!args.IsRemote) return;

            if (_remoteHolograms.TryGetValue(args.ConnectionId, out var hologram))
            {
                UpdateHologramFrame(hologram, args.Frame);
            }
        }

        private void OnMessageReceived(MessageEventArgs args)
        {
            Log($"Message from {args.ConnectionId}: {args.Content}");

            // First message from peer is their ID
            // Could use this to store username mapping
        }

        private void OnError(ErrorEventArgs args)
        {
            LogError($"Connection error: {args?.Info?.ToString() ?? "Unknown error"}");
        }

        #endregion

        #region Hologram Management

        private void CreateRemoteHologram(ConnectionId connectionId)
        {
            if (_remoteHolograms.ContainsKey(connectionId))
            {
                Log($"Hologram already exists for {connectionId}");
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
            go.name = $"RemoteHologram_{connectionId}";
            go.transform.localScale = Vector3.one * _hologramScale;

            // Get components
            var vfx = go.GetComponent<VisualEffect>();
            var binder = go.GetComponent<H3MWebRTCVFXBinder>();

            // Add Metavido decoder for proper depth extraction
            var decoder = go.GetComponent<MetavidoWebRTCDecoder>();
            if (decoder == null)
            {
                decoder = go.AddComponent<MetavidoWebRTCDecoder>();
            }

            // Subscribe to decoder events for automatic VFX binding
            decoder.OnFrameDecoded += (color, depth, metadata) =>
            {
                if (vfx != null)
                {
                    decoder.BindToVFX(vfx);
                }
            };

            // Store hologram data (textures managed by decoder)
            var hologram = new RemoteHologram
            {
                connectionId = connectionId,
                gameObject = go,
                vfx = vfx,
                binder = binder,
                decoder = decoder,
                videoTexture = null, // Created on first frame
                colorTexture = null, // Managed by decoder
                depthTexture = null, // Managed by decoder
                metadata = H3MStreamMetadata.Default
            };
            _remoteHolograms[connectionId] = hologram;

            Log($"Created hologram for {connectionId}");
        }

        private void UpdateHologramFrame(RemoteHologram hologram, IFrame frame)
        {
            if (frame == null || hologram.decoder == null) return;

            // Use UnityMediaHelper.UpdateTexture to convert frame to Texture2D
            // This handles the frame format conversion (ABGR/I420p)
            UnityMediaHelper.UpdateTexture(frame, ref hologram.videoTexture);

            // Feed the raw frame to Metavido decoder for depth extraction
            // Decoder extracts: color (left half), depth (right bottom), camera metadata (barcode)
            if (hologram.videoTexture != null)
            {
                hologram.decoder.DecodeFrame(hologram.videoTexture);
            }

            // VFX binding happens automatically via decoder.OnFrameDecoded event
        }

        private void DestroyRemoteHologram(ConnectionId connectionId)
        {
            if (!_remoteHolograms.TryGetValue(connectionId, out var hologram)) return;

            // Cleanup video texture (decoder manages its own textures via OnDestroy)
            if (hologram.videoTexture != null)
            {
                Destroy(hologram.videoTexture);
            }

            // Destroy GameObject (decoder cleanup happens automatically via OnDestroy)
            if (hologram.gameObject != null)
            {
                Destroy(hologram.gameObject);
            }

            _remoteHolograms.Remove(connectionId);
            Log($"Destroyed hologram for {connectionId}");
        }

        private void CleanupAllHolograms()
        {
            var connectionIds = new List<ConnectionId>(_remoteHolograms.Keys);
            foreach (var id in connectionIds)
            {
                DestroyRemoteHologram(id);
            }
        }

        #endregion

        #region Debug GUI

        private void OnGUI()
        {
            if (!_showDebugGUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>Hologram Conference (WebRtcVideoChat)</b>");
            GUILayout.Label($"Local Peer: {_localPeerId}");
            GUILayout.Label($"Room: {_roomName}");
            GUILayout.Label($"Connected: {IsInRoom}");
            GUILayout.Label($"Initialized: {_isInitialized}");
            GUILayout.Label($"Remote Peers: {_remoteHolograms.Count}");

            GUILayout.Space(10);

            if (!IsInRoom)
            {
                _roomName = GUILayout.TextField(_roomName);
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
                Debug.Log($"[HologramConference] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[HologramConference] {message}");
        }

        #endregion
    }
}
