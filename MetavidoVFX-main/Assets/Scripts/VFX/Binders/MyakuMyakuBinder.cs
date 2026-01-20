using UnityEngine;
using UnityEngine.VFX;

namespace MetavidoVFX
{
    /// <summary>
    /// Binds MyakuMyaku VFX to our AR pipeline.
    /// Maps: StencilMap → _SegmentationTex, ColorMap → _ARRgbDTex
    /// </summary>
    [RequireComponent(typeof(VisualEffect))]
    public class MyakuMyakuBinder : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] ARDepthSource arDepthSource;

        [Header("Spawn Settings")]
        [SerializeField] Vector4 spawnUvMinMax = new(0.2f, 0.2f, 0.8f, 0.8f);
        [SerializeField] float spawnRate = 0.5f;
        [SerializeField] bool autoDetectBounds = true;

        VisualEffect vfx;

        // Property IDs
        static readonly int _SegmentationTex = Shader.PropertyToID("_SegmentationTex");
        static readonly int _ARRgbDTex = Shader.PropertyToID("_ARRgbDTex");
        static readonly int _SpawnUvMinMax = Shader.PropertyToID("_SpawnUvMinMax");
        static readonly int _SpawnRate = Shader.PropertyToID("_SpawnRate");

        void Awake()
        {
            vfx = GetComponent<VisualEffect>();
        }

        void Start()
        {
            if (arDepthSource == null)
                arDepthSource = FindAnyObjectByType<ARDepthSource>();
        }

        void LateUpdate()
        {
            if (arDepthSource == null || vfx == null) return;

            // Bind textures
            var stencil = arDepthSource.StencilMap;
            var color = arDepthSource.ColorMap;

            if (stencil != null)
            {
                vfx.SetTexture(_SegmentationTex, stencil);

                // Auto-detect spawn rate from stencil coverage
                if (autoDetectBounds)
                    spawnRate = EstimateStencilCoverage();
            }

            if (color != null)
                vfx.SetTexture(_ARRgbDTex, color);

            // Set spawn parameters
            vfx.SetVector4(_SpawnUvMinMax, spawnUvMinMax);
            vfx.SetFloat(_SpawnRate, spawnRate);
        }

        float EstimateStencilCoverage()
        {
            // Simple heuristic: use fixed rate when stencil is available
            // Could be enhanced with GPU readback for actual coverage
            return arDepthSource.StencilMap != null ? 0.3f : 0f;
        }

        public void SetSpawnBounds(Rect bounds)
        {
            spawnUvMinMax = new Vector4(bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax);
        }

        public void SetSpawnRate(float rate) => spawnRate = Mathf.Clamp01(rate);
    }
}
