# Spec 012: Hand Tracking Implementation & Verification

**Status**: In Progress (Providers implemented 2026-01-21)
**Created**: 2026-01-20
**Sprint**: 3 (estimated)
**Implementation**:
- ✅ IHandTrackingProvider.cs - Unified interface (26 joints)
- ✅ HandTrackingProviderManager.cs - Auto-discovery, fallback chain
- ✅ HoloKitHandTrackingProvider.cs - Priority 100, native iOS
- ✅ XRHandsTrackingProvider.cs - Priority 80, AR Foundation
- ✅ MediaPipeHandTrackingProvider.cs - Priority 60, ML-based
- ✅ BodyPixHandTrackingProvider.cs - Priority 40, wrist fallback
- ✅ TouchInputHandTrackingProvider.cs - Priority 10, Editor
- ✅ VFXHandBinder.cs - Hand→VFX property binding
- ⬜ BrushController for stroke management
- ⬜ Two-hand palette gesture

## Overview

Implement and verify hand tracking support for both AR Foundation XR Hands and HoloKit SDK backends, with unified VFX integration and comprehensive test coverage.

## User Scenarios

### Scenario 0: Brush Painting with Hand Gestures
**Actor**: User painting in AR with hand-controlled VFX brush
**Precondition**: Hand tracking active, VFX brush system enabled
**Flow**:
1. User makes pinch gesture to start drawing
2. Index finger tip position becomes brush cursor
3. Pinch distance controls brush width (tighter = thinner)
4. Hand speed controls brush intensity/particle rate
5. Hand rotation controls brush angle/orientation
6. Opening hand (release pinch) stops drawing
7. Two-hand pinch gesture opens brush palette

### Scenario 0b: Brush Selection via Gesture
**Actor**: User selecting different brush types
**Flow**:
1. User performs palm-up gesture (left hand)
2. Virtual brush palette appears above palm
3. User points with right index finger to select brush
4. Pinch gesture confirms selection
5. Palette disappears, new brush active

### Scenario 1: HoloKit Hand Tracking (iOS Device)
**Actor**: Developer testing on iPhone with HoloKit SDK
**Precondition**: HOLOKIT_AVAILABLE define enabled, HoloKit SDK installed
**Flow**:
1. Build and deploy to iOS device
2. Launch app with HoloKit hand tracking enabled
3. Hold hand in camera view
4. Verify wrist position tracked (21 joints available)
5. Perform pinch gesture (thumb + index)
6. Verify pinch detection triggers VFX response
7. Move hand to verify velocity/speed tracking

### Scenario 2: AR Foundation XR Hands (Cross-Platform)
**Actor**: Developer testing XR Hands subsystem
**Precondition**: UNITY_XR_HANDS define enabled, XR Hands package installed
**Flow**:
1. Build for supported platform (Quest, iOS with ARKit 4+)
2. Launch app with XR Hands enabled
3. Verify XRHandSubsystem initializes
4. Track hand joint positions (XRHandJointID)
5. Perform pinch gesture
6. Verify VFX responds to hand input

### Scenario 3: BodyPix Fallback (No Hand Tracking)
**Actor**: User on device without hand tracking hardware
**Precondition**: No hand tracking available, BodyPartSegmenter active
**Flow**:
1. App detects no hand tracking hardware
2. Falls back to BodyPix wrist keypoint estimation
3. Tracks approximate wrist positions from body segmentation
4. Provides degraded but functional hand position to VFX

### Scenario 4: Touch Input Fallback (Editor/Desktop)
**Actor**: Developer testing in Unity Editor
**Precondition**: No hand tracking, no camera
**Flow**:
1. Run in Editor without device
2. Use mouse/touch to simulate hand position
3. Click/tap to simulate pinch gesture
4. Verify VFX responds to simulated input

## Requirements

### Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-01 | Support HoloKit SDK hand tracking (21 joints) | P0 |
| FR-02 | Support AR Foundation XR Hands (26 joints) | P0 |
| FR-03 | Unified IHandTrackingProvider interface | P0 |
| FR-04 | Pinch detection with hysteresis | P0 |
| FR-05 | Grab gesture detection | P1 |
| FR-06 | Hand velocity calculation | P0 |
| FR-07 | Left/right hand differentiation | P1 |
| FR-08 | BodyPix fallback for wrist positions | P2 |
| FR-09 | Touch input fallback for Editor | P1 |
| FR-10 | VFX property binding (HandPosition, HandVelocity, etc.) | P0 |

### Non-Functional Requirements

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-01 | Tracking latency | < 16ms (60 FPS) |
| NFR-02 | Pinch detection accuracy | > 95% |
| NFR-03 | False positive pinch rate | < 5% |
| NFR-04 | CPU overhead | < 2ms per frame |
| NFR-05 | Memory allocation | Zero per-frame alloc |

## Architecture

### Unified Interface

```csharp
public interface IHandTrackingProvider
{
    bool IsTracking { get; }
    bool TryGetJointPosition(HandJointID joint, out Vector3 position);
    bool TryGetJointRotation(HandJointID joint, out Quaternion rotation);
    float GetPinchStrength(Hand hand);
    bool IsPinching(Hand hand);
    Vector3 GetHandVelocity(Hand hand);
    event Action<Hand, GestureType> OnGestureDetected;
}

public enum Hand { Left, Right }
public enum GestureType { Pinch, Grab, Point, OpenPalm }

public enum HandJointID
{
    Wrist,
    ThumbMetacarpal, ThumbProximal, ThumbDistal, ThumbTip,
    IndexMetacarpal, IndexProximal, IndexIntermediate, IndexDistal, IndexTip,
    MiddleMetacarpal, MiddleProximal, MiddleIntermediate, MiddleDistal, MiddleTip,
    RingMetacarpal, RingProximal, RingIntermediate, RingDistal, RingTip,
    LittleMetacarpal, LittleProximal, LittleIntermediate, LittleDistal, LittleTip,
    Palm
}
```

### Provider Implementations

```
IHandTrackingProvider
├── HoloKitHandTrackingProvider   (HOLOKIT_AVAILABLE)
├── XRHandsTrackingProvider       (UNITY_XR_HANDS)
├── BodyPixHandTrackingProvider   (Fallback)
└── TouchInputHandTrackingProvider (Editor)
```

### VFX Property Bindings

| Property | Type | Description |
|----------|------|-------------|
| HandPosition | Vector3 | Primary hand wrist position |
| HandVelocity | Vector3 | Hand movement velocity |
| HandSpeed | float | Velocity magnitude |
| BrushWidth | float | Mapped from pinch distance |
| IsPinching | bool | Pinch gesture active |
| PinchDistance | float | Thumb-index distance |
| TrailLength | float | Mapped from hand speed |
| LeftHandPosition | Vector3 | Left hand wrist |
| RightHandPosition | Vector3 | Right hand wrist |

## Existing Implementation Analysis

### HandVFXController.cs (HoloKit Path)
**Location**: `Assets/Scripts/HandTracking/HandVFXController.cs`
**Lines**: ~420
**Features**:
- HoloKit SDK integration via `#if HOLOKIT_AVAILABLE`
- HandTrackingManager for 21-joint tracking
- HandGestureRecognitionManager for gestures
- BodyPix fallback using BodyPartSegmenter
- Pinch detection with hysteresis (start: 0.03m, end: 0.05m)
- Velocity smoothing with configurable factor
- Audio reactive integration (AudioBridge)

**Key Code Pattern**:
```csharp
#if HOLOKIT_AVAILABLE && !UNITY_EDITOR
Vector3 thumbTip = handTrackingManager.GetHandJointPosition(0, JointName.ThumbTip);
Vector3 indexTip = handTrackingManager.GetHandJointPosition(0, JointName.IndexTip);
leftPinchDistance = Vector3.Distance(thumbTip, indexTip);
#endif
```

### ARKitHandTracking.cs (XR Hands Path)
**Location**: `Assets/Scripts/HandTracking/ARKitHandTracking.cs`
**Lines**: ~340
**Features**:
- XR Hands subsystem via `#if UNITY_XR_HANDS`
- XRHandSubsystem lifecycle management
- 26-joint tracking (XRHandJointID)
- Touch input fallback
- Similar VFX property bindings

**Key Code Pattern**:
```csharp
#if UNITY_XR_HANDS
if (hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbPose) &&
    hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
{
    float distance = Vector3.Distance(thumbPose.position, indexPose.position);
}
#endif
```

## Implementation Tasks

### Phase 1: Interface Unification
- [ ] Create `IHandTrackingProvider` interface
- [ ] Create `HandJointID` enum mapping both systems
- [ ] Create `HandTrackingManager` (not HoloKit's) as unified entry point
- [ ] Implement provider auto-detection

### Phase 2: Provider Implementation
- [ ] Implement `HoloKitHandTrackingProvider`
- [ ] Implement `XRHandsTrackingProvider`
- [ ] Implement `BodyPixHandTrackingProvider`
- [ ] Implement `TouchInputHandTrackingProvider`

### Phase 3: VFX Integration
- [ ] Update `VFXHandDataBinder` (preferred) to use unified provider
- [ ] Create `VFXHandBinder` component
- [ ] Test with existing hand-driven VFX (Fluo, Buddha)

### Phase 4: Gesture System
- [ ] Implement pinch detection with configurable thresholds
- [ ] Implement grab detection
- [ ] Add gesture event system
- [ ] Test gesture reliability

## Test Verification Plan

### Unit Tests

| Test ID | Description | Pass Criteria |
|---------|-------------|---------------|
| UT-01 | Joint mapping HoloKit→Unified | All 21 joints map correctly |
| UT-02 | Joint mapping XRHands→Unified | All 26 joints map correctly |
| UT-03 | Pinch hysteresis logic | No rapid on/off oscillation |
| UT-04 | Velocity calculation | Matches expected physics |
| UT-05 | Provider auto-detection | Correct provider selected |

### Integration Tests

| Test ID | Description | Pass Criteria |
|---------|-------------|---------------|
| IT-01 | HoloKit hand tracking end-to-end | VFX responds to hand |
| IT-02 | XR Hands tracking end-to-end | VFX responds to hand |
| IT-03 | BodyPix fallback activation | Falls back when no HW |
| IT-04 | Touch input in Editor | Simulates hand correctly |
| IT-05 | Provider hot-swap | Graceful transition |

### Device Tests (Manual)

| Test ID | Device | Test | Pass Criteria |
|---------|--------|------|---------------|
| DT-01 | iPhone + HoloKit | Track hand position | Stable tracking, < 5cm error |
| DT-02 | iPhone + HoloKit | Pinch gesture | 95%+ detection rate |
| DT-03 | iPhone + HoloKit | Hand velocity VFX | Smooth trail response |
| DT-04 | Quest 3 | XR Hands tracking | Stable tracking |
| DT-05 | Quest 3 | Pinch gesture | 95%+ detection rate |
| DT-06 | iPhone (no HoloKit) | BodyPix fallback | Wrist position estimated |

### VFX Verification Tests

| Test ID | VFX | Property | Pass Criteria |
|---------|-----|----------|---------------|
| VT-01 | Fluo brush VFX | HandPosition | Particles follow hand |
| VT-02 | Fluo brush VFX | BrushWidth | Width changes with pinch |
| VT-03 | Buddha hand VFX | HandVelocity | Velocity affects particles |
| VT-04 | Trail VFX | TrailLength | Trail scales with speed |
| VT-05 | Any hand VFX | IsPinching | VFX reacts to pinch |

## Success Criteria

### P0 (Must Have)
- [ ] HoloKit hand tracking works on iOS device
- [ ] XR Hands tracking works on supported platforms
- [ ] Pinch detection accuracy > 95%
- [ ] VFX properties bound correctly
- [ ] Zero per-frame allocations

### P1 (Should Have)
- [ ] Unified IHandTrackingProvider interface
- [ ] BodyPix fallback functional
- [ ] Touch input works in Editor
- [ ] Grab gesture detection
- [ ] Left/right hand differentiation

### P2 (Nice to Have)
- [ ] Gesture event system
- [ ] Hand skeleton visualization
- [ ] Configurable gesture thresholds in Inspector
- [ ] Runtime provider switching

## Dependencies

### Packages Required
- `com.holokit.ios` (HoloKit SDK) - for HoloKit path
- `com.unity.xr.hands` (XR Hands) - for AR Foundation path
- `com.unity.barracuda` or Sentis - for BodyPix fallback

### Define Symbols
- `HOLOKIT_AVAILABLE` - Enable HoloKit hand tracking
- `UNITY_XR_HANDS` - Enable XR Hands subsystem
- `BODYPIX_AVAILABLE` - Enable BodyPix fallback

---

## Part 2: VFX & Brush Painting Control

### Overview

Hand-controlled VFX and brush painting following Open Brush patterns. Users control particle brushes, trails, and volumetric effects through natural hand gestures and movement.

### Gesture Vocabulary

#### Core Gestures (5 Essential)

| Gesture | Detection | Action | Feedback |
|---------|-----------|--------|----------|
| **Pinch** | Thumb-Index < 3cm | Start/stop drawing | Haptic pulse |
| **Pinch Hold** | Sustained pinch | Continue drawing | Trail follows fingertip |
| **Pinch Drag** | Pinch + movement | Draw stroke | Brush particles spawn |
| **Open Palm** | All fingers extended | Stop drawing / Erase mode | Brush cursor changes |
| **Fist** | All fingers curled | Grab/move stroke | Selected stroke highlights |

#### Advanced Gestures (Modifiers)

| Gesture | Detection | Action |
|---------|-----------|--------|
| **Two-Hand Pinch** | Both hands pinching | Open brush palette |
| **Pinch Distance** | Thumb-Index 1-5cm | Adjust brush width continuously |
| **Wrist Rotation** | Palm facing changes | Rotate brush angle |
| **Two-Hand Spread** | Hands moving apart | Scale selected stroke |
| **Point** | Index extended only | Select / UI interaction |

### Hand → Brush Parameter Mapping

#### Speed-Based Parameters

| Parameter | Hand Input | Mapping | Range |
|-----------|------------|---------|-------|
| **ParticleRate** | HandSpeed | Faster = more particles | 10-1000/sec |
| **StrokeOpacity** | HandSpeed | Faster = more transparent | 0.2-1.0 |
| **TrailLength** | HandSpeed | Faster = longer trail | 0.1-2.0m |
| **JitterAmount** | HandSpeed | Faster = more jitter | 0-0.1m |

```csharp
// Speed mapping example
float speedNormalized = Mathf.InverseLerp(0f, 2f, handSpeed); // 0-2 m/s range
float particleRate = Mathf.Lerp(10f, 1000f, speedNormalized);
float trailLength = Mathf.Lerp(0.1f, 2f, speedNormalized);
```

#### Shape-Based Parameters

| Parameter | Hand Input | Mapping |
|-----------|------------|---------|
| **BrushWidth** | Pinch distance | Tighter pinch = thinner brush |
| **BrushAngle** | Wrist rotation | Palm direction → brush tilt |
| **BrushPressure** | Pinch strength | Harder pinch = denser particles |

```csharp
// Pinch distance to brush width (exponential for fine control at small sizes)
float pinchNorm = Mathf.InverseLerp(0.01f, 0.08f, pinchDistance);
float brushWidth = Mathf.Lerp(0.002f, 0.1f, pinchNorm * pinchNorm);

// Wrist rotation to brush angle (Y-axis only for stability)
float brushAngle = Mathf.Atan2(palmForward.x, palmForward.z) * Mathf.Rad2Deg;
```

#### Velocity-Based Parameters

| Parameter | Hand Input | Mapping |
|-----------|------------|---------|
| **ParticleVelocity** | HandVelocity direction | Particles shoot in movement direction |
| **RibbonCurvature** | HandAcceleration | Sharper turns = tighter curves |
| **SprayAngle** | Angular velocity | Wrist twist spreads spray |

### VFX Property Bindings (Brush System)

```csharp
// Core brush properties
public static readonly ExposedProperty BrushPosition = "BrushPosition";      // Vector3
public static readonly ExposedProperty BrushDirection = "BrushDirection";    // Vector3
public static readonly ExposedProperty BrushWidth = "BrushWidth";            // float
public static readonly ExposedProperty BrushColor = "BrushColor";            // Vector4
public static readonly ExposedProperty BrushTexture = "BrushTexture";        // Texture2D

// Dynamic brush properties (from hand)
public static readonly ExposedProperty IsDrawing = "IsDrawing";              // bool
public static readonly ExposedProperty DrawSpeed = "DrawSpeed";              // float
public static readonly ExposedProperty DrawPressure = "DrawPressure";        // float
public static readonly ExposedProperty DrawAngle = "DrawAngle";              // float

// Stroke accumulation
public static readonly ExposedProperty StrokeBuffer = "StrokeBuffer";        // GraphicsBuffer
public static readonly ExposedProperty StrokePointCount = "StrokePointCount";// uint
```

### Brush Types (VFX Library)

| Brush | VFX Asset | Properties | Best For |
|-------|-----------|------------|----------|
| **Particle Trail** | `Fluo/Brush.vfx` | Color, Width, Lifetime | Basic painting |
| **Ribbon** | `Fluo/Ribbon.vfx` | Width curve, Texture | Smooth strokes |
| **Spray** | `Essentials/ParticleSpray.vfx` | Spread angle, Rate | Graffiti effect |
| **Glow** | `Portals6/GlowTrail.vfx` | Emission, Bloom | Light painting |
| **Fire** | `Testbed4/Flame.vfx` | Intensity, Turbulence | Dramatic effects |
| **Sparkle** | `Buddha/Sparkle.vfx` | Size, Frequency | Magical effects |
| **Tube** | `WebRTC/Tube.vfx` | Radius, Segments | 3D strokes |
| **Smoke** | `Rcam4/Smoke.vfx` | Density, Dissipation | Volumetric |

### Brush Selection System

#### Gesture-Based Selection

```csharp
public class BrushPalette : MonoBehaviour
{
    // Triggered by two-hand pinch gesture
    public void ShowPalette(Vector3 palmPosition)
    {
        // Position palette above non-dominant hand palm
        transform.position = palmPosition + Vector3.up * 0.15f;
        transform.LookAt(Camera.main.transform);

        // Show 8 brush options in circular layout
        for (int i = 0; i < 8; i++)
        {
            float angle = i * (360f / 8f) * Mathf.Deg2Rad;
            brushIcons[i].localPosition = new Vector3(
                Mathf.Sin(angle) * 0.08f,
                Mathf.Cos(angle) * 0.08f,
                0
            );
        }
    }

    // Point with dominant hand to select
    public void SelectBrush(Vector3 fingerTipPosition)
    {
        // Find closest brush icon
        int selected = GetClosestBrushIndex(fingerTipPosition);
        HighlightBrush(selected);

        // Pinch confirms selection
        if (IsPinching)
        {
            SetActiveBrush(selected);
            HidePalette();
        }
    }
}
```

#### Quick Switch (Swipe Gestures)

| Swipe Direction | Action |
|-----------------|--------|
| **Left** | Previous brush |
| **Right** | Next brush |
| **Up** | Increase brush size |
| **Down** | Decrease brush size |

### Color Selection

#### Hand-Based Color Picker

```csharp
// Palm-up shows color wheel projected onto palm
// Pinch position on wheel selects hue
// Distance from center selects saturation
// Second hand height selects brightness

public Color GetColorFromPalm(Vector3 palmCenter, Vector3 pinchPosition)
{
    Vector3 offset = pinchPosition - palmCenter;

    // Hue from angle
    float hue = Mathf.Atan2(offset.x, offset.z) / (2 * Mathf.PI) + 0.5f;

    // Saturation from distance (0-8cm = 0-1)
    float sat = Mathf.Clamp01(offset.magnitude / 0.08f);

    // Value from second hand height relative to color hand
    float val = Mathf.Clamp01((secondHandY - palmCenter.y + 0.1f) / 0.2f);

    return Color.HSVToRGB(hue, sat, val);
}
```

### Stroke Recording & Persistence

#### GraphicsBuffer Stroke Storage

```csharp
public struct StrokePoint
{
    public Vector3 position;   // World position
    public Vector3 direction;  // Tangent direction
    public float width;        // Brush width at point
    public Color32 color;      // RGBA color
    public float timestamp;    // For replay/animation
}

// Buffer layout
GraphicsBuffer strokeBuffer = new GraphicsBuffer(
    GraphicsBuffer.Target.Structured,
    maxPoints,                 // 10000 points per stroke
    System.Runtime.InteropServices.Marshal.SizeOf<StrokePoint>()
);
```

### Implementation Architecture

```
HandTrackingProvider
    ↓
BrushController
    ├─ GestureInterpreter → Pinch, Grab, Point detection
    ├─ ParameterMapper → Speed/shape → brush params
    ├─ BrushPalette → Selection UI
    └─ StrokeManager → Record/playback
    ↓
VFXBrushBinder
    ↓
VFX Graph (brush asset)
```

### Test Scenarios (Brush System)

| Test ID | Description | Pass Criteria |
|---------|-------------|---------------|
| BT-01 | Pinch to start drawing | Particles spawn at fingertip |
| BT-02 | Release to stop | No particles after release |
| BT-03 | Speed affects rate | Faster = more particles |
| BT-04 | Pinch distance = width | Visual width matches |
| BT-05 | Two-hand palette | Palette appears, selectable |
| BT-06 | Color selection | Correct HSV from palm |
| BT-07 | Stroke persistence | Stroke remains after drawing |
| BT-08 | Erase gesture | Selected strokes deleted |

---

## References

- **Existing Code**: `Assets/Scripts/HandTracking/`
- **HoloKit SDK**: https://github.com/holoi/holokit-unity-sdk
- **XR Hands**: https://docs.unity3d.com/Packages/com.unity.xr.hands@1.4/manual/index.html
- **KB Reference**: `KnowledgeBase/_HAND_SENSING_CAPABILITIES.md`
- **TouchingHologram**: `TouchingHologram/` (21 hand-driven Buddha VFX)
- **Open Brush**: https://github.com/icosa-foundation/open-brush
- **Fluo**: `Fluo-GHURT-main/` (keijiro brush VFX)
- **VFX Library**: `Resources/VFX/` (235 total VFX assets)
