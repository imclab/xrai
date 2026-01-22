// VoiceInputProviderBase.cs - Base class for voice input providers (spec-009)
// Provides common functionality for voice providers

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MetavidoVFX.Icosa
{
    /// <summary>
    /// Base class for voice input providers.
    /// Handles common recording logic and audio management.
    /// </summary>
    public abstract class VoiceInputProviderBase : IVoiceInputProvider
    {
        #region Protected Fields

        protected AudioClip _recordingClip;
        protected bool _isRecording;
        protected bool _isReady;
        protected CancellationTokenSource _cancellationToken;
        protected MeteringCallback _meteringCallback;

        // Recording settings
        protected int _sampleRate = 16000;
        protected int _maxRecordingSeconds = 30;
        protected string _microphoneDevice;

        #endregion

        #region IVoiceInputProvider Implementation

        public abstract string ProviderName { get; }
        public bool IsRecording => _isRecording;
        public bool IsReady => _isReady;

        public virtual async Task<bool> InitializeAsync()
        {
            // Check microphone availability
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError($"[{ProviderName}] No microphone found");
                return false;
            }

            _microphoneDevice = Microphone.devices[0];
            _isReady = true;

            Debug.Log($"[{ProviderName}] Initialized with microphone: {_microphoneDevice}");
            return await Task.FromResult(true);
        }

        public virtual void StartRecording(MeteringCallback onMetering = null)
        {
            if (_isRecording)
            {
                Debug.LogWarning($"[{ProviderName}] Already recording");
                return;
            }

            _meteringCallback = onMetering;
            _cancellationToken = new CancellationTokenSource();

            // Start Unity microphone recording
            _recordingClip = Microphone.Start(_microphoneDevice, false, _maxRecordingSeconds, _sampleRate);
            _isRecording = true;

            Debug.Log($"[{ProviderName}] Recording started");

            // Start metering coroutine if callback provided
            if (_meteringCallback != null)
            {
                StartMeteringAsync();
            }
        }

        public virtual async Task<string> StopRecordingAsync()
        {
            if (!_isRecording)
            {
                Debug.LogWarning($"[{ProviderName}] Not recording");
                return null;
            }

            _isRecording = false;
            _cancellationToken?.Cancel();

            // Get recording position
            int position = Microphone.GetPosition(_microphoneDevice);
            Microphone.End(_microphoneDevice);

            if (position == 0 || _recordingClip == null)
            {
                Debug.LogWarning($"[{ProviderName}] No audio recorded");
                return null;
            }

            // Save to temporary WAV file
            string audioPath = await SaveAudioToFileAsync(_recordingClip, position);

            Debug.Log($"[{ProviderName}] Recording stopped, saved to: {audioPath}");
            return audioPath;
        }

        public abstract Task<VoiceCommandResult> ProcessCommandAsync(string audioUri, string context = null);

        public virtual async Task<VoiceCommandResult> RecordAndProcessAsync(string context = null)
        {
            // This is a blocking record - wait for external stop signal
            // In practice, you'd use a push-to-talk or silence detection
            StartRecording();

            // Wait for recording to be stopped externally
            while (_isRecording && !_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100);
            }

            string audioUri = await StopRecordingAsync();

            if (string.IsNullOrEmpty(audioUri))
            {
                return default;
            }

            return await ProcessCommandAsync(audioUri, context);
        }

        public virtual void Cancel()
        {
            _cancellationToken?.Cancel();

            if (_isRecording)
            {
                Microphone.End(_microphoneDevice);
                _isRecording = false;
            }

            Debug.Log($"[{ProviderName}] Operation cancelled");
        }

        public virtual void Dispose()
        {
            Cancel();

            if (_recordingClip != null)
            {
                UnityEngine.Object.Destroy(_recordingClip);
                _recordingClip = null;
            }
        }

        #endregion

        #region Protected Methods

        protected virtual async void StartMeteringAsync()
        {
            while (_isRecording && !_cancellationToken.IsCancellationRequested)
            {
                float level = GetCurrentAudioLevel();
                _meteringCallback?.Invoke(level);
                await Task.Delay(50); // 20Hz update
            }
        }

        protected virtual float GetCurrentAudioLevel()
        {
            if (_recordingClip == null || !_isRecording)
                return 0f;

            int position = Microphone.GetPosition(_microphoneDevice);
            if (position == 0)
                return 0f;

            // Sample recent audio for level detection
            int sampleWindow = Mathf.Min(256, position);
            float[] samples = new float[sampleWindow];

            int startSample = Mathf.Max(0, position - sampleWindow);
            _recordingClip.GetData(samples, startSample);

            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += Mathf.Abs(sample);
            }

            return sum / sampleWindow;
        }

        protected virtual async Task<string> SaveAudioToFileAsync(AudioClip clip, int samples)
        {
            // Create temporary file path
            string fileName = $"voice_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
            string filePath = Path.Combine(Application.temporaryCachePath, fileName);

            // Convert to WAV
            await Task.Run(() =>
            {
                float[] data = new float[samples * clip.channels];
                clip.GetData(data, 0);
                SaveWavFile(filePath, data, clip.channels, clip.frequency);
            });

            return filePath;
        }

        protected virtual void SaveWavFile(string filePath, float[] samples, int channels, int sampleRate)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                int byteRate = sampleRate * channels * 2; // 16-bit
                int dataSize = samples.Length * 2;

                // RIFF header
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + dataSize);
                writer.Write("WAVE".ToCharArray());

                // fmt chunk
                writer.Write("fmt ".ToCharArray());
                writer.Write(16); // chunk size
                writer.Write((short)1); // PCM format
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write((short)(channels * 2)); // block align
                writer.Write((short)16); // bits per sample

                // data chunk
                writer.Write("data".ToCharArray());
                writer.Write(dataSize);

                // Write samples as 16-bit PCM
                foreach (float sample in samples)
                {
                    short value = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                    writer.Write(value);
                }
            }
        }

        #endregion
    }
}
