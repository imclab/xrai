# Tasks: Cross-Platform Multimodal ML Foundations

**Spec**: [008-crossplatform-multimodal-ml-foundations](./spec.md)
**Architecture**: [FINAL_RECOMMENDATIONS.md](./FINAL_RECOMMENDATIONS.md)
**Master Plan**: [../MASTER_DEVELOPMENT_PLAN.md](../MASTER_DEVELOPMENT_PLAN.md)
**Status**: Architecture Approved

---

## Phase 0: Debug Infrastructure (Sprint 0) ✅

**Goal**: Establish debugging and testing foundation before any new features.
**Status**: COMPLETE (simplified implementation - reuses existing components)

- [x] **D-001**: Create DebugFlags.cs (~180 LOC) ✅
  - `[Conditional("DEBUG_TRACKING")]` attribute wrapper
  - `[Conditional("DEBUG_VOICE")]` attribute wrapper
  - `[Conditional("DEBUG_VFX")]` attribute wrapper
  - `[Conditional("DEBUG_NETWORK")]` attribute wrapper
  - `[Conditional("DEBUG_SYSTEM")]` attribute wrapper
  - Runtime mute flags + Solo() function
  - Production builds: all debug code stripped

- [x] **D-002**: Create DebugConfig.cs ScriptableObject (~100 LOC) ✅
  - LogCategory enum (Tracking, Voice, VFX, Network, System)
  - Category muting flags
  - Editor mock data settings
  - Singleton access pattern
  - `ApplySettings()` to sync with DebugFlags

- [x] **D-003**: Create DebugBootstrap.cs (~90 LOC) ✅
  - Applies DebugConfig on scene start
  - Solo category support
  - Integrates with VFXPipelineDashboard (Tab toggle)
  - **Note**: VFXPipelineDashboard already provides IMGUI overlay

- [x] **D-004**: ARDepthSource built-in mock data ✅
  - **SIMPLIFIED**: ARDepthSource already has `_useMockDataInEditor` flag
  - Creates gradient depth + center blob stencil automatically
  - No external WebcamMockSource needed for basic testing
  - Resolution configurable via `_mockResolution`

- [ ] **D-005**: Integrate ARFoundationRemote fallback (DEFERRED)
  - Not required for initial testing
  - ARDepthSource mock data sufficient for Editor testing

- [x] **D-006**: Create DebugTestScene.unity via menu ✅
  - `H3M > Debug > Create Debug Test Scene`
  - Creates: DebugBootstrap, ARDepthSource, Dashboard, TestHarness, Profiler
  - Saves to `Assets/Scenes/Testing/DebugTestScene.unity`

- [x] **D-007**: Create DebugDefinesSetup.cs Editor tool ✅
  - `H3M > Debug > Setup Debug Defines (Development)`
  - `H3M > Debug > Remove Debug Defines (Production)`
  - `H3M > Debug > Check Debug Defines`
  - `H3M > Debug > Add DebugBootstrap to Scene`
  - `H3M > Debug > Create Debug Test Scene`

### Debug Infrastructure File Structure (Implemented)

```
Assets/Scripts/Debug/
├── DebugFlags.cs           # [Conditional] compile-time logging
├── DebugConfig.cs          # ScriptableObject settings
├── DebugBootstrap.cs       # Scene initialization
└── Editor/
    └── DebugDefinesSetup.cs # Editor menu tools

Assets/Scenes/Testing/
└── DebugTestScene.unity    # Created via menu command
```

### Scripting Define Symbols (Debug Builds)

```
DEBUG_TRACKING;DEBUG_VOICE;DEBUG_VFX;DEBUG_NETWORK;DEBUG_SYSTEM
```

### Reused Components (No Duplication)

| Original Task | Reused Component | Location |
|---------------|------------------|----------|
| DebugOverlay | VFXPipelineDashboard | `Assets/Scripts/VFX/VFXPipelineDashboard.cs` |
| WebcamMockSource | ARDepthSource mock | `Assets/Scripts/Bridges/ARDepthSource.cs:106-168` |
| Log history | PlayerLogDumper | `Assets/Scripts/PlayerLogDumper.cs` |
| VFX shortcuts | VFXTestHarness | `Assets/Scripts/VFX/VFXTestHarness.cs` |
| Performance | VFXProfiler | `Assets/Scripts/Performance/VFXProfiler.cs` |

---

## Phase 1: BodyPix/Sentis Testing & Validation

- [ ] **T-001**: Create BodyPix test scene
  - Instantiate BodyPartSegmenter
  - Display 24-part mask visualization
  - Display 17-keypoint skeleton overlay
  - FPS counter and inference time display

- [ ] **T-002**: Validate segmentation accuracy
  - Test single person (standing, sitting, walking)
  - Test partial occlusion scenarios
  - Test lighting variations
  - Test distance variations (0.5m - 5m)

- [ ] **T-003**: Profile memory and performance
  - Measure GPU memory allocation
  - Monitor for memory leaks (30 min session)
  - Profile inference time (min/avg/max)
  - Test on iPhone 12, 14, 15 Pro

- [ ] **T-004**: VFX integration testing
  - Verify BodyPartMask binds correctly
  - Verify KeypointBuffer binds correctly
  - Test all NNCam2 VFX with keypoints
  - Test segmented position maps (Face, Arms, Hands)

## Phase 2: Abstraction Layer

- [ ] **T-005**: Create ITrackingProvider interface (~100 LOC)
  - Define TrackingCapabilities flags
  - Body: GetBodySegmentation, GetPoseKeypoints
  - Hands: GetHandJoints, GetPinchState
  - Face: GetFaceMesh, GetFaceBlendshapes
  - Events: OnTrackingLost, OnTrackingFound

- [ ] **T-006**: Create TrackingProviderFactory
  - Platform detection (iOS, Android, Quest, WebGL, visionOS)
  - Provider priority/fallback chain
  - Runtime provider switching support

- [ ] **T-007**: Create SentisTrackingProvider (~200 LOC)
  - Wrap existing BodyPartSegmenter
  - Implement ITrackingProvider interface
  - Add capability reporting

- [ ] **T-008**: Create ARKitStencilProvider (~80 LOC)
  - Use AR Foundation AROcclusionManager
  - Binary human segmentation stencil
  - Implement ITrackingProvider interface

- [ ] **T-008b**: Create ARKitBodyTrackingProvider (~200 LOC)
  - Use ARBodyTrackingConfiguration (91 joints)
  - Requires A12+ chip, rear camera only
  - LiDAR enhancement when available
  - Expose full skeleton hierarchy
  - Joint confidence values
  - 3D world-space positions

- [ ] **T-008c**: Create ARKitHandTrackingProvider (~180 LOC)
  - Use Vision VNDetectHumanHandPoseRequest (21 joints)
  - Works with any camera (front or rear)
  - 2D normalized coordinates with confidence
  - Chirality detection (left/right)
  - Up to 2 hands simultaneously
  - Pinch gesture calculation
  - Performance: ~3-5ms on A14+

- [ ] **T-008d**: Create ARKitPoseProvider (~150 LOC)
  - Use Vision VNDetectHumanBodyPoseRequest (17 joints)
  - Works with any camera
  - 2D COCO-format keypoints
  - Multi-person detection support
  - Confidence per joint

- [ ] **T-008e**: Create ARKitFaceProvider (~200 LOC)
  - Use ARFaceTrackingConfiguration
  - 52 blendshapes output
  - 1220-vertex face mesh
  - Eye gaze direction
  - Tongue tracking
  - Requires TrueDepth (front) or iOS 14+ (rear)

- [ ] **T-008f**: Create CompositeTrackingProvider (~150 LOC)
  - Combine multiple providers
  - Capability aggregation
  - Priority-based fallback
  - Runtime provider switching

## Phase 3: Hand Tracking Abstraction

- [ ] **T-009**: Create hand tracking abstraction
  - Common 21-joint format (MediaPipe standard)
  - Conversion from XR Hands (26 joints)
  - Conversion from Meta SDK (26 joints)
  - Conversion from HoloKit (21 joints)

- [ ] **T-010**: Integrate with HandVFXController
  - Replace direct HoloKit calls
  - Use ITrackingProvider.TryGetHandJoints
  - Maintain pinch gesture detection
  - Maintain velocity tracking

- [ ] **T-011**: Create HoloKitTrackingProvider (~100 LOC)
  - Wrap existing HoloKit integration
  - Implement hand joint output
  - Implement gesture events

## Phase 4: Quest/Meta Provider

- [ ] **T-012**: Create MetaTrackingProvider (~200 LOC)
  - Movement SDK body tracking
  - Hand tracking (Hands 2.4)
  - Face tracking (Quest Pro or Audio-to-Expression)

- [ ] **T-013**: Test on Quest 3
  - Body tracking validation
  - Hand tracking validation
  - Performance profiling
  - VFX compatibility testing

## Phase 4b: ARKit Body Tracking (91-joint)

- [ ] **T-013b**: Test ARKit body tracking on iOS
  - Test on iPhone 12 Pro (A14 + LiDAR)
  - Test on iPhone 15 Pro (A17 + LiDAR)
  - Compare 91-joint vs 17-keypoint accuracy
  - Measure performance difference
  - Test occlusion scenarios

- [ ] **T-013c**: Create VFX for 91-joint skeleton
  - Full skeleton visualization VFX
  - Joint-to-joint line trails
  - Per-limb particle effects
  - Compare visual quality vs 17-keypoint

## Phase 5: WebGL/ONNX Integration

- [ ] **T-014**: Research ONNX Runtime Web setup
  - Evaluate WebGPU vs WASM backends
  - Test model loading and inference
  - Measure performance on target browsers
  - Document browser compatibility

- [ ] **T-015**: Create jslib bridge for Unity-JS communication
  - Async inference API
  - Texture/buffer transfer
  - Error handling

- [ ] **T-016**: Create ONNXWebTrackingProvider (~300 LOC)
  - Load models at startup
  - Worker thread inference
  - Implement ITrackingProvider interface
  - Texture transfer to Unity

- [ ] **T-017**: Select/test WebGL models
  - Body: YOLO11-seg nano or Selfie Segmentation
  - Pose: MoveNet Lightning
  - Hands: MediaPipe Hands (via JS)
  - Face: MediaPipe Face Mesh (via JS)

## Phase 6: visionOS Provider

- [ ] **T-018**: Create visionOSTrackingProvider (~150 LOC)
  - ARKit via PolySpatial
  - XR Hands integration
  - Head tracking
  - Implement ITrackingProvider interface

- [ ] **T-019**: Test on Vision Pro
  - Hand tracking validation
  - Head tracking validation
  - Performance profiling (battery, thermal)

## Phase 7: VFX Pipeline Integration

- [ ] **T-020**: Update VFXARBinder for providers
  - Get tracking data from ITrackingProvider
  - Auto-detect available capabilities
  - Graceful fallback when features missing

- [ ] **T-021**: Update VFXCategory for platform features
  - Mark VFX with required capabilities
  - Filter VFX by platform support
  - Show warnings for unsupported VFX

- [ ] **T-022**: Create platform-specific VFX variants
  - Full (24-part + keypoints)
  - Lite (binary mask + pose)
  - Minimal (depth only)

## Phase 8: Multiuser Sync

- [ ] **T-023**: Create TrackingDataSerializer
  - Compact binary format (msgpack or protobuf)
  - Delta encoding for masks
  - Keypoint interpolation support

- [ ] **T-024**: Integrate with WebRTC DataChannel
  - Use existing H3MSignalingClient
  - Add tracking data channel
  - Handle reliability vs latency tradeoff

- [ ] **T-025**: Create RemoteAvatarReconstructor
  - Receive pose keypoints
  - Reconstruct avatar/VFX from data
  - Interpolation for network jitter
  - Extrapolation for packet loss

- [ ] **T-026**: Test multiuser scenarios
  - 2-user P2P
  - 4-user mesh
  - Latency measurement
  - Bandwidth measurement

## Phase 9: Testing & Documentation

- [ ] **T-027**: Create cross-platform test matrix
  - iOS: iPhone 12, 14, 15 Pro
  - Android: Pixel 6, Samsung S23
  - Quest: Quest 3
  - WebGL: Chrome, Safari, Firefox
  - visionOS: Vision Pro

- [ ] **T-028**: Performance benchmarking
  - Inference time per platform
  - FPS with tracking + VFX
  - Memory usage
  - Battery impact (mobile)

- [ ] **T-029**: Documentation
  - Provider implementation guide
  - WebGL deployment guide
  - Multiuser setup guide
  - Platform compatibility matrix

---

## Verification Checklist

### BodyPix/Sentis
- [ ] 24-part segmentation accurate
- [ ] 17 keypoints tracked reliably
- [ ] <5ms inference on iPhone 15 Pro
- [ ] No memory leaks over 30 min
- [ ] All NNCam2 VFX work

### Abstraction Layer
- [ ] ITrackingProvider interface complete
- [ ] All providers implement interface
- [ ] TrackingProviderFactory selects correctly
- [ ] Runtime provider switching works

### ARKit Native Tracking
- [ ] ARBodyTrackingConfiguration initializes (91 joints)
- [ ] 91 joints tracked in 3D world space
- [ ] LiDAR enhancement working on Pro devices
- [ ] Performance <3ms on A14+
- [ ] Graceful fallback on non-LiDAR devices
- [ ] VFX skeleton visualization working

### ARKit Hand Tracking (Vision)
- [ ] VNDetectHumanHandPoseRequest working
- [ ] 21 joints per hand tracked
- [ ] Both hands simultaneously
- [ ] Chirality (left/right) correct
- [ ] Pinch detection working
- [ ] Performance <5ms on A14+
- [ ] Works with front and rear camera

### ARKit 2D Pose (Vision)
- [ ] VNDetectHumanBodyPoseRequest working
- [ ] 17 COCO keypoints tracked
- [ ] Multi-person detection working
- [ ] Confidence values accurate
- [ ] Works with any camera

### ARKit Face Tracking
- [ ] ARFaceTrackingConfiguration working
- [ ] 52 blendshapes output correctly
- [ ] Face mesh (1220 verts) generated
- [ ] Eye gaze direction accurate
- [ ] Tongue tracking working
- [ ] Works on TrueDepth devices

### Cross-Platform
- [ ] Same VFX works on iOS + Android
- [ ] Same VFX works on Quest
- [ ] Same VFX works on WebGL (with fallback)
- [ ] Same VFX works on visionOS

### WebGL
- [ ] ONNX Runtime Web loading <3s
- [ ] Pose inference <30ms
- [ ] Hand tracking via MediaPipe
- [ ] No browser console errors

### Multiuser
- [ ] 2-user latency <50ms
- [ ] 4-user stable at 30 FPS
- [ ] Avatar reconstruction smooth
- [ ] Network interruption recovery

---

## Phase 10: Voice Architecture

- [ ] **T-030**: Create IVoiceProvider interface (~60 LOC)
  - Define VoiceCap flags (STT, TTS, LLM, Streaming, Local)
  - TranscribeAsync, SpeakAsync, AskAsync methods
  - Events: OnTranscriptionStart, OnTranscriptionEnd

- [ ] **T-031**: Create IVoiceConsumer interface (~40 LOC)
  - Required/Optional capabilities
  - OnBind, OnUnbind, OnVoiceData callbacks

- [ ] **T-032**: Create VoiceService orchestrator (~180 LOC)
  - Auto-discover providers via reflection
  - Capability-based routing
  - Hot-swap support

- [ ] **T-033**: Create WhisperProvider (~150 LOC)
  - Local Whisper inference via Sentis/ONNX
  - Streaming transcription
  - iOS/Android optimized

- [ ] **T-034**: Create WebSpeechProvider (~100 LOC)
  - Browser-native Speech API
  - WebGL fallback
  - jslib bridge for Unity

- [ ] **T-035**: Create ElevenLabsProvider (~120 LOC)
  - Cloud TTS with low latency
  - Voice cloning support
  - Streaming audio

- [ ] **T-036**: Create GPT4oVoiceProvider (~150 LOC)
  - Full voice agent (STT + LLM + TTS)
  - Function calling support
  - Conversation context

- [ ] **T-037**: Create MockVoiceProvider (~80 LOC)
  - Simulates all voice capabilities
  - Canned responses for testing
  - Latency simulation

## Phase 11: Testing Infrastructure

- [ ] **T-038**: Create MockTrackingProvider (~150 LOC)
  - Simulates all tracking capabilities
  - Procedural motion (breathing, idle)
  - Gesture simulation (pinch, wave)

- [ ] **T-039**: Create TrackingRecorder (~120 LOC)
  - Record live tracking sessions
  - JSON/binary serialization
  - Metadata (device, timestamp, duration)

- [ ] **T-040**: Create TrackingSimulator (~150 LOC)
  - Playback recorded sessions
  - Inject into MockProvider
  - Speed control (0.5x, 1x, 2x)

- [ ] **T-041**: Create TrackingDebugVisualizer (~100 LOC)
  - Editor Gizmos for skeleton
  - Confidence-based joint sizing
  - Hand joint visualization

- [ ] **T-042**: Unit test suite for tracking
  - Test provider switching
  - Test capability detection
  - Test pinch/gesture detection

- [ ] **T-043**: Unit test suite for voice
  - Test transcription pipeline
  - Test TTS pipeline
  - Test voice command parsing

## Phase 12: LLM Integration (LLMR-Inspired)

- [ ] **T-044**: Create IContextProvider interface (~40 LOC)
  - GetContextSummary() for LLM prompts
  - GetContextDetails() for structured data
  - LLMR SceneAnalyzerGPT pattern

- [ ] **T-045**: Create TrackingContextProvider (~80 LOC)
  - Tracking state → text summary
  - Active capabilities, joint positions, gestures
  - For LLM situational awareness

- [ ] **T-046**: Create VoiceContextProvider (~60 LOC)
  - Voice state → text summary
  - Active providers, last transcription
  - Conversation history

- [ ] **T-047**: Create ContextService (~100 LOC)
  - Aggregate all context providers
  - Format for LLM prompts
  - Token-aware truncation

- [ ] **T-048**: Create ISelfValidator interface (~40 LOC)
  - Validate(output) method
  - FixIfPossible(output, errors) method
  - LLMR InspectorGPT pattern

- [ ] **T-049**: Create SelfRefiningAgent (~120 LOC)
  - Generate → Critique → Improve loop
  - Max iterations configurable
  - Early exit on "correct + complete"

## Phase 13: XR Assistant Integration

- [ ] **T-050**: Create XRService (~80 LOC)
  - Unified access to TrackingService + VoiceService
  - Combined context for LLM
  - Event coordination

- [ ] **T-051**: Create XRAssistant example (~150 LOC)
  - Pinch → Start listening
  - Voice command → Action
  - VFX feedback

- [ ] **T-052**: Create VoiceCommandConsumer (~100 LOC)
  - Parse voice → commands
  - Execute registered actions
  - Confirmation feedback

---

## Dependencies

### Phase 0 (Debug Infrastructure) - No Dependencies
| Task | Depends On | Notes |
|------|------------|-------|
| D-001 | - | Foundation |
| D-002 | D-001 | Uses DebugFlags |
| D-003 | D-002 | Uses DebugLogger |
| D-004 | - | Independent |
| D-005 | D-004 | Extends mock source |
| D-006 | D-001-D-005 | Uses all debug components |
| D-007 | D-002 | Config for DebugLogger |

### Phase 1+ Dependencies
| Task | Depends On | Notes |
|------|------------|-------|
| T-001 | D-006 | Uses EditorTestScene |
| T-005 | D-001 | Uses DebugFlags for logging |
| T-007 | T-005 | Needs interface first |
| T-010 | T-005, T-009 | Needs abstraction |
| T-016 | T-014, T-015 | Needs ONNX + bridge |
| T-020 | T-005 | Needs interface |
| T-024 | T-023 | Needs serialization |
| T-025 | T-023, T-024 | Needs data + transport |
| T-032 | T-030, T-031 | Needs voice interfaces |
| T-033-T-037 | T-032 | Needs VoiceService |
| T-038 | D-004 | Uses WebcamMockSource pattern |
| T-040 | T-038, T-039 | Needs mock + recorder |
| T-044-T-047 | T-005, T-030 | Needs tracking + voice |
| T-049 | T-048 | Needs validator interface |
| T-050 | T-005, T-032 | Needs both services |
| T-051 | T-050 | Needs XRService |

---

## Verification Checklist (Extended)

### Debug Infrastructure (Phase 0)
- [ ] DebugFlags strips all debug code in Release builds
- [ ] DebugLogger filters by category correctly
- [ ] DebugOverlay toggles with `~` key
- [ ] WebcamMockSource provides ColorTexture + DepthTexture
- [ ] ARFoundationRemote detected and integrated when available
- [ ] EditorTestScene loads without errors
- [ ] MCP can interact with EditorTestScene
- [ ] Debug symbols defined in Debug build only

### Voice Architecture
- [ ] IVoiceProvider interface complete
- [ ] VoiceService auto-discovers providers
- [ ] WhisperProvider <200ms latency on iOS
- [ ] WebSpeechProvider works in Chrome/Safari
- [ ] MockVoiceProvider enables testing

### Testing Infrastructure
- [ ] MockTrackingProvider simulates all capabilities
- [ ] TrackingRecorder captures 30 FPS data
- [ ] TrackingSimulator playback matches recording
- [ ] Unit tests pass without hardware
- [ ] CI/CD integration working

### LLM Integration
- [ ] IContextProvider generates useful summaries
- [ ] TrackingContextProvider includes gesture state
- [ ] SelfRefiningAgent improves output quality
- [ ] Context fits within token limits

### MCP Integration
- [ ] MCP can read Unity console
- [ ] MCP can run EditMode tests
- [ ] MCP can take screenshots
- [ ] MCP can query scene hierarchy
- [ ] MCP can modify GameObjects

---

## Task Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| 0: Debug Infrastructure | D-001 to D-007 (7 tasks) | Complete |
| 1: BodyPix Testing | T-001 to T-004 (4 tasks) | Pending |
| 2: Abstraction Layer | T-005 to T-008f (10 tasks) | Pending |
| 3: Hand Tracking | T-009 to T-011 (3 tasks) | Pending |
| 4: Quest/Meta | T-012 to T-013 (2 tasks) | Pending |
| 4b: ARKit 91-joint | T-013b to T-013c (2 tasks) | Pending |
| 5: WebGL/ONNX | T-014 to T-017 (4 tasks) | Pending |
| 6: visionOS | T-018 to T-019 (2 tasks) | Pending |
| 7: VFX Pipeline | T-020 to T-022 (3 tasks) | Pending |
| 8: Multiuser | T-023 to T-026 (4 tasks) | Pending |
| 9: Testing & Docs | T-027 to T-029 (3 tasks) | Pending |
| 10: Voice | T-030 to T-037 (8 tasks) | Pending |
| 11: Testing Infra | T-038 to T-043 (6 tasks) | Pending |
| 12: LLM | T-044 to T-049 (6 tasks) | Pending |
| 13: XR Assistant | T-050 to T-052 (3 tasks) | Pending |
| **Total** | **67 tasks** | |

---

*Created: 2026-01-20*
*Updated: 2026-01-21 - Phase 0 Complete*
