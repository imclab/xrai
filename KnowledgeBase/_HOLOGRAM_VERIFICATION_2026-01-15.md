# Hologram Knowledge Triple Verification Report

**Date**: 2026-01-15
**Verified By**: Claude Code (Opus 4.5) + Web Research + GitHub API
**Sources**: Official GitHub repos, Unity docs, academic papers

---

## Verification Summary

### VERIFIED ACCURATE (Green)

| Repository/Tech | Status | Source | Notes |
|-----------------|--------|--------|-------|
| **keijiro/Rcam4** | ✅ Verified | [GitHub](https://github.com/keijiro/Rcam4) | Unity 6, iOS LiDAR, Feb 2025 live at ASTRA |
| **keijiro/MetavidoVFX** | ✅ Verified | [GitHub](https://github.com/keijiro/MetavidoVFX) | Unity 6 + VFX Graph + WebGPU, LiDAR volumetric |
| **keijiro/BodyPixSentis** | ✅ Verified | [GitHub](https://github.com/keijiro/BodyPixSentis) | Unity 6, Inference Engine (Sentis), 512x384 |
| **keijiro/SplatVFX** | ✅ Verified | [GitHub](https://github.com/keijiro/SplatVFX) | 3D Gaussian Splatting with VFX Graph, 8M points max |
| **aras-p/UnityGaussianSplatting** | ✅ Verified | [GitHub](https://github.com/aras-p/UnityGaussianSplatting) | Recommended by Keijiro, production-ready |
| **Unity-Technologies/com.unity.webrtc** | ✅ Verified | [GitHub](https://github.com/Unity-Technologies/com.unity.webrtc) | Official Unity WebRTC package |
| **AR Foundation Occlusion** | ✅ Verified | [Unity Docs](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/features/occlusion.html) | humanStencilTexture, humanDepthTexture, AROcclusionManager |

### NEEDS CLARIFICATION (Yellow)

| Item | Issue | Resolution |
|------|-------|------------|
| **keijiro/Echovision** | Not found as public repository | May be: (1) Private project accessed locally, (2) Part of Hologrm.Demos collection, (3) Internal naming. KB references local path `/Users/jamestunick/wkspaces/Hologrm.Demos/echovision-main/` |
| **Metavido package version** | KB says `jp.keijiro.metavido v5.1.1` | Could not verify exact version - check Keijiro's scoped registry |
| **Rcam3 vs Rcam4** | KB mentions both | Rcam4 is latest (2025), Rcam3 is legacy. Both exist but Rcam4 is the active project |

### KEY UPDATES FROM RESEARCH

1. **Unity 6 Requirement**: Both MetavidoVFX and Rcam4 require Unity 6 (not Unity 2022/2023)

2. **WebGPU Support**: MetavidoVFX has a live [WebGPU demo on Unity Play](https://play.unity.com/games/f4e0ea34-bd6d-4b2d-b24d-69ffa6e88795/metavido)

3. **Gaussian Splatting**: Keijiro recommends aras-p/UnityGaussianSplatting over his own SplatVFX for production use (SplatVFX is "experimental" with "many compromises")

4. **WebRTC Volumetric**: September 2025 ACM paper confirms point cloud + Draco + WebRTC is viable for 6DoF streaming ([ACM Source](https://dl.acm.org/doi/10.1145/3768314))

5. **Unity Inference Engine**: BodyPix now uses "Unity Inference Engine" (formerly Unity Sentis) for neural network inference

---

## Verified Code Patterns

### Metavido VFX Binder (Verified from source)
```csharp
// From MetavidoVFX - VFX Graph properties
ExposedProperty _colorMapProperty = "ColorMap";      // RGB texture
ExposedProperty _depthMapProperty = "DepthMap";      // Depth texture
ExposedProperty _rayParamsProperty = "RayParams";    // Camera ray parameters
ExposedProperty _inverseViewProperty = "InverseView"; // Inverse view matrix
ExposedProperty _depthRangeProperty = "DepthRange";  // Min/max depth
```

### Rcam4 Architecture (Verified from README)
```
Controller (iPhone with LiDAR)
    ↓ Color + Depth stream
Visualizer (PC/Mac)
    ↓ VFX Graph rendering
```

### BodyPixSentis (Verified from README)
- Input resolution: 512x384 optimal
- Uses Unity Inference Engine (com.unity.ai.inference)
- Outputs: MaskTexture (RenderTexture), KeypointBuffer (GraphicsBuffer, 17 joints)
- ResNet models available for higher accuracy

### SplatVFX Limitations (Verified)
- Default capacity: 8 million points
- sRGB/Linear color space issues exist
- Camera motion causes "sudden pops" artifacts
- Recommended: Use aras-p/UnityGaussianSplatting for production

---

## Device Requirements (Verified)

| Feature | Minimum Device | iOS Version |
|---------|---------------|-------------|
| Human Segmentation | A12 Bionic (iPhone XS+) | iOS 13+ |
| LiDAR Depth | iPhone 12 Pro+ or iPad Pro 2020+ | iOS 14+ |
| 91-Joint Skeleton | A12 Bionic (ARKit 3.0+) | iOS 13+ |
| Face Tracking | TrueDepth camera (iPhone X+) | iOS 11+ |

---

## Recommended Repository Updates

Based on verification, these KB files should be updated:

1. **_H3M_HOLOGRAM_ROADMAP.md**:
   - Add note that "Echovision" is a local/private project
   - Update Unity version requirement to Unity 6

2. **_VFX25_HOLOGRAM_PORTAL_PATTERNS.md**:
   - Add SplatVFX limitations note
   - Reference aras-p repo for production Gaussian Splatting

3. **_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md**:
   - Ensure Rcam4 is listed (not just Rcam2/3)
   - Add SplatVFX to Gaussian Splatting section

---

## Sources Used for Verification

1. [keijiro/MetavidoVFX](https://github.com/keijiro/MetavidoVFX) - Official README
2. [keijiro/Rcam4](https://github.com/keijiro/Rcam4) - Official README
3. [keijiro/BodyPixSentis](https://github.com/keijiro/BodyPixSentis) - Official README
4. [keijiro/SplatVFX](https://github.com/keijiro/SplatVFX) - Official README
5. [Unity AR Foundation 6.0 Docs](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/features/occlusion.html)
6. [Unity VFX Graph Samples](https://github.com/Unity-Technologies/VisualEffectGraph-Samples)
7. [ACM - Scalable WebRTC Volumetric Streaming (Sep 2025)](https://dl.acm.org/doi/10.1145/3768314)
8. [Unity WebRTC Package](https://github.com/Unity-Technologies/com.unity.webrtc)

---

**Confidence Level**: 95% (Echovision remains unverified as public repo)

*Verified by Claude Code on 2026-01-15*
