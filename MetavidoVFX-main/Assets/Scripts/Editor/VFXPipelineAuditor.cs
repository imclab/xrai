// VFXPipelineAuditor - Comprehensive pipeline audit and fix system
// Checks entire VFX pipeline: ARDepthSource → VFXARBinder → VFX → associated components
// Includes all binders, prefab wiring, and performance components

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using System.Collections.Generic;
using System.Text;
using XRRAI.VFXBinders;

/// <summary>
/// Comprehensive audit result for a single VFX and its pipeline components
/// </summary>
public class VFXPipelineAuditResult
{
    public VisualEffect VFX;
    public string VFXName => VFX?.name ?? "Unknown";
    public string AssetName => VFX?.visualEffectAsset?.name ?? "No Asset";

    // Component status
    public bool HasVFXARBinder;
    public bool HasVFXCategory;
    public bool HasVFXNoCull;
    public bool HasNNCamBinder;
    public bool HasHandBinder;
    public bool HasAudioBinder;
    public bool HasPhysicsBinder;

    // Binding status (from VFXARBinder auto-detect)
    public int BindingsConfigured;
    public List<string> ConfiguredBindings = new();
    public List<string> MissingBindings = new();
    public List<string> OptionalBindings = new();  // User can choose to add these
    public List<string> SelectedOptionalBindings = new();  // User-selected optional bindings

    // Specialized bindings (require dedicated binders, not VFXARBinder)
    public List<string> SpecializedBindingsNeeded = new();  // e.g., KeypointBuffer
    public List<string> SpecializedBindingsConfigured = new();  // Already handled by specialized binders

    // Prefab/hierarchy status
    public bool HasARDepthSourceInScene;
    public bool HasAudioBridgeInScene;
    public bool HasBodySegmenterInScene;

    // Performance status
    public bool IsVisible;
    public bool IsCulled;
    public float BoundsSize;

    // Issues and recommendations
    public List<string> Issues = new();
    public List<string> Recommendations = new();
    public int FixesApplied;

    public bool HasIssues => Issues.Count > 0;
    public bool IsHealthy => !HasIssues;
}

/// <summary>
/// Scene-wide audit result
/// </summary>
public class ScenePipelineAuditResult
{
    public List<VFXPipelineAuditResult> VFXResults = new();
    public int TotalVFX;
    public int HealthyVFX;
    public int VFXWithIssues;

    // Scene infrastructure
    public bool HasARDepthSource;
    public bool HasAudioBridge;
    public bool HasBodySegmenter;
    public bool HasARCameraManager;
    public bool HasAROcclusionManager;

    public List<string> SceneIssues = new();
    public int TotalFixesApplied;
}

public static class VFXPipelineAuditor
{
    /// <summary>
    /// Run comprehensive audit on all VFX in scene
    /// </summary>
    [MenuItem("H3M/VFX Pipeline Master/Audit & Fix/Audit Entire Pipeline")]
    public static void AuditEntirePipeline()
    {
        var result = RunSceneAudit();
        LogAuditResults(result);
    }

    /// <summary>
    /// Audit and auto-fix all issues found
    /// </summary>
    [MenuItem("H3M/VFX Pipeline Master/Audit & Fix/Audit & Fix All Issues")]
    public static void AuditAndFixAll()
    {
        var result = RunSceneAudit();
        FixAllIssues(result);
        LogAuditResults(result);
    }

    /// <summary>
    /// Audit selected VFX only
    /// </summary>
    [MenuItem("H3M/VFX Pipeline Master/Audit & Fix/Audit Selected VFX")]
    public static void AuditSelectedVFX()
    {
        var selected = Selection.activeGameObject;
        if (selected == null || !selected.TryGetComponent<VisualEffect>(out var vfx))
        {
            Debug.LogWarning("[VFXPipelineAuditor] No VFX selected. Select a GameObject with VisualEffect component.");
            return;
        }

        var result = AuditSingleVFX(vfx);
        LogSingleVFXResult(result);
    }

    /// <summary>
    /// Audit and fix selected VFX
    /// </summary>
    [MenuItem("H3M/VFX Pipeline Master/Audit & Fix/Audit & Fix Selected VFX")]
    public static void AuditAndFixSelectedVFX()
    {
        var selected = Selection.activeGameObject;
        if (selected == null || !selected.TryGetComponent<VisualEffect>(out var vfx))
        {
            Debug.LogWarning("[VFXPipelineAuditor] No VFX selected. Select a GameObject with VisualEffect component.");
            return;
        }

        var result = AuditSingleVFX(vfx);
        FixSingleVFXIssues(result);
        LogSingleVFXResult(result);
    }

    /// <summary>
    /// Run comprehensive scene-wide audit
    /// </summary>
    public static ScenePipelineAuditResult RunSceneAudit()
    {
        var result = new ScenePipelineAuditResult();

        // Check scene infrastructure
        result.HasARDepthSource = Object.FindFirstObjectByType<ARDepthSource>() != null;
        result.HasAudioBridge = Object.FindFirstObjectByType<AudioBridge>() != null;

        // Use reflection for BodyPartSegmenter (conditionally compiled)
        result.HasBodySegmenter = FindComponentByTypeName("BodyPartSegmenter") != null;

        result.HasARCameraManager = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraManager>() != null;
        result.HasAROcclusionManager = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.AROcclusionManager>() != null;

        // Check scene-level issues
        if (!result.HasARDepthSource)
            result.SceneIssues.Add("Missing ARDepthSource singleton - core pipeline component required");
        if (!result.HasARCameraManager)
            result.SceneIssues.Add("Missing ARCameraManager - AR Foundation required");
        if (!result.HasAROcclusionManager)
            result.SceneIssues.Add("Missing AROcclusionManager - depth data required");

        // Audit all VFX in scene
        var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
        result.TotalVFX = allVFX.Length;

        foreach (var vfx in allVFX)
        {
            var vfxResult = AuditSingleVFX(vfx);
            vfxResult.HasARDepthSourceInScene = result.HasARDepthSource;
            vfxResult.HasAudioBridgeInScene = result.HasAudioBridge;
            vfxResult.HasBodySegmenterInScene = result.HasBodySegmenter;

            result.VFXResults.Add(vfxResult);

            if (vfxResult.IsHealthy)
                result.HealthyVFX++;
            else
                result.VFXWithIssues++;
        }

        return result;
    }

    /// <summary>
    /// Audit a single VFX and all its associated components
    /// </summary>
    public static VFXPipelineAuditResult AuditSingleVFX(VisualEffect vfx)
    {
        var result = new VFXPipelineAuditResult { VFX = vfx };
        var go = vfx.gameObject;

        // Check required components
        result.HasVFXARBinder = go.TryGetComponent<VFXARBinder>(out var binder);
        result.HasVFXCategory = go.TryGetComponent<VFXCategory>(out var category);
        result.HasVFXNoCull = go.TryGetComponent<VFXNoCull>(out var noCull);

        // Check optional specialized binders
        result.HasNNCamBinder = go.GetComponent("NNCamKeypointBinder") != null;
        result.HasHandBinder = go.GetComponent("HandVFXController") != null;
        result.HasAudioBinder = go.TryGetComponent<XRRAI.VFXBinders.VFXAudioDataBinder>(out _);
        result.HasPhysicsBinder = go.TryGetComponent<XRRAI.VFXBinders.VFXPhysicsBinder>(out _);

        // Check visibility status
        if (go.TryGetComponent<VFXRenderer>(out var renderer))
        {
            result.IsVisible = renderer.isVisible;
            result.IsCulled = !result.IsVisible;
            result.BoundsSize = renderer.localBounds.size.magnitude;
        }

        // Audit VFX properties to determine required bindings
        var requiredBindings = DetectRequiredBindings(vfx);

        // Separate specialized bindings (handled by dedicated binders, not VFXARBinder)
        var specializedBindings = new List<string> { "KeypointBuffer" };
        foreach (var sb in specializedBindings)
        {
            if (requiredBindings.Contains(sb))
            {
                result.SpecializedBindingsNeeded.Add(sb);
                requiredBindings.Remove(sb);  // Don't check in VFXARBinder

                // Check if specialized binder exists
                if (sb == "KeypointBuffer" && result.HasNNCamBinder)
                    result.SpecializedBindingsConfigured.Add(sb);
            }
        }

        // Check VFXARBinder configuration
        if (result.HasVFXARBinder && binder != null)
        {
            // Check which bindings are configured
            if (binder.BindDepthMap) result.ConfiguredBindings.Add("DepthMap");
            if (binder.BindStencilMap) result.ConfiguredBindings.Add("StencilMap");
            if (binder.BindPositionMap) result.ConfiguredBindings.Add("PositionMap");
            if (binder.BindColorMap) result.ConfiguredBindings.Add("ColorMap");
            if (binder.BindVelocityMap) result.ConfiguredBindings.Add("VelocityMap");
            if (binder.BindRayParams) result.ConfiguredBindings.Add("RayParams");
            if (binder.BindInverseView) result.ConfiguredBindings.Add("InverseView");
            if (binder.BindDepthRange) result.ConfiguredBindings.Add("DepthRange");
            if (binder.BindThrottle) result.ConfiguredBindings.Add("Throttle");
            if (binder.BindAudio) result.ConfiguredBindings.Add("Audio");
            if (binder.BindAnchorPos) result.ConfiguredBindings.Add("AnchorPos");
            if (binder.BindHologramScale) result.ConfiguredBindings.Add("HologramScale");

            result.BindingsConfigured = result.ConfiguredBindings.Count;

            // Find missing bindings
            foreach (var req in requiredBindings)
            {
                if (!result.ConfiguredBindings.Contains(req))
                    result.MissingBindings.Add(req);
            }
        }

        // Detect optional bindings (not required but could enhance VFX)
        result.OptionalBindings = DetectOptionalBindings(vfx, requiredBindings, result.ConfiguredBindings);

        // Generate issues
        GenerateIssues(result, vfx, requiredBindings);

        // Generate recommendations
        GenerateRecommendations(result, vfx);

        return result;
    }

    /// <summary>
    /// Detect optional bindings that could enhance VFX (not required, but available)
    /// </summary>
    static List<string> DetectOptionalBindings(VisualEffect vfx, List<string> required, List<string> configured)
    {
        var optional = new List<string>();

        // All possible bindings
        var allBindings = new[] {
            "DepthMap", "StencilMap", "PositionMap", "ColorMap", "VelocityMap",
            "RayParams", "InverseView", "DepthRange", "Throttle", "Audio",
            "AnchorPos", "HologramScale"
        };

        foreach (var binding in allBindings)
        {
            // Skip if already required or configured
            if (required.Contains(binding) || configured.Contains(binding))
                continue;

            // Check if VFX could use this binding (has property with alias)
            if (CouldUseBinding(vfx, binding))
                optional.Add(binding);
        }

        // Add enhancement bindings based on VFX type
        string name = vfx.name.ToLowerInvariant();

        // Audio enhancement for any VFX
        if (!required.Contains("Audio") && !configured.Contains("Audio") && !optional.Contains("Audio"))
            optional.Add("Audio");

        // Throttle for intensity control
        if (!required.Contains("Throttle") && !configured.Contains("Throttle") && !optional.Contains("Throttle"))
            optional.Add("Throttle");

        // VelocityMap for motion-based effects
        if (!required.Contains("VelocityMap") && !configured.Contains("VelocityMap") && !optional.Contains("VelocityMap"))
        {
            if (name.Contains("trail") || name.Contains("motion") || name.Contains("flow"))
                optional.Add("VelocityMap");
        }

        return optional;
    }

    /// <summary>
    /// Check if VFX could use a specific binding (has matching property)
    /// </summary>
    static bool CouldUseBinding(VisualEffect vfx, string binding)
    {
        return binding switch
        {
            "DepthMap" => HasAnyTexture(vfx, new[] { "DepthMap", "Depth", "_Depth" }),
            "StencilMap" => HasAnyTexture(vfx, new[] { "StencilMap", "Stencil", "Mask" }),
            "PositionMap" => HasAnyTexture(vfx, new[] { "PositionMap", "Position", "WorldPos" }),
            "ColorMap" => HasAnyTexture(vfx, new[] { "ColorMap", "Color", "MainTex" }),
            "VelocityMap" => HasAnyTexture(vfx, new[] { "VelocityMap", "Velocity", "Motion" }),
            "RayParams" => HasAnyVector4(vfx, new[] { "RayParams", "CameraParams" }),
            "InverseView" => HasAnyMatrix(vfx, new[] { "InverseView", "CameraToWorld" }),
            "DepthRange" => HasAnyVector2(vfx, new[] { "DepthRange", "ClipRange" }),
            "Throttle" => HasAnyFloat(vfx, new[] { "Throttle", "Intensity", "Scale" }),
            "Audio" => vfx.HasFloat("AudioVolume") || vfx.HasVector4("AudioBands"),
            _ => false
        };
    }

    /// <summary>
    /// Detect what bindings this VFX requires based on exposed properties
    /// </summary>
    static List<string> DetectRequiredBindings(VisualEffect vfx)
    {
        var required = new List<string>();

        // Texture bindings
        var depthAliases = new[] { "DepthMap", "Depth", "DepthTexture", "_Depth" };
        var stencilAliases = new[] { "StencilMap", "Stencil", "HumanStencil", "StencilTexture" };
        var positionAliases = new[] { "PositionMap", "Position", "WorldPosition", "WorldPos" };
        var colorAliases = new[] { "ColorMap", "Color", "ColorTexture", "CameraColor", "MainTex" };
        var velocityAliases = new[] { "VelocityMap", "Velocity", "Motion", "MotionVector" };

        if (HasAnyTexture(vfx, depthAliases)) required.Add("DepthMap");
        if (HasAnyTexture(vfx, stencilAliases)) required.Add("StencilMap");
        if (HasAnyTexture(vfx, positionAliases)) required.Add("PositionMap");
        if (HasAnyTexture(vfx, colorAliases)) required.Add("ColorMap");
        if (HasAnyTexture(vfx, velocityAliases)) required.Add("VelocityMap");

        // Vector/Matrix bindings
        var rayAliases = new[] { "RayParams", "RayParameters", "CameraParams" };
        var viewAliases = new[] { "InverseView", "InvView", "CameraToWorld", "ViewInverse" };
        var depthRangeAliases = new[] { "DepthRange", "DepthClip", "NearFar", "ClipRange" };

        if (HasAnyVector4(vfx, rayAliases)) required.Add("RayParams");
        if (HasAnyMatrix(vfx, viewAliases)) required.Add("InverseView");
        if (HasAnyVector2(vfx, depthRangeAliases)) required.Add("DepthRange");

        // Float bindings
        var throttleAliases = new[] { "Throttle", "Intensity", "Scale", "Amount", "Strength" };
        if (HasAnyFloat(vfx, throttleAliases)) required.Add("Throttle");

        // Audio bindings
        if (vfx.HasFloat("AudioVolume") || vfx.HasVector4("AudioBands")) required.Add("Audio");

        // Hologram bindings
        if (vfx.HasVector3("AnchorPos")) required.Add("AnchorPos");
        if (vfx.HasFloat("HologramScale")) required.Add("HologramScale");

        // NNCam keypoint bindings
        if (vfx.HasGraphicsBuffer("KeypointBuffer") || vfx.HasGraphicsBuffer("Keypoints"))
            required.Add("KeypointBuffer");

        return required;
    }

    static bool HasAnyTexture(VisualEffect vfx, string[] names)
    {
        foreach (var n in names)
            if (vfx.HasTexture(n)) return true;
        return false;
    }

    static bool HasAnyVector4(VisualEffect vfx, string[] names)
    {
        foreach (var n in names)
            if (vfx.HasVector4(n)) return true;
        return false;
    }

    static bool HasAnyVector2(VisualEffect vfx, string[] names)
    {
        foreach (var n in names)
            if (vfx.HasVector2(n)) return true;
        return false;
    }

    static bool HasAnyMatrix(VisualEffect vfx, string[] names)
    {
        foreach (var n in names)
            if (vfx.HasMatrix4x4(n)) return true;
        return false;
    }

    static bool HasAnyFloat(VisualEffect vfx, string[] names)
    {
        foreach (var n in names)
            if (vfx.HasFloat(n)) return true;
        return false;
    }

    /// <summary>
    /// Find component by type name using reflection (for conditionally compiled types)
    /// </summary>
    static Component FindComponentByTypeName(string typeName)
    {
        // Search all assemblies for the type
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName) ?? assembly.GetType($"MetavidoVFX.Segmentation.{typeName}");
            if (type != null && typeof(Component).IsAssignableFrom(type))
            {
                return Object.FindFirstObjectByType(type) as Component;
            }
        }
        return null;
    }

    /// <summary>
    /// Generate issues based on audit findings
    /// </summary>
    static void GenerateIssues(VFXPipelineAuditResult result, VisualEffect vfx, List<string> requiredBindings)
    {
        // VFX Asset check
        if (vfx.visualEffectAsset == null)
        {
            result.Issues.Add("VFX has no asset assigned");
            return;
        }

        // VFXARBinder check
        if (requiredBindings.Count > 0 && !result.HasVFXARBinder)
        {
            result.Issues.Add($"Missing VFXARBinder - VFX requires {requiredBindings.Count} bindings: {string.Join(", ", requiredBindings)}");
        }

        // Missing bindings check
        if (result.MissingBindings.Count > 0)
        {
            result.Issues.Add($"VFXARBinder missing {result.MissingBindings.Count} required bindings: {string.Join(", ", result.MissingBindings)}");
        }

        // Culling check (NNCam VFX are often culled if bounds are wrong)
        if (result.IsCulled && vfx.name.Contains("nncam", System.StringComparison.OrdinalIgnoreCase))
        {
            if (!result.HasVFXNoCull)
            {
                result.Issues.Add("NNCam VFX is culled and missing VFXNoCull component");
            }
            else if (result.BoundsSize < 100f)
            {
                result.Issues.Add($"VFX bounds too small ({result.BoundsSize:F0}m) - may be culled");
            }
        }

        // Specialized binder checks
        if (result.SpecializedBindingsNeeded.Contains("KeypointBuffer") && !result.HasNNCamBinder)
        {
            result.Issues.Add("VFX requires KeypointBuffer but missing NNCamKeypointBinder (will be auto-added)");
        }

        if (requiredBindings.Contains("Audio") && !result.HasAudioBinder && result.HasVFXARBinder)
        {
            // Check if audio binding is enabled on VFXARBinder
            var binder = vfx.GetComponent<VFXARBinder>();
            if (binder != null && !binder.BindAudio)
            {
                result.Issues.Add("VFX has audio properties but audio binding is disabled");
            }
        }

        // Hologram wiring check
        if (requiredBindings.Contains("AnchorPos") || requiredBindings.Contains("HologramScale"))
        {
            var binder = vfx.GetComponent<VFXARBinder>();
            if (binder != null && binder.AnchorTransform == null && !binder.UseTransformMode)
            {
                result.Issues.Add("Hologram VFX missing AnchorTransform reference");
            }
        }
    }

    /// <summary>
    /// Generate recommendations based on audit findings
    /// </summary>
    static void GenerateRecommendations(VFXPipelineAuditResult result, VisualEffect vfx)
    {
        // VFXCategory for mode management
        if (!result.HasVFXCategory && result.HasVFXARBinder)
        {
            result.Recommendations.Add("Consider adding VFXCategory for mode-based binding management");
        }

        // VFXNoCull for screen-space VFX
        if (!result.HasVFXNoCull)
        {
            bool isScreenSpace = vfx.name.Contains("nncam", System.StringComparison.OrdinalIgnoreCase) ||
                                 vfx.name.Contains("screen", System.StringComparison.OrdinalIgnoreCase);
            if (isScreenSpace)
            {
                result.Recommendations.Add("Add VFXNoCull component to prevent frustum culling for screen-space VFX");
            }
        }

        // Physics binder for velocity-based VFX
        if (result.ConfiguredBindings.Contains("VelocityMap") && !result.HasPhysicsBinder)
        {
            result.Recommendations.Add("Consider adding VFXPhysicsBinder for physics-based particle behavior");
        }

        // Audio binder for audio-reactive VFX
        if (result.ConfiguredBindings.Contains("Audio") && !result.HasAudioBinder && !result.HasAudioBridgeInScene)
        {
            result.Recommendations.Add("Add AudioBridge to scene for audio-reactive VFX");
        }
    }

    /// <summary>
    /// Fix all issues found in scene audit
    /// </summary>
    public static void FixAllIssues(ScenePipelineAuditResult sceneResult)
    {
        int totalFixes = 0;

        // Fix scene-level issues
        if (!sceneResult.HasARDepthSource)
        {
            CreateARDepthSource();
            sceneResult.HasARDepthSource = true;
            totalFixes++;
        }

        // Fix VFX-level issues
        foreach (var vfxResult in sceneResult.VFXResults)
        {
            FixSingleVFXIssues(vfxResult);
            totalFixes += vfxResult.FixesApplied;
        }

        sceneResult.TotalFixesApplied = totalFixes;

        if (totalFixes > 0)
        {
            EditorUtility.DisplayDialog("VFX Pipeline Audit",
                $"Fixed {totalFixes} issues across the pipeline.\n\nSee Console for details.",
                "OK");
        }
    }

    /// <summary>
    /// Fix issues for a single VFX (mode-aware per source-bindings.md)
    /// </summary>
    public static void FixSingleVFXIssues(VFXPipelineAuditResult result)
    {
        if (result.VFX == null) return;

        var go = result.VFX.gameObject;
        Undo.RecordObject(go, "VFX Pipeline Audit Fix");

        int fixes = 0;

        // Detect binding mode from VFX properties (per source-bindings.md)
        var mode = DetectBindingMode(result.VFX);

        // For Audio/Standalone modes, only add VFXARBinder if AR pipeline is explicitly needed
        bool needsARBinder = mode == VFXBindingMode.AR ||
                             mode == VFXBindingMode.Keypoint;

        // Add VFXARBinder if needed (for AR/Keypoint modes or if VFX has bindable properties)
        if (!result.HasVFXARBinder && (needsARBinder || result.MissingBindings.Count > 0))
        {
            var binder = Undo.AddComponent<VFXARBinder>(go);
            // Enable all detected bindings
            foreach (var missing in result.MissingBindings)
            {
                EnableBinding(binder, missing);
            }
            result.HasVFXARBinder = true;
            fixes += result.MissingBindings.Count + 1;
            Debug.Log($"[VFXPipelineAuditor] Added VFXARBinder to {go.name} with {result.MissingBindings.Count} bindings (mode: {mode})");
        }
        // Enable missing bindings on existing binder
        else if (result.HasVFXARBinder && result.MissingBindings.Count > 0)
        {
            var binder = go.GetComponent<VFXARBinder>();
            if (binder != null)
            {
                Undo.RecordObject(binder, "Enable missing VFX bindings");
                foreach (var missing in result.MissingBindings)
                {
                    EnableBinding(binder, missing);
                }
                fixes += result.MissingBindings.Count;
                Debug.Log($"[VFXPipelineAuditor] Enabled {result.MissingBindings.Count} missing bindings on {go.name}: {string.Join(", ", result.MissingBindings)}");
            }
        }

        // Add VFXNoCull for NNCam VFX
        if (!result.HasVFXNoCull)
        {
            bool isNNCam = result.VFX.name.Contains("nncam", System.StringComparison.OrdinalIgnoreCase);
            if (isNNCam || result.IsCulled)
            {
                var noCull = Undo.AddComponent<VFXNoCull>(go);
                result.HasVFXNoCull = true;
                fixes++;
                Debug.Log($"[VFXPipelineAuditor] Added VFXNoCull to {go.name}");
            }
        }

        // Add VFXCategory if using VFXARBinder
        if (!result.HasVFXCategory && result.HasVFXARBinder)
        {
            var category = Undo.AddComponent<VFXCategory>(go);
            category.SetCategory(DetectCategoryFromName(result.VFX.name));
            category.SetBindingMode(mode);
            result.HasVFXCategory = true;
            fixes++;
            Debug.Log($"[VFXPipelineAuditor] Added VFXCategory ({category.Category}, mode: {mode}) to {go.name}");
        }

        // Add specialized binders based on requirements
        fixes += AddSpecializedBinders(result, go, mode);

        // Apply custom bindings from custom-bindings.md (user overrides)
        fixes += ApplyCustomBindings(result);

        // Mark dirty
        if (fixes > 0)
        {
            EditorUtility.SetDirty(go);
        }

        result.FixesApplied = fixes;
    }

    /// <summary>
    /// Add specialized binders based on VFX requirements
    /// </summary>
    static int AddSpecializedBinders(VFXPipelineAuditResult result, GameObject go, VFXBindingMode mode)
    {
        int fixes = 0;
        var vfx = result.VFX;

        // NNCamKeypointBinder for keypoint VFX (KeypointBuffer requires this specialized binder)
        if (!result.HasNNCamBinder && result.SpecializedBindingsNeeded.Contains("KeypointBuffer"))
        {
            var binderType = FindType("NNCamKeypointBinder");
            if (binderType != null)
            {
                Undo.AddComponent(go, binderType);
                result.HasNNCamBinder = true;
                result.SpecializedBindingsConfigured.Add("KeypointBuffer");
                fixes++;
                Debug.Log($"[VFXPipelineAuditor] Added NNCamKeypointBinder to {go.name} (KeypointBuffer binding)");
            }
            else
            {
                Debug.LogWarning($"[VFXPipelineAuditor] Could not find NNCamKeypointBinder type. Ensure BODYPIX_AVAILABLE is defined.");
            }
        }

        // VFXAudioDataBinder for audio-reactive VFX
        if ((mode == VFXBindingMode.Audio || result.ConfiguredBindings.Contains("Audio"))
            && !result.HasAudioBinder)
        {
            // Ensure AudioBridge exists in scene (required for global audio properties)
            EnsureAudioBridgeExists();

            var audioBinderType = FindType("XRRAI.VFXBinders.VFXAudioDataBinder");
            if (audioBinderType != null)
            {
                Undo.AddComponent(go, audioBinderType);
                result.HasAudioBinder = true;
                fixes++;

                var existingAudioProps = GetExistingAudioProperties(vfx);
                bool hasTextureProperty = vfx.HasTexture("AudioDataTexture");

                if (existingAudioProps.Count > 0)
                {
                    // VFX has float audio properties - report what was found
                    Debug.Log($"[VFXPipelineAuditor] Added VFXAudioDataBinder to {go.name}\n" +
                        $"  Binding mode: Float properties ({string.Join(", ", existingAudioProps)})");
                }
                else if (hasTextureProperty)
                {
                    // VFX has AudioDataTexture - will use texture binding
                    Debug.Log($"[VFXPipelineAuditor] Added VFXAudioDataBinder to {go.name}\n" +
                        $"  Binding mode: AudioDataTexture (automatic 2x2 texture binding)");
                }
                else
                {
                    // No exposed properties - audio available via global shader properties
                    Debug.Log($"[VFXPipelineAuditor] Added VFXAudioDataBinder to {go.name}\n" +
                        $"  Binding mode: Global shader properties (fully automatic)\n" +
                        $"  VFX can access audio via Custom HLSL: #include \"Assets/Shaders/ARGlobals.hlsl\"\n" +
                        $"  Functions: GetAudioVolume(), GetAudioBass(), GetBeatPulse(), etc.");
                }
            }
        }

        // VFXPhysicsBinder for velocity/physics VFX
        if (result.ConfiguredBindings.Contains("VelocityMap") && !result.HasPhysicsBinder)
        {
            var physicsBinderType = FindType("XRRAI.VFXBinders.VFXPhysicsBinder");
            if (physicsBinderType != null)
            {
                Undo.AddComponent(go, physicsBinderType);
                result.HasPhysicsBinder = true;
                fixes++;
                Debug.Log($"[VFXPipelineAuditor] Added VFXPhysicsBinder to {go.name}");
            }
        }

        // HandVFXController for hand tracking VFX
        if (!result.HasHandBinder)
        {
            string name = vfx.name.ToLowerInvariant();
            if (name.Contains("hand") || name.Contains("pinch") || name.Contains("brush"))
            {
                var handType = FindType("HandVFXController");
                if (handType != null)
                {
                    Undo.AddComponent(go, handType);
                    result.HasHandBinder = true;
                    fixes++;
                    Debug.Log($"[VFXPipelineAuditor] Added HandVFXController to {go.name}");
                }
            }
        }

        // FluoCanvas for Fluo audio VFX (if not already in scene)
        if (mode == VFXBindingMode.Audio)
        {
            string name = vfx.name.ToLowerInvariant();
            if (name.Contains("fluo") || name.Contains("bubble") || name.Contains("stream"))
            {
                var fluoCanvasType = FindType("MetavidoVFX.Fluo.FluoCanvas");
                if (fluoCanvasType != null && Object.FindFirstObjectByType(fluoCanvasType) == null)
                {
                    var fluoGO = new GameObject("FluoCanvas");
                    Undo.RegisterCreatedObjectUndo(fluoGO, "Create FluoCanvas");
                    var canvas = fluoGO.AddComponent(fluoCanvasType);
                    // Add VFX to canvas target list via reflection
                    var addMethod = fluoCanvasType.GetMethod("AddVFX");
                    addMethod?.Invoke(canvas, new object[] { vfx });
                    fixes++;
                    Debug.Log($"[VFXPipelineAuditor] Created FluoCanvas and linked to {go.name}");
                }
            }
        }

        return fixes;
    }

    /// <summary>
    /// Ensures an AudioBridge exists in the scene for global audio shader properties.
    /// Called automatically when adding VFXAudioDataBinder to enable fully automatic audio binding.
    /// </summary>
    static void EnsureAudioBridgeExists()
    {
        var existingBridge = Object.FindFirstObjectByType<AudioBridge>();
        if (existingBridge != null) return;

        var bridgeGO = new GameObject("AudioBridge");
        Undo.RegisterCreatedObjectUndo(bridgeGO, "Create AudioBridge");
        bridgeGO.AddComponent<AudioBridge>();

        // Check if any AudioSource exists in scene
        var existingSource = Object.FindFirstObjectByType<AudioSource>();
        if (existingSource == null)
            bridgeGO.AddComponent<AudioSource>();

        Debug.Log("[VFXPipelineAuditor] Created AudioBridge - global audio properties now available for all VFX");
    }

    /// <summary>
    /// Find type by name across all assemblies (portable - no namespace assumptions)
    /// Searches by full name first, then by simple name across all types
    /// </summary>
    static System.Type FindType(string typeName)
    {
        // Try as full name first (if it includes namespace)
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            catch { /* ignore assembly access issues */ }
        }

        // Extract simple name (in case typeName includes namespace)
        string simpleName = typeName;
        int lastDot = typeName.LastIndexOf('.');
        if (lastDot >= 0)
            simpleName = typeName.Substring(lastDot + 1);

        // Search all types by simple name (portable across any namespace)
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == simpleName && typeof(Component).IsAssignableFrom(type))
                        return type;
                }
            }
            catch { /* ignore assemblies that can't be reflected */ }
        }

        return null;
    }

    /// <summary>
    /// Standard audio properties that VFXAudioDataBinder can bind (with aliases)
    /// </summary>
    static readonly string[] AudioPropertyNames = new[]
    {
        "AudioVolume", "AudioBass", "AudioMid", "AudioTreble", "AudioSubBass",
        "AudioPitch", "BeatPulse", "BeatIntensity"
    };

    /// <summary>
    /// Alternative audio property names used by different VFX (Fluo, Echovision, etc.)
    /// Maps canonical name → aliases
    /// </summary>
    static readonly Dictionary<string, string[]> AudioPropertyAliases = new()
    {
        { "AudioVolume", new[] { "AudioLevel", "Volume", "SoundVolume", "_AudioVolume" } },
        { "AudioBass", new[] { "AudioLowLevel", "Bass", "LowFreq", "SubBass" } },
        { "AudioMid", new[] { "AudioMidLevel", "Mids", "MidFreq" } },
        { "AudioTreble", new[] { "AudioHighLevel", "Treble", "HighFreq", "Highs" } },
        { "BeatPulse", new[] { "Beat", "Pulse", "OnBeat" } },
        { "BeatIntensity", new[] { "BeatStrength", "Intensity" } }
    };

    /// <summary>
    /// Texture-based audio properties (waveforms)
    /// </summary>
    static readonly string[] AudioTextureProperties = new[]
    {
        "WaveformTexture", "LeftWaveform", "RightWaveform", "SpectrumTexture"
    };

    /// <summary>
    /// Get list of audio properties missing from VFX Graph
    /// Returns empty if VFX has any audio properties (even aliased ones)
    /// </summary>
    static List<string> GetMissingAudioProperties(VisualEffect vfx)
    {
        var missing = new List<string>();
        if (vfx == null || vfx.visualEffectAsset == null) return missing;

        // First check if VFX has ANY audio property (including aliases)
        var existingProps = GetExistingAudioProperties(vfx);
        if (existingProps.Count > 0)
        {
            // VFX already has audio properties - just suggest any core ones missing
            string[] coreProps = { "AudioVolume", "AudioBass", "AudioMid", "AudioTreble" };
            foreach (var prop in coreProps)
            {
                if (!HasAudioPropertyOrAlias(vfx, prop) && !existingProps.Contains(prop))
                    missing.Add(prop);
            }
            return missing;
        }

        // No audio properties at all - suggest core set
        return new List<string> { "AudioVolume", "AudioBass", "AudioMid", "AudioTreble" };
    }

    /// <summary>
    /// Get all existing audio properties on VFX (canonical + aliased)
    /// </summary>
    static List<string> GetExistingAudioProperties(VisualEffect vfx)
    {
        var existing = new List<string>();
        if (vfx == null || vfx.visualEffectAsset == null) return existing;

        // Check standard names
        foreach (var prop in AudioPropertyNames)
        {
            if (vfx.HasFloat(prop))
                existing.Add(prop);
        }

        // Check aliases
        foreach (var kvp in AudioPropertyAliases)
        {
            foreach (var alias in kvp.Value)
            {
                if (vfx.HasFloat(alias) && !existing.Contains(alias))
                    existing.Add($"{alias} (={kvp.Key})");
            }
        }

        // Check texture-based audio
        foreach (var prop in AudioTextureProperties)
        {
            if (vfx.HasTexture(prop))
                existing.Add($"{prop} (texture)");
        }

        return existing;
    }

    /// <summary>
    /// Check if VFX has a property or any of its aliases
    /// </summary>
    static bool HasAudioPropertyOrAlias(VisualEffect vfx, string canonicalName)
    {
        if (vfx.HasFloat(canonicalName)) return true;

        if (AudioPropertyAliases.TryGetValue(canonicalName, out var aliases))
        {
            foreach (var alias in aliases)
            {
                if (vfx.HasFloat(alias)) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if VFX has any audio properties (float or texture)
    /// </summary>
    static bool HasAnyAudioProperty(VisualEffect vfx)
    {
        if (vfx == null || vfx.visualEffectAsset == null) return false;

        // Check standard float properties
        foreach (var prop in AudioPropertyNames)
        {
            if (vfx.HasFloat(prop)) return true;
        }

        // Check aliases
        foreach (var kvp in AudioPropertyAliases)
        {
            foreach (var alias in kvp.Value)
            {
                if (vfx.HasFloat(alias)) return true;
            }
        }

        // Check texture-based audio
        foreach (var prop in AudioTextureProperties)
        {
            if (vfx.HasTexture(prop)) return true;
        }

        return false;
    }

    /// <summary>
    /// Show audio properties template dialog
    /// </summary>
    [MenuItem("H3M/VFX Pipeline Master/Audio/Show Audio Properties Template")]
    public static void ShowAudioPropertiesTemplate()
    {
        string template = @"=== VFX Graph Audio Properties Template ===

Add these Float properties to your VFX Graph's Blackboard panel:

CORE PROPERTIES (recommended):
  • AudioVolume    (float, 0-1)  - Overall audio level
  • AudioBass      (float, 0-1)  - Low frequency band (20-250Hz)
  • AudioMid       (float, 0-1)  - Mid frequency band (250-4000Hz)
  • AudioTreble    (float, 0-1)  - High frequency band (4000-20000Hz)

OPTIONAL PROPERTIES:
  • AudioSubBass   (float, 0-1)  - Sub-bass (20-60Hz)
  • AudioPitch     (float, Hz)   - Detected pitch (requires EnhancedAudioProcessor)
  • BeatPulse      (float, 0-1)  - Beat detection pulse (decays after beat)
  • BeatIntensity  (float, 0-1)  - Beat strength

USAGE IN VFX GRAPH:
  1. Open Blackboard panel (Window > VFX > Blackboard)
  2. Click '+' and select 'Float'
  3. Name it exactly as shown above
  4. Check 'Exposed' checkbox
  5. Connect to your VFX systems (e.g., spawn rate, size, color)

EXAMPLE USAGE:
  • Spawn Rate = BaseRate * (1 + AudioBass * 2)
  • Particle Size = BaseSize * (0.5 + AudioVolume)
  • Color intensity = lerp(dim, bright, AudioTreble)
";

        EditorUtility.DisplayDialog("VFX Audio Properties Template", template, "OK");
        Debug.Log(template);
    }

    /// <summary>
    /// Add audio properties to selected VFX (logs instructions since programmatic add isn't supported)
    /// </summary>
    [MenuItem("H3M/VFX Pipeline Master/Audio/Check Audio Properties on Selected")]
    public static void CheckAudioPropertiesOnSelected()
    {
        var selected = Selection.activeGameObject;
        if (selected == null || !selected.TryGetComponent<VisualEffect>(out var vfx))
        {
            Debug.LogWarning("[VFXPipelineAuditor] Select a GameObject with VisualEffect component.");
            return;
        }

        var missing = GetMissingAudioProperties(vfx);
        var present = new List<string>();

        foreach (var prop in AudioPropertyNames)
        {
            if (vfx.HasFloat(prop))
                present.Add(prop);
        }

        var sb = new StringBuilder();
        sb.AppendLine($"=== Audio Properties Check: {vfx.name} ===");
        sb.AppendLine();

        if (present.Count > 0)
        {
            sb.AppendLine($"✓ Present ({present.Count}): {string.Join(", ", present)}");
        }

        if (missing.Count > 0)
        {
            sb.AppendLine($"✗ Missing ({missing.Count}): {string.Join(", ", missing)}");
            sb.AppendLine();
            sb.AppendLine("To add missing properties:");
            sb.AppendLine("  1. Double-click the VFX asset to open VFX Graph Editor");
            sb.AppendLine("  2. Open Blackboard panel (View > Blackboard)");
            sb.AppendLine("  3. Click '+' > Float for each missing property");
            sb.AppendLine("  4. Name exactly as shown above and check 'Exposed'");
        }
        else if (present.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("✓ All core audio properties present!");
        }
        else
        {
            sb.AppendLine("No audio properties found. This VFX may not be audio-reactive.");
        }

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Detect VFX category from name
    /// </summary>
    static VFXCategoryType DetectCategoryFromName(string name)
    {
        name = name.ToLowerInvariant();

        if (name.Contains("hand") || name.Contains("pinch"))
            return VFXCategoryType.Hands;
        if (name.Contains("face") || name.Contains("head"))
            return VFXCategoryType.Face;
        if (name.Contains("audio") || name.Contains("sound") || name.Contains("music"))
            return VFXCategoryType.Audio;
        if (name.Contains("env") || name.Contains("world") || name.Contains("grid"))
            return VFXCategoryType.Environment;
        if (name.Contains("body") || name.Contains("human") || name.Contains("people") || name.Contains("nncam"))
            return VFXCategoryType.People;

        return VFXCategoryType.Hybrid;
    }

    /// <summary>
    /// Detect binding mode from VFX properties (per source-bindings.md)
    /// </summary>
    static VFXBindingMode DetectBindingMode(VisualEffect vfx)
    {
        if (vfx == null || vfx.visualEffectAsset == null)
            return VFXBindingMode.Standalone;

        // Check for keypoint buffer (NNCam)
        if (vfx.HasGraphicsBuffer("KeypointBuffer"))
            return VFXBindingMode.Keypoint;

        // Check for AR depth (Rcam/Akvfx)
        if (vfx.HasTexture("DepthMap") || vfx.HasTexture("StencilMap") || vfx.HasTexture("PositionMap"))
            return VFXBindingMode.AR;

        // Check for audio-only (Fluo)
        string name = vfx.name.ToLowerInvariant();
        bool isFluo = name.Contains("fluo") || name.Contains("bubble") || name.Contains("sparkle");
        if ((vfx.HasFloat("Throttle") || vfx.HasFloat("AudioLevel") || isFluo) && !vfx.HasTexture("DepthMap"))
            return VFXBindingMode.Audio;

        return VFXBindingMode.Standalone;
    }

    /// <summary>
    /// Enable a specific binding on VFXARBinder
    /// </summary>
    static void EnableBinding(VFXARBinder binder, string bindingName)
    {
        switch (bindingName)
        {
            case "DepthMap": binder.BindDepthMap = true; break;
            case "StencilMap": binder.BindStencilMap = true; break;
            case "PositionMap": binder.BindPositionMap = true; break;
            case "ColorMap": binder.BindColorMap = true; break;
            case "VelocityMap": binder.BindVelocityMap = true; break;
            case "RayParams": binder.BindRayParams = true; break;
            case "InverseView": binder.BindInverseView = true; break;
            case "DepthRange": binder.BindDepthRange = true; break;
            case "Throttle": binder.BindThrottle = true; break;
            case "Audio": binder.BindAudio = true; break;
            case "AnchorPos": binder.BindAnchorPos = true; break;
            case "HologramScale": binder.BindHologramScale = true; break;
        }
    }

    /// <summary>
    /// Create ARDepthSource if missing
    /// </summary>
    static void CreateARDepthSource()
    {
        var go = new GameObject("ARDepthSource");
        Undo.RegisterCreatedObjectUndo(go, "Create ARDepthSource");
        go.AddComponent<ARDepthSource>();
        Debug.Log("[VFXPipelineAuditor] Created ARDepthSource in scene");
    }

    /// <summary>
    /// Log audit results to console
    /// </summary>
    static void LogAuditResults(ScenePipelineAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                    VFX PIPELINE AUDIT REPORT                   ");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        // Scene Infrastructure
        sb.AppendLine("▶ SCENE INFRASTRUCTURE");
        sb.AppendLine($"  ARDepthSource:      {(result.HasARDepthSource ? "✓" : "✗ MISSING")}");
        sb.AppendLine($"  AudioBridge:        {(result.HasAudioBridge ? "✓" : "○ Not found")}");
        sb.AppendLine($"  BodyPartSegmenter:  {(result.HasBodySegmenter ? "✓" : "○ Not found")}");
        sb.AppendLine($"  ARCameraManager:    {(result.HasARCameraManager ? "✓" : "✗ MISSING")}");
        sb.AppendLine($"  AROcclusionManager: {(result.HasAROcclusionManager ? "✓" : "✗ MISSING")}");
        sb.AppendLine();

        if (result.SceneIssues.Count > 0)
        {
            sb.AppendLine("▶ SCENE ISSUES");
            foreach (var issue in result.SceneIssues)
                sb.AppendLine($"  ⚠ {issue}");
            sb.AppendLine();
        }

        // VFX Summary
        sb.AppendLine("▶ VFX SUMMARY");
        sb.AppendLine($"  Total VFX:      {result.TotalVFX}");
        sb.AppendLine($"  Healthy:        {result.HealthyVFX} ({(result.TotalVFX > 0 ? (100f * result.HealthyVFX / result.TotalVFX) : 0):F0}%)");
        sb.AppendLine($"  With Issues:    {result.VFXWithIssues}");
        sb.AppendLine();

        // VFX Details
        if (result.VFXResults.Count > 0)
        {
            sb.AppendLine("▶ VFX DETAILS");
            foreach (var vfx in result.VFXResults)
            {
                string status = vfx.IsHealthy ? "✓" : "⚠";
                sb.AppendLine($"  {status} {vfx.VFXName} ({vfx.AssetName})");
                sb.AppendLine($"      Bindings: {vfx.BindingsConfigured} configured");
                if (vfx.ConfiguredBindings.Count > 0)
                    sb.AppendLine($"      Configured: {string.Join(", ", vfx.ConfiguredBindings)}");

                if (vfx.Issues.Count > 0)
                {
                    sb.AppendLine("      Issues:");
                    foreach (var issue in vfx.Issues)
                        sb.AppendLine($"        ⚠ {issue}");
                }

                if (vfx.Recommendations.Count > 0)
                {
                    sb.AppendLine("      Recommendations:");
                    foreach (var rec in vfx.Recommendations)
                        sb.AppendLine($"        → {rec}");
                }

                if (vfx.FixesApplied > 0)
                    sb.AppendLine($"      ✓ {vfx.FixesApplied} fixes applied");
            }
        }

        if (result.TotalFixesApplied > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"═══ TOTAL FIXES APPLIED: {result.TotalFixesApplied} ═══");
        }

        sb.AppendLine("═══════════════════════════════════════════════════════════════");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Log single VFX result
    /// </summary>
    static void LogSingleVFXResult(VFXPipelineAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"═══ VFX AUDIT: {result.VFXName} ═══");
        sb.AppendLine($"Asset: {result.AssetName}");
        sb.AppendLine();

        sb.AppendLine("Components:");
        sb.AppendLine($"  VFXARBinder:        {(result.HasVFXARBinder ? "✓" : "✗")}");
        sb.AppendLine($"  VFXCategory:        {(result.HasVFXCategory ? "✓" : "○")}");
        sb.AppendLine($"  VFXNoCull:          {(result.HasVFXNoCull ? "✓" : "○")}");
        sb.AppendLine($"  NNCamKeypointBinder:{(result.HasNNCamBinder ? "✓" : "○")}");
        sb.AppendLine($"  VFXAudioDataBinder: {(result.HasAudioBinder ? "✓" : "○")}");
        sb.AppendLine();

        sb.AppendLine($"Bindings: {result.BindingsConfigured} configured");
        if (result.ConfiguredBindings.Count > 0)
            sb.AppendLine($"  Configured: {string.Join(", ", result.ConfiguredBindings)}");
        if (result.MissingBindings.Count > 0)
            sb.AppendLine($"  Missing: {string.Join(", ", result.MissingBindings)}");

        // Specialized bindings (e.g., KeypointBuffer via NNCamKeypointBinder)
        if (result.SpecializedBindingsNeeded.Count > 0)
        {
            sb.AppendLine($"  Specialized Needed: {string.Join(", ", result.SpecializedBindingsNeeded)}");
            if (result.SpecializedBindingsConfigured.Count > 0)
                sb.AppendLine($"  Specialized Configured: {string.Join(", ", result.SpecializedBindingsConfigured)}");
        }
        sb.AppendLine();

        if (result.Issues.Count > 0)
        {
            sb.AppendLine("Issues:");
            foreach (var issue in result.Issues)
                sb.AppendLine($"  ⚠ {issue}");
        }
        else
        {
            sb.AppendLine("✓ No issues found");
        }

        if (result.Recommendations.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Recommendations:");
            foreach (var rec in result.Recommendations)
                sb.AppendLine($"  → {rec}");
        }

        if (result.FixesApplied > 0)
            sb.AppendLine($"\n✓ {result.FixesApplied} fixes applied");

        Debug.Log(sb.ToString());
    }

    #region Custom Bindings Parser

    /// <summary>
    /// Custom binding configuration from custom-bindings.md
    /// </summary>
    public class CustomBindingConfig
    {
        public string VFXName;
        public string Mode;  // AR, Audio, Keypoint, Standalone
        public Dictionary<string, bool> Bindings = new();
        public Dictionary<string, string> CustomValues = new();
    }

    static Dictionary<string, CustomBindingConfig> _customBindingsCache;
    static string _customBindingsPath = "Assets/Documentation/custom-bindings.md";

    /// <summary>
    /// Load custom bindings from custom-bindings.md
    /// </summary>
    public static Dictionary<string, CustomBindingConfig> LoadCustomBindings()
    {
        if (_customBindingsCache != null) return _customBindingsCache;

        _customBindingsCache = new Dictionary<string, CustomBindingConfig>();

        if (!System.IO.File.Exists(_customBindingsPath))
        {
            Debug.Log("[VFXPipelineAuditor] No custom-bindings.md found, using defaults.");
            return _customBindingsCache;
        }

        try
        {
            string content = System.IO.File.ReadAllText(_customBindingsPath);
            ParseCustomBindings(content, _customBindingsCache);
            Debug.Log($"[VFXPipelineAuditor] Loaded {_customBindingsCache.Count} custom binding configs.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[VFXPipelineAuditor] Failed to parse custom-bindings.md: {e.Message}");
        }

        return _customBindingsCache;
    }

    /// <summary>
    /// Parse YAML-like custom bindings format
    /// </summary>
    static void ParseCustomBindings(string content, Dictionary<string, CustomBindingConfig> configs)
    {
        // Simple line-by-line parser for the YAML-like format
        // Handles: VFXName:, mode:, bindings:, PropertyName: true/false, custom:

        string[] lines = content.Split('\n');
        CustomBindingConfig currentConfig = null;
        string currentSection = null; // "bindings" or "custom"
        bool inYamlBlock = false;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            // Track YAML blocks
            if (line == "```yaml")
            {
                inYamlBlock = true;
                continue;
            }
            if (line == "```")
            {
                inYamlBlock = false;
                continue;
            }

            if (!inYamlBlock) continue;

            // Check for VFX name (no indentation, ends with :)
            if (!line.StartsWith(" ") && line.EndsWith(":") && !line.Contains(" "))
            {
                string vfxName = line.TrimEnd(':');
                currentConfig = new CustomBindingConfig { VFXName = vfxName };
                configs[vfxName] = currentConfig;
                currentSection = null;
                continue;
            }

            if (currentConfig == null) continue;

            // Check for section headers (bindings:, custom:)
            string trimmedLine = line.TrimStart();
            if (trimmedLine == "bindings:")
            {
                currentSection = "bindings";
                continue;
            }
            if (trimmedLine == "custom:")
            {
                currentSection = "custom";
                continue;
            }

            // Check for mode:
            if (trimmedLine.StartsWith("mode:"))
            {
                currentConfig.Mode = trimmedLine.Substring(5).Trim();
                continue;
            }

            // Parse key: value
            int colonIdx = trimmedLine.IndexOf(':');
            if (colonIdx > 0)
            {
                string key = trimmedLine.Substring(0, colonIdx).Trim();
                string value = trimmedLine.Substring(colonIdx + 1).Trim();

                if (currentSection == "bindings")
                {
                    bool boolValue = value.ToLower() == "true";
                    currentConfig.Bindings[key] = boolValue;
                }
                else if (currentSection == "custom")
                {
                    currentConfig.CustomValues[key] = value;
                }
            }
        }
    }

    /// <summary>
    /// Apply custom bindings to a VFX during audit & fix
    /// </summary>
    public static int ApplyCustomBindings(VFXPipelineAuditResult result)
    {
        var configs = LoadCustomBindings();
        if (configs.Count == 0) return 0;

        var vfx = result.VFX;
        if (vfx == null) return 0;

        // Try to find config by VFX name or asset name
        string vfxName = vfx.name;
        string assetName = vfx.visualEffectAsset?.name ?? "";

        CustomBindingConfig config = null;
        if (configs.TryGetValue(vfxName, out config) || configs.TryGetValue(assetName, out config))
        {
            return ApplyConfig(result, config);
        }

        return 0;
    }

    static int ApplyConfig(VFXPipelineAuditResult result, CustomBindingConfig config)
    {
        int fixes = 0;
        var go = result.VFX.gameObject;

        // Apply mode override
        if (!string.IsNullOrEmpty(config.Mode))
        {
            var category = go.GetComponent<VFXCategory>();
            if (category == null)
            {
                category = Undo.AddComponent<VFXCategory>(go);
                fixes++;
            }

            VFXBindingMode mode = config.Mode.ToLower() switch
            {
                "ar" => VFXBindingMode.AR,
                "audio" => VFXBindingMode.Audio,
                "keypoint" => VFXBindingMode.Keypoint,
                "standalone" => VFXBindingMode.Standalone,
                _ => VFXBindingMode.AR
            };

            Undo.RecordObject(category, "Apply custom binding mode");
            category.SetBindingMode(mode);
            fixes++;
            Debug.Log($"[VFXPipelineAuditor] Applied custom mode '{config.Mode}' to {go.name}");
        }

        // Apply binding overrides
        if (config.Bindings.Count > 0)
        {
            var binder = go.GetComponent<VFXARBinder>();
            if (binder == null)
            {
                binder = Undo.AddComponent<VFXARBinder>(go);
                fixes++;
            }

            Undo.RecordObject(binder, "Apply custom bindings");
            foreach (var kvp in config.Bindings)
            {
                switch (kvp.Key)
                {
                    case "DepthMap": binder.BindDepthMap = kvp.Value; break;
                    case "StencilMap": binder.BindStencilMap = kvp.Value; break;
                    case "PositionMap": binder.BindPositionMap = kvp.Value; break;
                    case "ColorMap": binder.BindColorMap = kvp.Value; break;
                    case "VelocityMap": binder.BindVelocityMap = kvp.Value; break;
                    case "RayParams": binder.BindRayParams = kvp.Value; break;
                    case "InverseView": binder.BindInverseView = kvp.Value; break;
                    case "DepthRange": binder.BindDepthRange = kvp.Value; break;
                    case "Throttle": binder.BindThrottle = kvp.Value; break;
                    case "Audio": binder.BindAudio = kvp.Value; break;
                    case "AnchorPos": binder.BindAnchorPos = kvp.Value; break;
                    case "HologramScale": binder.BindHologramScale = kvp.Value; break;
                }
                fixes++;
            }
            Debug.Log($"[VFXPipelineAuditor] Applied {config.Bindings.Count} custom bindings to {go.name}");
        }

        // Apply custom values (Throttle, etc.)
        if (config.CustomValues.Count > 0)
        {
            var binder = go.GetComponent<VFXARBinder>();
            if (binder != null)
            {
                foreach (var kvp in config.CustomValues)
                {
                    if (kvp.Key == "Throttle" && float.TryParse(kvp.Value, out float throttle))
                    {
                        binder.Throttle = throttle;
                        fixes++;
                    }
                }
            }
            Debug.Log($"[VFXPipelineAuditor] Applied {config.CustomValues.Count} custom values to {go.name}");
        }

        return fixes;
    }

    /// <summary>
    /// Clear custom bindings cache (call after editing custom-bindings.md)
    /// </summary>
    [MenuItem("H3M/VFX Pipeline Master/Audit & Fix/Reload Custom Bindings")]
    public static void ReloadCustomBindings()
    {
        _customBindingsCache = null;
        LoadCustomBindings();
    }

    #endregion
}
#endif
