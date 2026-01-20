# Feature Specification: Normcore AR Multiuser Drawing

**Feature Branch**: `010-normcore-multiuser`
**Created**: 2026-01-20
**Status**: Draft
**Input**: AR-only multiplayer drawing using Normcore + AR Spectator package

## Triple Verification

| Source | Status | Notes |
|--------|--------|-------|
| KB `_WEBRTC_MULTIUSER_MULTIPLATFORM_GUIDE.md` | Verified | Normcore comparison, pricing, features |
| `_ref/Normcore-Multiplayer-Drawing-Multiplayer/` | Verified | VR drawing example (OpenXR/Quest) |
| `Normcore AR Spectator.unitypackage` | Available | AR spectator for iOS |
| Normcore Docs | Verified | [normcore.io/docs](https://normcore.io/documentation/) |

## Overview

This spec extends the existing Normcore Multiplayer Drawing project for AR-only multiuser experiences. The original project targets Quest VR with OpenXR; we adapt it for iOS AR Foundation using the AR Spectator package.

### Goals

1. **AR-Only Multiplayer** - Replace VR controllers with AR hand tracking or touch input
2. **Test All Normcore Features** - Room connection, ownership, real-time sync, voice chat
3. **Spectator Mode** - AR users can view and interact with drawings in real-time
4. **Cross-Platform Foundation** - Architecture that could extend to Quest passthrough later

### Platform Comparison

| Feature | Original (Quest VR) | Target (iOS AR) |
|---------|---------------------|-----------------|
| Input | OpenXR Controllers | AR Touch / Hand Tracking |
| Tracking | 6DOF Headset | ARKit World Tracking |
| View | VR Headset | iPhone/iPad Camera |
| Render Pipeline | Built-in | URP |
| Normcore Version | 3.0.0 | 3.x (latest) |

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Room Connection (Priority: P1)

As an AR user, I want to join a shared room and see other users' drawings in real-time.

**Why this priority**: Core networking functionality must work first.

**Independent Test**:
1. Launch app on Device A
2. Verify Normcore connects (room URL visible)
3. Launch app on Device B with same room
4. Verify both devices show "connected" state
5. Verify client ID is unique per device

**Acceptance Scenarios**:
1. **Given** app launch, **When** Normcore initializes, **Then** connects within 5 seconds
2. **Given** two devices, **When** same room name, **Then** both see each other's client IDs
3. **Given** connection lost, **When** network restored, **Then** automatic reconnection

---

### User Story 2 - Brush Stroke Synchronization (Priority: P1)

As a user, I want to draw in AR and have my strokes appear on all connected devices instantly.

**Why this priority**: Core drawing feature.

**Independent Test**:
1. Device A: Touch and drag to draw
2. Verify stroke appears locally
3. Device B: Verify same stroke appears within 100ms
4. Device B: Draw a second stroke
5. Device A: Verify stroke appears

**Acceptance Scenarios**:
1. **Given** touch input, **When** drawing begins, **Then** BrushStroke instantiated via `Realtime.Instantiate()`
2. **Given** stroke synced, **When** ownership verified, **Then** stroke owned by creating client
3. **Given** rapid drawing, **When** ribbon points added, **Then** sync latency <100ms

---

### User Story 3 - AR Plane Anchoring (Priority: P1)

As an AR user, I want drawings to be anchored to real-world surfaces so they stay in place.

**Why this priority**: AR-specific requirement for usability.

**Independent Test**:
1. Point device at horizontal surface
2. Verify plane detected (visual indicator)
3. Draw on detected plane
4. Move device away and back
5. Verify drawing stays anchored to plane

**Acceptance Scenarios**:
1. **Given** AR session active, **When** plane detected, **Then** drawing enabled on plane
2. **Given** drawing anchored, **When** user moves, **Then** drawing maintains world position
3. **Given** session resumed, **When** relocalization succeeds, **Then** drawings reappear

---

### User Story 4 - Voice Chat (Priority: P2)

As a user, I want to talk to other users while drawing collaboratively.

**Why this priority**: Social feature enhances collaboration.

**Independent Test**:
1. Device A: Enable microphone
2. Device B: Enable microphone
3. Device A: Speak
4. Device B: Verify audio plays
5. Test vice versa

**Acceptance Scenarios**:
1. **Given** microphone permission, **When** RealtimeAvatarVoice attached, **Then** audio transmitted
2. **Given** voice active, **When** network latency measured, **Then** <200ms one-way
3. **Given** multiple users, **When** spatial audio enabled, **Then** voice positioned at avatar

---

### User Story 5 - Brush Color & Width (Priority: P2)

As a user, I want to change brush color and width and have those settings sync across devices.

**Why this priority**: Enhanced drawing features.

**Independent Test**:
1. Device A: Select red color
2. Device A: Draw stroke
3. Device B: Verify red stroke visible
4. Device B: Change to thick brush
5. Device B: Draw stroke
6. Device A: Verify thick stroke visible

**Acceptance Scenarios**:
1. **Given** color change, **When** stroke synced, **Then** color matches on all devices
2. **Given** width change, **When** ribbon generated, **Then** width accurate within 5%
3. **Given** realtime property, **When** color updated, **Then** existing strokes don't change (immutable after creation)

---

### Edge Cases

- Network disconnect during draw → Buffer locally, sync on reconnect
- Two users draw same spot → Both strokes render (no collision)
- Late joiner → Receives all existing strokes via Normcore datastore
- High-latency connection (>500ms) → Graceful degradation, visual indicator
- Microphone permission denied → Drawing works, voice disabled with UI indicator

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST connect to Normcore room on app launch
- **FR-002**: System MUST sync brush strokes to all connected clients
- **FR-003**: System MUST support AR plane detection for drawing surfaces
- **FR-004**: System MUST support touch input for drawing (fallback from controllers)
- **FR-005**: System SHOULD support voice chat via RealtimeAvatarVoice
- **FR-006**: System SHOULD support brush color/width customization
- **FR-007**: System MUST handle reconnection gracefully

### Non-Functional Requirements

- **NFR-001**: Connection time MUST be <5 seconds
- **NFR-002**: Stroke sync latency MUST be <100ms (same region)
- **NFR-003**: Voice latency MUST be <200ms one-way
- **NFR-004**: App MUST maintain 30+ FPS during drawing
- **NFR-005**: Battery usage SHOULD NOT exceed 20%/hour during active session

### Key Components

| Component | Source | Adaptation Required |
|-----------|--------|---------------------|
| `Brush.cs` | `_ref/Normcore.../Brush/` | Replace XRNode with touch/hand input |
| `BrushStroke.cs` | `_ref/Normcore.../Brush/` | Keep as-is (RealtimeModel) |
| `BrushStrokeMesh.cs` | `_ref/Normcore.../Brush/` | Keep as-is (ribbon generation) |
| `RealtimeAvatarVoice` | Normcore SDK | Add to AR avatar prefab |
| AR Spectator components | `.unitypackage` | Import and configure |
| `ARBrushInput.cs` | NEW | AR touch → brush tip position |
| `ARPlaneDrawing.cs` | NEW | Plane detection → drawing anchor |

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `com.normalvr.normcore` | 3.0.0+ | Realtime networking |
| `com.unity.xr.arfoundation` | 6.2.1 | AR Foundation |
| `com.unity.xr.arkit` | 6.2.1 | ARKit provider |

### Scripting Defines

```
NORMCORE_AVAILABLE      - Normcore SDK installed
AR_SPECTATOR_AVAILABLE  - AR Spectator package imported
```

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Room connection in <5 seconds (95th percentile)
- **SC-002**: Stroke sync latency <100ms (P95, same-region)
- **SC-003**: Voice latency <200ms (P95)
- **SC-004**: 30+ FPS on iPhone 12+ during active drawing
- **SC-005**: Successful reconnection after network interruption within 10 seconds
- **SC-006**: Zero data loss for strokes during normal operation

## Architecture

### Data Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           iOS AR Device                                      │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ ARSession → ARPlaneManager → Plane Detection                            ││
│  │                    ↓                                                    ││
│  │            ARPlaneDrawing.cs → Drawing Surface                          ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ Touch Input / Hand Tracking                                             ││
│  │         ↓                                                               ││
│  │   ARBrushInput.cs → (_handPosition, _handRotation)                      ││
│  │         ↓                                                               ││
│  │   Brush.cs (adapted) → trigger detection                                ││
│  │         ↓                                                               ││
│  │   Realtime.Instantiate(BrushStrokePrefab)                               ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────┬───────────────────────────────────────────┘
                                  │ WebSocket/UDP
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Normcore Cloud                                        │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ Room: "ar-drawing-demo"                                                 ││
│  │   - Client A (iPhone 15)                                                ││
│  │   - Client B (iPhone 14)                                                ││
│  │                                                                         ││
│  │ Datastore:                                                              ││
│  │   - BrushStroke[] (RealtimeModel)                                       ││
│  │   - RibbonPoint[] per stroke                                            ││
│  │   - Avatar positions (optional)                                         ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
```

### Normcore Model Hierarchy

```
Realtime
├── RealtimeAvatarManager
│   └── LocalAvatar
│       ├── ARBrushInput (local only)
│       ├── RealtimeAvatarVoice
│       └── RealtimeTransform (position sync)
└── BrushStroke (instantiated per stroke)
    └── BrushStrokeModel
        ├── ribbonPoints: RealtimeArray<RibbonPointModel>
        ├── color: Color
        └── width: float
```

## Implementation Notes

### Adapting VR → AR Input

The original `Brush.cs` uses `XRNode.LeftHand/RightHand` for controller tracking:

```csharp
// Original (VR)
XRNode node = _hand == Hand.LeftHand ? XRNode.LeftHand : XRNode.RightHand;
bool handIsTracking = UpdatePose(node, ref _handPosition, ref _handRotation);
bool triggerPressed = Input.GetAxisRaw(trigger) > 0.1f;
```

AR adaptation (`ARBrushInput.cs`):

```csharp
// AR Touch Input
if (Input.touchCount > 0)
{
    Touch touch = Input.GetTouch(0);
    Ray ray = _arCamera.ScreenPointToRay(touch.position);

    // Raycast to AR planes or fixed distance
    if (Physics.Raycast(ray, out RaycastHit hit, 10f, _planeLayer))
    {
        _handPosition = hit.point;
        _handRotation = Quaternion.LookRotation(hit.normal);
        triggerPressed = touch.phase != TouchPhase.Ended;
    }
}
```

### AR Spectator Package Integration

The `.unitypackage` likely contains:
- AR-specific Normcore prefabs
- Camera rig for AR
- Spectator view components

Integration steps:
1. Import package to MetavidoVFX project
2. Configure Normcore app key in `NormcoreAppSettings.asset`
3. Replace VR Player prefab with AR Spectator prefab
4. Update scene to use AR Session instead of XR Origin

### Voice Chat Setup

Normcore's voice chat requires:
1. `RealtimeAvatarVoice` component on avatar prefab
2. Microphone permission in iOS capabilities
3. Audio source for playback

## Security Considerations

- Normcore app key stored in `NormcoreAppSettings.asset` (not in version control)
- Room names should be sufficiently random to prevent uninvited access
- Voice data is encrypted in transit by Normcore
- No persistent storage of audio

## References

### Normcore
- [Normcore Documentation](https://normcore.io/documentation/)
- [Multiplayer Drawing Guide](https://normcore.io/documentation/guides/recipes/creating-a-multiplayer-drawing-app)
- [RealtimeAvatarVoice](https://normcore.io/documentation/realtime/realtimeavatarvoice)

### Source Projects
- `_ref/Normcore-Multiplayer-Drawing-Multiplayer/` - VR drawing example
- `Normcore AR Spectator.unitypackage` - AR spectator addon

### Related KB
- `KnowledgeBase/_WEBRTC_MULTIUSER_MULTIPLATFORM_GUIDE.md`

---

*Created: 2026-01-20*
*Author: Claude Code + User*
