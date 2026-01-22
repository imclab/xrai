// ConferenceHUDController.cs - Controller for ConferenceHUD.uxml
// Part of Spec 013: UI/UX Conferencing System
//
// Manages the in-conference HUD with controls, chat, reactions, and status displays.
// Integrates with audio, networking, and hologram systems.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using XRRAI.Auth;

namespace XRRAI.UI
{
    /// <summary>
    /// Controls the Conference HUD overlay during active hologram calls.
    /// Attach to a GameObject with UIDocument referencing ConferenceHUD.uxml.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ConferenceHUDController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Audio source for voice activity detection")]
        [SerializeField] AudioSource _microphoneSource;

        [Tooltip("Toast display duration in seconds")]
        [SerializeField] float _toastDuration = 3f;

        [Header("Keyboard Shortcuts")]
        [SerializeField] KeyCode _muteKey = KeyCode.M;
        [SerializeField] KeyCode _cameraKey = KeyCode.V;
        [SerializeField] KeyCode _raiseHandKey = KeyCode.H;
        [SerializeField] KeyCode _chatKey = KeyCode.C;
        [SerializeField] KeyCode _leaveKey = KeyCode.Escape;

        // Events
        public event Action OnMuteToggled;
        public event Action OnCameraToggled;
        public event Action OnHandRaised;
        public event Action<string> OnReactionSent;
        public event Action<string> OnChatMessageSent;
        public event Action OnSettingsRequested;
        public event Action OnLeaveRequested;

        // UI Elements
        UIDocument _document;
        VisualElement _root;

        // Top bar
        Label _roomName;
        Label _roomCode;
        Label _timer;
        Label _participantCount;
        Label _activeSpeaker;

        // Voice meter
        VisualElement _voiceMeterFill;
        Label _voiceLevelText;

        // Control buttons
        Button _btnMute;
        Button _btnCamera;
        Button _btnRaiseHand;
        Button _btnReactions;
        Button _btnChat;
        Button _btnSettings;
        Button _btnLeave;

        // Icons for state changes
        VisualElement _muteIcon;
        VisualElement _cameraIcon;
        VisualElement _handIcon;

        // Panels
        VisualElement _reactionsPopup;
        VisualElement _chatPanel;
        ScrollView _chatMessages;
        TextField _chatInput;
        Button _btnSendChat;
        Button _btnCloseChat;

        // Toast
        VisualElement _toast;
        Label _toastMessage;
        Coroutine _toastCoroutine;

        // State
        bool _isMuted;
        bool _isCameraOn = true;
        bool _isHandRaised;
        bool _isChatOpen;
        DateTime _conferenceStartTime;
        List<ChatMessage> _chatHistory = new();

        void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            _root = _document.rootVisualElement;
            _conferenceStartTime = DateTime.UtcNow;

            CacheUIElements();
            BindEvents();
            LoadRoomInfo();
            UpdateControlStates();
        }

        void OnDisable()
        {
            UnbindEvents();
        }

        void Update()
        {
            UpdateTimer();
            UpdateVoiceMeter();
            HandleKeyboardShortcuts();
        }

        void CacheUIElements()
        {
            // Top bar
            _roomName = _root.Q<Label>("room-name");
            _roomCode = _root.Q<Label>("room-code");
            _timer = _root.Q<Label>("timer");
            _participantCount = _root.Q<Label>("participant-count");
            _activeSpeaker = _root.Q<Label>("active-speaker");

            // Voice meter
            _voiceMeterFill = _root.Q<VisualElement>("voice-meter-fill");
            _voiceLevelText = _root.Q<Label>("voice-level-text");

            // Control buttons
            _btnMute = _root.Q<Button>("btn-mute");
            _btnCamera = _root.Q<Button>("btn-camera");
            _btnRaiseHand = _root.Q<Button>("btn-raise-hand");
            _btnReactions = _root.Q<Button>("btn-reactions");
            _btnChat = _root.Q<Button>("btn-chat");
            _btnSettings = _root.Q<Button>("btn-settings");
            _btnLeave = _root.Q<Button>("btn-leave");

            // Icons
            _muteIcon = _root.Q<VisualElement>("mute-icon");
            _cameraIcon = _root.Q<VisualElement>("camera-icon");
            _handIcon = _root.Q<VisualElement>("hand-icon");

            // Reactions popup
            _reactionsPopup = _root.Q<VisualElement>("reactions-popup");

            // Chat panel
            _chatPanel = _root.Q<VisualElement>("chat-panel");
            _chatMessages = _root.Q<ScrollView>("chat-messages");
            _chatInput = _root.Q<TextField>("chat-input");
            _btnSendChat = _root.Q<Button>("btn-send-chat");
            _btnCloseChat = _root.Q<Button>("btn-close-chat");

            // Toast
            _toast = _root.Q<VisualElement>("toast");
            _toastMessage = _root.Q<Label>("toast-message");
        }

        void BindEvents()
        {
            // Control buttons
            _btnMute?.RegisterCallback<ClickEvent>(_ => ToggleMute());
            _btnCamera?.RegisterCallback<ClickEvent>(_ => ToggleCamera());
            _btnRaiseHand?.RegisterCallback<ClickEvent>(_ => ToggleRaiseHand());
            _btnReactions?.RegisterCallback<ClickEvent>(_ => ToggleReactionsPopup());
            _btnChat?.RegisterCallback<ClickEvent>(_ => ToggleChat());
            _btnSettings?.RegisterCallback<ClickEvent>(_ => OnSettingsClicked());
            _btnLeave?.RegisterCallback<ClickEvent>(_ => OnLeaveClicked());

            // Reaction buttons
            _root.Q<Button>("react-thumbsup")?.RegisterCallback<ClickEvent>(_ => SendReaction("thumbsup"));
            _root.Q<Button>("react-heart")?.RegisterCallback<ClickEvent>(_ => SendReaction("heart"));
            _root.Q<Button>("react-clap")?.RegisterCallback<ClickEvent>(_ => SendReaction("clap"));
            _root.Q<Button>("react-laugh")?.RegisterCallback<ClickEvent>(_ => SendReaction("laugh"));
            _root.Q<Button>("react-surprised")?.RegisterCallback<ClickEvent>(_ => SendReaction("surprised"));

            // Chat
            _btnSendChat?.RegisterCallback<ClickEvent>(_ => SendChatMessage());
            _btnCloseChat?.RegisterCallback<ClickEvent>(_ => CloseChat());
            _chatInput?.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    SendChatMessage();
            });
        }

        void UnbindEvents()
        {
            // UI Toolkit handles cleanup
        }

        #region Room Info

        void LoadRoomInfo()
        {
            string roomName = PlayerPrefs.GetString("CurrentRoomName", "Conference");
            string roomCode = PlayerPrefs.GetString("CurrentRoomCode", "------");

            if (_roomName != null) _roomName.text = roomName;
            if (_roomCode != null) _roomCode.text = roomCode;
        }

        void UpdateTimer()
        {
            if (_timer == null) return;

            TimeSpan elapsed = DateTime.UtcNow - _conferenceStartTime;
            _timer.text = elapsed.TotalHours >= 1
                ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
                : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        #endregion

        #region Voice Meter

        void UpdateVoiceMeter()
        {
            if (_voiceMeterFill == null) return;

            float level = 0f;

            if (_microphoneSource != null && _microphoneSource.isPlaying && !_isMuted)
            {
                float[] samples = new float[256];
                _microphoneSource.GetOutputData(samples, 0);

                float sum = 0f;
                foreach (float sample in samples)
                    sum += Mathf.Abs(sample);

                level = Mathf.Clamp01(sum / samples.Length * 10f);
            }

            _voiceMeterFill.style.width = new Length(level * 100f, LengthUnit.Percent);
            if (_voiceLevelText != null)
                _voiceLevelText.text = $"{Mathf.RoundToInt(level * 100)}%";
        }

        #endregion

        #region Controls

        void ToggleMute()
        {
            _isMuted = !_isMuted;
            UpdateControlStates();
            ShowToast(_isMuted ? "Microphone muted" : "Microphone unmuted");
            OnMuteToggled?.Invoke();
            Debug.Log($"[ConferenceHUD] Mute: {_isMuted}");
        }

        void ToggleCamera()
        {
            _isCameraOn = !_isCameraOn;
            UpdateControlStates();
            ShowToast(_isCameraOn ? "Camera on" : "Camera off");
            OnCameraToggled?.Invoke();
            Debug.Log($"[ConferenceHUD] Camera: {_isCameraOn}");
        }

        void ToggleRaiseHand()
        {
            _isHandRaised = !_isHandRaised;
            UpdateControlStates();
            ShowToast(_isHandRaised ? "Hand raised" : "Hand lowered");
            OnHandRaised?.Invoke();
            Debug.Log($"[ConferenceHUD] Hand raised: {_isHandRaised}");
        }

        void UpdateControlStates()
        {
            // Mute state
            if (_btnMute != null)
            {
                if (_isMuted)
                    _btnMute.AddToClassList("hud-btn-active");
                else
                    _btnMute.RemoveFromClassList("hud-btn-active");
            }

            // Camera state
            if (_btnCamera != null)
            {
                if (!_isCameraOn)
                    _btnCamera.AddToClassList("hud-btn-active");
                else
                    _btnCamera.RemoveFromClassList("hud-btn-active");
            }

            // Hand raised state
            if (_btnRaiseHand != null)
            {
                if (_isHandRaised)
                    _btnRaiseHand.AddToClassList("hud-btn-active");
                else
                    _btnRaiseHand.RemoveFromClassList("hud-btn-active");
            }
        }

        #endregion

        #region Reactions

        void ToggleReactionsPopup()
        {
            if (_reactionsPopup == null) return;

            bool isVisible = _reactionsPopup.style.display == DisplayStyle.Flex;
            _reactionsPopup.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void SendReaction(string reactionType)
        {
            Debug.Log($"[ConferenceHUD] Reaction: {reactionType}");
            _reactionsPopup.style.display = DisplayStyle.None;
            ShowToast($"Sent {reactionType}");
            OnReactionSent?.Invoke(reactionType);
        }

        #endregion

        #region Chat

        void ToggleChat()
        {
            _isChatOpen = !_isChatOpen;
            if (_chatPanel != null)
                _chatPanel.style.display = _isChatOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void CloseChat()
        {
            _isChatOpen = false;
            if (_chatPanel != null)
                _chatPanel.style.display = DisplayStyle.None;
        }

        void SendChatMessage()
        {
            if (_chatInput == null) return;

            string message = _chatInput.value?.Trim();
            if (string.IsNullOrEmpty(message)) return;

            // Add to local history
            var chatMessage = new ChatMessage
            {
                Sender = AuthManager.Instance?.UserDisplayName ?? "You",
                Text = message,
                Timestamp = DateTime.UtcNow,
                IsLocal = true
            };
            _chatHistory.Add(chatMessage);

            // Update UI
            AddChatMessageToUI(chatMessage);
            _chatInput.value = "";

            // Fire event
            OnChatMessageSent?.Invoke(message);
            Debug.Log($"[ConferenceHUD] Chat: {message}");
        }

        void AddChatMessageToUI(ChatMessage message)
        {
            if (_chatMessages == null) return;

            // Clear "no messages" placeholder
            if (_chatHistory.Count == 1)
                _chatMessages.Clear();

            var msgElement = new VisualElement();
            msgElement.style.marginBottom = 8;
            msgElement.style.paddingLeft = message.IsLocal ? 24 : 0;
            msgElement.style.paddingRight = message.IsLocal ? 0 : 24;

            var bubble = new VisualElement();
            bubble.AddToClassList(message.IsLocal ? "chat-bubble-local" : "chat-bubble-remote");
            bubble.style.backgroundColor = message.IsLocal
                ? new Color(0, 0.48f, 1, 0.8f)
                : new Color(0.3f, 0.3f, 0.3f, 0.8f);
            bubble.style.borderTopLeftRadius = 12;
            bubble.style.borderTopRightRadius = 12;
            bubble.style.borderBottomLeftRadius = 12;
            bubble.style.borderBottomRightRadius = 12;
            bubble.style.paddingTop = 8;
            bubble.style.paddingBottom = 8;
            bubble.style.paddingLeft = 12;
            bubble.style.paddingRight = 12;

            var sender = new Label(message.Sender);
            sender.AddToClassList("caption");
            sender.style.color = new Color(1, 1, 1, 0.7f);
            bubble.Add(sender);

            var text = new Label(message.Text);
            text.AddToClassList("body-sm");
            text.style.whiteSpace = WhiteSpace.Normal;
            bubble.Add(text);

            msgElement.Add(bubble);
            _chatMessages.Add(msgElement);

            // Scroll to bottom
            _chatMessages.scrollOffset = new Vector2(0, float.MaxValue);
        }

        /// <summary>
        /// Add a received chat message from another participant
        /// </summary>
        public void ReceiveChatMessage(string sender, string text)
        {
            var message = new ChatMessage
            {
                Sender = sender,
                Text = text,
                Timestamp = DateTime.UtcNow,
                IsLocal = false
            };
            _chatHistory.Add(message);
            AddChatMessageToUI(message);

            if (!_isChatOpen)
                ShowToast($"{sender}: {text}");
        }

        #endregion

        #region Navigation

        void OnSettingsClicked()
        {
            Debug.Log("[ConferenceHUD] Settings requested");
            OnSettingsRequested?.Invoke();
        }

        void OnLeaveClicked()
        {
            Debug.Log("[ConferenceHUD] Leave requested");
            OnLeaveRequested?.Invoke();
        }

        #endregion

        #region Toast

        void ShowToast(string message)
        {
            if (_toast == null || _toastMessage == null) return;

            if (_toastCoroutine != null)
                StopCoroutine(_toastCoroutine);

            _toastMessage.text = message;
            _toast.style.display = DisplayStyle.Flex;
            _toastCoroutine = StartCoroutine(HideToastAfterDelay());
        }

        IEnumerator HideToastAfterDelay()
        {
            yield return new WaitForSeconds(_toastDuration);
            if (_toast != null)
                _toast.style.display = DisplayStyle.None;
        }

        #endregion

        #region Keyboard Shortcuts

        void HandleKeyboardShortcuts()
        {
            if (Input.GetKeyDown(_muteKey)) ToggleMute();
            if (Input.GetKeyDown(_cameraKey)) ToggleCamera();
            if (Input.GetKeyDown(_raiseHandKey)) ToggleRaiseHand();
            if (Input.GetKeyDown(_chatKey)) ToggleChat();
            if (Input.GetKeyDown(_leaveKey)) OnLeaveClicked();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Update participant count display
        /// </summary>
        public void SetParticipantCount(int current, int max)
        {
            if (_participantCount != null)
                _participantCount.text = $"{current}/{max}";
        }

        /// <summary>
        /// Update active speaker display
        /// </summary>
        public void SetActiveSpeaker(string displayName)
        {
            if (_activeSpeaker != null)
                _activeSpeaker.text = string.IsNullOrEmpty(displayName)
                    ? ""
                    : $"Speaking: @{displayName}";
        }

        /// <summary>
        /// Get mute state
        /// </summary>
        public bool IsMuted => _isMuted;

        /// <summary>
        /// Get camera state
        /// </summary>
        public bool IsCameraOn => _isCameraOn;

        /// <summary>
        /// Get hand raised state
        /// </summary>
        public bool IsHandRaised => _isHandRaised;

        /// <summary>
        /// Show the HUD
        /// </summary>
        public void Show()
        {
            if (_root != null)
                _root.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Hide the HUD
        /// </summary>
        public void Hide()
        {
            if (_root != null)
                _root.style.display = DisplayStyle.None;
        }

        #endregion
    }

    /// <summary>
    /// Chat message data
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        public string Sender;
        public string Text;
        public DateTime Timestamp;
        public bool IsLocal;
    }
}
