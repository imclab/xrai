// H3MHologramSetup - Editor utilities for setting up H3M Hologram prefabs
// Created: 2026-01-16

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;
using H3M.Core;

namespace H3M.Editor
{
    /// <summary>
    /// Editor setup utilities for H3M Hologram prefabs.
    /// Provides menu items for instantiating and wiring hologram components.
    /// </summary>
    public static class H3MHologramSetup
    {
        private const string PREFAB_PATH = "Assets/H3M/Prefabs/";
        private const string RIG_PREFAB = "H3M_HologramRig.prefab";
        private const string SOURCE_PREFAB = "H3M_HologramSource.prefab";
        private const string RENDERER_PREFAB = "H3M_HologramRenderer.prefab";
        private const string ANCHOR_PREFAB = "H3M_HologramAnchor.prefab";
        private const string HOLOGRAM_VFX_PATH = "Assets/H3M/VFX/hologram_depth_people_metavido.vfx";

        #region Menu Items - Complete Setup

        [MenuItem("H3M/Hologram/Setup Complete Hologram Rig", priority = 100)]
        public static void SetupCompleteRig()
        {
            // Check for existing rig
            var existingRig = Object.FindFirstObjectByType<HologramSource>();
            if (existingRig != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Hologram Rig Exists",
                    $"A HologramSource already exists on '{existingRig.gameObject.name}'.\n\nReplace it?",
                    "Replace", "Cancel");

                if (!replace) return;
                Undo.DestroyObjectImmediate(existingRig.gameObject.transform.root.gameObject);
            }

            // Load and instantiate prefab
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH + RIG_PREFAB);
            if (prefab == null)
            {
                Debug.LogError($"[H3MHologramSetup] Prefab not found: {PREFAB_PATH + RIG_PREFAB}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create Hologram Rig");

            // Wire references
            WireHologramRig(instance);

            // Assign VFX asset
            AssignHologramVFX(instance);

            Selection.activeGameObject = instance;
            Debug.Log("[H3MHologramSetup] Complete Hologram Rig created and wired");
        }

        [MenuItem("H3M/Hologram/Add HologramSource Only", priority = 110)]
        public static void AddHologramSourceOnly()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH + SOURCE_PREFAB);
            if (prefab == null)
            {
                Debug.LogError($"[H3MHologramSetup] Prefab not found: {PREFAB_PATH + SOURCE_PREFAB}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create HologramSource");

            WireHologramSource(instance.GetComponent<HologramSource>());

            Selection.activeGameObject = instance;
            Debug.Log("[H3MHologramSetup] HologramSource created and wired");
        }

        [MenuItem("H3M/Hologram/Add HologramRenderer to Selected VFX", priority = 111)]
        public static void AddHologramRendererToSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Select a GameObject with a VisualEffect component.", "OK");
                return;
            }

            var vfx = selected.GetComponent<VisualEffect>();
            if (vfx == null)
            {
                EditorUtility.DisplayDialog("No VFX", "Selected GameObject doesn't have a VisualEffect component.", "OK");
                return;
            }

            // Check if already has HologramRenderer
            if (selected.GetComponent<HologramRenderer>() != null)
            {
                Debug.Log("[H3MHologramSetup] Selected VFX already has HologramRenderer");
                return;
            }

            var renderer = Undo.AddComponent<HologramRenderer>(selected);
            WireHologramRenderer(renderer);

            Debug.Log($"[H3MHologramSetup] Added HologramRenderer to '{selected.name}'");
        }

        #endregion

        #region Menu Items - Validation

        [MenuItem("H3M/Hologram/Verify Hologram Setup", priority = 200)]
        public static void VerifySetup()
        {
            Debug.Log("=== H3M Hologram Setup Verification ===\n");

            // Check AR components
            var arSession = Object.FindFirstObjectByType<ARSession>();
            var arOcclusion = Object.FindFirstObjectByType<AROcclusionManager>();
            var arCamera = arOcclusion?.GetComponent<Camera>();

            LogCheck("AR Session", arSession != null);
            LogCheck("AR Occlusion Manager", arOcclusion != null);
            if (arOcclusion != null)
            {
                Debug.Log($"  - Depth Mode: {arOcclusion.requestedEnvironmentDepthMode}");
                Debug.Log($"  - Stencil Mode: {arOcclusion.requestedHumanStencilMode}");
            }

            // Check Hologram components
            var source = Object.FindFirstObjectByType<HologramSource>();
            var renderer = Object.FindFirstObjectByType<HologramRenderer>();
            var anchor = Object.FindFirstObjectByType<HologramAnchor>();
            var debugUI = Object.FindFirstObjectByType<HologramDebugUI>();

            LogCheck("HologramSource", source != null);
            LogCheck("HologramRenderer", renderer != null);
            LogCheck("HologramAnchor", anchor != null, optional: true);
            LogCheck("HologramDebugUI", debugUI != null, optional: true);

            // Check VFX
            if (renderer != null)
            {
                var vfx = renderer.GetComponent<VisualEffect>();
                LogCheck("VFX on Renderer", vfx != null);
                if (vfx != null)
                {
                    LogCheck("VFX Asset assigned", vfx.visualEffectAsset != null);
                    if (vfx.visualEffectAsset != null)
                        Debug.Log($"  - VFX Asset: {vfx.visualEffectAsset.name}");
                }
            }

            // Check wiring via SerializedObject
            if (source != null)
            {
                var so = new SerializedObject(source);
                var occProp = so.FindProperty("_occlusionManager");
                var camProp = so.FindProperty("_arCamera");
                var computeProp = so.FindProperty("_computeShader");

                LogCheck("Source → Occlusion Manager", occProp?.objectReferenceValue != null);
                LogCheck("Source → AR Camera", camProp?.objectReferenceValue != null);
                LogCheck("Source → Compute Shader", computeProp?.objectReferenceValue != null);
            }

            if (renderer != null)
            {
                var so = new SerializedObject(renderer);
                var sourceProp = so.FindProperty("_source");
                var anchorProp = so.FindProperty("_anchor");

                LogCheck("Renderer → Source", sourceProp?.objectReferenceValue != null);
                LogCheck("Renderer → Anchor", anchorProp?.objectReferenceValue != null, optional: true);
            }

            Debug.Log("\n=== Verification Complete ===");
        }

        [MenuItem("H3M/Hologram/Re-Wire All References", priority = 201)]
        public static void ReWireAllReferences()
        {
            var sources = Object.FindObjectsByType<HologramSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var renderers = Object.FindObjectsByType<HologramRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            int wired = 0;
            foreach (var source in sources)
            {
                WireHologramSource(source);
                wired++;
            }

            foreach (var renderer in renderers)
            {
                WireHologramRenderer(renderer);
                wired++;
            }

            Debug.Log($"[H3MHologramSetup] Re-wired {wired} hologram components");
        }

        #endregion

        #region Wiring Helpers

        private static void WireHologramRig(GameObject rig)
        {
            // Find children
            var sourceTransform = rig.transform.Find("Source");
            var rendererTransform = rig.transform.Find("Renderer");
            var anchorTransform = rig.transform.Find("Anchor");
            var debugUITransform = rig.transform.Find("DebugUI");

            // Wire Source
            if (sourceTransform != null)
            {
                var source = sourceTransform.GetComponent<HologramSource>();
                if (source != null) WireHologramSource(source);
            }

            // Wire Renderer
            if (rendererTransform != null)
            {
                var renderer = rendererTransform.GetComponent<HologramRenderer>();
                if (renderer != null)
                {
                    WireHologramRenderer(renderer);

                    // Link to Source
                    var source = sourceTransform?.GetComponent<HologramSource>();
                    if (source != null)
                    {
                        var so = new SerializedObject(renderer);
                        so.FindProperty("_source").objectReferenceValue = source;
                        so.ApplyModifiedProperties();
                    }

                    // Link to Anchor
                    if (anchorTransform != null)
                    {
                        var so = new SerializedObject(renderer);
                        so.FindProperty("_anchor").objectReferenceValue = anchorTransform;
                        so.ApplyModifiedProperties();
                    }
                }
            }

            // Wire Anchor
            if (anchorTransform != null)
            {
                var anchor = anchorTransform.GetComponent<HologramAnchor>();
                if (anchor != null && rendererTransform != null)
                {
                    var so = new SerializedObject(anchor);
                    var hologramRootProp = so.FindProperty("_hologramRoot");
                    if (hologramRootProp != null)
                    {
                        hologramRootProp.objectReferenceValue = rendererTransform;
                        so.ApplyModifiedProperties();
                    }
                }
            }

            // Wire DebugUI
            if (debugUITransform != null)
            {
                var debugUI = debugUITransform.GetComponent<HologramDebugUI>();
                if (debugUI != null)
                {
                    var so = new SerializedObject(debugUI);

                    var rendererProp = so.FindProperty("hologramRenderer");
                    var sourceProp = so.FindProperty("hologramSource");

                    if (rendererProp != null && rendererTransform != null)
                        rendererProp.objectReferenceValue = rendererTransform.GetComponent<HologramRenderer>();

                    if (sourceProp != null && sourceTransform != null)
                        sourceProp.objectReferenceValue = sourceTransform.GetComponent<HologramSource>();

                    so.ApplyModifiedProperties();
                }
            }
        }

        private static void WireHologramSource(HologramSource source)
        {
            if (source == null) return;

            var so = new SerializedObject(source);

            // Find AR components
            var arOcclusion = Object.FindFirstObjectByType<AROcclusionManager>();
            var arCamera = arOcclusion?.GetComponent<Camera>() ?? Camera.main;
            var computeShader = Resources.Load<ComputeShader>("DepthToWorld");

            // Wire references
            so.FindProperty("_occlusionManager").objectReferenceValue = arOcclusion;
            so.FindProperty("_arCamera").objectReferenceValue = arCamera;
            if (computeShader != null)
                so.FindProperty("_computeShader").objectReferenceValue = computeShader;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(source);

            Debug.Log($"[H3MHologramSetup] Wired HologramSource on '{source.gameObject.name}'");
        }

        private static void WireHologramRenderer(HologramRenderer renderer)
        {
            if (renderer == null) return;

            var so = new SerializedObject(renderer);

            // Find HologramSource
            var source = Object.FindFirstObjectByType<HologramSource>();
            if (source != null)
                so.FindProperty("_source").objectReferenceValue = source;

            // Find HologramAnchor
            var anchor = Object.FindFirstObjectByType<HologramAnchor>();
            if (anchor != null)
                so.FindProperty("_anchor").objectReferenceValue = anchor.transform;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(renderer);

            Debug.Log($"[H3MHologramSetup] Wired HologramRenderer on '{renderer.gameObject.name}'");
        }

        private static void AssignHologramVFX(GameObject rig)
        {
            var rendererTransform = rig.transform.Find("Renderer");
            if (rendererTransform == null) return;

            var vfx = rendererTransform.GetComponent<VisualEffect>();
            if (vfx == null) return;

            // Try to load the hologram VFX asset
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(HOLOGRAM_VFX_PATH);
            if (vfxAsset != null)
            {
                vfx.visualEffectAsset = vfxAsset;
                EditorUtility.SetDirty(vfx);
                Debug.Log($"[H3MHologramSetup] Assigned VFX: {vfxAsset.name}");
            }
            else
            {
                Debug.LogWarning($"[H3MHologramSetup] Hologram VFX not found at: {HOLOGRAM_VFX_PATH}");
                Debug.Log("[H3MHologramSetup] Please assign a VFX asset manually to the Renderer's VisualEffect component");
            }
        }

        private static void LogCheck(string name, bool passed, bool optional = false)
        {
            string status = passed ? "✓" : (optional ? "○" : "✗");
            string suffix = !passed && optional ? " (optional)" : "";
            Debug.Log($"  [{status}] {name}{suffix}");
        }

        #endregion
    }
}
#endif
