// LobbyController.cs - Controller for LobbyView.uxml
// Part of Spec 013: UI/UX Conferencing System
//
// Handles room creation, joining, and user session management.
// Works with IAuthProvider for user state and room service for conferencing.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using XRRAI.Auth;

namespace XRRAI.UI
{
    /// <summary>
    /// Controls the LobbyView UI for room management.
    /// Attach to a GameObject with UIDocument referencing LobbyView.uxml.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class LobbyController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Auth controller reference for user state")]
        [SerializeField] AuthController _authController;

        [Tooltip("Scene to load for conference")]
        [SerializeField] string _conferenceSceneName = "Conference";

        [Tooltip("Maximum recent rooms to display")]
        [SerializeField] int _maxRecentRooms = 5;

        // Events
        public event Action<RoomInfo> OnRoomCreated;
        public event Action<RoomInfo> OnRoomJoined;
        public event Action OnSignOutRequested;
        public event Action OnSettingsRequested;

        // UI Elements
        UIDocument _document;
        VisualElement _root;

        // Top bar
        VisualElement _userAvatar;
        Label _userName;
        Label _userEmail;
        Button _btnSettings;
        Button _btnSignOut;

        // Create room
        TextField _roomName;
        DropdownField _maxParticipants;
        DropdownField _qualityPreset;
        Button _btnCreate;

        // Join room
        TextField _roomCode;
        Button _btnJoin;

        // Error & recent
        Label _lobbyError;
        VisualElement _recentList;

        // State
        List<RoomInfo> _recentRooms = new();
        bool _isProcessing;

        void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            _root = _document.rootVisualElement;

            CacheUIElements();
            BindEvents();
            UpdateUserInfo();
            UpdateRecentRooms();
            ClearError();
        }

        void OnDisable()
        {
            UnbindEvents();
        }

        void CacheUIElements()
        {
            // Top bar
            _userAvatar = _root.Q<VisualElement>("user-avatar");
            _userName = _root.Q<Label>("user-name");
            _userEmail = _root.Q<Label>("user-email");
            _btnSettings = _root.Q<Button>("btn-settings");
            _btnSignOut = _root.Q<Button>("btn-signout");

            // Create room
            _roomName = _root.Q<TextField>("room-name");
            _maxParticipants = _root.Q<DropdownField>("max-participants");
            _qualityPreset = _root.Q<DropdownField>("quality-preset");
            _btnCreate = _root.Q<Button>("btn-create");

            // Join room
            _roomCode = _root.Q<TextField>("room-code");
            _btnJoin = _root.Q<Button>("btn-join");

            // Error & recent
            _lobbyError = _root.Q<Label>("lobby-error");
            _recentList = _root.Q<VisualElement>("recent-list");
        }

        void BindEvents()
        {
            _btnSettings?.RegisterCallback<ClickEvent>(_ => OnSettingsClicked());
            _btnSignOut?.RegisterCallback<ClickEvent>(_ => OnSignOutClicked());
            _btnCreate?.RegisterCallback<ClickEvent>(_ => OnCreateRoomClicked());
            _btnJoin?.RegisterCallback<ClickEvent>(_ => OnJoinRoomClicked());

            // Enter key submits join form
            _roomCode?.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    OnJoinRoomClicked();
            });

            // Format room code as uppercase
            _roomCode?.RegisterValueChangedCallback(e =>
            {
                if (!string.IsNullOrEmpty(e.newValue))
                {
                    string formatted = e.newValue.ToUpper().Replace(" ", "");
                    if (formatted.Length > 6) formatted = formatted.Substring(0, 6);
                    if (formatted != e.newValue)
                        _roomCode.SetValueWithoutNotify(formatted);
                }
            });
        }

        void UnbindEvents()
        {
            // UI Toolkit handles cleanup on disable
        }

        #region User Info

        void UpdateUserInfo()
        {
            AuthUser user = _authController?.CurrentUser;

            if (user != null)
            {
                _userName?.SetEnabled(true);
                if (_userName != null) _userName.text = user.DisplayName ?? "User";
                if (_userEmail != null) _userEmail.text = user.Email ?? "";

                // TODO: Load avatar from user.PhotoUrl
            }
            else
            {
                if (_userName != null) _userName.text = "Guest";
                if (_userEmail != null) _userEmail.text = "Not signed in";
            }
        }

        #endregion

        #region Room Creation

        async void OnCreateRoomClicked()
        {
            if (_isProcessing) return;

            string roomName = _roomName?.value?.Trim();
            int maxParticipants = GetSelectedMaxParticipants();
            string quality = _qualityPreset?.value ?? "Medium";

            // Generate room name if not provided
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = GenerateRoomName();
            }

            SetProcessing(true);
            ClearError();

            try
            {
                // Generate 6-character room code
                string roomCode = GenerateRoomCode();

                var roomInfo = new RoomInfo
                {
                    Code = roomCode,
                    Name = roomName,
                    MaxParticipants = maxParticipants,
                    QualityPreset = quality,
                    CreatedAt = DateTime.UtcNow,
                    HostId = _authController?.CurrentUser?.UserId ?? "guest"
                };

                Debug.Log($"[LobbyController] Room created: {roomInfo.Name} ({roomInfo.Code})");

                // Add to recent rooms
                AddRecentRoom(roomInfo);

                OnRoomCreated?.Invoke(roomInfo);
                NavigateToConference(roomInfo);
            }
            catch (Exception ex)
            {
                ShowError("Failed to create room. Please try again.");
                Debug.LogError($"[LobbyController] Create room error: {ex.Message}");
            }
            finally
            {
                SetProcessing(false);
            }
        }

        int GetSelectedMaxParticipants()
        {
            string value = _maxParticipants?.value;
            if (int.TryParse(value, out int result))
                return result;
            return 4; // Default
        }

        string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude confusing chars
            var random = new System.Random();
            var code = new char[6];
            for (int i = 0; i < 6; i++)
                code[i] = chars[random.Next(chars.Length)];
            return new string(code);
        }

        string GenerateRoomName()
        {
            string[] adjectives = { "Cosmic", "Azure", "Crystal", "Golden", "Stellar" };
            string[] nouns = { "Portal", "Chamber", "Space", "Nexus", "Hub" };
            var random = new System.Random();
            return $"{adjectives[random.Next(adjectives.Length)]} {nouns[random.Next(nouns.Length)]}";
        }

        #endregion

        #region Room Joining

        async void OnJoinRoomClicked()
        {
            if (_isProcessing) return;

            string code = _roomCode?.value?.Trim()?.ToUpper();

            if (string.IsNullOrEmpty(code))
            {
                ShowError("Please enter a room code");
                return;
            }

            if (code.Length != 6)
            {
                ShowError("Room code must be 6 characters");
                return;
            }

            SetProcessing(true);
            ClearError();

            try
            {
                // TODO: Validate room code with server
                // For now, create a placeholder room info
                var roomInfo = new RoomInfo
                {
                    Code = code,
                    Name = $"Room {code}",
                    MaxParticipants = 4,
                    QualityPreset = "Medium",
                    CreatedAt = DateTime.UtcNow
                };

                Debug.Log($"[LobbyController] Joining room: {code}");

                // Add to recent rooms
                AddRecentRoom(roomInfo);

                OnRoomJoined?.Invoke(roomInfo);
                NavigateToConference(roomInfo);
            }
            catch (Exception ex)
            {
                ShowError("Room not found or no longer available");
                Debug.LogError($"[LobbyController] Join room error: {ex.Message}");
            }
            finally
            {
                SetProcessing(false);
            }
        }

        #endregion

        #region Recent Rooms

        void AddRecentRoom(RoomInfo room)
        {
            // Remove if already exists
            _recentRooms.RemoveAll(r => r.Code == room.Code);

            // Add to front
            _recentRooms.Insert(0, room);

            // Trim to max
            while (_recentRooms.Count > _maxRecentRooms)
                _recentRooms.RemoveAt(_recentRooms.Count - 1);

            UpdateRecentRooms();
            SaveRecentRooms();
        }

        void UpdateRecentRooms()
        {
            if (_recentList == null) return;

            _recentList.Clear();

            if (_recentRooms.Count == 0)
            {
                var emptyLabel = new Label("No recent rooms");
                emptyLabel.AddToClassList("body-sm");
                emptyLabel.AddToClassList("text-secondary");
                emptyLabel.AddToClassList("text-center");
                emptyLabel.style.paddingTop = 16;
                emptyLabel.style.paddingBottom = 16;
                _recentList.Add(emptyLabel);
                return;
            }

            foreach (var room in _recentRooms)
            {
                var item = CreateRecentRoomItem(room);
                _recentList.Add(item);
            }
        }

        VisualElement CreateRecentRoomItem(RoomInfo room)
        {
            var item = new VisualElement();
            item.AddToClassList("flex-row");
            item.style.paddingTop = 12;
            item.style.paddingBottom = 12;
            item.style.paddingLeft = 16;
            item.style.paddingRight = 16;
            item.style.alignItems = Align.Center;
            item.style.justifyContent = Justify.SpaceBetween;
            item.style.borderBottomWidth = 1;
            item.style.borderBottomColor = new Color(1, 1, 1, 0.1f);

            // Room info
            var info = new VisualElement();

            var nameLabel = new Label(room.Name);
            nameLabel.AddToClassList("body");
            info.Add(nameLabel);

            var codeLabel = new Label($"Code: {room.Code}");
            codeLabel.AddToClassList("caption");
            codeLabel.AddToClassList("text-secondary");
            info.Add(codeLabel);

            item.Add(info);

            // Rejoin button
            var rejoinBtn = new Button(() => RejoinRoom(room));
            rejoinBtn.text = "Rejoin";
            rejoinBtn.AddToClassList("btn");
            rejoinBtn.AddToClassList("btn-secondary");
            item.Add(rejoinBtn);

            return item;
        }

        void RejoinRoom(RoomInfo room)
        {
            _roomCode.value = room.Code;
            OnJoinRoomClicked();
        }

        void SaveRecentRooms()
        {
            // TODO: Save to PlayerPrefs or Firestore
        }

        void LoadRecentRooms()
        {
            // TODO: Load from PlayerPrefs or Firestore
        }

        #endregion

        #region Navigation

        void OnSettingsClicked()
        {
            Debug.Log("[LobbyController] Settings requested");
            OnSettingsRequested?.Invoke();
        }

        void OnSignOutClicked()
        {
            Debug.Log("[LobbyController] Sign out requested");

            if (_authController != null)
            {
                _authController.SignOut();
            }

            OnSignOutRequested?.Invoke();

            // Show auth screen
            if (_root != null)
                _root.style.display = DisplayStyle.None;
        }

        void NavigateToConference(RoomInfo room)
        {
            Debug.Log($"[LobbyController] Navigating to conference: {room.Code}");

            // Store room info for conference scene
            PlayerPrefs.SetString("CurrentRoomCode", room.Code);
            PlayerPrefs.SetString("CurrentRoomName", room.Name);
            PlayerPrefs.Save();

            // TODO: Load conference scene
            // UnityEngine.SceneManagement.SceneManager.LoadScene(_conferenceSceneName);

            // For now, hide lobby
            if (_root != null)
                _root.style.display = DisplayStyle.None;
        }

        #endregion

        #region Helpers

        void ShowError(string message)
        {
            if (_lobbyError == null) return;
            _lobbyError.text = message;
            _lobbyError.style.display = DisplayStyle.Flex;
        }

        void ClearError()
        {
            if (_lobbyError != null)
                _lobbyError.style.display = DisplayStyle.None;
        }

        void SetProcessing(bool processing)
        {
            _isProcessing = processing;
            _btnCreate?.SetEnabled(!processing);
            _btnJoin?.SetEnabled(!processing);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Show the lobby view
        /// </summary>
        public void Show()
        {
            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
                UpdateUserInfo();
            }
        }

        /// <summary>
        /// Hide the lobby view
        /// </summary>
        public void Hide()
        {
            if (_root != null)
                _root.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Refresh user information from auth provider
        /// </summary>
        public void RefreshUserInfo()
        {
            UpdateUserInfo();
        }

        #endregion
    }

    /// <summary>
    /// Room information for conferencing
    /// </summary>
    [Serializable]
    public class RoomInfo
    {
        public string Code;
        public string Name;
        public int MaxParticipants;
        public string QualityPreset;
        public DateTime CreatedAt;
        public string HostId;
        public List<string> ParticipantIds = new();
    }
}
