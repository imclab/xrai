# Feature Specification: MetavidoVFX Systems (EchoVision + Hand Tracking)

**Feature Branch**: `004-metavidovfx-systems`
**Created**: 2026-01-14
**Status**: Implemented
**Input**: User requests for hand tracking VFX, audio-reactive particles, EchoVision AR mesh integration, and performance optimization.

## Overview

This spec documents the new systems added to MetavidoVFX for:
1. Hand tracking with velocity-driven VFX
2. Audio-reactive particle systems
3. EchoVision AR mesh → VFX pipeline
4. Performance optimization (auto-optimizer, LOD, profiler)
5. UI systems for VFX selection

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Hand-Driven VFX Painting (Priority: P1)
As a user, I want to move my hand and see particles follow my hand movement, with the particle trail length and width controlled by my hand velocity and pinch gesture.

**Independent Test**:
1. Run app with HoloKit hand tracking enabled
2. Move hand through space
3. Verify particles emit from wrist position
4. Verify faster movement = longer trails
5. Pinch fingers - verify brush width changes

**Acceptance Scenarios**:
1. **Given** hand tracking is active, **When** I move my hand, **Then** VFX follows HandPosition
2. **Given** pinch gesture, **When** I pinch, **Then** BrushWidth property changes
3. **Given** hand velocity, **When** I move fast, **Then** TrailLength increases

---

### User Story 2 - Audio-Reactive VFX (Priority: P1)
As a user, I want particles to respond to sound - bass drops make particles larger, treble makes them faster.

**Independent Test**:
1. Enable microphone
2. Play music or speak
3. Verify AudioVolume/AudioBass/AudioMid/AudioTreble update
4. Verify VFX responds to frequency bands

**Acceptance Scenarios**:
1. **Given** microphone input, **When** I make sound, **Then** AudioVolume updates 0-1
2. **Given** bass-heavy audio, **When** bass detected, **Then** AudioBass > 0.5
3. **Given** sound wave emitter, **When** volume threshold hit, **Then** wave expands

---

### User Story 3 - AR Mesh VFX (EchoVision) (Priority: P1)
As a user, I want particles to spawn on real-world surfaces detected by ARMeshManager.

**Independent Test**:
1. Enable AR mesh scanning
2. Point at environment
3. Verify MeshPointCache buffer receives vertices
4. Verify VFX particles spawn on mesh surfaces

**Acceptance Scenarios**:
1. **Given** ARMeshManager running, **When** mesh updates, **Then** MeshVFX pushes buffer
2. **Given** VFX with MeshPointCache, **When** enabled, **Then** particles spawn on surfaces

---

### User Story 4 - Performance Optimization (Priority: P2)
As a user, I want smooth 60 FPS even with complex VFX.

**Independent Test**:
1. Enable VFXAutoOptimizer
2. Enable many expensive VFX
3. Verify FPS stays above 45
4. Verify quality reduces when FPS drops
5. Verify quality recovers when FPS improves

**Acceptance Scenarios**:
1. **Given** FPS < 45, **When** optimizer checks, **Then** quality reduces
2. **Given** distance > 10m from VFX, **When** LOD checks, **Then** particle count reduces

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Hand tracking MUST support HoloKit (`HOLOKIT_AVAILABLE`) and XR Hands fallback
- **FR-002**: Audio analysis MUST provide bass/mid/treble frequency bands
- **FR-003**: MeshVFX MUST use GraphicsBuffers (not deprecated ComputeBuffers)
- **FR-004**: VFXAutoOptimizer MUST maintain 45+ FPS via quality reduction
- **FR-005**: VFX selector UI MUST match original MetavidoVFX style (Inconsolata font)

### Key Components

| Component | File | Purpose |
|-----------|------|---------|
| HandVFXController | `Scripts/HandTracking/HandVFXController.cs` | Velocity-driven VFX |
| EnhancedAudioProcessor | `Scripts/Audio/EnhancedAudioProcessor.cs` | Frequency bands |
| MeshVFX | `Echovision/Scripts/MeshVFX.cs` | AR mesh → VFX buffers |
| VFXAutoOptimizer | `Scripts/Performance/VFXAutoOptimizer.cs` | FPS-based quality |
| VFXSelectorUI | `Scripts/UI/VFXSelectorUI.cs` | UI Toolkit selector |

### VFX Properties

```
# Hand Tracking
HandPosition     Vector3    Wrist world position
HandVelocity     Vector3    Velocity vector
HandSpeed        float      Velocity magnitude
TrailLength      float      Velocity * multiplier
BrushWidth       float      Pinch-controlled width
IsPinching       bool       Pinch gesture active

# Audio
AudioVolume      float      0-1 overall volume
AudioBass        float      0-1 low frequency (20-250Hz)
AudioMid         float      0-1 mid frequency (250-2000Hz)
AudioTreble      float      0-1 high frequency (2000-20000Hz)

# AR Mesh
MeshPointCache   Buffer     Vertex positions
MeshNormalCache  Buffer     Vertex normals
MeshPointCount   int        Active vertex count

# Sound Waves
WaveOrigin       Vector3    Emission point
WaveDirection    Vector3    Wave direction
WaveRange        float      Current radius
WaveAge          float      Normalized age 0-1
```

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Hand tracking latency < 50ms
- **SC-002**: Audio analysis runs at 60 Hz minimum
- **SC-003**: VFXAutoOptimizer maintains FPS > 45 on iPhone 12+
- **SC-004**: LOD reduces particles by 60% at 5m distance
- **SC-005**: UI renders with Inconsolata font matching original style

## Implementation Status

| System | Status | Notes |
|--------|--------|-------|
| HandVFXController | Complete | Velocity + pinch |
| EnhancedAudioProcessor | Complete | FFT frequency bands |
| MeshVFX | Complete | GraphicsBuffer pipeline |
| VFXAutoOptimizer | Complete | 4-state FPS tracking |
| VFXLODController | Complete | Distance-based quality |
| VFXProfiler | Complete | Analysis + recommendations |
| VFXSelectorUI | Complete | UI Toolkit + auto-cycle |
| HumanParticleVFX | Complete | Depth→world via compute |
| Editor Menus | Complete | H3M > HoloKit/EchoVision/Post-Processing |

## Documentation

- `Assets/Documentation/README.md` - Complete system documentation
- `Assets/Documentation/QUICK_REFERENCE.md` - VFX properties cheat sheet
- `MetavidoVFX-main/CLAUDE.md` - Project-level AI instructions
