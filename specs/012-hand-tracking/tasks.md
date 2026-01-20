# Spec 012: Hand Tracking - Implementation Tasks

**Status**: Not Started
**Estimated Effort**: 3-4 days

## Task Breakdown

### Phase 1: Interface Unification (Day 1)

- [ ] **T1.1** Create `IHandTrackingProvider.cs` interface
  - Location: `Assets/Scripts/HandTracking/Interfaces/`
  - Define all methods from spec
  - Include Hand and GestureType enums

- [ ] **T1.2** Create `HandJointID.cs` enum
  - Map to both HoloKit JointName and XRHandJointID
  - Include Palm joint for compatibility

- [ ] **T1.3** Create `HandTrackingProviderManager.cs`
  - Auto-detect available providers
  - Priority: HoloKit > XRHands > BodyPix > Touch
  - Singleton pattern with lazy init

- [ ] **T1.4** Create joint mapping utilities
  - `HoloKitJointMapper.cs` - HoloKit JointName → HandJointID
  - `XRHandsJointMapper.cs` - XRHandJointID → HandJointID

### Phase 2: Provider Implementation (Day 2)

- [ ] **T2.1** Implement `HoloKitHandTrackingProvider.cs`
  - Wrap existing HandVFXController HoloKit code
  - Implement IHandTrackingProvider interface
  - Use `#if HOLOKIT_AVAILABLE` guards

- [ ] **T2.2** Implement `XRHandsTrackingProvider.cs`
  - Wrap existing ARKitHandTracking XR Hands code
  - Implement IHandTrackingProvider interface
  - Use `#if UNITY_XR_HANDS` guards

- [ ] **T2.3** Implement `BodyPixHandTrackingProvider.cs`
  - Use BodyPartSegmenter for wrist estimation
  - Limited to wrist positions only
  - Always return false for gestures

- [ ] **T2.4** Implement `TouchInputHandTrackingProvider.cs`
  - Mouse position → hand position (screen to world)
  - Click/tap → pinch gesture
  - Editor-only with `#if UNITY_EDITOR` fallback

### Phase 3: VFX Integration (Day 3)

- [ ] **T3.1** Create `VFXHandBinder.cs`
  - Location: `Assets/Scripts/VFX/Binders/`
  - Bind all hand properties from spec
  - Use IHandTrackingProvider

- [ ] **T3.2** Update `VFXBinderUtility.cs`
  - Use new unified provider
  - Update `VFXBinderPreset.HandOnly` logic

- [ ] **T3.3** Refactor `HandVFXController.cs`
  - Remove direct HoloKit/BodyPix code
  - Delegate to IHandTrackingProvider
  - Keep velocity smoothing and audio reactive

- [ ] **T3.4** Refactor `ARKitHandTracking.cs`
  - Remove direct XR Hands code
  - Delegate to IHandTrackingProvider
  - Consolidate with HandVFXController if possible

### Phase 4: Gesture System (Day 3-4)

- [ ] **T4.1** Implement pinch detection
  - Configurable start/end thresholds
  - Hysteresis to prevent oscillation
  - Expose in Inspector

- [ ] **T4.2** Implement grab detection
  - All fingers curled check
  - Configurable threshold

- [ ] **T4.3** Create gesture event system
  - `OnGestureStart(Hand, GestureType)`
  - `OnGestureEnd(Hand, GestureType)`
  - `OnGestureHold(Hand, GestureType, float duration)`

### Phase 5: Testing & Verification (Day 4)

- [ ] **T5.1** Create `HandTrackingTests.cs` (EditMode)
  - Test joint mapping
  - Test pinch hysteresis logic
  - Test velocity calculation

- [ ] **T5.2** Create `HandTrackingPlayModeTests.cs`
  - Test provider auto-detection
  - Test VFX binding
  - Test fallback chain

- [ ] **T5.3** Manual device testing checklist
  - iPhone + HoloKit tracking
  - iPhone + HoloKit pinch
  - Quest 3 XR Hands
  - Editor touch simulation

- [ ] **T5.4** VFX verification
  - Test with Fluo brush VFX
  - Test with Buddha hand VFX
  - Verify all property bindings

## Verification Checklist

### HoloKit Backend
- [ ] Builds successfully with HOLOKIT_AVAILABLE
- [ ] Hand position tracked on device
- [ ] Pinch gesture detected
- [ ] VFX responds to hand input
- [ ] No errors in console

### XR Hands Backend
- [ ] Builds successfully with UNITY_XR_HANDS
- [ ] XRHandSubsystem initializes
- [ ] Hand position tracked
- [ ] Pinch gesture detected
- [ ] VFX responds to hand input

### Fallback Chain
- [ ] BodyPix activates when no HW
- [ ] Touch input works in Editor
- [ ] Graceful degradation message
- [ ] VFX still functional with fallback

### Performance
- [ ] < 16ms frame time maintained
- [ ] Zero per-frame allocations
- [ ] No GC spikes during tracking

## Files to Create

```
Assets/Scripts/HandTracking/
├── Interfaces/
│   └── IHandTrackingProvider.cs
├── Providers/
│   ├── HoloKitHandTrackingProvider.cs
│   ├── XRHandsTrackingProvider.cs
│   ├── BodyPixHandTrackingProvider.cs
│   └── TouchInputHandTrackingProvider.cs
├── HandTrackingProviderManager.cs
├── HandJointID.cs
└── Mappers/
    ├── HoloKitJointMapper.cs
    └── XRHandsJointMapper.cs

Assets/Scripts/VFX/Binders/
└── VFXHandBinder.cs

Assets/Tests/EditMode/
└── HandTrackingTests.cs

Assets/Tests/PlayMode/
└── HandTrackingPlayModeTests.cs
```

## Files to Modify

- `Assets/Scripts/HandTracking/HandVFXController.cs` - Refactor to use provider
- `Assets/Scripts/HandTracking/ARKitHandTracking.cs` - Refactor to use provider
- `Assets/Scripts/VFX/Binders/VFXBinderUtility.cs` - Update hand detection
- `Assets/Scripts/Debug/Editor/DebugDefinesSetup.cs` - Add UNITY_XR_HANDS define

---

## Part 2: VFX & Brush Painting Tasks

### Phase 5: Brush Controller (Day 4-5)

- [ ] **T5.1** Create `BrushController.cs`
  - Location: `Assets/Scripts/Painting/`
  - Manages active brush state
  - Routes hand data to VFX

- [ ] **T5.2** Create `GestureInterpreter.cs`
  - Interpret pinch as draw start/stop
  - Detect two-hand palette gesture
  - Swipe detection for brush switching

- [ ] **T5.3** Create `ParameterMapper.cs`
  - Hand speed → particle rate
  - Pinch distance → brush width
  - Wrist rotation → brush angle
  - Configurable curves in Inspector

- [ ] **T5.4** Create `VFXBrushBinder.cs`
  - Location: `Assets/Scripts/VFX/Binders/`
  - Bind brush properties to VFX Graph
  - Support all brush types (trail, ribbon, spray, etc.)

### Phase 6: Brush Palette System (Day 5)

- [ ] **T6.1** Create `BrushPalette.cs`
  - Circular layout (8 brushes)
  - Position above non-dominant palm
  - Point-to-select interaction

- [ ] **T6.2** Create `ColorPicker.cs`
  - Palm-projected color wheel
  - Pinch position → hue/saturation
  - Second hand → brightness

- [ ] **T6.3** Create palette prefab
  - Visual brush icons
  - Highlight selected brush
  - Animation on show/hide

### Phase 7: Stroke Management (Day 6)

- [ ] **T7.1** Create `StrokeManager.cs`
  - Stroke recording to GraphicsBuffer
  - Stroke persistence (save/load)
  - Undo/redo support

- [ ] **T7.2** Create `StrokePoint.cs` struct
  - Position, direction, width, color, timestamp
  - Aligned for GPU buffer

- [ ] **T7.3** Implement stroke selection
  - Fist gesture to grab stroke
  - Two-hand spread to scale
  - Open palm to delete

### Phase 8: Brush VFX Integration (Day 6-7)

- [ ] **T8.1** Configure Fluo brush VFX
  - Verify property names match spec
  - Test with hand input

- [ ] **T8.2** Create ribbon brush VFX
  - Width curve from hand data
  - Texture mapping

- [ ] **T8.3** Create spray brush VFX
  - Spread angle from wrist twist
  - Particle rate from speed

- [ ] **T8.4** Test all 8 brush types
  - Particle Trail, Ribbon, Spray, Glow
  - Fire, Sparkle, Tube, Smoke

## Brush System Files to Create

```
Assets/Scripts/Painting/
├── BrushController.cs
├── GestureInterpreter.cs
├── ParameterMapper.cs
├── BrushPalette.cs
├── ColorPicker.cs
└── StrokeManager.cs

Assets/Scripts/VFX/Binders/
└── VFXBrushBinder.cs

Assets/Prefabs/Painting/
├── BrushPalette.prefab
├── ColorWheel.prefab
└── BrushCursor.prefab

Assets/Scripts/Painting/Data/
└── StrokePoint.cs
```

## Brush Verification Checklist

### Gesture Controls
- [ ] Pinch starts drawing
- [ ] Release stops drawing
- [ ] Two-hand pinch opens palette
- [ ] Point selects brush
- [ ] Fist grabs stroke
- [ ] Open palm erases

### Parameter Mapping
- [ ] Speed → particle rate works
- [ ] Pinch distance → width works
- [ ] Wrist rotation → angle works
- [ ] All curves configurable

### Brush Types
- [ ] Particle Trail functional
- [ ] Ribbon functional
- [ ] Spray functional
- [ ] Glow functional
- [ ] Fire functional
- [ ] Sparkle functional
- [ ] Tube functional
- [ ] Smoke functional

### Persistence
- [ ] Strokes recorded to buffer
- [ ] Strokes persist after drawing
- [ ] Undo removes last stroke
- [ ] Save/load works

---

## Notes

- HoloKit uses different joint naming (JointName enum)
- XR Hands has more joints (26 vs 21)
- Palm joint exists in XR Hands but not HoloKit
- BodyPix only provides wrist positions (2 keypoints)
- Touch input simulates single hand only
- Fluo VFX from keijiro provides reference brush implementations
- Open Brush source available for gesture patterns reference
