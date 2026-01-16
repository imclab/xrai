# Feature Specification: Body Tracking Particle Fix

**Created**: 2026-01-13
**Status**: Active
**Input**: "particles still not tracking my body or hand. but they do appear"

## Problem Analysis

### Root Cause
The VFX Graph samples Position Map at random UV coordinates but has **no activation condition** to filter out invalid positions. When Position Map contains `(0,0,0,0)` at a UV (for non-body pixels), particles still spawn and end up at world origin (0,0,0) or scattered randomly.

### Evidence
- VFX activation slots show `m_LinkedSlots: []` (not connected to any condition)
- Compute shader outputs `(0,0,0,0)` for invalid positions, `(worldPos, 1.0)` for valid
- But VFX doesn't check the alpha channel before spawning

## User Scenarios & Testing

### User Story 1 - Body-Only Particles (Priority: P1)
User points camera at themselves. Particles should ONLY appear on/around their body silhouette, not in the background or at random positions.

**Why this priority**: Core feature - without this, the hologram effect is broken.

**Independent Test**: Point camera at self. Particles should track body outline. Moving hand should move particles with it.

**Acceptance Scenarios**:
1. **Given** camera pointed at user, **When** ARKit detects body, **Then** particles appear only on body regions
2. **Given** user moves hand, **When** ARKit tracks hand movement, **Then** particles follow hand in real-time
3. **Given** background scene, **When** no body detected, **Then** NO particles appear

## Requirements

### Functional Requirements
- **FR-001**: System MUST only generate particle positions for pixels where humanStencilTexture > 0
- **FR-002**: System MUST output invalid marker (0,0,0,0) for non-body pixels
- **FR-003**: VFX MUST filter out particles at invalid positions

## Technical Approach

### Option A: Compute Shader Fix (Recommended)
Add stencil texture sampling to the compute shader. Only output valid positions where BOTH:
- humanDepthTexture has valid depth (0.1-5.0m)
- humanStencilTexture > 127 (body pixel)

**Pros**: Single change, no VFX graph editing needed
**Cons**: Requires passing stencil texture to compute shader

### Option B: VFX Graph Fix
Edit VFX to add activation condition checking position.alpha > 0.

**Pros**: Uses existing alpha output
**Cons**: VFX graph editing is complex, risk of breaking asset

### Chosen: Option A - Compute Shader Fix

## Implementation Plan

1. Modify `GeneratePositionTexture.compute` to accept StencilTexture input
2. Sample stencil at same UV as depth
3. Only output valid position if stencil > 127
4. Update `PeopleOcclusionVFXManager.cs` to pass humanStencilTexture to compute shader
5. Test and deploy

## Success Criteria
- [ ] Particles only appear on detected body regions
- [ ] No particles at world origin (0,0,0)
- [ ] Particles track body movement in real-time
- [ ] 60 FPS performance maintained
