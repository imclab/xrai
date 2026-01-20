# Final Recommendations: Cross-Platform Multimodal ML Foundations

**Date**: 2026-01-20
**Status**: Approved Architecture
**Spec**: [008-crossplatform-multimodal-ml-foundations](./spec.md)

---

## Executive Summary

After comprehensive research into tracking systems, voice AI, industry roadmaps, and XR+AI architectural patterns (LLMR, XR Blocks, CHI 2024/2025), we recommend a **modular, capability-based architecture** that:

1. **Abstracts all inputs** (tracking, voice) behind unified interfaces
2. **Auto-discovers** providers and consumers via attributes
3. **Hot-swaps** components at runtime for debugging/extension
4. **Tests without hardware** via mock providers and recorded sessions
5. **Integrates LLM context** via LLMR-inspired patterns

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          APPLICATION LAYER                                   │
│   VFX, Avatars, Voice Commands, XR Assistants (uses interfaces only)        │
├─────────────────────────────────────────────────────────────────────────────┤
│                          SERVICE LAYER                                       │
│   TrackingService          VoiceService           ContextService            │
│   (orchestrates tracking)  (orchestrates voice)   (LLM context)             │
├─────────────────────────────────────────────────────────────────────────────┤
│                          ABSTRACTION LAYER                                   │
│   ITrackingProvider        IVoiceProvider         IContextProvider          │
│   ITrackingConsumer        IVoiceConsumer         ISelfValidator            │
├─────────────────────────────────────────────────────────────────────────────┤
│                          ADAPTER LAYER                                       │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ TRACKING                                                             │   │
│   │ ARKitBody | ARKitHand | ARKitFace | Meta | Sentis | ONNX | Mock    │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ VOICE                                                                │   │
│   │ Whisper | WebSpeech | ElevenLabs | GPT-4o | LocalLLM | Mock        │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────────┤
│                          PLATFORM LAYER                                      │
│   iOS/ARKit | Android/ARCore | Quest/Meta SDK | WebGL/ONNX | visionOS       │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Recommended Implementation Order

### Phase 1: Core Interfaces (Week 1)

| Priority | Task | LOC | Dependency |
|----------|------|-----|------------|
| P0 | `ITrackingProvider` interface | ~80 | None |
| P0 | `ITrackingConsumer` interface | ~40 | None |
| P0 | `TrackingCap` flags enum | ~30 | None |
| P0 | `TrackingService` orchestrator | ~200 | Above |
| P0 | `TrackingData` structs | ~100 | None |

### Phase 2: Provider Implementations (Week 2-3)

| Priority | Provider | Platform | LOC | Capabilities |
|----------|----------|----------|-----|--------------|
| P0 | `SentisTrackingProvider` | iOS/Android | ~200 | Body24Part, Pose17 |
| P0 | `MockTrackingProvider` | All | ~150 | All (simulated) |
| P1 | `ARKitBodyProvider` | iOS | ~200 | Body91, LiDAR |
| P1 | `ARKitHandProvider` | iOS | ~180 | Hand21, Gestures |
| P1 | `ARKitFaceProvider` | iOS | ~200 | Face52, Mesh, Gaze |
| P1 | `CompositeProvider` | iOS | ~150 | Aggregates above |
| P2 | `MetaTrackingProvider` | Quest | ~200 | Body70, Hand26, Face63 |
| P2 | `ONNXWebProvider` | WebGL | ~300 | Pose17, Hand21 |
| P3 | `visionOSProvider` | visionOS | ~150 | Hand26, Head |

### Phase 3: Consumer Integration (Week 3)

| Priority | Consumer | Purpose | LOC |
|----------|----------|---------|-----|
| P0 | `VFXTrackingConsumer` | Binds tracking to VFX | ~100 |
| P1 | `AvatarTrackingConsumer` | Drives avatar skeleton | ~150 |
| P1 | `NetworkTrackingConsumer` | Serializes for multiuser | ~120 |

### Phase 4: Voice Architecture (Week 4)

| Priority | Task | LOC |
|----------|------|-----|
| P1 | `IVoiceProvider` interface | ~60 |
| P1 | `IVoiceConsumer` interface | ~40 |
| P1 | `VoiceService` orchestrator | ~180 |
| P1 | `WhisperProvider` (local) | ~150 |
| P2 | `WebSpeechProvider` | ~100 |
| P2 | `ElevenLabsProvider` | ~120 |
| P3 | `GPT4oVoiceProvider` | ~150 |

### Phase 5: Testing Infrastructure (Week 4-5)

| Priority | Task | Purpose |
|----------|------|---------|
| P0 | `MockTrackingProvider` | Zero-hardware testing |
| P0 | `TrackingRecorder` | Record live sessions |
| P0 | `TrackingSimulator` | Replay recordings |
| P1 | `MockVoiceProvider` | Voice testing |
| P1 | Unit test suite | Automated validation |

### Phase 6: LLM Integration (Week 5-6)

| Priority | Task | Purpose |
|----------|------|---------|
| P2 | `IContextProvider` | LLMR-style scene context |
| P2 | `TrackingContextProvider` | Tracking state for LLM |
| P2 | `VoiceContextProvider` | Voice state for LLM |
| P3 | `SelfRefiningAgent` | Meta-prompting pattern |

---

## Key Design Decisions

### 1. Capability-Based Routing (Recommended)

```csharp
[Flags]
public enum TrackingCap
{
    None = 0,
    BodySegmentation24Part = 1 << 0,
    BodySkeleton91 = 1 << 1,
    HandTracking21 = 1 << 2,
    HandTracking26 = 1 << 3,
    FaceBlendshapes52 = 1 << 4,
    // ... etc
}
```

**Why**: Runtime detection allows graceful degradation and platform-specific optimization.

### 2. Auto-Discovery via Attributes (Recommended)

```csharp
[TrackingProvider(Priority = 100, Platforms = Platform.iOS)]
public class ARKitBodyProvider : ITrackingProvider { }

[TrackingConsumer(Required = TrackingCap.HandTracking21)]
public class HandVFXController : MonoBehaviour, ITrackingConsumer { }
```

**Why**: Zero configuration, self-documenting, compile-time validation.

### 3. Zero External Dependencies in Core (Recommended)

```csharp
namespace Tracking.Core
{
    // Pure C# - works in Unity, native, WebGL, server
    public struct Vector3f { public float X, Y, Z; }
    public struct JointData { public int Id; public Vector3f Position; public float Confidence; }
}
```

**Why**: Maximum portability, can be shared across platforms and even non-Unity contexts.

### 4. Mock-First Testing (Recommended)

```csharp
[TrackingProvider(Priority = -100)] // Lowest priority
public class MockTrackingProvider : ITrackingProvider
{
    public TrackingCap Capabilities => TrackingCap.All;
    public bool IsAvailable => Application.isEditor || Debug.isDebugBuild;
}
```

**Why**: Test all code paths without hardware. Critical for CI/CD and rapid iteration.

### 5. Hot-Swap at Runtime (Recommended)

```csharp
// Debug keys for runtime switching
if (Input.GetKeyDown(KeyCode.F1)) TrackingService.Instance.ForceProvider<MockTrackingProvider>();
if (Input.GetKeyDown(KeyCode.F2)) TrackingService.Instance.ForceProvider<RecordingPlaybackProvider>();
if (Input.GetKeyDown(KeyCode.F3)) TrackingService.Instance.AutoSelectProvider();
```

**Why**: Debugging without rebuilding. Essential for diagnosing platform-specific issues.

---

## Platform-Specific Recommendations

### iOS (Primary)

| Feature | Recommended Provider | Fallback |
|---------|---------------------|----------|
| Body Segmentation | SentisProvider (24-part) | ARKit Stencil (binary) |
| Body Pose | ARKitBodyProvider (91j) | SentisProvider (17kp) |
| Hands | ARKitHandProvider (21j) | HoloKit SDK |
| Face | ARKitFaceProvider (52bs) | - |

**Strategy**: CompositeProvider aggregates all ARKit + Sentis capabilities.

### Quest 3 (Secondary)

| Feature | Recommended Provider |
|---------|---------------------|
| Body | MetaTrackingProvider (70j) |
| Hands | MetaTrackingProvider (26j) |
| Face | MetaTrackingProvider (63bs) or Audio-to-Expression |

**Strategy**: Single MetaTrackingProvider wraps Movement SDK.

### WebGL (Tertiary)

| Feature | Recommended Provider |
|---------|---------------------|
| Body | ONNXWebProvider (YOLO11-seg nano) |
| Pose | ONNXWebProvider (MoveNet Lightning) |
| Hands | MediaPipe JS via jslib bridge |
| Face | MediaPipe JS via jslib bridge |

**Strategy**: Hybrid ONNX + MediaPipe via JavaScript interop.

### visionOS (Tertiary)

| Feature | Recommended Provider |
|---------|---------------------|
| Hands | visionOSProvider via XR Hands |
| Head | visionOSProvider via PolySpatial |

**Strategy**: Minimal tracking (hands + head) sufficient for most VFX.

---

## Voice Architecture Recommendations

### Provider Priority (iOS)

1. **Whisper Local** - Best latency, privacy, offline
2. **GPT-4o Realtime** - Best quality, requires network
3. **WebSpeech** - Fallback, browser-native

### Provider Priority (WebGL)

1. **WebSpeech API** - Native browser support
2. **GPT-4o Realtime** - Cloud fallback

### Hybrid Architecture (Recommended)

```
On-device STT (Whisper) → Cloud LLM (GPT-4) → On-device TTS (local or streaming)
```

**Target latency**: <200ms turn latency for conversational feel.

---

## Testing Strategy

### 1. Unit Tests (Mock Provider)

```csharp
[Test]
public void TestPinchDetection()
{
    var recording = LoadRecording("pinch_gesture.json");
    var simulator = new TrackingSimulator(recording);
    var detector = new PinchDetector();

    int pinchCount = 0;
    detector.OnPinch += () => pinchCount++;
    simulator.PlayAll(detector);

    Assert.AreEqual(3, pinchCount);
}
```

### 2. Integration Tests (Recorded Sessions)

- Record 10-minute sessions on each device
- Replay in CI for regression testing
- Compare output against baseline

### 3. Performance Tests

| Metric | iOS Target | Quest Target | WebGL Target |
|--------|------------|--------------|--------------|
| Inference | <5ms | <3ms | <30ms |
| FPS | 60 | 72 | 30 |
| Memory | <100MB | <150MB | <200MB |

### 4. Cross-Platform Parity Tests

- Same VFX asset → Visual output comparison
- Automated screenshot comparison
- Joint position variance <5% between platforms

---

## Success Criteria

| ID | Criteria | Metric |
|----|----------|--------|
| SC-01 | Same VFX works on all platforms | 5 platforms verified |
| SC-02 | Provider switch in <1 frame | <16ms hot-swap |
| SC-03 | Zero hardware testing | 100% code coverage via mocks |
| SC-04 | New provider in <1 week | Documented extension guide |
| SC-05 | Voice latency | <200ms turn time |
| SC-06 | LLM context available | IContextProvider working |

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| WebGL performance | Fallback to simpler models, degrade gracefully |
| Meta SDK breaking changes | Abstract behind interface, version pin |
| visionOS PolySpatial limitations | Design for hands-only, head-only modes |
| Voice API costs | Local Whisper first, cloud fallback |
| LLM hallucination | ISelfValidator pattern, rule-based guards |

---

## File Structure

```
Assets/Scripts/
├── Tracking/
│   ├── Core/
│   │   ├── ITrackingProvider.cs
│   │   ├── ITrackingConsumer.cs
│   │   ├── TrackingService.cs
│   │   ├── TrackingCap.cs
│   │   └── TrackingData.cs
│   ├── Providers/
│   │   ├── SentisTrackingProvider.cs
│   │   ├── ARKitBodyProvider.cs
│   │   ├── ARKitHandProvider.cs
│   │   ├── ARKitFaceProvider.cs
│   │   ├── CompositeProvider.cs
│   │   ├── MetaTrackingProvider.cs
│   │   ├── ONNXWebProvider.cs
│   │   ├── visionOSProvider.cs
│   │   └── MockTrackingProvider.cs
│   ├── Consumers/
│   │   ├── VFXTrackingConsumer.cs
│   │   ├── AvatarTrackingConsumer.cs
│   │   └── NetworkTrackingConsumer.cs
│   └── Testing/
│       ├── TrackingRecorder.cs
│       ├── TrackingSimulator.cs
│       └── MockDataGenerator.cs
├── Voice/
│   ├── Core/
│   │   ├── IVoiceProvider.cs
│   │   ├── IVoiceConsumer.cs
│   │   ├── VoiceService.cs
│   │   └── VoiceCap.cs
│   ├── Providers/
│   │   ├── WhisperProvider.cs
│   │   ├── WebSpeechProvider.cs
│   │   ├── ElevenLabsProvider.cs
│   │   ├── GPT4oVoiceProvider.cs
│   │   └── MockVoiceProvider.cs
│   └── Consumers/
│       ├── VoiceCommandConsumer.cs
│       └── ConversationalAgent.cs
├── Context/
│   ├── IContextProvider.cs
│   ├── TrackingContextProvider.cs
│   ├── VoiceContextProvider.cs
│   └── ContextService.cs
└── XR/
    └── XRService.cs  # Unified Tracking + Voice access
```

---

## Related Documents

- [spec.md](./spec.md) - Feature specification
- [tasks.md](./tasks.md) - Implementation tasks
- [MODULAR_TRACKING_ARCHITECTURE.md](./MODULAR_TRACKING_ARCHITECTURE.md) - Detailed architecture & code
- [TRACKING_SYSTEMS_DEEP_DIVE.md](./TRACKING_SYSTEMS_DEEP_DIVE.md) - Platform comparison
- [KnowledgeBase/_LLMR_XR_AI_ARCHITECTURE_PATTERNS.md](../../KnowledgeBase/_LLMR_XR_AI_ARCHITECTURE_PATTERNS.md) - LLM integration patterns
- [KnowledgeBase/_XR_AI_INDUSTRY_ROADMAP_2025-2027.md](../../KnowledgeBase/_XR_AI_INDUSTRY_ROADMAP_2025-2027.md) - Industry roadmap

---

## Approval

| Role | Name | Date | Status |
|------|------|------|--------|
| Architect | Claude Code | 2026-01-20 | ✅ Proposed |
| Lead | - | - | Pending |
| Review | - | - | Pending |

---

*This document represents the final architectural recommendations based on comprehensive research of tracking systems, voice AI, industry roadmaps (Unity, Meta, Google, OpenAI, xAI), patent trends, and XR+AI patterns (LLMR, XR Blocks, CHI 2024/2025).*
