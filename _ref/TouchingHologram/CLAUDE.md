# TouchingHologram (Mofo) - HoloKit Hand Interaction Tutorial

**Source**: [holoi/touching-hologram](https://github.com/holoi/touching-hologram) (HoloKit Tutorial 1)
**Tutorial**: [HoloKit Docs - Hand Tracking for Holograms](https://docs.holokit.io/for-creators/tutorials-and-case-studies/tutorial-1-use-hand-tracking-for-interacting-with-holograms)
**Unity Version**: 2022.3.8f1
**License**: MIT

---

## Overview

AR experience enabling users to create particle-style Buddha holograms and interact with them using hand gestures. Features HoloKit stereo rendering for MR hardware.

**Key Features**:
- Tap-to-place AR object placement
- Hand tracking interaction with VFX
- 24 Buddha VFX variations (Hologram, Plexus, Particles, Cubes, etc.)
- Stereo rendering for HoloKit headset
- Singing bowl audio feedback

---

## Project Structure

```
TouchingHologram/
├── Assets/
│   ├── Art Resources/
│   │   ├── BuddhaVFX/           # 24 Buddha particle effects
│   │   ├── SeatVFX/             # Location indicator effects
│   │   ├── Thumbnails/          # VFX thumbnails
│   │   ├── Animations/          # Buddha animations
│   │   ├── Audios/              # Singing bowl sounds
│   │   ├── Meshes/              # Buddha mesh
│   │   └── PointClouds/         # PCX point cloud data
│   │
│   ├── HoloKit/
│   │   ├── GUI/                 # AR placement, hand tracking UI
│   │   │   ├── Runtime/Scripts/ # Core interaction scripts
│   │   │   └── Assets/          # GUI VFX assets
│   │   └── VFXAssets/           # HoloKit VFX library
│   │
│   ├── Samples/HoloKit Unity SDK/
│   │   ├── Hand Tracking/
│   │   ├── Hand Gesture Recognition/
│   │   ├── Stereoscopic Rendering/
│   │   └── Phone Model Specs Calibration/
│   │
│   ├── Scripts/
│   │   ├── BuddhaController.cs  # Main VFX controller
│   │   ├── BuddhaGUI.cs         # UI management
│   │   └── BuddhaSceneManager.cs
│   │
│   ├── Scenes/
│   │   ├── Buddha_PlacingWithTouch.unity  # Main scene (tap placement)
│   │   └── Buddha_PlacingWithHand.unity   # Hand placement variant
│   │
│   └── Prefabs/                 # Buddha prefabs
│
├── Documentation~/Images/       # Tutorial screenshots
├── Packages/                    # Package dependencies
└── ProjectSettings/
```

---

## Buddha VFX Library (24 Effects)

| Effect | Description |
|--------|-------------|
| `Hologram.vfx` | Classic hologram scanlines |
| `Plexus.vfx` | Connected point network |
| `Particles.vfx` | Floating particles |
| `Cubes.vfx` | Voxel-style cubes |
| `Petals.vfx` | Floating flower petals |
| `Triangles.vfx` | Triangle mesh effect |
| `Filament.vfx` | Thread-like strands |
| `Stream.vfx` | Flowing streams |
| `Scanner.vfx` | Scanning effect |
| `Bubbles.vfx` | Bubble particles |
| `Rain.vfx` | Rain particles |
| `Wiper.vfx` | Wiping reveal |
| `Voxel.vfx` | Voxelized rendering |
| `Morph.vfx` | Morphing effect |
| `Points.vfx` | Point cloud rendering |
| `Simple.vfx` | Basic particles |
| `Squares.vfx` | Square particles |
| `Capture.vfx` | Capture effect |
| `AkParticles.vfx` | Keijiro-style particles |
| `AkPoint.vfx` | Keijiro point cloud |
| `DParticles.vfx` | Dense particles |
| `Particles 1.vfx` | Particle variant |

---

## Key Scripts

### BuddhaController.cs
Main VFX controller - manages Buddha effects and hand interaction.

### ARPlacementWithTouch.cs
Tap-to-place AR placement using ARKit raycast.

### HoverObject.cs / HoverableObject.cs
Hand hover detection for VFX interaction.

### CustomVFXPositionBinder.cs
Binds transform positions to VFX Graph properties.

---

## Dependencies

| Package | Version | Source |
|---------|---------|--------|
| HoloKit Unity SDK | latest | GitHub |
| URP | 14.0.8 | Unity |
| VFX Graph | 14.0.8 | Unity |
| ARKit | 5.0.7 | Unity |
| jp.keijiro.pcx | 1.0.1 | Keijiro Registry |
| jp.keijiro.vfxgraphassets | 1.2.0 | Keijiro Registry |

---

## Quick Start

1. Open `Assets/Scenes/Buddha_PlacingWithTouch.unity`
2. Build to iOS (requires LiDAR iPhone)
3. Scan floor surfaces
4. Tap to place Buddha
5. Move hand close to interact
6. Press "Stereo" for HoloKit rendering

---

## HoloKit SDK Features Used

- **Hand Tracking**: 21-joint tracking via Vision framework
- **Gesture Recognition**: Built-in gesture detection
- **Stereo Rendering**: Side-by-side for HoloKit optics
- **Phone Calibration**: Per-device offset tuning

---

## Patterns for MetavidoVFX

### Hand-VFX Interaction Pattern
```csharp
// From HoverObject.cs - proximity-based VFX triggering
void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Hand")) {
        vfxGraph.SetBool("IsHovered", true);
        vfxGraph.SendEvent("OnInteract");
    }
}
```

### AR Placement Pattern
```csharp
// From ARPlacementWithTouch.cs
if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) {
    if (arRaycastManager.Raycast(Input.GetTouch(0).position, hits, TrackableType.PlaneWithinPolygon)) {
        Instantiate(prefab, hits[0].pose.position, hits[0].pose.rotation);
    }
}
```

---

## Related KB Files

- `KnowledgeBase/_HAND_SENSING_CAPABILITIES.md` - Hand tracking patterns
- `KnowledgeBase/_VFX25_HOLOGRAM_PORTAL_PATTERNS.md` - VFX patterns
- `KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` - AR + VFX integration
