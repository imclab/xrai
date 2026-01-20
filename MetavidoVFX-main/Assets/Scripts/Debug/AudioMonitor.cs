using UnityEngine;

/// <summary>
/// Real-time audio output monitor displaying AudioBridge data.
/// Shows frequency bands, volume, beat detection with visual bars.
/// Toggle visibility with 'M' key or set ShowMonitor in inspector.
/// </summary>
public class AudioMonitor : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] bool _showMonitor = true;
    [SerializeField] KeyCode _toggleKey = KeyCode.M;

    [Header("Position")]
    [SerializeField] Vector2 _position = new Vector2(10, 10);
    [SerializeField] float _width = 220f;
    [SerializeField] float _barHeight = 20f;

    [Header("Colors")]
    [SerializeField] Color _subBassColor = new Color(0.8f, 0.2f, 0.8f); // Purple
    [SerializeField] Color _bassColor = new Color(1f, 0.3f, 0.3f);      // Red
    [SerializeField] Color _midsColor = new Color(0.3f, 1f, 0.3f);      // Green
    [SerializeField] Color _trebleColor = new Color(0.3f, 0.6f, 1f);    // Blue
    [SerializeField] Color _volumeColor = new Color(1f, 1f, 1f);        // White
    [SerializeField] Color _beatColor = new Color(1f, 0.8f, 0f);        // Yellow
    [SerializeField] Color _backgroundColor = new Color(0, 0, 0, 0.7f);

    [Header("Scaling")]
    [SerializeField] float _bandScale = 5f;  // Multiply band values for visibility
    [SerializeField] float _volumeScale = 3f;

    // Cached textures
    Texture2D _whiteTex;
    Texture2D _bgTex;

    // Smoothed values for visual appeal
    float _smoothVolume;
    float _smoothBass;
    float _smoothMids;
    float _smoothTreble;
    float _smoothSubBass;
    float _smoothBeatPulse;

    const float SMOOTH_SPEED = 15f;

    public bool ShowMonitor
    {
        get => _showMonitor;
        set => _showMonitor = value;
    }

    void Start()
    {
        // Create white texture for bars
        _whiteTex = new Texture2D(1, 1);
        _whiteTex.SetPixel(0, 0, Color.white);
        _whiteTex.Apply();

        // Create background texture
        _bgTex = new Texture2D(1, 1);
        _bgTex.SetPixel(0, 0, _backgroundColor);
        _bgTex.Apply();
    }

    void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
            _showMonitor = !_showMonitor;

        // Smooth values
        var bridge = AudioBridge.Instance;
        if (bridge != null)
        {
            float dt = Time.deltaTime * SMOOTH_SPEED;
            _smoothVolume = Mathf.Lerp(_smoothVolume, bridge.Volume, dt);
            _smoothBass = Mathf.Lerp(_smoothBass, bridge.Bass, dt);
            _smoothMids = Mathf.Lerp(_smoothMids, bridge.Mids, dt);
            _smoothTreble = Mathf.Lerp(_smoothTreble, bridge.Treble, dt);
            _smoothSubBass = Mathf.Lerp(_smoothSubBass, bridge.SubBass, dt);
            _smoothBeatPulse = Mathf.Lerp(_smoothBeatPulse, bridge.BeatPulse, dt * 2f); // Faster for pulse
        }
    }

    void OnGUI()
    {
        if (!_showMonitor) return;

        var bridge = AudioBridge.Instance;
        if (bridge == null)
        {
            GUI.Label(new Rect(_position.x, _position.y, 200, 25), "AudioBridge not found");
            return;
        }

        float y = _position.y;
        float labelWidth = 70f;
        float barWidth = _width - labelWidth - 50f;
        float valueWidth = 45f;
        float spacing = 4f;
        float totalHeight = (_barHeight + spacing) * 8 + 40f;

        // Background
        GUI.DrawTexture(new Rect(_position.x - 5, _position.y - 5, _width + 10, totalHeight), _bgTex);

        // Title with input source
        GUI.color = Color.white;
        string inputSource = bridge.InputMode == AudioBridge.AudioInputMode.Microphone
            ? (bridge.IsMicrophoneActive ? "🎤 MIC" : "🎤 (init...)")
            : "🔊 CLIP";
        GUI.Label(new Rect(_position.x, y, _width, 20), $"<b>AUDIO MONITOR</b> [{inputSource}]");
        y += 25f;

        // Volume bar
        DrawBar("Volume", _smoothVolume * _volumeScale, bridge.Volume, _volumeColor, ref y, labelWidth, barWidth, valueWidth, spacing);

        // Frequency bands
        DrawBar("SubBass", _smoothSubBass * _bandScale, bridge.SubBass, _subBassColor, ref y, labelWidth, barWidth, valueWidth, spacing);
        DrawBar("Bass", _smoothBass * _bandScale, bridge.Bass, _bassColor, ref y, labelWidth, barWidth, valueWidth, spacing);
        DrawBar("Mids", _smoothMids * _bandScale, bridge.Mids, _midsColor, ref y, labelWidth, barWidth, valueWidth, spacing);
        DrawBar("Treble", _smoothTreble * _bandScale, bridge.Treble, _trebleColor, ref y, labelWidth, barWidth, valueWidth, spacing);

        // Beat detection
        y += 5f;
        DrawBar("BeatPulse", _smoothBeatPulse, bridge.BeatPulse, _beatColor, ref y, labelWidth, barWidth, valueWidth, spacing);
        DrawBar("BeatInt", bridge.BeatIntensity, bridge.BeatIntensity, _beatColor, ref y, labelWidth, barWidth, valueWidth, spacing);

        // Beat onset indicator
        if (bridge.IsOnset)
        {
            GUI.color = _beatColor;
            GUI.Label(new Rect(_position.x + labelWidth, y, 100, 20), "★ BEAT! ★");
        }
        else
        {
            GUI.color = new Color(0.5f, 0.5f, 0.5f);
            float timeSince = bridge.TimeSinceLastBeat;
            GUI.Label(new Rect(_position.x + labelWidth, y, 100, 20), $"Last: {timeSince:F2}s");
        }

        GUI.color = Color.white;
    }

    void DrawBar(string label, float normalizedValue, float rawValue, Color color, ref float y, float labelWidth, float barWidth, float valueWidth, float spacing)
    {
        float x = _position.x;

        // Label
        GUI.color = color;
        GUI.Label(new Rect(x, y, labelWidth, _barHeight), label);
        x += labelWidth;

        // Background bar
        GUI.color = new Color(0.2f, 0.2f, 0.2f);
        GUI.DrawTexture(new Rect(x, y + 2, barWidth, _barHeight - 4), _whiteTex);

        // Filled bar (clamped to 0-1)
        float fillWidth = Mathf.Clamp01(normalizedValue) * barWidth;
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y + 2, fillWidth, _barHeight - 4), _whiteTex);
        x += barWidth + 5;

        // Value text
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, valueWidth, _barHeight), rawValue.ToString("F3"));

        y += _barHeight + spacing;
    }

    void OnDestroy()
    {
        if (_whiteTex != null) Destroy(_whiteTex);
        if (_bgTex != null) Destroy(_bgTex);
    }
}
