# Rcam Series Quick Reference

**TL;DR**: Keijiro's production-proven AR VFX patterns (2020-2025)

---

## Property Naming Convention (Rcam3/4 - STANDARD)

```csharp
// VFX Graph exposed properties
ColorMap           (Texture2D)   // AR camera color
DepthMap           (Texture2D)   // AR depth (RHalf format)
InverseProjection  (Vector4)     // [1/fx, 1/fy, cx/fx, cy/fy]
InverseView        (Matrix4x4)   // TRS(position, rotation, Vector3.one)
```

**DO NOT USE** (Rcam2 legacy):
- `ProjectionVector` → renamed to `InverseProjection`
- `InverseViewMatrix` → renamed to `InverseView`

---

## Depth Reconstruction (Production Pattern)

### C# Side
```csharp
// Calculate InverseProjection from ARCameraFrameEventArgs
Vector4 GetInverseProjection(Matrix4x4 proj) {
    return new Vector4(
        1f / proj[0, 0],          // 1 / focal_x
        1f / proj[1, 1],          // 1 / focal_y
        proj[0, 2] / proj[0, 0],  // center_x / focal_x
        proj[1, 2] / proj[1, 1]   // center_y / focal_y
    );
}

Matrix4x4 GetInverseView(Vector3 pos, Quaternion rot) {
    return Matrix4x4.TRS(pos, rot, Vector3.one);
}
```

### HLSL Side (VFX Graph)
```hlsl
// Reconstruct world position from UV + depth
float3 UVDepthToWorld(float2 uv, float depth, float4 inv_proj, float4x4 inv_view) {
    float3 p = float3((uv - 0.5) * 2, 1);        // UV → NDC
    p.xy = (p.xy * inv_proj.xy) + inv_proj.zw;  // NDC → View space
    return mul(inv_view, float4(p * depth, 1)).xyz;  // View → World
}
```

**Usage in VFX Graph**: Sample DepthMap → pass to function → get world XYZ

---

## VFX Binder Pattern (Rcam3/4)

```csharp
[VFXBinder("Rcam")]
public sealed class RcamBinder : VFXBinderBase {
    [VFXPropertyBinding("UnityEngine.Texture2D")]
    public ExposedProperty ColorMapProperty = "ColorMap";

    [VFXPropertyBinding("UnityEngine.Texture2D")]
    public ExposedProperty DepthMapProperty = "DepthMap";

    [VFXPropertyBinding("UnityEngine.Vector4")]
    public ExposedProperty InverseProjectionProperty = "InverseProjection";

    [VFXPropertyBinding("UnityEngine.Matrix4x4")]
    public ExposedProperty InverseViewProperty = "InverseView";

    public override void UpdateBinding(VisualEffect component) {
        component.SetTexture(ColorMapProperty, source.ColorTexture);
        component.SetTexture(DepthMapProperty, source.DepthTexture);
        component.SetVector4(InverseProjectionProperty, invProj);
        component.SetMatrix4x4(InverseViewProperty, invView);
    }
}
```

**Why ExposedProperty?** Property IDs resolved once (not per-frame string hashing).

---

## VFXProxBuffer (Spatial Proximity Structure)

**When to use**: Plexus/Metaball effects needing nearest-neighbor queries

```csharp
// C# Setup (16x16x16 grid, 32 points/cell)
const int CellsPerAxis = 16;
const int CellCapacity = 32;

GraphicsBuffer pointBuffer;  // 4096 * 32 * float3
GraphicsBuffer countBuffer;  // 4096 * uint

Shader.SetGlobalBuffer("VFXProx_PointBuffer", pointBuffer);
Shader.SetGlobalBuffer("VFXProx_CountBuffer", countBuffer);
```

```hlsl
// HLSL Usage (VFX Graph custom HLSL block)
void VFXProx_AddPoint(float3 pos);  // Add particle to grid
void VFXProx_LookUpNearestPair(float3 pos, out float3 p1, out float3 p2);
```

**Performance**: O(864) queries per particle (27 cells × 32 max points)

---

## Modern Blitter (Unity 6 / URP)

Replaces deprecated `Graphics.Blit`:

```csharp
public class Blitter : System.IDisposable {
    Material _material;

    public Blitter(Shader shader) => _material = new Material(shader);

    public void Run(Texture source, RenderTexture dest, int pass) {
        RenderTexture.active = dest;
        _material.mainTexture = source;
        _material.SetPass(pass);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 3, 1);
    }

    public void Dispose() { Object.Destroy(_material); }
}
```

---

## Depth Format Convention

**Always use**: `RenderTextureFormat.RHalf` (16-bit float)

```csharp
var depthRT = new RenderTexture(width, height, 0, RenderTextureFormat.RHalf) {
    wrapMode = TextureWrapMode.Clamp
};
```

---

## Body vs Environment Separation

**VFX Organization**:
- `Body/` or `BodyFX/` - Human-targeted effects
- `Environment/` or `EnvFX/` - Scene-wide effects

**Stencil Access** (if needed):
```csharp
// AR Foundation provides human segmentation
void OnOcclusionFrameReceived(AROcclusionFrameEventArgs args) {
    for (var i = 0; i < args.textures.Count; i++) {
        var id = args.propertyNameIds[i];
        if (id == ShaderID.HumanStencil)
            // Binary mask: 0 = environment, 1 = human
        if (id == ShaderID.EnvironmentDepth)
            // Full-scene depth map
        if (id == ShaderID.EnvironmentDepthConfidence)
            // Quality metric (0-1)
    }
}
```

**Note**: Standard Rcam binders **don't expose stencil** - create custom binder if needed.

---

## Rcam Series Evolution

| | Rcam2 (2020) | Rcam3 (2024) | Rcam4 (2025) |
|---|---|---|---|
| **Pipeline** | HDRP 8.2.0 | URP 17.0.3 | URP 17.0.3 |
| **Unity** | 2020.1.6 | Unity 6 | Unity 6 |
| **Key Feature** | First LiDAR VFX | VFXProxBuffer | URP Renderer Features |
| **Blit Method** | Graphics.Blit | Graphics.Blit | Blitter class |

**Migration Path**: Rcam2 VFX → rename 2 properties → works in Rcam3/4

---

## Critical Files to Study

**Rcam3/4 (URP)**:
- `VFXRcamBinder.cs` - Standard binder pattern
- `RcamCommon.hlsl` - Depth reconstruction functions
- `VFXProxBuffer.cs` + `VFXProxCommon.hlsl` - Spatial queries
- `FrameEncoder.cs` - AR Foundation texture access

**Rcam4 Only**:
- `Utils.cs` - Blitter class, RTUtil helpers

---

## MetavidoVFX Integration Checklist

- [ ] Rename VFX properties to Rcam convention (`ColorMap`, `InverseProjection`, etc.)
- [ ] Use `ExposedProperty` instead of string literals in binders
- [ ] Implement `GetInverseProjection()` from projection matrix
- [ ] Port `RcamDistanceToWorldPosition()` HLSL function
- [ ] Consider VFXProxBuffer for Plexus/Line effects
- [ ] Replace `Graphics.Blit` with Blitter class (Unity 6+)
- [ ] Use `RenderTextureFormat.RHalf` for all depth textures

---

**Full Research**: `/KnowledgeBase/_RCAM_SERIES_ARCHITECTURE_RESEARCH.md`
