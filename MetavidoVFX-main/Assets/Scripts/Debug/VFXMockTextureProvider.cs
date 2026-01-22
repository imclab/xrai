using UnityEngine;
using UnityEngine.VFX;

namespace XRRAI.Debugging
{
    /// <summary>
    /// Provides mock textures for VFX testing in Editor without AR device.
    /// Generates procedural depth, color, stencil, and position maps.
    /// Useful for developing and debugging VFX graphs.
    /// </summary>
    [ExecuteAlways]
    public class VFXMockTextureProvider : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private VisualEffect _targetVFX;
        [SerializeField] private bool _autoFindVFX = true;

        [Header("Mock Data Settings")]
        [SerializeField] private int _textureSize = 256;
        [SerializeField] private MockPattern _pattern = MockPattern.AnimatedWave;
        [SerializeField] private float _animationSpeed = 1f;
        [SerializeField] private float _depthMin = 0.5f;
        [SerializeField] private float _depthMax = 3f;

        [Header("Stencil")]
        [SerializeField] private bool _generateStencil = true;
        [SerializeField] private float _stencilRadius = 0.3f;
        [SerializeField] private Vector2 _stencilCenter = new Vector2(0.5f, 0.5f);

        [Header("Debug")]
        [SerializeField] private bool _showPreview = true;
        [SerializeField] private float _previewSize = 128f;

        public enum MockPattern
        {
            StaticNoise,
            AnimatedWave,
            CircularGradient,
            Checkerboard,
            HumanSilhouette
        }

        // Generated textures
        private RenderTexture _colorMap;
        private RenderTexture _depthMap;
        private RenderTexture _stencilMap;
        private RenderTexture _positionMap;
        private Texture2D _cpuColorMap;
        private Texture2D _cpuDepthMap;
        private Texture2D _cpuStencilMap;

        // Camera simulation
        private Matrix4x4 _inverseView = Matrix4x4.identity;
        private Vector4 _rayParams = new Vector4(0, 0, 1, 1);
        private Vector2 _depthRange;

        private float _time;

        private void OnEnable()
        {
            CreateTextures();
            if (_autoFindVFX && _targetVFX == null)
            {
                _targetVFX = GetComponent<VisualEffect>();
            }
        }

        private void OnDisable()
        {
            ReleaseTextures();
        }

        private void Update()
        {
            _time += Time.deltaTime * _animationSpeed;
            UpdateTextures();
            BindToVFX();
        }

        private void CreateTextures()
        {
            ReleaseTextures();

            _colorMap = new RenderTexture(_textureSize, _textureSize, 0, RenderTextureFormat.ARGB32);
            _colorMap.name = "MockColorMap";
            _colorMap.Create();

            _depthMap = new RenderTexture(_textureSize, _textureSize, 0, RenderTextureFormat.RFloat);
            _depthMap.name = "MockDepthMap";
            _depthMap.Create();

            _stencilMap = new RenderTexture(_textureSize, _textureSize, 0, RenderTextureFormat.R8);
            _stencilMap.name = "MockStencilMap";
            _stencilMap.Create();

            _positionMap = new RenderTexture(_textureSize, _textureSize, 0, RenderTextureFormat.ARGBFloat);
            _positionMap.name = "MockPositionMap";
            _positionMap.enableRandomWrite = true;
            _positionMap.Create();

            _cpuColorMap = new Texture2D(_textureSize, _textureSize, TextureFormat.RGBA32, false);
            _cpuDepthMap = new Texture2D(_textureSize, _textureSize, TextureFormat.RFloat, false);
            _cpuStencilMap = new Texture2D(_textureSize, _textureSize, TextureFormat.R8, false);

            _depthRange = new Vector2(_depthMin, _depthMax);
        }

        private void ReleaseTextures()
        {
            if (_colorMap != null) { _colorMap.Release(); DestroyImmediate(_colorMap); }
            if (_depthMap != null) { _depthMap.Release(); DestroyImmediate(_depthMap); }
            if (_stencilMap != null) { _stencilMap.Release(); DestroyImmediate(_stencilMap); }
            if (_positionMap != null) { _positionMap.Release(); DestroyImmediate(_positionMap); }
            if (_cpuColorMap != null) DestroyImmediate(_cpuColorMap);
            if (_cpuDepthMap != null) DestroyImmediate(_cpuDepthMap);
            if (_cpuStencilMap != null) DestroyImmediate(_cpuStencilMap);
        }

        private void UpdateTextures()
        {
            if (_cpuColorMap == null || _cpuDepthMap == null) return;

            Color[] colors = new Color[_textureSize * _textureSize];
            Color[] depths = new Color[_textureSize * _textureSize];
            Color[] stencils = new Color[_textureSize * _textureSize];

            for (int y = 0; y < _textureSize; y++)
            {
                for (int x = 0; x < _textureSize; x++)
                {
                    float u = (float)x / _textureSize;
                    float v = (float)y / _textureSize;
                    int i = y * _textureSize + x;

                    // Generate based on pattern
                    float depth;
                    Color color;
                    float stencil;

                    switch (_pattern)
                    {
                        case MockPattern.AnimatedWave:
                            depth = Mathf.Lerp(_depthMin, _depthMax,
                                0.5f + 0.3f * Mathf.Sin(u * 6f + _time) * Mathf.Cos(v * 6f + _time * 0.7f));
                            color = new Color(
                                0.5f + 0.5f * Mathf.Sin(u * 10f + _time),
                                0.5f + 0.5f * Mathf.Cos(v * 10f + _time * 1.3f),
                                0.5f + 0.5f * Mathf.Sin((u + v) * 5f + _time * 0.5f),
                                1f);
                            break;

                        case MockPattern.CircularGradient:
                            float dist = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
                            depth = Mathf.Lerp(_depthMin, _depthMax, dist * 2f);
                            color = Color.HSVToRGB(dist + _time * 0.1f, 0.8f, 0.9f);
                            break;

                        case MockPattern.Checkerboard:
                            int cx = Mathf.FloorToInt(u * 8);
                            int cy = Mathf.FloorToInt(v * 8);
                            bool isWhite = (cx + cy) % 2 == 0;
                            depth = isWhite ? _depthMin : _depthMax;
                            color = isWhite ? Color.white : Color.black;
                            break;

                        case MockPattern.HumanSilhouette:
                            float headDist = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.8f));
                            float bodyDist = Mathf.Abs(u - 0.5f) + Mathf.Abs(v - 0.4f) * 0.5f;
                            bool isHuman = headDist < 0.12f || (bodyDist < 0.15f && v > 0.1f && v < 0.7f);
                            depth = isHuman ? _depthMin + 0.5f : _depthMax;
                            color = isHuman ? new Color(0.9f, 0.7f, 0.6f) : new Color(0.2f, 0.3f, 0.4f);
                            break;

                        default: // StaticNoise
                            depth = Mathf.Lerp(_depthMin, _depthMax, Mathf.PerlinNoise(u * 10f, v * 10f));
                            color = new Color(
                                Mathf.PerlinNoise(u * 20f, v * 20f + 100),
                                Mathf.PerlinNoise(u * 20f + 100, v * 20f),
                                Mathf.PerlinNoise(u * 20f + 200, v * 20f + 200),
                                1f);
                            break;
                    }

                    // Stencil (human mask)
                    if (_generateStencil)
                    {
                        float stencilDist = Vector2.Distance(new Vector2(u, v), _stencilCenter);
                        stencil = stencilDist < _stencilRadius ? 1f : 0f;
                    }
                    else
                    {
                        stencil = 1f;
                    }

                    colors[i] = color;
                    depths[i] = new Color(depth, 0, 0, 1);
                    stencils[i] = new Color(stencil, 0, 0, 1);
                }
            }

            _cpuColorMap.SetPixels(colors);
            _cpuColorMap.Apply();
            Graphics.Blit(_cpuColorMap, _colorMap);

            _cpuDepthMap.SetPixels(depths);
            _cpuDepthMap.Apply();
            Graphics.Blit(_cpuDepthMap, _depthMap);

            if (_generateStencil)
            {
                _cpuStencilMap.SetPixels(stencils);
                _cpuStencilMap.Apply();
                Graphics.Blit(_cpuStencilMap, _stencilMap);
            }

            // Update camera simulation
            _inverseView = Camera.main != null ? Camera.main.cameraToWorldMatrix : Matrix4x4.identity;
            float fov = Camera.main != null ? Camera.main.fieldOfView * Mathf.Deg2Rad : 60f * Mathf.Deg2Rad;
            float aspect = Camera.main != null ? Camera.main.aspect : 16f / 9f;
            _rayParams = new Vector4(0, 0, Mathf.Tan(fov / 2f) * aspect, Mathf.Tan(fov / 2f));
        }

        private void BindToVFX()
        {
            if (_targetVFX == null) return;

            // Bind textures
            if (_targetVFX.HasTexture("ColorMap"))
                _targetVFX.SetTexture("ColorMap", _colorMap);

            if (_targetVFX.HasTexture("DepthMap"))
                _targetVFX.SetTexture("DepthMap", _depthMap);

            if (_targetVFX.HasTexture("StencilMap"))
                _targetVFX.SetTexture("StencilMap", _stencilMap);

            if (_targetVFX.HasTexture("PositionMap"))
                _targetVFX.SetTexture("PositionMap", _positionMap);

            // Bind camera data
            if (_targetVFX.HasMatrix4x4("InverseView"))
                _targetVFX.SetMatrix4x4("InverseView", _inverseView);

            if (_targetVFX.HasVector4("RayParams"))
                _targetVFX.SetVector4("RayParams", _rayParams);

            if (_targetVFX.HasVector2("DepthRange"))
                _targetVFX.SetVector2("DepthRange", _depthRange);
        }

        private void OnGUI()
        {
            if (!_showPreview || !Application.isPlaying) return;

            float x = 10;
            float y = Screen.height - _previewSize - 10;

            if (_colorMap != null)
            {
                GUI.DrawTexture(new Rect(x, y, _previewSize, _previewSize), _colorMap);
                GUI.Label(new Rect(x, y - 20, _previewSize, 20), "ColorMap");
                x += _previewSize + 5;
            }

            if (_depthMap != null)
            {
                GUI.DrawTexture(new Rect(x, y, _previewSize, _previewSize), _depthMap);
                GUI.Label(new Rect(x, y - 20, _previewSize, 20), "DepthMap");
                x += _previewSize + 5;
            }

            if (_stencilMap != null && _generateStencil)
            {
                GUI.DrawTexture(new Rect(x, y, _previewSize, _previewSize), _stencilMap);
                GUI.Label(new Rect(x, y - 20, _previewSize, 20), "StencilMap");
            }
        }
    }
}
