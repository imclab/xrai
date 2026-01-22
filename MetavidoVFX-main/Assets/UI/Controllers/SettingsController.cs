// SettingsController.cs - Controller for SettingsView.uxml
// Part of Spec 013: UI/UX Conferencing System
//
// Manages user preferences for audio, video, hologram quality, and appearance.
// Persists settings to PlayerPrefs and applies them at runtime.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using XRRAI.Auth;

namespace XRRAI.UI
{
    /// <summary>
    /// Controls the Settings panel for user preferences.
    /// Attach to a GameObject with UIDocument referencing SettingsView.uxml.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SettingsController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Auth controller for sign out")]
        [SerializeField] AuthController _authController;

        // Events
        public event Action OnClose;
        public event Action OnSignOut;
        public event Action<UserSettings> OnSettingsChanged;

        // UI Elements
        UIDocument _document;
        VisualElement _root;
        VisualElement _settingsPanel;

        // Profile
        VisualElement _profileAvatar;
        Label _profileName;
        Label _profileEmail;
        Button _btnEditProfile;
        VisualElement _editProfileFields;
        TextField _editName;
        Button _btnCancelEdit;
        Button _btnSaveProfile;

        // Audio
        DropdownField _microphoneDevice;
        Slider _micVolume;
        Label _micVolumeLabel;
        DropdownField _speakerDevice;
        Slider _speakerVolume;
        Label _speakerVolumeLabel;
        Toggle _noiseSuppression;
        Toggle _echoCancellation;

        // Video
        DropdownField _cameraDevice;
        DropdownField _qualityPreset;
        Toggle _mirrorVideo;
        Toggle _hdVideo;

        // Hologram
        DropdownField _hologramQuality;
        Slider _particleDensity;
        Label _particleDensityLabel;
        Toggle _depthOcclusion;
        Toggle _handTracking;

        // Appearance
        DropdownField _theme;
        Toggle _showHud;
        Toggle _showVoiceMeter;

        // Notifications
        Toggle _notifyJoin;
        Toggle _notifyLeave;
        Toggle _notifyChat;
        Toggle _notifyReactions;

        // Footer
        Button _btnClose;
        Button _btnSignOut;
        Label _versionLabel;

        // State
        UserSettings _settings = new();
        bool _isEditingProfile;

        void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            _root = _document.rootVisualElement;

            CacheUIElements();
            BindEvents();
            LoadSettings();
            UpdateProfileDisplay();
            PopulateDeviceLists();
        }

        void OnDisable()
        {
            SaveSettings();
        }

        void CacheUIElements()
        {
            _settingsPanel = _root.Q<VisualElement>("settings-panel");

            // Profile
            _profileAvatar = _root.Q<VisualElement>("profile-avatar");
            _profileName = _root.Q<Label>("profile-name");
            _profileEmail = _root.Q<Label>("profile-email");
            _btnEditProfile = _root.Q<Button>("btn-edit-profile");
            _editProfileFields = _root.Q<VisualElement>("edit-profile-fields");
            _editName = _root.Q<TextField>("edit-name");
            _btnCancelEdit = _root.Q<Button>("btn-cancel-edit");
            _btnSaveProfile = _root.Q<Button>("btn-save-profile");

            // Audio
            _microphoneDevice = _root.Q<DropdownField>("microphone-device");
            _micVolume = _root.Q<Slider>("mic-volume");
            _micVolumeLabel = _root.Q<Label>("mic-volume-label");
            _speakerDevice = _root.Q<DropdownField>("speaker-device");
            _speakerVolume = _root.Q<Slider>("speaker-volume");
            _speakerVolumeLabel = _root.Q<Label>("speaker-volume-label");
            _noiseSuppression = _root.Q<Toggle>("noise-suppression");
            _echoCancellation = _root.Q<Toggle>("echo-cancellation");

            // Video
            _cameraDevice = _root.Q<DropdownField>("camera-device");
            _qualityPreset = _root.Q<DropdownField>("quality-preset");
            _mirrorVideo = _root.Q<Toggle>("mirror-video");
            _hdVideo = _root.Q<Toggle>("hd-video");

            // Hologram
            _hologramQuality = _root.Q<DropdownField>("hologram-quality");
            _particleDensity = _root.Q<Slider>("particle-density");
            _particleDensityLabel = _root.Q<Label>("particle-density-label");
            _depthOcclusion = _root.Q<Toggle>("depth-occlusion");
            _handTracking = _root.Q<Toggle>("hand-tracking");

            // Appearance
            _theme = _root.Q<DropdownField>("theme");
            _showHud = _root.Q<Toggle>("show-hud");
            _showVoiceMeter = _root.Q<Toggle>("show-voice-meter");

            // Notifications
            _notifyJoin = _root.Q<Toggle>("notify-join");
            _notifyLeave = _root.Q<Toggle>("notify-leave");
            _notifyChat = _root.Q<Toggle>("notify-chat");
            _notifyReactions = _root.Q<Toggle>("notify-reactions");

            // Footer
            _btnClose = _root.Q<Button>("btn-close");
            _btnSignOut = _root.Q<Button>("btn-sign-out");
            _versionLabel = _root.Q<Label>("version-label");

            // Set version
            if (_versionLabel != null)
                _versionLabel.text = $"H3M Hologram v{Application.version}";
        }

        void BindEvents()
        {
            // Close
            _btnClose?.RegisterCallback<ClickEvent>(_ => Close());
            _root?.RegisterCallback<ClickEvent>(e =>
            {
                // Close when clicking backdrop
                if (e.target == _root)
                    Close();
            });

            // Profile editing
            _btnEditProfile?.RegisterCallback<ClickEvent>(_ => StartEditProfile());
            _btnCancelEdit?.RegisterCallback<ClickEvent>(_ => CancelEditProfile());
            _btnSaveProfile?.RegisterCallback<ClickEvent>(_ => SaveProfile());

            // Audio sliders
            _micVolume?.RegisterValueChangedCallback(e =>
            {
                _settings.MicVolume = e.newValue / 100f;
                UpdateSliderLabel(_micVolumeLabel, e.newValue);
                NotifySettingsChanged();
            });
            _speakerVolume?.RegisterValueChangedCallback(e =>
            {
                _settings.SpeakerVolume = e.newValue / 100f;
                UpdateSliderLabel(_speakerVolumeLabel, e.newValue);
                NotifySettingsChanged();
            });

            // Audio toggles
            _noiseSuppression?.RegisterValueChangedCallback(e =>
            {
                _settings.NoiseSuppression = e.newValue;
                NotifySettingsChanged();
            });
            _echoCancellation?.RegisterValueChangedCallback(e =>
            {
                _settings.EchoCancellation = e.newValue;
                NotifySettingsChanged();
            });

            // Video
            _qualityPreset?.RegisterValueChangedCallback(e =>
            {
                _settings.QualityPresetIndex = _qualityPreset.index;
                NotifySettingsChanged();
            });
            _mirrorVideo?.RegisterValueChangedCallback(e =>
            {
                _settings.MirrorVideo = e.newValue;
                NotifySettingsChanged();
            });
            _hdVideo?.RegisterValueChangedCallback(e =>
            {
                _settings.HdVideo = e.newValue;
                NotifySettingsChanged();
            });

            // Hologram
            _hologramQuality?.RegisterValueChangedCallback(e =>
            {
                _settings.HologramQualityIndex = _hologramQuality.index;
                NotifySettingsChanged();
            });
            _particleDensity?.RegisterValueChangedCallback(e =>
            {
                _settings.ParticleDensity = e.newValue / 100f;
                UpdateSliderLabel(_particleDensityLabel, e.newValue);
                NotifySettingsChanged();
            });
            _depthOcclusion?.RegisterValueChangedCallback(e =>
            {
                _settings.DepthOcclusion = e.newValue;
                NotifySettingsChanged();
            });
            _handTracking?.RegisterValueChangedCallback(e =>
            {
                _settings.HandTracking = e.newValue;
                NotifySettingsChanged();
            });

            // Appearance
            _theme?.RegisterValueChangedCallback(e =>
            {
                _settings.ThemeIndex = _theme.index;
                NotifySettingsChanged();
            });
            _showHud?.RegisterValueChangedCallback(e =>
            {
                _settings.ShowHud = e.newValue;
                NotifySettingsChanged();
            });
            _showVoiceMeter?.RegisterValueChangedCallback(e =>
            {
                _settings.ShowVoiceMeter = e.newValue;
                NotifySettingsChanged();
            });

            // Notifications
            _notifyJoin?.RegisterValueChangedCallback(e =>
            {
                _settings.NotifyJoin = e.newValue;
                NotifySettingsChanged();
            });
            _notifyLeave?.RegisterValueChangedCallback(e =>
            {
                _settings.NotifyLeave = e.newValue;
                NotifySettingsChanged();
            });
            _notifyChat?.RegisterValueChangedCallback(e =>
            {
                _settings.NotifyChat = e.newValue;
                NotifySettingsChanged();
            });
            _notifyReactions?.RegisterValueChangedCallback(e =>
            {
                _settings.NotifyReactions = e.newValue;
                NotifySettingsChanged();
            });

            // Sign out
            _btnSignOut?.RegisterCallback<ClickEvent>(_ => SignOut());
        }

        #region Profile

        void UpdateProfileDisplay()
        {
            var user = AuthManager.Instance?.CurrentUser;
            if (user != null)
            {
                if (_profileName != null) _profileName.text = user.DisplayName ?? "User";
                if (_profileEmail != null) _profileEmail.text = user.Email ?? "";
            }
            else
            {
                if (_profileName != null) _profileName.text = "Guest";
                if (_profileEmail != null) _profileEmail.text = "Not signed in";
            }
        }

        void StartEditProfile()
        {
            _isEditingProfile = true;
            if (_editProfileFields != null)
                _editProfileFields.style.display = DisplayStyle.Flex;
            if (_editName != null)
                _editName.value = AuthManager.Instance?.UserDisplayName ?? "";
            if (_btnEditProfile != null)
                _btnEditProfile.style.display = DisplayStyle.None;
        }

        void CancelEditProfile()
        {
            _isEditingProfile = false;
            if (_editProfileFields != null)
                _editProfileFields.style.display = DisplayStyle.None;
            if (_btnEditProfile != null)
                _btnEditProfile.style.display = DisplayStyle.Flex;
        }

        async void SaveProfile()
        {
            string newName = _editName?.value?.Trim();
            if (string.IsNullOrEmpty(newName)) return;

            var authProvider = AuthManager.Instance?.AuthProvider;
            if (authProvider != null)
            {
                var result = await authProvider.UpdateProfileAsync(newName, null);
                if (result.Success)
                {
                    UpdateProfileDisplay();
                    Debug.Log($"[SettingsController] Profile updated: {newName}");
                }
            }

            CancelEditProfile();
        }

        #endregion

        #region Device Lists

        void PopulateDeviceLists()
        {
            // Microphones
            var microphones = new List<string> { "Default" };
            foreach (var device in Microphone.devices)
                microphones.Add(device);

            if (_microphoneDevice != null)
            {
                _microphoneDevice.choices = microphones;
                _microphoneDevice.index = 0;
            }

            // Speakers (Unity doesn't enumerate speakers, use Default)
            var speakers = new List<string> { "Default", "System Default" };
            if (_speakerDevice != null)
            {
                _speakerDevice.choices = speakers;
                _speakerDevice.index = 0;
            }

            // Cameras (Unity doesn't enumerate cameras on all platforms)
            var cameras = new List<string> { "Default", "Front Camera", "Rear Camera" };
            if (_cameraDevice != null)
            {
                _cameraDevice.choices = cameras;
                _cameraDevice.index = 0;
            }
        }

        #endregion

        #region Settings Persistence

        void LoadSettings()
        {
            _settings = UserSettings.Load();
            ApplySettingsToUI();
        }

        void SaveSettings()
        {
            _settings.Save();
            Debug.Log("[SettingsController] Settings saved");
        }

        void ApplySettingsToUI()
        {
            // Audio
            _micVolume?.SetValueWithoutNotify(_settings.MicVolume * 100f);
            UpdateSliderLabel(_micVolumeLabel, _settings.MicVolume * 100f);
            _speakerVolume?.SetValueWithoutNotify(_settings.SpeakerVolume * 100f);
            UpdateSliderLabel(_speakerVolumeLabel, _settings.SpeakerVolume * 100f);
            _noiseSuppression?.SetValueWithoutNotify(_settings.NoiseSuppression);
            _echoCancellation?.SetValueWithoutNotify(_settings.EchoCancellation);

            // Video
            _qualityPreset?.SetValueWithoutNotify(_qualityPreset.choices[_settings.QualityPresetIndex]);
            _mirrorVideo?.SetValueWithoutNotify(_settings.MirrorVideo);
            _hdVideo?.SetValueWithoutNotify(_settings.HdVideo);

            // Hologram
            _hologramQuality?.SetValueWithoutNotify(_hologramQuality.choices[_settings.HologramQualityIndex]);
            _particleDensity?.SetValueWithoutNotify(_settings.ParticleDensity * 100f);
            UpdateSliderLabel(_particleDensityLabel, _settings.ParticleDensity * 100f);
            _depthOcclusion?.SetValueWithoutNotify(_settings.DepthOcclusion);
            _handTracking?.SetValueWithoutNotify(_settings.HandTracking);

            // Appearance
            _theme?.SetValueWithoutNotify(_theme.choices[_settings.ThemeIndex]);
            _showHud?.SetValueWithoutNotify(_settings.ShowHud);
            _showVoiceMeter?.SetValueWithoutNotify(_settings.ShowVoiceMeter);

            // Notifications
            _notifyJoin?.SetValueWithoutNotify(_settings.NotifyJoin);
            _notifyLeave?.SetValueWithoutNotify(_settings.NotifyLeave);
            _notifyChat?.SetValueWithoutNotify(_settings.NotifyChat);
            _notifyReactions?.SetValueWithoutNotify(_settings.NotifyReactions);
        }

        void UpdateSliderLabel(Label label, float value)
        {
            if (label != null)
                label.text = $"{Mathf.RoundToInt(value)}%";
        }

        void NotifySettingsChanged()
        {
            OnSettingsChanged?.Invoke(_settings);
        }

        #endregion

        #region Navigation

        void Close()
        {
            SaveSettings();
            if (_root != null)
                _root.style.display = DisplayStyle.None;
            OnClose?.Invoke();
            Debug.Log("[SettingsController] Settings closed");
        }

        void SignOut()
        {
            if (_authController != null)
                _authController.SignOut();

            OnSignOut?.Invoke();
            Close();
            Debug.Log("[SettingsController] Sign out requested");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Show the settings panel
        /// </summary>
        public void Show()
        {
            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
                UpdateProfileDisplay();
            }
        }

        /// <summary>
        /// Hide the settings panel
        /// </summary>
        public void Hide()
        {
            Close();
        }

        /// <summary>
        /// Get current settings
        /// </summary>
        public UserSettings Settings => _settings;

        #endregion
    }

    /// <summary>
    /// User settings data with PlayerPrefs persistence
    /// </summary>
    [Serializable]
    public class UserSettings
    {
        // Audio
        public float MicVolume = 1f;
        public float SpeakerVolume = 1f;
        public bool NoiseSuppression = true;
        public bool EchoCancellation = true;

        // Video
        public int QualityPresetIndex = 1; // Medium
        public bool MirrorVideo = true;
        public bool HdVideo = false;

        // Hologram
        public int HologramQualityIndex = 1; // Balanced
        public float ParticleDensity = 0.5f;
        public bool DepthOcclusion = true;
        public bool HandTracking = true;

        // Appearance
        public int ThemeIndex = 0; // Auto
        public bool ShowHud = true;
        public bool ShowVoiceMeter = true;

        // Notifications
        public bool NotifyJoin = true;
        public bool NotifyLeave = true;
        public bool NotifyChat = true;
        public bool NotifyReactions = false;

        const string PREFS_KEY = "XRRAI_UserSettings";

        public void Save()
        {
            string json = JsonUtility.ToJson(this);
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();
        }

        public static UserSettings Load()
        {
            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                string json = PlayerPrefs.GetString(PREFS_KEY);
                try
                {
                    return JsonUtility.FromJson<UserSettings>(json);
                }
                catch
                {
                    Debug.LogWarning("[UserSettings] Failed to load settings, using defaults");
                }
            }
            return new UserSettings();
        }
    }
}
