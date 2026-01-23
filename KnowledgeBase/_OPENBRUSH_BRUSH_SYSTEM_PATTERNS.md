# OpenBrush Brush System Patterns

**Source Projects Analyzed**:
- `/Users/jamestunick/wkspaces/OpenBrushMobile`
- `/Users/jamestunick/Documents/GitHub/Unity-XR-AI/_ref/open-brush-feature-pure-openxr`
- `/Users/jamestunick/Documents/GitHub/open-brush-main`

**Date**: 2026-01-22

---

## Architecture Overview

### Core Components

| Component | File | Purpose |
|-----------|------|---------|
| **BrushDescriptor** | `Assets/Scripts/Brushes/BrushDescriptor.cs` | ScriptableObject with brush data, material, prefab ref |
| **BrushCatalog** | `Assets/Scripts/BrushCatalog.cs` | Singleton manager for all brush lookups |
| **TiltBrushManifest** | `Assets/Scripts/TiltBrushManifest.cs` | Asset listing all brushes/environments |
| **BaseBrushScript** | `Assets/Scripts/Brushes/BaseBrushScript.cs` | Base class for all brush geometry |
| **GeometryBrush** | `Assets/Scripts/Brushes/GeometryBrush.cs` | Knot-based geometry generation |
| **TubeBrush** | `Assets/Scripts/Brushes/TubeBrush.cs` | Cylindrical tube geometry |
| **QuadStripBrush** | `Assets/Scripts/Brushes/QuadStripBrush.cs` | Flat ribbon geometry |
| **HullBrush** | `Assets/Scripts/Brushes/HullBrush.cs` | Convex hull geometry |
| **Brush.cginc** | `Assets/Shaders/Include/Brush.cginc` | Shader include with color conversion |

---

## BrushDescriptor ScriptableObject

### Key Properties

```csharp
public class BrushDescriptor : ScriptableObject, IExportableMaterial
{
    [Header("Identity")]
    public SerializableGuid m_Guid;
    public string m_DurableName;
    public string m_ShaderVersion = "10.0";
    public GameObject m_BrushPrefab;           // Prefab with brush script
    public List<string> m_Tags = new List<string> { "default" };

    [Header("Material")]
    [SerializeField] private Material m_Material;  // NON-INSTANTIATED asset
    public int m_TextureAtlasV;
    public float m_TileRate;

    [Header("Size")]
    public Vector2 m_BrushSizeRange;
    public Vector2 m_PressureSizeRange = new Vector2(.1f, 1f);

    [Header("Color")]
    public float m_Opacity;
    public Vector2 m_PressureOpacityRange;
    public float m_ColorLuminanceMin;
    public float m_ColorSaturationMax;

    [Header("Tube")]
    public float m_SolidMinLengthMeters_PS = 0.002f;
    public bool m_TubeStoreRadiusInTexcoord0Z;

    [Header("Misc")]
    public bool m_RenderBackfaces;
    public bool m_BackIsInvisible;
    public float m_BackfaceHueShift;
    public float m_BoundsPadding;

    [Header("Audio")]
    public bool m_AudioReactive;
    public AudioClip[] m_BrushAudioLayers;

    [Header("Export Settings")]
    public ExportableMaterialBlendMode m_BlendMode;
    public float m_EmissiveFactor;
}
```

### Material Property

```csharp
// Non-instantiated material asset - cloned at runtime
public Material Material => m_Material;

// Material in asset has:
// - _Color = white (1,1,1,1) - allows user color multiplication
// - _MainTex = texture atlas
// - _Cutoff = alpha threshold
// - Shader = Brush/DiffuseDoubleSided or similar
```

---

## Shader Architecture

### Color Multiplication Chain

```hlsl
// Final color = Texture * Material._Color * Vertex Color
void surf (Input IN, inout SurfaceOutput o) {
    fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = tex.rgb * _Color.rgb * IN.color.rgb;  // 3-way multiply
    o.Alpha = tex.a * IN.color.a;
}
```

### Critical: TbVertToNative Color Conversion

```hlsl
// File: Brush.cginc

// TB mesh colors are sRGB. TBT mesh colors are linear.
float4 TbVertToSrgb(float4 color) { return color; }
float4 TbVertToLinear(float4 color) { return SrgbToLinear(color); }

// Conversions to native colorspace
#ifdef TBT_LINEAR_TARGET
    float4 TbVertToNative(float4 color) { return TbVertToLinear(color); }
#else
    float4 TbVertToNative(float4 color) { return TbVertToSrgb(color); }
#endif

// ALWAYS call in vertex shader:
void vert (inout appdata v, out Input o) {
    v.color = TbVertToNative(v.color);  // <-- CRITICAL
}
```

### sRGB to Linear Conversion (Fast Approximation)

```hlsl
float4 SrgbToLinear(float4 color) {
    float3 sRGB = color.rgb;
    color.rgb = sRGB * (sRGB * (sRGB * 0.305306011 + 0.682171111) + 0.012522878);
    return color;
}
```

### Standard Shader Properties

```hlsl
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    _Shininess ("Shininess", Range(0.01,1)) = 0.078125
    _BumpMap ("Normalmap", 2D) = "bump" {}
}
```

### Double-Sided Rendering

```hlsl
// ALL brush shaders use Cull Off
Cull Off

// Normal flipping for backfaces using VFACE
void surf (Input IN, inout SurfaceOutput o) {
    o.Normal = float3(0, 0, IN.vface);  // +1 for front, -1 for back
}
```

---

## Geometry Generation

### TubeBrush Parameters

```csharp
class TubeBrush : GeometryBrush
{
    [SerializeField] float m_CapAspect = .8f;
    [SerializeField] ushort m_PointsInClosedCircle = 8;  // Sides of tube
    [SerializeField] bool m_EndCaps = true;
    [SerializeField] bool m_HardEdges = false;  // Flat vs smooth shading
    [SerializeField] protected UVStyle m_uvStyle = UVStyle.Distance;
    [SerializeField] protected ShapeModifier m_ShapeModifier = ShapeModifier.None;

    protected enum ShapeModifier {
        None,
        DoubleSidedTaper,
        Sin,
        Comet,
        Taper,
        Petal
    };
}
```

### QuadStripBrush Vertex Layout

```
// Vertex ordering for front-facing quads:
//    0--1  4
//    |,' ,'|     <- leading edge
//    2  3--5
//    stroke direction ->

// Backfaces interleaved: Q0 P0 Q1 P1 Q2 P2...
// Front→Back vertex mapping: 0→0, 1→2, 2→1, 3→3, 4→5, 5→4
```

### Knot-Based Geometry (GeometryBrush)

```csharp
public struct Knot {
    public PointerManager.ControlPoint point;
    public Vector3 smoothedPos;
    public float smoothedPressure;
    public float length;
    public Quaternion qFrame;
    public Vector3 nRight;      // Right normal
    public Vector3 nSurface;    // Surface normal
    public int iTri;            // First triangle index
    public ushort iVert;        // First vertex index
    public ushort nTri, nVert;  // Counts
}
```

### Minimal Rotation Frame (Parallel Transport)

```csharp
// Prevents tube twisting - maintains consistent orientation
public static Quaternion ComputeMinimalRotationFrame(
    Vector3 tangent, Quaternion prevFrame, Quaternion brushOrientation)
{
    // Project previous up onto plane perpendicular to new tangent
    Vector3 prevUp = prevFrame * Vector3.up;
    Vector3 right = Vector3.Cross(tangent, prevUp);
    if (right.sqrMagnitude < 0.0001f) {
        right = Vector3.Cross(tangent, Vector3.forward);
    }
    right.Normalize();
    Vector3 newUp = Vector3.Cross(right, tangent).normalized;
    return Quaternion.LookRotation(tangent, newUp);
}
```

---

## Runtime Initialization Flow

### 1. Brush Creation

```csharp
// BaseBrushScript.Create()
public static BaseBrushScript Create(
    Transform parent,
    TrTransform xfInParentSpace,
    BrushDescriptor desc,
    Color color,
    float size_PS)
{
    // Instantiate prefab from descriptor
    GameObject line = Instantiate(desc.m_BrushPrefab);
    line.transform.SetParent(parent);
    line.name = desc.Description;

    // Get brush script
    BaseBrushScript brush = line.GetComponent<BaseBrushScript>();
    brush.m_Color = color;          // Set user color
    brush.m_BaseSize_PS = size_PS;
    brush.InitBrush(desc, xfInParentSpace);

    return brush;
}
```

### 2. Brush Initialization

```csharp
// BaseBrushScript.InitBrush()
protected virtual void InitBrush(BrushDescriptor desc, TrTransform localPointerXf)
{
    m_Desc = desc;
    m_LastSpawnXf = localPointerXf;

    // Get renderer and assign cloned material
    m_Renderer = GetComponent<Renderer>();
    m_Renderer.material = desc.Material;  // Unity clones automatically

    // Initialize geometry
    MeshFilter mf = GetComponent<MeshFilter>();
    mf.mesh = null;  // Force new mesh
    mf.mesh.MarkDynamic();
}
```

### 3. Color Application

```csharp
// During geometry generation, vertex colors are set:
void AppendVert(Vector3 pos, Vector3 normal, Color32 color, Vector2 uv)
{
    m_geometry.m_colors[vertIndex] = color;  // User-selected color
}
```

---

## Audio Reactive Brushes

### Shader Keywords

```hlsl
#pragma multi_compile __ AUDIO_REACTIVE
#pragma multi_compile __ HDR_EMULATED HDR_SIMPLE

#ifdef AUDIO_REACTIVE
    o.color = musicReactiveColor(v.color, _BeatOutput.w);
    v.vertex = musicReactiveAnimation(v.vertex, v.color, _BeatOutput.w, o.texcoord.x);
#endif
```

### Audio Modulation Functions

```hlsl
sampler2D _WaveFormTex;
sampler2D _FFTTex;
uniform float4 _BeatOutputAccum;
uniform float4 _BeatOutput;
uniform float4 _AudioVolume;
uniform float4 _PeakBandLevels;

float4 musicReactiveColor(float4 color, float beat) {
    float randomOffset = randomizeByColor(color);
    color.xyz = color.xyz * .5 + color.xyz * saturate(sin(beat * 3.14159 + randomOffset));
    return color;
}

float4 musicReactiveAnimation(float4 vertex, float4 color, float beat, float t) {
    float intensity = .15;
    float randomOffset = randomizeByColor(color) + _Time.w + vertex.z;
    vertex.xyz += randomNormal(color.rgb) * beat * sin(t * 3.14159) * sin(randomOffset) * intensity;
    return vertex;
}
```

### Bloom Color Function

```hlsl
float4 bloomColor(float4 color, float gain) {
    // Guarantee minimum of all channels (prevents saturated → secondary)
    float cmin = length(color.rgb) * .05;
    color.rgb = max(color.rgb, float3(cmin, cmin, cmin));
    // Gamma correction
    color = pow(color, 2.2);
    // Emission gain
    color.rgb *= 2 * exp(gain * 10);
    return color;
}
```

---

## File Locations

### Shaders

| Shader | Path |
|--------|------|
| DiffuseDoubleSided | `Assets/Resources/Brushes/Shared/Shaders/DiffuseDoubleSided.shader` |
| StandardDoubleSided | `Assets/Resources/Brushes/Shared/Shaders/StandardDoubleSided.shader` |
| Additive | `Assets/Resources/Brushes/Shared/Shaders/Additive.shader` |
| Brush.cginc | `Assets/Shaders/Include/Brush.cginc` |

### Brush Assets

| Category | Path |
|----------|------|
| Basic | `Assets/Resources/Brushes/Basic/{BrushName}/` |
| Environment | `Assets/Resources/Brushes/Environment/` |
| Blocks | `Assets/Resources/Brushes/Blocks/{BrushName}/` |
| Prefabs | `Assets/Resources/BrushPrefabs/` |
| Tube Prefabs | `Assets/Resources/BrushPrefabs/Geom/TubeDistanceUV.prefab` |

---

## Key Differences: OpenBrush vs XRRAI Implementation

| Aspect | OpenBrush | XRRAI (Current) | Status |
|--------|-----------|-----------------|--------|
| **Material._Color** | White (1,1,1,1) | White ✓ | ✅ Fixed |
| **Vertex Color** | Set at runtime | Set at runtime ✓ | ✅ Fixed |
| **Color Conversion** | `TbVertToNative()` in shader | `SrgbToLinear()` ✓ | ✅ Fixed (2026-01-22) |
| **Shader Include** | `Brush.cginc` | URP Core.hlsl ✓ | ✅ Fixed |
| **Double-Sided** | `Cull Off` + VFACE | `Cull Off` ✓ | ✅ Fixed |
| **Tube Sides** | 8 (m_PointsInClosedCircle) | 10-16 ✓ | ✅ Fixed |
| **Flat Shading** | m_HardEdges option | Per-face normals ✓ | ✅ Fixed |

---

## Fixes Applied to XRRAI (2026-01-22)

### 1. ✅ Color Space Conversion (All 5 Shaders)

Applied OpenBrush's fast approximation `SrgbToLinear()` to all brush shaders:

**Files Modified**:
- `Assets/Shaders/BrushStroke.shader`
- `Assets/Shaders/BrushStrokeGlow.shader`
- `Assets/Shaders/BrushStrokeTube.shader`
- `Assets/Shaders/BrushStrokeHull.shader`
- `Assets/Shaders/BrushStrokeParticle.shader`

```hlsl
// sRGB to Linear conversion (OpenBrush TbVertToNative pattern)
// Fast approximation: color * (color * (color * 0.305306011 + 0.682171111) + 0.012522878)
half3 SrgbToLinear(half3 srgb)
{
    return srgb * (srgb * (srgb * 0.305306011h + 0.682171111h) + 0.012522878h);
}

// In vertex shader:
Varyings vert(Attributes input)
{
    // Color space conversion: sRGB vertex colors to Linear (URP requirement)
    #if defined(UNITY_COLORSPACE_GAMMA)
        output.color = input.color;
    #else
        output.color.rgb = SrgbToLinear(input.color.rgb);
        output.color.a = input.color.a;
    #endif
    // ...
}
```

### 2. ✅ Material._Color = White

Applied in `BrushStroke.CreateMaterial()`:
```csharp
mat.color = Color.white;  // Vertex colors carry actual stroke color
```

### 3. ✅ Glow Properties for Emissive Brushes

Added glow property configuration in `BrushStroke.CreateMaterial()`:
```csharp
if (isGlowBrush)
{
    if (mat.HasProperty("_GlowColor"))
        mat.SetColor("_GlowColor", color);
    if (mat.HasProperty("_GlowStrength"))
        mat.SetFloat("_GlowStrength", 2.5f);
    if (mat.HasProperty("_CoreBrightness"))
        mat.SetFloat("_CoreBrightness", 1.5f);
}
```

### 4. ✅ Increased TubeSides for Roundness

Updated in `BrushCatalogFactory.cs`:
- Tube: 8 → 12
- Icing: 12 → 16
- Petal: 8 → 12
- Toon: 8 → 12
- Disco: 8 → 12
- NeonPulse: 6 → 10
- ChromaticWave: 8 → 12
- WaveformTube: 6 → 10

### 5. ✅ Hull Flat Shading

Rewrote `GenerateHull()` in `BrushStroke.cs` with per-face normals for faceted appearance

---

---

## Portals_6 / Tilt Brush Toolkit Patterns

**Source**: `/Users/jamestunick/wkspaces/Portals_6/Assets/TiltBrush/`

### Shader Color Pipeline (3-Stage)

The original Tilt Brush shaders use a 3-stage color pipeline:

```hlsl
// Stage 1: Vertex - Convert to sRGB for intuitive calculations
v.color = TbVertToSrgb(v.color);  // LINEAR → sRGB

// Stage 2: All color calculations done in sRGB space
// (more intuitive for artists)

// Stage 3: Fragment - Convert to native colorspace (Linear for URP)
color = SrgbToNative(color);  // sRGB → LINEAR (if TBT_LINEAR_TARGET)
return color;
```

### Brush.cginc Key Functions

```hlsl
// Fast sRGB to Linear (Chilliant approximation)
float4 SrgbToLinear(float4 color) {
  float3 sRGB = color.rgb;
  color.rgb = sRGB * (sRGB * (sRGB * 0.305306011 + 0.682171111) + 0.012522878);
  return color;
}

// Linear to sRGB (using sqrt approximation)
float4 LinearToSrgb(float4 color) {
  float3 linearColor = color.rgb;
  float3 S1 = sqrt(linearColor);
  float3 S2 = sqrt(S1);
  float3 S3 = sqrt(S2);
  color.rgb = 0.662002687 * S1 + 0.684122060 * S2 - 0.323583601 * S3 - 0.0225411470 * linearColor;
  return color;
}

// Native colorspace conversion (based on TBT_LINEAR_TARGET define)
#ifdef TBT_LINEAR_TARGET
  float4 SrgbToNative(float4 color) { return SrgbToLinear_Large(color); }
  float4 TbVertToNative(float4 color) { return color; }  // TBT colors are already linear
#else
  float4 SrgbToNative(float4 color) { return color; }
  float4 TbVertToNative(float4 color) { return LinearToSrgb(color); }
#endif
```

### Bloom/Glow Function

```hlsl
float4 bloomColor(float4 color, float gain) {
  // Guarantee minimum of all channels (prevents saturated → secondary)
  float cmin = length(color.rgb) * .05;
  color.rgb = max(color.rgb, float3(cmin, cmin, cmin));
  // Gamma correction (sRGB → linear)
  color = pow(color, 2.2);
  // Emission gain (gain 0-1 maps to 2x-180x brightness)
  color.rgb *= 2 * exp(gain * 10);
  return color;
}
```

### Audio Reactive Pattern

```hlsl
#pragma multi_compile __ AUDIO_REACTIVE

#ifdef AUDIO_REACTIVE
  // Waveform displacement
  float waveform = tex2Dlod(_WaveFormTex, float4(uv.x, 0, 0, 0)).r - 0.5;
  disp.y += waveform * 0.1;

  // Beat-driven color boost
  v.color = v.color * 0.5 + v.color * _BeatOutput.z * 0.5;

  // FFT-driven animation
  float fft = tex2Dlod(_FFTTex, float4(pos.y, 0, 0, 0)).b * 2 + 0.1;
#endif
```

### Electricity Shader Pattern (URP)

Multi-pass additive glow with curl noise displacement:

```hlsl
// Tags for URP compatibility
Tags { "LightMode" = "UniversalForward" }
Tags { "LightMode" = "SRPDefaultUnlit" }  // Second pass

// Additive blending
Blend One One
BlendOp Add, Min
```

### Particle Spread System

```hlsl
// From Particles.cginc
float SpreadProgress(float birthTime, float spreadRate) {
  return saturate((_Time.y - birthTime) * spreadRate);
}

float4 SpreadParticle(ParticleVertexWithSpread_t v, float progress) {
  return lerp(v.center, v.corner, progress);
}
```

---

## References

- [OpenBrush GitHub](https://github.com/icosa-foundation/open-brush)
- [Open Brush Toolkit](https://github.com/icosa-foundation/open-brush-toolkit)
- [Tilt Brush Toolkit](https://github.com/googlevr/tilt-brush-toolkit)
- [OpenBrush Documentation](https://docs.openbrush.app/)
- Portals_6 local project: `/Users/jamestunick/wkspaces/Portals_6/`

---

**Last Updated**: 2026-01-22
