// VFX Pipeline Cleanup - Removes redundant pipelines, keeps VFXBinderManager as primary
// Created: 2026-01-14

using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using System.Collections.Generic;

namespace MetavidoVFX.Editor
{
    public static class VFXPipelineCleanup
    {
        [MenuItem("H3M/Pipeline Cleanup/1. Disable PeopleOcclusionVFXManager (Redundant)")]
        public static void DisablePeopleOcclusionVFXManager()
        {
            var managers = Object.FindObjectsByType<PeopleOcclusionVFXManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (managers.Length == 0)
            {
                Debug.Log("[Pipeline Cleanup] No PeopleOcclusionVFXManager found in scene");
                return;
            }

            int disabledCount = 0;
            foreach (var manager in managers)
            {
                if (manager.enabled)
                {
                    manager.enabled = false;
                    EditorUtility.SetDirty(manager);
                    disabledCount++;
                    Debug.Log($"[Pipeline Cleanup] Disabled PeopleOcclusionVFXManager on '{manager.gameObject.name}'");
                }
            }

            Debug.Log($"[Pipeline Cleanup] ✓ Disabled {disabledCount} PeopleOcclusionVFXManager(s)");
            Debug.Log("[Pipeline Cleanup] Reason: Redundant - creates its own VFX at runtime, conflicts with VFXBinderManager");
        }

        [MenuItem("H3M/Pipeline Cleanup/2. Find ARKitMetavidoBinder Components")]
        public static void FindARKitMetavidoBinders()
        {
            // ARKitMetavidoBinder is in Metavido namespace
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var bindersFound = new List<(GameObject go, Component binder)>();

            foreach (var vfx in allVFX)
            {
                // Check for VFXPropertyBinder components
                var binders = vfx.GetComponents<UnityEngine.VFX.Utility.VFXPropertyBinder>();
                foreach (var binder in binders)
                {
                    // Check if it's ARKitMetavidoBinder by type name
                    if (binder.GetType().Name == "ARKitMetavidoBinder" ||
                        binder.GetType().FullName?.Contains("ARKitMetavidoBinder") == true)
                    {
                        bindersFound.Add((vfx.gameObject, binder));
                    }
                }

                // Also check for the binder directly
                var directBinders = vfx.GetComponents<Component>();
                foreach (var comp in directBinders)
                {
                    if (comp != null && comp.GetType().Name == "ARKitMetavidoBinder")
                    {
                        if (!bindersFound.Exists(b => b.binder == comp))
                            bindersFound.Add((vfx.gameObject, comp));
                    }
                }
            }

            if (bindersFound.Count == 0)
            {
                Debug.Log("[Pipeline Cleanup] No ARKitMetavidoBinder components found on VFX");
            }
            else
            {
                Debug.Log($"[Pipeline Cleanup] Found {bindersFound.Count} ARKitMetavidoBinder(s):");
                foreach (var (go, binder) in bindersFound)
                {
                    Debug.Log($"  - {go.name}: {binder.GetType().Name}");
                }
                Debug.Log("[Pipeline Cleanup] Note: ARKitMetavidoBinder computes PositionMap. If VFX need world-space positions, keep it.");
                Debug.Log("[Pipeline Cleanup] If VFX only need DepthMap/StencilMap/ColorMap, VFXBinderManager handles that.");
            }
        }

        [MenuItem("H3M/Pipeline Cleanup/3. Remove ARKitMetavidoBinder Components")]
        public static void RemoveARKitMetavidoBinders()
        {
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int removedCount = 0;

            foreach (var vfx in allVFX)
            {
                var components = vfx.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp != null && comp.GetType().Name == "ARKitMetavidoBinder")
                    {
                        Debug.Log($"[Pipeline Cleanup] Removing ARKitMetavidoBinder from '{vfx.gameObject.name}'");
                        Undo.DestroyObjectImmediate(comp);
                        removedCount++;
                    }
                }
            }

            if (removedCount == 0)
            {
                Debug.Log("[Pipeline Cleanup] No ARKitMetavidoBinder components to remove");
            }
            else
            {
                Debug.Log($"[Pipeline Cleanup] ✓ Removed {removedCount} ARKitMetavidoBinder component(s)");
            }
        }

        [MenuItem("H3M/Pipeline Cleanup/4. Verify VFX Data Sources")]
        public static void VerifyVFXDataSources()
        {
            Debug.Log("=== VFX Data Source Verification ===\n");

            // Check VFXBinderManager
            var binderManager = Object.FindFirstObjectByType<VFX.VFXBinderManager>();
            if (binderManager != null)
            {
                Debug.Log($"✓ VFXBinderManager: FOUND on '{binderManager.gameObject.name}'");
                Debug.Log($"  - Auto-find sources: {GetFieldValue(binderManager, "autoFindSources")}");
                Debug.Log($"  - Binds: DepthMap, StencilMap, ColorMap, InverseView, DepthRange");
            }
            else
            {
                Debug.LogWarning("✗ VFXBinderManager: NOT FOUND - Add via H3M > EchoVision > Setup");
            }

            // Check HologramSource (H3M pipeline)
            var hologramSource = Object.FindFirstObjectByType<H3M.Core.HologramSource>(FindObjectsInactive.Include);
            if (hologramSource != null)
            {
                Debug.Log($"✓ HologramSource (H3M): FOUND on '{hologramSource.gameObject.name}'");
                Debug.Log($"  - Binds: PositionMap (computed), ColorTexture, StencilTexture");
            }
            else
            {
                Debug.Log("○ HologramSource (H3M): Not in scene (optional for hologram features)");
            }

            // Check specialized controllers
            var handController = Object.FindFirstObjectByType<HandTracking.HandVFXController>(FindObjectsInactive.Include);
            if (handController != null)
            {
                Debug.Log($"✓ HandVFXController: FOUND on '{handController.gameObject.name}'");
                Debug.Log($"  - Binds: HandPosition, HandVelocity, HandSpeed, BrushWidth, IsPinching");
            }

            var audioProcessor = Object.FindFirstObjectByType<Audio.EnhancedAudioProcessor>(FindObjectsInactive.Include);
            if (audioProcessor != null)
            {
                Debug.Log($"✓ EnhancedAudioProcessor: FOUND on '{audioProcessor.gameObject.name}'");
                Debug.Log($"  - Binds: AudioVolume, AudioBass, AudioMid, AudioTreble");
            }

            var soundWaveEmitter = Object.FindFirstObjectByType<SoundWaveEmitter>(FindObjectsInactive.Include);
            if (soundWaveEmitter != null)
            {
                Debug.Log($"✓ SoundWaveEmitter: FOUND on '{soundWaveEmitter.gameObject.name}'");
                Debug.Log($"  - Binds: WaveOrigin, WaveDirection, WaveRange, WaveAge");
            }

            // Check for redundant/disabled pipelines
            var peopleOcclusion = Object.FindFirstObjectByType<PeopleOcclusionVFXManager>(FindObjectsInactive.Include);
            if (peopleOcclusion != null)
            {
                string status = peopleOcclusion.enabled ? "⚠️ ENABLED (REDUNDANT)" : "○ Disabled (OK)";
                Debug.Log($"{status} PeopleOcclusionVFXManager on '{peopleOcclusion.gameObject.name}'");
            }

            // Count VFX
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int enabledVFX = 0;
            foreach (var vfx in allVFX)
            {
                if (vfx.enabled) enabledVFX++;
            }
            Debug.Log($"\n📊 Total VFX: {allVFX.Length} ({enabledVFX} enabled)");

            Debug.Log("\n=== Verification Complete ===");
        }

        [MenuItem("H3M/Pipeline Cleanup/5. Run Full Cleanup")]
        public static void RunFullCleanup()
        {
            Debug.Log("=== Running Full Pipeline Cleanup ===\n");

            DisablePeopleOcclusionVFXManager();
            Debug.Log("");

            FindARKitMetavidoBinders();
            Debug.Log("");

            VerifyVFXDataSources();

            Debug.Log("\n=== Cleanup Complete ===");
            Debug.Log("Recommended architecture:");
            Debug.Log("  1. VFXBinderManager (primary) - AR textures to all VFX");
            Debug.Log("  2. HologramSource/Renderer - H3M hologram features");
            Debug.Log("  3. HandVFXController - Hand tracking properties");
            Debug.Log("  4. EnhancedAudioProcessor - Audio frequency bands");
            Debug.Log("  5. SoundWaveEmitter - Expanding wave effects");
        }

        private static object GetFieldValue(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            return field?.GetValue(obj) ?? "N/A";
        }
    }
}
