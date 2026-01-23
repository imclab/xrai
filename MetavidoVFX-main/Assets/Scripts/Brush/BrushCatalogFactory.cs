// BrushCatalogFactory.cs - Creates Open Brush compatible brush catalog
// Part of Spec 011: Open Brush Integration
//
// Full catalog of 100 brushes matching Open Brush (54 standard + 46 experimental)
// Sources: https://docs.openbrush.app/user-guide/brushes/brush-list
//          https://github.com/icosa-foundation/open-brush-toolkit

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// Factory for creating brushes compatible with Open Brush.
    /// Includes all 100 brushes (54 standard + 46 experimental).
    /// </summary>
    public static class BrushCatalogFactory
    {
        // Cached shader references
        private static Shader _strokeShader;
        private static Shader _glowShader;
        private static Shader _tubeShader;
        private static Shader _hullShader;
        private static Shader _particleShader;

        /// <summary>
        /// Assigns appropriate material to brush based on category and geometry type.
        /// </summary>
        public static void AssignMaterial(BrushData brush)
        {
            if (brush == null) return;

            // Lazy load shaders - try Resources.Load first (more reliable), then Shader.Find
            if (_strokeShader == null)
            {
                _strokeShader = Resources.Load<Shader>("Shaders/BrushStroke");
                if (_strokeShader == null)
                    _strokeShader = Shader.Find("XRRAI/BrushStroke");
                if (_strokeShader == null)
                    Debug.LogWarning("[BrushCatalogFactory] XRRAI/BrushStroke shader not found!");
            }
            if (_glowShader == null)
            {
                _glowShader = Resources.Load<Shader>("Shaders/BrushStrokeGlow");
                if (_glowShader == null)
                    _glowShader = Shader.Find("XRRAI/BrushStrokeGlow");
            }
            if (_tubeShader == null)
            {
                _tubeShader = Resources.Load<Shader>("Shaders/BrushStrokeTube");
                if (_tubeShader == null)
                    _tubeShader = Shader.Find("XRRAI/BrushStrokeTube");
            }
            if (_hullShader == null)
            {
                _hullShader = Resources.Load<Shader>("Shaders/BrushStrokeHull");
                if (_hullShader == null)
                    _hullShader = Shader.Find("XRRAI/BrushStrokeHull");
            }
            if (_particleShader == null)
            {
                _particleShader = Resources.Load<Shader>("Shaders/BrushStrokeParticle");
                if (_particleShader == null)
                    _particleShader = Shader.Find("XRRAI/BrushStrokeParticle");
            }

            // Fallback chain - try multiple options
            Shader fallback = Shader.Find("Universal Render Pipeline/Unlit");
            if (fallback == null) fallback = Shader.Find("Universal Render Pipeline/Lit");
            if (fallback == null) fallback = Shader.Find("Unlit/Color");
            if (fallback == null) fallback = Shader.Find("Standard");

            if (fallback == null)
                Debug.LogError("[BrushCatalogFactory] No fallback shader found!");

            Material mat = null;
            Shader selectedShader = null;
            string shaderReason = "";

            // Select shader based on GEOMETRY TYPE FIRST (more specific), then category
            // This ensures Tube brushes get tube shader even if they're also AudioReactive
            switch (brush.GeometryType)
            {
                case BrushGeometryType.Tube:
                    selectedShader = _tubeShader ?? fallback;
                    shaderReason = "Tube";
                    break;
                case BrushGeometryType.Hull:
                    selectedShader = _hullShader ?? fallback;
                    shaderReason = "Hull";
                    break;
                case BrushGeometryType.Particle:
                case BrushGeometryType.Spray:
                    selectedShader = _particleShader ?? fallback;
                    shaderReason = "Particle/Spray";
                    break;
                case BrushGeometryType.Slice:
                    selectedShader = _strokeShader ?? fallback;
                    shaderReason = "Slice";
                    break;
                case BrushGeometryType.Flat:
                default:
                    // For flat brushes, check if emissive/audio reactive for glow shader
                    if (brush.Category == BrushCategory.Emissive || brush.Category == BrushCategory.AudioReactive)
                    {
                        selectedShader = _glowShader ?? fallback;
                        shaderReason = "Emissive/AudioReactive";
                    }
                    else
                    {
                        selectedShader = _strokeShader ?? fallback;
                        shaderReason = "Flat";
                    }
                    break;
            }

            mat = new Material(selectedShader);

            // Apply category-specific settings
            if (brush.Category == BrushCategory.Emissive || brush.Category == BrushCategory.AudioReactive)
            {
                if (mat.HasProperty("_GlowStrength"))
                    mat.SetFloat("_GlowStrength", 1.5f);
                if (mat.HasProperty("_CoreBrightness"))
                    mat.SetFloat("_CoreBrightness", 1.2f);
                if (brush.IsAudioReactive && mat.HasProperty("_AudioReactive"))
                    mat.SetFloat("_AudioReactive", 1f);
            }

            // Slice brushes need double-sided rendering
            if (brush.GeometryType == BrushGeometryType.Slice && mat.HasProperty("_Cull"))
            {
                mat.SetFloat("_Cull", 0f);
            }

            // Common settings
            mat.SetFloat("_UseVertexColor", 1f);
            mat.color = Color.white;

            // Enable backface rendering if specified
            if (brush.RenderBackfaces && mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", 0f); // Cull Off

            // Set render queue for proper transparency
            mat.renderQueue = 3000;

            brush.Material = mat;

            // DEBUG: Log material assignment
            Debug.Log($"[BrushCatalogFactory] Assigned: {brush.DisplayName} ({brush.GeometryType}) -> {mat.shader?.name ?? "NULL"}, " +
                      $"Color={mat.color}, RenderQueue={mat.renderQueue}");
        }

        /// <summary>
        /// Assigns materials to all brushes in a catalog.
        /// </summary>
        public static void AssignMaterials(List<BrushData> catalog)
        {
            // Reset shader logging flag for fresh catalog
            var counts = new Dictionary<string, int>();

            foreach (var brush in catalog)
            {
                AssignMaterial(brush);

                // Count shader usage
                string shaderName = brush.Material?.shader?.name ?? "NULL";
                if (!counts.ContainsKey(shaderName))
                    counts[shaderName] = 0;
                counts[shaderName]++;
            }

            // Log summary
            string summary = string.Join(", ", counts.Select(kv => $"{kv.Key}:{kv.Value}"));
            Debug.Log($"[BrushCatalogFactory] Material assignment complete: {catalog.Count} brushes - {summary}");
        }

        /// <summary>
        /// Creates the essential 20-brush catalog for quick startup.
        /// Automatically assigns materials based on brush type.
        /// </summary>
        public static List<BrushData> CreateEssentialCatalog()
        {
            var catalog = new List<BrushData>
            {
                // Basic
                CreateFlat(), CreateMarker(), CreateTaperedMarker(), CreateHighlighter(),
                // Paint
                CreateOilPaint(), CreateThickPaint(), CreateWetPaint(),
                // Tube
                CreateTube(), CreateWire(), CreateIcing(),
                // Emissive
                CreateLight(), CreateNeonPulse(), CreateFire(),
                // Particle
                CreateBubbles(), CreateEmbers(), CreateStars(),
                // Audio Reactive
                CreateWaveform(), CreateElectricity(), CreateDisco(),
                // Hull
                CreateDiamond()
            };

            AssignMaterials(catalog);
            return catalog;
        }

        /// <summary>
        /// Creates full 54 standard brush catalog.
        /// Automatically assigns materials based on brush type.
        /// </summary>
        public static List<BrushData> CreateStandardCatalog()
        {
            var catalog = new List<BrushData>();

            // Flat brushes (24)
            catalog.Add(CreateFlat());
            catalog.Add(CreateMarker());
            catalog.Add(CreateTaperedMarker());
            catalog.Add(CreateTaperedFlat());
            catalog.Add(CreateTaperedHighlighter());
            catalog.Add(CreateHighlighter());
            catalog.Add(CreateSoftHighlighter());
            catalog.Add(CreateCelVinyl());
            catalog.Add(CreateInk());
            catalog.Add(CreatePaper());
            catalog.Add(CreateFelt());
            catalog.Add(CreateDuctTape());
            catalog.Add(CreateOilPaint());
            catalog.Add(CreateThickPaint());
            catalog.Add(CreateWetPaint());
            catalog.Add(CreatePinchedFlat());
            catalog.Add(CreatePinchedMarker());
            catalog.Add(CreateVelvetInk());
            catalog.Add(CreateFire());
            catalog.Add(CreatePlasma());
            catalog.Add(CreateRainbow());
            catalog.Add(CreateStreamers());
            catalog.Add(CreateTaffy());
            catalog.Add(CreateHypercolor());

            // Tube brushes (14)
            catalog.Add(CreateTube());
            catalog.Add(CreateWire());
            catalog.Add(CreateIcing());
            catalog.Add(CreateLofted());
            catalog.Add(CreatePetal());
            catalog.Add(CreateSpikes());
            catalog.Add(CreateFacetedTube());
            catalog.Add(CreateToon());
            catalog.Add(CreateTubeToonInverted());
            catalog.Add(CreateComet());
            catalog.Add(CreateDisco());
            catalog.Add(CreateLightWire());
            catalog.Add(CreateNeonPulse());
            catalog.Add(CreateChromaticWave());

            // Hull brushes (5)
            catalog.Add(CreateDiamond());
            catalog.Add(CreateConcaveHull());
            catalog.Add(CreateMatteHull());
            catalog.Add(CreateShinyHull());
            catalog.Add(CreateUnlitHull());

            // Particle brushes (5)
            catalog.Add(CreateBubbles());
            catalog.Add(CreateEmbers());
            catalog.Add(CreateSmoke());
            catalog.Add(CreateSnow());
            catalog.Add(CreateStars());

            // Spray brushes (4)
            catalog.Add(CreateCoarseBristles());
            catalog.Add(CreateDotMarker());
            catalog.Add(CreateLeaves());
            catalog.Add(CreateSplatter());

            // Slice brushes (3) - Open Brush SliceBrush pattern
            catalog.Add(CreateSlice());
            catalog.Add(CreateMotionTrail());
            catalog.Add(CreateRibbonSlice());

            // Audio Reactive (6) - these overlap with above but are key
            catalog.Add(CreateWaveform());
            catalog.Add(CreateWaveformFFT());
            catalog.Add(CreateWaveformTube());
            catalog.Add(CreateWaveformParticles());
            catalog.Add(CreateElectricity());
            catalog.Add(CreateLight());

            AssignMaterials(catalog);
            return catalog;
        }

        #region Flat Brushes

        public static BrushData CreateFlat()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "flat"; b.DisplayName = "Flat";
            b.Category = BrushCategory.Basic;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.001f, 0.1f);
            b.RenderBackfaces = true;
            return b;
        }

        public static BrushData CreateMarker()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "marker"; b.DisplayName = "Marker";
            b.Category = BrushCategory.Markers;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.002f, 0.05f);
            b.ColorLuminanceMin = 0.3f; // Self-lit
            return b;
        }

        public static BrushData CreateTaperedMarker()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "tapered_marker"; b.DisplayName = "Tapered Marker";
            b.Category = BrushCategory.Markers;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.001f, 0.03f);
            b.PressureRange = new Vector2(0.1f, 1f);
            b.ColorLuminanceMin = 0.3f;
            return b;
        }

        public static BrushData CreateTaperedFlat()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "tapered_flat"; b.DisplayName = "Tapered Flat";
            b.Category = BrushCategory.Basic;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.001f, 0.03f);
            b.PressureRange = new Vector2(0.1f, 1f);
            return b;
        }

        public static BrushData CreateTaperedHighlighter()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "tapered_highlighter"; b.DisplayName = "Tapered Highlighter";
            b.Category = BrushCategory.Markers;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.002f, 0.04f);
            b.PressureRange = new Vector2(0.1f, 1f);
            b.ColorLuminanceMin = 0.4f;
            return b;
        }

        public static BrushData CreateHighlighter()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "highlighter"; b.DisplayName = "Highlighter";
            b.Category = BrushCategory.Markers;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.05f);
            b.ColorLuminanceMin = 0.4f;
            return b;
        }

        public static BrushData CreateSoftHighlighter()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "soft_highlighter"; b.DisplayName = "Soft Highlighter";
            b.Category = BrushCategory.Markers;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.005f, 0.06f);
            b.BaseOpacity = 0.7f;
            b.ColorLuminanceMin = 0.3f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { FrequencyBand = 2 };
            return b;
        }

        public static BrushData CreateCelVinyl()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "cel_vinyl"; b.DisplayName = "Cel Vinyl";
            b.Category = BrushCategory.Paint;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.ColorLuminanceMin = 0.3f;
            return b;
        }

        public static BrushData CreateInk()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "ink"; b.DisplayName = "Ink";
            b.Category = BrushCategory.Ink;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.001f, 0.03f);
            return b;
        }

        public static BrushData CreatePaper()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "paper"; b.DisplayName = "Paper";
            b.Category = BrushCategory.Basic;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.1f);
            return b;
        }

        public static BrushData CreateFelt()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "felt"; b.DisplayName = "Felt";
            b.Category = BrushCategory.Basic;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            return b;
        }

        public static BrushData CreateDuctTape()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "duct_tape"; b.DisplayName = "Duct Tape";
            b.Category = BrushCategory.Basic;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.03f; b.SizeRange = new Vector2(0.01f, 0.1f);
            return b;
        }

        public static BrushData CreateOilPaint()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "oil_paint"; b.DisplayName = "Oil Paint";
            b.Category = BrushCategory.Paint;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.RenderBackfaces = true;
            return b;
        }

        public static BrushData CreateThickPaint()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "thick_paint"; b.DisplayName = "Thick Paint";
            b.Category = BrushCategory.Paint;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.1f);
            return b;
        }

        public static BrushData CreateWetPaint()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "wet_paint"; b.DisplayName = "Wet Paint";
            b.Category = BrushCategory.Paint;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            return b;
        }

        public static BrushData CreatePinchedFlat()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "pinched_flat"; b.DisplayName = "Pinched Flat";
            b.Category = BrushCategory.Basic;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.002f, 0.05f);
            return b;
        }

        public static BrushData CreatePinchedMarker()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "pinched_marker"; b.DisplayName = "Pinched Marker";
            b.Category = BrushCategory.Markers;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.012f; b.SizeRange = new Vector2(0.002f, 0.04f);
            b.ColorLuminanceMin = 0.3f;
            return b;
        }

        public static BrushData CreateVelvetInk()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "velvet_ink"; b.DisplayName = "Velvet Ink";
            b.Category = BrushCategory.Ink;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.ColorLuminanceMin = 0.3f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { FrequencyBand = 3 };
            return b;
        }

        #endregion

        #region Emissive Flat Brushes

        public static BrushData CreateFire()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "fire"; b.DisplayName = "Fire";
            b.Category = BrushCategory.Emissive;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.ColorLuminanceMin = 0.6f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams
            {
                FrequencyBand = 3,
                ModulateSize = true, SizeMultiplierRange = new Vector2(0.5f, 2.5f),
                ModulateEmission = true, EmissionRange = new Vector2(0.5f, 3f)
            };
            return b;
        }

        public static BrushData CreatePlasma()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "plasma"; b.DisplayName = "Plasma";
            b.Category = BrushCategory.Emissive;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.005f, 0.1f);
            b.ColorLuminanceMin = 0.5f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { FrequencyBand = 5, ModulateEmission = true };
            return b;
        }

        public static BrushData CreateRainbow()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "rainbow"; b.DisplayName = "Rainbow";
            b.Category = BrushCategory.Emissive;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.ColorLuminanceMin = 0.4f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { ModulateHue = true, HueShiftRange = 1f };
            return b;
        }

        public static BrushData CreateStreamers()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "streamers"; b.DisplayName = "Streamers";
            b.Category = BrushCategory.Emissive;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.ColorLuminanceMin = 0.4f;
            b.IsAudioReactive = true;
            return b;
        }

        public static BrushData CreateTaffy()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "taffy"; b.DisplayName = "Taffy";
            b.Category = BrushCategory.Emissive;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.06f);
            b.ColorLuminanceMin = 0.3f;
            return b;
        }

        public static BrushData CreateHypercolor()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "hypercolor"; b.DisplayName = "Hypercolor";
            b.Category = BrushCategory.Emissive;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.ColorLuminanceMin = 0.5f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { ModulateHue = true, ModulateEmission = true };
            return b;
        }

        public static BrushData CreateLight()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "light"; b.DisplayName = "Light";
            b.Category = BrushCategory.Emissive;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.002f, 0.05f);
            b.ColorLuminanceMin = 0.6f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { FrequencyBand = 0, ModulateEmission = true };
            return b;
        }

        public static BrushData CreateElectricity()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "electricity"; b.DisplayName = "Electricity";
            b.Category = BrushCategory.AudioReactive;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.002f, 0.03f);
            b.ColorLuminanceMin = 0.7f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams
            {
                FrequencyBand = 7,
                ModulateSize = true, SizeMultiplierRange = new Vector2(0.3f, 3f),
                ModulateEmission = true, EmissionRange = new Vector2(1f, 5f)
            };
            return b;
        }

        public static BrushData CreateWaveform()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "waveform"; b.DisplayName = "Waveform";
            b.Category = BrushCategory.AudioReactive;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.ColorLuminanceMin = 0.5f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams
            {
                Mode = AudioReactiveMode.FFTSpectrum,
                ModulateSize = true, SizeMultiplierRange = new Vector2(0.5f, 3f),
                ModulateEmission = true
            };
            return b;
        }

        public static BrushData CreateWaveformFFT()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "waveform_fft"; b.DisplayName = "Waveform FFT";
            b.Category = BrushCategory.AudioReactive;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.005f, 0.1f);
            b.ColorLuminanceMin = 0.5f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams
            {
                Mode = AudioReactiveMode.FFTSpectrum,
                ModulateSize = true, ModulateEmission = true
            };
            return b;
        }

        #endregion

        #region Tube Brushes

        public static BrushData CreateTube()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "tube"; b.DisplayName = "Tube";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.002f, 0.05f);
            b.TubeSides = 12; b.MinSegmentLength = 0.002f; // 12 sides for smooth round appearance
            return b;
        }

        public static BrushData CreateWire()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "wire"; b.DisplayName = "Wire";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.003f; b.SizeRange = new Vector2(0.001f, 0.01f);
            b.TubeSides = 4; b.MinSegmentLength = 0.001f;
            b.ColorLuminanceMin = 0.4f;
            return b;
        }

        public static BrushData CreateIcing()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "icing"; b.DisplayName = "Icing";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.06f);
            b.TubeSides = 16; b.MinSegmentLength = 0.003f; // Extra smooth for icing effect
            return b;
        }

        public static BrushData CreateLofted()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "lofted"; b.DisplayName = "Lofted";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.TubeSides = 6;
            return b;
        }

        public static BrushData CreatePetal()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "petal"; b.DisplayName = "Petal";
            b.Category = BrushCategory.Nature;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.06f);
            b.TubeSides = 12; // Smooth round petal
            return b;
        }

        public static BrushData CreateSpikes()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "spikes"; b.DisplayName = "Spikes";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.002f, 0.04f);
            b.TubeSides = 4;
            return b;
        }

        public static BrushData CreateFacetedTube()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "faceted_tube"; b.DisplayName = "Faceted Tube";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.TubeSides = 6;
            b.ColorLuminanceMin = 0.3f;
            return b;
        }

        public static BrushData CreateToon()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "toon"; b.DisplayName = "Toon";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.TubeSides = 12; // Smooth round toon
            b.ColorLuminanceMin = 0.3f;
            b.IsAudioReactive = true;
            return b;
        }

        public static BrushData CreateTubeToonInverted()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "tube_toon_inverted"; b.DisplayName = "Tube Toon Inverted";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.TubeSides = 12; // Smooth round
            b.ColorLuminanceMin = 0.3f;
            return b;
        }

        public static BrushData CreateComet()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "comet"; b.DisplayName = "Comet";
            b.Category = BrushCategory.Emissive;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.TubeSides = 6;
            b.ColorLuminanceMin = 0.6f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { FrequencyBand = 4 };
            return b;
        }

        public static BrushData CreateDisco()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "disco"; b.DisplayName = "Disco";
            b.Category = BrushCategory.AudioReactive;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.06f);
            b.TubeSides = 12; // Smooth round disco tube
            b.ColorLuminanceMin = 0.5f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams
            {
                Mode = AudioReactiveMode.Beat,
                ModulateHue = true, ModulateEmission = true
            };
            return b;
        }

        public static BrushData CreateLightWire()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "light_wire"; b.DisplayName = "Light Wire";
            b.Category = BrushCategory.Emissive;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.005f; b.SizeRange = new Vector2(0.001f, 0.02f);
            b.TubeSides = 4;
            b.ColorLuminanceMin = 0.7f;
            b.IsAudioReactive = true;
            return b;
        }

        public static BrushData CreateNeonPulse()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "neon_pulse"; b.DisplayName = "Neon Pulse";
            b.Category = BrushCategory.AudioReactive;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.002f, 0.03f);
            b.TubeSides = 10; // Smooth round neon
            b.ColorLuminanceMin = 0.7f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams
            {
                FrequencyBand = 0,
                ModulateSize = true, ModulateEmission = true,
                EmissionRange = new Vector2(1f, 4f)
            };
            return b;
        }

        public static BrushData CreateChromaticWave()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "chromatic_wave"; b.DisplayName = "Chromatic Wave";
            b.Category = BrushCategory.AudioReactive;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.TubeSides = 12; // Smooth round
            b.ColorLuminanceMin = 0.5f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { ModulateHue = true, HueShiftRange = 1f };
            return b;
        }

        public static BrushData CreateWaveformTube()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "waveform_tube"; b.DisplayName = "Waveform Tube";
            b.Category = BrushCategory.AudioReactive;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.TubeSides = 10; // Smooth round
            b.ColorLuminanceMin = 0.5f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams
            {
                Mode = AudioReactiveMode.FFTSpectrum,
                ModulateSize = true, ModulateEmission = true
            };
            return b;
        }

        #endregion

        #region Hull Brushes

        public static BrushData CreateDiamond()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "diamond"; b.DisplayName = "Diamond";
            b.Category = BrushCategory.Hull;
            b.GeometryType = BrushGeometryType.Hull;
            b.DefaultSize = 0.03f; b.SizeRange = new Vector2(0.01f, 0.1f);
            return b;
        }

        public static BrushData CreateConcaveHull()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "concave_hull"; b.DisplayName = "Concave Hull";
            b.Category = BrushCategory.Hull;
            b.GeometryType = BrushGeometryType.Hull;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.08f);
            return b;
        }

        public static BrushData CreateMatteHull()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "matte_hull"; b.DisplayName = "Matte Hull";
            b.Category = BrushCategory.Hull;
            b.GeometryType = BrushGeometryType.Hull;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.08f);
            return b;
        }

        public static BrushData CreateShinyHull()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "shiny_hull"; b.DisplayName = "Shiny Hull";
            b.Category = BrushCategory.Hull;
            b.GeometryType = BrushGeometryType.Hull;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.08f);
            return b;
        }

        public static BrushData CreateUnlitHull()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "unlit_hull"; b.DisplayName = "Unlit Hull";
            b.Category = BrushCategory.Hull;
            b.GeometryType = BrushGeometryType.Hull;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.08f);
            b.ColorLuminanceMin = 0.4f;
            return b;
        }

        #endregion

        #region Particle Brushes

        public static BrushData CreateBubbles()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "bubbles"; b.DisplayName = "Bubbles";
            b.Category = BrushCategory.Particle;
            b.GeometryType = BrushGeometryType.Particle;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.SizeVariance = 0.5f;
            b.ParticleRate = 30f; b.ParticleSpeed = 0.3f;
            b.ColorLuminanceMin = 0.3f;
            return b;
        }

        public static BrushData CreateEmbers()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "embers"; b.DisplayName = "Embers";
            b.Category = BrushCategory.Particle;
            b.GeometryType = BrushGeometryType.Particle;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.002f, 0.03f);
            b.SizeVariance = 0.7f;
            b.ParticleRate = 50f; b.ParticleSpeed = 1.5f;
            b.ColorLuminanceMin = 0.6f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { FrequencyBand = 3, ModulateParticleRate = true };
            return b;
        }

        public static BrushData CreateSmoke()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "smoke"; b.DisplayName = "Smoke";
            b.Category = BrushCategory.Particle;
            b.GeometryType = BrushGeometryType.Particle;
            b.DefaultSize = 0.03f; b.SizeRange = new Vector2(0.01f, 0.1f);
            b.SizeVariance = 0.4f;
            b.ParticleRate = 20f; b.ParticleSpeed = 0.5f;
            b.BaseOpacity = 0.6f;
            b.ColorLuminanceMin = 0.2f;
            return b;
        }

        public static BrushData CreateSnow()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "snow"; b.DisplayName = "Snow";
            b.Category = BrushCategory.Particle;
            b.GeometryType = BrushGeometryType.Particle;
            b.DefaultSize = 0.005f; b.SizeRange = new Vector2(0.001f, 0.02f);
            b.SizeVariance = 0.6f;
            b.ParticleRate = 100f; b.ParticleSpeed = 0.8f;
            b.ColorLuminanceMin = 0.3f;
            b.IsAudioReactive = true;
            return b;
        }

        public static BrushData CreateStars()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "stars"; b.DisplayName = "Stars";
            b.Category = BrushCategory.Particle;
            b.GeometryType = BrushGeometryType.Particle;
            b.DefaultSize = 0.008f; b.SizeRange = new Vector2(0.002f, 0.025f);
            b.SizeVariance = 0.8f;
            b.ParticleRate = 40f; b.ParticleSpeed = 0.1f;
            b.ColorLuminanceMin = 0.7f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { ModulateEmission = true };
            return b;
        }

        public static BrushData CreateWaveformParticles()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "waveform_particles"; b.DisplayName = "Waveform Particles";
            b.Category = BrushCategory.AudioReactive;
            b.GeometryType = BrushGeometryType.Particle;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.002f, 0.03f);
            b.SizeVariance = 0.5f;
            b.ParticleRate = 60f; b.ParticleSpeed = 1f;
            b.ColorLuminanceMin = 0.5f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams
            {
                Mode = AudioReactiveMode.FFTSpectrum,
                ModulateSize = true, ModulateParticleRate = true, ModulateEmission = true
            };
            return b;
        }

        #endregion

        #region Spray Brushes

        public static BrushData CreateCoarseBristles()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "coarse_bristles"; b.DisplayName = "Coarse Bristles";
            b.Category = BrushCategory.Spray;
            b.GeometryType = BrushGeometryType.Spray;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.08f);
            b.SizeVariance = 0.3f;
            b.ParticleRate = 150f;
            return b;
        }

        public static BrushData CreateDotMarker()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "dot_marker"; b.DisplayName = "Dot Marker";
            b.Category = BrushCategory.Spray;
            b.GeometryType = BrushGeometryType.Spray;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.002f, 0.03f);
            b.SizeVariance = 0.2f;
            b.ParticleRate = 100f;
            b.ColorLuminanceMin = 0.3f;
            return b;
        }

        public static BrushData CreateLeaves()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "leaves"; b.DisplayName = "Leaves";
            b.Category = BrushCategory.Nature;
            b.GeometryType = BrushGeometryType.Spray;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.06f);
            b.SizeVariance = 0.5f;
            b.ParticleRate = 50f;
            b.ParticleRotationRange = 360f;
            return b;
        }

        public static BrushData CreateSplatter()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "splatter"; b.DisplayName = "Splatter";
            b.Category = BrushCategory.Spray;
            b.GeometryType = BrushGeometryType.Spray;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.SizeVariance = 0.7f;
            b.ParticleRate = 80f;
            b.ParticleRotationRange = 360f;
            return b;
        }

        #endregion

        #region Slice Brushes

        public static BrushData CreateSlice()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "slice"; b.DisplayName = "Slice";
            b.Category = BrushCategory.Basic;
            b.GeometryType = BrushGeometryType.Slice;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.RenderBackfaces = true;
            return b;
        }

        public static BrushData CreateMotionTrail()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "motion_trail"; b.DisplayName = "Motion Trail";
            b.Category = BrushCategory.Effects;
            b.GeometryType = BrushGeometryType.Slice;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.RenderBackfaces = true;
            b.ColorLuminanceMin = 0.4f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams
            {
                FrequencyBand = 3,
                ModulateSize = true, SizeMultiplierRange = new Vector2(0.5f, 2f)
            };
            return b;
        }

        public static BrushData CreateRibbonSlice()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "ribbon_slice"; b.DisplayName = "Ribbon Slice";
            b.Category = BrushCategory.Basic;
            b.GeometryType = BrushGeometryType.Slice;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.1f);
            b.RenderBackfaces = true;
            b.PressureRange = new Vector2(0.2f, 1f);
            return b;
        }

        #endregion

        #region Experimental Brushes (46 total)

        /// <summary>
        /// Creates full 100-brush catalog (54 standard + 46 experimental).
        /// </summary>
        public static List<BrushData> CreateFullCatalog()
        {
            var catalog = CreateStandardCatalog();

            // Add all experimental brushes
            catalog.Add(CreateBubbleWand());
            catalog.Add(CreateCharcoal());
            catalog.Add(CreateDigital());
            catalog.Add(CreateDoubleFlat());
            catalog.Add(CreateDrafting());
            catalog.Add(CreateDryBrush());
            catalog.Add(CreateDuctTapeGeometry());
            catalog.Add(CreateFairy());
            catalog.Add(CreateFeather());
            catalog.Add(CreateFire2());
            catalog.Add(CreateFlatGeometry());
            catalog.Add(CreateGouache());
            catalog.Add(CreateGuts());
            catalog.Add(CreateDiffuse());
            catalog.Add(CreateInkGeometry());
            catalog.Add(CreateKeijiroTube());
            catalog.Add(CreateLacewing());
            catalog.Add(CreateLeakyPen());
            catalog.Add(CreateWireLit());
            catalog.Add(CreateLoftedHueShift());
            catalog.Add(CreateMarbledRainbow());
            catalog.Add(CreateMarkerGeometry());
            catalog.Add(CreateMuscle());
            catalog.Add(CreateMylarTube());
            catalog.Add(CreateOilPaintGeometry());
            catalog.Add(CreatePaperGeometry());
            catalog.Add(CreateRace());
            catalog.Add(CreateRain());
            catalog.Add(CreateRisingBubbles());
            catalog.Add(CreateSingleSided());
            catalog.Add(CreateSmoothHull());
            catalog.Add(CreateSpace());
            catalog.Add(CreateSparks());
            catalog.Add(CreateSquareFlat());
            catalog.Add(CreateTaperedHueShift());
            catalog.Add(CreateTaperedMarkerGeo());
            catalog.Add(CreateTaperedWire());
            catalog.Add(CreateLeaves2());
            catalog.Add(CreateGeomThick());
            catalog.Add(CreateTubeHighlighter());
            catalog.Add(CreateTubeFlat());
            catalog.Add(CreateTubeMarker());
            catalog.Add(CreateWatercolorPaper());
            catalog.Add(CreateWatercolorPaperGeometry());
            catalog.Add(CreateWetPaintGeometry());
            catalog.Add(CreateWind());

            // Note: AssignMaterials already called by CreateStandardCatalog
            // Re-assign for experimental brushes only would require tracking
            // For simplicity, just re-assign all
            AssignMaterials(catalog);
            return catalog;
        }

        // --- Experimental Flat Brushes ---

        public static BrushData CreateCharcoal()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "charcoal"; b.DisplayName = "Charcoal";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.BaseOpacity = 0.85f;
            return b;
        }

        public static BrushData CreateDigital()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "digital"; b.DisplayName = "Digital";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { FrequencyBand = 5 };
            return b;
        }

        public static BrushData CreateDoubleFlat()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "double_flat"; b.DisplayName = "Double Flat";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.005f, 0.1f);
            b.RenderBackfaces = true;
            return b;
        }

        public static BrushData CreateDrafting()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "drafting"; b.DisplayName = "Drafting";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.005f; b.SizeRange = new Vector2(0.001f, 0.02f);
            return b;
        }

        public static BrushData CreateDryBrush()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "dry_brush"; b.DisplayName = "Dry Brush";
            b.Category = BrushCategory.Paint;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.1f);
            b.BaseOpacity = 0.7f;
            return b;
        }

        public static BrushData CreateDuctTapeGeometry()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "duct_tape_geo"; b.DisplayName = "Duct Tape (Geometry)";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.03f; b.SizeRange = new Vector2(0.01f, 0.1f);
            return b;
        }

        public static BrushData CreateFairy()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "fairy"; b.DisplayName = "Fairy";
            b.Category = BrushCategory.Effects;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.002f, 0.03f);
            b.ColorLuminanceMin = 0.5f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { FrequencyBand = 6, ModulateEmission = true };
            return b;
        }

        public static BrushData CreateFeather()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "feather"; b.DisplayName = "Feather";
            b.Category = BrushCategory.Nature;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.06f);
            b.IsAudioReactive = true;
            return b;
        }

        public static BrushData CreateFire2()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "fire2"; b.DisplayName = "Fire 2";
            b.Category = BrushCategory.Emissive;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.1f);
            b.ColorLuminanceMin = 0.6f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams
            {
                FrequencyBand = 2,
                ModulateSize = true, ModulateEmission = true
            };
            return b;
        }

        public static BrushData CreateFlatGeometry()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "flat_geo"; b.DisplayName = "Flat (Geometry)";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.1f);
            return b;
        }

        public static BrushData CreateGouache()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "gouache"; b.DisplayName = "Gouache";
            b.Category = BrushCategory.Paint;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            return b;
        }

        public static BrushData CreateDiffuse()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "diffuse"; b.DisplayName = "Diffuse";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            return b;
        }

        public static BrushData CreateInkGeometry()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "ink_geo"; b.DisplayName = "Ink (Geometry)";
            b.Category = BrushCategory.Ink;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.002f, 0.03f);
            return b;
        }

        public static BrushData CreateLacewing()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "lacewing"; b.DisplayName = "Lacewing";
            b.Category = BrushCategory.Nature;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            return b;
        }

        public static BrushData CreateLeakyPen()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "leaky_pen"; b.DisplayName = "Leaky Pen";
            b.Category = BrushCategory.Ink;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.008f; b.SizeRange = new Vector2(0.002f, 0.025f);
            b.SizeVariance = 0.3f;
            return b;
        }

        public static BrushData CreateMarbledRainbow()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "marbled_rainbow"; b.DisplayName = "Marbled Rainbow";
            b.Category = BrushCategory.Effects;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { ModulateHue = true, HueShiftRange = 1f };
            return b;
        }

        public static BrushData CreateMarkerGeometry()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "marker_geo"; b.DisplayName = "Marker (Geometry)";
            b.Category = BrushCategory.Markers;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.ColorLuminanceMin = 0.3f;
            return b;
        }

        public static BrushData CreateOilPaintGeometry()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "oil_paint_geo"; b.DisplayName = "Oil Paint (Geometry)";
            b.Category = BrushCategory.Paint;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            return b;
        }

        public static BrushData CreatePaperGeometry()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "paper_geo"; b.DisplayName = "Paper (Geometry)";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.1f);
            return b;
        }

        public static BrushData CreateRace()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "race"; b.DisplayName = "Race";
            b.Category = BrushCategory.Effects;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.ColorLuminanceMin = 0.5f;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { FrequencyBand = 4, ModulateEmission = true };
            return b;
        }

        public static BrushData CreateSingleSided()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "single_sided"; b.DisplayName = "Single Sided";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.1f);
            b.RenderBackfaces = false;
            return b;
        }

        public static BrushData CreateSpace()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "space"; b.DisplayName = "Space";
            b.Category = BrushCategory.Effects;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.03f; b.SizeRange = new Vector2(0.01f, 0.15f);
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { FrequencyBand = 0, ModulateEmission = true };
            return b;
        }

        public static BrushData CreateSquareFlat()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "square_flat"; b.DisplayName = "Square Flat";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            return b;
        }

        public static BrushData CreateTaperedHueShift()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "tapered_hue_shift"; b.DisplayName = "Tapered Hue Shift";
            b.Category = BrushCategory.Effects;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.002f, 0.03f);
            b.PressureRange = new Vector2(0.1f, 1f);
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { ModulateHue = true, HueShiftRange = 0.5f };
            return b;
        }

        public static BrushData CreateTaperedMarkerGeo()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "tapered_marker_geo"; b.DisplayName = "Tapered Marker (Geometry)";
            b.Category = BrushCategory.Markers;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.01f; b.SizeRange = new Vector2(0.002f, 0.03f);
            b.PressureRange = new Vector2(0.1f, 1f);
            b.ColorLuminanceMin = 0.3f;
            return b;
        }

        public static BrushData CreateWatercolorPaper()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "watercolor_paper"; b.DisplayName = "Watercolor Paper";
            b.Category = BrushCategory.Paint;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.1f);
            b.BaseOpacity = 0.8f;
            return b;
        }

        public static BrushData CreateWatercolorPaperGeometry()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "watercolor_paper_geo"; b.DisplayName = "Watercolor Paper (Geometry)";
            b.Category = BrushCategory.Paint;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.1f);
            b.BaseOpacity = 0.8f;
            return b;
        }

        public static BrushData CreateWetPaintGeometry()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "wet_paint_geo"; b.DisplayName = "Wet Paint (Geometry)";
            b.Category = BrushCategory.Paint;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            return b;
        }

        public static BrushData CreateWind()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "wind"; b.DisplayName = "Wind";
            b.Category = BrushCategory.Effects;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.08f);
            b.ColorLuminanceMin = 0.4f;
            return b;
        }

        // --- Experimental Tube Brushes ---

        public static BrushData CreateBubbleWand()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "bubble_wand"; b.DisplayName = "Bubble Wand";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.06f);
            b.TubeSides = 8;
            return b;
        }

        public static BrushData CreateGuts()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "guts"; b.DisplayName = "Guts";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.TubeSides = 6;
            b.SizeVariance = 0.3f;
            return b;
        }

        public static BrushData CreateKeijiroTube()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "keijiro_tube"; b.DisplayName = "Keijiro Tube";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.012f; b.SizeRange = new Vector2(0.002f, 0.04f);
            b.TubeSides = 8;
            b.ColorLuminanceMin = 0.4f;
            return b;
        }

        public static BrushData CreateWireLit()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "wire_lit"; b.DisplayName = "Wire (Lit)";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.003f; b.SizeRange = new Vector2(0.001f, 0.01f);
            b.TubeSides = 4;
            return b;
        }

        public static BrushData CreateLoftedHueShift()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "lofted_hue_shift"; b.DisplayName = "Lofted (Hue Shift)";
            b.Category = BrushCategory.Effects;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.TubeSides = 6;
            b.IsAudioReactive = true;
            b.AudioParams = new AudioReactiveParams { ModulateHue = true, HueShiftRange = 0.8f };
            return b;
        }

        public static BrushData CreateMuscle()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "muscle"; b.DisplayName = "Muscle";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.02f; b.SizeRange = new Vector2(0.005f, 0.06f);
            b.TubeSides = 8;
            return b;
        }

        public static BrushData CreateMylarTube()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "mylar_tube"; b.DisplayName = "Mylar Tube";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.TubeSides = 6;
            return b;
        }

        public static BrushData CreateRain()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "rain"; b.DisplayName = "Rain";
            b.Category = BrushCategory.Nature;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.002f; b.SizeRange = new Vector2(0.0005f, 0.005f);
            b.TubeSides = 4;
            return b;
        }

        public static BrushData CreateSparks()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "sparks"; b.DisplayName = "Sparks";
            b.Category = BrushCategory.Effects;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.005f; b.SizeRange = new Vector2(0.001f, 0.015f);
            b.TubeSides = 4;
            return b;
        }

        public static BrushData CreateTaperedWire()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "tapered_wire"; b.DisplayName = "Tapered Wire";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.003f; b.SizeRange = new Vector2(0.0005f, 0.01f);
            b.TubeSides = 4;
            b.PressureRange = new Vector2(0.1f, 1f);
            return b;
        }

        public static BrushData CreateTubeHighlighter()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "tube_highlighter"; b.DisplayName = "Tube (Highlighter)";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.TubeSides = 6;
            b.ColorLuminanceMin = 0.4f;
            return b;
        }

        public static BrushData CreateTubeFlat()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "tube_flat"; b.DisplayName = "Tube (Flat)";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.TubeSides = 4;
            return b;
        }

        public static BrushData CreateTubeMarker()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "tube_marker"; b.DisplayName = "Tube (Marker)";
            b.Category = BrushCategory.Tube;
            b.GeometryType = BrushGeometryType.Tube;
            b.DefaultSize = 0.012f; b.SizeRange = new Vector2(0.002f, 0.04f);
            b.TubeSides = 6;
            b.ColorLuminanceMin = 0.3f;
            return b;
        }

        // --- Experimental Hull Brushes ---

        public static BrushData CreateSmoothHull()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "smooth_hull"; b.DisplayName = "Smooth Hull";
            b.Category = BrushCategory.Hull;
            b.GeometryType = BrushGeometryType.Hull;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.08f);
            return b;
        }

        // --- Experimental Particle Brushes ---

        public static BrushData CreateRisingBubbles()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "rising_bubbles"; b.DisplayName = "Rising Bubbles";
            b.Category = BrushCategory.Particle;
            b.GeometryType = BrushGeometryType.Particle;
            b.DefaultSize = 0.015f; b.SizeRange = new Vector2(0.003f, 0.05f);
            b.SizeVariance = 0.6f;
            b.ParticleRate = 25f; b.ParticleSpeed = 0.8f;
            return b;
        }

        // --- Experimental Spray Brushes ---

        public static BrushData CreateLeaves2()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "leaves2"; b.DisplayName = "Leaves 2";
            b.Category = BrushCategory.Nature;
            b.GeometryType = BrushGeometryType.Spray;
            b.DefaultSize = 0.025f; b.SizeRange = new Vector2(0.008f, 0.08f);
            b.SizeVariance = 0.6f;
            b.ParticleRate = 40f;
            b.ParticleRotationRange = 360f;
            return b;
        }

        public static BrushData CreateGeomThick()
        {
            var b = ScriptableObject.CreateInstance<BrushData>();
            b.BrushId = "geom_thick"; b.DisplayName = "Geom/Thick (Duct Tape)";
            b.Category = BrushCategory.Experimental;
            b.GeometryType = BrushGeometryType.Flat;
            b.DefaultSize = 0.03f; b.SizeRange = new Vector2(0.01f, 0.1f);
            return b;
        }

        #endregion
    }
}
