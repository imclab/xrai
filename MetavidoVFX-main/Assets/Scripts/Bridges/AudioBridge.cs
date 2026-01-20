using UnityEngine;
using MetavidoVFX.Audio;

/// <summary>
/// FFT analysis → global audio properties for ALL VFX.
/// Includes 4-band frequency analysis and beat detection (spec-007).
/// Also provides AudioDataTexture for VFX without exposed properties.
/// </summary>
public class AudioBridge : MonoBehaviour
{
    public static AudioBridge Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] AudioSource _source;
    [Range(64, 8192)] [SerializeField] int _sampleCount = 1024;

    [Header("Beat Detection (spec-007)")]
    [Tooltip("Enable beat detection for audio-reactive VFX")]
    [SerializeField] bool _enableBeatDetection = true;

    [Tooltip("Multiplier for adaptive threshold (higher = less sensitive)")]
    [Range(1f, 3f)]
    [SerializeField] float _beatThreshold = 1.5f;

    [Tooltip("Pulse decay time in seconds")]
    [Range(0.05f, 0.5f)]
    [SerializeField] float _pulseDecayTime = 0.1f;

    [Tooltip("Minimum interval between beats (prevents double-triggers)")]
    [Range(0.05f, 0.5f)]
    [SerializeField] float _minBeatInterval = 0.1f;

    [Header("Audio Data Texture")]
    [Tooltip("Enable texture output for VFX without exposed audio properties")]
    [SerializeField] bool _enableAudioDataTexture = true;

    [Header("Debug")]
    [SerializeField] bool _verboseLogging = false;

    float[] _spectrum;
    BeatDetector _beatDetector;

    // Audio data texture (2x2 RGBA float): encodes all audio values
    // Pixel layout:
    //   (0,0): Volume, Bass, Mids, Treble
    //   (1,0): SubBass, BeatPulse, BeatIntensity, 0
    Texture2D _audioDataTexture;
    Color[] _audioDataPixels;

    /// <summary>
    /// 2x2 texture containing all audio data for VFX binding.
    /// Sample at UV (0.25, 0.25) for (Volume, Bass, Mids, Treble)
    /// Sample at UV (0.75, 0.25) for (SubBass, BeatPulse, BeatIntensity, 0)
    /// </summary>
    public Texture2D AudioDataTexture => _audioDataTexture;

    // Shader property IDs
    static readonly int _AudioBandsID = Shader.PropertyToID("_AudioBands");
    static readonly int _AudioVolumeID = Shader.PropertyToID("_AudioVolume");
    static readonly int _BeatPulseID = Shader.PropertyToID("_BeatPulse");
    static readonly int _BeatIntensityID = Shader.PropertyToID("_BeatIntensity");

    // Public outputs
    public float Volume { get; private set; }
    public float Bass { get; private set; }
    public float Mids { get; private set; }
    public float Treble { get; private set; }
    public float SubBass { get; private set; }

    // Beat detection outputs (spec-007)
    public float BeatPulse => _beatDetector?.BeatPulse ?? 0f;
    public float BeatIntensity => _beatDetector?.BeatIntensity ?? 0f;
    public bool IsOnset => _beatDetector?.IsOnset ?? false;
    public float TimeSinceLastBeat => _beatDetector?.TimeSinceLastBeat ?? 999f;

    // Beat detection control
    public bool BeatDetectionEnabled
    {
        get => _enableBeatDetection;
        set => _enableBeatDetection = value;
    }

    void Awake() => Instance = this;

    void Start()
    {
        _source ??= GetComponent<AudioSource>() ?? FindFirstObjectByType<AudioSource>();
        _spectrum = new float[_sampleCount];

        // Initialize beat detector (spec-007)
        int sampleRate = AudioSettings.outputSampleRate;
        _beatDetector = new BeatDetector(
            sampleCount: _sampleCount,
            sampleRate: sampleRate,
            thresholdMultiplier: _beatThreshold,
            pulseDecayTime: _pulseDecayTime
        );
        _beatDetector.MinBeatInterval = _minBeatInterval;

        // Initialize audio data texture (2x2 for efficient sampling)
        if (_enableAudioDataTexture)
        {
            _audioDataTexture = new Texture2D(2, 2, TextureFormat.RGBAFloat, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "AudioDataTexture"
            };
            _audioDataPixels = new Color[4];

            if (_verboseLogging)
                Debug.Log("[AudioBridge] Created AudioDataTexture (2x2 RGBAFloat)");
        }

        if (_verboseLogging)
            Debug.Log($"[AudioBridge] Initialized with {_sampleCount} samples, {sampleRate}Hz, beat detection: {_enableBeatDetection}");
    }

    void Update()
    {
        if (_source == null || !_source.isPlaying)
        {
            Volume = 0;
            Bass = Mids = Treble = SubBass = 0;

            // Reset beat detector when audio stops
            _beatDetector?.Reset();

            // Set zeroed shader properties
            Shader.SetGlobalVector(_AudioBandsID, Vector4.zero);
            Shader.SetGlobalFloat(_AudioVolumeID, 0f);
            Shader.SetGlobalFloat(_BeatPulseID, 0f);
            Shader.SetGlobalFloat(_BeatIntensityID, 0f);
            return;
        }

        _source.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);

        // Compute 4 bands: SubBass (20-60Hz), Bass (60-250Hz), Mids (250-2kHz), Treble (2k-16kHz)
        // Bin indices depend on sample rate and FFT size
        // For 1024 samples at 44.1kHz: bin width = 44100/1024 ≈ 43Hz
        SubBass = Average(_spectrum, 0, 2);      // ~0-86Hz
        Bass = Average(_spectrum, 2, 6);         // ~86-258Hz
        Mids = Average(_spectrum, 6, 48);        // ~258-2064Hz
        Treble = Average(_spectrum, 48, 372);    // ~2064-16kHz

        Volume = (SubBass + Bass + Mids + Treble) * 0.25f;

        // Set global shader properties (frequency bands)
        Shader.SetGlobalVector(_AudioBandsID, new Vector4(Bass, Mids, Treble, SubBass) * 100f);
        Shader.SetGlobalFloat(_AudioVolumeID, Volume);

        // Beat detection (spec-007)
        if (_enableBeatDetection && _beatDetector != null)
        {
            // Update beat detector parameters if changed in inspector
            _beatDetector.ThresholdMultiplier = _beatThreshold;
            _beatDetector.PulseDecayTime = _pulseDecayTime;
            _beatDetector.MinBeatInterval = _minBeatInterval;

            // Process spectrum for beat detection
            _beatDetector.Process(_spectrum);

            // Set beat shader properties
            Shader.SetGlobalFloat(_BeatPulseID, _beatDetector.BeatPulse);
            Shader.SetGlobalFloat(_BeatIntensityID, _beatDetector.BeatIntensity);

            if (_verboseLogging && _beatDetector.IsOnset)
                Debug.Log($"[AudioBridge] BEAT! Intensity: {_beatDetector.BeatIntensity:F2}");
        }
        else
        {
            Shader.SetGlobalFloat(_BeatPulseID, 0f);
            Shader.SetGlobalFloat(_BeatIntensityID, 0f);
        }

        // Update audio data texture for VFX without exposed properties
        UpdateAudioDataTexture();
    }

    void UpdateAudioDataTexture()
    {
        if (!_enableAudioDataTexture || _audioDataTexture == null) return;

        // Pixel (0,0): Volume, Bass, Mids, Treble
        _audioDataPixels[0] = new Color(Volume, Bass, Mids, Treble);
        // Pixel (1,0): SubBass, BeatPulse, BeatIntensity, reserved
        _audioDataPixels[1] = new Color(SubBass, BeatPulse, BeatIntensity, 0f);
        // Pixel (0,1) and (1,1): reserved for future use
        _audioDataPixels[2] = Color.clear;
        _audioDataPixels[3] = Color.clear;

        _audioDataTexture.SetPixels(_audioDataPixels);
        _audioDataTexture.Apply(false, false);
    }

    void OnDestroy()
    {
        if (_audioDataTexture != null)
        {
            Destroy(_audioDataTexture);
            _audioDataTexture = null;
        }
    }

    float Average(float[] data, int start, int end)
    {
        if (end > data.Length) end = data.Length;
        if (start >= end) return 0;

        float sum = 0;
        for (int i = start; i < end; i++) sum += data[i];
        return sum / (end - start);
    }

    /// <summary>
    /// Enable or disable beat detection at runtime (demand-driven).
    /// </summary>
    public void SetBeatDetectionEnabled(bool enabled)
    {
        _enableBeatDetection = enabled;
        if (!enabled)
            _beatDetector?.Reset();

        if (_verboseLogging)
            Debug.Log($"[AudioBridge] Beat detection: {enabled}");
    }

    [ContextMenu("Debug Audio Bridge")]
    void DebugAudioBridge()
    {
        Debug.Log("=== AudioBridge Debug ===");
        Debug.Log($"Source: {_source}, Playing: {_source?.isPlaying}");
        Debug.Log($"Volume: {Volume:F3}, Bass: {Bass:F3}, Mids: {Mids:F3}, Treble: {Treble:F3}");
        Debug.Log($"Beat Detection: {_enableBeatDetection}");
        if (_beatDetector != null)
            Debug.Log(_beatDetector.GetDebugInfo());
    }
}
