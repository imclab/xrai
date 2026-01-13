using UnityEngine;
using UnityEngine.VFX;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

/// <summary>
/// High-performance audio reactive VFX for iOS using Unity microphone input
/// Features: Real-time FFT, spectrum analysis, beat detection, optimized for mobile
/// </summary>
public class AudioReactiveVFXiOS : MonoBehaviour
{
    [Header("Audio Configuration")]
    [SerializeField] private int sampleRate = 44100;
    [SerializeField] private int fftSize = 1024; // Must be power of 2
    [SerializeField] private FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    
    [Header("VFX Integration")]
    [SerializeField] private VisualEffect vfxGraph;
    [SerializeField] private int spectrumBands = 64;
    [SerializeField] private float amplitudeMultiplier = 100f;
    
    [Header("Beat Detection")]
    [SerializeField] private bool enableBeatDetection = true;
    [SerializeField] private float beatThreshold = 1.5f;
    [SerializeField] private float beatDecay = 0.95f;
    
    [Header("Performance")]
    [SerializeField] private bool useJobSystem = true;
    [SerializeField] private int updateFrameInterval = 1; // Update every N frames
    
    // Audio components
    private AudioSource audioSource;
    private string microphoneName;
    private float[] samples;
    private float[] spectrum;
    private float[] bandLevels;
    private float[] bandVelocities;
    private float[] previousBandLevels;
    
    // FFT processing
    private NativeArray<float> nativeSamples;
    private NativeArray<float> nativeSpectrum;
    private NativeArray<float> nativeBands;
    
    // Beat detection
    private float averageEnergy;
    private float beatEnergy;
    private bool isBeat;
    private float beatTimer;
    
    // VFX textures
    private Texture2D spectrumTexture;
    private RenderTexture spectrumRT;
    
    // Performance
    private int frameCounter;
    
    private void Start()
    {
        InitializeAudio();
        InitializeArrays();
        InitializeVFX();
        RequestMicrophonePermission();
    }
    
    private void InitializeAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.mute = true; // Mute to avoid feedback
        
        // Check available microphones
        if (Microphone.devices.Length > 0)
        {
            microphoneName = Microphone.devices[0];
            Debug.Log($"Using microphone: {microphoneName}");
        }
        else
        {
            Debug.LogError("No microphone detected!");
            enabled = false;
            return;
        }
    }
    
    private void InitializeArrays()
    {
        samples = new float[fftSize];
        spectrum = new float[fftSize];
        bandLevels = new float[spectrumBands];
        bandVelocities = new float[spectrumBands];
        previousBandLevels = new float[spectrumBands];
        
        if (useJobSystem)
        {
            nativeSamples = new NativeArray<float>(fftSize, Allocator.Persistent);
            nativeSpectrum = new NativeArray<float>(fftSize, Allocator.Persistent);
            nativeBands = new NativeArray<float>(spectrumBands, Allocator.Persistent);
        }
    }
    
    private void InitializeVFX()
    {
        // Create spectrum texture for VFX Graph
        spectrumTexture = new Texture2D(spectrumBands, 1, TextureFormat.RFloat, false);
        spectrumTexture.filterMode = FilterMode.Bilinear;
        spectrumTexture.wrapMode = TextureWrapMode.Clamp;
        
        // Create RT for smooth interpolation
        spectrumRT = new RenderTexture(spectrumBands, 1, 0, RenderTextureFormat.RFloat);
        spectrumRT.filterMode = FilterMode.Bilinear;
        
        // Set VFX properties
        vfxGraph.SetTexture("SpectrumTexture", spectrumRT);
        vfxGraph.SetInt("SpectrumBands", spectrumBands);
    }
    
    private void RequestMicrophonePermission()
    {
        #if UNITY_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
            StartCoroutine(WaitForMicrophonePermission());
        }
        else
        {
            StartMicrophone();
        }
        #else
        StartMicrophone();
        #endif
    }
    
    private System.Collections.IEnumerator WaitForMicrophonePermission()
    {
        while (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            yield return null;
        }
        StartMicrophone();
    }
    
    private void StartMicrophone()
    {
        audioSource.clip = Microphone.Start(microphoneName, true, 1, sampleRate);
        
        // Wait until the recording has started
        while (!(Microphone.GetPosition(microphoneName) > 0)) { }
        
        audioSource.Play();
        Debug.Log("Microphone started");
    }
    
    private void Update()
    {
        frameCounter++;
        if (frameCounter % updateFrameInterval != 0) return;
        
        if (!audioSource.isPlaying) return;
        
        // Get audio samples
        audioSource.GetOutputData(samples, 0);
        
        if (useJobSystem)
        {
            ProcessAudioWithJobs();
        }
        else
        {
            ProcessAudio();
        }
        
        UpdateVFX();
    }
    
    private void ProcessAudio()
    {
        // Apply FFT
        audioSource.GetSpectrumData(spectrum, 0, fftWindow);
        
        // Calculate frequency bands
        CalculateFrequencyBands();
        
        // Beat detection
        if (enableBeatDetection)
        {
            DetectBeat();
        }
        
        // Calculate velocities
        for (int i = 0; i < spectrumBands; i++)
        {
            bandVelocities[i] = bandLevels[i] - previousBandLevels[i];
            previousBandLevels[i] = bandLevels[i];
        }
    }
    
    private void ProcessAudioWithJobs()
    {
        // Copy data to native arrays
        nativeSamples.CopyFrom(samples);
        
        // Schedule FFT job
        var fftJob = new FFTJob
        {
            samples = nativeSamples,
            spectrum = nativeSpectrum,
            fftSize = fftSize
        };
        
        var fftHandle = fftJob.Schedule();
        
        // Schedule band calculation job
        var bandJob = new FrequencyBandJob
        {
            spectrum = nativeSpectrum,
            bands = nativeBands,
            spectrumSize = fftSize,
            bandCount = spectrumBands,
            amplitudeMultiplier = amplitudeMultiplier
        };
        
        var bandHandle = bandJob.Schedule(spectrumBands, 32, fftHandle);
        bandHandle.Complete();
        
        // Copy results back
        nativeBands.CopyTo(bandLevels);
        
        // Calculate velocities
        for (int i = 0; i < spectrumBands; i++)
        {
            bandVelocities[i] = bandLevels[i] - previousBandLevels[i];
            previousBandLevels[i] = bandLevels[i];
        }
        
        if (enableBeatDetection)
        {
            DetectBeat();
        }
    }
    
    private void CalculateFrequencyBands()
    {
        int count = 0;
        float average = 0;
        
        for (int i = 0; i < spectrumBands; i++)
        {
            int sampleCount = (int)Mathf.Pow(2, i) * 2;
            
            if (i == spectrumBands - 1)
            {
                sampleCount += 2;
            }
            
            average = 0;
            for (int j = 0; j < sampleCount; j++)
            {
                if (count < spectrum.Length)
                {
                    average += spectrum[count] * (count + 1);
                    count++;
                }
            }
            
            average /= count;
            bandLevels[i] = average * amplitudeMultiplier;
        }
    }
    
    private void DetectBeat()
    {
        float currentEnergy = 0;
        for (int i = 0; i < 4; i++) // Focus on bass frequencies
        {
            currentEnergy += bandLevels[i];
        }
        
        averageEnergy = averageEnergy * 0.95f + currentEnergy * 0.05f;
        
        if (currentEnergy > averageEnergy * beatThreshold && beatTimer <= 0)
        {
            isBeat = true;
            beatEnergy = currentEnergy;
            beatTimer = 0.2f; // 200ms cooldown
            vfxGraph.SendEvent("OnBeat");
        }
        else
        {
            isBeat = false;
        }
        
        beatTimer -= Time.deltaTime;
        beatEnergy *= beatDecay;
    }
    
    private void UpdateVFX()
    {
        // Update spectrum texture
        for (int i = 0; i < spectrumBands; i++)
        {
            spectrumTexture.SetPixel(i, 0, new Color(bandLevels[i], 0, 0, 1));
        }
        spectrumTexture.Apply();
        
        // Smooth interpolation to RT
        Graphics.Blit(spectrumTexture, spectrumRT);
        
        // Update VFX parameters
        vfxGraph.SetFloat("TotalEnergy", GetTotalEnergy());
        vfxGraph.SetFloat("BassLevel", GetBassLevel());
        vfxGraph.SetFloat("MidLevel", GetMidLevel());
        vfxGraph.SetFloat("HighLevel", GetHighLevel());
        vfxGraph.SetBool("IsBeat", isBeat);
        vfxGraph.SetFloat("BeatEnergy", beatEnergy);
        
        // Send velocity data
        var velocityTexture = new Texture2D(spectrumBands, 1, TextureFormat.RFloat, false);
        for (int i = 0; i < spectrumBands; i++)
        {
            velocityTexture.SetPixel(i, 0, new Color(bandVelocities[i], 0, 0, 1));
        }
        velocityTexture.Apply();
        vfxGraph.SetTexture("VelocityTexture", velocityTexture);
    }
    
    private float GetTotalEnergy()
    {
        float total = 0;
        foreach (float level in bandLevels)
        {
            total += level;
        }
        return total / spectrumBands;
    }
    
    private float GetBassLevel()
    {
        float bass = 0;
        for (int i = 0; i < 4; i++)
        {
            bass += bandLevels[i];
        }
        return bass / 4f;
    }
    
    private float GetMidLevel()
    {
        float mid = 0;
        for (int i = 4; i < 16; i++)
        {
            mid += bandLevels[i];
        }
        return mid / 12f;
    }
    
    private float GetHighLevel()
    {
        float high = 0;
        for (int i = 16; i < spectrumBands; i++)
        {
            high += bandLevels[i];
        }
        return high / (spectrumBands - 16);
    }
    
    private void OnDisable()
    {
        if (Microphone.IsRecording(microphoneName))
        {
            Microphone.End(microphoneName);
        }
        
        if (useJobSystem)
        {
            nativeSamples.Dispose();
            nativeSpectrum.Dispose();
            nativeBands.Dispose();
        }
        
        if (spectrumRT != null)
        {
            spectrumRT.Release();
        }
    }
    
    // Burst-compiled job for FFT processing
    [BurstCompile]
    private struct FFTJob : IJob
    {
        [ReadOnly] public NativeArray<float> samples;
        public NativeArray<float> spectrum;
        public int fftSize;
        
        public void Execute()
        {
            // Simplified FFT calculation
            // In production, use Unity.Mathematics FFT or native implementation
            for (int i = 0; i < fftSize; i++)
            {
                spectrum[i] = Mathf.Abs(samples[i]) * (1 + i * 0.1f);
            }
        }
    }
    
    // Burst-compiled job for frequency band calculation
    [BurstCompile]
    private struct FrequencyBandJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> spectrum;
        [WriteOnly] public NativeArray<float> bands;
        public int spectrumSize;
        public int bandCount;
        public float amplitudeMultiplier;
        
        public void Execute(int bandIndex)
        {
            int startIndex = (int)math.pow(2, bandIndex);
            int endIndex = (int)math.pow(2, bandIndex + 1);
            
            float sum = 0;
            int count = 0;
            
            for (int i = startIndex; i < math.min(endIndex, spectrumSize); i++)
            {
                sum += spectrum[i];
                count++;
            }
            
            bands[bandIndex] = (sum / math.max(1, count)) * amplitudeMultiplier;
        }
    }
}