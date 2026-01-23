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
    /// Uses pooled geometry buffers and parallel transport for efficiency.
    /// </summary>
    public class BrushStroke : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] MeshFilter _meshFilter;
        [SerializeField] MeshRenderer _meshRenderer;

        [Header("Smoothing")]
        [SerializeField] bool _enablePressureSmoothing = true;
        [SerializeField] float _pressureSmoothingWindow = 0.02f;
        [SerializeField] bool _enablePositionSmoothing = true;

        // Stroke data
        BrushData _brushData;
        Color _color = Color.white;
        float _baseSize = 0.02f;
        List<ControlPoint> _controlPoints = new();
        bool _isDirty;
        bool _isFinalized;

        // Pooled geometry (replaces per-stroke allocations)
        Mesh _mesh;
        BrushGeometryPool _geometryPool;

        // Parallel transport state for tubes
        Vector3 _lastTubeUp = Vector3.up;

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

            // Allocate from pool instead of creating new lists
            _geometryPool = BrushGeometryPool.Allocate();
        }

        void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);

            // Return geometry buffers to pool for reuse
            if (_geometryPool != null)
            {
                BrushGeometryPool.Free(_geometryPool);
                _geometryPool = null;
            }
        }

        /// <summary>
        /// Initialize stroke with brush data and color
        /// </summary>
        public void Initialize(BrushData brushData, Color color, float size)
        {
            _brushData = brushData;
            _color = color;
            _baseSize = brushData.ClampSize(size);

            // Ensure we have required components
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();

            if (_meshRenderer != null)
            {
                Material mat = CreateMaterial(brushData, color);
                _meshRenderer.material = mat;

                // DEBUG: Log shader and material info
                Debug.Log($"[BrushStroke] Initialized: Brush={brushData?.DisplayName}, " +
                          $"Shader={mat.shader?.name ?? "NULL"}, " +
                          $"Color={mat.color}, " +
                          $"GlowStrength={(mat.HasProperty("_GlowStrength") ? mat.GetFloat("_GlowStrength").ToString() : "N/A")}, " +
                          $"UseVertexColor={(mat.HasProperty("_UseVertexColor") ? mat.GetFloat("_UseVertexColor").ToString() : "N/A")}");
            }
            else
            {
                Debug.LogError($"[BrushStroke] No MeshRenderer! Brush={brushData?.DisplayName}");
            }

            _isDirty = true;
        }

        /// <summary>
        /// Creates a material for the stroke, with robust fallback chain.
        /// </summary>
        Material CreateMaterial(BrushData brushData, Color color)
        {
            Material mat = null;

            // Try to use pre-assigned material from BrushData
            if (brushData?.Material != null)
            {
                mat = new Material(brushData.Material);
            }
            else
            {
                // Try Resources.Load first (more reliable at runtime)
                Shader foundShader = Resources.Load<Shader>("Shaders/BrushStroke");

                // Fallback chain: try multiple shaders in order of preference
                if (foundShader == null)
                {
                    string[] shaderNames = new[]
                    {
                        "XRRAI/BrushStroke",
                        "Universal Render Pipeline/Unlit",
                        "Universal Render Pipeline/Lit",
                        "Unlit/Color",
                        "Standard",
                        "Sprites/Default"
                    };

                    foreach (var shaderName in shaderNames)
                    {
                        foundShader = Shader.Find(shaderName);
                        if (foundShader != null && foundShader.name != "Hidden/InternalErrorShader")
                        {
                            break;
                        }
                        foundShader = null;
                    }
                }

                if (foundShader != null)
                {
                    mat = new Material(foundShader);
                }
                else
                {
                    // Last resort: use default diffuse
                    Debug.LogWarning("[BrushStroke] No shader found, using fallback diffuse");
                    mat = new Material(Shader.Find("Diffuse") ?? Shader.Find("Mobile/Diffuse"));
                    if (mat.shader == null)
                    {
                        // Absolute last resort - create from built-in
                        mat = new Material(Shader.Find("Hidden/InternalErrorShader"));
                    }
                }
            }

            // FIX: Set material color to WHITE - vertex colors carry the actual stroke color
            // This prevents color² darkening (mat.color * vertexColor = color * color)
            mat.color = Color.white;

            // Enable vertex colors - this is where stroke color comes from
            if (mat.HasProperty("_UseVertexColor"))
                mat.SetFloat("_UseVertexColor", 1f);

            // Set render queue to transparent for visibility
            mat.renderQueue = 3000;

            // Disable culling for double-sided rendering (critical for AR visibility)
            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", 0f);

            // Enable transparency blending
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f); // 1 = Transparent

            // Apply glow settings for emissive/audio-reactive brushes
            bool isGlowBrush = brushData != null &&
                (brushData.Category == BrushCategory.Emissive ||
                 brushData.Category == BrushCategory.AudioReactive ||
                 brushData.IsAudioReactive);

            if (isGlowBrush)
            {
                // Set glow color to match stroke color (bright)
                if (mat.HasProperty("_GlowColor"))
                    mat.SetColor("_GlowColor", color);

                // Boost glow strength for visibility
                if (mat.HasProperty("_GlowStrength"))
                    mat.SetFloat("_GlowStrength", 2.5f);

                // Bright core
                if (mat.HasProperty("_CoreBrightness"))
                    mat.SetFloat("_CoreBrightness", 1.5f);

                // Enable audio reactive if applicable
                if (brushData.IsAudioReactive && mat.HasProperty("_AudioReactive"))
                    mat.SetFloat("_AudioReactive", 1f);

                // Set emission for lit shaders
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", color * 0.5f);
                if (mat.HasProperty("_EmissionStrength"))
                    mat.SetFloat("_EmissionStrength", 1.5f);
            }

            return mat;
        }

        /// <summary>
        /// Add a control point to the stroke with optional pressure smoothing
        /// </summary>
        public bool AddPoint(Vector3 position, Quaternion rotation, float pressure = 1f)
        {
            if (_isFinalized) return false;

            // Check minimum distance from last point
            float distance = 0f;
            if (_controlPoints.Count > 0)
            {
                float minDist = _brushData?.MinSegmentLength ?? 0.002f;
                distance = Vector3.Distance(position, _controlPoints[^1].Position);
                if (distance < minDist)
                    return false;
            }

            // Apply pressure smoothing (Open Brush pattern)
            float smoothedPressure = pressure;
            if (_enablePressureSmoothing && _controlPoints.Count > 0)
            {
                float prevPressure = _controlPoints[^1].Pressure;
                smoothedPressure = BrushMathUtils.SmoothPressure(
                    pressure, prevPressure, distance, _pressureSmoothingWindow);
            }

            _controlPoints.Add(new ControlPoint
            {
                Position = position,
                Rotation = rotation,
                Pressure = Mathf.Clamp01(smoothedPressure),
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
                case BrushGeometryType.Hull:
                    GenerateHull();
                    break;
                case BrushGeometryType.Particle:
                    GenerateParticles();
                    break;
                case BrushGeometryType.Spray:
                    GenerateSpray();
                    break;
                case BrushGeometryType.Slice:
                    GenerateSlice();
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
            var pool = _geometryPool;
            pool.Clear();

            if (_controlPoints.Count < 2) return;

            // Pre-allocate estimated capacity
            int vertCount = _controlPoints.Count * 2;
            int triCount = (_controlPoints.Count - 1) * 6 * (_brushData?.RenderBackfaces == true ? 2 : 1);
            pool.EnsureCapacity(vertCount, triCount);

            // Optional position smoothing
            List<Vector3> positions = GetSmoothedPositions();

            float totalLength = 0f;
            for (int i = 1; i < positions.Count; i++)
                totalLength += Vector3.Distance(positions[i], positions[i - 1]);

            float currentLength = 0f;
            Vector3 prevRight = Vector3.right;

            for (int i = 0; i < _controlPoints.Count; i++)
            {
                var cp = _controlPoints[i];
                Vector3 pos = positions[i];

                // Calculate direction
                Vector3 forward;
                if (i == 0)
                    forward = (positions[1] - pos).normalized;
                else if (i == _controlPoints.Count - 1)
                    forward = (pos - positions[i - 1]).normalized;
                else
                    forward = (positions[i + 1] - positions[i - 1]).normalized;

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

                // Add vertices using pooled buffers - convert to LOCAL space
                Vector3 left = pos - right * halfWidth;
                Vector3 rightPos = pos + right * halfWidth;

                // Convert world positions to local space for proper shader transformation
                pool.Vertices.Add((left));
                pool.Vertices.Add((rightPos));

                // Normals (face camera) - convert to local space
                Vector3 normal = Vector3.Cross(right, forward).normalized;
                Vector3 localNormal = (normal);
                pool.Normals.Add(localNormal);
                pool.Normals.Add(localNormal);

                // UVs
                float v = totalLength > 0 ? currentLength / totalLength : 0f;
                pool.UVs.Add(new Vector2(0f, v));
                pool.UVs.Add(new Vector2(1f, v));

                // Colors with opacity
                float opacity = _brushData.GetPressuredOpacity(cp.Pressure);
                Color32 c = _color;
                c.a = (byte)(opacity * 255);
                pool.Colors.Add(c);
                pool.Colors.Add(c);

                // Update length
                if (i > 0)
                    currentLength += Vector3.Distance(pos, positions[i - 1]);
            }

            // Generate triangles
            for (int i = 0; i < _controlPoints.Count - 1; i++)
            {
                int baseIdx = i * 2;
                // First triangle
                pool.Triangles.Add(baseIdx);
                pool.Triangles.Add(baseIdx + 2);
                pool.Triangles.Add(baseIdx + 1);
                // Second triangle
                pool.Triangles.Add(baseIdx + 1);
                pool.Triangles.Add(baseIdx + 2);
                pool.Triangles.Add(baseIdx + 3);

                // Backfaces if enabled
                if (_brushData.RenderBackfaces)
                {
                    pool.Triangles.Add(baseIdx);
                    pool.Triangles.Add(baseIdx + 1);
                    pool.Triangles.Add(baseIdx + 2);
                    pool.Triangles.Add(baseIdx + 1);
                    pool.Triangles.Add(baseIdx + 3);
                    pool.Triangles.Add(baseIdx + 2);
                }
            }
        }

        #endregion

        #region Tube Generation

        void GenerateTube()
        {
            var pool = _geometryPool;
            pool.Clear();

            if (_controlPoints.Count < 2) return;

            int sides = _brushData.TubeSides;
            float angleStep = 2f * Mathf.PI / sides;

            // Pre-allocate capacity
            int vertCount = _controlPoints.Count * sides;
            int triCount = (_controlPoints.Count - 1) * sides * 6;
            pool.EnsureCapacity(vertCount, triCount);

            // Optional position smoothing
            List<Vector3> positions = GetSmoothedPositions();

            float totalLength = 0f;
            for (int i = 1; i < positions.Count; i++)
                totalLength += Vector3.Distance(positions[i], positions[i - 1]);

            float currentLength = 0f;

            // Reset parallel transport state at stroke start
            _lastTubeUp = Vector3.up;

            for (int i = 0; i < _controlPoints.Count; i++)
            {
                var cp = _controlPoints[i];
                Vector3 pos = positions[i];

                // Calculate direction (tangent)
                Vector3 forward;
                if (i == 0)
                    forward = (positions[1] - pos).normalized;
                else if (i == _controlPoints.Count - 1)
                    forward = (pos - positions[i - 1]).normalized;
                else
                    forward = (positions[i + 1] - positions[i - 1]).normalized;

                if (forward.sqrMagnitude < 0.001f)
                    forward = Vector3.forward;

                // Use parallel transport for consistent orientation (prevents twisting)
                Vector3 up = BrushMathUtils.ComputeMinimalRotationFrame(forward, _lastTubeUp);
                _lastTubeUp = up;

                Vector3 right = Vector3.Cross(forward, up).normalized;

                // Calculate radius with pressure
                float radius = _brushData.GetPressuredSize(_baseSize, cp.Pressure) * 0.5f;

                // Generate ring vertices
                for (int j = 0; j < sides; j++)
                {
                    float angle = j * angleStep;
                    Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * radius;
                    Vector3 vertPos = pos + offset;
                    Vector3 normal = offset.normalized;

                    // Convert to local space
                    pool.Vertices.Add((vertPos));
                    pool.Normals.Add((normal));

                    float u = (float)j / sides;
                    float v = totalLength > 0 ? currentLength / totalLength : 0f;
                    pool.UVs.Add(new Vector2(u, v));

                    float opacity = _brushData.GetPressuredOpacity(cp.Pressure);
                    Color32 c = _color;
                    c.a = (byte)(opacity * 255);
                    pool.Colors.Add(c);
                }

                if (i > 0)
                    currentLength += Vector3.Distance(pos, positions[i - 1]);
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
                    pool.Triangles.Add(ring1 + j);
                    pool.Triangles.Add(ring2 + j);
                    pool.Triangles.Add(ring1 + j1);

                    // Second triangle
                    pool.Triangles.Add(ring1 + j1);
                    pool.Triangles.Add(ring2 + j);
                    pool.Triangles.Add(ring2 + j1);
                }
            }
        }

        #endregion

        #region Hull Generation

        /// <summary>
        /// Generate convex hull geometry around control points.
        /// Based on Open Brush's Hull brush pattern.
        /// Uses FLAT shading (per-face normals) for faceted appearance.
        /// </summary>
        void GenerateHull()
        {
            var pool = _geometryPool;
            pool.Clear();

            if (_controlPoints.Count < 3) return;

            List<Vector3> positions = GetSmoothedPositions();
            float hullExpand = _baseSize * 0.5f;

            int segments = 6; // Hexagonal cross-section for faceted hull
            float angleStep = 2f * Mathf.PI / segments;

            // For FLAT shading, we need separate vertices per face (no vertex sharing)
            // Each quad face needs 4 unique vertices with the same normal
            int facesPerRing = segments;
            int rings = _controlPoints.Count - 1;
            int sideVertCount = rings * facesPerRing * 4; // 4 verts per quad face
            int capVertCount = segments * 3 * 2; // Triangles for caps
            int triCount = rings * facesPerRing * 6 + segments * 3 * 2;
            pool.EnsureCapacity(sideVertCount + capVertCount, triCount);

            _lastTubeUp = Vector3.up;

            // Pre-compute ring positions for all control points
            var ringPositions = new List<Vector3[]>();
            var ringForwards = new List<Vector3>();

            for (int i = 0; i < _controlPoints.Count; i++)
            {
                var cp = _controlPoints[i];
                Vector3 pos = positions[i];

                Vector3 forward;
                if (i == 0)
                    forward = (positions[1] - pos).normalized;
                else if (i == _controlPoints.Count - 1)
                    forward = (pos - positions[i - 1]).normalized;
                else
                    forward = (positions[i + 1] - positions[i - 1]).normalized;

                if (forward.sqrMagnitude < 0.001f)
                    forward = Vector3.forward;

                Vector3 up = BrushMathUtils.ComputeMinimalRotationFrame(forward, _lastTubeUp);
                _lastTubeUp = up;
                Vector3 right = Vector3.Cross(forward, up).normalized;

                float radius = _brushData.GetPressuredSize(_baseSize, cp.Pressure) * hullExpand;

                var ringVerts = new Vector3[segments];
                for (int j = 0; j < segments; j++)
                {
                    float angle = j * angleStep;
                    Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * radius;
                    ringVerts[j] = pos + offset;
                }

                ringPositions.Add(ringVerts);
                ringForwards.Add(forward);
            }

            // Generate FLAT-SHADED side faces (each quad has its own vertices with face normal)
            for (int i = 0; i < _controlPoints.Count - 1; i++)
            {
                var ring1 = ringPositions[i];
                var ring2 = ringPositions[i + 1];
                var cp = _controlPoints[i];

                float opacity = _brushData.GetPressuredOpacity(cp.Pressure);
                Color32 c = _color;
                c.a = (byte)(opacity * 255);

                for (int j = 0; j < segments; j++)
                {
                    int j1 = (j + 1) % segments;

                    // Quad corners
                    Vector3 v0 = ring1[j];
                    Vector3 v1 = ring2[j];
                    Vector3 v2 = ring2[j1];
                    Vector3 v3 = ring1[j1];

                    // Calculate FLAT face normal (same for all 4 vertices of this quad)
                    Vector3 edge1 = v1 - v0;
                    Vector3 edge2 = v3 - v0;
                    Vector3 faceNormal = Vector3.Cross(edge1, edge2).normalized;

                    int baseIdx = pool.Vertices.Count;

                    // Add 4 vertices for this quad (not shared with other faces)
                    pool.Vertices.Add(v0);
                    pool.Vertices.Add(v1);
                    pool.Vertices.Add(v2);
                    pool.Vertices.Add(v3);

                    // All 4 vertices get the SAME face normal (flat shading)
                    pool.Normals.Add(faceNormal);
                    pool.Normals.Add(faceNormal);
                    pool.Normals.Add(faceNormal);
                    pool.Normals.Add(faceNormal);

                    // UVs
                    float u0 = (float)j / segments;
                    float u1 = (float)(j + 1) / segments;
                    float v_i = (float)i / (_controlPoints.Count - 1);
                    float v_i1 = (float)(i + 1) / (_controlPoints.Count - 1);
                    pool.UVs.Add(new Vector2(u0, v_i));
                    pool.UVs.Add(new Vector2(u0, v_i1));
                    pool.UVs.Add(new Vector2(u1, v_i1));
                    pool.UVs.Add(new Vector2(u1, v_i));

                    // Colors
                    pool.Colors.Add(c);
                    pool.Colors.Add(c);
                    pool.Colors.Add(c);
                    pool.Colors.Add(c);

                    // Triangles for this quad
                    pool.Triangles.Add(baseIdx);
                    pool.Triangles.Add(baseIdx + 1);
                    pool.Triangles.Add(baseIdx + 2);
                    pool.Triangles.Add(baseIdx);
                    pool.Triangles.Add(baseIdx + 2);
                    pool.Triangles.Add(baseIdx + 3);
                }
            }

            // Generate FLAT-SHADED end caps
            // Start cap
            Vector3 startCenter = positions[0];
            Vector3 startNormal = -ringForwards[0];
            var startRing = ringPositions[0];

            for (int j = 0; j < segments; j++)
            {
                int j1 = (j + 1) % segments;
                int baseIdx = pool.Vertices.Count;

                pool.Vertices.Add(startCenter);
                pool.Vertices.Add(startRing[j1]);
                pool.Vertices.Add(startRing[j]);

                pool.Normals.Add(startNormal);
                pool.Normals.Add(startNormal);
                pool.Normals.Add(startNormal);

                pool.UVs.Add(new Vector2(0.5f, 0f));
                pool.UVs.Add(new Vector2((float)(j + 1) / segments, 0f));
                pool.UVs.Add(new Vector2((float)j / segments, 0f));

                pool.Colors.Add(_color);
                pool.Colors.Add(_color);
                pool.Colors.Add(_color);

                pool.Triangles.Add(baseIdx);
                pool.Triangles.Add(baseIdx + 1);
                pool.Triangles.Add(baseIdx + 2);
            }

            // End cap
            Vector3 endCenter = positions[^1];
            Vector3 endNormal = ringForwards[^1];
            var endRing = ringPositions[^1];

            for (int j = 0; j < segments; j++)
            {
                int j1 = (j + 1) % segments;
                int baseIdx = pool.Vertices.Count;

                pool.Vertices.Add(endCenter);
                pool.Vertices.Add(endRing[j]);
                pool.Vertices.Add(endRing[j1]);

                pool.Normals.Add(endNormal);
                pool.Normals.Add(endNormal);
                pool.Normals.Add(endNormal);

                pool.UVs.Add(new Vector2(0.5f, 1f));
                pool.UVs.Add(new Vector2((float)j / segments, 1f));
                pool.UVs.Add(new Vector2((float)(j + 1) / segments, 1f));

                pool.Colors.Add(_color);
                pool.Colors.Add(_color);
                pool.Colors.Add(_color);

                pool.Triangles.Add(baseIdx);
                pool.Triangles.Add(baseIdx + 1);
                pool.Triangles.Add(baseIdx + 2);
            }
        }

        #endregion

        #region Particle Generation

        /// <summary>
        /// Generate billboard quads at control points for particle-style brushes.
        /// Based on Open Brush's particle brush pattern.
        /// </summary>
        void GenerateParticles()
        {
            var pool = _geometryPool;
            pool.Clear();

            if (_controlPoints.Count < 1) return;

            // Each particle is a camera-facing quad
            int vertCount = _controlPoints.Count * 4;
            int triCount = _controlPoints.Count * 6;
            pool.EnsureCapacity(vertCount, triCount);

            float sizeVariance = _brushData.SizeVariance;
            System.Random rng = new System.Random(_brushData.BrushId.GetHashCode());

            for (int i = 0; i < _controlPoints.Count; i++)
            {
                var cp = _controlPoints[i];

                // Size with variance
                float variance = 1f + (float)(rng.NextDouble() * 2 - 1) * sizeVariance;
                float size = _brushData.GetPressuredSize(_baseSize, cp.Pressure) * variance;
                float halfSize = size * 0.5f;

                // Random rotation for variety
                float rotation = (float)(rng.NextDouble() * _brushData.ParticleRotationRange * Mathf.Deg2Rad);
                float cos = Mathf.Cos(rotation);
                float sin = Mathf.Sin(rotation);

                // Billboard corners (will be oriented by shader or CPU)
                Vector3 right = new Vector3(cos, sin, 0) * halfSize;
                Vector3 up = new Vector3(-sin, cos, 0) * halfSize;

                int baseIdx = i * 4;
                Vector3 pos = cp.Position;

                // Quad vertices (local offset, shader should billboard)
                pool.Vertices.Add((pos + (-right - up)));
                pool.Vertices.Add((pos + (right - up)));
                pool.Vertices.Add((pos + (right + up)));
                pool.Vertices.Add((pos + (-right + up)));

                // Normals pointing at camera (placeholder, shader handles)
                Vector3 normal = Vector3.back;
                pool.Normals.Add(normal);
                pool.Normals.Add(normal);
                pool.Normals.Add(normal);
                pool.Normals.Add(normal);

                // UVs for sprite/texture
                pool.UVs.Add(new Vector2(0, 0));
                pool.UVs.Add(new Vector2(1, 0));
                pool.UVs.Add(new Vector2(1, 1));
                pool.UVs.Add(new Vector2(0, 1));

                // Colors with opacity
                float opacity = _brushData.GetPressuredOpacity(cp.Pressure);
                Color32 c = _color;
                c.a = (byte)(opacity * 255);
                pool.Colors.Add(c);
                pool.Colors.Add(c);
                pool.Colors.Add(c);
                pool.Colors.Add(c);

                // Triangles
                pool.Triangles.Add(baseIdx);
                pool.Triangles.Add(baseIdx + 1);
                pool.Triangles.Add(baseIdx + 2);
                pool.Triangles.Add(baseIdx);
                pool.Triangles.Add(baseIdx + 2);
                pool.Triangles.Add(baseIdx + 3);
            }
        }

        #endregion

        #region Spray Generation

        /// <summary>
        /// Generate scattered quads around stroke path for spray/splatter effects.
        /// Based on Open Brush's spray brush pattern.
        /// </summary>
        void GenerateSpray()
        {
            var pool = _geometryPool;
            pool.Clear();

            if (_controlPoints.Count < 2) return;

            List<Vector3> positions = GetSmoothedPositions();

            // Calculate total path length for spray distribution
            float totalLength = 0f;
            for (int i = 1; i < positions.Count; i++)
                totalLength += Vector3.Distance(positions[i], positions[i - 1]);

            // Spray rate determines particle count
            float sprayRate = _brushData.ParticleRate;
            int particleCount = Mathf.Max(1, Mathf.CeilToInt(totalLength * sprayRate));

            int vertCount = particleCount * 4;
            int triCount = particleCount * 6;
            pool.EnsureCapacity(vertCount, triCount);

            System.Random rng = new System.Random(_brushData.BrushId.GetHashCode() + _controlPoints.Count);
            float sizeVariance = _brushData.SizeVariance;

            float currentLength = 0f;
            int segmentIdx = 0;

            for (int p = 0; p < particleCount; p++)
            {
                // Position along path
                float t = (float)p / particleCount;
                float targetLength = t * totalLength;

                // Find segment
                while (segmentIdx < positions.Count - 2 && currentLength + Vector3.Distance(positions[segmentIdx], positions[segmentIdx + 1]) < targetLength)
                {
                    currentLength += Vector3.Distance(positions[segmentIdx], positions[segmentIdx + 1]);
                    segmentIdx++;
                }

                float segmentLength = Vector3.Distance(positions[segmentIdx], positions[segmentIdx + 1]);
                float segmentT = segmentLength > 0 ? (targetLength - currentLength) / segmentLength : 0f;
                Vector3 basePos = Vector3.Lerp(positions[segmentIdx], positions[segmentIdx + 1], segmentT);

                // Interpolate control point data
                int cpIdx = Mathf.Min(segmentIdx, _controlPoints.Count - 1);
                var cp = _controlPoints[cpIdx];

                // Random offset perpendicular to path
                Vector3 forward = (positions[Mathf.Min(segmentIdx + 1, positions.Count - 1)] - positions[segmentIdx]).normalized;
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

                Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
                if (right.sqrMagnitude < 0.001f) right = Vector3.right;
                Vector3 up = Vector3.Cross(right, forward).normalized;

                // Spray spread
                float spreadRadius = _baseSize * 2f;
                float angle = (float)(rng.NextDouble() * 2 * Mathf.PI);
                float dist = (float)(rng.NextDouble() * spreadRadius);
                Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * dist;
                Vector3 pos = basePos + offset;

                // Size with variance
                float variance = 1f + (float)(rng.NextDouble() * 2 - 1) * sizeVariance;
                float size = _brushData.GetPressuredSize(_baseSize, cp.Pressure) * variance * 0.5f;
                float halfSize = size * 0.5f;

                // Random rotation
                float rotation = (float)(rng.NextDouble() * _brushData.ParticleRotationRange * Mathf.Deg2Rad);
                float cos = Mathf.Cos(rotation);
                float sin = Mathf.Sin(rotation);

                Vector3 quadRight = (cos * right + sin * up) * halfSize;
                Vector3 quadUp = (-sin * right + cos * up) * halfSize;

                int baseIdx = p * 4;

                // Quad vertices - convert to local space
                pool.Vertices.Add((pos + (-quadRight - quadUp)));
                pool.Vertices.Add((pos + (quadRight - quadUp)));
                pool.Vertices.Add((pos + (quadRight + quadUp)));
                pool.Vertices.Add((pos + (-quadRight + quadUp)));

                // Normals
                Vector3 normal = -forward;
                pool.Normals.Add(normal);
                pool.Normals.Add(normal);
                pool.Normals.Add(normal);
                pool.Normals.Add(normal);

                // UVs
                pool.UVs.Add(new Vector2(0, 0));
                pool.UVs.Add(new Vector2(1, 0));
                pool.UVs.Add(new Vector2(1, 1));
                pool.UVs.Add(new Vector2(0, 1));

                // Colors
                float opacity = _brushData.GetPressuredOpacity(cp.Pressure) * (float)(0.5 + rng.NextDouble() * 0.5);
                Color32 c = _color;
                c.a = (byte)(opacity * 255);
                pool.Colors.Add(c);
                pool.Colors.Add(c);
                pool.Colors.Add(c);
                pool.Colors.Add(c);

                // Triangles
                pool.Triangles.Add(baseIdx);
                pool.Triangles.Add(baseIdx + 1);
                pool.Triangles.Add(baseIdx + 2);
                pool.Triangles.Add(baseIdx);
                pool.Triangles.Add(baseIdx + 2);
                pool.Triangles.Add(baseIdx + 3);
            }
        }

        #endregion

        #region Slice Generation

        /// <summary>
        /// Generate motion-aligned quads at each control point.
        /// Based on Open Brush's SliceBrush pattern.
        /// Each quad's normal is the direction of motion (tangent).
        /// </summary>
        void GenerateSlice()
        {
            var pool = _geometryPool;
            pool.Clear();

            if (_controlPoints.Count < 2) return;

            List<Vector3> positions = GetSmoothedPositions();

            int vertCount = _controlPoints.Count * 4;
            int triCount = _controlPoints.Count * 6;
            pool.EnsureCapacity(vertCount, triCount);

            _lastTubeUp = Vector3.up;

            for (int i = 0; i < _controlPoints.Count; i++)
            {
                var cp = _controlPoints[i];
                Vector3 pos = positions[i];

                // Calculate tangent (direction of motion)
                Vector3 tangent;
                if (i == 0)
                    tangent = (positions[1] - pos).normalized;
                else if (i == _controlPoints.Count - 1)
                    tangent = (pos - positions[i - 1]).normalized;
                else
                    tangent = (positions[i + 1] - positions[i - 1]).normalized;

                if (tangent.sqrMagnitude < 0.001f)
                    tangent = Vector3.forward;

                // Use parallel transport for consistent up vector
                Vector3 up = BrushMathUtils.ComputeMinimalRotationFrame(tangent, _lastTubeUp);
                _lastTubeUp = up;
                Vector3 right = Vector3.Cross(tangent, up).normalized;

                // Size with pressure
                float size = _brushData.GetPressuredSize(_baseSize, cp.Pressure);
                float halfSize = size * 0.5f;

                int baseIdx = i * 4;

                // Quad perpendicular to motion direction (normal = tangent)
                Vector3 v0 = pos + (-right - up) * halfSize;
                Vector3 v1 = pos + (right - up) * halfSize;
                Vector3 v2 = pos + (right + up) * halfSize;
                Vector3 v3 = pos + (-right + up) * halfSize;

                // Convert to local space
                pool.Vertices.Add((v0));
                pool.Vertices.Add((v1));
                pool.Vertices.Add((v2));
                pool.Vertices.Add((v3));

                // Normals point along motion direction
                pool.Normals.Add(tangent);
                pool.Normals.Add(tangent);
                pool.Normals.Add(tangent);
                pool.Normals.Add(tangent);

                // UVs
                float v = (float)i / Mathf.Max(1, _controlPoints.Count - 1);
                pool.UVs.Add(new Vector2(0, v));
                pool.UVs.Add(new Vector2(1, v));
                pool.UVs.Add(new Vector2(1, v));
                pool.UVs.Add(new Vector2(0, v));

                // Colors with opacity
                float opacity = _brushData.GetPressuredOpacity(cp.Pressure);
                Color32 c = _color;
                c.a = (byte)(opacity * 255);
                pool.Colors.Add(c);
                pool.Colors.Add(c);
                pool.Colors.Add(c);
                pool.Colors.Add(c);

                // Triangles (double-sided for slice visibility from both directions)
                // Front face
                pool.Triangles.Add(baseIdx);
                pool.Triangles.Add(baseIdx + 1);
                pool.Triangles.Add(baseIdx + 2);
                pool.Triangles.Add(baseIdx);
                pool.Triangles.Add(baseIdx + 2);
                pool.Triangles.Add(baseIdx + 3);

                // Back face (if needed for two-sided rendering)
                if (_brushData.RenderBackfaces)
                {
                    pool.Triangles.Add(baseIdx);
                    pool.Triangles.Add(baseIdx + 2);
                    pool.Triangles.Add(baseIdx + 1);
                    pool.Triangles.Add(baseIdx);
                    pool.Triangles.Add(baseIdx + 3);
                    pool.Triangles.Add(baseIdx + 2);
                }
            }
        }

        #endregion

        void ApplyMesh()
        {
            // Apply pooled geometry to mesh
            _geometryPool.ApplyToMesh(_mesh);
        }

        /// <summary>
        /// Get smoothed positions using kernel averaging
        /// </summary>
        List<Vector3> GetSmoothedPositions()
        {
            var result = new List<Vector3>(_controlPoints.Count);

            if (!_enablePositionSmoothing || _controlPoints.Count < 3)
            {
                // No smoothing - return raw positions
                foreach (var cp in _controlPoints)
                    result.Add(cp.Position);
                return result;
            }

            // Apply (.25, .5, .25) smoothing kernel
            result.Add(_controlPoints[0].Position); // First point unchanged

            for (int i = 1; i < _controlPoints.Count - 1; i++)
            {
                Vector3 smoothed = BrushMathUtils.SmoothPosition(
                    _controlPoints[i - 1].Position,
                    _controlPoints[i].Position,
                    _controlPoints[i + 1].Position);
                result.Add(smoothed);
            }

            result.Add(_controlPoints[^1].Position); // Last point unchanged

            return result;
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

            if (_meshRenderer != null)
            {
                Material mat = CreateMaterial(brushData, _color);
                _meshRenderer.material = mat;
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
