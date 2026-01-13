# Unity URP Optimization Guide

> **Source**: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/optimize-for-better-performance.html  
> **Last Updated**: 2025-01-13

---

## Overview

Performance optimization guide for Universal Render Pipeline (URP) projects.

---

## Profiling Tools

### Unity Profiler
Use the [Unity Profiler](https://docs.unity3d.com/Manual/Profiler.html) for CPU and memory performance data.

### GPU Profilers
- **Xcode** - iOS/macOS GPU profiling
- **RenderDoc** - Cross-platform (less accurate timing)

### Other Tools
- Scene View Options
- Rendering Debugger
- Frame Debugger

---

## Key Profiler Markers

| Marker | Description |
|--------|-------------|
| `Inl_UniversalRenderPipeline.RenderSingleCameraInternal` | URP builds render commands for a camera |
| `Inl_ScriptableRenderer.Setup` | Prepares render textures, shadow maps |
| `CullScriptable` | Culls GameObjects/lights outside camera view |
| `Inl_ScriptableRenderContext.Submit` | Submits commands to graphics API |
| `MainLightShadow` | Renders shadow map for main Directional Light |
| `AdditionalLightsShadow` | Renders shadow maps for other lights |
| `UberPostProcess` | Renders post-processing effects |
| `RenderLoop.DrawSRPBatcher` | SRP Batcher renders object batches |
| `CopyColor` | Copies color buffer between render textures |
| `CopyDepth` | Copies depth buffer between render textures |
| `FinalBlit` | Copies render texture to camera target |

---

## Performance Settings Quick Reference

### Shadows (URP Asset > Shadows)

| Setting | For Better Performance |
|---------|----------------------|
| Main Light > Cast Shadows | Disable |
| Additional Lights > Cast Shadows | Disable |
| Additional Lights > Shadow Atlas Resolution | Set to lowest acceptable |
| Additional Lights > Shadow Resolution | Set to lowest acceptable |
| Cascade Count | Set to lowest acceptable |
| Max Distance | Reduce |
| Soft Shadows | Disable or set to Low |
| Conservative Enclosing Sphere | Enable |

### Lighting (URP Asset > Lighting)

| Setting | For Better Performance |
|---------|----------------------|
| Additional Lights > Per Object Limit | Lowest acceptable (no effect on Deferred/Forward+) |
| Additional Lights > Cookie Atlas Format | Color Low |
| Additional Lights > Cookie Atlas Resolution | Lowest acceptable |

### Quality (URP Asset > Quality)

| Setting | For Better Performance |
|---------|----------------------|
| Render Scale | Below 1.0 |
| LOD Cross Fade Dither | Bayer Matrix |
| Upscaling Filter | Bilinear or Nearest-Neighbor |

### Post Processing (URP Asset > Post Processing)

| Setting | For Better Performance |
|---------|----------------------|
| Grading Mode | Low Dynamic Range |
| LUT size | Lowest acceptable |
| Fast sRGB/Linear conversion | Enable |

### Rendering (URP Asset > Rendering)

| Setting | For Better Performance |
|---------|----------------------|
| Opaque Texture | Disable if not needed |
| Opaque Downsampling | 4x Bilinear (if Opaque Texture enabled) |
| Depth Texture | Disable unless needed for shaders |

### Universal Renderer

| Setting | For Better Performance |
|---------|----------------------|
| Accurate G-buffer normals | Disable (if using Deferred) |
| Decal Technique | Screen Space with Normal Blend Low/Medium |

---

## Additional Resources

- [Understand performance in URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/understand-performance.html)
- [Configure for better performance](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/configure-for-better-performance.html)
- [Graphics performance and profiling](https://docs.unity3d.com/Manual/graphics-performance-profiling.html)

---

*Created for Unity-XR-AI Knowledge Base*
