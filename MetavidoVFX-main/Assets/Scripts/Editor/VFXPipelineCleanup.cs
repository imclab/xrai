// VFX Pipeline Cleanup - Removes redundant pipelines, uses Hybrid Bridge as primary
// Created: 2026-01-14

using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using System.Collections.Generic;
using H3M.Core;
using MetavidoVFX.VFX;
using MetavidoVFX.Audio;
using MetavidoVFX.HandTracking;

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
            Debug.Log("[Pipeline Cleanup] Reason: Redundant - creates its own VFX at runtime, conflicts with ARDepthSource/VFXARBinder");
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
                Debug.Log("[Pipeline Cleanup] If VFX only need DepthMap/StencilMap/ColorMap, use ARDepthSource + VFXARBinder.");
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

            // Check ARDepthSource (Hybrid Bridge)
            var depthSource = Object.FindFirstObjectByType<ARDepthSource>(FindObjectsInactive.Include);
            if (depthSource != null)
            {
                Debug.Log($"✓ ARDepthSource: FOUND on '{depthSource.gameObject.name}'");
                Debug.Log($"  - DepthMap: {depthSource.DepthMap}");
                Debug.Log($"  - StencilMap: {depthSource.StencilMap}");
                Debug.Log($"  - PositionMap: {depthSource.PositionMap}");
                Debug.Log($"  - ColorMap Allocated: {depthSource.ColorMapAllocated}");
                Debug.Log($"  - VelocityMap: {depthSource.VelocityMap}");
            }
            else
            {
                Debug.LogWarning("✗ ARDepthSource: NOT FOUND - Add via H3M > VFX Pipeline Master > Create ARDepthSource");
            }

            // Check VFXARBinder usage (Hybrid Bridge)
            var arBinders = Object.FindObjectsByType<VFXARBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (arBinders.Length > 0)
            {
                Debug.Log($"✓ VFXARBinder: FOUND on {arBinders.Length} VFX object(s)");
            }
            else
            {
                Debug.LogWarning("✗ VFXARBinder: NOT FOUND - Add via H3M > VFX > Auto-Setup Binders");
            }

            // Check HologramSource (H3M pipeline)
            var hologramSource = Object.FindFirstObjectByType<HologramSource>(FindObjectsInactive.Include);
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
            var handController = Object.FindFirstObjectByType<HandVFXController>(FindObjectsInactive.Include);
            if (handController != null)
            {
                Debug.Log($"✓ HandVFXController: FOUND on '{handController.gameObject.name}'");
                Debug.Log($"  - Binds: HandPosition, HandVelocity, HandSpeed, BrushWidth, IsPinching");
            }

            var audioBridge = Object.FindFirstObjectByType<AudioBridge>(FindObjectsInactive.Include);
            if (audioBridge != null)
            {
                Debug.Log($"✓ AudioBridge: FOUND on '{audioBridge.gameObject.name}'");
                Debug.Log("  - Binds: AudioVolume, AudioBands (global shader properties)");
            }
            else
            {
                Debug.LogWarning("✗ AudioBridge: NOT FOUND - Add via H3M > VFX Pipeline Master > Create AudioBridge");
            }

            var audioProcessor = Object.FindFirstObjectByType<EnhancedAudioProcessor>(FindObjectsInactive.Include);
            if (audioProcessor != null)
            {
                Debug.Log($"○ EnhancedAudioProcessor: FOUND on '{audioProcessor.gameObject.name}' (legacy/optional)");
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

            // Use reflection for legacy binder
            var legacyType = System.Type.GetType("MetavidoVFX.VFX.Binders.VFXARDataBinder, Assembly-CSharp");
            var legacyBinder = legacyType != null ? Object.FindFirstObjectByType(legacyType, FindObjectsInactive.Include) : null;
            
            if (legacyBinder != null)
            {
                Debug.LogWarning($"⚠️ Legacy VFXARDataBinder found on '{legacyBinder.name}' (replace with VFXARBinder)");
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
            Debug.Log("  1. ARDepthSource + VFXARBinder (primary) - AR textures to all VFX");
            Debug.Log("  2. AudioBridge - audio bands + beat globals");
            Debug.Log("  3. HologramSource/Renderer - H3M hologram features");
            Debug.Log("  4. HandVFXController - Hand tracking properties");
            Debug.Log("  5. SoundWaveEmitter - Expanding wave effects");
        }

    }
}
