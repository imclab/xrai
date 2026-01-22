// IVoiceInputProvider.cs - Abstract voice input interface (spec-009)
// Allows swapping between Whisper, Gemini, or other voice backends

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MetavidoVFX.Icosa
{
    /// <summary>
    /// Result from voice command processing.
    /// Mirrors portals_main VoiceService response structure.
    /// </summary>
    [Serializable]
    public struct VoiceCommandResult
    {
        /// <summary>The recognized action (e.g., "place", "search", "delete").</summary>
        public string Action;

        /// <summary>Optional parameters for the action.</summary>
        public VoiceCommandParams Params;

        /// <summary>The full transcription text.</summary>
        public string Text;

        /// <summary>Confidence score 0-1.</summary>
        public float Confidence;

        /// <summary>Whether the command was successfully recognized.</summary>
        public bool IsValid => !string.IsNullOrEmpty(Action) && Confidence > 0;
    }

    /// <summary>
    /// Parameters extracted from voice command.
    /// </summary>
    [Serializable]
    public struct VoiceCommandParams
    {
        /// <summary>Object/model name to search for.</summary>
        public string ObjectName;

        /// <summary>Color specification if any.</summary>
        public string Color;

        /// <summary>Size specification (small, medium, large).</summary>
        public string Size;

        /// <summary>Position hint (here, there, left, right).</summary>
        public string Position;

        /// <summary>Additional context from the command.</summary>
        public string Context;
    }

    /// <summary>
    /// Callback for audio metering updates during recording.
    /// </summary>
    public delegate void MeteringCallback(float level);

    /// <summary>
    /// Abstract interface for voice input providers.
    /// Implement this to add new voice backends (Whisper, Gemini, Azure, etc.)
    /// </summary>
    public interface IVoiceInputProvider
    {
        /// <summary>Provider name for debugging/UI.</summary>
        string ProviderName { get; }

        /// <summary>Whether the provider is currently recording.</summary>
        bool IsRecording { get; }

        /// <summary>Whether the provider is initialized and ready.</summary>
        bool IsReady { get; }

        /// <summary>
        /// Initialize the voice provider with optional configuration.
        /// </summary>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Start recording audio.
        /// </summary>
        /// <param name="onMetering">Optional callback for audio level updates.</param>
        void StartRecording(MeteringCallback onMetering = null);

        /// <summary>
        /// Stop recording and get the audio data URI/path.
        /// </summary>
        /// <returns>Path or URI to the recorded audio, or null if failed.</returns>
        Task<string> StopRecordingAsync();

        /// <summary>
        /// Process recorded audio and extract command.
        /// </summary>
        /// <param name="audioUri">Path or URI to the audio file.</param>
        /// <param name="context">Optional context to help with recognition.</param>
        /// <returns>Parsed voice command result.</returns>
        Task<VoiceCommandResult> ProcessCommandAsync(string audioUri, string context = null);

        /// <summary>
        /// Combined record + process for convenience.
        /// Starts recording, waits for stop signal, then processes.
        /// </summary>
        /// <param name="context">Optional context to help with recognition.</param>
        /// <returns>Parsed voice command result.</returns>
        Task<VoiceCommandResult> RecordAndProcessAsync(string context = null);

        /// <summary>
        /// Cancel any ongoing recording or processing.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Cleanup resources.
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// Event args for voice recognition events.
    /// </summary>
    public class VoiceRecognitionEventArgs : EventArgs
    {
        public VoiceCommandResult Result { get; }
        public bool IsFinal { get; }

        public VoiceRecognitionEventArgs(VoiceCommandResult result, bool isFinal = true)
        {
            Result = result;
            IsFinal = isFinal;
        }
    }
}
