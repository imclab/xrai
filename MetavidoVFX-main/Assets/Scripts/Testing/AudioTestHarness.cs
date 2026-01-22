// AudioTestHarness - Spec 007 T-017 Audio Test Scene Setup
// Validates: Beat detection, frequency bands, audio-reactive VFX

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using MetavidoVFX.VFX.Binders;

namespace MetavidoVFX.Testing
{
    /// <summary>
    /// Test harness for audio-reactive VFX validation.
    /// Creates multiple VFX instances responding to different audio bands.
    /// Press Space to cycle test audio clips, M to toggle AudioMonitor.
    /// </summary>
    public class AudioTestHarness : MonoBehaviour
    {
        [Header("VFX Setup")]
        [Tooltip("VFX assets to test (leave empty to auto-load from Resources)")]
        [SerializeField] private VisualEffectAsset[] testVFXAssets;
        [SerializeField] private int maxVFXCount = 5;
        [SerializeField] private float vfxSpacing = 2f;

        [Header("Audio")]
        [SerializeField] private AudioClip[] testAudioClips;
        [SerializeField] private AudioSource audioSource;

        [Header("Test Configuration")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool showBeatIndicator = true;

        [Header("Runtime Status (Read-Only)")]
        [SerializeField] private int _currentClipIndex = 0;
        [SerializeField] private int _activeVFXCount = 0;
        [SerializeField] private float _currentVolume = 0f;
        [SerializeField] private float _currentBeatPulse = 0f;
        [SerializeField] private int _beatCount = 0;

        private List<VisualEffect> _testVFX = new();
        private AudioBridge _audioBridge;
        private float _lastBeatPulse;

        void Start()
        {
            _audioBridge = AudioBridge.Instance ?? FindFirstObjectByType<AudioBridge>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

            if (testVFXAssets == null || testVFXAssets.Length == 0)
                LoadDefaultVFX();

            SetupTestVFX();

            if (autoStart && testAudioClips != null && testAudioClips.Length > 0)
            {
                PlayClip(0);
            }

            Debug.Log($"[AudioTestHarness] Initialized with {_testVFX.Count} VFX, {testAudioClips?.Length ?? 0} audio clips");
        }

        void Update()
        {
            // Cycle audio clips with Space
            if (Input.GetKeyDown(KeyCode.Space) && testAudioClips != null && testAudioClips.Length > 0)
            {
                _currentClipIndex = (_currentClipIndex + 1) % testAudioClips.Length;
                PlayClip(_currentClipIndex);
            }

            // Update runtime status
            UpdateRuntimeStatus();
        }

        void UpdateRuntimeStatus()
        {
            _activeVFXCount = _testVFX.Count;

            if (_audioBridge != null)
            {
                _currentVolume = _audioBridge.Volume;
                _currentBeatPulse = _audioBridge.BeatPulse;

                // Count beats (rising edge detection)
                if (_currentBeatPulse > 0.5f && _lastBeatPulse <= 0.5f)
                {
                    _beatCount++;
                }
                _lastBeatPulse = _currentBeatPulse;
            }
        }

        void LoadDefaultVFX()
        {
            // Load a few VFX from Resources for testing
            var allVFX = Resources.LoadAll<VisualEffectAsset>("VFX");
            var selectedVFX = new List<VisualEffectAsset>();

            // Pick VFX that work well for audio visualization
            string[] preferredNames = { "particles", "spark", "flame", "point", "bubble" };

            foreach (var vfx in allVFX)
            {
                if (selectedVFX.Count >= maxVFXCount) break;

                foreach (var name in preferredNames)
                {
                    if (vfx.name.ToLower().Contains(name))
                    {
                        selectedVFX.Add(vfx);
                        break;
                    }
                }
            }

            // Fill with any VFX if not enough
            foreach (var vfx in allVFX)
            {
                if (selectedVFX.Count >= maxVFXCount) break;
                if (!selectedVFX.Contains(vfx))
                    selectedVFX.Add(vfx);
            }

            testVFXAssets = selectedVFX.ToArray();
            Debug.Log($"[AudioTestHarness] Loaded {testVFXAssets.Length} VFX from Resources");
        }

        void SetupTestVFX()
        {
            float startX = -(testVFXAssets.Length - 1) * vfxSpacing / 2f;

            for (int i = 0; i < testVFXAssets.Length; i++)
            {
                var asset = testVFXAssets[i];
                if (asset == null) continue;

                // Create VFX GameObject
                var go = new GameObject($"AudioVFX_{asset.name}");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(startX + i * vfxSpacing, 0, 0);

                // Add VisualEffect
                var vfx = go.AddComponent<VisualEffect>();
                vfx.visualEffectAsset = asset;

                // Add VFXARBinder for depth/position data
                var arBinder = go.AddComponent<VFXARBinder>();

                // Add VFXAudioDataBinder for audio reactivity
                var audioBinder = go.AddComponent<VFXAudioDataBinder>();
                audioBinder.bindBeatDetection = true;

                _testVFX.Add(vfx);

                Debug.Log($"[AudioTestHarness] Created VFX: {asset.name} at ({go.transform.localPosition.x:F1}, 0, 0)");
            }
        }

        void PlayClip(int index)
        {
            if (testAudioClips == null || index >= testAudioClips.Length) return;

            var clip = testAudioClips[index];
            if (clip == null) return;

            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();

            // Wire to AudioBridge
            if (_audioBridge != null)
            {
                // AudioBridge should auto-detect the AudioSource
            }

            Debug.Log($"[AudioTestHarness] Playing: {clip.name}");
        }

        void OnGUI()
        {
            if (!showBeatIndicator) return;

            // Beat indicator in top-left
            var beatColor = Color.Lerp(Color.gray, Color.red, _currentBeatPulse);
            GUI.backgroundColor = beatColor;
            GUI.Box(new Rect(10, 10, 100, 30), $"Beat: {_beatCount}");

            // Volume bar
            GUI.backgroundColor = Color.green;
            GUI.Box(new Rect(10, 50, _currentVolume * 200, 20), "");
            GUI.backgroundColor = Color.white;
            GUI.Label(new Rect(10, 50, 200, 20), $"Vol: {_currentVolume:F2}");

            // Instructions
            GUI.Label(new Rect(10, 80, 300, 20), "Space: Cycle audio | Tab: Dashboard | M: AudioMonitor");
        }
    }
}
