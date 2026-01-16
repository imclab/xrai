# Hand Sensing Capabilities for VFX Control

## Overview
Comprehensive list of all possible gestures, measurements, and sensing capabilities available through hand tracking, pose detection, and segmentation for controlling VFX parameters.

---

## 1. Joint Positions (21 joints per hand)

### Wrist
- `Wrist` - Base of the hand

### Thumb (4 joints)
- `ThumbCMC` - Carpometacarpal joint
- `ThumbMP` - Metacarpophalangeal joint
- `ThumbIP` - Interphalangeal joint
- `ThumbTip` - Tip of thumb

### Index Finger (4 joints)
- `IndexMCP` - Metacarpophalangeal joint
- `IndexPIP` - Proximal interphalangeal joint
- `IndexDIP` - Distal interphalangeal joint
- `IndexTip` - Tip of index finger

### Middle Finger (4 joints)
- `MiddleMCP`, `MiddlePIP`, `MiddleDIP`, `MiddleTip`

### Ring Finger (4 joints)
- `RingMCP`, `RingPIP`, `RingDIP`, `RingTip`

### Little Finger (4 joints)
- `LittleMCP`, `LittlePIP`, `LittleDIP`, `LittleTip`

---

## 2. Computed Distances (VFX Parameter Control)

### Pinch Distances
| Distance | Joints | VFX Use Case |
|----------|--------|--------------|
| **Thumb-Index** | ThumbTip ↔ IndexTip | Brush width, particle size, selection |
| **Thumb-Middle** | ThumbTip ↔ MiddleTip | Grab detection, intensity control |
| **Thumb-Ring** | ThumbTip ↔ RingTip | Secondary parameter |
| **Thumb-Little** | ThumbTip ↔ LittleTip | Tertiary parameter |

### Finger Spread
| Distance | Joints | VFX Use Case |
|----------|--------|--------------|
| **Index-Middle Spread** | IndexTip ↔ MiddleTip | Explosion radius, spray width |
| **Full Spread** | IndexTip ↔ LittleTip | VFX volume, emission area |
| **Palm Width** | IndexMCP ↔ LittleMCP | Scale factor |

### Hand Size Metrics
| Metric | Computation | VFX Use Case |
|--------|-------------|--------------|
| **Palm Size** | Area of Wrist-IndexMCP-LittleMCP-LittleMCP polygon | Global VFX scale |
| **Hand Length** | Wrist ↔ MiddleTip | Reach, trail length |
| **Fingertip Centroid** | Average of all 5 tips | VFX spawn origin |

---

## 3. Gesture Recognition

### Basic Gestures (HoloKit Native)
| Gesture | Detection | Threshold | VFX Action |
|---------|-----------|-----------|------------|
| **Pinch** | ThumbTip-IndexTip < 0.02m | Start: 0.02m, End: 0.03m | Select, spawn particle |
| **Grab/Fist** | ThumbTip-MiddleTip < 0.04m | With hysteresis 1.5x | Capture, hold VFX |
| **Five/Open Palm** | All fingers extended | Joint alignment check | Release, explode VFX |
| **Poke** | Index extended, others curled | Tip distance ratios | Point, draw |

### Extended Gestures (Detectable)
| Gesture | Detection Method | VFX Action |
|---------|------------------|------------|
| **Point** | Index extended only | Ray-based VFX, laser |
| **Peace** | Index + Middle extended | Dual emitters |
| **Rock** | Index + Little extended | Split effects |
| **Thumbs Up** | Thumb extended, fingers curled | Trigger/confirm |
| **Thumbs Down** | Thumb down, fingers curled | Cancel/undo |
| **OK Sign** | Thumb-Index ring, others extended | Fine control mode |
| **Finger Gun** | Thumb up, index forward, others curled | Projectile spawn |
| **Claw** | All fingers bent at DIP | Attraction force |
| **Wave** | Periodic wrist rotation | Modulation effect |

### Dynamic Gestures
| Gesture | Detection | VFX Action |
|---------|-----------|------------|
| **Swipe Left/Right** | Wrist velocity + direction | Switch VFX, navigate |
| **Swipe Up/Down** | Wrist vertical velocity | Intensity increase/decrease |
| **Circle** | Wrist circular motion | Create portal, loop |
| **Throw** | Fast forward motion + release | Projectile launch |
| **Catch** | Grab at object position | VFX capture |

---

## 4. Velocity & Motion Data

### Per-Joint Velocity
| Data | Computation | VFX Use Case |
|------|-------------|--------------|
| **Joint Velocity** | Position delta / deltaTime | Trail length, particle speed |
| **Joint Acceleration** | Velocity delta / deltaTime | Impact effects, burst intensity |
| **Angular Velocity** | Rotation delta / deltaTime | Spin effects, vortex |

### Derived Motion Metrics
| Metric | Computation | VFX Use Case |
|--------|-------------|--------------|
| **Hand Speed** | Wrist velocity magnitude | Overall intensity |
| **Finger Wiggle** | Sum of fingertip velocity magnitudes | Shimmer, sparkle rate |
| **Punch Detection** | High wrist acceleration forward | Explosive burst |
| **Shake Detection** | High-frequency wrist oscillation | Scatter effect |

---

## 5. Hand Stencil/Segmentation

### Silhouette-Based Parameters
| Parameter | Source | VFX Use Case |
|-----------|--------|--------------|
| **Hand Silhouette Texture** | Human segmentation | Mask for particle emission |
| **Silhouette Area** | Pixel count in stencil | VFX volume scaling |
| **Silhouette Perimeter** | Edge pixel count | Outline effects |
| **Silhouette Centroid** | Center of mass | VFX origin point |
| **Bounding Box** | Min/max extent | Containment area |

### Finger Stencil Analysis
| Parameter | Detection | VFX Use Case |
|-----------|-----------|--------------|
| **Extended Fingers Count** | Stencil topology | Emission multiplier |
| **Finger Spread Angle** | Stencil analysis | Cone angle for effects |
| **Hand Orientation** | Stencil rotation | Directional effects |

---

## 6. Audio-Reactive Parameters

### Microphone Input
| Parameter | Source | VFX Mapping |
|-----------|--------|-------------|
| **Volume/Amplitude** | RMS of audio buffer | Particle count, scale |
| **Pitch/Frequency** | FFT dominant frequency | Color hue, particle lifetime |
| **Beat Detection** | Onset detection | Pulse/burst effects |
| **Spectral Centroid** | FFT weighted average | Turbulence, chaos |

### Combined Hand + Audio
| Combination | Effect |
|-------------|--------|
| **Pinch + Volume** | Modulated brush width |
| **Hand Speed + Pitch** | Velocity-reactive trails |
| **Gesture + Beat** | Beat-synced effects |

---

## 7. Physics-Enabled VFX

### Collision Sources
| Source | Data Available | VFX Behavior |
|--------|----------------|--------------|
| **AR Mesh (LiDAR)** | Floor, walls, objects | Particles bounce off surfaces |
| **AR Planes** | Horizontal/vertical planes | Ground collision, wall bounce |
| **Hand Colliders** | Joint-based capsules | Particles interact with hands |

### Physics Parameters
| Parameter | VFX Use Case |
|-----------|--------------|
| **Bounciness** | How much particles bounce |
| **Friction** | Surface drag |
| **Gravity** | Downward force |
| **Lifetime on Collision** | Particles die or persist |

---

## 8. Implementation Patterns

### VFX Graph Properties
```csharp
// Position data
vfx.SetVector3("HandPosition", wristPosition);
vfx.SetGraphicsBuffer("HandJoints", jointBuffer);

// Distance parameters
vfx.SetFloat("PinchDistance", thumbIndexDistance);
vfx.SetFloat("HandSpread", indexLittleDistance);

// Velocity data
vfx.SetVector3("HandVelocity", wristVelocity);
vfx.SetFloat("HandSpeed", wristVelocity.magnitude);

// Gesture state
vfx.SetBool("IsPinching", isPinching);
vfx.SetInt("ExtendedFingers", fingerCount);

// Audio data
vfx.SetFloat("AudioVolume", volume);
vfx.SetFloat("AudioPitch", pitch);

// Stencil
vfx.SetTexture("HandStencil", stencilTexture);
```

### Gesture Detection Thresholds (Recommended)
```csharp
const float PINCH_START = 0.02f;      // 2cm
const float PINCH_END = 0.03f;        // 3cm (hysteresis)
const float GRAB_START = 0.04f;       // 4cm
const float GRAB_END = 0.06f;         // 6cm (hysteresis)
const float POKE_RATIO = 0.7f;        // Tip to joint ratio
const float VELOCITY_THRESHOLD = 1.0f; // m/s for fast motion
```

---

## 9. VFX Parameter Mapping Examples

### Brush Width (Pinch Control)
```
Thumb-Index Distance → Remap(0.02, 0.15) → BrushWidth (0.01, 0.5)
```

### Trail Intensity (Velocity)
```
Hand Speed → Remap(0, 3) → TrailLength (0.1, 2.0)
```

### Particle Color (Audio)
```
AudioPitch → Remap(100, 1000) → ColorHue (0, 360)
```

### Emission Rate (Gesture + Audio)
```
ExtendedFingers × AudioVolume → EmissionRate (10, 1000)
```

---

## 10. Platform Availability

| Feature | iOS (HoloKit) | Quest | WebGL |
|---------|---------------|-------|-------|
| 21 Joint Positions | ✅ | ✅ | ✅ (WebXR) |
| Joint Velocity | ✅ | ✅ | ✅ |
| Pinch/Grab Gesture | ✅ | ✅ | ✅ |
| Hand Stencil | ✅ (AROcclusion) | ❌ | ❌ |
| Audio Input | ✅ | ✅ | ✅ |
| AR Mesh Collision | ✅ (LiDAR) | ✅ (Guardian) | ❌ |
| AR Planes | ✅ | ✅ | Partial |

---

## References
- Apple Vision Framework: https://developer.apple.com/documentation/vision/vnhumanhandposeobservation
- XR Hands Package: https://docs.unity3d.com/Packages/com.unity.xr.hands@1.5
- HoloKit SDK: https://github.com/holoi/holokit-unity-sdk
- Portals_6 GestureDetector: `/Users/jamestunick/wkspaces/Portals_6/Assets/[H3M]/Portals/Code/v3/XR/GestureDetector.cs`
