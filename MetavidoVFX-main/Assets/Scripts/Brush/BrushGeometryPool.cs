// BrushGeometryPool.cs - Object pooling for brush stroke geometry
// Part of Spec 011: OpenBrush Integration
//
// Based on Open Brush's GeometryPool pattern for memory efficiency.
// Reuses vertex/triangle buffers instead of allocating new ones per stroke.

using System.Collections.Generic;
using UnityEngine;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// Pooled geometry buffers for efficient stroke rendering.
    /// Prevents GC allocation during active drawing.
    /// </summary>
    public class BrushGeometryPool
    {
        // Pool management
        static readonly Stack<BrushGeometryPool> _unused = new();
        static int _totalAllocated;
        static int _peakUsage;

        // Geometry buffers
        public List<Vector3> Vertices { get; private set; }
        public List<Vector3> Normals { get; private set; }
        public List<Vector2> UVs { get; private set; }
        public List<Color32> Colors { get; private set; }
        public List<int> Triangles { get; private set; }

        // Capacity tracking
        const int InitialVertexCapacity = 256;
        const int InitialTriangleCapacity = 512;

        BrushGeometryPool()
        {
            Vertices = new List<Vector3>(InitialVertexCapacity);
            Normals = new List<Vector3>(InitialVertexCapacity);
            UVs = new List<Vector2>(InitialVertexCapacity);
            Colors = new List<Color32>(InitialVertexCapacity);
            Triangles = new List<int>(InitialTriangleCapacity);
            _totalAllocated++;
        }

        /// <summary>
        /// Allocate a geometry pool from the cache or create new
        /// </summary>
        public static BrushGeometryPool Allocate()
        {
            BrushGeometryPool pool;
            if (_unused.Count > 0)
            {
                pool = _unused.Pop();
            }
            else
            {
                pool = new BrushGeometryPool();
            }

            int currentUsage = _totalAllocated - _unused.Count;
            if (currentUsage > _peakUsage)
                _peakUsage = currentUsage;

            return pool;
        }

        /// <summary>
        /// Return a geometry pool to the cache for reuse
        /// </summary>
        public static void Free(BrushGeometryPool pool)
        {
            if (pool == null) return;
            pool.Clear();
            _unused.Push(pool);
        }

        /// <summary>
        /// Clear all buffers but keep capacity
        /// </summary>
        public void Clear()
        {
            Vertices.Clear();
            Normals.Clear();
            UVs.Clear();
            Colors.Clear();
            Triangles.Clear();
        }

        /// <summary>
        /// Apply geometry to a mesh
        /// </summary>
        public void ApplyToMesh(Mesh mesh)
        {
            mesh.Clear();

            if (Vertices.Count == 0) return;

            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, UVs);
            mesh.SetColors(Colors);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Ensure capacity for expected vertex count
        /// </summary>
        public void EnsureCapacity(int vertexCount, int triangleCount)
        {
            if (Vertices.Capacity < vertexCount)
            {
                int newCapacity = Mathf.NextPowerOfTwo(vertexCount);
                Vertices.Capacity = newCapacity;
                Normals.Capacity = newCapacity;
                UVs.Capacity = newCapacity;
                Colors.Capacity = newCapacity;
            }

            if (Triangles.Capacity < triangleCount)
            {
                Triangles.Capacity = Mathf.NextPowerOfTwo(triangleCount);
            }
        }

        #region Static Utilities

        /// <summary>
        /// Get pool statistics for debugging
        /// </summary>
        public static (int total, int unused, int peak) GetStats()
        {
            return (_totalAllocated, _unused.Count, _peakUsage);
        }

        /// <summary>
        /// Clear all cached pools (call during scene unload)
        /// </summary>
        public static void ClearCache()
        {
            _unused.Clear();
        }

        /// <summary>
        /// Trim cache to specified size
        /// </summary>
        public static void TrimCache(int maxCached = 10)
        {
            while (_unused.Count > maxCached)
            {
                _unused.Pop();
                _totalAllocated--;
            }
        }

        #endregion
    }

    /// <summary>
    /// Math utilities for brush geometry generation.
    /// Based on Open Brush's MathUtils patterns.
    /// </summary>
    public static class BrushMathUtils
    {
        /// <summary>
        /// Compute minimal rotation frame (parallel transport).
        /// Prevents tube twisting by maintaining consistent orientation.
        /// </summary>
        /// <param name="tangent">Current stroke direction</param>
        /// <param name="prevUp">Previous frame's up vector</param>
        /// <returns>New up vector with minimal rotation from previous</returns>
        public static Vector3 ComputeMinimalRotationFrame(Vector3 tangent, Vector3 prevUp)
        {
            // Project previous up onto plane perpendicular to new tangent
            // This is parallel transport - minimizes twist
            Vector3 right = Vector3.Cross(tangent, prevUp);

            if (right.sqrMagnitude < 0.0001f)
            {
                // Tangent parallel to up - find alternative
                right = Vector3.Cross(tangent, Vector3.forward);
                if (right.sqrMagnitude < 0.0001f)
                    right = Vector3.Cross(tangent, Vector3.right);
            }

            right.Normalize();
            return Vector3.Cross(right, tangent).normalized;
        }

        /// <summary>
        /// Smooth pressure value using exponential decay.
        /// Based on Open Brush's pressure smoothing algorithm.
        /// </summary>
        /// <param name="current">Current raw pressure</param>
        /// <param name="previous">Previous smoothed pressure</param>
        /// <param name="distance">Distance traveled since last point</param>
        /// <param name="smoothingWindow">Smoothing distance (meters)</param>
        /// <returns>Smoothed pressure value</returns>
        public static float SmoothPressure(float current, float previous, float distance, float smoothingWindow = 0.02f)
        {
            if (smoothingWindow <= 0f) return current;

            // Exponential decay based on distance
            float k = Mathf.Pow(0.1f, distance / smoothingWindow);
            return Mathf.Lerp(current, previous, k);
        }

        /// <summary>
        /// Smooth position using kernel averaging.
        /// Applies (.25, .5, .25) smoothing kernel.
        /// </summary>
        public static Vector3 SmoothPosition(Vector3 prev, Vector3 current, Vector3 next)
        {
            return prev * 0.25f + current * 0.5f + next * 0.25f;
        }

        /// <summary>
        /// Calculate catmull-rom spline point for smoother curves.
        /// </summary>
        public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        /// <summary>
        /// Generate points along a circle for tube cross-section.
        /// </summary>
        public static void GenerateCirclePoints(
            Vector3 center,
            Vector3 forward,
            Vector3 up,
            float radius,
            int sides,
            List<Vector3> outPositions,
            List<Vector3> outNormals)
        {
            Vector3 right = Vector3.Cross(forward, up).normalized;
            float angleStep = 2f * Mathf.PI / sides;

            for (int i = 0; i < sides; i++)
            {
                float angle = i * angleStep;
                Vector3 offset = Mathf.Cos(angle) * right + Mathf.Sin(angle) * up;
                outPositions.Add(center + offset * radius);
                outNormals.Add(offset);
            }
        }
    }
}
