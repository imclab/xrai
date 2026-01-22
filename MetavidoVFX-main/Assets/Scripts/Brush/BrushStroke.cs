// BrushStroke.cs - Individual stroke data and mesh generation
// Part of Spec 011: OpenBrush Integration
//
// Represents a single brush stroke with control points and generated mesh.
// Handles flat ribbon and tube geometry generation.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// Represents a single brush stroke with control points and rendering.
    /// </summary>
    public class BrushStroke : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] MeshFilter _meshFilter;
        [SerializeField] MeshRenderer _meshRenderer;

        // Stroke data
        BrushData _brushData;
        Color _color = Color.white;
        float _baseSize = 0.02f;
        List<ControlPoint> _controlPoints = new();
        bool _isDirty;
        bool _isFinalized;

        // Generated mesh
        Mesh _mesh;
        List<Vector3> _vertices = new();
        List<Vector3> _normals = new();
        List<Vector2> _uvs = new();
        List<Color32> _colors = new();
        List<int> _triangles = new();

        // Properties
        public BrushData BrushData => _brushData;
        public Color Color => _color;
        public float BaseSize => _baseSize;
        public int PointCount => _controlPoints.Count;
        public bool IsFinalized => _isFinalized;
        public IReadOnlyList<ControlPoint> ControlPoints => _controlPoints;

        void Awake()
        {
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();

            _mesh = new Mesh { name = "BrushStroke" };
            if (_meshFilter != null)
                _meshFilter.mesh = _mesh;
        }

        void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }

        /// <summary>
        /// Initialize stroke with brush data and color
        /// </summary>
        public void Initialize(BrushData brushData, Color color, float size)
        {
            _brushData = brushData;
            _color = color;
            _baseSize = brushData.ClampSize(size);

            if (_meshRenderer != null && brushData.Material != null)
            {
                _meshRenderer.material = new Material(brushData.Material);
                _meshRenderer.material.color = color;
            }

            _isDirty = true;
        }

        /// <summary>
        /// Add a control point to the stroke
        /// </summary>
        public bool AddPoint(Vector3 position, Quaternion rotation, float pressure = 1f)
        {
            if (_isFinalized) return false;

            // Check minimum distance from last point
            if (_controlPoints.Count > 0)
            {
                float minDist = _brushData?.MinSegmentLength ?? 0.002f;
                if (Vector3.Distance(position, _controlPoints[^1].Position) < minDist)
                    return false;
            }

            _controlPoints.Add(new ControlPoint
            {
                Position = position,
                Rotation = rotation,
                Pressure = Mathf.Clamp01(pressure),
                Timestamp = Time.time
            });

            _isDirty = true;
            return true;
        }

        /// <summary>
        /// Update the last control point position (for smooth drawing)
        /// </summary>
        public void UpdateLastPoint(Vector3 position, Quaternion rotation, float pressure = 1f)
        {
            if (_controlPoints.Count == 0 || _isFinalized) return;

            var lastIdx = _controlPoints.Count - 1;
            _controlPoints[lastIdx] = new ControlPoint
            {
                Position = position,
                Rotation = rotation,
                Pressure = Mathf.Clamp01(pressure),
                Timestamp = Time.time
            };

            _isDirty = true;
        }

        /// <summary>
        /// Finalize the stroke (no more points can be added)
        /// </summary>
        public void Finalize()
        {
            _isFinalized = true;
            RegenerateMesh();
        }

        /// <summary>
        /// Regenerate mesh from control points
        /// </summary>
        public void RegenerateMesh()
        {
            if (_controlPoints.Count < 2 || _brushData == null)
            {
                _mesh.Clear();
                return;
            }

            switch (_brushData.GeometryType)
            {
                case BrushGeometryType.Flat:
                    GenerateFlatRibbon();
                    break;
                case BrushGeometryType.Tube:
                    GenerateTube();
                    break;
                default:
                    GenerateFlatRibbon();
                    break;
            }

            ApplyMesh();
            _isDirty = false;
        }

        void Update()
        {
            if (_isDirty && !_isFinalized)
            {
                RegenerateMesh();
            }
        }

        #region Flat Ribbon Generation

        void GenerateFlatRibbon()
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _colors.Clear();
            _triangles.Clear();

            if (_controlPoints.Count < 2) return;

            float totalLength = 0f;
            for (int i = 1; i < _controlPoints.Count; i++)
                totalLength += Vector3.Distance(_controlPoints[i].Position, _controlPoints[i - 1].Position);

            float currentLength = 0f;
            Vector3 prevRight = Vector3.right;

            for (int i = 0; i < _controlPoints.Count; i++)
            {
                var cp = _controlPoints[i];

                // Calculate direction
                Vector3 forward;
                if (i == 0)
                    forward = (_controlPoints[1].Position - cp.Position).normalized;
                else if (i == _controlPoints.Count - 1)
                    forward = (cp.Position - _controlPoints[i - 1].Position).normalized;
                else
                    forward = (_controlPoints[i + 1].Position - _controlPoints[i - 1].Position).normalized;

                if (forward.sqrMagnitude < 0.001f)
                    forward = Vector3.forward;

                // Calculate right vector (perpendicular to forward, prefer previous for consistency)
                Vector3 up = cp.Rotation * Vector3.up;
                Vector3 right = Vector3.Cross(forward, up).normalized;
                if (right.sqrMagnitude < 0.001f)
                    right = prevRight;
                else
                    prevRight = right;

                // Calculate size with pressure
                float size = _brushData.GetPressuredSize(_baseSize, cp.Pressure);
                float halfWidth = size * 0.5f;

                // Add vertices
                Vector3 left = cp.Position - right * halfWidth;
                Vector3 rightPos = cp.Position + right * halfWidth;

                _vertices.Add(left);
                _vertices.Add(rightPos);

                // Normals (face camera)
                Vector3 normal = Vector3.Cross(right, forward).normalized;
                _normals.Add(normal);
                _normals.Add(normal);

                // UVs
                float v = totalLength > 0 ? currentLength / totalLength : 0f;
                _uvs.Add(new Vector2(0f, v));
                _uvs.Add(new Vector2(1f, v));

                // Colors with opacity
                float opacity = _brushData.GetPressuredOpacity(cp.Pressure);
                Color32 c = _color;
                c.a = (byte)(opacity * 255);
                _colors.Add(c);
                _colors.Add(c);

                // Update length
                if (i > 0)
                    currentLength += Vector3.Distance(cp.Position, _controlPoints[i - 1].Position);
            }

            // Generate triangles
            for (int i = 0; i < _controlPoints.Count - 1; i++)
            {
                int baseIdx = i * 2;
                // First triangle
                _triangles.Add(baseIdx);
                _triangles.Add(baseIdx + 2);
                _triangles.Add(baseIdx + 1);
                // Second triangle
                _triangles.Add(baseIdx + 1);
                _triangles.Add(baseIdx + 2);
                _triangles.Add(baseIdx + 3);

                // Backfaces if enabled
                if (_brushData.RenderBackfaces)
                {
                    _triangles.Add(baseIdx);
                    _triangles.Add(baseIdx + 1);
                    _triangles.Add(baseIdx + 2);
                    _triangles.Add(baseIdx + 1);
                    _triangles.Add(baseIdx + 3);
                    _triangles.Add(baseIdx + 2);
                }
            }
        }

        #endregion

        #region Tube Generation

        void GenerateTube()
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _colors.Clear();
            _triangles.Clear();

            if (_controlPoints.Count < 2) return;

            int sides = _brushData.TubeSides;
            float angleStep = 2f * Mathf.PI / sides;

            float totalLength = 0f;
            for (int i = 1; i < _controlPoints.Count; i++)
                totalLength += Vector3.Distance(_controlPoints[i].Position, _controlPoints[i - 1].Position);

            float currentLength = 0f;
            Vector3 prevUp = Vector3.up;

            for (int i = 0; i < _controlPoints.Count; i++)
            {
                var cp = _controlPoints[i];

                // Calculate direction
                Vector3 forward;
                if (i == 0)
                    forward = (_controlPoints[1].Position - cp.Position).normalized;
                else if (i == _controlPoints.Count - 1)
                    forward = (cp.Position - _controlPoints[i - 1].Position).normalized;
                else
                    forward = (_controlPoints[i + 1].Position - _controlPoints[i - 1].Position).normalized;

                if (forward.sqrMagnitude < 0.001f)
                    forward = Vector3.forward;

                // Create rotation basis
                Vector3 up = Vector3.Cross(forward, prevUp).sqrMagnitude > 0.001f
                    ? Vector3.Cross(Vector3.Cross(forward, prevUp), forward).normalized
                    : Vector3.up;
                prevUp = up;

                Vector3 right = Vector3.Cross(forward, up).normalized;

                // Calculate radius with pressure
                float radius = _brushData.GetPressuredSize(_baseSize, cp.Pressure) * 0.5f;

                // Generate ring vertices
                for (int j = 0; j < sides; j++)
                {
                    float angle = j * angleStep;
                    Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * radius;
                    Vector3 pos = cp.Position + offset;
                    Vector3 normal = offset.normalized;

                    _vertices.Add(pos);
                    _normals.Add(normal);

                    float u = (float)j / sides;
                    float v = totalLength > 0 ? currentLength / totalLength : 0f;
                    _uvs.Add(new Vector2(u, v));

                    float opacity = _brushData.GetPressuredOpacity(cp.Pressure);
                    Color32 c = _color;
                    c.a = (byte)(opacity * 255);
                    _colors.Add(c);
                }

                if (i > 0)
                    currentLength += Vector3.Distance(cp.Position, _controlPoints[i - 1].Position);
            }

            // Generate triangles connecting rings
            for (int i = 0; i < _controlPoints.Count - 1; i++)
            {
                int ring1 = i * sides;
                int ring2 = (i + 1) * sides;

                for (int j = 0; j < sides; j++)
                {
                    int j1 = (j + 1) % sides;

                    // First triangle
                    _triangles.Add(ring1 + j);
                    _triangles.Add(ring2 + j);
                    _triangles.Add(ring1 + j1);

                    // Second triangle
                    _triangles.Add(ring1 + j1);
                    _triangles.Add(ring2 + j);
                    _triangles.Add(ring2 + j1);
                }
            }
        }

        #endregion

        void ApplyMesh()
        {
            _mesh.Clear();

            if (_vertices.Count == 0) return;

            _mesh.SetVertices(_vertices);
            _mesh.SetNormals(_normals);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);

            _mesh.RecalculateBounds();
        }

        #region Serialization

        /// <summary>
        /// Serialize stroke to JSON-compatible format
        /// </summary>
        public StrokeData ToData()
        {
            return new StrokeData
            {
                BrushId = _brushData?.BrushId ?? "",
                Color = new float[] { _color.r, _color.g, _color.b, _color.a },
                Size = _baseSize,
                Points = _controlPoints.ConvertAll(cp => new ControlPointData
                {
                    Position = new float[] { cp.Position.x, cp.Position.y, cp.Position.z },
                    Rotation = new float[] { cp.Rotation.x, cp.Rotation.y, cp.Rotation.z, cp.Rotation.w },
                    Pressure = cp.Pressure
                })
            };
        }

        /// <summary>
        /// Load stroke from serialized data
        /// </summary>
        public void FromData(StrokeData data, BrushData brushData)
        {
            _brushData = brushData;
            _color = new Color(data.Color[0], data.Color[1], data.Color[2], data.Color[3]);
            _baseSize = data.Size;

            _controlPoints.Clear();
            foreach (var cpData in data.Points)
            {
                _controlPoints.Add(new ControlPoint
                {
                    Position = new Vector3(cpData.Position[0], cpData.Position[1], cpData.Position[2]),
                    Rotation = new Quaternion(cpData.Rotation[0], cpData.Rotation[1], cpData.Rotation[2], cpData.Rotation[3]),
                    Pressure = cpData.Pressure
                });
            }

            if (_meshRenderer != null && brushData?.Material != null)
            {
                _meshRenderer.material = new Material(brushData.Material);
                _meshRenderer.material.color = _color;
            }

            _isFinalized = true;
            RegenerateMesh();
        }

        #endregion
    }

    /// <summary>
    /// Single control point in a brush stroke
    /// </summary>
    [Serializable]
    public struct ControlPoint
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Pressure;
        public float Timestamp;
    }

    /// <summary>
    /// Serializable stroke data for save/load
    /// </summary>
    [Serializable]
    public class StrokeData
    {
        public string BrushId;
        public float[] Color;
        public float Size;
        public List<ControlPointData> Points;
    }

    /// <summary>
    /// Serializable control point data
    /// </summary>
    [Serializable]
    public class ControlPointData
    {
        public float[] Position;
        public float[] Rotation;
        public float Pressure;
    }
}
