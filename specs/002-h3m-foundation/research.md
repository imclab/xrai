# Research: H3M Hologram Foundation (Phase 0)

**Feature**: 002-h3m-foundation
**Date**: 2025-12-06
**Status**: Validated

## 1. Existing Assets ("The Blueprint")

We have a working reference in `MetavidoVFX-main/Assets/Scripts/Rcam4/H3MLiDARCapture.cs`.
**Key Findings**:
- **Script**: `H3MLiDARCapture.cs` already implements the core binding logic.
- **Inputs**:
    - `_colorProvider`: Standard `ARCameraTextureProvider`.
    - `_occlusionManager`: `AROcclusionManager`.
    - `_vfx`: `VisualEffect`.
- **Logic**:
    - Sets `ColorMap` and `DepthMap`.
    - Sets `InverseView` matrix (Camera -> World).
    - Sets `RayParams` for unprojection.

## 2. Gap Analysis for "Man in the Mirror"

The current `Rcam4` implementation focuses on **LiDAR Environment Depth** (Static Geometry).
For the "Man in the Mirror" MVP, we need **Human Segmentation**.

**Analysis of `Rcam4Scene.unity` settings**:
- `m_EnvironmentDepthMode: 3` (Best) -> Good for walls/tables.
- `m_HumanSegmentationStencilMode: 0` (Disabled) -> **CRITICAL GAP**.

**Action Item**:
- We MUST enable `HumanSegmentationStencilMode` (Set to Best/2) in the AR Manager.
- We MUST bind `AROcclusionManager.humanStencilTexture` to the VFX Graph.
- We MUST add a `StencilMap` property to the VFX Graph.
- We MUST add a filter step in the Shader/VFX to discard points where `Stencil < 0.5`.

## 3. Technology Strategy

- **Pipeline**: AR Texture -> Compute Shader (Unproject & Filter) -> VFX Graph (Attributes).
- **Optimization**:
    - "Compute Shader" approach allows us to filter *before* the VFX Graph spawns particles, saving GPU cycles.
    - If efficient enough, we can use the `humanStencilTexture` purely as a mask in the Compute Shader.

## 4. Conclusion

The "Simple Hologram Pipeline" is comprised of:
1.  **Input**: AR Kit Depth + Color + Human Stencil.
2.  **Process**: Unproject Depth to World Points (Compute Shader).
3.  **Filter**: Mask by Stencil (Compute Shader).
4.  **Output**: Render Points (VFX Graph).

This confirms the tasks in `tasks.md` are correct, but explicitly highlights the need to *enable* segmentation on the Unity Component.
