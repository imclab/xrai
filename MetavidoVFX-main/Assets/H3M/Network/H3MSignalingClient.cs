// H3MSignalingClient - WebRTC signaling for hologram conferencing
// Handles offer/answer/ICE candidate exchange between peers
//
// DEPRECATED: This client is no longer needed when using WebRtcVideoChat (Byn.Awrtc)
// which has built-in signaling via wss://s.y-not.app/conferenceapp
//
// Use HologramConferenceManager instead - it uses WebRtcVideoChat directly.
// This file is kept for reference or custom self-hosted signaling scenarios.
//
// Based on Rcam3-WebRTC SimpleSignalingClient

using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_WEBRTC_AVAILABLE
using Unity.WebRTC;
#endif

namespace XRRAI.Hologram
{
    /// <summary>
    /// Signaling client for WebRTC peer connections.
    /// Manages room connections and SDP/ICE exchange.
    ///
    /// Note: Requires com.unity.webrtc package.
    /// Add "UNITY_WEBRTC_AVAILABLE" to scripting defines to enable.
    /// </summary>
    public class H3MSignalingClient : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Server Settings")]
        [SerializeField] private string _serverUrl = "ws://localhost:3003";
        [SerializeField] private bool _autoReconnect = true;
        [SerializeField] private float _reconnectInterval = 5f;
        [SerializeField] private int _maxReconnectAttempts = 5;

        [Header("Debug")]
        [SerializeField] private bool _logMessages = true;

        #endregion

        #region Events

        public event Action<string> OnPeerConnected;
        public event Action<string> OnPeerDisconnected;
        public event Action<string, string> OnSignalingOffer;
        public event Action<string, string> OnSignalingAnswer;

        #if UNITY_WEBRTC_AVAILABLE
        public event Action<string, RTCIceCandidateInit> OnSignalingCandidate;
        #endif

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;

        #endregion

        #region Public Properties

        public string ServerUrl => _serverUrl;
        public bool IsConnected => _isConnected;
        public string CurrentRoom => _currentRoom;

        #endregion

        #region Private Members

        private bool _isConnected = false;
        private float _reconnectTimer = 0f;
        private int _reconnectAttempts = 0;
        private string _currentRoom = "";
        private HashSet<string> _connectedPeers = new HashSet<string>();
        private Queue<Action> _pendingActions = new Queue<Action>();

        // TODO: Replace with actual WebSocket implementation
        // Options: NativeWebSocket, WebSocketSharp, or Unity's built-in (Unity 6+)
        // private WebSocket _webSocket;

        #endregion

        #region MonoBehaviour

        private void Update()
        {
            // Handle reconnection logic
            if (!_isConnected && _autoReconnect &&
                _reconnectAttempts < _maxReconnectAttempts &&
                !string.IsNullOrEmpty(_currentRoom))
            {
                _reconnectTimer += Time.deltaTime;
                if (_reconnectTimer >= _reconnectInterval)
                {
                    _reconnectTimer = 0f;
                    _reconnectAttempts++;
                    Log($"Reconnecting... attempt {_reconnectAttempts}/{_maxReconnectAttempts}");
                    Connect(_currentRoom);
                }
            }

            // Process pending actions on main thread
            while (_pendingActions.Count > 0)
            {
                var action = _pendingActions.Dequeue();
                action?.Invoke();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Connect to signaling server and join room.
        /// </summary>
        public void Connect(string roomName)
        {
            _currentRoom = roomName;
            Log($"Connecting to {_serverUrl}, room: {roomName}");

            // TODO: Implement actual WebSocket connection
            // _webSocket = new WebSocket(_serverUrl);
            // _webSocket.OnOpen += OnWebSocketOpen;
            // _webSocket.OnMessage += OnWebSocketMessage;
            // _webSocket.OnError += OnWebSocketError;
            // _webSocket.OnClose += OnWebSocketClose;
            // _webSocket.Connect();

            // Mock connection for now
            _isConnected = true;
            _reconnectAttempts = 0;
            OnConnected?.Invoke();
        }

        /// <summary>
        /// Disconnect from signaling server.
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected) return;

            Log("Disconnecting from signaling server");

            // TODO: Close WebSocket
            // _webSocket?.Close();

            _isConnected = false;
            _connectedPeers.Clear();
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// Send SDP offer to peer.
        /// </summary>
        public void SendOffer(string peerId, string sdp)
        {
            if (!_isConnected)
            {
                LogWarning("Cannot send offer - not connected");
                return;
            }

            Log($"Sending offer to {peerId}");
            SendSignalingMessage(new SignalingMessage
            {
                type = "offer",
                targetPeerId = peerId,
                sdp = sdp
            });
        }

        /// <summary>
        /// Send SDP answer to peer.
        /// </summary>
        public void SendAnswer(string peerId, string sdp)
        {
            if (!_isConnected)
            {
                LogWarning("Cannot send answer - not connected");
                return;
            }

            Log($"Sending answer to {peerId}");
            SendSignalingMessage(new SignalingMessage
            {
                type = "answer",
                targetPeerId = peerId,
                sdp = sdp
            });
        }

        #if UNITY_WEBRTC_AVAILABLE
        /// <summary>
        /// Send ICE candidate to peer.
        /// </summary>
        public void SendCandidate(string peerId, RTCIceCandidate candidate)
        {
            if (!_isConnected)
            {
                LogWarning("Cannot send ICE candidate - not connected");
                return;
            }

            Log($"Sending ICE candidate to {peerId}");
            SendSignalingMessage(new SignalingMessage
            {
                type = "candidate",
                targetPeerId = peerId,
                candidate = candidate.Candidate,
                sdpMid = candidate.SdpMid,
                sdpMLineIndex = candidate.SdpMLineIndex ?? 0
            });
        }
        #endif

        #endregion

        #region Internal

        private void SendSignalingMessage(SignalingMessage message)
        {
            message.room = _currentRoom;
            var json = JsonUtility.ToJson(message);

            // TODO: Send via WebSocket
            // _webSocket?.Send(json);

            Log($"[TX] {message.type} to {message.targetPeerId}");
        }

        private void HandleSignalingMessage(string json)
        {
            try
            {
                var message = JsonUtility.FromJson<SignalingMessage>(json);
                Log($"[RX] {message.type} from {message.sourcePeerId}");

                switch (message.type)
                {
                    case "peer-joined":
                        _connectedPeers.Add(message.sourcePeerId);
                        OnPeerConnected?.Invoke(message.sourcePeerId);
                        break;

                    case "peer-left":
                        _connectedPeers.Remove(message.sourcePeerId);
                        OnPeerDisconnected?.Invoke(message.sourcePeerId);
                        break;

                    case "offer":
                        OnSignalingOffer?.Invoke(message.sourcePeerId, message.sdp);
                        break;

                    case "answer":
                        OnSignalingAnswer?.Invoke(message.sourcePeerId, message.sdp);
                        break;

                    #if UNITY_WEBRTC_AVAILABLE
                    case "candidate":
                        var candidateInit = new RTCIceCandidateInit
                        {
                            candidate = message.candidate,
                            sdpMid = message.sdpMid,
                            sdpMLineIndex = message.sdpMLineIndex
                        };
                        OnSignalingCandidate?.Invoke(message.sourcePeerId, candidateInit);
                        break;
                    #endif
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to parse signaling message: {e.Message}");
            }
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_logMessages)
                Debug.Log($"[H3MSignaling] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[H3MSignaling] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[H3MSignaling] {message}");
            OnError?.Invoke(message);
        }

        #endregion

        #region Message Types

        [Serializable]
        private class SignalingMessage
        {
            public string type;
            public string room;
            public string sourcePeerId;
            public string targetPeerId;
            public string sdp;
            public string candidate;
            public string sdpMid;
            public int sdpMLineIndex;
        }

        #endregion
    }
}
