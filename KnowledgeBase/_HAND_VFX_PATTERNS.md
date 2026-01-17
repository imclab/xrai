# Hand Tracking VFX Patterns

**Source**: TouchingHologram, HoloKitApp
**Updated**: 2026-01-17

---

## VFX Categories

### Buddha VFX Library (22 effects)
Statue-driven particle effects that respond to hand position.

| VFX | Description | Input |
|-----|-------------|-------|
| AkParticles | Akvfx-style point cloud | PositionMap |
| AkPoint | Single point emitter | HandPosition |
| Bubbles | Floating bubbles | HandPosition, Velocity |
| Capture | Capture/grab effect | PinchPosition |
| Cubes | Cubic particles | PositionMap |
| DParticles | Directional particles | HandVelocity |
| Filament | Wire-like strands | PositionMap |
| Hologram | Classic hologram scan | PositionMap |
| Morph | Shape morphing | Time, Scale |
| Particles | Standard particles | HandPosition |
| Particles 1 | Variant particles | HandPosition |
| Petals | Flower petals | HandPosition, Wind |
| Plexus | Connected nodes | PositionMap |
| Points | Point sprites | PositionMap |
| Rain | Rain drops | WorldUp, Gravity |
| Scanner | Scanning lines | Time |
| Simple | Minimal particles | HandPosition |
| Squares | Square sprites | PositionMap |
| Stream | Flowing stream | HandVelocity |
| Triangles | Triangle mesh | PositionMap |
| Voxel | Voxelized output | PositionMap |
| Wiper | Wiping effect | HandPosition |

### HoloKit VFX Library (30 effects)
AR placement, combat, and UI effects.

| Category | VFX | Purpose |
|----------|-----|---------|
| **Placement** | V_ARPlacementIndicator | AR anchor preview |
| | V_ARPlacementIndicator_Birth | Spawn animation |
| | V_Placement Hook | Hook-style indicator |
| **Combat** | V_Bolt | Lightning bolt |
| | V_Beam | Energy beam |
| | V_Particle_Explosion | Explosion burst |
| | V_Flare_Explosion | Flare burst |
| | V_ScreenEffect_OnHit | Hit feedback |
| **UI** | V_LifeCircle | Health indicator |
| | V_ChargingBar | Charge progress |
| | V_DeathCountdown | Death timer |
| **Effects** | V_Fog | Atmospheric fog |
| | V_Trail | Motion trail |
| | V_PixelatedBirth | Spawn effect |

---

## Hand Tracking Input Properties

```hlsl
// Common exposed properties for hand-driven VFX
HandPosition      Vector3   // Wrist world position
HandVelocity      Vector3   // Hand movement vector
HandSpeed         float     // Velocity magnitude
PinchPosition     Vector3   // Pinch point (index + thumb midpoint)
IsPinching        bool      // Pinch gesture active
GripStrength      float     // 0-1 grip amount
```

---

## Implementation Patterns

### 1. Velocity-Driven Emission
```csharp
// HandVFXController pattern
float speed = handVelocity.magnitude;
vfx.SetFloat("SpawnRate", Mathf.Lerp(0, 1000, speed / maxSpeed));
vfx.SetVector3("EmitVelocity", handVelocity.normalized * emitSpeed);
```

### 2. Pinch-Triggered Burst
```csharp
// Burst on pinch start
if (isPinching && !wasPinching)
{
    vfx.SendEvent("OnPinch");
    vfx.SetVector3("BurstPosition", pinchPosition);
}
```

### 3. Position Map Sampling
```hlsl
// VFX Graph: Sample hand position texture
float3 worldPos = SampleTexture(PositionMap, particleId).xyz;
// Apply offset from hand center
worldPos += HandPosition;
```

---

## Scene Setup

### TouchingHologram Hierarchy
```
XR Origin
├── Main Camera (AR Camera)
├── HandTrackingManager (HoloKit)
│   └── LeftHand / RightHand
│       └── HandVFXController
└── Buddha
    └── BuddhaVFXManager
        └── [Active VFX]
```

### Required Components
1. `ARSession` - AR Foundation session
2. `HandTrackingManager` - HoloKit hand provider
3. `HandVFXController` - Velocity/pinch to VFX binding
4. `VisualEffect` - VFX Graph instance

---

## Performance Notes

| VFX Count | Target FPS | Notes |
|-----------|------------|-------|
| 1 | 60 | Ideal for AR |
| 3-5 | 45-60 | Acceptable |
| 10+ | <30 | Use LOD |

**Optimization**:
- Use `VFXAutoOptimizer` for FPS-adaptive particle count
- Limit active VFX to 1-3 at a time
- Use culling by distance from hand

---

## Related Files

- `TouchingHologram/Assets/Art Resources/BuddhaVFX/` - Buddha VFX library
- `TouchingHologram/Assets/HoloKit/VFXAssets/` - HoloKit VFX library
- `MetavidoVFX-main/Assets/Scripts/HandTracking/HandVFXController.cs` - Controller
- `KnowledgeBase/_HAND_SENSING_CAPABILITIES.md` - Hand tracking reference
