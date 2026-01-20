// AudioHLSLGuide.cs - Editor utilities for VFX Graph audio integration

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace MetavidoVFX.VFXPipeline.Editor
{
    /// <summary>
    /// Editor utilities for integrating audio into VFX Graph.
    /// Three approaches supported:
    /// 1. Exposed Float Properties + VFXAudioDataBinder (recommended)
    /// 2. AudioDataTexture auto-binding (automatic, no HLSL needed)
    /// 3. Custom HLSL + ARGlobals.hlsl (advanced, most flexible)
    /// </summary>
    public static class AudioHLSLGuide
    {
        const string HLSLPath = "Assets/Shaders/ARGlobals.hlsl";

        [MenuItem("H3M/VFX Pipeline Master/Audio/Show Audio Integration Guide")]
        public static void ShowAudioGuide()
        {
            string guide = @"
╔══════════════════════════════════════════════════════════════════╗
║           VFX GRAPH AUDIO INTEGRATION GUIDE                      ║
╠══════════════════════════════════════════════════════════════════╣

There are THREE approaches for audio-reactive VFX:

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
▶ APPROACH 1: Exposed Properties + VFXAudioDataBinder (RECOMMENDED)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Best for: Most VFX, easy setup, no HLSL knowledge required
Performance: ~0.01ms per VFX (negligible)

STEP 1: Add exposed Float properties to your VFX Graph
  • Open VFX Graph Editor (double-click .vfx asset)
  • Open Blackboard panel (View > Blackboard)
  • Click '+' > Float
  • Add these properties (names must match exactly):
    - AudioVolume    (0-1 overall volume)
    - AudioBass      (0-1 low frequency)
    - AudioMid       (0-1 mid frequency)
    - AudioTreble    (0-1 high frequency)
    - BeatPulse      (0-1 decaying beat pulse)
    - BeatIntensity  (0-1 beat strength)
  • Check 'Exposed' checkbox for each

STEP 2: Add VFXAudioDataBinder to VFX GameObject
  • Select your VFX GameObject
  • Add Component > VFXAudioDataBinder
  • It will auto-bind to available properties

Alternative property names that also work:
  • AudioLevel = AudioVolume
  • AudioLowLevel = AudioBass
  • AudioMidLevel = AudioMid
  • AudioHighLevel = AudioTreble

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
▶ APPROACH 2: AudioDataTexture (AUTOMATIC - No Float Props Needed)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Best for: VFX without Float properties, just need a Texture2D property
Performance: ~0.005ms (texture binding + sample)
Setup: AUTOMATIC when VFXAudioDataBinder is added

HOW IT WORKS:
  • AudioBridge creates a 2x2 RGBAFloat texture encoding all audio data
  • VFXAudioDataBinder auto-binds this to VFX with 'AudioDataTexture' property
  • VFX samples texture with Sample Texture 2D LOD operator

SETUP:
  1. Add 'AudioDataTexture' as Texture2D exposed property in VFX Graph
  2. Add VFXAudioDataBinder component to VFX GameObject
  3. Done! Audio data is automatically bound

SAMPLING IN VFX GRAPH:
  • Use 'Sample Texture 2D LOD' operator with Level=0
  • UV (0.25, 0.25): Returns (Volume, Bass, Mids, Treble)
  • UV (0.75, 0.25): Returns (SubBass, BeatPulse, BeatIntensity, 0)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
▶ APPROACH 3: Custom HLSL + ARGlobals.hlsl (ADVANCED)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Best for: Complex audio math, no exposed properties at all
Performance: ~0.001ms (direct shader read, fastest possible)
Requires: HLSL knowledge

STEP 1: Ensure AudioBridge is in scene
  • H3M > VFX Pipeline Master > Setup Complete Pipeline

STEP 2: Add Custom HLSL Operator in VFX Graph
  • Right-click in VFX Graph > Create Node > Custom HLSL
  • Set Include: Assets/Shaders/ARGlobals.hlsl
  • Use these helper functions:

float GetAudioVolume()      // 0-1 overall volume
float GetAudioBass()        // 0-1 bass band
float GetAudioMid()         // 0-1 mid band
float GetAudioTreble()      // 0-1 treble band
float GetAudioSubBass()     // 0-1 sub-bass band
float GetBeatPulse()        // 0-1 decaying beat pulse
float GetBeatIntensity()    // 0-1 beat strength
float4 GetAudioData()       // (volume, bass, mid, treble)

EXAMPLE HLSL:
╭────────────────────────────────────────────╮
│ #include ""Assets/Shaders/ARGlobals.hlsl"" │
│                                            │
│ // Scale particle size by bass             │
│ float size = baseSize * (1 + GetAudioBass() * 2);│
│                                            │
│ // Pulse on beat                           │
│ float pulse = GetBeatPulse();              │
│ color.rgb *= 1 + pulse * 2;                │
╰────────────────────────────────────────────╯

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
▶ GLOBAL SHADER PROPERTIES (set by AudioBridge.cs)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

_AudioBands      Vector4  (bass*100, mids*100, treble*100, subBass*100)
_AudioVolume     float    0-1 overall volume
_BeatPulse       float    0-1 decaying pulse (1 on beat, decays to 0)
_BeatIntensity   float    0-1 strength of detected beat

╚══════════════════════════════════════════════════════════════════╝
";

            Debug.Log(guide);

            // Also show in dialog
            EditorUtility.DisplayDialog("VFX Audio Integration Guide",
                "Guide printed to Console.\n\n" +
                "Three approaches:\n\n" +
                "1. RECOMMENDED: Add exposed Float properties to VFX Graph " +
                "(AudioVolume, AudioBass, etc.), then add VFXAudioDataBinder.\n\n" +
                "2. AUTOMATIC: Add 'AudioDataTexture' Texture2D property to VFX Graph. " +
                "VFXAudioDataBinder auto-binds a 2x2 texture with all audio data. " +
                "Sample at UV (0.25,0.25) for (Volume,Bass,Mids,Treble).\n\n" +
                "3. ADVANCED: Use Custom HLSL with #include \"Assets/Shaders/ARGlobals.hlsl\" " +
                "to access global audio properties directly.\n\n" +
                "See Console for detailed instructions.",
                "OK");
        }

        [MenuItem("H3M/VFX Pipeline Master/Audio/Open ARGlobals.hlsl")]
        public static void OpenARGlobals()
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(HLSLPath);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
            else
            {
                Debug.LogError($"[AudioHLSLGuide] Could not find {HLSLPath}");
            }
        }

        [MenuItem("H3M/VFX Pipeline Master/Audio/Copy Custom HLSL Template (Globals)")]
        public static void CopyHLSLTemplate()
        {
            string template = @"// VFX Custom HLSL - Audio Reactive (Global Shader Properties)
// Include the audio globals
#include ""Assets/Shaders/ARGlobals.hlsl""

// Get audio values (all 0-1 normalized)
float volume = GetAudioVolume();
float bass = GetAudioBass();
float mid = GetAudioMid();
float treble = GetAudioTreble();
float beatPulse = GetBeatPulse();

// Example: Scale by audio
float scale = 1.0 + bass * 2.0;

// Example: Pulse color on beat
float3 color = baseColor * (1.0 + beatPulse * 3.0);

// Output
o = scale;";

            GUIUtility.systemCopyBuffer = template;
            Debug.Log("[AudioHLSLGuide] Custom HLSL template (globals) copied to clipboard!");
            EditorUtility.DisplayDialog("Template Copied",
                "Custom HLSL template copied to clipboard.\n\n" +
                "Uses global shader properties (no exposed properties needed).\n" +
                "Paste into a Custom HLSL Operator in VFX Graph.",
                "OK");
        }

        [MenuItem("H3M/VFX Pipeline Master/Audio/Copy AudioVFX Template (Texture)")]
        public static void CopyAudioVFXTemplate()
        {
            string template = @"// VFX Custom HLSL - Audio Reactive (AudioDataTexture)
// Requires: AudioDataTexture (Texture2D) exposed property
#include ""Assets/Shaders/AudioVFX.hlsl""

// Sample audio once for efficiency
float4 audioPrimary = SampleAudioPrimary(AudioDataTexture, samplerAudioDataTexture);
float4 audioExtended = SampleAudioExtended(AudioDataTexture, samplerAudioDataTexture);

float volume = audioPrimary.r;
float bass = audioPrimary.g;
float mids = audioPrimary.b;
float treble = audioPrimary.a;
float beat = audioExtended.g;

// Position: lift by bass, pulse on beat
position.y += bass * 1.5;
float3 pulseDir = normalize(position + 0.001);
position += pulseDir * beat * 2.0;

// Velocity: add turbulence
velocity += sin(position * 5.0) * treble * 0.5;

// Size: pulse on beat
size *= 1.0 + beat * 0.5;";

            GUIUtility.systemCopyBuffer = template;
            Debug.Log("[AudioHLSLGuide] AudioVFX template copied to clipboard!");
            EditorUtility.DisplayDialog("Template Copied",
                "AudioVFX template copied to clipboard.\n\n" +
                "Requires 'AudioDataTexture' (Texture2D) exposed property.\n" +
                "VFXAudioDataBinder will auto-bind the texture.\n\n" +
                "Paste into a Custom HLSL block in VFX Graph.",
                "OK");
        }

        [MenuItem("H3M/VFX Pipeline Master/Audio/Open AudioVFX.hlsl")]
        public static void OpenAudioVFX()
        {
            const string path = "Assets/Shaders/AudioVFX.hlsl";
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
            else
            {
                Debug.LogError($"[AudioHLSLGuide] Could not find {path}");
            }
        }

        [MenuItem("H3M/VFX Pipeline Master/Audio/Open AudioVFX_Examples.hlsl")]
        public static void OpenAudioVFXExamples()
        {
            const string path = "Assets/Shaders/AudioVFX_Examples.hlsl";
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
            else
            {
                Debug.LogError($"[AudioHLSLGuide] Could not find {path}");
            }
        }

        [MenuItem("H3M/VFX Pipeline Master/Audio/Add Audio Monitor")]
        public static void AddAudioMonitor()
        {
            // Check if AudioMonitor already exists
            var existing = Object.FindFirstObjectByType<AudioMonitor>();
            if (existing != null)
            {
                Debug.Log($"[AudioHLSLGuide] AudioMonitor already exists on '{existing.gameObject.name}'");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Try to add to AudioBridge if it exists
            var audioBridge = Object.FindFirstObjectByType<AudioBridge>();
            if (audioBridge != null)
            {
                var monitor = audioBridge.gameObject.AddComponent<AudioMonitor>();
                Undo.RegisterCreatedObjectUndo(monitor, "Add AudioMonitor");
                Debug.Log($"[AudioHLSLGuide] Added AudioMonitor to AudioBridge GameObject");
                Selection.activeGameObject = audioBridge.gameObject;
                return;
            }

            // Create new GameObject with AudioMonitor
            var go = new GameObject("AudioMonitor");
            go.AddComponent<AudioMonitor>();
            Undo.RegisterCreatedObjectUndo(go, "Create AudioMonitor");
            Debug.Log("[AudioHLSLGuide] Created AudioMonitor GameObject (note: AudioBridge not found)");
            Selection.activeGameObject = go;
        }

        [MenuItem("H3M/VFX Pipeline Master/Audio/Setup Complete Audio System")]
        public static void SetupCompleteAudioSystem()
        {
            // Find or create AudioBridge
            var audioBridge = Object.FindFirstObjectByType<AudioBridge>();
            if (audioBridge == null)
            {
                var go = new GameObject("AudioBridge");
                audioBridge = go.AddComponent<AudioBridge>();
                go.AddComponent<AudioSource>();
                Undo.RegisterCreatedObjectUndo(go, "Create AudioBridge");
                Debug.Log("[AudioHLSLGuide] Created AudioBridge with AudioSource");
            }

            // Add AudioMonitor if not present
            var monitor = audioBridge.GetComponent<AudioMonitor>();
            if (monitor == null)
            {
                monitor = audioBridge.gameObject.AddComponent<AudioMonitor>();
                Undo.RegisterCreatedObjectUndo(monitor, "Add AudioMonitor");
                Debug.Log("[AudioHLSLGuide] Added AudioMonitor to AudioBridge");
            }

            Selection.activeGameObject = audioBridge.gameObject;

            EditorUtility.DisplayDialog("Audio System Setup",
                "Audio system configured:\n\n" +
                "• AudioBridge - FFT analysis + beat detection\n" +
                "• AudioMonitor - Visual debug display (M key)\n\n" +
                "Next: Add an AudioClip to the AudioSource and press Play.",
                "OK");
        }

        [MenuItem("H3M/VFX Pipeline Master/Audio/Verify AudioBridge in Scene")]
        public static void VerifyAudioBridge()
        {
            var bridge = Object.FindFirstObjectByType<AudioBridge>();
            if (bridge != null)
            {
                Debug.Log($"[AudioHLSLGuide] ✅ AudioBridge found on '{bridge.gameObject.name}'");

                var source = bridge.GetComponent<AudioSource>();
                if (source == null)
                    source = Object.FindFirstObjectByType<AudioSource>();

                if (source != null)
                {
                    Debug.Log($"[AudioHLSLGuide] ✅ AudioSource found: '{source.gameObject.name}' " +
                        $"(clip: {source.clip?.name ?? "none"}, playing: {source.isPlaying})");
                }
                else
                {
                    Debug.LogWarning("[AudioHLSLGuide] ⚠️ No AudioSource found - add one with audio clip");
                }

                EditorUtility.DisplayDialog("AudioBridge Status",
                    $"✅ AudioBridge active on '{bridge.gameObject.name}'\n\n" +
                    $"Audio Source: {(source != null ? "Found" : "Missing")}\n" +
                    $"Playing: {(source?.isPlaying ?? false)}\n\n" +
                    "Global shader properties are being set:\n" +
                    "• _AudioBands (Vector4)\n" +
                    "• _AudioVolume (float)\n" +
                    "• _BeatPulse (float)\n" +
                    "• _BeatIntensity (float)",
                    "OK");
            }
            else
            {
                Debug.LogWarning("[AudioHLSLGuide] ❌ No AudioBridge found in scene");
                bool add = EditorUtility.DisplayDialog("AudioBridge Missing",
                    "No AudioBridge found in scene.\n\n" +
                    "AudioBridge is required to set global audio shader properties.",
                    "Add AudioBridge", "Cancel");

                if (add)
                {
                    var go = new GameObject("AudioBridge");
                    go.AddComponent<AudioBridge>();
                    go.AddComponent<AudioSource>();
                    Selection.activeGameObject = go;
                    Undo.RegisterCreatedObjectUndo(go, "Add AudioBridge");
                    Debug.Log("[AudioHLSLGuide] Created AudioBridge. Add an AudioClip to the AudioSource.");
                }
            }
        }
    }
}
#endif
