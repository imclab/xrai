# Tasks: Normcore AR Multiuser Drawing

**Spec**: `010-normcore-multiuser`
**Created**: 2026-01-20

## Sprint Overview

| Sprint | Focus | Status |
|--------|-------|--------|
| Sprint 0 | Project Setup & AR Spectator Import | Not Started |
| Sprint 1 | Core Networking Tests | Not Started |
| Sprint 2 | AR Input & Drawing Adaptation | Not Started |
| Sprint 3 | Voice Chat & Polish | Not Started |

---

## Sprint 0: Project Setup & AR Spectator Import

### Task 0.1: Import AR Spectator Package
**Priority**: P1 | **Estimate**: 1h | **Status**: Not Started

Import and configure the AR Spectator unitypackage.

**Subtasks**:
- [ ] Create new scene `Scenes/NormcoreARDrawing.unity`
- [ ] Import `Normcore AR Spectator.unitypackage`
- [ ] Verify no import errors
- [ ] Document imported components and prefabs
- [ ] Identify which VR components need replacement

**Acceptance**:
- [ ] Package imported without errors
- [ ] Contents documented in this file

---

### Task 0.2: Configure Normcore Project Settings
**Priority**: P1 | **Estimate**: 30m | **Status**: Not Started

Configure Normcore SDK for the MetavidoVFX project.

**Subtasks**:
- [ ] Add Normcore scoped registry to `Packages/manifest.json`
- [ ] Add `com.normalvr.normcore` package (3.0.0+)
- [ ] Create `NormcoreAppSettings.asset` in Resources
- [ ] Configure app key (from normcore.io dashboard)
- [ ] Add `NORMCORE_AVAILABLE` scripting define

**Normcore Registry**:
```json
{
  "scopedRegistries": [
    {
      "name": "Normal",
      "url": "https://normcore-registry.normcore.io",
      "scopes": ["com.normalvr", "io.normcore"]
    }
  ]
}
```

**Acceptance**:
- [ ] Normcore compiles without errors
- [ ] App key configured (test connection works)

---

### Task 0.3: Setup AR Foundation Scene
**Priority**: P1 | **Estimate**: 1h | **Status**: Not Started

Configure AR Foundation components for drawing.

**Subtasks**:
- [ ] Add AR Session + AR Session Origin
- [ ] Add AR Plane Manager (horizontal detection)
- [ ] Add AR Raycast Manager
- [ ] Configure URP renderer for AR
- [ ] Add basic AR debug visualization (plane overlays)

**Hierarchy**:
```
NormcoreARDrawing
├── AR Session
├── AR Session Origin
│   ├── AR Camera
│   ├── AR Plane Manager
│   └── AR Raycast Manager
├── Normcore
│   └── Realtime
├── Drawing
│   └── [BrushStroke instances]
└── UI
    └── ConnectionStatus
```

**Acceptance**:
- [ ] AR session initializes on device
- [ ] Planes detected and visualized

---

## Sprint 1: Core Networking Tests

### Task 1.1: Room Connection Test
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Verify Normcore room connection works on iOS.

**Test Procedure**:
```
1. Deploy to iOS device
2. Launch app
3. Verify console shows "Connected to room: [name]"
4. Verify client ID assigned
5. Measure connection time (target: <5s)
```

**Subtasks**:
- [ ] Create `NormcoreConnectionTest.cs` component
- [ ] Log connection events (`didConnectToRoom`, `didDisconnectFromRoom`)
- [ ] Display connection status in UI
- [ ] Test with known room name
- [ ] Test auto-generated room name

**Test Script**:
```csharp
public class NormcoreConnectionTest : MonoBehaviour
{
    [SerializeField] private Realtime _realtime;
    [SerializeField] private TMP_Text _statusText;

    private float _connectStartTime;

    void Start()
    {
        _connectStartTime = Time.time;
        _realtime.didConnectToRoom += OnConnect;
        _realtime.didDisconnectFromRoom += OnDisconnect;
    }

    void OnConnect(Realtime realtime)
    {
        float elapsed = Time.time - _connectStartTime;
        Debug.Log($"[Normcore] Connected in {elapsed:F2}s, clientID={realtime.clientID}");
        _statusText.text = $"Connected ({elapsed:F1}s)";
    }
}
```

**Acceptance**:
- [ ] Connection time <5 seconds
- [ ] Client ID displayed
- [ ] UI shows connection status

---

### Task 1.2: Multi-Device Room Join Test
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Verify multiple devices can join the same room.

**Test Procedure**:
```
1. Deploy to Device A (iPhone 15)
2. Deploy to Device B (iPhone 14 or iPad)
3. Both launch with same room name
4. Verify both show "2 clients connected"
5. Verify client IDs are unique
```

**Subtasks**:
- [ ] Create `ClientListUI.cs` to show connected clients
- [ ] Test on 2 devices simultaneously
- [ ] Test on 3+ devices (stretch goal)
- [ ] Document client ID assignment

**Acceptance**:
- [ ] Both devices show connected
- [ ] Client count accurate on both
- [ ] Unique client IDs

---

### Task 1.3: Reconnection Test
**Priority**: P2 | **Estimate**: 1h | **Status**: Not Started

Verify reconnection after network interruption.

**Test Procedure**:
```
1. Connect to room
2. Toggle airplane mode
3. Wait 5 seconds
4. Disable airplane mode
5. Verify automatic reconnection
```

**Subtasks**:
- [ ] Add reconnection handler
- [ ] Add UI indicator for reconnecting state
- [ ] Measure reconnection time
- [ ] Test with airplane mode toggle

**Acceptance**:
- [ ] Reconnects automatically within 10 seconds
- [ ] No data loss for pending operations

---

## Sprint 2: AR Input & Drawing Adaptation

### Task 2.1: Create ARBrushInput Component
**Priority**: P1 | **Estimate**: 3h | **Status**: Not Started

Replace VR controller input with AR touch input.

**Subtasks**:
- [ ] Create `Assets/Scripts/Normcore/ARBrushInput.cs`
- [ ] Convert touch position to world position via AR raycast
- [ ] Support drawing on detected planes
- [ ] Support drawing at fixed distance (fallback)
- [ ] Add visual indicator for brush tip position

**API**:
```csharp
public class ARBrushInput : MonoBehaviour
{
    public Vector3 BrushPosition { get; private set; }
    public Quaternion BrushRotation { get; private set; }
    public bool IsDrawing { get; private set; }

    public event Action<Vector3, Quaternion> OnBrushBegin;
    public event Action<Vector3, Quaternion> OnBrushMove;
    public event Action<Vector3, Quaternion> OnBrushEnd;
}
```

**Acceptance**:
- [ ] Touch triggers drawing
- [ ] Brush position matches touch raycast hit
- [ ] Drawing works on AR planes

---

### Task 2.2: Adapt Brush.cs for AR
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Modify Brush.cs to use ARBrushInput instead of XRNode.

**Subtasks**:
- [ ] Copy `Brush.cs` to `Assets/Scripts/Normcore/ARBrush.cs`
- [ ] Replace XRNode tracking with ARBrushInput
- [ ] Replace trigger input with touch state
- [ ] Test local drawing (no network)
- [ ] Test networked drawing

**Changes**:
```csharp
// Before (VR)
XRNode node = _hand == Hand.LeftHand ? XRNode.LeftHand : XRNode.RightHand;
bool handIsTracking = UpdatePose(node, ref _handPosition, ref _handRotation);
bool triggerPressed = Input.GetAxisRaw(trigger) > 0.1f;

// After (AR)
_handPosition = _arBrushInput.BrushPosition;
_handRotation = _arBrushInput.BrushRotation;
bool triggerPressed = _arBrushInput.IsDrawing;
```

**Acceptance**:
- [ ] Drawing works with touch input
- [ ] BrushStroke prefab instantiates correctly
- [ ] Ribbon points sync to network

---

### Task 2.3: Stroke Sync Test
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Verify brush strokes sync between devices.

**Test Procedure**:
```
1. Device A: Draw stroke
2. Device B: Verify stroke appears
3. Measure sync latency
4. Device B: Draw stroke
5. Device A: Verify stroke appears
```

**Subtasks**:
- [ ] Create sync latency measurement tool
- [ ] Test with local network (same WiFi)
- [ ] Test with cellular (if available)
- [ ] Document sync performance

**Metrics**:
| Condition | Target | Actual |
|-----------|--------|--------|
| Same WiFi | <100ms | - |
| Cellular | <300ms | - |

**Acceptance**:
- [ ] Strokes appear on both devices
- [ ] Latency meets targets
- [ ] No missing strokes

---

### Task 2.4: Late Joiner Test
**Priority**: P2 | **Estimate**: 1h | **Status**: Not Started

Verify late joiners receive existing strokes.

**Test Procedure**:
```
1. Device A: Connect and draw 5 strokes
2. Device B: Join room after strokes drawn
3. Verify Device B sees all 5 strokes
```

**Acceptance**:
- [ ] Late joiner receives all existing strokes
- [ ] Stroke order preserved
- [ ] No visual artifacts

---

## Sprint 3: Voice Chat & Polish

### Task 3.1: Setup Voice Chat
**Priority**: P2 | **Estimate**: 2h | **Status**: Not Started

Configure RealtimeAvatarVoice for voice communication.

**Subtasks**:
- [ ] Add `RealtimeAvatarVoice` to avatar prefab
- [ ] Request microphone permission (iOS)
- [ ] Add mute toggle UI
- [ ] Test voice transmission between devices

**iOS Permission** (Info.plist):
```xml
<key>NSMicrophoneUsageDescription</key>
<string>Voice chat with other users</string>
```

**Acceptance**:
- [ ] Microphone permission requested
- [ ] Voice transmits between devices
- [ ] Mute toggle works

---

### Task 3.2: Voice Latency Test
**Priority**: P2 | **Estimate**: 1h | **Status**: Not Started

Measure voice chat latency.

**Test Procedure**:
```
1. Device A: Start talking
2. Device B: Measure time until audio heard
3. Repeat 10 times
4. Calculate average latency
```

**Target**: <200ms one-way latency

**Acceptance**:
- [ ] Latency measured
- [ ] Meets <200ms target
- [ ] Documented in this file

---

### Task 3.3: Brush Color & Width UI
**Priority**: P3 | **Estimate**: 2h | **Status**: Not Started

Add UI for selecting brush color and width.

**Subtasks**:
- [ ] Create color picker UI (6 preset colors)
- [ ] Create width slider UI
- [ ] Store selection in local state
- [ ] Apply to new strokes

**UI Layout**:
```
┌────────────────────────────────┐
│  [R][O][Y][G][B][P]  Width:[▬]│
└────────────────────────────────┘
```

**Acceptance**:
- [ ] Color picker works
- [ ] Width slider works
- [ ] Settings apply to new strokes

---

### Task 3.4: Clear All Strokes
**Priority**: P3 | **Estimate**: 1h | **Status**: Not Started

Add ability to clear all strokes (room owner only).

**Subtasks**:
- [ ] Add "Clear All" button
- [ ] Verify ownership before clearing
- [ ] Destroy all BrushStroke objects
- [ ] Confirm sync to all clients

**Acceptance**:
- [ ] Clear button works
- [ ] All clients see strokes removed
- [ ] Only room owner can clear

---

### Task 3.5: Device Performance Test
**Priority**: P2 | **Estimate**: 2h | **Status**: Not Started

Verify performance meets targets on iOS devices.

**Test Procedure**:
```
1. Connect 2 devices
2. Draw continuously for 5 minutes
3. Monitor FPS (target: 30+)
4. Monitor memory usage
5. Monitor battery drain
```

**Subtasks**:
- [ ] Add FPS counter to debug UI
- [ ] Test on iPhone 12 (baseline)
- [ ] Test on iPhone 15 Pro (target)
- [ ] Document performance metrics

**Target Metrics**:
| Device | FPS | Memory | Battery/Hour |
|--------|-----|--------|--------------|
| iPhone 12 | 30+ | <500MB | <25% |
| iPhone 15 Pro | 60+ | <500MB | <20% |

**Acceptance**:
- [ ] FPS meets targets
- [ ] No memory leaks
- [ ] Battery drain acceptable

---

## Test Checklist Summary

### Sprint 0 (Setup)
- [ ] Task 0.1: Import AR Spectator Package
- [ ] Task 0.2: Configure Normcore Project Settings
- [ ] Task 0.3: Setup AR Foundation Scene

### Sprint 1 (Networking)
- [ ] Task 1.1: Room Connection Test
- [ ] Task 1.2: Multi-Device Room Join Test
- [ ] Task 1.3: Reconnection Test

### Sprint 2 (Drawing)
- [ ] Task 2.1: Create ARBrushInput Component
- [ ] Task 2.2: Adapt Brush.cs for AR
- [ ] Task 2.3: Stroke Sync Test
- [ ] Task 2.4: Late Joiner Test

### Sprint 3 (Voice & Polish)
- [ ] Task 3.1: Setup Voice Chat
- [ ] Task 3.2: Voice Latency Test
- [ ] Task 3.3: Brush Color & Width UI
- [ ] Task 3.4: Clear All Strokes
- [ ] Task 3.5: Device Performance Test

---

## Test Results Log

### Connection Tests
| Date | Device A | Device B | Connection Time | Pass |
|------|----------|----------|-----------------|------|
| - | - | - | - | - |

### Sync Tests
| Date | Stroke Count | Avg Latency | Max Latency | Pass |
|------|--------------|-------------|-------------|------|
| - | - | - | - | - |

### Voice Tests
| Date | Avg Latency | Quality | Pass |
|------|-------------|---------|------|
| - | - | - | - |

### Performance Tests
| Date | Device | FPS | Memory | Battery | Pass |
|------|--------|-----|--------|---------|------|
| - | - | - | - | - | - |

---

*Total Estimated Effort: ~22 hours across 4 sprints*
