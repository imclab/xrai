// VFXProxBuffer - Manages proximity buffers for particle-to-particle interactions
// Required for plexus_depth_people_rcam3 and similar VFX that use neighbor lookups
// Add this component to the scene when using VFX with VFXProx features
//
// Based on Keijiro's Rcam3 VFXProx system

using UnityEngine;

namespace XRRAI.VFXBinders
{
    /// <summary>
    /// Creates and manages global GraphicsBuffers for VFX proximity lookups.
    /// Particles can register their positions and query for nearby neighbors.
    ///
    /// Required by VFX that include VFXProxCommon.hlsl:
    /// - plexus_depth_people_rcam3.vfx
    /// - Any VFX using VFXProx_AddPoint / VFXProx_LookUpNearestPair
    /// </summary>
    [ExecuteInEditMode]
    public sealed class VFXProxBuffer : MonoBehaviour
    {
        #region Public properties

        [Tooltip("Spatial extent of the proximity grid (world units)")]
        [field:SerializeField]
        public Vector3 Extent { get; set; } = Vector3.one * 4f;

        #endregion

        #region Project asset reference

        [Tooltip("Compute shader for clearing buffers each frame")]
        [SerializeField]
        private ComputeShader clearCompute;

        #endregion

        #region Shader constants

        // These constants must match those defined in VFXProxCommon.hlsl
        const int CellsPerAxis = 16;
        const int CellCapacity = 32;

        #endregion

        #region Shader IDs

        static class ShaderID
        {
            public static readonly int VFXProx_CellSize = Shader.PropertyToID("VFXProx_CellSize");
            public static readonly int VFXProx_CountBuffer = Shader.PropertyToID("VFXProx_CountBuffer");
            public static readonly int VFXProx_PointBuffer = Shader.PropertyToID("VFXProx_PointBuffer");
        }

        #endregion

        #region Private members

        int TotalCells => CellsPerAxis * CellsPerAxis * CellsPerAxis;

        (GraphicsBuffer point, GraphicsBuffer count) _buffer;
        bool _initialized;

        #endregion

        #region MonoBehaviour implementation

        void OnEnable()
        {
            // Load compute shader if not assigned
            if (clearCompute == null)
            {
                clearCompute = Resources.Load<ComputeShader>("VFXProxClear");
                if (clearCompute == null)
                {
                    // Try alternate locations
                    clearCompute = Resources.Load<ComputeShader>("Rcam/VFXProxClear");
                }
            }

            if (clearCompute == null)
            {
                Debug.LogWarning("[VFXProxBuffer] VFXProxClear compute shader not found. Proximity features will not work.");
            }

            // Create buffers
            _buffer.point = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                TotalCells * CellCapacity,
                sizeof(float) * 3);

            _buffer.count = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                TotalCells,
                sizeof(uint));

            // Set global buffers so all VFX can access them
            Shader.SetGlobalBuffer(ShaderID.VFXProx_PointBuffer, _buffer.point);
            Shader.SetGlobalBuffer(ShaderID.VFXProx_CountBuffer, _buffer.count);

            _initialized = true;
            Debug.Log($"[VFXProxBuffer] Initialized with extent {Extent}, {TotalCells} cells");
        }

        void OnDisable()
        {
            _buffer.point?.Dispose();
            _buffer.count?.Dispose();
            _buffer = (null, null);
            _initialized = false;
        }

        void LateUpdate()
        {
            if (!_initialized || clearCompute == null) return;

            // Update cell size based on current extent
            Shader.SetGlobalVector(ShaderID.VFXProx_CellSize, Extent / CellsPerAxis);

            // Clear buffers for next frame
            DispatchCompute(clearCompute, 0, CellsPerAxis, CellsPerAxis, CellsPerAxis);
        }

        #endregion

        #region Helper methods

        void DispatchCompute(ComputeShader compute, int kernel, int x, int y = 1, int z = 1)
        {
            compute.GetKernelThreadGroupSizes(kernel, out uint xc, out uint yc, out uint zc);
            x = (x + (int)xc - 1) / (int)xc;
            y = (y + (int)yc - 1) / (int)yc;
            z = (z + (int)zc - 1) / (int)zc;
            compute.Dispatch(kernel, x, y, z);
        }

        #endregion

        #region Editor validation

        void OnValidate()
        {
            // Ensure minimum extent
            Extent = Vector3.Max(Extent, Vector3.one * 0.1f);
        }

        #endregion
    }
}
