using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Metavido.Encoder;

namespace MetavidoVFX.Recording
{
    /// <summary>
    /// Controls Metavido hologram recording using keijiro's Avfi package for iOS video encoding.
    /// Pattern follows keijiro's typical usage in Bibcam/MetavidoVFX projects.
    ///
    /// References:
    /// - https://github.com/keijiro/Avfi
    /// - https://github.com/keijiro/MetavidoVFX
    /// </summary>
    public class RecordingController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Dependencies")]
        [SerializeField] private FrameEncoder _frameEncoder;
        [SerializeField] private XRDataProvider _xrDataProvider;

        [Header("Recording Settings")]
        [SerializeField] private int _frameRate = 30;
        [SerializeField] private float _maxDuration = 60f;
        [SerializeField] private bool _saveToGallery = true;

        [Header("Events")]
        public UnityEvent<float> OnRecordingProgress;
        public UnityEvent<string> OnRecordingSaved;
        public UnityEvent<string> OnRecordingError;
        public UnityEvent OnRecordingStarted;
        public UnityEvent OnRecordingStopped;

        #endregion

        #region Public Properties

        public bool IsRecording { get; private set; }
        public float RecordingDuration { get; private set; }
        public string LastSavedPath { get; private set; }

        #endregion

        #region Private Fields

        private RenderTexture _buffer;
        private uint _frameCount;
        private float _recordingStartTime;
        private string _currentRecordingPath;
        private bool _pendingStop;

        private const int Width = 1920;
        private const int Height = 1080;

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            if (_frameEncoder == null)
                _frameEncoder = FindObjectOfType<FrameEncoder>();
            if (_xrDataProvider == null)
                _xrDataProvider = FindObjectOfType<XRDataProvider>();
        }

        void Start()
        {
            _buffer = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32);
            _buffer.Create();

#if UNITY_IOS && !UNITY_EDITOR
            WarmUp();
#endif
        }

        void OnDestroy()
        {
            if (IsRecording)
                StopRecording();

            if (_buffer != null)
            {
                _buffer.Release();
                Destroy(_buffer);
            }
        }

        void Update()
        {
            if (!IsRecording) return;

            RecordingDuration = Time.time - _recordingStartTime;
            OnRecordingProgress?.Invoke(RecordingDuration);

            if (RecordingDuration >= _maxDuration)
            {
                Debug.Log($"[RecordingController] Max duration ({_maxDuration}s)");
                StopRecording();
            }
        }

        void LateUpdate()
        {
            if (!IsRecording || _pendingStop) return;
            CaptureFrame();
        }

        #endregion

        #region Public API

        public void StartRecording()
        {
            if (IsRecording)
            {
                Debug.LogWarning("[RecordingController] Already recording");
                return;
            }

            if (_frameEncoder == null)
            {
                OnRecordingError?.Invoke("FrameEncoder not found");
                return;
            }

            _currentRecordingPath = GetRecordingPath();

#if AVFI_AVAILABLE && UNITY_IOS && !UNITY_EDITOR
            Avfi.ScreenRecorder.StartRecording(_currentRecordingPath, Width, Height);
#else
            Debug.Log($"[RecordingController] (Editor/No Avfi) Recording to: {_currentRecordingPath}");
#endif

            IsRecording = true;
            _recordingStartTime = Time.time;
            RecordingDuration = 0f;
            _frameCount = 0;
            _pendingStop = false;

            OnRecordingStarted?.Invoke();
            Debug.Log($"[RecordingController] Recording started");
        }

        public void StopRecording()
        {
            if (!IsRecording) return;

            _pendingStop = true;
            StartCoroutine(FinalizeRecording());
        }

        public void ToggleRecording()
        {
            if (IsRecording) StopRecording();
            else StartRecording();
        }

        #endregion

        #region Private Methods

        private void WarmUp()
        {
#if AVFI_AVAILABLE && UNITY_IOS && !UNITY_EDITOR
            var warmupPath = GetRecordingPath();
            Avfi.ScreenRecorder.StartRecording(warmupPath, Width, Height);
            Avfi.ScreenRecorder.EndRecording(false);
            Debug.Log("[RecordingController] Avfi warmed up");
#endif
        }

        private string GetRecordingPath()
        {
            string dir = Application.platform == RuntimePlatform.IPhonePlayer
                ? Application.temporaryCachePath
                : Application.temporaryCachePath;
            string fileName = $"Hologram_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
            return Path.Combine(dir, fileName);
        }

        private void CaptureFrame()
        {
            var encodedTexture = _frameEncoder?.EncodedTexture;
            if (encodedTexture == null) return;

            Graphics.Blit(encodedTexture, _buffer);
            AsyncGPUReadback.Request(_buffer, 0, OnFrameReadback);
        }

        private unsafe void OnFrameReadback(AsyncGPUReadbackRequest request)
        {
            if (!IsRecording || _pendingStop) return;
            if (request.hasError) return;

            double frameTime = _frameCount * (1.0 / _frameRate);

#if AVFI_AVAILABLE && UNITY_IOS && !UNITY_EDITOR
            using var pixelData = request.GetData<byte>(0);
            var pixelPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(pixelData);
            Avfi.ScreenRecorder.AppendFrame(pixelPtr, (uint)pixelData.Length, frameTime);
#endif

            _frameCount++;
        }

        private IEnumerator FinalizeRecording()
        {
            IsRecording = false;
            OnRecordingStopped?.Invoke();

            AsyncGPUReadback.WaitAllRequests();

#if AVFI_AVAILABLE && UNITY_IOS && !UNITY_EDITOR
            Avfi.ScreenRecorder.EndRecording(_saveToGallery);
#endif

            Debug.Log($"[RecordingController] Stopped: {_frameCount} frames, {RecordingDuration:F1}s");

            if (_saveToGallery)
            {
                LastSavedPath = "Camera Roll";
                OnRecordingSaved?.Invoke("Saved to Camera Roll");
            }
            else
            {
                LastSavedPath = _currentRecordingPath;
                OnRecordingSaved?.Invoke(LastSavedPath);
            }

            yield return null;
        }

        #endregion
    }
}
