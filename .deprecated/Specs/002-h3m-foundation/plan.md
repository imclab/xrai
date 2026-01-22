# Implementation Plan: H3M Hologram Foundation (MVP)

**Feature Branch**: `002-h3m-foundation`
**Created**: 2025-12-06
**Spec**: [Link to Spec](../spec.md)
**Status**: Planning

## Summary

This plan outlines the technical steps to build the **H3M Hologram Foundation** MVP ("Man in the Mirror").
We will implement an iOS App using **AR Foundation** (Depth/Occlusion) and **VFX Graph** to create a real-time volumetric hologram of the user on a tabletop.
Key constraints: **MetavidoVFX** architecture, **30+ FPS**, **Stencil Segmentation**.

## Technical Context

**Language/Version**: C# (Unity 6000.1.2f1)
**Primary Dependencies**:
- **AR Foundation 6.0+** (Depth, Occlusion, Plane Detection)
- **Visual Effect Graph** (Rendering)
- **Universal Render Pipeline (URP)**
- **MetavidoVFX** (Reference implementation for Rcam4)
**Target Platform**: iOS 16+ (LiDAR devices preferred: iPhone 12 Pro+)
**Performance Goals**: 30 FPS min, <100ms latency.
**Project Type**: Mobile AR (Unity)

## Constitution Check

- [x] **Holographic First**: Output is 100% volumetric particles.
- [x] **Cross-Platform**: Built on standard Unity X/AR Foundation (portability).
- [x] **Robustness**: Unit tests for "Network" stubs; Debug UI built-in.
- [x] **SDD**: Spec validated; Checklists used.

## Project Structure

### Documentation

```text
specs/002-h3m-foundation/
├── plan.md              # This file
├── research.md          # Technical analysis of Rcam4 vs. AR Foundation 6
└── tasks.md             # Task breakdown
```

### Source Code (`Assets/`)

We will adopt a structure consistent with `MetavidoVFX` but modernized for ARF 6.

```text
Assets/
├── H3M/
│   ├── Core/
│   │   ├── HologramSource.cs      # Data provider (RGB + Depth + Stencil)
│   │   ├── HologramRenderer.cs    # VFX Graph Controller
│   │   └── HologramAnchor.cs      # AR Placement logic
│   ├── Pipelines/
│   │   └── PointCloud.compute     # Preprocessing (Depth -> Points)
│   ├── VFX/
│   │   └── Hologram.vfx          # The core Visual Effect Graph
│   └── Editor/
│       └── HologramBuildProcessor.cs # Automated iOS build settings
├── Scenes/
│   └── H3M_Mirror_MVP.unity       # The main scene
└── Tests/
    └── H3M/                      # PlayMode tests
```

---

## Technical Approach

### 1. RGBD Capture Strategy
- Instead of raw `ARCameraBackground`, we must access the underlying textures.
- **Color**: `ARCameraManager.TryAcquireLatestCpuImage` (converted to Texture2D) OR `ARCameraBackground.material.mainTexture`.
- **Depth**: `AROcclusionManager.environmentDepthTexture`.
- **Stencil**: `AROcclusionManager.humanStencilTexture` (Critical for "Man in the Mirror" cutout).

### 2. Compute Shader Preprocessing
- We cannot feed raw AR textures directly to VFX Graph efficiently if we want struct-based particle data.
- **Compute Shader (`PointCloud.compute`)**:
    - Filter: If `Stencil > 0.5` (Person) -> Emit Particle. Else -> Discard.
    - Transform: Unproject (UV + Depth) -> World Space (XYZ).
    - Store: Write valid points to a `GraphicsBuffer`.

### 3. VFX Graph Integration
- Input: `GraphicsBuffer` (Position/Color) from Compute Shader.
- System: `Initialize Particle` (Set Position/Color from Buffer).
- Output: `Output Particle Quad` (Billboard).

### 4. Tabletop "Mini Me"
- The computed World Space points will be relative to the Camera.
- We need to transform them into the `HologramAnchor`'s local space.
- Shader logic: `(PointWorld - CameraPos) * Scale + AnchorPos`.

## Phased Execution

- **Phase 1: Pipeline Validation**: Can we get Depth+Stencil on screen?
- **Phase 2: Point Cloud Rendering**: "Ghost" style rendering (Full depth).
- **Phase 3: Segmentation & Scale**: "Mini Me" cutout.
- **Phase 4: Optimization**: 30 FPS target check.
