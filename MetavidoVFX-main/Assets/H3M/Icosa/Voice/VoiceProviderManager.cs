// VoiceProviderManager.cs - Manages voice input providers (spec-009)
// Singleton manager for swapping between Whisper, Gemini, LLM, etc.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MetavidoVFX.Icosa
{
    /// <summary>
    /// Available voice provider types.
    /// </summary>
    public enum VoiceProviderType
    {
        /// <summary>Use LLMUnity for local LLM inference.</summary>
        LLMUnity,
        /// <summary>Use Google Gemini AI (cloud).</summary>
        Gemini,
        /// <summary>Use OpenAI Whisper (cloud or local).</summary>
        Whisper,
        /// <summary>Custom provider.</summary>
        Custom
    }

    /// <summary>
    /// Manages voice input providers with hot-swapping capability.
    /// Provides unified access to voice command processing.
    /// </summary>
    public class VoiceProviderManager : MonoBehaviour
    {
        #region Singleton

        private static VoiceProviderManager _instance;
        public static VoiceProviderManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<VoiceProviderManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[VoiceProviderManager]");
                        _instance = go.AddComponent<VoiceProviderManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField] private VoiceProviderType _defaultProvider = VoiceProviderType.LLMUnity;
        [SerializeField] private bool _initializeOnStart = true;

        [Header("API Keys (Optional)")]
        [SerializeField] private string _geminiApiKey;
        [SerializeField] private string _openAiApiKey;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = true;

        #endregion

        #region Private Fields

        private readonly Dictionary<VoiceProviderType, IVoiceInputProvider> _providers = new();
        private IVoiceInputProvider _activeProvider;
        private bool _isInitialized;

        #endregion

        #region Events

        /// <summary>Fired when active provider changes.</summary>
        public event Action<VoiceProviderType, IVoiceInputProvider> OnProviderChanged;

        /// <summary>Fired when a command is processed.</summary>
        public event Action<VoiceCommandResult> OnCommandProcessed;

        #endregion

        #region Properties

        /// <summary>The currently active voice provider.</summary>
        public IVoiceInputProvider ActiveProvider => _activeProvider;

        /// <summary>Current provider type.</summary>
        public VoiceProviderType ActiveProviderType { get; private set; }

        /// <summary>Whether the manager is initialized.</summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>Whether a provider is currently recording.</summary>
        public bool IsRecording => _activeProvider?.IsRecording ?? false;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            if (_initializeOnStart)
            {
                await InitializeAsync(_defaultProvider);
            }
        }

        private void OnDestroy()
        {
            // Cleanup all providers
            foreach (var provider in _providers.Values)
            {
                provider.Dispose();
            }
            _providers.Clear();

            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the manager with the specified provider.
        /// </summary>
        public async Task<bool> InitializeAsync(VoiceProviderType providerType = VoiceProviderType.LLMUnity)
        {
            if (_isInitialized && ActiveProviderType == providerType)
            {
                return true;
            }

            // Create provider if not exists
            if (!_providers.TryGetValue(providerType, out var provider))
            {
                provider = CreateProvider(providerType);
                if (provider == null)
                {
                    Debug.LogError($"[VoiceProviderManager] Failed to create provider: {providerType}");
                    return false;
                }
                _providers[providerType] = provider;
            }

            // Initialize provider
            bool success = await provider.InitializeAsync();
            if (!success)
            {
                Debug.LogError($"[VoiceProviderManager] Failed to initialize provider: {providerType}");
                return false;
            }

            _activeProvider = provider;
            ActiveProviderType = providerType;
            _isInitialized = true;

            if (_debugMode)
            {
                Debug.Log($"[VoiceProviderManager] Initialized with provider: {provider.ProviderName}");
            }

            OnProviderChanged?.Invoke(providerType, provider);
            return true;
        }

        private IVoiceInputProvider CreateProvider(VoiceProviderType type)
        {
            return type switch
            {
                VoiceProviderType.LLMUnity => new LLMVoiceProvider(),
                VoiceProviderType.Gemini => CreateGeminiProvider(),
                // VoiceProviderType.Whisper => new WhisperVoiceProvider(),
                _ => null
            };
        }

        private GeminiVoiceProvider CreateGeminiProvider()
        {
            var provider = new GeminiVoiceProvider();
            if (!string.IsNullOrEmpty(_geminiApiKey))
            {
                provider.ApiKey = _geminiApiKey;
            }
            return provider;
        }

        #endregion

        #region Provider Switching

        /// <summary>
        /// Switch to a different voice provider.
        /// </summary>
        public async Task<bool> SwitchProvider(VoiceProviderType providerType)
        {
            if (ActiveProviderType == providerType && _activeProvider != null)
            {
                return true;
            }

            // Cancel any ongoing operation
            _activeProvider?.Cancel();

            // Initialize new provider
            return await InitializeAsync(providerType);
        }

        /// <summary>
        /// Register a custom voice provider.
        /// </summary>
        public void RegisterProvider(VoiceProviderType type, IVoiceInputProvider provider)
        {
            if (_providers.ContainsKey(type))
            {
                _providers[type].Dispose();
            }
            _providers[type] = provider;

            if (_debugMode)
            {
                Debug.Log($"[VoiceProviderManager] Registered custom provider: {provider.ProviderName}");
            }
        }

        #endregion

        #region Voice Commands

        /// <summary>
        /// Start recording audio.
        /// </summary>
        public void StartRecording(MeteringCallback onMetering = null)
        {
            if (_activeProvider == null)
            {
                Debug.LogError("[VoiceProviderManager] No active provider");
                return;
            }

            _activeProvider.StartRecording(onMetering);
        }

        /// <summary>
        /// Stop recording and process the command.
        /// </summary>
        public async Task<VoiceCommandResult> StopAndProcessAsync(string context = null)
        {
            if (_activeProvider == null)
            {
                Debug.LogError("[VoiceProviderManager] No active provider");
                return default;
            }

            string audioUri = await _activeProvider.StopRecordingAsync();
            if (string.IsNullOrEmpty(audioUri))
            {
                return default;
            }

            var result = await _activeProvider.ProcessCommandAsync(audioUri, context);

            if (result.IsValid)
            {
                OnCommandProcessed?.Invoke(result);
            }

            if (_debugMode)
            {
                Debug.Log($"[VoiceProviderManager] Command: {result.Action} - {result.Text} (confidence: {result.Confidence:F2})");
            }

            return result;
        }

        /// <summary>
        /// Process text directly (bypass audio recording).
        /// Useful for testing or text-based input.
        /// </summary>
        public async Task<VoiceCommandResult> ProcessTextAsync(string text, string context = null)
        {
            if (_activeProvider == null)
            {
                Debug.LogError("[VoiceProviderManager] No active provider");
                return default;
            }

            // Check if provider supports direct text processing
            if (_activeProvider is LLMVoiceProvider llmProvider)
            {
                var result = await llmProvider.ParseCommandAsync(text, context);
                if (result.IsValid)
                {
                    OnCommandProcessed?.Invoke(result);
                }
                return result;
            }
            else if (_activeProvider is GeminiVoiceProvider geminiProvider)
            {
                var result = await geminiProvider.ProcessTextCommandAsync(text, context);
                if (result.IsValid)
                {
                    OnCommandProcessed?.Invoke(result);
                }
                return result;
            }

            // Fallback: treat text as "audio" (provider should handle)
            return await _activeProvider.ProcessCommandAsync(text, context);
        }

        /// <summary>
        /// Cancel any ongoing operation.
        /// </summary>
        public void Cancel()
        {
            _activeProvider?.Cancel();
        }

        #endregion

        #region Utility

        /// <summary>
        /// Get list of available providers.
        /// </summary>
        public VoiceProviderType[] GetAvailableProviders()
        {
            var available = new List<VoiceProviderType>();

#if LLMUNITY_AVAILABLE
            available.Add(VoiceProviderType.LLMUnity);
#else
            // LLMUnity works with fallback parsing even without the package
            available.Add(VoiceProviderType.LLMUnity);
#endif

            if (!string.IsNullOrEmpty(_geminiApiKey) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GEMINI_API_KEY")))
            {
                available.Add(VoiceProviderType.Gemini);
            }

            return available.ToArray();
        }

        #endregion
    }
}
