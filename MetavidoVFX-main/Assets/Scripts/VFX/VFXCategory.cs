// VFX Category System - Groups VFX by type for organized switching
// Mode-based binding management per source-bindings.md

using UnityEngine;
using UnityEngine.VFX;

namespace XRRAI.VFXBinders
{
    /// <summary>
    /// Binding mode determines which data sources are used (from source-bindings.md)
    /// </summary>
    public enum VFXBindingMode
    {
        AR,         // Full AR depth/color pipeline (DepthMap, StencilMap, ColorMap, RayParams)
        Audio,      // Audio-reactive only (Throttle, global _AudioBands)
        Keypoint,   // ML body keypoints (KeypointBuffer, optional ColorMap)
        Standalone  // No external data required
    }

    /// <summary>
    /// VFX category types for organized switching
    /// </summary>
    public enum VFXCategoryType
    {
        People,      // Body/person effects (depth-based, stencil-masked)
        Face,        // Face tracking effects
        Hands,       // Hand tracking effects
        Environment, // World/environment effects
        Audio,       // Audio-reactive effects
        Hybrid       // Multiple input sources
    }

    /// <summary>
    /// VFX data binding requirements
    /// </summary>
    [System.Flags]
    public enum VFXBindingRequirements
    {
        None = 0,
        DepthMap = 1,
        ColorMap = 2,
        StencilMap = 4,
        HandTracking = 8,
        FaceTracking = 16,
        Audio = 32,
        ARMesh = 64,
        Keypoints = 128,
        All = DepthMap | ColorMap | StencilMap | HandTracking | FaceTracking | Audio | ARMesh | Keypoints
    }

    /// <summary>
    /// Attach to VFX to define its category and binding requirements
    /// </summary>
    [RequireComponent(typeof(VisualEffect))]
    public class VFXCategory : MonoBehaviour
    {
        [Header("Binding Mode (source-bindings.md)")]
        [SerializeField] private VFXBindingMode bindingMode = VFXBindingMode.AR;

        [Header("Category")]
        [SerializeField] private VFXCategoryType category = VFXCategoryType.People;
        [SerializeField] private VFXBindingRequirements bindings = VFXBindingRequirements.DepthMap | VFXBindingRequirements.ColorMap;

        [Header("Metadata")]
        [SerializeField] private string displayName;
        [SerializeField] private Sprite thumbnail;
        [SerializeField, TextArea] private string description;

        [Header("Performance")]
        [SerializeField, Range(1, 5)] private int performanceTier = 3; // 1=Light, 5=Heavy
        [SerializeField] private bool mobileOptimized = true;

        public VFXBindingMode BindingMode => bindingMode;
        public VFXCategoryType Category => category;
        public VFXBindingRequirements Bindings => bindings;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;
        public Sprite Thumbnail => thumbnail;
        public string Description => description;
        public int PerformanceTier => performanceTier;
        public bool MobileOptimized => mobileOptimized;

        /// <summary>
        /// Set binding mode (used by auditor)
        /// </summary>
        public void SetBindingMode(VFXBindingMode mode)
        {
            bindingMode = mode;
            // Auto-set bindings based on mode (per source-bindings.md)
            bindings = mode switch
            {
                VFXBindingMode.AR => VFXBindingRequirements.DepthMap | VFXBindingRequirements.ColorMap | VFXBindingRequirements.StencilMap,
                VFXBindingMode.Audio => VFXBindingRequirements.Audio,
                VFXBindingMode.Keypoint => VFXBindingRequirements.Keypoints | VFXBindingRequirements.ColorMap,
                VFXBindingMode.Standalone => VFXBindingRequirements.None,
                _ => VFXBindingRequirements.None
            };
        }

        /// <summary>
        /// Auto-detect binding mode from VFX properties (per source-bindings.md)
        /// </summary>
        public VFXBindingMode DetectBindingMode()
        {
            if (VFX == null || VFX.visualEffectAsset == null) return VFXBindingMode.Standalone;

            // Check for keypoint buffer first (NNCam)
            if (VFX.HasGraphicsBuffer("KeypointBuffer")) return VFXBindingMode.Keypoint;

            // Check for AR depth properties (Rcam/Akvfx)
            if (VFX.HasTexture("DepthMap") || VFX.HasTexture("StencilMap") || VFX.HasTexture("PositionMap"))
                return VFXBindingMode.AR;

            // Check for audio-only (Fluo)
            if (VFX.HasFloat("Throttle") && !VFX.HasTexture("DepthMap"))
                return VFXBindingMode.Audio;

            return VFXBindingMode.Standalone;
        }

        /// <summary>
        /// Set the category (used by VFXLibraryManager during creation)
        /// </summary>
        public void SetCategory(VFXCategoryType newCategory)
        {
            category = newCategory;
            // Auto-set default bindings based on category
            bindings = newCategory switch
            {
                VFXCategoryType.People => VFXBindingRequirements.DepthMap | VFXBindingRequirements.ColorMap | VFXBindingRequirements.StencilMap,
                VFXCategoryType.Face => VFXBindingRequirements.FaceTracking | VFXBindingRequirements.ColorMap,
                VFXCategoryType.Hands => VFXBindingRequirements.HandTracking | VFXBindingRequirements.ColorMap,
                VFXCategoryType.Environment => VFXBindingRequirements.None,
                VFXCategoryType.Audio => VFXBindingRequirements.Audio,
                VFXCategoryType.Hybrid => VFXBindingRequirements.All,
                _ => VFXBindingRequirements.DepthMap | VFXBindingRequirements.ColorMap
            };
        }

        private VisualEffect _vfx;
        public VisualEffect VFX => _vfx ??= GetComponent<VisualEffect>();

        /// <summary>
        /// Check if this VFX requires a specific binding
        /// </summary>
        public bool RequiresBinding(VFXBindingRequirements binding)
        {
            return (bindings & binding) != 0;
        }

        /// <summary>
        /// Auto-detect category from VFX asset name (Editor only)
        /// </summary>
#if UNITY_EDITOR
        [ContextMenu("Auto-Detect Category")]
        public void AutoDetectCategory()
        {
            if (VFX?.visualEffectAsset == null) return;

            string name = VFX.visualEffectAsset.name.ToLower();
            string path = UnityEditor.AssetDatabase.GetAssetPath(VFX.visualEffectAsset).ToLower();

            // Detect from name
            if (name.Contains("hand") || path.Contains("hand"))
            {
                category = VFXCategoryType.Hands;
                bindings = VFXBindingRequirements.HandTracking | VFXBindingRequirements.ColorMap;
            }
            else if (name.Contains("face") || path.Contains("face"))
            {
                category = VFXCategoryType.Face;
                bindings = VFXBindingRequirements.FaceTracking | VFXBindingRequirements.ColorMap;
            }
            else if (name.Contains("audio") || name.Contains("sound") || name.Contains("wave"))
            {
                category = VFXCategoryType.Audio;
                bindings = VFXBindingRequirements.Audio;
            }
            else if (path.Contains("environment") || path.Contains("env") ||
                     name.Contains("grid") || name.Contains("world"))
            {
                category = VFXCategoryType.Environment;
                bindings = VFXBindingRequirements.None;
            }
            else if (path.Contains("body") || name.Contains("body") ||
                     name.Contains("particle") || name.Contains("point"))
            {
                category = VFXCategoryType.People;
                bindings = VFXBindingRequirements.DepthMap | VFXBindingRequirements.ColorMap | VFXBindingRequirements.StencilMap;
            }

            // Detect performance tier from features
            if (name.Contains("trail") || name.Contains("strip"))
                performanceTier = 4;
            else if (name.Contains("voxel") || name.Contains("sdf"))
                performanceTier = 5;
            else if (name.Contains("simple") || name.Contains("point"))
                performanceTier = 2;

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
