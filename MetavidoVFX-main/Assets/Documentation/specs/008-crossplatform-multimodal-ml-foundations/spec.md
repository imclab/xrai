# Feature Specification: Cross-Platform Multimodal ML Foundations

**Feature Branch**: `008-crossplatform-multimodal-ml-foundations`
**Created**: 2026-01-20
**Updated**: 2026-01-22
**Status**: In Progress (Core providers implemented)
**Scope**: Unified tracking + voice + LLM integration for iOS, Android, Quest, WebGL, visionOS
**Implementation**:
- ✅ ITrackingProvider interface (~120 LOC)
- ✅ TrackingManager orchestrator (~150 LOC)
- ✅ TrackingData structs (BodyPoseData, HandData, FaceData, DepthData, SegmentationData, AudioData)
- ✅ ARKitDepthProvider - Native depth/stencil (~100 LOC)
- ✅ ARKitBodyProvider - Native 91-joint body tracking (~200 LOC) **NEW**
- ✅ SentisTrackingProvider - 24-part segmentation (~150 LOC) **NEW**
- ✅ AudioProvider - FFT audio bands (~100 LOC)
- ✅ MockTrackingProvider - Editor testing with keyboard controls (~280 LOC) **NEW**
- ✅ Hand tracking via spec 012 (5 providers: HoloKit, XRHands, MediaPipe, BodyPix, Touch)
- ⬜ ARKitFaceProvider - Native 52 blendshapes (pending)
- ⬜ MediaPipeTrackingProvider - General fallback (pending)
**Priority**: Native AR Foundation first, ML second (see FINAL_RECOMMENDATIONS.md)

## Related Documents

| Document | Purpose |
|----------|---------|
| [FINAL_RECOMMENDATIONS.md](./FINAL_RECOMMENDATIONS.md) | **Architecture decisions & implementation order** |
| [MODULAR_TRACKING_ARCHITECTURE.md](./MODULAR_TRACKING_ARCHITECTURE.md) | Detailed interfaces, providers, consumers, code |
| [TRACKING_SYSTEMS_DEEP_DIVE.md](./TRACKING_SYSTEMS_DEEP_DIVE.md) | Platform comparison & research |
| [tasks.md](./tasks.md) | Implementation tasks & verification |

## Triple Verification (2026-01-21)

| Source | Status | Notes |
|--------|--------|-------|
| MetavidoVFX BodyPartSegmenter | Verified | 24-part + 17-keypoint working on iOS |
| Unity Sentis Docs | Verified | WebGL limited to GPUPixel (slow) |
| MediaPipe Unity Plugin | Verified | homuler/MediaPipeUnityPlugin active |
| Meta Movement SDK | Verified | Native hand/body tracking on Quest |
| visionOS PolySpatial | Verified | XR Hands via ARKit |
| ONNX Runtime Web | Verified | WebGL-compatible inference |
| AR Foundation | Verified | Native body/hand/face tracking preferred |

## Executive Summary

### The Problem

Current MetavidoVFX uses BodyPix via Sentis for 24-part body segmentation. This works excellently on iOS/Android but:
- **WebGL**: Sentis GPUCompute not supported; GPUPixel has memory leaks
- **Quest**: No Sentis support; Meta provides native tracking SDK
- **visionOS**: ARKit via PolySpatial; XR Hands package
- **Multiuser**: Each device runs its own inference; need shared state

### Recommended Architecture

**Abstraction Layer + Platform-Specific Backends**:

```
┌─────────────────────────────────────────────────────────────────┐
│                 ITrackingProvider Interface                      │
│  GetBodySegmentation(), GetPoseKeypoints(), GetHandJoints()     │
│  GetFaceMesh(), GetFaceBlendshapes()                            │
└─────────────────────────────────────────────────────────────────┘
        │           │           │           │           │
        ▼           ▼           ▼           ▼           ▼
┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐
│ ARKit   │ │ ARCore  │ │ Meta    │ │ ONNX    │ │ MediaPipe│
│Provider │ │Provider │ │Provider │ │Provider │ │Provider │
└─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘
   iOS        Android     Quest      WebGL      Fallback
  visionOS               Quest Pro
```

## Platform Compatibility Matrix

### Body Segmentation

| Platform | Best Option | Fallback | Performance | Notes |
|----------|-------------|----------|-------------|-------|
| **iOS** | BodyPixSentis | ARKit Stencil | 4.8ms (24-part) | Production ready |
| **Android** | BodyPixSentis | ARCore Stencil | 5-8ms | Tested on Pixel 6+ |
| **Quest 3** | Meta Movement SDK | ONNX Runtime | ~3ms (native) | Body tracking API |
| **WebGL** | ONNX Runtime Web | MediaPipe | 10-30ms (WASM) | No GPU compute |
| **visionOS** | ARKit via XR | BodyPixSentis | ~2ms (native) | PolySpatial required |

### Hand Tracking

| Platform | Best Option | Joints | Pinch Detection | Notes |
|----------|-------------|--------|-----------------|-------|
| **iOS** | HoloKit + ARKit | 21 | Yes (native) | Our current impl |
| **iOS** | XR Hands | 26 | Yes | Fallback |
| **Android** | XR Hands | 26 | Yes | AR Foundation |
| **Quest** | Meta Hand SDK | 26 | Yes (native) | Hands 2.4 |
| **WebGL** | MediaPipe Hands | 21 | Manual | Via JS bridge |
| **visionOS** | XR Hands | 26 | Yes | ARKit backend |

### Face Tracking

| Platform | Best Option | Blendshapes | Mesh | Notes |
|----------|-------------|-------------|------|-------|
| **iOS** | ARKit Face | 52 | Yes | TrueDepth camera |
| **Android** | ARCore Face | 52 | Yes | ML-based |
| **Quest Pro** | Meta Face SDK | 63 | Yes | Eye + Face |
| **Quest 3** | Audio-to-Expression | ~20 | No | Inferred from audio |
| **WebGL** | MediaPipe Face | 478 landmarks | Yes | Via JS bridge |
| **visionOS** | ARKit via XR | 52 | Yes | PolySpatial |

### Pose Estimation

| Platform | Best Option | Keypoints | 3D | Notes |
|----------|-------------|-----------|-----|-------|
| **iOS** | ARKit Body | 91 | Yes | **Native, LiDAR-enhanced, rear camera only** |
| **iOS** | BodyPixSentis | 17 | Via depth | Our current impl (any camera) |
| **Android** | BodyPixSentis | 17 | Via depth | |
| **Quest** | Meta Body SDK | 70 | Yes | Full skeleton |
| **WebGL** | ONNX + YOLO | 17 | 2D only | Or MediaPipe |
| **visionOS** | ARKit Body | 91 | Yes | PolySpatial |

### ARKit Native Tracking Capabilities (iOS)

#### ARKit Body Tracking (91 joints)

**Configuration**: `ARBodyTrackingConfiguration`
**Requirements**: A12+ chip (iPhone XS+), rear camera only
**Enhancement**: LiDAR provides significantly better Z-axis accuracy

**Joint Hierarchy** (91 total):
- Root (hips) → Spine chain (7 joints)
- Left/Right leg chains (6 joints each)
- Left/Right arm chains (9 joints each)
- Head/neck chain (6 joints)
- Left/Right hand chains (detailed finger joints)

**Limitations**:
- Single person only
- Rear camera only (not selfie camera)
- Requires good lighting
- Person must be mostly visible

**When to Use**:
- Full-body AR experiences (dance, fitness, motion capture)
- LiDAR-equipped devices for best results
- When rear camera is acceptable

---

#### ARKit Hand Tracking (21 joints per hand via Vision)

**Framework**: Vision (`VNDetectHumanHandPoseRequest`)
**Requirements**: iOS 14+, any camera
**Unity Access**: Via HoloKit SDK or custom Vision bridge

**Joint Hierarchy (21 per hand)**:
```
Wrist (1)
├── Thumb: CMC → MCP → IP → Tip (4)
├── Index: MCP → PIP → DIP → Tip (4)
├── Middle: MCP → PIP → DIP → Tip (4)
├── Ring: MCP → PIP → DIP → Tip (4)
└── Pinky: MCP → PIP → DIP → Tip (4)
```

**Output Data**:
- 2D normalized coordinates (0-1)
- Confidence per joint (0-1)
- Chirality (left/right hand detection)
- Up to 2 hands simultaneously

**Performance**: ~3-5ms on A14+, ~8ms on A12

**Pinch Detection**:
- Thumb tip to Index tip distance
- Native gesture recognition available

---

#### ARKit 2D Pose Detection (17 joints via Vision)

**Framework**: Vision (`VNDetectHumanBodyPoseRequest`)
**Requirements**: iOS 14+, any camera
**Difference from Body Tracking**: 2D only, works with any camera

**Joint Set (17 - COCO format)**:
```
0: Nose
1-2: Left/Right Eye
3-4: Left/Right Ear
5-6: Left/Right Shoulder
7-8: Left/Right Elbow
9-10: Left/Right Wrist
11-12: Left/Right Hip
13-14: Left/Right Knee
15-16: Left/Right Ankle
```

**Output Data**:
- 2D normalized coordinates
- Confidence per joint
- Multiple people supported (unlike 3D body tracking)

**When to Use**:
- Front camera / selfie mode
- Multiple people detection
- When 2D pose is sufficient

---

#### ARKit Face Tracking (52 blendshapes + mesh)

**Configuration**: `ARFaceTrackingConfiguration`
**Requirements**: TrueDepth camera (iPhone X+) for front, or rear camera (iOS 14+)
**Unity Access**: `ARFaceManager` in AR Foundation

**Capabilities**:
- 52 ARKit blendshapes (eye, mouth, brow movements)
- 1220-vertex face mesh
- Face anchor with position/rotation
- Eye gaze direction (left/right)
- Tongue detection

**Blendshape Categories**:
- Eye: `eyeBlinkLeft/Right`, `eyeLookUp/Down/In/Out`, `eyeSquint`, `eyeWide`
- Brow: `browDownLeft/Right`, `browInnerUp`, `browOuterUp`
- Mouth: `mouthSmile`, `mouthFrown`, `jawOpen`, `mouthPucker`, etc.
- Cheek: `cheekPuff`, `cheekSquint`
- Nose: `noseSneer`

---

#### Comparison: ARKit Native vs BodyPix ML

| Feature | ARKit Native | BodyPix (Sentis) |
|---------|--------------|------------------|
| **Body Pose** | 91 joints, 3D, rear only | 17 joints, 2D+depth, any camera |
| **Hand Tracking** | 21 joints, 2D, any camera | Wrist only (keypoint 9-10) |
| **Face** | 52 blendshapes + mesh | Not supported |
| **Segmentation** | Binary stencil only | 24-part body segmentation |
| **Multi-person** | No (body), Yes (2D pose) | No |
| **Performance** | <3ms native | ~5ms GPU inference |
| **Camera** | Rear (body), Any (hands/face) | Any |

## Current Implementation (MetavidoVFX)

### Hand Tracking Comparison

| System | File | Joints | Gestures | Performance |
|--------|------|--------|----------|-------------|
| **HoloKit** | `HandVFXController.cs` | 21 | Pinch, Five | ~1ms native |
| **XR Hands** | `ARKitHandTracking.cs` | 26 | Pinch | ~2ms |
| **BodyPix Fallback** | `HandVFXController.cs:177` | 2 (wrists) | None | ~0.5ms |

### BodyPix Integration

**File**: `Assets/Scripts/Segmentation/BodyPartSegmenter.cs` (350 LOC)

```
Inputs:
  - Camera frame (512x384)
  - Model: MobileNetV1-x050-stride16 (ONNX)

Outputs:
  - MaskTexture (24-part body segmentation)
  - KeypointBuffer (17 pose landmarks)
  - SegmentedPositionMaps (Face, Arms, Hands, Torso, Legs)

Performance:
  - iOS (A17): ~4.8ms GPU inference
  - Editor: ~16ms CPU fallback
```

### VFX Pipeline Integration

Our Hybrid Bridge Pattern (O(1) compute) already supports multiple tracking sources:

```csharp
// ARDepthSource binds AR Foundation data
// BodyPartSegmenter outputs can be added as additional source
// VFXARBinder auto-detects which properties VFX needs
```

## WebGL Strategy

### Challenge

Unity Sentis requires GPU Compute shaders, which WebGL doesn't support. Options:

| Approach | Pros | Cons | Recommended |
|----------|------|------|-------------|
| **ONNX Runtime Web** | Full model support, good perf | Requires JS interop | ✅ Best |
| **MediaPipe JS** | Well-tested, hand/face/pose | Limited customization | ✅ Good fallback |
| **Sentis GPUPixel** | Native Unity | Memory leaks, slow | ❌ Avoid |
| **Server-side** | Any model, fast | Latency, cost | ⚠️ For complex models |
| **TensorFlow.js** | Large ecosystem | Heavy, slow startup | ⚠️ If needed |

### Recommended WebGL Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Unity WebGL Build                             │
│  C# ←→ jslib bridge ←→ JavaScript Worker Thread                 │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│              ONNX Runtime Web (WebGPU/WebGL backend)            │
│  - Body segmentation model (YOLO11-seg or MobileNet)           │
│  - Pose estimation model (MoveNet or BlazePose)                 │
│  - Hand tracking via MediaPipe Hands                            │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│              Texture/Buffer Transfer to Unity                    │
│  - SharedArrayBuffer for low-latency transfer                   │
│  - WebGL texture binding for segmentation masks                 │
└─────────────────────────────────────────────────────────────────┘
```

### Performance Expectations (WebGL)

| Model | WebGL (WASM) | WebGPU | Target |
|-------|--------------|--------|--------|
| Pose (17kp) | 15-25ms | 8-12ms | 30 FPS |
| Hands (21j) | 10-15ms | 5-8ms | 60 FPS |
| Body Seg | 20-40ms | 12-20ms | 30 FPS |
| Face (478) | 12-18ms | 6-10ms | 60 FPS |

## Multiuser Scalability

### Architecture for Real-Time Collaboration

```
┌─────────────────────────────────────────────────────────────────┐
│                    User Device (Local Inference)                 │
│  ITrackingProvider → Local VFX rendering                        │
│  Pose/Hand/Face data (compressed) → Network                     │
└─────────────────────────────────────────────────────────────────┘
                              ↓ WebRTC DataChannel
┌─────────────────────────────────────────────────────────────────┐
│                    WebRTC Mesh / SFU                            │
│  Signaling server (peer discovery)                              │
│  Optional: SFU for 4+ users                                     │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    Remote User Device                            │
│  Receive pose data → Reconstruct avatar/VFX                     │
│  No remote inference needed (data-only transfer)                │
└─────────────────────────────────────────────────────────────────┘
```

### Data Transfer Format (Per Frame)

| Data Type | Size | Frequency | Notes |
|-----------|------|-----------|-------|
| Pose (17kp) | ~200 bytes | 30 Hz | x,y,z,confidence per joint |
| Hands (2×21j) | ~500 bytes | 60 Hz | Both hands |
| Face (52 bs) | ~210 bytes | 30 Hz | Blendshape weights |
| Body Seg Mask | Compressed | 10 Hz | Delta encoding |
| Total | ~1-2 KB/frame | | ~50-100 KB/s per user |

### Scaling Considerations

| Users | Topology | Bandwidth/User | Latency |
|-------|----------|----------------|---------|
| 2 | P2P Mesh | ~100 KB/s | <50ms |
| 3-4 | P2P Mesh | ~300 KB/s | <50ms |
| 5-10 | SFU | ~100 KB/s | <100ms |
| 10+ | SFU + Sharding | ~100 KB/s | <150ms |

## Recommended Implementation Phases

### Phase 1: Abstraction Layer (P1)

Create `ITrackingProvider` interface:

```csharp
public interface ITrackingProvider
{
    // Capabilities
    TrackingCapabilities Capabilities { get; }

    // Provider info
    string ProviderName { get; }
    int PoseKeypointCount { get; }  // 17 (BodyPix), 91 (ARKit), 70 (Meta)

    // Body Segmentation (24-part mask)
    bool TryGetBodySegmentation(out Texture2D mask, out int partCount);

    // Pose/Skeleton (variable joint count)
    bool TryGetPoseKeypoints(out NativeArray<Keypoint> keypoints);
    bool TryGetBodySkeleton(out NativeArray<JointPose> skeleton);  // For 91-joint ARKit

    // Hands (21-26 joints per hand)
    bool TryGetHandJoints(Handedness hand, out NativeArray<JointPose> joints);
    bool TryGetPinchState(Handedness hand, out float distance, out bool isPinching);

    // Face
    bool TryGetFaceMesh(out Mesh mesh);
    bool TryGetFaceBlendshapes(out NativeArray<float> weights);
}

[Flags]
public enum TrackingCapabilities
{
    None = 0,

    // Body Segmentation
    BodySegmentation24Part = 1 << 0,   // 24-part mask (BodyPix)
    BodySegmentationBinary = 1 << 1,   // Binary person mask (ARKit stencil)

    // Body Pose
    PoseKeypoints17 = 1 << 2,          // 17-joint 2D pose (Vision/BodyPix)
    BodySkeleton91 = 1 << 3,           // 91-joint 3D skeleton (ARKit)
    BodySkeleton70 = 1 << 4,           // 70-joint skeleton (Meta)
    MultiPersonPose = 1 << 5,          // Multiple people (Vision 2D only)

    // Hand Tracking
    HandTracking21 = 1 << 6,           // 21-joint hands (Vision/MediaPipe)
    HandTracking26 = 1 << 7,           // 26-joint hands (XR Hands/Meta)
    HandGestures = 1 << 8,             // Native gesture recognition

    // Face Tracking
    FaceMesh = 1 << 9,                 // Face mesh geometry (1220 verts ARKit)
    FaceBlendshapes52 = 1 << 10,       // 52 ARKit blendshapes
    FaceBlendshapes63 = 1 << 11,       // 63 Meta blendshapes
    EyeGaze = 1 << 12,                 // Eye gaze direction
    TongueTracking = 1 << 13,          // Tongue detection

    // Hardware
    LiDAREnhanced = 1 << 14,           // LiDAR depth enhancement
    RearCameraOnly = 1 << 15,          // Rear camera requirement
    TrueDepthRequired = 1 << 16,       // TrueDepth camera (face)
}
```

### Phase 2: Platform Providers (P1)

| Provider | Platform | Backend | Capabilities |
|----------|----------|---------|--------------|
| `ARKitBodyTrackingProvider` | iOS, visionOS | ARKit Body (91j) | BodySkeleton91, LiDAREnhanced, RearCameraOnly |
| `ARKitHandTrackingProvider` | iOS, visionOS | Vision Hands (21j) | HandTracking21, HandGestures |
| `ARKitPoseProvider` | iOS, visionOS | Vision Pose (17j) | PoseKeypoints17, MultiPersonPose |
| `ARKitFaceProvider` | iOS, visionOS | ARKit Face | FaceMesh, FaceBlendshapes52, EyeGaze, TongueTracking, TrueDepthRequired |
| `ARKitStencilProvider` | iOS, visionOS | AROcclusion | BodySegmentationBinary |
| `ARCoreTrackingProvider` | Android | ARCore native | HandTracking26, FaceBlendshapes52 |
| `MetaTrackingProvider` | Quest | Meta Movement SDK | BodySkeleton70, HandTracking26, FaceBlendshapes63, EyeGaze |
| `SentisTrackingProvider` | iOS, Android | BodyPixSentis | BodySegmentation24Part, PoseKeypoints17 |
| `ONNXWebTrackingProvider` | WebGL | ONNX Runtime Web | PoseKeypoints17, HandTracking21 |
| `MediaPipeTrackingProvider` | All | MediaPipe fallback | HandTracking21, PoseKeypoints17, FaceMesh |

**Composite Provider Strategy (iOS)**:
```csharp
// Combine multiple ARKit providers for full capability
var composite = new CompositeTrackingProvider(
    new ARKitBodyTrackingProvider(),   // 91-joint body (rear)
    new ARKitHandTrackingProvider(),   // 21-joint hands (any)
    new ARKitPoseProvider(),           // 17-joint 2D pose (any, multi-person)
    new ARKitFaceProvider(),           // Face mesh + blendshapes
    new SentisTrackingProvider()       // 24-part segmentation (any)
);
```

### Phase 3: VFX Pipeline Integration (P1)

Modify `VFXARBinder` to use `ITrackingProvider`:

```csharp
// Instead of hardcoded ARDepthSource
ITrackingProvider provider = TrackingProviderFactory.GetProvider();

if (provider.TryGetBodySegmentation(out var mask, out var parts))
{
    vfx.SetTexture("BodyPartMask", mask);
}
```

### Phase 4: WebGL ONNX Integration (P2)

1. Create `.jslib` bridge for ONNX Runtime Web
2. Implement `ONNXWebTrackingProvider`
3. Worker thread for non-blocking inference
4. Texture transfer via WebGL bindings

### Phase 5: Multiuser Sync (P2)

1. Serialize tracking data (protobuf or msgpack)
2. WebRTC DataChannel transport
3. Remote avatar reconstruction from pose data
4. Interpolation for network jitter

## Testing Requirements

### BodyPix/Sentis Validation

- [ ] 24-part segmentation accuracy on iOS
- [ ] Keypoint tracking reliability (17 joints)
- [ ] Performance: <5ms inference time
- [ ] Memory: No leaks over 30 min session
- [ ] Edge cases: Partial occlusion, multiple people

### Cross-Platform Parity

- [ ] Same VFX works on all platforms
- [ ] Graceful degradation when features unavailable
- [ ] Consistent joint naming across providers
- [ ] Performance within 2x of native on each platform

### Multiuser Testing

- [ ] 2-user P2P: <50ms end-to-end latency
- [ ] 4-user mesh: Stable at 30 FPS
- [ ] Network interruption recovery
- [ ] Avatar reconstruction accuracy

## Open Questions

1. **WebGPU Timeline**: When will Unity WebGL support WebGPU for faster inference?
2. **Quest Body Tracking**: Is 70-joint skeleton overkill vs 17-keypoint pose?
3. **visionOS Permissions**: What user prompts required for body tracking?
4. **MediaPipe Licensing**: GPL concerns for commercial use?

## Success Criteria

- **SC-001**: Same VFX asset works on iOS + Android + Quest + WebGL
- **SC-002**: WebGL inference <30ms for pose (30 FPS target)
- **SC-003**: Multiuser latency <100ms for 4 users
- **SC-004**: Provider abstraction allows new platforms in <1 week
- **SC-005**: 24-part segmentation available on iOS/Android
- **SC-006**: Hand tracking on all platforms (fallback to wrist-only on WebGL)

---

## Appendix A: Model Recommendations

### Body Segmentation

| Model | Size | Inference | Parts | Best For |
|-------|------|-----------|-------|----------|
| **BodyPix MobileNetV1** | 7MB | 4-8ms | 24 | iOS/Android (current) |
| **YOLO11-seg nano** | 3MB | 10-15ms | Instance | WebGL |
| **Selfie Segmentation** | 2MB | 3-5ms | 1 (person) | Lightweight |

### Pose Estimation

| Model | Size | Inference | Keypoints | Best For |
|-------|------|-----------|-----------|----------|
| **MoveNet Lightning** | 2MB | 8-12ms | 17 | Mobile/WebGL |
| **MoveNet Thunder** | 6MB | 15-20ms | 17 | Accuracy |
| **BlazePose Full** | 5MB | 12-18ms | 33 | 3D pose |

### Hand Tracking

| Model | Size | Inference | Joints | Best For |
|-------|------|-----------|--------|----------|
| **MediaPipe Hands** | 3MB | 8-12ms | 21 | All platforms |
| **Meta Hand SDK** | Native | <3ms | 26 | Quest |
| **ARKit Hands** | Native | <2ms | 26 | iOS |

---

## Appendix B: References

**Sources**:
- [Unity Sentis Documentation](https://docs.unity3d.com/Packages/com.unity.sentis@2.1/manual/index.html)
- [MediaPipe Unity Plugin](https://github.com/homuler/MediaPipeUnityPlugin)
- [Meta Movement SDK](https://developers.meta.com/horizon/documentation/unity/move-overview/)
- [ONNX Runtime Web](https://onnxruntime.ai/docs/tutorials/web/)
- [visionOS PolySpatial Input](https://docs.unity3d.com/Packages/com.unity.polyspatial.visionos@2.2/manual/Input.html)
- [Barracuda PoseNet WebGL Tutorial](https://christianjmills.com/posts/barracuda-posenet-tutorial-v2/webgl/)

---

*Created: 2026-01-20*
*Author: Claude Code + User*
