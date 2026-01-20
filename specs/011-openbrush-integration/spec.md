# Feature Specification: Open Brush Integration

**Feature Branch**: `011-openbrush-integration`
**Created**: 2026-01-20
**Status**: Draft
**Input**: Migrate Open Brush URP brushes, audio reactive system, save/load, mirror modes, and API painting to MetavidoVFX

## Triple Verification

| Source | Status | Notes |
|--------|--------|-------|
| `_ref/open-brush-feature-pure-openxr/` | Verified | Full Open Brush codebase with pure OpenXR branch |
| Open Brush GitHub | Verified | [github.com/icosa-gallery/open-brush](https://github.com/icosa-gallery/open-brush) |
| Reaktion Audio System | Verified | keijiro's audio reactive library integrated |
| KB `_HAND_VFX_PATTERNS.md` | Verified | VFX trigger patterns compatible with brush system |

## Overview

This spec defines the integration of key Open Brush features into MetavidoVFX while maintaining our simpler AR-focused architecture. The goal is to extract maximum value from Open Brush's mature feature set (90+ brushes, audio reactive painting, sophisticated symmetry modes, API-driven drawing) while avoiding the complexity of their full VR-focused scene graph.

### Goals

1. **URP Brush Migration** - Port all 90+ brushes including 5 audio reactive variants
2. **Simplified Save/Load** - Implement lightweight scene serialization without full .tilt format complexity
3. **Mirror Painting Modes** - Integrate point symmetry (14 families) and wallpaper groups (17 patterns)
4. **API-Driven Painting** - Enable programmatic brush control via HTTP/WebSocket API
5. **Cross-Platform Input** - Extract OpenXR input patterns while keeping AR-first simplicity

### Scope Boundaries

**In Scope:**
- Brush materials, shaders, and geometry generation
- Audio reactive brush parameters
- Symmetry/mirror transform calculation
- Core API endpoints for brush control
- JSON scene serialization (strokes, layers, camera)
- AR touch → brush position mapping

**Out of Scope (Open Brush features we won't migrate):**
- Full .tilt file format compatibility
- VR controller-specific features (grip, haptics)
- Poly/Icosa asset catalog integration (covered by Spec 009)
- Multiplayer synchronization (covered by Spec 010)
- Environment/skybox system
- Reference images/videos widgets
- Stencil/guide tools

## Architecture Comparison

### Open Brush (Complex)

```
TiltBrush Namespace (500+ scripts)
├── App (singleton, state machine)
├── PointerManager (multi-pointer, symmetry)
├── BrushCatalog (GUID → BrushDescriptor)
├── SketchMemoryScript (undo/redo, history)
├── SaveLoadScript (.tilt ZIP format)
├── InputManager (VR controllers, stylus)
├── WidgetManager (models, images, lights)
├── EnvironmentCatalog (skyboxes, scenes)
└── ApiManager (HTTP/WebSocket scripting)
```

### MetavidoVFX Target (Simplified)

```
Metavido.Brush Namespace (~15 scripts)
├── BrushManager (singleton, brush registry)
├── BrushStroke (stroke data + mesh generation)
├── BrushInput (AR touch/raycast → brush position)
├── BrushMirror (symmetry transforms)
├── BrushAudioReactive (Reaktion integration)
├── BrushSerializer (JSON save/load)
└── BrushApi (HTTP endpoints subset)
```

## User Scenarios & Testing

### User Story 1 - Brush Selection (Priority: P1)

As an AR user, I want to select from a variety of brush styles for different artistic effects.

**Why this priority**: Core painting functionality.

**Independent Test**:
1. Open brush selection UI
2. Verify 20+ brush options visible (subset of full 90)
3. Select "Ink" brush
4. Draw stroke on AR plane
5. Verify stroke has correct visual appearance

**Acceptance Scenarios**:
1. **Given** brush UI open, **When** browsing, **Then** categorized brush list shown
2. **Given** brush selected, **When** drawing, **Then** correct shader/geometry applied
3. **Given** stroke complete, **When** render, **Then** matches Open Brush reference

---

### User Story 2 - Audio Reactive Painting (Priority: P1)

As a user, I want my brush strokes to react to music/audio input in real-time.

**Why this priority**: Key differentiating feature.

**Independent Test**:
1. Select "Waveform" or "WaveformFFT" brush
2. Enable microphone input
3. Play music near device
4. Draw strokes
5. Verify brush effect pulses/animates to audio

**Acceptance Scenarios**:
1. **Given** Waveform brush active, **When** audio playing, **Then** stroke width modulates
2. **Given** WaveformFFT brush, **When** audio playing, **Then** frequency bands drive colors
3. **Given** silence, **When** drawing, **Then** default static appearance

**Audio Reactive Brushes to Port**:
- `Waveform.mat` - RMS amplitude modulation
- `WaveformFFT.mat` - Frequency band colors
- `WaveformParticles.mat` - Particle emission rate
- `WaveformPulse.mat` (NeonPulse) - Pulsing intensity
- `WaveformTube.mat` - Tube radius animation

---

### User Story 3 - Mirror Painting (Priority: P1)

As an artist, I want to draw with symmetry modes to create complex patterns efficiently.

**Why this priority**: Professional art tool feature.

**Independent Test**:
1. Enable mirror mode
2. Select "Point Symmetry - C4v" (4-fold with vertical mirrors)
3. Draw single stroke
4. Verify 8 strokes appear (4 rotations × 2 mirror)
5. Move brush position
6. Verify all mirrors follow

**Acceptance Scenarios**:
1. **Given** point symmetry C4, **When** draw stroke, **Then** 4 rotated copies appear
2. **Given** wallpaper p6m, **When** draw, **Then** hexagonal tiled pattern
3. **Given** color shift enabled, **When** mirrors generated, **Then** HSB shifts per copy

**Point Symmetry Families** (from Open Brush):
- Cyclic: Cn, Cnv, Cnh, Sn (n = 1-12)
- Dihedral: Dn, Dnh, Dnd
- Polyhedral: T, Th, Td, O, Oh, I, Ih

**Wallpaper Groups** (17 patterns):
- p1, pg, cm, pm
- p6, p6m, p3, p3m1, p31m
- p4, p4m, p4g
- p2, pgg, pmg, pmm, cmm

---

### User Story 4 - Save/Load Scenes (Priority: P2)

As a user, I want to save my AR drawings and load them later.

**Why this priority**: Essential persistence feature.

**Independent Test**:
1. Draw several strokes with different brushes
2. Tap "Save" button
3. Verify save confirmation
4. Close app
5. Reopen app
6. Load saved scene
7. Verify all strokes restored with correct appearance

**Acceptance Scenarios**:
1. **Given** strokes in scene, **When** save, **Then** JSON file created in app storage
2. **Given** saved scene, **When** load, **Then** all stroke data restored
3. **Given** different brushes used, **When** load, **Then** each brush type preserved

**Simplified Save Format** (JSON, not .tilt):
```json
{
  "version": "1.0",
  "created": "2026-01-20T10:30:00Z",
  "brushIndex": ["Ink", "Waveform", "Light"],
  "strokes": [
    {
      "brushIdx": 0,
      "color": [1.0, 0.5, 0.2, 1.0],
      "size": 0.02,
      "points": [
        {"pos": [0,1,2], "rot": [0,0,0,1], "pressure": 1.0},
        ...
      ]
    }
  ],
  "layers": [
    {"name": "Layer 1", "visible": true, "strokeIndices": [0, 1]}
  ],
  "camera": {
    "position": [0, 1.5, -2],
    "rotation": [0, 0, 0, 1]
  }
}
```

---

### User Story 5 - API-Driven Painting (Priority: P2)

As a developer, I want to control brush painting programmatically via HTTP API.

**Why this priority**: Enables LLM integration, automation, generative art.

**Independent Test**:
1. Enable API server
2. Send HTTP request: `GET /api/v1?brush.type=Light&brush.draw=1,0,0,0,1,0`
3. Verify stroke appears at specified position
4. Send WebSocket command for continuous drawing
5. Verify real-time stroke generation

**Acceptance Scenarios**:
1. **Given** API enabled, **When** `brush.type=X`, **Then** brush changes
2. **Given** `brush.move.to=x,y,z`, **When** sent, **Then** brush position updates
3. **Given** `draw.stroke=[[x,y,z,rx,ry,rz,p],...]`, **When** sent, **Then** full stroke created

**Core API Endpoints** (subset of Open Brush's 100+):
| Endpoint | Description |
|----------|-------------|
| `brush.type` | Set active brush by name |
| `brush.size.set` | Set brush size |
| `color.set.rgb` | Set brush color |
| `brush.move.to` | Move brush to position |
| `brush.turn.y` | Rotate brush yaw |
| `brush.draw` | Draw line from current position |
| `draw.stroke` | Draw complete stroke from points |
| `symmetry.type` | Set mirror mode |
| `symmetry.pointfamily` | Set point symmetry family |
| `save.scene` | Save current scene |
| `load.scene` | Load saved scene |

---

### User Story 6 - AR Touch Drawing (Priority: P1)

As an AR user, I want to draw by touching the screen with my finger.

**Why this priority**: Primary AR input method.

**Independent Test**:
1. Point device at horizontal surface
2. Touch screen and drag
3. Verify stroke follows finger position on AR plane
4. Lift finger
5. Verify stroke completes

**Acceptance Scenarios**:
1. **Given** AR plane detected, **When** touch, **Then** brush position at raycast hit
2. **Given** touch moving, **When** dragging, **Then** stroke points added
3. **Given** touch ended, **When** lift, **Then** stroke finalized and rendered

---

### Edge Cases

- No AR plane detected → Draw at fixed distance (2m) from camera
- Audio source unavailable → Audio reactive brushes use default values
- Symmetry order > 12 → Cap at 12 to prevent performance issues
- API request while drawing → Queue command, apply after stroke ends
- Save with 1000+ strokes → Warn user, offer selective save

## Requirements

### Functional Requirements

- **FR-001**: System MUST support minimum 20 brush types (subset of Open Brush 90+)
- **FR-002**: System MUST support 5 audio reactive brush variants
- **FR-003**: System MUST support point symmetry with orders 1-12
- **FR-004**: System MUST support all 17 wallpaper groups
- **FR-005**: System MUST save/load scenes to JSON format
- **FR-006**: System SHOULD expose HTTP API for brush control
- **FR-007**: System MUST map AR touch input to brush position

### Non-Functional Requirements

- **NFR-001**: Brush stroke rendering MUST maintain 30+ FPS with 100 strokes
- **NFR-002**: Audio reactive update rate MUST be 60Hz minimum
- **NFR-003**: Save operation MUST complete in <2 seconds for 500 strokes
- **NFR-004**: Mirror calculation MUST complete in <16ms per frame
- **NFR-005**: API response latency MUST be <50ms

### Key Components to Port

| Open Brush Component | MetavidoVFX Target | Effort |
|---------------------|-------------------|--------|
| `BrushDescriptor` | `BrushData` (ScriptableObject) | Medium |
| `PointerScript.cs` | `BrushStroke.cs` | High |
| `PointerManager.cs` (symmetry) | `BrushMirror.cs` | Medium |
| `AudioInjector.cs` (Reaktion) | `BrushAudioReactive.cs` | Low |
| `SaveLoadScript.cs` | `BrushSerializer.cs` | Medium |
| `ApiManager.cs` | `BrushApi.cs` | Medium |
| `UnityXRControllerInfo.cs` | `BrushInput.cs` (AR-adapted) | Medium |
| Brush materials (90+) | Port 20+ priority brushes | High |

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `com.unity.xr.arfoundation` | 6.2.1 | AR plane raycasting |
| `com.unity.visualeffectgraph` | 17.2.0 | VFX-based brushes |
| `Newtonsoft.Json` | - | Scene serialization |

### Scripting Defines

```
OPENBRUSH_BRUSHES     - Open Brush materials imported
BRUSH_API_ENABLED     - HTTP API server active
BRUSH_AUDIO_ENABLED   - Reaktion audio reactive features
```

## Implementation Architecture

### Brush System

```
BrushManager (Singleton)
├── Dictionary<string, BrushData> _brushes
├── BrushData CurrentBrush
├── List<BrushStroke> ActiveStrokes
├── BrushMirror SymmetryHandler
├── BrushAudioReactive AudioHandler
│
├── SetBrush(string name)
├── BeginStroke(Vector3 pos, Quaternion rot)
├── UpdateStroke(Vector3 pos, Quaternion rot, float pressure)
├── EndStroke()
└── ClearAllStrokes()

BrushData (ScriptableObject)
├── string Name
├── Guid Id (for .tilt compatibility)
├── Material Material
├── BrushGeometryType GeometryType (Flat, Tube, Particles)
├── bool IsAudioReactive
├── AudioReactiveParams AudioParams
└── float DefaultSize

BrushStroke
├── BrushData Brush
├── Color Color
├── float Size
├── List<ControlPoint> Points
├── Mesh GeneratedMesh
├── int LayerIndex
│
├── AddPoint(ControlPoint cp)
├── GenerateMesh()
└── Render()

ControlPoint
├── Vector3 Position
├── Quaternion Rotation
├── float Pressure
└── float Timestamp
```

### Mirror System

```
BrushMirror
├── SymmetryMode Mode (None, Point, Wallpaper)
├── PointSymmetryFamily PointFamily
├── int PointOrder
├── WallpaperGroup WallpaperGroup
├── Vector2Int WallpaperRepeats
├── List<Matrix4x4> Transforms
│
├── CalculateTransforms()
├── ApplyToStroke(BrushStroke stroke) → List<BrushStroke>
└── PreviewMirrors(Vector3 brushPos) → List<Vector3>

enum PointSymmetryFamily { Cn, Cnv, Cnh, Sn, Dn, Dnh, Dnd, T, Th, Td, O, Oh, I, Ih }
enum WallpaperGroup { p1, pg, cm, pm, p6, p6m, p3, p3m1, p31m, p4, p4m, p4g, p2, pgg, pmg, pmm, cmm }
```

### Audio Reactive System

```
BrushAudioReactive : MonoBehaviour
├── bool Enabled
├── float[] FrequencyBands (8 bands)
├── float RmsLevel (dB)
├── float PeakLevel
│
├── OnAudioFilterRead(float[] data, int channels)
├── UpdateFrequencyBands()
└── GetBrushModulation(BrushData brush) → AudioModulation

AudioModulation
├── float SizeMultiplier
├── float ColorHueShift
├── float EmissionIntensity
└── float ParticleRate
```

### Data Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        MetavidoVFX AR Drawing                               │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ AR Input                                                                ││
│  │   Touch → ARRaycast → Hit Point → BrushInput.BrushPosition             ││
│  │   Touch Phase → BrushInput.IsDrawing                                   ││
│  └────────────────────────────────┬────────────────────────────────────────┘│
│                                   ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ Brush Processing                                                        ││
│  │   BrushManager.UpdateStroke(pos, rot, pressure)                        ││
│  │       ├── BrushAudioReactive.GetModulation() → size/color adjust       ││
│  │       ├── BrushMirror.ApplyToStroke() → multiple strokes               ││
│  │       └── BrushStroke.AddPoint() → mesh generation                     ││
│  └────────────────────────────────┬────────────────────────────────────────┘│
│                                   ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ Rendering                                                               ││
│  │   BrushStroke.GenerateMesh() → Dynamic mesh                            ││
│  │   Material from BrushData → GPU render                                 ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                        API Integration (Optional)                           │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ HTTP Server (localhost:40020)                                           ││
│  │   GET /api/v1?brush.type=Light                                         ││
│  │   GET /api/v1?draw.stroke=[[0,1,0,...],[0,1.1,0,...]]                   ││
│  │                                   │                                     ││
│  │                                   ▼                                     ││
│  │   BrushApi.HandleCommand() → BrushManager.SetBrush/BeginStroke/...     ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
```

## Success Criteria

### Measurable Outcomes

- **SC-001**: 20+ brushes functional with correct visual appearance
- **SC-002**: All 5 audio reactive brushes respond to audio input
- **SC-003**: Point symmetry orders 1-12 generate correct mirror count
- **SC-004**: All 17 wallpaper groups produce correct tiling patterns
- **SC-005**: Save/load round-trip preserves all stroke data
- **SC-006**: API can create strokes programmatically
- **SC-007**: AR touch input draws strokes on detected planes

### Performance Targets

| Metric | Target | Test Condition |
|--------|--------|----------------|
| FPS with 100 strokes | 30+ | iPhone 12 |
| FPS with 500 strokes | 30+ | iPhone 15 Pro |
| Audio reactive latency | <20ms | Waveform brush |
| Save 500 strokes | <2 seconds | JSON serialization |
| Mirror calculation | <16ms | 12-fold symmetry |

## Brush Priority List

### Tier 1 - Essential (Port First)

| Brush | Category | Audio Reactive | Notes |
|-------|----------|----------------|-------|
| Ink | Basic | No | Primary drawing brush |
| Light | Basic | No | Glowing effect |
| Flat | Basic | No | Simple flat ribbon |
| Marker | Basic | No | Solid opaque |
| Highlighter | Basic | No | Semi-transparent |
| Wire | Basic | No | Thin line |
| Waveform | Basic | Yes | RMS modulated |
| WaveformFFT | Basic | Yes | Frequency colors |
| Smoke | Basic | No | Particle effect |
| Fire | Basic | No | Animated |

### Tier 2 - Enhanced (Port Second)

| Brush | Category | Audio Reactive | Notes |
|-------|----------|----------------|-------|
| Rainbow | Basic | No | Color gradient |
| Electricity | Basic | No | Lightning effect |
| Bubbles | Basic | No | Particle spheres |
| Stars | Basic | No | Star particles |
| Streamers | Basic | No | Ribbon particles |
| WaveformParticles | Basic | Yes | Audio particles |
| WaveformPulse | Basic | Yes | Pulsing neon |
| WaveformTube | Basic | Yes | Audio tube radius |
| Disco | Basic | No | Reflective |
| Plasma | Basic | No | Animated |

### Tier 3 - Complete (Port Last)

Remaining 70+ brushes from Open Brush catalog.

## Security Considerations

- API server binds to localhost only by default
- Optional authentication token for remote API access
- Save files stored in app-specific directory
- No cloud sync (local only for MVP)

## References

### Open Brush
- [Open Brush GitHub](https://github.com/icosa-gallery/open-brush)
- [Scripting API Docs](https://docs.openbrush.app/user-guide/open-brush-api)
- [Brush Reference](https://docs.openbrush.app/user-guide/brushes-guide)

### Reaktion (keijiro)
- [Reaktion GitHub](https://github.com/keijiro/Reaktion)

### Related Specs
- Spec 009: Icosa/Sketchfab 3D Model Integration
- Spec 010: Normcore AR Multiuser Drawing

---

*Created: 2026-01-20*
*Author: Claude Code + User*
