// WhisperIcosaController.cs - Voice-to-object integration (spec-009)
// "Put a cat here" → search Icosa/Sketchfab → import glTF → AR placement
// Supports swappable voice providers: LLMUnity, Gemini, Whisper, Custom

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace MetavidoVFX.Icosa
{
    /// <summary>
    /// Listens for voice commands via pluggable voice providers and triggers
    /// 3D model search + AR placement from Icosa/Sketchfab.
    /// Supports hot-swapping between LLMUnity, Gemini, Whisper, or custom providers.
    /// </summary>
    public class WhisperIcosaController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Dependencies")]
        [SerializeField] private UnifiedModelSearch _searchManager;
        [SerializeField] private ModelPlacer _placer;
        [SerializeField] private ARRaycastManager _raycastManager;

        [Header("Voice Provider")]
        [SerializeField] private VoiceProviderType _voiceProvider = VoiceProviderType.LLMUnity;
        [SerializeField] private bool _useVoiceProviderManager = true;

        [Header("Voice Settings")]
        [SerializeField] private float _minConfidence = 0.5f;
        [SerializeField] private float _commandCooldown = 2f;

        [Header("Search Settings")]
        [SerializeField] private int _maxSearchResults = 5;

        [Header("Placement")]
        [SerializeField] private float _defaultPlacementDistance = 1.5f;
        [SerializeField] private float _defaultModelScale = 0.1f;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip _searchingSound;
        [SerializeField] private AudioClip _foundSound;
        [SerializeField] private AudioClip _placedSound;
        [SerializeField] private AudioClip _errorSound;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = true;

        #endregion

        #region Events

        public event Action<string> OnCommandRecognized;
        public event Action<string[]> OnKeywordsExtracted;
        public event Action<UnifiedSearchResult[]> OnSearchResults;
        public event Action<string> OnSearchError;
        public event Action<GameObject> OnModelPlaced;
        public event Action<string> OnStatusChanged;

        #endregion

        #region Private State

        private AudioSource _audioSource;
        private float _lastCommandTime;
        private bool _isProcessing;

        // Command patterns
        private static readonly Regex _placePattern = new Regex(
            @"(?:put|place|add|show|create|spawn|summon)\s+(?:a\s+|an\s+)?(.+?)(?:\s+(?:here|there|on\s+the\s+\w+))?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex _searchPattern = new Regex(
            @"(?:find|search|look\s+for|get)\s+(?:a\s+|an\s+)?(.+?)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Stop words to filter from search queries
        private static readonly HashSet<string> _stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "here", "there", "this", "that", "on", "in", "at",
            "please", "can", "could", "would", "should", "i", "want", "need"
        };

        #endregion

        #region Properties

        public bool IsProcessing => _isProcessing;
        public UnifiedModelSearch SearchManager => _searchManager;
        public ModelPlacer Placer => _placer;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private async void Start()
        {
            // Auto-find dependencies if not assigned
            if (_searchManager == null)
                _searchManager = FindAnyObjectByType<UnifiedModelSearch>();

            if (_placer == null)
                _placer = FindAnyObjectByType<ModelPlacer>();

            if (_raycastManager == null)
                _raycastManager = FindAnyObjectByType<ARRaycastManager>();

            if (_searchManager == null)
            {
                Debug.LogError("[WhisperIcosaController] UnifiedModelSearch not found!");
            }

            // Initialize voice provider
            if (_useVoiceProviderManager)
            {
                await InitializeVoiceProviderAsync();
            }
        }

        #endregion

        #region Voice Provider Integration

        /// <summary>
        /// Initialize the voice provider system.
        /// </summary>
        private async Task InitializeVoiceProviderAsync()
        {
            var manager = VoiceProviderManager.Instance;
            bool success = await manager.InitializeAsync(_voiceProvider);

            if (success)
            {
                // Subscribe to command events
                manager.OnCommandProcessed += HandleVoiceCommand;

                if (_debugMode)
                    Debug.Log($"[WhisperIcosa] Voice provider initialized: {manager.ActiveProvider?.ProviderName}");
            }
            else
            {
                Debug.LogWarning("[WhisperIcosa] Failed to initialize voice provider - using fallback");
            }
        }

        /// <summary>
        /// Handle processed voice commands from the provider.
        /// </summary>
        private async void HandleVoiceCommand(VoiceCommandResult result)
        {
            if (!result.IsValid)
                return;

            if (result.Confidence < _minConfidence)
            {
                if (_debugMode)
                    Debug.Log($"[WhisperIcosa] Low confidence ({result.Confidence:F2}): {result.Text}");
                return;
            }

            // Cooldown check
            if (Time.time - _lastCommandTime < _commandCooldown)
            {
                if (_debugMode)
                    Debug.Log("[WhisperIcosa] Command cooldown active");
                return;
            }

            _lastCommandTime = Time.time;
            OnCommandRecognized?.Invoke(result.Text);

            // Handle command based on action type
            await HandleActionAsync(result);
        }

        /// <summary>
        /// Handle specific action types from voice commands.
        /// </summary>
        private async Task HandleActionAsync(VoiceCommandResult result)
        {
            switch (result.Action.ToLowerInvariant())
            {
                case "place":
                case "search":
                    if (!string.IsNullOrEmpty(result.Params.ObjectName))
                    {
                        OnKeywordsExtracted?.Invoke(new[] { result.Params.ObjectName });
                        await ProcessModelCommand(new[] { result.Params.ObjectName });
                    }
                    break;

                case "delete":
                    UpdateStatus("Delete command received");
                    // TODO: Implement delete logic
                    break;

                case "clear":
                    UpdateStatus("Clear command received");
                    // TODO: Implement clear logic
                    break;

                case "undo":
                    UpdateStatus("Undo command received");
                    // TODO: Implement undo logic
                    break;

                default:
                    // Fallback to keyword extraction
                    string[] keywords = ExtractKeywords(result.Text);
                    if (keywords.Length > 0)
                    {
                        OnKeywordsExtracted?.Invoke(keywords);
                        await ProcessModelCommand(keywords);
                    }
                    break;
            }
        }

        /// <summary>
        /// Start recording voice input using the active provider.
        /// </summary>
        public void StartVoiceRecording(MeteringCallback onMetering = null)
        {
            if (!_useVoiceProviderManager)
            {
                Debug.LogWarning("[WhisperIcosa] Voice provider not enabled");
                return;
            }

            VoiceProviderManager.Instance.StartRecording(onMetering);
            UpdateStatus("Listening...");
        }

        /// <summary>
        /// Stop recording and process the voice command.
        /// </summary>
        public async Task<VoiceCommandResult> StopVoiceRecordingAsync()
        {
            if (!_useVoiceProviderManager)
            {
                Debug.LogWarning("[WhisperIcosa] Voice provider not enabled");
                return default;
            }

            UpdateStatus("Processing...");
            return await VoiceProviderManager.Instance.StopAndProcessAsync();
        }

        /// <summary>
        /// Process a text command directly (bypass audio).
        /// Useful for testing or text-based UI input.
        /// </summary>
        public async Task ProcessTextCommandAsync(string text)
        {
            if (_useVoiceProviderManager && VoiceProviderManager.Instance.IsInitialized)
            {
                var result = await VoiceProviderManager.Instance.ProcessTextAsync(text);
                if (result.IsValid)
                {
                    await HandleActionAsync(result);
                }
            }
            else
            {
                // Fallback to legacy processing
                ProcessTranscription(text, 0.9f);
            }
        }

        /// <summary>
        /// Switch to a different voice provider at runtime.
        /// </summary>
        public async Task<bool> SwitchVoiceProviderAsync(VoiceProviderType providerType)
        {
            _voiceProvider = providerType;
            return await VoiceProviderManager.Instance.SwitchProvider(providerType);
        }

        private void OnDestroy()
        {
            if (_useVoiceProviderManager && VoiceProviderManager.Instance != null)
            {
                VoiceProviderManager.Instance.OnCommandProcessed -= HandleVoiceCommand;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Process a transcription result from Whisper.
        /// Call this from your Whisper integration when transcription completes.
        /// </summary>
        public async void ProcessTranscription(string text, float confidence)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (confidence < _minConfidence)
            {
                if (_debugMode)
                    Debug.Log($"[WhisperIcosa] Low confidence ({confidence:F2}): {text}");
                return;
            }

            // Cooldown check
            if (Time.time - _lastCommandTime < _commandCooldown)
            {
                if (_debugMode)
                    Debug.Log("[WhisperIcosa] Command cooldown active");
                return;
            }

            // Try to match command patterns
            string[] keywords = ExtractKeywords(text);
            if (keywords.Length == 0)
            {
                if (_debugMode)
                    Debug.Log($"[WhisperIcosa] No keywords extracted from: {text}");
                return;
            }

            _lastCommandTime = Time.time;
            OnCommandRecognized?.Invoke(text);
            OnKeywordsExtracted?.Invoke(keywords);

            // Process command
            await ProcessModelCommand(keywords);
        }

        /// <summary>
        /// Directly search and place a model by keyword.
        /// Useful for programmatic or UI-triggered searches.
        /// </summary>
        public async Task SearchAndPlace(string query)
        {
            if (_isProcessing)
            {
                Debug.LogWarning("[WhisperIcosa] Already processing a request");
                return;
            }

            await ProcessModelCommand(new[] { query });
        }

        /// <summary>
        /// Cancel the current operation.
        /// </summary>
        public void CancelOperation()
        {
            _isProcessing = false;
            UpdateStatus("Cancelled");
        }

        #endregion

        #region Command Processing

        private string[] ExtractKeywords(string text)
        {
            // Try place pattern first
            var placeMatch = _placePattern.Match(text);
            if (placeMatch.Success)
            {
                return FilterKeywords(placeMatch.Groups[1].Value);
            }

            // Try search pattern
            var searchMatch = _searchPattern.Match(text);
            if (searchMatch.Success)
            {
                return FilterKeywords(searchMatch.Groups[1].Value);
            }

            // Fallback: extract nouns/adjectives (simple approach)
            return FilterKeywords(text);
        }

        private string[] FilterKeywords(string text)
        {
            var words = text.Split(new[] { ' ', ',', '.', '!', '?' },
                StringSplitOptions.RemoveEmptyEntries);

            var keywords = new List<string>();

            foreach (var word in words)
            {
                string clean = word.Trim().ToLowerInvariant();
                if (!_stopWords.Contains(clean) && clean.Length > 2)
                {
                    keywords.Add(clean);
                }
            }

            return keywords.ToArray();
        }

        private async Task ProcessModelCommand(string[] keywords)
        {
            if (keywords.Length == 0)
                return;

            _isProcessing = true;
            string query = string.Join(" ", keywords);

            try
            {
                // Step 1: Search
                UpdateStatus($"Searching for '{query}'...");
                PlaySound(_searchingSound);

                var results = await _searchManager.SearchAsync(query);

                if (results == null || results.Count == 0)
                {
                    UpdateStatus($"No results for '{query}'");
                    PlaySound(_errorSound);
                    OnSearchError?.Invoke($"No models found for '{query}'");
                    return;
                }

                OnSearchResults?.Invoke(results.ToArray());
                PlaySound(_foundSound);

                if (_debugMode)
                    Debug.Log($"[WhisperIcosa] Found {results.Count} results for '{query}'");

                // Step 2: Auto-select first result
                var selected = results[0];
                UpdateStatus($"Loading '{selected.Name}'...");

                // Step 3: Place in AR
                if (_placer != null)
                {
                    var model = await _placer.PlaceModelAsync(selected);

                    if (model != null)
                    {
                        UpdateStatus($"Placed '{selected.Name}'");
                        PlaySound(_placedSound);
                        OnModelPlaced?.Invoke(model);
                    }
                    else
                    {
                        UpdateStatus("Failed to place model");
                        PlaySound(_errorSound);
                    }
                }
                else
                {
                    // No placer, just report found
                    UpdateStatus($"Found: {selected.Name}");
                }
            }
            catch (Exception e)
            {
                UpdateStatus($"Error: {e.Message}");
                PlaySound(_errorSound);
                OnSearchError?.Invoke(e.Message);

                if (_debugMode)
                    Debug.LogError($"[WhisperIcosa] Error: {e}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        #endregion

        #region Helpers

        private void UpdateStatus(string status)
        {
            if (_debugMode)
                Debug.Log($"[WhisperIcosa] {status}");

            OnStatusChanged?.Invoke(status);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Editor Integration

#if UNITY_EDITOR
        [ContextMenu("Test: Put a cat here")]
        private void TestCatCommand()
        {
            ProcessTranscription("Put a cat here", 0.9f);
        }

        [ContextMenu("Test: Find a robot")]
        private void TestRobotCommand()
        {
            ProcessTranscription("Find a robot", 0.9f);
        }

        [ContextMenu("Test: Show me a dragon")]
        private void TestDragonCommand()
        {
            ProcessTranscription("Show me a dragon", 0.9f);
        }
#endif

        #endregion
    }
}
