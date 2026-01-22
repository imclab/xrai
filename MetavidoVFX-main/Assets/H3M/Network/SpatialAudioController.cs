// SpatialAudioController.cs - Spatial audio for hologram conferencing (spec-003)
// Positions audio sources at hologram locations for 3D sound
// Inspired by Apple Vision Pro Spatial Personas directional audio
//
// Features:
// - Point sounds from each remote hologram position
// - Distance-based attenuation
// - Head-related transfer function (HRTF) via Unity Audio
// - Voice priority and ducking for active speakers

using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRRAI.Hologram
{
    /// <summary>
    /// Manages spatial audio for remote holograms in conference mode.
    /// Each remote user's audio is positioned at their hologram's location.
    /// </summary>
    [RequireComponent(typeof(ConferenceLayoutManager))]
    public class SpatialAudioController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Audio Settings")]
        [SerializeField] private float _minDistance = 0.5f;
        [SerializeField] private float _maxDistance = 10f;
        [SerializeField] private float _dopplerLevel = 0f;
        [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Voice Settings")]
        [SerializeField] private float _voiceVolume = 1f;
        [SerializeField] private float _activeSpeakerBoost = 1.2f;
        [SerializeField] private float _inactiveDucking = 0.7f;
        [SerializeField] private float _voiceActivityThreshold = 0.1f;
        [SerializeField] private float _speakerTransitionTime = 0.3f;

        [Header("Spatial Blend")]
        [SerializeField, Range(0, 1)] private float _spatialBlend = 1f;
        [SerializeField] private float _spread = 30f;

        [Header("Debug")]
        [SerializeField] private bool _logAudioEvents = false;

        #endregion

        #region Private Fields

        private ConferenceLayoutManager _layoutManager;
        private readonly Dictionary<string, HologramAudioState> _audioStates = new();
        private string _activeSpeakerId;
        private float _activeSpeakerStartTime;

        #endregion

        #region Events

        public event Action<string> OnActiveSpeakerChanged;
        public event Action<string, float> OnVoiceActivity;

        #endregion

        #region Properties

        public string ActiveSpeakerId => _activeSpeakerId;
        public int ActiveAudioSourceCount => _audioStates.Count;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            _layoutManager = GetComponent<ConferenceLayoutManager>();
        }

        private void OnEnable()
        {
            _layoutManager.OnHologramSeated += OnHologramSeated;
            _layoutManager.OnHologramUnseated += OnHologramUnseated;
        }

        private void OnDisable()
        {
            _layoutManager.OnHologramSeated -= OnHologramSeated;
            _layoutManager.OnHologramUnseated -= OnHologramUnseated;
        }

        private void Update()
        {
            UpdateVoiceActivity();
            UpdateVolumes();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Feed audio data from WebRTC for a remote peer.
        /// </summary>
        public void FeedAudio(string peerId, float[] samples, int channels, int sampleRate)
        {
            if (!_audioStates.TryGetValue(peerId, out var state))
            {
                Debug.LogWarning($"[SpatialAudio] No audio state for peer {peerId}");
                return;
            }

            // Create AudioClip from samples
            if (state.StreamClip == null || state.StreamClip.samples != samples.Length)
            {
                if (state.StreamClip != null)
                    Destroy(state.StreamClip);

                state.StreamClip = AudioClip.Create(
                    $"VoiceStream_{peerId}",
                    samples.Length,
                    channels,
                    sampleRate,
                    false
                );
                state.AudioSource.clip = state.StreamClip;
                state.AudioSource.loop = true;
                state.AudioSource.Play();
                _audioStates[peerId] = state;
            }

            // Update clip data
            state.StreamClip.SetData(samples, 0);

            // Update voice activity
            float rms = CalculateRMS(samples);
            state.VoiceLevel = rms;
            _audioStates[peerId] = state;

            OnVoiceActivity?.Invoke(peerId, rms);
        }

        /// <summary>
        /// Get the AudioSource for a peer (for external WebRTC binding).
        /// </summary>
        public AudioSource GetAudioSource(string peerId)
        {
            if (_audioStates.TryGetValue(peerId, out var state))
            {
                return state.AudioSource;
            }
            return null;
        }

        /// <summary>
        /// Set manual volume for a peer (0-1).
        /// </summary>
        public void SetPeerVolume(string peerId, float volume)
        {
            if (_audioStates.TryGetValue(peerId, out var state))
            {
                state.ManualVolume = Mathf.Clamp01(volume);
                _audioStates[peerId] = state;
            }
        }

        /// <summary>
        /// Mute/unmute a specific peer.
        /// </summary>
        public void SetPeerMuted(string peerId, bool muted)
        {
            if (_audioStates.TryGetValue(peerId, out var state))
            {
                state.IsMuted = muted;
                state.AudioSource.mute = muted;
                _audioStates[peerId] = state;
            }
        }

        /// <summary>
        /// Get voice activity level for a peer (0-1).
        /// </summary>
        public float GetVoiceLevel(string peerId)
        {
            if (_audioStates.TryGetValue(peerId, out var state))
            {
                return state.VoiceLevel;
            }
            return 0f;
        }

        #endregion

        #region Event Handlers

        private void OnHologramSeated(string peerId, SeatPose seat)
        {
            if (_audioStates.ContainsKey(peerId))
            {
                Debug.LogWarning($"[SpatialAudio] Audio source already exists for {peerId}");
                return;
            }

            // Create audio source at hologram position
            var hologramTransform = GetHologramTransform(peerId);
            if (hologramTransform == null)
            {
                Debug.LogError($"[SpatialAudio] Could not find hologram transform for {peerId}");
                return;
            }

            // Create child object for audio
            var audioGO = new GameObject($"Audio_{peerId}");
            audioGO.transform.SetParent(hologramTransform);
            audioGO.transform.localPosition = Vector3.up * 0.3f; // Head height offset

            var audioSource = audioGO.AddComponent<AudioSource>();
            ConfigureAudioSource(audioSource);

            var state = new HologramAudioState
            {
                PeerId = peerId,
                AudioSource = audioSource,
                ManualVolume = 1f,
                VoiceLevel = 0f,
                IsMuted = false,
                StreamClip = null
            };

            _audioStates[peerId] = state;

            if (_logAudioEvents)
                Debug.Log($"[SpatialAudio] Created audio source for {peerId}");
        }

        private void OnHologramUnseated(string peerId)
        {
            if (_audioStates.TryGetValue(peerId, out var state))
            {
                if (state.AudioSource != null)
                {
                    Destroy(state.AudioSource.gameObject);
                }
                if (state.StreamClip != null)
                {
                    Destroy(state.StreamClip);
                }
                _audioStates.Remove(peerId);

                if (_activeSpeakerId == peerId)
                {
                    _activeSpeakerId = null;
                    OnActiveSpeakerChanged?.Invoke(null);
                }

                if (_logAudioEvents)
                    Debug.Log($"[SpatialAudio] Removed audio source for {peerId}");
            }
        }

        #endregion

        #region Internal Methods

        private void ConfigureAudioSource(AudioSource source)
        {
            source.spatialBlend = _spatialBlend;
            source.minDistance = _minDistance;
            source.maxDistance = _maxDistance;
            source.dopplerLevel = _dopplerLevel;
            source.rolloffMode = _rolloffMode;
            source.spread = _spread;
            source.volume = _voiceVolume;
            source.playOnAwake = false;
        }

        private Transform GetHologramTransform(string peerId)
        {
            // Find hologram by peer ID (assumes parent set this up)
            var seat = _layoutManager.GetSeatForPeer(peerId);
            if (!string.IsNullOrEmpty(seat.OccupantId))
            {
                // Find in scene by naming convention
                var go = GameObject.Find($"Hologram_{peerId}");
                return go?.transform;
            }
            return null;
        }

        private void UpdateVoiceActivity()
        {
            // Find the loudest speaker
            string loudestPeer = null;
            float loudestLevel = _voiceActivityThreshold;

            foreach (var kvp in _audioStates)
            {
                if (kvp.Value.VoiceLevel > loudestLevel && !kvp.Value.IsMuted)
                {
                    loudestLevel = kvp.Value.VoiceLevel;
                    loudestPeer = kvp.Key;
                }
            }

            // Update active speaker with hysteresis
            if (loudestPeer != _activeSpeakerId)
            {
                if (loudestPeer != null || Time.time - _activeSpeakerStartTime > _speakerTransitionTime)
                {
                    _activeSpeakerId = loudestPeer;
                    _activeSpeakerStartTime = Time.time;
                    OnActiveSpeakerChanged?.Invoke(_activeSpeakerId);

                    if (_logAudioEvents)
                        Debug.Log($"[SpatialAudio] Active speaker changed to: {_activeSpeakerId ?? "none"}");
                }
            }
        }

        private void UpdateVolumes()
        {
            foreach (var kvp in _audioStates)
            {
                var state = kvp.Value;
                if (state.AudioSource == null) continue;

                // Base volume
                float targetVolume = _voiceVolume * state.ManualVolume;

                // Active speaker boost / ducking
                if (!string.IsNullOrEmpty(_activeSpeakerId))
                {
                    if (kvp.Key == _activeSpeakerId)
                    {
                        targetVolume *= _activeSpeakerBoost;
                    }
                    else
                    {
                        targetVolume *= _inactiveDucking;
                    }
                }

                // Smooth transition
                state.AudioSource.volume = Mathf.Lerp(
                    state.AudioSource.volume,
                    targetVolume,
                    Time.deltaTime * 5f
                );
            }
        }

        private float CalculateRMS(float[] samples)
        {
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += samples[i] * samples[i];
            }
            return Mathf.Sqrt(sum / samples.Length);
        }

        #endregion

        #region Internal Types

        private struct HologramAudioState
        {
            public string PeerId;
            public AudioSource AudioSource;
            public AudioClip StreamClip;
            public float ManualVolume;
            public float VoiceLevel;
            public bool IsMuted;
        }

        #endregion
    }
}
