// LLMVoiceProvider.cs - LLM-based voice command provider (spec-009)
// Uses LLMUnity for local LLM inference to parse voice commands
// Can also be adapted for Microsoft LLMR or other LLM backends

using System;
using System.Threading.Tasks;
using UnityEngine;
#if LLMUNITY_AVAILABLE
using LLMUnity;
#endif

namespace MetavidoVFX.Icosa
{
    /// <summary>
    /// Voice command provider using local LLM inference.
    /// Uses LLMUnity for on-device processing (no internet required).
    /// Compatible with Microsoft LLMR architecture patterns.
    /// </summary>
    public class LLMVoiceProvider : VoiceInputProviderBase
    {
        #region Fields

#if LLMUNITY_AVAILABLE
        private LLMAgent _llmAgent;
#endif

        private readonly string _systemPrompt = @"You are a voice command parser for an AR application.
Extract structured commands from user speech. Always respond with valid JSON.

Available actions:
- place: Create/place a 3D object (params: objectName, color, size, position)
- search: Search for 3D models (params: objectName)
- delete: Remove an object (params: objectName or 'last')
- clear: Clear all objects
- undo: Undo last action
- help: Show help

Example input: ""Put a red cat over there""
Example output: {""action"":""place"",""params"":{""objectName"":""cat"",""color"":""red"",""position"":""there""},""text"":""Put a red cat over there"",""confidence"":0.9}

If you cannot understand the command, respond with:
{""action"":"""",""params"":{},""text"":""<original text>"",""confidence"":0.0}

Only output JSON, nothing else.";

        private bool _useFallbackParsing = true;

        #endregion

        #region Properties

        public override string ProviderName => "LLMUnity";

#if LLMUNITY_AVAILABLE
        /// <summary>External LLMAgent reference (optional - will create if not set).</summary>
        public LLMAgent LLMAgent
        {
            get => _llmAgent;
            set => _llmAgent = value;
        }
#endif

        /// <summary>Use regex fallback if LLM unavailable.</summary>
        public bool UseFallbackParsing
        {
            get => _useFallbackParsing;
            set => _useFallbackParsing = value;
        }

        #endregion

        #region Initialization

        public override async Task<bool> InitializeAsync()
        {
            bool baseInit = await base.InitializeAsync();
            if (!baseInit) return false;

#if LLMUNITY_AVAILABLE
            // Find or create LLMAgent
            if (_llmAgent == null)
            {
                _llmAgent = UnityEngine.Object.FindAnyObjectByType<LLMAgent>();
            }

            if (_llmAgent != null)
            {
                _llmAgent.systemPrompt = _systemPrompt;
                Debug.Log($"[{ProviderName}] Connected to LLMAgent");
            }
            else
            {
                Debug.LogWarning($"[{ProviderName}] No LLMAgent found - using fallback parsing");
            }
#else
            Debug.LogWarning($"[{ProviderName}] LLMUnity not available - using fallback parsing");
#endif

            return true;
        }

        #endregion

        #region Command Processing

        public override async Task<VoiceCommandResult> ProcessCommandAsync(string audioUri, string context = null)
        {
            // First, we need to transcribe the audio to text
            // This would typically use Whisper or another STT service
            // For now, assume audioUri is actually the transcribed text
            // In production, integrate with a speech-to-text service

            string transcription = await TranscribeAudioAsync(audioUri);

            if (string.IsNullOrEmpty(transcription))
            {
                Debug.LogWarning($"[{ProviderName}] No transcription from audio");
                return default;
            }

            return await ParseCommandAsync(transcription, context);
        }

        /// <summary>
        /// Parse a text command using LLM or fallback regex.
        /// Can be called directly with text input (bypassing audio).
        /// </summary>
        public async Task<VoiceCommandResult> ParseCommandAsync(string text, string context = null)
        {
#if LLMUNITY_AVAILABLE
            if (_llmAgent != null)
            {
                return await ParseWithLLMAsync(text, context);
            }
#endif

            if (_useFallbackParsing)
            {
                return ParseWithRegex(text);
            }

            return new VoiceCommandResult
            {
                Action = "",
                Text = text,
                Confidence = 0f
            };
        }

#if LLMUNITY_AVAILABLE
        private async Task<VoiceCommandResult> ParseWithLLMAsync(string text, string context)
        {
            try
            {
                string prompt = text;
                if (!string.IsNullOrEmpty(context))
                {
                    prompt = $"Context: {context}\nUser said: {text}";
                }

                string response = await _llmAgent.Chat(prompt, addToHistory: false);

                if (string.IsNullOrEmpty(response))
                {
                    Debug.LogWarning($"[{ProviderName}] Empty LLM response");
                    return ParseWithRegex(text);
                }

                // Parse JSON response
                return ParseLLMResponse(response, text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{ProviderName}] LLM error: {e.Message}");
                return ParseWithRegex(text);
            }
        }
#endif

        private VoiceCommandResult ParseLLMResponse(string json, string originalText)
        {
            try
            {
                // Simple JSON parsing (JsonUtility doesn't handle nested objects well)
                // Extract action and params from response
                var result = new VoiceCommandResult
                {
                    Text = originalText,
                    Confidence = 0.8f
                };

                // Extract action
                int actionStart = json.IndexOf("\"action\"", StringComparison.Ordinal);
                if (actionStart >= 0)
                {
                    int colonPos = json.IndexOf(':', actionStart);
                    int quoteStart = json.IndexOf('"', colonPos + 1);
                    int quoteEnd = json.IndexOf('"', quoteStart + 1);
                    if (quoteStart >= 0 && quoteEnd > quoteStart)
                    {
                        result.Action = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                    }
                }

                // Extract params.objectName
                int objectNameStart = json.IndexOf("\"objectName\"", StringComparison.Ordinal);
                if (objectNameStart >= 0)
                {
                    int colonPos = json.IndexOf(':', objectNameStart);
                    int quoteStart = json.IndexOf('"', colonPos + 1);
                    int quoteEnd = json.IndexOf('"', quoteStart + 1);
                    if (quoteStart >= 0 && quoteEnd > quoteStart)
                    {
                        result.Params.ObjectName = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                    }
                }

                // Extract params.color
                int colorStart = json.IndexOf("\"color\"", StringComparison.Ordinal);
                if (colorStart >= 0)
                {
                    int colonPos = json.IndexOf(':', colorStart);
                    int quoteStart = json.IndexOf('"', colonPos + 1);
                    int quoteEnd = json.IndexOf('"', quoteStart + 1);
                    if (quoteStart >= 0 && quoteEnd > quoteStart)
                    {
                        result.Params.Color = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                    }
                }

                // Extract confidence
                int confidenceStart = json.IndexOf("\"confidence\"", StringComparison.Ordinal);
                if (confidenceStart >= 0)
                {
                    int colonPos = json.IndexOf(':', confidenceStart);
                    int endPos = json.IndexOfAny(new[] { ',', '}' }, colonPos + 1);
                    if (colonPos >= 0 && endPos > colonPos)
                    {
                        string confStr = json.Substring(colonPos + 1, endPos - colonPos - 1).Trim();
                        if (float.TryParse(confStr, out float conf))
                        {
                            result.Confidence = conf;
                        }
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{ProviderName}] JSON parse error: {e.Message}");
                return ParseWithRegex(originalText);
            }
        }

        private VoiceCommandResult ParseWithRegex(string text)
        {
            var result = new VoiceCommandResult
            {
                Text = text,
                Confidence = 0.6f
            };

            string lower = text.ToLowerInvariant();

            // Detect action type
            if (lower.Contains("put") || lower.Contains("place") || lower.Contains("add") ||
                lower.Contains("show") || lower.Contains("create") || lower.Contains("spawn"))
            {
                result.Action = "place";
                result.Params.ObjectName = ExtractObjectName(lower);
                result.Params.Color = ExtractColor(lower);
            }
            else if (lower.Contains("find") || lower.Contains("search") || lower.Contains("look for"))
            {
                result.Action = "search";
                result.Params.ObjectName = ExtractObjectName(lower);
            }
            else if (lower.Contains("delete") || lower.Contains("remove"))
            {
                result.Action = "delete";
                result.Params.ObjectName = lower.Contains("last") ? "last" : ExtractObjectName(lower);
            }
            else if (lower.Contains("clear") || lower.Contains("reset"))
            {
                result.Action = "clear";
            }
            else if (lower.Contains("undo"))
            {
                result.Action = "undo";
            }
            else if (lower.Contains("help"))
            {
                result.Action = "help";
            }
            else
            {
                // Default to search if we find an object name
                string obj = ExtractObjectName(lower);
                if (!string.IsNullOrEmpty(obj))
                {
                    result.Action = "search";
                    result.Params.ObjectName = obj;
                    result.Confidence = 0.4f;
                }
            }

            return result;
        }

        private string ExtractObjectName(string text)
        {
            // Remove common words to find the object
            string[] removeWords = {
                "put", "place", "add", "show", "create", "spawn", "a", "an", "the",
                "here", "there", "find", "search", "look", "for", "get", "me",
                "please", "can", "you", "i", "want", "need", "on", "in", "at"
            };

            string[] words = text.Split(new[] { ' ', ',', '.', '!', '?' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
                if (word.Length > 2 && !Array.Exists(removeWords, w => w == word))
                {
                    // Skip color words
                    if (!IsColorWord(word))
                    {
                        return word;
                    }
                }
            }

            return "";
        }

        private string ExtractColor(string text)
        {
            string[] colors = { "red", "blue", "green", "yellow", "orange", "purple",
                "pink", "white", "black", "brown", "gray", "grey", "cyan", "magenta" };

            foreach (string color in colors)
            {
                if (text.Contains(color))
                {
                    return color;
                }
            }

            return "";
        }

        private bool IsColorWord(string word)
        {
            string[] colors = { "red", "blue", "green", "yellow", "orange", "purple",
                "pink", "white", "black", "brown", "gray", "grey", "cyan", "magenta" };

            return Array.Exists(colors, c => c == word);
        }

        #endregion

        #region Audio Transcription

        /// <summary>
        /// Transcribe audio to text. Override this to integrate with Whisper or other STT.
        /// Default implementation treats audioUri as already-transcribed text.
        /// </summary>
        protected virtual Task<string> TranscribeAudioAsync(string audioUri)
        {
            // In a full implementation, this would:
            // 1. Load audio from audioUri
            // 2. Send to Whisper API or local Whisper model
            // 3. Return transcription

            // For now, check if it's a file path or text
            if (System.IO.File.Exists(audioUri))
            {
                Debug.LogWarning($"[{ProviderName}] Audio transcription not implemented - integrate Whisper");
                return Task.FromResult<string>(null);
            }

            // Assume it's already transcribed text
            return Task.FromResult(audioUri);
        }

        #endregion
    }
}
