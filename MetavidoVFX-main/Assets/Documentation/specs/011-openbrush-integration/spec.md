# Feature Specification: Open Brush Integration

**Feature Branch**: `011-openbrush-integration`
**Created**: 2026-01-20
**Status**: In Progress (90% Complete)
**Input**: Migrate Open Brush URP brushes, audio reactive system, save/load, mirror modes, and API painting to MetavidoVFX

## Test Results (2026-01-22)

✅ **ALL 107 BRUSHES PASSED** comprehensive testing:
- Material assignment: ✓
- Shader loading: ✓
- Mesh generation: ✓
- Geometry types: 6/7 implemented
- **Mouse input drawing: ✓** (fixed 2026-01-22)

**Shader Distribution:**
| Shader | Count |
|--------|-------|
| XRRAI/BrushStroke | 50 |
| XRRAI/BrushStrokeTube | 28 |
| XRRAI/BrushStrokeParticle | 12 |
| XRRAI/BrushStrokeGlow | 11 |
| XRRAI/BrushStrokeHull | 6 |

**Test Tools:**
- `H3M > Testing > Test ALL Brushes (Play Mode)` - Tests all 107 brushes
- `H3M > Testing > Test Brush Types (Play Mode)` - Tests each geometry type
- `H3M > Testing > Diagnose Brush System (Play Mode)` - Deep diagnostic report

## Implementation Status (Updated 2026-01-22)

| Component | Status | File |
|-----------|--------|------|
| BrushData (ScriptableObject) | ✅ Complete | `Assets/Scripts/Brush/BrushData.cs` |
| BrushManager (Singleton) | ✅ Complete | `Assets/Scripts/Brush/BrushManager.cs` |
| BrushStroke (Mesh Generation) | ✅ Complete | `Assets/Scripts/Brush/BrushStroke.cs` |
| BrushGeometryPool (Object Pooling) | ✅ Complete | `Assets/Scripts/Brush/BrushGeometryPool.cs` |
| BrushMathUtils (Parallel Transport) | ✅ Complete | `Assets/Scripts/Brush/BrushMathUtils.cs` |
| BrushCatalogFactory (Default Brushes) | ✅ Complete | `Assets/Scripts/Brush/BrushCatalogFactory.cs` |
| BrushInput (Generic Input) | ✅ Complete | `Assets/Scripts/Brush/BrushInput.cs` |
| BrushAudioReactive | ✅ Replaced | → Uses `UnifiedAudioReactive` singleton |
| UnifiedAudioReactive | ✅ Complete | `Assets/Scripts/Audio/UnifiedAudioReactive.cs` |
| VFXAudioDataBinder8Band | ✅ Complete | `Assets/Scripts/VFX/Binders/VFXAudioDataBinder8Band.cs` |
| BrushMirror (Symmetry) | ✅ Complete | `Assets/Scripts/Brush/BrushMirror.cs` |
| ARBrushInput (AR Touch) | ✅ Complete | `Assets/Scripts/Brush/ARBrushInput.cs` |
| ARPlaneDrawing (Plane Mgmt) | ✅ Complete | `Assets/Scripts/Brush/ARPlaneDrawing.cs` |
| BrushStroke Shader | ✅ Complete | `Assets/Shaders/BrushStroke.shader` |
| BrushStrokeGlow Shader | ✅ Complete | `Assets/Shaders/BrushStrokeGlow.shader` |
| BrushStrokeTube Shader | ✅ Complete | `Assets/Shaders/BrushStrokeTube.shader` |
| BrushStrokeHull Shader | ✅ Complete | `Assets/Shaders/BrushStrokeHull.shader` |
| BrushStrokeParticle Shader | ✅ Complete | `Assets/Shaders/BrushStrokeParticle.shader` |
| Brush Materials (5) | ✅ Complete | `Assets/Materials/Brushes/` |
| BrushSerializer (JSON) | ✅ Complete | `Assets/Scripts/Brush/BrushSerializer.cs` |
| BrushTestRunner (Editor) | ✅ Complete | `Assets/Scripts/Editor/BrushTestRunner.cs` |
| BrushDiagnostics (Editor) | ✅ Complete | `Assets/Scripts/Editor/BrushDiagnostics.cs` |
| StrokeBatchManager | ⬜ Future | Batch rendering optimization |
| BrushAPI (HTTP) | ⬜ Pending (P3) | API endpoints |
| Additional Brushes (20+) | ⬜ Pending | Port from OpenBrush |

### Geometry Types Implemented (7 total)

| Type | Status | Open Brush Equivalent | Notes |
|------|--------|----------------------|-------|
| Flat | ✅ | QuadStripBrush | Ribbon facing camera |
| Tube | ✅ | TubeBrush | Parallel transport framing |
| Hull | ✅ | GeometryBrush (hull variant) | Hexagonal cross-section + caps |
| Particle | ✅ | SprayBrush (particle mode) | Billboard quads at points |
| Spray | ✅ | SprayBrush | Scattered quads along path |
| Slice | ✅ | SliceBrush | Quad normal = motion direction |
| Custom | ⬜ | Custom mesh | Per-point mesh instances |

## Triple Verification

| Source | Status | Notes |
|--------|--------|-------|
| `_ref/open-brush-feature-pure-openxr/` | Verified | Full Open Brush codebase with pure OpenXR branch |
| Open Brush GitHub | Verified | [github.com/icosa-gallery/open-brush](https://github.com/icosa-gallery/open-brush) |
| Reaktion Audio System | Verified | keijiro's audio reactive library integrated |
| KB `_HAND_VFX_PATTERNS.md` | Verified | VFX trigger patterns compatible with brush system |

## Overview

This spec defines the integration of key Open Brush features into MetavidoVFX while maintaining our simpler AR-focused architecture. The goal is to extract core value from Open Brush's mature feature set (brushes, audio reactive painting, symmetry modes) while avoiding the complexity of their full VR-focused scene graph.

### Goals

1. **URP Brush Migration** - Port 10 essential brushes including 3 audio reactive variants (MVP)
2. **Simplified Save/Load** - Implement lightweight scene serialization without full .tilt format complexity
3. **Mirror Painting Modes** - Integrate point symmetry (orders 1-8) and 4 wallpaper groups
4. **Cross-Platform Input** - Extract OpenXR input patterns while keeping AR-first simplicity

### Future Goals (P3)

5. **API-Driven Painting** - Enable programmatic brush control via HTTP/WebSocket API (deferred)
6. **Full Brush Catalog** - Port remaining 80+ brushes (deferred)

### Scope Boundaries

**In Scope (MVP):**
- 10 brush materials, shaders, and geometry generation
- 3 audio reactive brush parameters (Waveform, WaveformFFT, WaveformTube)
- Point symmetry (orders 1-8) and 4 wallpaper groups (p1, pm, p4m, p6m)
- JSON scene serialization (strokes, layers, camera)
- AR touch → brush position mapping

**Out of Scope (Deferred to P3):**
- API endpoints for brush control (HTTP/WebSocket)
- Full 90+ brush catalog (start with 10)
- Complex symmetry (orders 9-12, polyhedral families)
- 13 additional wallpaper groups

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
XRRAI.BrushPainting Namespace (~15 scripts)
├── BrushManager (singleton, brush registry)
├── BrushStroke (stroke data + mesh generation)
├── BrushGeometryPool (object pooling for mesh buffers)
├── BrushMathUtils (parallel transport, smoothing)
├── BrushCatalogFactory (default brush creation)
├── BrushInput (AR touch/raycast → brush position)
├── BrushMirror (symmetry transforms)
├── BrushAudioReactive (Reaktion integration)
├── BrushSerializer (JSON save/load)
└── BrushApi (HTTP endpoints subset)
```

## Open Brush Technical Deep Dive

### Mesh Generation Architecture

Open Brush uses several key patterns for brush mesh generation:

#### 1. QuadStripBrush (Legacy)
The original brush type, generating flat ribbon geometry:
- 6 unique verts per triangle pair (3 top, 3 bottom, backface culled)
- Uses `MasterBrush` for temporary geometry storage before finalization
- Incremental algorithm with per-control-point processing
- **Our equivalent**: `BrushGeometryType.Flat`

#### 2. GeometryBrush (Modern)
Topology reboot enabling diverse mesh structures:
- Uses "knots" (control points with bounded local support)
- `GeometryPool` for intermediate mesh storage in C#
- More efficient than QuadStripBrush for complex shapes
- **Our equivalent**: `BrushGeometryPool` + all geometry types

#### 3. TubeBrush
First volumetric brush with consistent orientation:
- Uses **parallel transport** framing instead of standard surface frame
- Prevents "twisting" artifacts common in Frenet-Serret frames
- Configurable number of sides (3-32)
- **Our equivalent**: `BrushGeometryType.Tube` with `BrushMathUtils.ComputeMinimalRotationFrame()`

#### 4. SprayBrush
Particle/splatter effects:
- Used by: Coarse Bristles, Dot Marker, Leaves, Splatter, Leaves2
- Generates isolated quads scattered along stroke path
- **Our equivalent**: `BrushGeometryType.Spray` and `BrushGeometryType.Particle`

#### 5. SliceBrush
Motion-aligned quads:
- Each quad's normal is the **direction of motion** (tangent)
- Creates trail of motion-aligned planes
- **Our equivalent**: `BrushGeometryType.Slice`

### Batch Rendering System (Future Optimization)

Open Brush's `BatchManager` groups same-material strokes:
```
BatchPool (per-material)
├── Batch[] (large meshes)
│   └── BatchSubset[] (individual strokes)
```

**Why batching matters**:
- "1 gameobject per stroke is not scalable"
- Groups tiny meshes into larger batches by material
- Reduces draw calls from 1000s to 10s

**Our current approach**: 1 GameObject per stroke (acceptable for <100 strokes)
**Future optimization**: Implement `StrokeBatchManager` for 500+ stroke scenes

### MasterBrush Pattern

Open Brush uses `MasterBrush` for:
- Active stroke preview during drawing
- Decay effects (fading stroke ends)
- Temporary geometry before finalization into batch

**Our equivalent**: Direct mesh updates via `BrushStroke.RegenerateMesh()`

### Control Point Data Structure

Open Brush "Knot" structure (we call it `ControlPoint`):
```csharp
// Open Brush style
struct Knot {
    Vector3 position;
    Quaternion rotation;
    float pressure;
    // + derived: tangent, normal, binormal
}

// Our implementation
struct ControlPoint {
    Vector3 Position;
    Quaternion Rotation;
    float Pressure;
    float Timestamp;
}
```

### Parallel Transport Algorithm

Open Brush's TubeBrush uses parallel transport to avoid Frenet-Serret frame issues:

```csharp
// Our implementation in BrushMathUtils.cs
public static Vector3 ComputeMinimalRotationFrame(Vector3 newTangent, Vector3 prevUp)
{
    // Rotate previous up vector to align with new tangent
    // Minimizes twist compared to naive cross product
    Vector3 rotationAxis = Vector3.Cross(prevTangent, newTangent);
    float angle = Vector3.SignedAngle(prevTangent, newTangent, rotationAxis);
    return Quaternion.AngleAxis(angle, rotationAxis) * prevUp;
}
```

**Why this matters**: Standard Frenet-Serret frames cause visible "twisting" when stroke curves back on itself. Parallel transport maintains consistent orientation.

## User Scenarios & Testing

### User Story 1 - Brush Selection (Priority: P1)

As an AR user, I want to select from a variety of brush styles for different artistic effects.

**Why this priority**: Core painting functionality.

**Independent Test**:
1. Open brush selection UI
2. Verify 10 brush options visible (MVP set)
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

**Audio Reactive Brushes to Port (MVP - 3 brushes)**:
- `Waveform.mat` - RMS amplitude modulation
- `WaveformFFT.mat` - Frequency band colors
- `WaveformTube.mat` - Tube radius animation

**Deferred to P3**:
- `WaveformParticles.mat` - Particle emission rate
- `WaveformPulse.mat` (NeonPulse) - Pulsing intensity

---

### User Story 3 - Mirror Painting (Priority: P1)

As an artist, I want to draw with symmetry modes to create complex patterns efficiently.

**Why this priority**: Professional art tool feature.

**Independent Test**:
1. Enable mirror mode
2. Select "Point Symmetry - C4" (4-fold rotational)
3. Draw single stroke
4. Verify 4 strokes appear (4 rotations)
5. Move brush position
6. Verify all mirrors follow

**Acceptance Scenarios**:
1. **Given** point symmetry C4, **When** draw stroke, **Then** 4 rotated copies appear
2. **Given** wallpaper p4m, **When** draw, **Then** square tiled pattern
3. **Given** color shift enabled, **When** mirrors generated, **Then** HSB shifts per copy

**Point Symmetry (MVP - orders 1-8)**:
- Cyclic: Cn (n = 1-8)
- Example: C2 = 2-fold, C4 = 4-fold, C6 = hexagonal

**Deferred to P3**:
- Higher orders (9-12)
- Cnv, Cnh, Sn, Dn, Dnh, Dnd (mirror variants)
- Polyhedral: T, Th, Td, O, Oh, I, Ih

**Wallpaper Groups (MVP - 4 patterns)**:
- p1 (translation only)
- pm (horizontal reflection)
- p4m (square tiling with mirrors)
- p6m (hexagonal tiling with mirrors)

**Deferred to P3** (13 additional patterns):
- pg, cm, p3, p3m1, p31m, p4, p4g, p2, pgg, pmg, pmm, cmm, p6

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

### User Story 5 - API-Driven Painting (Priority: P3 - Deferred)

As a developer, I want to control brush painting programmatically via HTTP API.

**Why deferred**: Complex feature; focus MVP on core drawing. Enables LLM integration, automation, generative art in future phase.

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

### Functional Requirements (MVP)

- **FR-001**: System MUST support 10 brush types (MVP set)
- **FR-002**: System MUST support 3 audio reactive brush variants
- **FR-003**: System MUST support point symmetry with orders 1-8
- **FR-004**: System MUST support 4 wallpaper groups (p1, pm, p4m, p6m)
- **FR-005**: System MUST save/load scenes to JSON format
- **FR-006**: System MUST map AR touch input to brush position

### Functional Requirements (P3 - Deferred)

- **FR-007**: System SHOULD expose HTTP API for brush control
- **FR-008**: System SHOULD support full 90+ brush catalog
- **FR-009**: System SHOULD support orders 9-12 and polyhedral symmetry
- **FR-010**: System SHOULD support all 17 wallpaper groups

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

### Audio Reactive System (Unified 8-Band)

**Update 2026-01-22**: Audio reactive features now use `UnifiedAudioReactive` singleton instead of per-component `BrushAudioReactive`. This eliminates duplicate FFT computation (~0.2ms savings) and provides consistent 8-band data to all consumers.

```
UnifiedAudioReactive : MonoBehaviour (Singleton)
├── 8 Frequency Bands (logarithmic, keijiro Reaktion-style)
│   ├── Band0: Sub-bass (20-60 Hz)
│   ├── Band1: Bass (60-250 Hz)
│   ├── Band2: Low-mids (250-500 Hz)
│   ├── Band3: Mids (500-2000 Hz)
│   ├── Band4: High-mids (2000-4000 Hz)
│   ├── Band5: Presence (4000-6000 Hz)
│   ├── Band6: Brilliance (6000-10000 Hz)
│   └── Band7: Air (10000-20000 Hz)
│
├── Beat Detection (spectral flux algorithm)
│   ├── BeatPulse (0-1, decays after beat)
│   ├── BeatIntensity (0-1, normalized)
│   └── IsOnset (true on beat frame)
│
├── Global Shader Properties
│   ├── _AudioBand0-7 (float per band)
│   ├── _AudioSubBass, _AudioBass, _AudioMid, _AudioTreble (legacy 4-band)
│   ├── _BeatPulse, _BeatIntensity
│   └── _AudioVolume, _AudioPeak
│
└── API
    ├── GetBrushModulation(AudioReactiveParams) → AudioModulation
    └── 4x2 AudioTexture (RGBAFloat) for VFX without exposed properties

AudioModulation (returned by GetBrushModulation)
├── float NormalizedLevel (0-1)
├── float SizeMultiplier
├── float ColorHueShift
├── float EmissionIntensity
└── float ParticleRate
```

**VFX Binder**: Use `VFXAudioDataBinder8Band` for VFX Graph audio reactivity:
- Auto-binds to `UnifiedAudioReactive.Instance`
- Exposes all 8 bands + legacy 4-band compatibility
- Includes beat detection and audio texture

**Brush Integration**: `BrushManager` now uses:
```csharp
var audio = UnifiedAudioReactive.Instance;
var modulation = audio.GetBrushModulation(brush.AudioParams);
```

**KB Reference**: `KnowledgeBase/_UNIFIED_AUDIO_REACTIVE_PATTERNS.md`

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
│  │       ├── UnifiedAudioReactive.GetBrushModulation() → size/color       ││
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

### Measurable Outcomes (MVP)

- **SC-001**: 10 brushes functional with correct visual appearance
- **SC-002**: 3 audio reactive brushes respond to audio input
- **SC-003**: Point symmetry orders 1-8 generate correct mirror count
- **SC-004**: 4 wallpaper groups (p1, pm, p4m, p6m) produce correct tiling patterns
- **SC-005**: Save/load round-trip preserves all stroke data
- **SC-006**: AR touch input draws strokes on detected planes

### Measurable Outcomes (P3 - Deferred)

- **SC-007**: API can create strokes programmatically
- **SC-008**: Full 90+ brush catalog functional
- **SC-009**: All 17 wallpaper groups produce correct tiling patterns

### Performance Targets

| Metric | Target | Test Condition |
|--------|--------|----------------|
| FPS with 100 strokes | 30+ | iPhone 12 |
| FPS with 500 strokes | 30+ | iPhone 15 Pro |
| Audio reactive latency | <20ms | Waveform brush |
| Save 500 strokes | <2 seconds | JSON serialization |
| Mirror calculation | <16ms | 12-fold symmetry |

## Brush Priority List

### MVP (10 Brushes - Port First)

| Brush | Category | Audio Reactive | Notes |
|-------|----------|----------------|-------|
| Ink | Basic | No | Primary drawing brush |
| Light | Basic | No | Glowing effect |
| Flat | Basic | No | Simple flat ribbon |
| Marker | Basic | No | Solid opaque |
| Wire | Basic | No | Thin line |
| Smoke | Basic | No | Particle effect |
| Fire | Basic | No | Animated |
| Waveform | Audio | Yes | RMS modulated |
| WaveformFFT | Audio | Yes | Frequency colors |
| WaveformTube | Audio | Yes | Audio tube radius |

### P3 - Enhanced (Port Later)

| Brush | Category | Audio Reactive | Notes |
|-------|----------|----------------|-------|
| Highlighter | Basic | No | Semi-transparent |
| Rainbow | Basic | No | Color gradient |
| Electricity | Basic | No | Lightning effect |
| Bubbles | Basic | No | Particle spheres |
| Stars | Basic | No | Star particles |
| WaveformParticles | Audio | Yes | Audio particles |
| WaveformPulse | Audio | Yes | Pulsing neon |
| Disco | Basic | No | Reflective |
| Plasma | Basic | No | Animated |
| Streamers | Basic | No | Ribbon particles |

### P3 - Complete (Port Last)

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
- Spec 016: XRRAI Scene Format (scene save/export)

### KB References
- `KnowledgeBase/_UNIFIED_AUDIO_REACTIVE_PATTERNS.md` - Unified 8-band audio system
- `KnowledgeBase/_XR_SCENE_FORMAT_COMPARISON.md` - Scene format analysis

---

*Created: 2026-01-20*
*Updated: 2026-01-22 (Unified Audio System)*
*Author: Claude Code + User*
