// ARFoundationFaceMeshBaker.cs - Modernized face mesh → VFX baking
// Upgraded from Unity 2019 ARKit Plugin to AR Foundation 6.x
// Pattern: Face mesh vertices baked to position texture for VFX Graph sampling

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;

namespace FaceTrackingVFX
{
    /// <summary>
    /// Bakes AR face mesh vertex data to a position RenderTexture for VFX Graph.
    /// Uses AR Foundation's ARFaceManager instead of legacy Unity-ARKit-Plugin.
    /// </summary>
    [RequireComponent(typeof(ARFace))]
    public class ARFoundationFaceMeshBaker : MonoBehaviour
    {
        [Header("Output")]
        [SerializeField] RenderTexture _positionMap;

        [Header("Compute")]
        [SerializeField] ComputeShader _vertexBaker;

        ARFace _arFace;
        ComputeBuffer _positionBuffer;
        RenderTexture _tmpPositionMap;

        // Shader property IDs
        int _vertexCountID;
        int _transformID;
        int _positionBufferID;
        int _positionMapID;

        void Awake()
        {
            _arFace = GetComponent<ARFace>();

            // Cache property IDs
            _vertexCountID = Shader.PropertyToID("VertexCount");
            _transformID = Shader.PropertyToID("Transform");
            _positionBufferID = Shader.PropertyToID("PositionBuffer");
            _positionMapID = Shader.PropertyToID("PositionMap");
        }

        void OnEnable()
        {
            if (_arFace != null)
            {
                _arFace.updated += OnFaceUpdated;
            }
        }

        void OnDisable()
        {
            if (_arFace != null)
            {
                _arFace.updated -= OnFaceUpdated;
            }

            CleanupBuffers();
        }

        void OnDestroy()
        {
            CleanupBuffers();
        }

        void OnFaceUpdated(ARFaceUpdatedEventArgs args)
        {
            if (_positionMap == null || _vertexBaker == null) return;

            // Get face mesh from ARFace
            Mesh faceMesh = _arFace.GetComponent<MeshFilter>()?.sharedMesh;
            if (faceMesh == null) return;

            // Get vertex data
            using (var meshDataArray = Mesh.AcquireReadOnlyMeshData(faceMesh))
            {
                var meshData = meshDataArray[0];
                int vertexCount = meshData.vertexCount;

                if (vertexCount == 0) return;

                // Create or resize buffers
                EnsureBuffers(vertexCount);

                // Get vertices as NativeArray
                var vertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
                meshData.GetVertices(vertices);

                // Upload to GPU
                _positionBuffer.SetData(vertices);
                vertices.Dispose();

                // Dispatch compute shader
                int mapWidth = _positionMap.width;
                int mapHeight = _positionMap.height;

                _vertexBaker.SetInt(_vertexCountID, vertexCount);
                _vertexBaker.SetMatrix(_transformID, transform.localToWorldMatrix);
                _vertexBaker.SetBuffer(0, _positionBufferID, _positionBuffer);
                _vertexBaker.SetTexture(0, _positionMapID, _tmpPositionMap);

                // Dispatch with proper thread group size
                int groupsX = Mathf.CeilToInt(mapWidth / 8f);
                int groupsY = Mathf.CeilToInt(mapHeight / 8f);
                _vertexBaker.Dispatch(0, groupsX, groupsY, 1);

                // Copy result to output
                Graphics.CopyTexture(_tmpPositionMap, _positionMap);
            }
        }

        void EnsureBuffers(int vertexCount)
        {
            // Create position buffer if needed
            if (_positionBuffer == null || _positionBuffer.count < vertexCount * 3)
            {
                _positionBuffer?.Release();
                _positionBuffer = new ComputeBuffer(vertexCount * 3, sizeof(float));
            }

            // Create temp render texture if needed
            if (_tmpPositionMap == null)
            {
                _tmpPositionMap = new RenderTexture(_positionMap.width, _positionMap.height, 0, _positionMap.format);
                _tmpPositionMap.enableRandomWrite = true;
                _tmpPositionMap.Create();
            }
        }

        void CleanupBuffers()
        {
            _positionBuffer?.Release();
            _positionBuffer = null;

            if (_tmpPositionMap != null)
            {
                _tmpPositionMap.Release();
                DestroyImmediate(_tmpPositionMap);
                _tmpPositionMap = null;
            }
        }
    }
}
