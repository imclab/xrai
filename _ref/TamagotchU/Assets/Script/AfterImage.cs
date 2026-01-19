using System.Collections.Generic;
using UnityEngine;

namespace AfterimageSample
{
    public class AfterImage
    {
        RenderParams[] _params;
        Mesh[] _meshes;
        Matrix4x4[] _matrices;

        /// 描画された回数.
        public int FrameCount { get; private set; }

        /// <summary>
        /// コンストラクタ.
        /// </summary>
        /// <param name="meshCount">描画するメッシュの数.</param>
        public AfterImage(int meshCount)
        {
            _params = new RenderParams[meshCount];
            _meshes = new Mesh[meshCount];
            _matrices = new Matrix4x4[meshCount];
            Reset();
        }

        /// <summary>
        /// 描画前もしくは後に実行する.
        /// </summary>
        public void Reset()
        {
            FrameCount = 0;
        }

        // optimization
        private static Stack<Mesh> s_MeshPool = new Stack<Mesh>();

        private Mesh GetMeshFromPool()
        {
            return s_MeshPool.Count > 0 ? s_MeshPool.Pop() : new Mesh();
        }

        private void ReturnMeshToPool(Mesh mesh)
        {
            mesh.Clear();
            s_MeshPool.Push(mesh);
        }
        private static List<Vector3> s_Vertices = new List<Vector3>(1024);
        private static List<Vector3> s_Normals = new List<Vector3>(1024);
        private static List<Vector2> s_UVs = new List<Vector2>(1024);
        private static List<int> s_Triangles = new List<int>(1024);
        private static Dictionary<int, int> s_IndexMap = new Dictionary<int, int>(1024);


        /// <summary>
        /// メッシュごとに使用するマテリアルを用意し、現在のメッシュの形状を記憶させる.
        /// </summary>
        /// <param name="material">使用するマテリアル. </param>
        /// <param name="layer">描画するレイヤー.</param>
        /// <param name="renderers">記憶させるSkinnedMeshRendereの配列.</param>
        public void Setup(Material material, int layer, SkinnedMeshRenderer[] renderers)
        {
            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                for (int j = 0; j < renderers[i].sharedMesh.subMeshCount; j++)
                {
                    material = renderers[i].materials[j];

                    if (_params[count].material != material)
                    {
                        _params[count] = new RenderParams(material);
                    }
                    if (_params[count].layer != layer)
                    {
                        _params[count].layer = layer;
                    }

                    // Get baked mesh from pool, clear and bake current frame
                    Mesh bakedMesh = GetMeshFromPool();
                    bakedMesh.Clear();
                    renderers[i].BakeMesh(bakedMesh);

                    // Prepare mesh at _meshes[count] or get pooled
                    if (_meshes[count] == null)
                    {
                        _meshes[count] = GetMeshFromPool();
                    }
                    _meshes[count].Clear();

                    // Fill _meshes[count] from bakedMesh submesh data without new allocations
                    ExtractTrueSubmeshInto(bakedMesh, j, _meshes[count]);

                    _matrices[count] = renderers[i].transform.localToWorldMatrix;

                    // Return bakedMesh to pool after extraction
                    ReturnMeshToPool(bakedMesh);

                    count++;
                }
            }
        }


        // helper function for extracting submeshes
        public static void ExtractTrueSubmeshInto(Mesh source, int subMeshIndex, Mesh target)
        {
            // Clear reusable buffers
            s_Vertices.Clear();
            s_Normals.Clear();
            s_UVs.Clear();
            s_Triangles.Clear();
            s_IndexMap.Clear();

            int[] triangles = source.GetTriangles(subMeshIndex);
            Vector3[] originalVertices = source.vertices;
            Vector3[] originalNormals = source.normals;
            Vector2[] originalUVs = source.uv;

            for (int i = 0; i < triangles.Length; i++)
            {
                int oldIndex = triangles[i];
                if (!s_IndexMap.TryGetValue(oldIndex, out int newIndex))
                {
                    newIndex = s_Vertices.Count;
                    s_IndexMap[oldIndex] = newIndex;

                    s_Vertices.Add(originalVertices[oldIndex]);
                    if (originalNormals != null && originalNormals.Length > oldIndex) s_Normals.Add(originalNormals[oldIndex]);
                    if (originalUVs != null && originalUVs.Length > oldIndex) s_UVs.Add(originalUVs[oldIndex]);
                }
                s_Triangles.Add(newIndex);
            }

            target.Clear();
            target.SetVertices(s_Vertices);
            if (s_Normals.Count > 0) target.SetNormals(s_Normals);
            if (s_UVs.Count > 0) target.SetUVs(0, s_UVs);
            target.SetTriangles(s_Triangles, 0);
            target.RecalculateBounds();
            if (s_Normals.Count == 0) target.RecalculateNormals();
        }


        /// <summary>
        /// 記憶したメッシュを全て描画する.
        /// </summary>
        public void RenderMeshes()
        {
            for (int i = 0; i < _meshes.Length; i++)
            {
                Graphics.RenderMesh(_params[i], _meshes[i], 0, _matrices[i]);
            }
            FrameCount++;
        }
    }
}
