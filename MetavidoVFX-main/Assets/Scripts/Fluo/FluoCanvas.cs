// FluoCanvas - Fluid simulation & canvas system for Fluo VFX
// Based on keijiro/Fluo-GHURT StableFluids implementation

using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;

namespace MetavidoVFX.Fluo
{
    public class FluoCanvas : MonoBehaviour
    {
        [Header("Canvas Settings")]
        [SerializeField] private int resolution = 512;
        [SerializeField] private float alphaDecay = 0.98f;
        [SerializeField] private float viscosity = 0.1f;
        [SerializeField] private float diffusion = 0.1f;

        [Header("Brush")]
        [SerializeField] private float brushRadius = 0.1f;
        [SerializeField] private Color brushColor = Color.white;
        [SerializeField] private float brushForce = 10f;

        [Header("Forcefield")]
        [SerializeField] private bool enableForcefield;
        [SerializeField] private float forcefieldStrength = 1f;
        [SerializeField] private Vector2 forcefieldCenter = Vector2.one * 0.5f;

        [Header("References")]
        [SerializeField] private ComputeShader fluidCompute;
        [SerializeField] private List<VisualEffect> targetVFX = new();

        // Render textures
        private RenderTexture _colorCanvas;
        private RenderTexture _velocityField;
        private RenderTexture _pressureField;
        private RenderTexture _divergenceField;

        // Compute kernels
        private int _advectKernel;
        private int _diffuseKernel;
        private int _projectKernel;
        private int _applyForceKernel;
        private int _decayKernel;

        // Input state
        private Vector2 _lastBrushPos;
        private bool _isBrushing;

        public RenderTexture ColorCanvas => _colorCanvas;
        public RenderTexture VelocityField => _velocityField;

        void OnEnable()
        {
            InitializeTextures();
            InitializeCompute();
        }

        void OnDisable()
        {
            ReleaseTextures();
        }

        void Update()
        {
            ProcessInput();
            RunFluidSimulation();
            BindToVFX();
        }

        void InitializeTextures()
        {
            _colorCanvas = CreateRT(RenderTextureFormat.ARGBFloat);
            _velocityField = CreateRT(RenderTextureFormat.RGFloat);
            _pressureField = CreateRT(RenderTextureFormat.RFloat);
            _divergenceField = CreateRT(RenderTextureFormat.RFloat);
        }

        RenderTexture CreateRT(RenderTextureFormat format)
        {
            var rt = new RenderTexture(resolution, resolution, 0, format)
            {
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            rt.Create();
            return rt;
        }

        void ReleaseTextures()
        {
            if (_colorCanvas) _colorCanvas.Release();
            if (_velocityField) _velocityField.Release();
            if (_pressureField) _pressureField.Release();
            if (_divergenceField) _divergenceField.Release();
        }

        void InitializeCompute()
        {
            if (fluidCompute == null)
            {
                // Try to load default compute shader
                fluidCompute = Resources.Load<ComputeShader>("FluoFluid");
                if (fluidCompute == null)
                {
                    Debug.LogWarning("[FluoCanvas] No compute shader assigned. Using simplified simulation.");
                    return;
                }
            }

            _advectKernel = fluidCompute.FindKernel("Advect");
            _diffuseKernel = fluidCompute.FindKernel("Diffuse");
            _projectKernel = fluidCompute.FindKernel("Project");
            _applyForceKernel = fluidCompute.FindKernel("ApplyForce");
            _decayKernel = fluidCompute.FindKernel("Decay");
        }

        void ProcessInput()
        {
            // Touch/mouse input for brushing
            if (Input.GetMouseButton(0))
            {
                Vector2 pos = new Vector2(
                    Input.mousePosition.x / Screen.width,
                    Input.mousePosition.y / Screen.height
                );

                if (_isBrushing)
                {
                    Vector2 delta = pos - _lastBrushPos;
                    ApplyBrush(pos, delta * brushForce);
                }

                _lastBrushPos = pos;
                _isBrushing = true;
            }
            else
            {
                _isBrushing = false;
            }

            // Forcefield
            if (enableForcefield)
            {
                ApplyForcefield();
            }
        }

        void ApplyBrush(Vector2 pos, Vector2 velocity)
        {
            if (fluidCompute == null) return;

            fluidCompute.SetTexture(_applyForceKernel, "VelocityField", _velocityField);
            fluidCompute.SetTexture(_applyForceKernel, "ColorCanvas", _colorCanvas);
            fluidCompute.SetVector("BrushPos", pos);
            fluidCompute.SetVector("BrushVelocity", velocity);
            fluidCompute.SetFloat("BrushRadius", brushRadius);
            fluidCompute.SetVector("BrushColor", brushColor);

            int groups = Mathf.CeilToInt(resolution / 8f);
            fluidCompute.Dispatch(_applyForceKernel, groups, groups, 1);
        }

        void ApplyForcefield()
        {
            // Radial forcefield from center
            if (fluidCompute == null) return;

            fluidCompute.SetTexture(_applyForceKernel, "VelocityField", _velocityField);
            fluidCompute.SetVector("ForcefieldCenter", forcefieldCenter);
            fluidCompute.SetFloat("ForcefieldStrength", forcefieldStrength);

            int groups = Mathf.CeilToInt(resolution / 8f);
            fluidCompute.Dispatch(_applyForceKernel, groups, groups, 1);
        }

        void RunFluidSimulation()
        {
            if (fluidCompute == null)
            {
                // Simplified decay without compute shader
                DecaySimple();
                return;
            }

            int groups = Mathf.CeilToInt(resolution / 8f);

            // Diffuse velocity
            fluidCompute.SetTexture(_diffuseKernel, "VelocityField", _velocityField);
            fluidCompute.SetFloat("Viscosity", viscosity);
            for (int i = 0; i < 20; i++)
                fluidCompute.Dispatch(_diffuseKernel, groups, groups, 1);

            // Project (make velocity divergence-free)
            fluidCompute.SetTexture(_projectKernel, "VelocityField", _velocityField);
            fluidCompute.SetTexture(_projectKernel, "PressureField", _pressureField);
            fluidCompute.SetTexture(_projectKernel, "DivergenceField", _divergenceField);
            for (int i = 0; i < 40; i++)
                fluidCompute.Dispatch(_projectKernel, groups, groups, 1);

            // Advect color by velocity
            fluidCompute.SetTexture(_advectKernel, "ColorCanvas", _colorCanvas);
            fluidCompute.SetTexture(_advectKernel, "VelocityField", _velocityField);
            fluidCompute.SetFloat("DeltaTime", Time.deltaTime);
            fluidCompute.Dispatch(_advectKernel, groups, groups, 1);

            // Decay alpha
            fluidCompute.SetTexture(_decayKernel, "ColorCanvas", _colorCanvas);
            fluidCompute.SetFloat("AlphaDecay", alphaDecay);
            fluidCompute.Dispatch(_decayKernel, groups, groups, 1);
        }

        void DecaySimple()
        {
            // Simple GPU decay using shader
            Shader.SetGlobalFloat("_Fluo_CanvasAlphaDecay", alphaDecay);
        }

        void BindToVFX()
        {
            foreach (var vfx in targetVFX)
            {
                if (vfx == null) continue;

                if (vfx.HasTexture("BrushColor"))
                    vfx.SetTexture("BrushColor", _colorCanvas);
                if (vfx.HasTexture("BrushMotion"))
                    vfx.SetTexture("BrushMotion", _velocityField);
            }

            // Also set global shader properties
            Shader.SetGlobalTexture("_Fluo_BrushColor", _colorCanvas);
            Shader.SetGlobalTexture("_Fluo_BrushMotion", _velocityField);
        }

        // Public API
        public void SetBrushColor(Color color) => brushColor = color;
        public void SetBrushRadius(float radius) => brushRadius = Mathf.Clamp01(radius);
        public void SetForcefieldStrength(float strength) => forcefieldStrength = strength;
        public void SetForcefieldCenter(Vector2 center) => forcefieldCenter = center;
        public void ClearCanvas()
        {
            Graphics.SetRenderTarget(_colorCanvas);
            GL.Clear(true, true, Color.clear);
            Graphics.SetRenderTarget(_velocityField);
            GL.Clear(true, true, Color.clear);
            Graphics.SetRenderTarget(null);
        }

        public void AddVFX(VisualEffect vfx)
        {
            if (!targetVFX.Contains(vfx))
                targetVFX.Add(vfx);
        }
    }
}
