# AR + VFX Human Particle Patterns

## Overview

Collection of techniques for spawning VFX particles based on AR depth data (human segmentation, LiDAR, face tracking). These patterns are foundational to MetavidoVFX and similar AR+VFX projects.

---

## Core Pattern: Human Depth → World Positions → VFX

**Pipeline:**
```
AROcclusionManager.humanDepthTexture
    ↓ (Compute Shader)
UV Adjustment → Depth Sampling → World Position Calculation
    ↓
RenderTexture (PositionMap)
    ↓
VFX Graph: Set Position From Map
```

**Key Insight:** AR depth textures have different UV orientation/resolution than screen. Must use UV adjustment formula.

---

## Reference Implementations

### 1. HumanParticleEffect (Qiita/YoHana19)
**Source:** https://github.com/YoHana19/HumanParticleEffect
**Article:** https://qiita.com/yohanashima/items/dd3f1ea20fc783bbcd8c

**Environment:**
- Unity 2019.3+
- URP 7.2.1+
- AR Foundation 3.1.0+ (ARKit 3)
- VFX Graph 7.2.1+
- iPhone 11 Pro (LiDAR)

**Key Components:**

```csharp
// Get human depth texture
var humanDepthTexture = _arOcclusionManager.humanDepthTexture;

// Calculate UV multiplier for portrait/landscape
private float CalculateUVMultiplierPortrait(Texture tex) {
    float screenAspect = (float)Screen.height / Screen.width;
    float cameraTextureAspect = (float)tex.width / tex.height;
    return screenAspect / cameraTextureAspect;
}

// Viewport inverse matrix for screen→world conversion
private void SetViewPortInv() {
    _viewportInv = Matrix4x4.identity;
    _viewportInv.m00 = _viewportInv.m03 = Screen.width / 2f;
    _viewportInv.m11 = Screen.height / 2f;
    _viewportInv.m13 = Screen.height / 2f;
    _viewportInv.m22 = (_camera.farClipPlane - _camera.nearClipPlane) / 2f;
    _viewportInv.m23 = (_camera.farClipPlane + _camera.nearClipPlane) / 2f;
    _viewportInv = _viewportInv.inverse;
}

// Converter matrix: view^-1 * proj^-1 * viewport^-1
private Matrix4x4 GetConverter() {
    Matrix4x4 viewMatInv = _camera.worldToCameraMatrix.inverse;
    Matrix4x4 projMatInv = _camera.projectionMatrix.inverse;
    return viewMatInv * projMatInv * _viewportInv;
}
```

**Compute Shader (HumanDepthMapper.compute):**
```hlsl
#pragma kernel Portrait
#pragma kernel Landscape

RWTexture2D<float4> target;
Texture2D<float4> origin;
float3 cameraPos;
float4x4 converter;
int isWide;
float uVFlip;
float uVMultiplierPortrait;
float uVMultiplierLandScape;

SamplerState _LinearClamp;

// UV adjustment for depth texture → screen UV
float2 adjustUV(float2 uv) {
    if (isWide == 1) {
        float2 forMask = float2(uv.x, (1.0 - (uVMultiplierLandScape * 0.5f)) + (uv.y / uVMultiplierLandScape));
        return float2(lerp(1.0 - forMask.x, forMask.x, uVFlip), lerp(forMask.y, 1.0 - forMask.y, uVFlip));
    } else {
        float2 forMask = float2((1.0 - (uVMultiplierPortrait * 0.5f)) + (uv.x / uVMultiplierPortrait), uv.y);
        return float2(1.0 - forMask.y, 1.0 - forMask.x);
    }
}

// Screen position + depth → world position
float3 getWorldPosition(uint2 screenPos, float distanceFromCamera) {
    float4 pos = float4((float)screenPos.x, (float)screenPos.y, 0, 1);
    float4 converted = mul(converter, pos);
    float3 onNearClip = converted.xyz / converted.w;
    float3 vec = onNearClip - cameraPos;
    float dist = sqrt(vec.x * vec.x + vec.y * vec.y + vec.z * vec.z);
    return cameraPos + vec * distanceFromCamera / dist;
}

[numthreads(25,29,1)]
void Portrait(uint3 id : SV_DispatchThreadID) {
    float tWidth, tHeight;
    target.GetDimensions(tWidth, tHeight);
    float2 uvOrigin = adjustUV(float2((float)id.x/tWidth, (float)id.y/tHeight));
    float4 t = origin.SampleLevel(_LinearClamp, uvOrigin, 0);
    if (t.x > 0) {
        // 0.625 is empirical correction factor for ARKit depth
        float4 depth = float4(getWorldPosition(id.xy, t.x * 0.625f), 1);
        target[id.xy] = depth;
    } else {
        target[id.xy] = float4(0, -10, 0, 1); // Outside view
    }
}
```

**Thread Group Size:** `[numthreads(25,29,1)]` for Portrait (iPhone screen ~1125×2436, divisible)

---

### 2. ARVolumeVFX (EyezLee)
**Source:** https://github.com/EyezLee/ARVolumeVFX

**Environment:**
- Unity 2021.2+
- URP
- VFX Graph
- AR Foundation + ARKit (LiDAR devices)

**Components:**
- **LidarDataProcessor** - Processes environment/human depth and stencil data
- **VFXLidarDataBinder** - Binds AR data to VFX Graph properties

**VFX Subgraph Tools:**
- **Environment Mesh Position** - Read vertices from AR environment mesh
- **Human Froxel** - Set particle positions to human body
- **Kill Nonhuman** - Remove particles outside human stencil mask

**Key Pattern:** Separates data processing (LidarDataProcessor) from VFX binding (VFXLidarDataBinder).

---

### 3. FaceTracking-VFX (mao-test-h)
**Source:** https://github.com/mao-test-h/FaceTracking-VFX

**Environment:**
- Unity 2019.1+
- LWRP 5.7.2
- VFX Graph preview-5.13.0
- ARKit Face Tracking

**References:** Uses keijiro/Smrvfx patterns for face mesh → VFX integration.

---

### 4. MyakuMyakuAR (asus4)
**Source:** https://github.com/asus4/MyakuMyakuAR

**Environment:**
- Unity 6000+
- URP 17.0.4
- VFX Graph 17.0.4
- ARCore/ARKit

**Key Packages:**
```json
"com.github.asus4.arfoundationreplay": "AR recording/playback",
"com.github.asus4.onnxruntime": "ONNX Runtime for ML inference",
"com.github.asus4.texture-source": "Texture source utilities"
```

**Pattern:** Uses YOLO11 segmentation for instance-aware AR effects. ONNX Runtime for on-device ML inference.

---

## MetavidoVFX Implementation

Our implementation follows the same core pattern with optimizations:

**File:** `Assets/Scripts/VFX/HumanParticleVFX.cs`
**Compute:** `Assets/Resources/DepthToWorld.compute`

**Key Optimizations:**
1. **Single Compute Dispatch** - ARDepthSource singleton handles ONE dispatch for all VFX
2. **ExposedProperty** - Uses VFX Graph's native property system instead of string IDs
3. **Thread Group Queries** - Dynamic `GetKernelThreadGroupSizes()` instead of hardcoded values
4. **CeilToInt for Dispatch** - Prevents edge-case pixel loss on non-divisible resolutions

```csharp
// Correct dispatch calculation
int groupsX = Mathf.CeilToInt((float)width / threadSizeX);
int groupsY = Mathf.CeilToInt((float)height / threadSizeY);
computeShader.Dispatch(kernel, groupsX, groupsY, 1);
```

---

## VFX Graph Setup

**Required Properties:**
| Name | Type | Description |
|------|------|-------------|
| PositionMap | Texture2D | RGB = world XYZ positions |
| ColorMap | Texture2D | Camera color for particle tinting |
| DepthMap | Texture2D | Raw depth (optional) |
| StencilMap | Texture2D | Human segmentation mask |

**VFX Graph Nodes:**
1. **Initialize Particle** → Set Position from Map (PositionMap)
2. **Update Particle** → Sample Gradient (ColorMap) for color
3. **Output** → Lit Cube/Sphere mesh particles

---

## Common Issues & Fixes

### 1. UV Mismatch Between Depth and Screen
**Symptom:** Particles appear rotated or offset
**Fix:** Use UV adjustment formula (see `adjustUV()` above)

### 2. Depth Scale Factor
**Symptom:** Particles at wrong distance
**Fix:** ARKit depth needs `* 0.625f` empirical correction (may vary by device)

### 3. Integer Division Truncation
**Symptom:** Missing particles at screen edges
**Fix:** Use `CeilToInt()` for compute dispatch groups

### 4. Thread Group Size Mismatch
**Symptom:** Crash or visual artifacts
**Fix:** Query thread sizes with `GetKernelThreadGroupSizes()`, match in Dispatch call

### 5. Orientation Changes
**Symptom:** Particles break on device rotation
**Fix:** Reinitialize RenderTextures and UV multipliers on orientation change

---

## Related Repositories

| Repo | Focus | Key Technique |
|------|-------|---------------|
| [keijiro/Rcam2](https://github.com/keijiro/Rcam2) | Depth+Body VFX | NDI streaming, HDRP |
| [keijiro/Akvfx](https://github.com/keijiro/Akvfx) | Kinect Azure VFX | Point cloud to VFX |
| [keijiro/Smrvfx](https://github.com/keijiro/Smrvfx) | Skinned mesh VFX | Mesh → point cloud |
| [YoHana19/HumanParticleEffect](https://github.com/YoHana19/HumanParticleEffect) | Human depth VFX | Compute shader UV fix |
| [EyezLee/ARVolumeVFX](https://github.com/EyezLee/ARVolumeVFX) | AR LiDAR toolkit | Subgraph tools |

---

## Open Brush Integration

For VR painting integration with voice-to-object:

**Packages:**
```json
"com.icosa.api-client": "Icosa Gallery API for 3D asset search",
"com.icosa.open-brush-unity-tools": "Open Brush shaders/materials",
"com.icosa.strokereceiver": "Real-time stroke data from Open Brush",
"org.khronos.unitygltf": "glTF loading"
```

**Pipeline:** Whisper transcription → keyword extraction → Icosa API search → glTF import → AR placement

---

## Performance Considerations

| Technique | GPU Cost | Notes |
|-----------|----------|-------|
| Full-screen compute | ~2-3ms | iPhone 11 Pro, 1125×2436 |
| Downsampled compute | ~0.5ms | 256×256 position map |
| VFX particle rendering | ~1-2ms | 50K particles |

**Recommendation:** Use downsampled position maps (256×256 or 512×512) for mobile. Full resolution only needed for pixel-accurate effects.

---

*Last Updated: 2026-01-17*
*Added: Qiita article patterns, MyakuMyakuAR, ARVolumeVFX, FaceTracking-VFX*
