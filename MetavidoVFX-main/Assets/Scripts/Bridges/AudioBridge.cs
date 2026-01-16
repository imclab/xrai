using UnityEngine;

/// <summary>
/// FFT analysis → global audio properties for ALL VFX.
/// </summary>
public class AudioBridge : MonoBehaviour
{
    public static AudioBridge Instance { get; private set; }

    [SerializeField] AudioSource _source;
    [Range(64, 8192)] [SerializeField] int _sampleCount = 1024;

    float[] _spectrum;
    static readonly int _AudioBands = Shader.PropertyToID("_AudioBands");

    public float Volume { get; private set; }

    void Awake() => Instance = this;

    void Start()
    {
        _source ??= GetComponent<AudioSource>() ?? FindFirstObjectByType<AudioSource>();
        _spectrum = new float[_sampleCount];
    }

    void Update()
    {
        if (_source == null || !_source.isPlaying)
        {
            Volume = 0;
            return;
        }

        _source.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);

        // Compute 4 bands (bass, lowmid, highmid, treble)
        float bass = Average(_spectrum, 0, 4);
        float lowmid = Average(_spectrum, 4, 16);
        float highmid = Average(_spectrum, 16, 64);
        float treble = Average(_spectrum, 64, 256);

        Volume = (bass + lowmid + highmid + treble) * 0.25f;

        Shader.SetGlobalVector(_AudioBands, new Vector4(bass, lowmid, highmid, treble) * 100f);
    }

    float Average(float[] data, int start, int end)
    {
        float sum = 0;
        for (int i = start; i < Mathf.Min(end, data.Length); i++) sum += data[i];
        return sum / (end - start);
    }
}
