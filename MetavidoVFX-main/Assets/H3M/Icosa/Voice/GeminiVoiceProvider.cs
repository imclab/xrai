// GeminiVoiceProvider.cs - Gemini AI voice command provider (spec-009)
// Mirrors portals_main voice.ts implementation using Google Generative AI
// Supports multimodal audio input for command processing

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MetavidoVFX.Icosa
{
    /// <summary>
    /// Voice command provider using Google Gemini AI.
    /// Mirrors portals_main/src/services/voice.ts implementation.
    /// Requires GEMINI_API_KEY environment variable or API key field.
    /// </summary>
    public class GeminiVoiceProvider : VoiceInputProviderBase
    {
        #region Constants

        private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent";

        private readonly string _systemInstruction = @"You are a voice command parser for an AR/VR application.
The user will speak commands to interact with 3D objects in augmented reality.

Parse the user's voice command and return a JSON object with:
- action: The action type (place, search, delete, clear, undo, help, move, scale, rotate)
- params: Object with relevant parameters:
  - objectName: The object being referenced
  - color: Color specification if any
  - size: Size (small, medium, large, or number)
  - position: Where to place (here, there, left, right, forward)
  - direction: Direction for move/rotate
  - amount: Amount for scale/move/rotate
- text: The original transcribed text
- confidence: Your confidence 0-1

Examples:
'put a red car here' → {""action"":""place"",""params"":{""objectName"":""car"",""color"":""red"",""position"":""here""},""text"":""put a red car here"",""confidence"":0.95}
'find me a robot' → {""action"":""search"",""params"":{""objectName"":""robot""},""text"":""find me a robot"",""confidence"":0.9}
'delete that' → {""action"":""delete"",""params"":{""objectName"":""last""},""text"":""delete that"",""confidence"":0.85}
'make it bigger' → {""action"":""scale"",""params"":{""amount"":1.5},""text"":""make it bigger"",""confidence"":0.8}

Only output valid JSON. No markdown, no explanation.";

        #endregion

        #region Fields

        private string _apiKey;
        private HttpClient _httpClient;

        #endregion

        #region Properties

        public override string ProviderName => "Gemini";

        /// <summary>Set API key programmatically (alternative to env var).</summary>
        public string ApiKey
        {
            get => _apiKey;
            set => _apiKey = value;
        }

        #endregion

        #region Initialization

        public override async Task<bool> InitializeAsync()
        {
            bool baseInit = await base.InitializeAsync();
            if (!baseInit) return false;

            // Get API key from environment or field
            _apiKey = string.IsNullOrEmpty(_apiKey)
                ? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                : _apiKey;

            if (string.IsNullOrEmpty(_apiKey))
            {
                Debug.LogError($"[{ProviderName}] No API key found. Set GEMINI_API_KEY environment variable or ApiKey property.");
                return false;
            }

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            Debug.Log($"[{ProviderName}] Initialized with API key");
            return true;
        }

        #endregion

        #region Command Processing

        public override async Task<VoiceCommandResult> ProcessCommandAsync(string audioUri, string context = null)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                Debug.LogError($"[{ProviderName}] API key not set");
                return default;
            }

            try
            {
                // Read audio file and convert to base64
                byte[] audioBytes;
                string mimeType = "audio/wav";

                if (File.Exists(audioUri))
                {
                    audioBytes = await File.ReadAllBytesAsync(audioUri);

                    // Detect MIME type from extension
                    string ext = Path.GetExtension(audioUri).ToLowerInvariant();
                    mimeType = ext switch
                    {
                        ".mp3" => "audio/mp3",
                        ".m4a" => "audio/mp4",
                        ".ogg" => "audio/ogg",
                        ".webm" => "audio/webm",
                        _ => "audio/wav"
                    };
                }
                else
                {
                    // Treat as text input (for testing)
                    return await ProcessTextCommandAsync(audioUri, context);
                }

                string audioBase64 = Convert.ToBase64String(audioBytes);

                // Build Gemini API request
                string requestBody = BuildRequestBody(audioBase64, mimeType, context);

                // Send request
                string url = $"{GEMINI_API_URL}?key={_apiKey}";
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[{ProviderName}] API error: {response.StatusCode} - {responseBody}");
                    return default;
                }

                // Parse response
                return ParseGeminiResponse(responseBody);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{ProviderName}] Error: {e.Message}");
                return default;
            }
        }

        /// <summary>
        /// Process a text command directly (bypass audio).
        /// Useful for testing or text-based input.
        /// </summary>
        public async Task<VoiceCommandResult> ProcessTextCommandAsync(string text, string context = null)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                Debug.LogError($"[{ProviderName}] API key not set");
                return default;
            }

            try
            {
                string requestBody = BuildTextRequestBody(text, context);

                string url = $"{GEMINI_API_URL}?key={_apiKey}";
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[{ProviderName}] API error: {response.StatusCode}");
                    return default;
                }

                return ParseGeminiResponse(responseBody);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{ProviderName}] Error: {e.Message}");
                return default;
            }
        }

        #endregion

        #region Request Building

        private string BuildRequestBody(string audioBase64, string mimeType, string context)
        {
            string userPrompt = string.IsNullOrEmpty(context)
                ? "Parse this voice command:"
                : $"Context: {context}\nParse this voice command:";

            // Gemini API format with audio
            return $@"{{
                ""system_instruction"": {{
                    ""parts"": [{{ ""text"": {JsonEscape(_systemInstruction)} }}]
                }},
                ""contents"": [{{
                    ""parts"": [
                        {{ ""text"": {JsonEscape(userPrompt)} }},
                        {{
                            ""inline_data"": {{
                                ""mime_type"": ""{mimeType}"",
                                ""data"": ""{audioBase64}""
                            }}
                        }}
                    ]
                }}],
                ""generationConfig"": {{
                    ""response_mime_type"": ""application/json"",
                    ""temperature"": 0.1,
                    ""maxOutputTokens"": 256
                }}
            }}";
        }

        private string BuildTextRequestBody(string text, string context)
        {
            string userPrompt = string.IsNullOrEmpty(context)
                ? $"Parse this voice command: \"{text}\""
                : $"Context: {context}\nParse this voice command: \"{text}\"";

            return $@"{{
                ""system_instruction"": {{
                    ""parts"": [{{ ""text"": {JsonEscape(_systemInstruction)} }}]
                }},
                ""contents"": [{{
                    ""parts"": [{{ ""text"": {JsonEscape(userPrompt)} }}]
                }}],
                ""generationConfig"": {{
                    ""response_mime_type"": ""application/json"",
                    ""temperature"": 0.1,
                    ""maxOutputTokens"": 256
                }}
            }}";
        }

        private string JsonEscape(string text)
        {
            return "\"" + text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t") + "\"";
        }

        #endregion

        #region Response Parsing

        private VoiceCommandResult ParseGeminiResponse(string responseBody)
        {
            try
            {
                // Extract text content from Gemini response
                // Response format: { "candidates": [{ "content": { "parts": [{ "text": "..." }] } }] }

                int textStart = responseBody.IndexOf("\"text\":", StringComparison.Ordinal);
                if (textStart < 0)
                {
                    Debug.LogWarning($"[{ProviderName}] No text in response");
                    return default;
                }

                int colonPos = responseBody.IndexOf(':', textStart);
                int quoteStart = responseBody.IndexOf('"', colonPos + 1);
                int quoteEnd = FindClosingQuote(responseBody, quoteStart + 1);

                if (quoteStart < 0 || quoteEnd < 0)
                {
                    Debug.LogWarning($"[{ProviderName}] Could not parse text content");
                    return default;
                }

                string jsonContent = responseBody.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);

                // Unescape the JSON string
                jsonContent = jsonContent
                    .Replace("\\n", "\n")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");

                // Parse the inner JSON
                return ParseCommandJson(jsonContent);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{ProviderName}] Parse error: {e.Message}");
                return default;
            }
        }

        private int FindClosingQuote(string text, int startIndex)
        {
            for (int i = startIndex; i < text.Length; i++)
            {
                if (text[i] == '"' && text[i - 1] != '\\')
                    return i;
            }
            return -1;
        }

        private VoiceCommandResult ParseCommandJson(string json)
        {
            var result = new VoiceCommandResult { Confidence = 0.8f };

            // Extract action
            ExtractStringField(json, "action", out string action);
            result.Action = action ?? "";

            // Extract text
            ExtractStringField(json, "text", out string text);
            result.Text = text ?? "";

            // Extract confidence
            if (ExtractFloatField(json, "confidence", out float confidence))
            {
                result.Confidence = confidence;
            }

            // Extract params
            ExtractStringField(json, "objectName", out string objectName);
            result.Params.ObjectName = objectName ?? "";

            ExtractStringField(json, "color", out string color);
            result.Params.Color = color ?? "";

            ExtractStringField(json, "size", out string size);
            result.Params.Size = size ?? "";

            ExtractStringField(json, "position", out string position);
            result.Params.Position = position ?? "";

            return result;
        }

        private bool ExtractStringField(string json, string fieldName, out string value)
        {
            value = null;
            int fieldStart = json.IndexOf($"\"{fieldName}\"", StringComparison.Ordinal);
            if (fieldStart < 0) return false;

            int colonPos = json.IndexOf(':', fieldStart);
            if (colonPos < 0) return false;

            int quoteStart = json.IndexOf('"', colonPos + 1);
            if (quoteStart < 0) return false;

            int quoteEnd = FindClosingQuote(json, quoteStart + 1);
            if (quoteEnd < 0) return false;

            value = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
            return true;
        }

        private bool ExtractFloatField(string json, string fieldName, out float value)
        {
            value = 0f;
            int fieldStart = json.IndexOf($"\"{fieldName}\"", StringComparison.Ordinal);
            if (fieldStart < 0) return false;

            int colonPos = json.IndexOf(':', fieldStart);
            if (colonPos < 0) return false;

            int endPos = json.IndexOfAny(new[] { ',', '}', ' ', '\n' }, colonPos + 1);
            if (endPos < 0) endPos = json.Length;

            string numStr = json.Substring(colonPos + 1, endPos - colonPos - 1).Trim();
            return float.TryParse(numStr, out value);
        }

        #endregion

        #region Cleanup

        public override void Dispose()
        {
            base.Dispose();
            _httpClient?.Dispose();
            _httpClient = null;
        }

        #endregion
    }
}
