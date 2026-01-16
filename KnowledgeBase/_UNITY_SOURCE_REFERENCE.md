# Unity Source Code Reference

**Source**: AgentBench (keijiro/AgentBench) - Unity research workbench
**Location**: `Unity-XR-AI/AgentBench/`
**Purpose**: Direct access to Unity engine internals, shader source, and package code

---

## Repository Structure

```
AgentBench/
├── UnityCsReference/          # Unity engine C# source (MIT license)
│   ├── Modules/VFX/           # VFX Graph runtime
│   ├── Modules/XR/            # XR subsystems
│   └── Runtime/Export/iOS/    # iOS device bindings
├── BuiltinShaders/            # Shader source code
│   ├── CGIncludes/            # UnityCG.cginc, HLSLSupport.cginc
│   └── DefaultResourcesExtra/ # Standard shaders
└── UnityProject/              # Minimal project for package cache access
```

---

## VFX Graph Complete API

**Source**: `UnityCsReference/Modules/VFX/Public/ScriptBindings/VisualEffect.bindings.cs`

### VisualEffectAsset

```csharp
// Built-in event names
public const string PlayEventName = "OnPlay";
public const string StopEventName = "OnStop";
public static readonly int PlayEventID = Shader.PropertyToID(PlayEventName);
public static readonly int StopEventID = Shader.PropertyToID(StopEventName);

// Query exposed properties
void GetExposedProperties(List<VFXExposedProperty> exposedProperties);
void GetEvents(List<string> names);
TextureDimension GetTextureDimension(int nameID);
VFXSpace GetExposedSpace(int nameID);
```

### VisualEffect Component

```csharp
// Playback properties
bool pause { get; set; }
float playRate { get; set; }
uint startSeed { get; set; }
bool resetSeedOnPlay { get; set; }
int initialEventID { get; set; }
string initialEventName { get; set; }
bool culled { get; }
VisualEffectAsset visualEffectAsset { get; set; }

// Playback control
void Play();
void Play(VFXEventAttribute eventAttribute);
void Stop();
void Stop(VFXEventAttribute eventAttribute);
void Reinit();
void AdvanceOneFrame();
void SendEvent(int eventNameID);
void SendEvent(string eventName);
void SendEvent(int eventNameID, VFXEventAttribute eventAttribute);

// Property checkers (by nameID or string)
bool HasBool(int nameID);
bool HasInt(int nameID);
bool HasUInt(int nameID);
bool HasFloat(int nameID);
bool HasVector2(int nameID);
bool HasVector3(int nameID);
bool HasVector4(int nameID);
bool HasMatrix4x4(int nameID);
bool HasTexture(int nameID);
bool HasAnimationCurve(int nameID);
bool HasGradient(int nameID);
bool HasMesh(int nameID);
bool HasSkinnedMeshRenderer(int nameID);
bool HasGraphicsBuffer(int nameID);

// Property setters
void SetBool(int nameID, bool b);
void SetInt(int nameID, int i);
void SetUInt(int nameID, uint i);
void SetFloat(int nameID, float f);
void SetVector2(int nameID, Vector2 v);
void SetVector3(int nameID, Vector3 v);
void SetVector4(int nameID, Vector4 v);
void SetMatrix4x4(int nameID, Matrix4x4 v);
void SetTexture(int nameID, Texture t);
void SetAnimationCurve(int nameID, AnimationCurve c);
void SetGradient(int nameID, Gradient g);
void SetMesh(int nameID, Mesh m);
void SetSkinnedMeshRenderer(int nameID, SkinnedMeshRenderer m);
void SetGraphicsBuffer(int nameID, GraphicsBuffer g);

// Property getters
bool GetBool(int nameID);
int GetInt(int nameID);
uint GetUInt(int nameID);
float GetFloat(int nameID);
Vector2 GetVector2(int nameID);
Vector3 GetVector3(int nameID);
Vector4 GetVector4(int nameID);
Matrix4x4 GetMatrix4x4(int nameID);
Texture GetTexture(int nameID);
Mesh GetMesh(int nameID);
Gradient GetGradient(int nameID);
AnimationCurve GetAnimationCurve(int nameID);

// System queries
bool HasSystem(int nameID);
void GetSystemNames(List<string> names);
void GetParticleSystemNames(List<string> names);
void GetOutputEventNames(List<string> names);
void GetSpawnSystemNames(List<string> names);
VFXParticleSystemInfo GetParticleSystemInfo(int nameID);
VFXSpawnerState GetSpawnSystemInfo(int nameID);
bool HasAnySystemAwake();
void ResetOverride(int nameID);
```

### VFXEventAttribute

```csharp
VFXEventAttribute CreateVFXEventAttribute();  // Create from VisualEffect
```

---

## Shader Functions Reference

### Depth Conversion (UnityCG.cginc)

```hlsl
// Z buffer to linear 0..1 depth (normalized)
inline float Linear01Depth(float z)
{
    return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
}

// Z buffer to linear eye-space depth (meters)
inline float LinearEyeDepth(float z)
{
    return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
}

// Convenience macros
#define DECODE_EYEDEPTH(i) LinearEyeDepth(i)
#define COMPUTE_EYEDEPTH(o) o = -UnityObjectToViewPos(v.vertex).z
#define COMPUTE_DEPTH_01 -(UnityObjectToViewPos(v.vertex).z * _ProjectionParams.w)
```

**_ZBufferParams** (set automatically by Unity):
- `x` = 1 - far/near
- `y` = far/near
- `z` = x/far
- `w` = y/far

### Depth Texture Sampling

```hlsl
// Declare depth texture
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

// Sample and convert to eye-space depth
float sceneZ = LinearEyeDepth(
    SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,
    UNITY_PROJ_COORD(i.projectedPosition))
);
```

### Color Space Conversion

```hlsl
// Gamma to Linear (sRGB approximation - fast)
inline half3 GammaToLinearSpace(half3 sRGB)
{
    return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);
}

// Linear to Gamma (fast approximation)
inline half3 LinearToGammaSpace(half3 linRGB)
{
    linRGB = max(linRGB, half3(0.h, 0.h, 0.h));
    return max(1.055h * pow(linRGB, 0.416666667h) - 0.055h, 0.h);
}

// Check color space at runtime
inline bool IsGammaSpace()
{
    #ifdef UNITY_COLORSPACE_GAMMA
        return true;
    #else
        return false;
    #endif
}
```

### Transform Functions

```hlsl
// World to clip space
inline float4 UnityWorldToClipPos(in float3 pos)
{
    return mul(UNITY_MATRIX_VP, float4(pos, 1.0));
}

// View to clip space
inline float4 UnityViewToClipPos(in float3 pos)
{
    return mul(UNITY_MATRIX_P, float4(pos, 1.0));
}

// Object to view space
inline float3 UnityObjectToViewPos(in float3 pos)
{
    return mul(UNITY_MATRIX_V, mul(unity_ObjectToWorld, float4(pos, 1.0))).xyz;
}

// World to view space
inline float3 UnityWorldToViewPos(in float3 pos)
{
    return mul(UNITY_MATRIX_V, float4(pos, 1.0)).xyz;
}

// Object to world direction (normalized)
inline float3 UnityObjectToWorldDir(in float3 dir)
{
    return normalize(mul((float3x3)unity_ObjectToWorld, dir));
}

// World to object direction (normalized)
inline float3 UnityWorldToObjectDir(in float3 dir)
{
    return normalize(mul((float3x3)unity_WorldToObject, dir));
}

// Object to world normal (handles non-uniform scale)
inline float3 UnityObjectToWorldNormal(in float3 norm)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
    return UnityObjectToWorldDir(norm);
#else
    return normalize(mul(norm, (float3x3)unity_WorldToObject));
#endif
}

// World space view direction
inline float3 UnityWorldSpaceViewDir(in float3 worldPos)
{
    return _WorldSpaceCameraPos.xyz - worldPos;
}

// World space light direction
inline float3 UnityWorldSpaceLightDir(in float3 worldPos)
{
    #ifndef USING_DIRECTIONAL_LIGHT
        return _WorldSpaceLightPos0.xyz - worldPos;
    #else
        return _WorldSpaceLightPos0.xyz;
    #endif
}
```

### Fog Macros

```hlsl
// Declare fog interpolator
#define UNITY_FOG_COORDS(idx) float1 fogCoord : TEXCOORD##idx;

// Transfer fog from vertex shader
#define UNITY_TRANSFER_FOG(o, outpos) // Outputs fog data

// Apply fog to color (uses unity_FogColor, or black in forward-add)
#define UNITY_APPLY_FOG(coord, col)

// Apply fog with custom color
#define UNITY_APPLY_FOG_COLOR(coord, col, fogCol)

// Fog factor calculation
#define UNITY_CALC_FOG_FACTOR(coord) UNITY_CALC_FOG_FACTOR_RAW(UNITY_Z_0_FAR_FROM_CLIPSPACE(coord))
```

### Shadow Macros (AutoLight.cginc)

```hlsl
// Declare shadow coordinates
#define SHADOW_COORDS(idx1) unityShadowCoord4 _ShadowCoord : TEXCOORD##idx1;

// Transfer shadow data from vertex shader
#define TRANSFER_SHADOW(a) a._ShadowCoord = mul(unity_WorldToShadow[0], mul(unity_ObjectToWorld, v.vertex));

// Sample shadow attenuation
#define SHADOW_ATTENUATION(a) UnitySampleShadow(a._ShadowCoord)

// Unity-prefixed versions (preferred)
#define UNITY_SHADOW_COORDS(idx1) SHADOW_COORDS(idx1)
#define UNITY_TRANSFER_SHADOW(a, coord) TRANSFER_SHADOW(a)
#define UNITY_SHADOW_ATTENUATION(a, worldPos) UnityComputeForwardShadows(...)

// Combined lighting coords
#define UNITY_LIGHTING_COORDS(idx1, idx2) DECLARE_LIGHT_COORDS(idx1) UNITY_SHADOW_COORDS(idx2)
#define UNITY_TRANSFER_LIGHTING(a, coord) COMPUTE_LIGHT_COORDS(a) UNITY_TRANSFER_SHADOW(a, coord)

// Shadow sampling functions
fixed UnitySampleShadowmap(float4 shadowCoord);        // Directional/spot
half UnitySampleShadowmap(float3 vec);                 // Point light
half UnitySampleShadowmap_PCF3x3(float4 coord, ...);   // 3x3 PCF
half UnitySampleShadowmap_PCF5x5(float4 coord, ...);   // 5x5 PCF
half UnitySampleShadowmap_PCF7x7(float4 coord, ...);   // 7x7 PCF
```

### Standard Vertex Structures

```hlsl
struct appdata_base {
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct appdata_tan {
    float4 vertex : POSITION;
    float4 tangent : TANGENT;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct appdata_full {
    float4 vertex : POSITION;
    float4 tangent : TANGENT;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;
    float4 texcoord2 : TEXCOORD2;
    float4 texcoord3 : TEXCOORD3;
    fixed4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
```

### Tangent Space

```hlsl
// Creates rotation matrix for tangent space
#define TANGENT_SPACE_ROTATION \
    float3 binormal = cross(normalize(v.normal), normalize(v.tangent.xyz)) * v.tangent.w; \
    float3x3 rotation = float3x3(v.tangent.xyz, binormal, v.normal)
```

---

## Platform Detection (HLSLSupport.cginc)

```hlsl
// Shader API detection
#if defined(SHADER_API_METAL)
    // Metal (iOS, macOS, tvOS, visionOS)
#endif

#if defined(SHADER_API_GLES3)
    // OpenGL ES 3.0+ (Android, WebGL 2.0)
#endif

#if defined(SHADER_API_VULKAN)
    // Vulkan (Android, PC)
#endif

#if defined(SHADER_API_D3D11)
    // Direct3D 11 (Windows, Xbox)
#endif

#if defined(SHADER_API_MOBILE)
    // Any mobile platform
#endif

// Compiler detection
#if defined(UNITY_COMPILER_HLSL)
    // HLSL compiler (D3D11, Metal, Vulkan, etc.)
#endif

#if defined(UNITY_COMPILER_HLSLCC)
    // HLSL Cross Compiler (GLES3, Metal, Vulkan, Switch)
#endif

// Feature detection
#if defined(SHADOWS_CUBE_IN_DEPTH_TEX)
    // Supports depth-format cube shadow maps
#endif

#if defined(UNITY_FAST_COHERENT_DYNAMIC_BRANCHING)
    // Hardware supports fast dynamic branching
#endif

// MRT limits
#if (defined(SHADER_API_GLES3) && !defined(SHADER_API_DESKTOP)) || defined(SHADER_API_GLES)
    #define UNITY_ALLOWED_MRT_COUNT 4
#else
    #define UNITY_ALLOWED_MRT_COUNT 8
#endif
```

---

## iOS / Apple Device APIs

**Source**: `UnityCsReference/Runtime/Export/iOS/`

### UnityEngine.iOS.Device

```csharp
// Home button / gesture bar
bool hideHomeButton { get; set; }

// Screen dimming
bool wantsSoftwareDimming { get; set; }

// System gesture deferral (edge swipes)
SystemGestureDeferMode deferSystemGesturesMode { get; set; }

// App Store review prompt
bool RequestStoreReview();
```

### UnityEngine.Apple.Device (Internal)

```csharp
// System info
string systemVersion { get; }      // iOS version string
int generation { get; }            // Device generation enum
string vendorIdentifier { get; }   // Vendor ID

// iCloud backup flags
void SetNoBackupFlag(string path);
void ResetNoBackupFlag(string path);

// Power state
bool lowPowerModeEnabled { get; }

// Advertising (requires ATT on iOS 14+)
string GetAdIdentifier();
bool IsAdTrackingEnabled();

// Platform detection
bool iosAppOnMac { get; }         // iOS app running on Apple Silicon Mac
bool runsOnSimulator { get; }     // Running in Xcode Simulator
```

---

## XR Mesh Subsystem

**Source**: `UnityCsReference/Modules/XR/Subsystems/Meshing/XRMeshSubsystem.bindings.cs`

```csharp
// Mesh identification
public struct MeshId : IEquatable<MeshId>
{
    public static MeshId InvalidId { get; }
    // 128-bit ID (two ulongs)
}

// Mesh generation status
public enum MeshGenerationStatus
{
    Success,
    InvalidMeshId,
    GenerationAlreadyInProgress,
    Canceled,
    UnknownError
}

// Result of mesh generation request
public struct MeshGenerationResult
{
    public MeshId MeshId { get; }
    public Mesh Mesh { get; }
    public MeshCollider MeshCollider { get; }
    public MeshGenerationStatus Status { get; }
    public MeshVertexAttributes Attributes { get; }
    public ulong Timestamp { get; }
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Vector3 Scale { get; }
}
```

---

## AR Depth → World Position Pattern

For compute shaders converting AR depth to world positions:

```hlsl
// Input: UV (0-1), raw depth value, inverse matrices
// Output: World position

// 1. Convert raw depth to linear eye-space depth
float depth = LinearEyeDepth(rawDepth);

// 2. Reconstruct view-space position using RayParams
// RayParams = (0, 0, tan(fov/2)*aspect, tan(fov/2))
float3 viewPos;
viewPos.xy = (uv * 2.0 - 1.0) * RayParams.zw * depth;
viewPos.z = -depth;  // Negative because camera looks down -Z

// 3. Transform to world space
float3 worldPos = mul(InverseView, float4(viewPos, 1.0)).xyz;
```

### Complete Compute Shader Example

```hlsl
#pragma kernel DepthToWorld

Texture2D<float> DepthMap;
RWTexture2D<float4> PositionMap;
float4x4 InverseView;
float4 RayParams;  // (0, 0, tan(fov/2)*aspect, tan(fov/2))

[numthreads(32, 32, 1)]
void DepthToWorld(uint3 id : SV_DispatchThreadID)
{
    uint width, height;
    DepthMap.GetDimensions(width, height);

    if (id.x >= width || id.y >= height) return;

    float2 uv = (float2(id.xy) + 0.5) / float2(width, height);
    float rawDepth = DepthMap[id.xy];

    // Skip invalid depth
    if (rawDepth <= 0.0 || rawDepth >= 1.0)
    {
        PositionMap[id.xy] = float4(0, 0, 0, 0);
        return;
    }

    float depth = LinearEyeDepth(rawDepth);

    float3 viewPos;
    viewPos.xy = (uv * 2.0 - 1.0) * RayParams.zw * depth;
    viewPos.z = -depth;

    float3 worldPos = mul(InverseView, float4(viewPos, 1.0)).xyz;
    PositionMap[id.xy] = float4(worldPos, 1.0);
}
```

---

## Constants Reference

```hlsl
// Math constants (UnityCG.cginc)
#define UNITY_PI            3.14159265359f
#define UNITY_TWO_PI        6.28318530718f
#define UNITY_FOUR_PI       12.56637061436f
#define UNITY_INV_PI        0.31830988618f
#define UNITY_INV_TWO_PI    0.15915494309f
#define UNITY_HALF_PI       1.57079632679f
#define UNITY_HALF_MIN      6.103515625e-5  // 2^-14 (half float minimum)

// Color space constants
#ifdef UNITY_COLORSPACE_GAMMA
    #define unity_ColorSpaceGrey fixed4(0.5, 0.5, 0.5, 0.5)
    #define unity_ColorSpaceDouble fixed4(2.0, 2.0, 2.0, 2.0)
#else // Linear
    #define unity_ColorSpaceGrey fixed4(0.214041144, 0.214041144, 0.214041144, 0.5)
    #define unity_ColorSpaceDouble fixed4(4.59479380, 4.59479380, 4.59479380, 2.0)
#endif

// Lightmap encoding
#define LIGHTMAP_RGBM_SCALE 5.0
#define EMISSIVE_RGBM_SCALE 97.0
```

---

## Search Patterns

```bash
# Find VFX property methods
grep -r "SetTexture\|SetGraphicsBuffer\|SetVector" UnityCsReference/Modules/VFX/

# Find iOS-specific code
grep -r "PLATFORM_IOS\|NativeConditional" UnityCsReference/Runtime/Export/iOS/

# Find depth functions
grep -r "LinearEyeDepth\|Linear01Depth\|DECODE_EYEDEPTH" BuiltinShaders/

# Find Metal-specific shaders
grep -r "SHADER_API_METAL" BuiltinShaders/CGIncludes/

# Find shadow macros
grep -r "SHADOW_COORDS\|TRANSFER_SHADOW" BuiltinShaders/CGIncludes/

# Find fog macros
grep -r "UNITY_FOG\|UNITY_APPLY_FOG" BuiltinShaders/CGIncludes/
```

---

## Online Documentation

| Topic | URL |
|-------|-----|
| Unity Manual | https://docs.unity3d.com/Manual/ |
| Scripting API | https://docs.unity3d.com/ScriptReference/ |
| VFX Graph | https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.0/ |
| AR Foundation | https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/ |
| URP | https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/ |

---

## Setup Instructions

```bash
cd Unity-XR-AI
git clone https://github.com/keijiro/AgentBench.git
cd AgentBench
./setup.sh  # Downloads UnityCsReference + BuiltinShaders
```

**Note**: Unity 6+ doesn't include offline Documentation folder. Use online docs.

---

**Last Updated**: 2026-01-15
**Maintained By**: Unity-XR-AI Project
