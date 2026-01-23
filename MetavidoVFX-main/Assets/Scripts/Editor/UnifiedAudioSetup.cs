// UnifiedAudioSetup.cs - Editor utilities for Unified Audio Reactive system
// Part of Spec 011: OpenBrush Integration
//
// Provides one-click setup for the unified 8-band audio system.

using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using XRRAI.Audio;
using XRRAI.VFXBinders;

namespace XRRAI.Editor
{
    /// <summary>
    /// Editor menu commands for setting up the Unified Audio Reactive system.
    /// </summary>
    public static class UnifiedAudioSetup
    {
        private const string MENU_PREFIX = "H3M/Audio/";

        [MenuItem(MENU_PREFIX + "Setup Complete Audio Pipeline", priority = 0)]
        public static void SetupCompletePipeline()
        {
            Undo.SetCurrentGroupName("Setup Complete Audio Pipeline");
            int group = Undo.GetCurrentGroup();

            bool createdSource = CreateUnifiedAudioReactive();
            int vfxCount = AddAudioBindersToAllVFX();

            Undo.CollapseUndoOperations(group);

            string message = $"Audio Pipeline Setup Complete:\n";
            message += createdSource ? "• Created UnifiedAudioReactive singleton\n" : "• UnifiedAudioReactive already exists\n";
            message += $"• Added VFXAudioDataBinder8Band to {vfxCount} VFX\n";
            message += "\nUse H3M > Audio > Verify Audio Setup to check configuration.";

            EditorUtility.DisplayDialog("Audio Pipeline Setup", message, "OK");
            Debug.Log($"[UnifiedAudioSetup] {message.Replace("\n", " ")}");
        }

        [MenuItem(MENU_PREFIX + "Create UnifiedAudioReactive", priority = 10)]
        public static bool CreateUnifiedAudioReactive()
        {
            var existing = Object.FindFirstObjectByType<UnifiedAudioReactive>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[UnifiedAudioSetup] UnifiedAudioReactive already exists in scene.");
                return false;
            }

            var go = new GameObject("UnifiedAudioReactive");
            Undo.RegisterCreatedObjectUndo(go, "Create UnifiedAudioReactive");

            var audio = go.AddComponent<UnifiedAudioReactive>();

            // Check for existing AudioSource in scene
            var existingAudioSource = Object.FindFirstObjectByType<AudioSource>();
            if (existingAudioSource != null)
            {
                // Use reflection to set the serialized field
                var serializedObj = new SerializedObject(audio);
                var audioSourceProp = serializedObj.FindProperty("_audioSource");
                if (audioSourceProp != null)
                {
                    audioSourceProp.objectReferenceValue = existingAudioSource;
                    serializedObj.ApplyModifiedProperties();
                    Debug.Log($"[UnifiedAudioSetup] Linked to existing AudioSource: {existingAudioSource.name}");
                }
            }

            Selection.activeGameObject = go;
            Debug.Log("[UnifiedAudioSetup] Created UnifiedAudioReactive singleton.");
            return true;
        }

        [MenuItem(MENU_PREFIX + "Add Audio Binder to Selected VFX", priority = 20)]
        public static void AddAudioBinderToSelected()
        {
            int count = 0;
            foreach (var go in Selection.gameObjects)
            {
                var vfx = go.GetComponent<VisualEffect>();
                if (vfx != null)
                {
                    if (AddAudioBinderToVFX(vfx))
                        count++;
                }
            }

            if (count == 0)
            {
                EditorUtility.DisplayDialog("No VFX Selected",
                    "Please select one or more GameObjects with VisualEffect components.", "OK");
            }
            else
            {
                Debug.Log($"[UnifiedAudioSetup] Added VFXAudioDataBinder8Band to {count} VFX.");
            }
        }

        [MenuItem(MENU_PREFIX + "Add Audio Binder to Selected VFX", true)]
        public static bool AddAudioBinderToSelectedValidate()
        {
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<VisualEffect>() != null)
                    return true;
            }
            return false;
        }

        [MenuItem(MENU_PREFIX + "Add Audio Binders to All VFX", priority = 21)]
        public static int AddAudioBindersToAllVFX()
        {
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var vfx in allVFX)
            {
                if (AddAudioBinderToVFX(vfx))
                    count++;
            }

            Debug.Log($"[UnifiedAudioSetup] Added VFXAudioDataBinder8Band to {count} of {allVFX.Length} VFX.");
            return count;
        }

        private static bool AddAudioBinderToVFX(VisualEffect vfx)
        {
            // Skip if already has the binder
            if (vfx.GetComponent<VFXAudioDataBinder8Band>() != null)
                return false;

            // Check if VFX has any audio-related properties
            bool hasAudioProps = HasAudioProperties(vfx);
            if (!hasAudioProps)
            {
                Debug.Log($"[UnifiedAudioSetup] Skipping {vfx.name} - no audio properties found.");
                return false;
            }

            Undo.AddComponent<VFXAudioDataBinder8Band>(vfx.gameObject);
            Debug.Log($"[UnifiedAudioSetup] Added VFXAudioDataBinder8Band to {vfx.name}");
            return true;
        }

        private static bool HasAudioProperties(VisualEffect vfx)
        {
            // Check for common audio property names
            string[] audioPropertyNames = {
                "AudioBand0", "AudioBand1", "AudioBand2", "AudioBand3",
                "AudioBand4", "AudioBand5", "AudioBand6", "AudioBand7",
                "AudioSubBass", "AudioBass", "AudioMid", "AudioTreble",
                "AudioVolume", "AudioPeak", "BeatPulse", "BeatIntensity",
                "AudioDataTexture"
            };

            foreach (var propName in audioPropertyNames)
            {
                if (vfx.HasFloat(propName) || vfx.HasTexture(propName))
                    return true;
            }
            return false;
        }

        [MenuItem(MENU_PREFIX + "Verify Audio Setup", priority = 100)]
        public static void VerifyAudioSetup()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Unified Audio Setup Report ===\n");

            // Check UnifiedAudioReactive
            var unified = Object.FindFirstObjectByType<UnifiedAudioReactive>();
            if (unified != null)
            {
                report.AppendLine("✅ UnifiedAudioReactive: Found");

                // Check audio source via serialized object
                var serializedObj = new SerializedObject(unified);
                var audioSourceProp = serializedObj.FindProperty("_audioSource");
                if (audioSourceProp != null && audioSourceProp.objectReferenceValue != null)
                {
                    report.AppendLine($"   AudioSource: {audioSourceProp.objectReferenceValue.name}");
                }
                else
                {
                    report.AppendLine("   ⚠️ AudioSource: Not assigned (will auto-detect)");
                }
            }
            else
            {
                report.AppendLine("❌ UnifiedAudioReactive: Not found");
                report.AppendLine("   Run 'H3M > Audio > Setup Complete Audio Pipeline'");
            }

            // Check legacy AudioBridge
            var legacyBridge = Object.FindFirstObjectByType<AudioBridge>();
            if (legacyBridge != null)
            {
                report.AppendLine("\n⚠️ Legacy AudioBridge found - can coexist but consider removing");
            }

            // Check VFX with audio binders
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            int with8Band = 0;
            int withLegacy = 0;
            int withAudioProps = 0;

            foreach (var vfx in allVFX)
            {
                if (vfx.GetComponent<VFXAudioDataBinder8Band>() != null)
                    with8Band++;
                if (vfx.GetComponent<VFXAudioDataBinder>() != null)
                    withLegacy++;
                if (HasAudioProperties(vfx))
                    withAudioProps++;
            }

            report.AppendLine($"\n=== VFX Audio Status ===");
            report.AppendLine($"Total VFX in scene: {allVFX.Length}");
            report.AppendLine($"VFX with audio properties: {withAudioProps}");
            report.AppendLine($"VFX with VFXAudioDataBinder8Band: {with8Band}");
            if (withLegacy > 0)
            {
                report.AppendLine($"⚠️ VFX with legacy VFXAudioDataBinder: {withLegacy}");
                report.AppendLine("   Consider migrating to VFXAudioDataBinder8Band");
            }

            if (withAudioProps > with8Band)
            {
                report.AppendLine($"\n⚠️ {withAudioProps - with8Band} VFX have audio properties but no binder");
                report.AppendLine("   Run 'H3M > Audio > Add Audio Binders to All VFX'");
            }

            report.AppendLine("\n=== Summary ===");
            bool setupComplete = unified != null && with8Band >= withAudioProps;
            if (setupComplete)
            {
                report.AppendLine("✅ Audio pipeline is properly configured");
            }
            else
            {
                report.AppendLine("⚠️ Audio pipeline needs attention");
            }

            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("Audio Setup Report", report.ToString(), "OK");
        }

        [MenuItem(MENU_PREFIX + "Remove Legacy Audio Binders", priority = 101)]
        public static void RemoveLegacyAudioBinders()
        {
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            int removed = 0;

            foreach (var vfx in allVFX)
            {
                var legacyBinder = vfx.GetComponent<VFXAudioDataBinder>();
                if (legacyBinder != null)
                {
                    Undo.DestroyObjectImmediate(legacyBinder);
                    removed++;
                }
            }

            if (removed > 0)
            {
                Debug.Log($"[UnifiedAudioSetup] Removed {removed} legacy VFXAudioDataBinder components.");
                EditorUtility.DisplayDialog("Legacy Binders Removed",
                    $"Removed {removed} legacy VFXAudioDataBinder components.\n\n" +
                    "Run 'Add Audio Binders to All VFX' to add 8-band binders.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Legacy Binders",
                    "No legacy VFXAudioDataBinder components found.", "OK");
            }
        }

        [MenuItem(MENU_PREFIX + "List VFX Audio Properties", priority = 200)]
        public static void ListVFXAudioProperties()
        {
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== VFX Audio Properties ===\n");

            string[] audioPropertyNames = {
                "AudioBand0", "AudioBand1", "AudioBand2", "AudioBand3",
                "AudioBand4", "AudioBand5", "AudioBand6", "AudioBand7",
                "AudioSubBass", "AudioBass", "AudioMid", "AudioTreble",
                "AudioVolume", "AudioPeak", "BeatPulse", "BeatIntensity",
                "AudioDataTexture"
            };

            foreach (var vfx in allVFX)
            {
                var props = new System.Collections.Generic.List<string>();
                foreach (var propName in audioPropertyNames)
                {
                    if (vfx.HasFloat(propName))
                        props.Add($"{propName} (float)");
                    else if (vfx.HasTexture(propName))
                        props.Add($"{propName} (texture)");
                }

                if (props.Count > 0)
                {
                    report.AppendLine($"{vfx.name}:");
                    foreach (var prop in props)
                    {
                        report.AppendLine($"  • {prop}");
                    }
                    report.AppendLine();
                }
            }

            if (report.Length < 40)
            {
                report.AppendLine("No VFX with audio properties found.");
            }

            Debug.Log(report.ToString());
        }
    }
}
