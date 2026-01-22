# Feature Specification: VFX Multi-Mode & Audio/Physics System

**Feature Branch**: `007-vfx-multi-mode`
**Created**: 2026-01-20
**Status**: Ready
**Input**: Unified system for VFX mode switching, audio reactivity, and physics-driven effects

## Triple Verification (2026-01-20)

| Source | Status | Notes |
|--------|--------|-------|
| KB `_ADVANCED_AR_FEATURES_IMPLEMENTATION_PLAN.md` | Verified | Audio/physics patterns documented |
| KB `_JT_PRIORITIES.md` | Verified | Audio reactive brushes P1 priority |
| KB `_HAND_VFX_PATTERNS.md` | Verified | 52 VFX patterns with physics/audio |
| MetavidoVFX Implementation | Audited | Existing gaps identified |

## Overview

This spec addresses three interconnected features:

1. **VFX Multi-Mode System** - Allow any VFX to operate in different modes (People, Environment, Face, Hands, Audio, Physics)
2. **Audio Reactivity** - Drive VFX parameters from mic input, FFT analysis, beat detection
3. **Physics Integration** - Drive VFX from user movement, camera velocity, mesh collisions, gravity

### Problem Statement

Current system has these gaps:
- VFXCategory exists but VFXARBinder ignores it completely
- Categories are UI-only, don't affect bindings
- No runtime mode switching
- Beat detection NOT implemented
- No demand-driven resource allocation

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Switch VFX Mode at Runtime (Priority: P1)

As a user, I want to switch a VFX between People, Environment, Face, Hands modes.

**Why this priority**: Core flexibility requirement - same VFX working with different data sources.

**Independent Test**:
1. Select any VFX (e.g., "particles")
2. Set mode to "People" - VFX responds to human depth/stencil
3. Switch mode to "Hands" - VFX responds to hand tracking data
4. Switch mode to "Environment" - VFX responds to AR mesh/depth

**Acceptance Scenarios**:
1. **Given** VFX with DepthMap property, **When** mode is People, **Then** uses human depth segmentation
2. **Given** same VFX, **When** mode switched to Environment, **Then** uses environment depth (no stencil)
3. **Given** VFX without HandTracking properties, **When** Hands mode selected, **Then** graceful fallback with warning

---

### User Story 2 - Audio-Reactive VFX (Priority: P1)

As a user, I want VFX to react to audio input with bass, mids, treble, and beat detection.

**Why this priority**: P1 per JT_PRIORITIES - audio reactive brushes/effects.

**Independent Test**:
1. Enable audio mode on any VFX
2. Play music or make sounds
3. Verify VFX responds to:
   - Bass (particle size/emission)
   - Mids (color shift)
   - Treble (velocity/turbulence)
   - Beat (spawn bursts)

**Acceptance Scenarios**:
1. **Given** audio input active, **When** bass peaks, **Then** VFX particle size increases
2. **Given** beat detection enabled, **When** beat detected, **Then** VFX spawns burst
3. **Given** mobile device, **When** FFT runs, **Then** CPU < 1ms

---

### User Story 3 - Physics-Driven VFX (Priority: P2)

As a user, I want VFX particles to respond to camera movement, gravity, and AR mesh collision.

**Why this priority**: Enhances immersion and interactivity.

**Independent Test**:
1. Enable physics mode on VFX
2. Move device - particles respond to camera velocity
3. Particles fall with gravity
4. Particles bounce off AR mesh surfaces

**Acceptance Scenarios**:
1. **Given** camera moving, **When** velocity > threshold, **Then** particles trail/stream
2. **Given** AR mesh available, **When** particles reach mesh, **Then** collision response
3. **Given** gravity enabled, **When** particles spawn, **Then** fall toward gravity direction

---

### Edge Cases

- No microphone permission → AudioBridge disabled, silent fallback
- AR mesh not available → Physics collision disabled, gravity-only
- VFX missing required property → Skip binding, log warning
- Mode switch while VFX playing → Seamless transition (no restart)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: VFXARBinder MUST respect VFXCategory.Bindings when binding textures
- **FR-002**: Runtime mode switching MUST NOT require VFX restart
- **FR-003**: Audio system MUST provide bass, mids, treble, volume bands
- **FR-004**: Audio system MUST implement beat detection (onset detection on bass)
- **FR-005**: Physics system MUST bind camera velocity, gravity direction
- **FR-006**: AR mesh collision MUST use MeshVFX GraphicsBuffer pattern
- **FR-007**: System MUST support demand-driven resource allocation
- **FR-008**: All audio processing MUST stay under 1ms CPU on mobile

### Existing Components (Audit)

| Component | Status | Gap |
|-----------|--------|-----|
| AudioBridge.cs (54 LOC) | Working | Basic FFT only, no beat detection |
| EnhancedAudioProcessor.cs (312 LOC) | Legacy | Superseded by AudioBridge |
| VFXAudioDataBinder.cs (82 LOC) | Working | Per-VFX audio props |
| VFXPhysicsBinder.cs (349 LOC) | Working | Velocity + gravity, no mesh collision |
| VFXCategory.cs (150 LOC) | Partial | Exists but VFXARBinder ignores it |
| MeshVFX.cs | Working | AR mesh → GraphicsBuffers |

### New/Modified Components

| Component | Action | LOC Est |
|-----------|--------|---------|
| VFXARBinder.cs | Modify | +100 - integrate VFXCategory |
| VFXModeController.cs | New | ~200 - runtime mode switching |
| BeatDetector.cs | New | ~150 - onset detection algorithm |
| AudioBridge.cs | Modify | +50 - add beat detection output |
| VFXPhysicsBinder.cs | Modify | +100 - add mesh collision |
| ARDepthSource.cs | Modify | +50 - demand-driven ColorMap |

### Audio Properties (VFX Graph)

```
# Frequency Bands (via AudioBridge)
_AudioBands      Vector4    [Bass, Mids, Treble, SubBass]
_AudioVolume     float      Overall volume 0-1
_BeatPulse       float      Beat detection pulse 0→1→0
_BeatIntensity   float      Running beat intensity

# Per-VFX Override (via VFXAudioDataBinder)
AudioBass        float      Bass band (0-1)
AudioMids        float      Mids band (0-1)
AudioTreble      float      Treble band (0-1)
AudioVolume      float      Volume (0-1)
```

### Physics Properties (VFX Graph)

```
# Velocity (via VFXPhysicsBinder)
CameraVelocity   Vector3    Camera world velocity
CameraAngular    Vector3    Camera angular velocity
UserVelocity     Vector3    Composite user movement

# Gravity & Forces
GravityDirection Vector3    Gravity vector (default: 0,-9.8,0)
WindDirection    Vector3    Optional wind force

# Collision (via MeshVFX integration)
MeshPositions    GraphicsBuffer   AR mesh vertex positions
MeshNormals      GraphicsBuffer   AR mesh normals for bounce
```

### VFX Category Binding Matrix

| Mode | DepthMap | StencilMap | ColorMap | HandTracking | FaceTracking | Audio | Physics |
|------|----------|------------|----------|--------------|--------------|-------|---------|
| People | Human | Yes | Yes | - | - | Opt | Opt |
| Environment | Full | - | Yes | - | - | Opt | Opt |
| Face | Human | Yes | Yes | - | Yes | Opt | - |
| Hands | Human | Yes | Yes | Yes | - | Opt | Opt |
| Audio | - | - | Opt | - | - | Yes | Opt |
| Physics | Opt | - | Opt | Opt | - | - | Yes |
| Hybrid | Full | Yes | Yes | Opt | Opt | Opt | Opt |

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Mode switch completes in <1 frame (no VFX restart)
- **SC-002**: Beat detection latency <50ms
- **SC-003**: Audio FFT CPU <1ms on iPhone 12+
- **SC-004**: 10 audio-reactive VFX maintain 60+ FPS
- **SC-005**: Physics collision check <0.5ms per VFX
- **SC-006**: Demand-driven ColorMap saves ~8MB when unused
- **SC-007**: All 73 VFX support at least 2 modes

## Architecture

### Mode Switching Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    VFXModeController                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │
│  │ UI Dropdown │→ │ SetMode()   │→ │ VFXARBinder.UpdateMode()│ │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         v                    v                    v
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ VFXCategory     │  │ ARDepthSource   │  │ AudioBridge     │
│ .GetBindings()  │  │ .EnableColorMap │  │ .EnableBeat     │
│ returns flags   │  │ (demand-driven) │  │ (demand-driven) │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

### Beat Detection Algorithm

```
1. FFT on bass band (20-200Hz)
2. Compute spectral flux (energy delta)
3. Apply adaptive threshold (running average * 1.5)
4. Onset detected when flux > threshold
5. Output _BeatPulse: rises on onset, decays over 100ms
```

### Physics Collision Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    ARMeshManager                                │
│  MeshFilter[] → MeshVFX.UpdateBuffers() → GraphicsBuffers       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              v
┌─────────────────────────────────────────────────────────────────┐
│                    VFXPhysicsBinder                             │
│  SetGraphicsBuffer(MeshPositions), SetGraphicsBuffer(MeshNormals)│
└─────────────────────────────────────────────────────────────────┘
                              │
                              v
┌─────────────────────────────────────────────────────────────────┐
│                    VFX Graph (Collision Node)                   │
│  Sample MeshPositions, compute distance, apply bounce force     │
└─────────────────────────────────────────────────────────────────┘
```

## Design Decisions (Resolved)

### Audio Features
- **Frequency Bands**: 4 bands (Bass, Mids, Treble, SubBass) - best mobile performance
- **Audio Source**: Mic + AudioClip - supports both real-time mic and music file analysis
- **Property Mapping**: Bass → size/emission, Mids → color, Treble → velocity, Beat → bursts

### Physics Features
- **Velocity Sources**: Camera (primary), Hands (when available), Objects (future)
- **Collision Response**: Bounce only - particles reflect off AR mesh surfaces
- **Gravity**: Default world-down (0, -9.8, 0), custom direction via inspector

### Multi-Mode System
- **Runtime Mode Change**: Yes, via UI dropdown per VFX
- **Missing Properties**: Graceful fallback - log warning, bind what's available, VFX continues
- **Mode Combining**: Yes, modes are combinable (e.g., People + Audio)

## Implementation Notes

**Key Design Decisions**:
1. **Category-Aware Binding** - VFXARBinder will check VFXCategory.Bindings before binding
2. **Demand-Driven Resources** - ARDepthSource.ColorMap created only when needed
3. **Beat Detection** - Simple onset detection on bass band (mobile-friendly)
4. **Graceful Degradation** - Missing properties logged but don't break VFX

**Performance Budget**:
- Audio FFT: 1024 samples @ ~0.5ms
- Beat detection: ~0.1ms additional
- Physics velocity: ~0.1ms per VFX
- Mesh collision: ~0.3ms per VFX (depends on mesh complexity)

---

*Created: 2026-01-20*
*Author: Claude Code + User*
