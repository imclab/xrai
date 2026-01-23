# Feature Specification: XRRAI Scene Format & Cross-Platform Export

**Feature Branch**: `016-xrrai-scene-format`
**Created**: 2026-01-22
**Status**: In Progress (Phase 1 Complete - 60%)

## Implementation Status (Updated 2026-01-22)

| Component | Status | File |
|-----------|--------|------|
| XRRAIScene (Data Model) | ✅ Complete | `Assets/Scripts/Scene/XRRAIScene.cs` |
| XRRAISceneManager | ✅ Complete | `Assets/Scripts/Scene/XRRAISceneManager.cs` |
| GLTFExporter | ✅ Complete | `Assets/Scripts/Scene/GLTFExporter.cs` |
| IcosaGalleryManager | ✅ Complete | `Assets/Scripts/Icosa/IcosaGalleryManager.cs` |
| Editor Setup Menu | ✅ Complete | `Assets/Scripts/Editor/XRRAISceneSetup.cs` |
| Save/Load UI | ⬜ Pending | Phase 2 |
| USDZ Export | ⬜ Pending | Phase 2 |
| .tilt Export | ⬜ Pending | Phase 3 |
| Web Viewer | ⬜ Pending | Phase 3 |
| Web AR (model-viewer) | ⬜ Pending | Phase 4 |
**Input**: Define optimal scene format for MetavidoVFX with cross-platform export, Icosa Gallery upload, WebGPU viewing, and Web AR preview

## Triple Verification

| Source | Status | Notes |
|--------|--------|-------|
| KB `_XR_SCENE_FORMAT_COMPARISON.md` | Verified | Deep research on .tilt, USDZ, glTF, AI formats |
| Khronos glTF Extensions | Verified | KHR_gaussian_splatting, KHR_interactivity |
| Icosa Gallery API | Verified | REST upload, JWT auth, glTF primary |
| model-viewer | Verified | Cross-platform Web AR pattern |
| OpenBrush .tilt | Verified | Stroke format, metadata schema |

## Overview

This spec defines the **XRRAI Scene Format** - a native scene representation for MetavidoVFX that preserves full fidelity while enabling seamless export to glTF (web/universal), USDZ (iOS), and .tilt (OpenBrush). Includes Icosa Gallery integration, WebGPU viewer, and Web AR preview.

### Goals

1. **XRRAI Native Format** - JSON schema with binary attachments for strokes, holograms, VFX, AR anchors
2. **glTF Export** - Universal distribution format with KHR extensions
3. **USDZ Export** - iOS/visionOS Quick Look compatibility
4. **Icosa Upload** - REST API integration for scene publishing
5. **WebGPU Viewer** - Three.js-based viewer with brush shader support
6. **Web AR Preview** - model-viewer pattern for iOS (Quick Look) + Android (WebXR)

### Scope Boundaries

**In Scope (MVP)**:
- XRRAI JSON schema v1.0
- Scene save/load in Unity
- glTF/GLB export with brush meshes
- Icosa Gallery upload flow
- Basic web viewer (Three.js + three-icosa)
- model-viewer AR preview

**Phase 2**:
- USDZ export with MaterialX
- .tilt export for OpenBrush compatibility
- Gaussian splat embedding (SPZ)
- Streaming/chunked loading
- Collaborative editing

**Out of Scope**:
- Full .tilt bidirectional sync
- USD runtime editing
- Custom WebXR experiences (beyond model-viewer)

---

## Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        MetavidoVFX Unity App                            │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ XRRAISceneManager                                                 │  │
│  │   ├── SaveScene(filepath) → .xrrai file                           │  │
│  │   ├── LoadScene(filepath) → Scene reconstruction                  │  │
│  │   ├── ExportGLTF(filepath) → .glb universal                       │  │
│  │   ├── ExportUSDZ(filepath) → .usdz iOS                            │  │
│  │   └── ExportTilt(filepath) → .tilt OpenBrush                      │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                              ↓                                           │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ IcosaGalleryManager                                               │  │
│  │   ├── Authenticate() → JWT device code flow                       │  │
│  │   ├── UploadScene(glbPath, metadata) → assetId, publishUrl        │  │
│  │   ├── GetUserAssets() → List of user's uploads                    │  │
│  │   └── DeleteAsset(assetId)                                        │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                              ↓ GLB upload
┌─────────────────────────────────────────────────────────────────────────┐
│                        Icosa Gallery Cloud                              │
│  ├── https://api.icosa.gallery/v1/users/me/assets (POST)               │
│  ├── Asset storage + thumbnail generation                               │
│  └── Public view: https://icosa.gallery/view/{assetId}                 │
└─────────────────────────────────────────────────────────────────────────┘
                              ↓ View URL
┌─────────────────────────────────────────────────────────────────────────┐
│                        Web Viewer (Vis/xrrai-viewer)                    │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ Three.js + three-icosa + WebGPURenderer                           │  │
│  │   ├── Load from Icosa URL or direct GLB                           │  │
│  │   ├── Render brush strokes with original shaders                  │  │
│  │   ├── OrbitControls, animation playback                           │  │
│  │   └── "View in AR" button (platform-adaptive)                     │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ model-viewer Web Component                                        │  │
│  │   ├── iOS: ar-modes="quick-look" → USDZ auto-convert             │  │
│  │   ├── Android: ar-modes="webxr scene-viewer" → Native AR         │  │
│  │   └── Desktop: ar-modes="webxr" → If available                   │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

### Data Flow

```
User creates AR scene in MetavidoVFX
  ↓
BrushManager, HologramManager, VFXLibraryManager
  ↓
XRRAISceneManager.SaveScene("myscene.xrrai")
  ↓
XRRAI JSON + binary attachments (local storage)
  ↓
XRRAISceneManager.ExportGLTF("myscene.glb")
  ↓
GLB with brush meshes, materials, metadata
  ↓
IcosaGalleryManager.UploadScene("myscene.glb", metadata)
  ↓
Icosa returns:
  - assetId: "abc123"
  - publishUrl: "https://icosa.gallery/upload/abc123"
  - viewUrl: "https://icosa.gallery/view/abc123"
  ↓
User opens publishUrl → adds title, description, license
  ↓
Scene publicly viewable at viewUrl
  ↓
Web viewer loads GLB via three-icosa
  ↓
"View in AR" → model-viewer handles platform detection
```

---

## XRRAI Format Specification v1.0

### File Structure

```
myscene.xrrai (JSON, human-readable)
  OR
myscene.xrraib (binary container, production)

Contents:
├── manifest                 - Version, generator, created
├── scene                    - Name, description, bounds
├── nodes[]                  - Scene hierarchy
├── strokes[]                - Brush stroke data
├── holograms[]              - Hologram configurations
├── vfxInstances[]           - VFX Graph instances
├── anchors[]                - AR anchors
├── assets                   - References to external files
└── extensions               - Optional extended data
```

### Schema Definition

```json
{
  "$schema": "https://xrrai.dev/schema/scene/1.0",
  "xrrai": "1.0",
  "generator": "MetavidoVFX/2026.1",
  "created": "2026-01-22T12:00:00Z",
  "modified": "2026-01-22T14:30:00Z",

  "scene": {
    "name": "My AR Drawing",
    "description": "Created in MetavidoVFX",
    "tags": ["ar", "brush", "hologram"],
    "bounds": {
      "min": [-5.0, -2.0, -5.0],
      "max": [5.0, 3.0, 5.0]
    },
    "upAxis": "Y",
    "units": "meters"
  },

  "nodes": [
    {
      "id": "node_001",
      "name": "Root",
      "children": ["node_002", "node_003"],
      "transform": {
        "position": [0, 0, 0],
        "rotation": [0, 0, 0, 1],
        "scale": [1, 1, 1]
      }
    }
  ],

  "brushes": [
    {
      "id": "brush_glow",
      "name": "Glow",
      "guid": "89d104cd-d012-426a-b35a-f929e7dc2a0e",
      "material": "BrushStrokeGlow",
      "geometry": "flat",
      "audioReactive": {
        "enabled": true,
        "sizeMultiplier": 0.5,
        "colorHueShift": 0.1,
        "emissionMultiplier": 1.5,
        "frequencyBand": 1
      }
    }
  ],

  "strokes": [
    {
      "id": "stroke_001",
      "brushId": "brush_glow",
      "nodeId": "node_002",
      "color": [1.0, 0.5, 0.2, 1.0],
      "size": 0.02,
      "layerId": "layer_001",
      "mirrorGroupId": null,
      "points": [
        {"p": [0, 1, 0], "r": [0, 0, 0, 1], "s": 1.0, "t": 0},
        {"p": [0.1, 1.1, 0], "r": [0, 0.1, 0, 0.99], "s": 0.9, "t": 33}
      ]
    }
  ],

  "holograms": [
    {
      "id": "holo_001",
      "type": "live",
      "nodeId": "node_003",
      "source": {
        "type": "ARDepthSource",
        "colorFormat": "R8G8B8A8",
        "depthFormat": "R16"
      },
      "quality": "high",
      "anchorId": "anchor_001"
    }
  ],

  "vfxInstances": [
    {
      "id": "vfx_001",
      "assetPath": "Resources/VFX/People/HologramParticles.vfx",
      "nodeId": "node_003",
      "parameters": {
        "ParticleCount": 10000,
        "ColorTint": [0.5, 0.8, 1.0, 1.0]
      },
      "bindings": {
        "AudioVolume": "AudioBridge.Volume",
        "DepthTexture": "holo_001.DepthTexture"
      }
    }
  ],

  "anchors": [
    {
      "id": "anchor_001",
      "type": "plane",
      "classification": "floor",
      "position": [0, 0, 0],
      "rotation": [0, 0, 0, 1],
      "size": [2.0, 2.0],
      "confidence": 0.95,
      "persistent": true,
      "nativeId": "aranchor_abc123"
    }
  ],

  "layers": [
    {
      "id": "layer_001",
      "name": "Layer 1",
      "visible": true,
      "locked": false
    }
  ],

  "assets": {
    "models": [
      {
        "id": "model_001",
        "path": "assets/cat.glb",
        "source": "icosa",
        "sourceId": "3UL8Bz_Id6I",
        "license": "CC-BY"
      }
    ],
    "textures": [],
    "audio": []
  },

  "extensions": {
    "XRRAI_audio_reactive": {
      "version": "1.0",
      "globalBands": [0.5, 0.3, 0.2, 0.1],
      "beatIntensity": 0.8
    }
  }
}
```

### Binary Attachment Format (.xrraib)

For production scenes with many strokes:

```
XRRAIB Header (32 bytes):
  magic: "XRRAIB" (6 bytes)
  version: uint16 (1)
  jsonOffset: uint64
  jsonLength: uint64
  binaryOffset: uint64
  binaryLength: uint64

Binary Chunks:
  [Stroke Points Chunk]
    chunkType: "STRK" (4 bytes)
    strokeId: string
    pointCount: uint32
    points[]:
      position: float32x3 (12 bytes)
      rotation: float32x4 (16 bytes)
      pressure: float32 (4 bytes)
      timestamp: uint32 (4 bytes)
      Total: 36 bytes per point

  [Hologram Recording Chunk]
    chunkType: "HOLO" (4 bytes)
    holoId: string
    frameCount: uint32
    frames[]: compressed H.264 data

JSON Payload:
  Same schema as .xrrai but with "pointsRef" instead of inline "points"
  Example: "pointsRef": "STRK:stroke_001:0-1024"
```

---

## User Stories & Testing

### User Story 1 - Save Scene (P1)

As an AR artist, I want to save my creation so I can continue later.

**Test**:
1. Create strokes, add hologram, place 3D model
2. Tap "Save" button
3. Enter scene name
4. Verify .xrrai file created in app storage
5. Close app, reopen
6. Load scene
7. Verify all content restored

**Acceptance**:
- All strokes preserved (position, color, size, brush type)
- Hologram configuration restored
- 3D models re-imported
- AR anchors re-established (if persistent)

---

### User Story 2 - Export to glTF (P1)

As a user, I want to export my scene for sharing on any platform.

**Test**:
1. Create scene with strokes and models
2. Tap "Export" → "glTF/GLB"
3. Choose quality (Low/Medium/High)
4. Verify .glb file created
5. Open in Blender/Three.js
6. Verify geometry and materials correct

**Acceptance**:
- Strokes converted to triangle meshes
- Brush materials approximated in PBR
- Models embedded or referenced
- Metadata preserved in glTF extras

---

### User Story 3 - Upload to Icosa Gallery (P1)

As an artist, I want to publish my work to Icosa Gallery.

**Test**:
1. Create scene
2. Tap "Share" → "Icosa Gallery"
3. If not authenticated: complete device code flow
4. Confirm upload
5. Receive publish URL
6. Open URL, add metadata
7. Verify scene visible at view URL

**Acceptance**:
- JWT token stored securely
- GLB uploaded successfully
- Thumbnail generated
- Scene publicly viewable

---

### User Story 4 - View in Web Browser (P1)

As a viewer, I want to see shared scenes in my browser.

**Test**:
1. Open Icosa view URL or direct viewer URL
2. Wait for loading
3. Use mouse/touch to orbit
4. Verify brush strokes render correctly
5. Test on Chrome, Safari, Firefox

**Acceptance**:
- WebGPU renderer (fallback to WebGL)
- Brush shaders via three-icosa
- Responsive controls
- Mobile-optimized

---

### User Story 5 - View in AR (P2)

As a viewer, I want to see the scene in AR on my phone.

**Test iOS**:
1. Open viewer on iOS Safari
2. Tap "View in AR"
3. Quick Look opens with USDZ
4. Place model on surface
5. Walk around, view from angles

**Test Android**:
1. Open viewer on Android Chrome
2. Tap "View in AR"
3. Scene Viewer or WebXR activates
4. Place model on surface

**Acceptance**:
- Platform detection correct
- iOS: USDZ Quick Look works
- Android: Scene Viewer/WebXR works
- Model placement accurate

---

## Requirements

### Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-001 | Save scene to XRRAI format | P1 |
| FR-002 | Load scene from XRRAI format | P1 |
| FR-003 | Export scene to glTF/GLB | P1 |
| FR-004 | Export scene to USDZ | P2 |
| FR-005 | Export scene to .tilt | P3 |
| FR-006 | Authenticate with Icosa Gallery | P1 |
| FR-007 | Upload scene to Icosa Gallery | P1 |
| FR-008 | WebGPU/WebGL viewer renders scene | P1 |
| FR-009 | Web AR via model-viewer | P2 |
| FR-010 | Streaming load for large scenes | P3 |

### Non-Functional Requirements

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-001 | Save 500 strokes | <2 seconds |
| NFR-002 | Load 500 strokes | <3 seconds |
| NFR-003 | GLB export 500 strokes | <5 seconds |
| NFR-004 | Web viewer initial load | <5 seconds |
| NFR-005 | Web viewer FPS | 30+ on mobile |
| NFR-006 | Upload 10MB GLB | <30 seconds |

---

## Implementation

### Unity Components

| File | Purpose |
|------|---------|
| `Assets/Scripts/Scene/XRRAISceneManager.cs` | Main save/load/export manager |
| `Assets/Scripts/Scene/XRRAIScene.cs` | Scene data model |
| `Assets/Scripts/Scene/XRRAISerializer.cs` | JSON/binary serialization |
| `Assets/Scripts/Scene/GLTFExporter.cs` | glTF export via glTFast |
| `Assets/Scripts/Scene/USDZExporter.cs` | USDZ export (Phase 2) |
| `Assets/Scripts/Scene/TiltExporter.cs` | .tilt export (Phase 3) |
| `Assets/Scripts/Icosa/IcosaGalleryManager.cs` | Gallery API integration |
| `Assets/Scripts/Icosa/IcosaAuthManager.cs` | JWT authentication |
| `Assets/UI/Views/SaveLoadUI.uxml` | Save/load UI |
| `Assets/UI/Views/ExportUI.uxml` | Export options UI |
| `Assets/UI/Views/ShareUI.uxml` | Share/upload UI |

### Web Viewer (Vis/xrrai-viewer)

| File | Purpose |
|------|---------|
| `package.json` | Dependencies (three, three-icosa, model-viewer) |
| `src/index.html` | Entry point with model-viewer |
| `src/main.js` | Three.js viewer setup |
| `src/renderer.js` | WebGPU/WebGL renderer selection |
| `src/loader.js` | GLB loading + Icosa API |
| `src/ar.js` | AR mode handling |
| `src/styles.css` | UI styles |

### Dependencies

**Unity**:
```json
{
  "com.unity.cloud.gltfast": "6.0.0",
  "com.unity.nuget.newtonsoft-json": "3.2.1"
}
```

**Web**:
```json
{
  "three": "^0.160.0",
  "three-icosa": "^1.0.0",
  "@google/model-viewer": "^3.5.0"
}
```

---

## Success Criteria

| ID | Criteria | Measurement |
|----|----------|-------------|
| SC-001 | Scene save/load round-trip | 100% fidelity |
| SC-002 | GLB opens in Blender | No errors |
| SC-003 | Icosa upload completes | Asset visible |
| SC-004 | Web viewer renders | Visual match |
| SC-005 | iOS Quick Look works | Model displays |
| SC-006 | Android AR works | Model places |

---

## Security Considerations

- JWT tokens stored in Unity PlayerPrefs (encrypted on iOS)
- HTTPS-only API calls
- No sensitive data in exported files
- User consent before upload

---

## References

### Specifications
- [XRRAI Format Comparison](../../KnowledgeBase/_XR_SCENE_FORMAT_COMPARISON.md)
- [glTF 2.0 Specification](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html)
- [Icosa Gallery API](https://api.icosa.gallery/v1/docs)

### Libraries
- [glTFast](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@5.2/manual/index.html)
- [three-icosa](https://github.com/icosa-foundation/three-icosa)
- [model-viewer](https://modelviewer.dev/)

### Related Specs
- Spec 009: Icosa/Sketchfab 3D Model Integration
- Spec 011: Open Brush Integration
- Spec 016: Integrated Brush Gallery System

---

*Created: 2026-01-22*
*Author: Claude Code + User*
