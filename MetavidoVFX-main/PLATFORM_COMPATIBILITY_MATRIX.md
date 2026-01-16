# Platform Compatibility Matrix - Unity XR/AR/VFX Patterns

**Last Updated**: 2025-11-02
**Purpose**: Comprehensive platform compatibility reference for all Unity-XR-AI patterns

**Platform Priority** (user preference): iOS > Android > Quest > WebGL > Vision Pro

---

## ✅ Master Compatibility Table

| Pattern | iOS | Android | Quest 3 | Quest Pro | WebGL | Vision Pro | Notes |
|---------|-----|---------|---------|-----------|-------|------------|-------|
| **KeijiroAudioVFX** | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | **NOT WebGL** - AudioListener.GetSpectrumData() unsupported in browsers; works on native platforms |
| **WebGLOptimizer** | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | **WebGL-only** - Browser-specific optimizations |
| **WebGLAdvanced** | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | **WebGL-only** - JS bridge, WASM pooling |
| **FaceTrackingVFX** | ✅ | ❌ | ❌ | ⚠️ | ❌ | ✅ | **iOS/visionOS** - ARKit face mesh; Quest Pro uses OVRFaceExpressions (different API) |
| **BodyTracking91Joints** | ✅ | ❌ | ⚠️ | ⚠️ | ❌ | ✅ | **iOS/visionOS** - ARKit 91 joints; Quest has 70 joints (18 core + 52 hand via Movement SDK) |
| **HumanDepthVFX** | ✅ | ❌ | ⚠️ | ⚠️ | ❌ | ✅ | **iOS/visionOS** - ARKit segmentation; Quest 3 has depth API but different implementation |
| **UniversalBuildPipeline** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | **Platform-agnostic** - Build automation for all platforms |
| **ParticleSystem (DOTS)** | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | **WebGL severely limited** - Single-threaded, 10x slower, minutes-long loads; use traditional particles instead |
| **HandTrackingPaint** | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | **iOS/Quest/visionOS** - iOS uses ARKit, Quest uses Hand Tracking API (26 joints per hand) |

**Legend**:
- ✅ Full support
- ⚠️ Partial support (different API or limited features)
- ❌ Not supported

---

## 📊 Platform-Specific Details

### iOS (iPhone, iPad)

**Strengths**:
- ✅ Full ARKit support (face, body, depth, LiDAR)
- ✅ 91-joint body tracking (including 48 finger joints - 24 per hand)
- ✅ Human segmentation with depth/stencil textures
- ✅ TrueDepth camera for face tracking
- ✅ LiDAR on Pro models (depth scanning)

**Supported Patterns**:
- ✅ All audio reactive patterns (KeijiroAudioVFX)
- ✅ All AR Foundation patterns (Face, Body, Depth)
- ✅ DOTS million-particle systems
- ✅ Hand tracking with ARKit

**Limitations**:
- ❌ No WebGL-specific features
- ❌ Requires A12+ chip for body tracking

**Device Requirements**:
- Face tracking: iPhone X+ (TrueDepth camera)
- Body tracking: iPhone XS+ (A12 Bionic+)
- LiDAR: iPhone 12 Pro+, iPad Pro 2020+

---

### Android

**Strengths**:
- ✅ ARCore basic support (planes, images, world tracking)
- ✅ DOTS/ECS support
- ✅ Audio reactive patterns

**Supported Patterns**:
- ✅ KeijiroAudioVFX (audio reactive)
- ✅ UniversalBuildPipeline
- ✅ ParticleSystem (DOTS)

**Limitations**:
- ❌ No face mesh (ARCore has basic face detection only, no mesh)
- ❌ No full body tracking (ARCore has 2D pose estimation only)
- ❌ No human segmentation depth
- ⚠️ Limited depth support (few devices have ToF sensors)

**Workarounds**:
- Use MediaPipe for face/body tracking (ML-based, no hardware required)
- Use ML models via Unity Sentis for segmentation

---

### Meta Quest 3

**Strengths**:
- ✅ Full body tracking (70 total joints: 18 core + 52 hand) via Movement SDK
- ✅ Inside-Out Body Tracking (IOBT) + Generative Legs
- ✅ Hand tracking (26 joints per hand) integrated with body tracking
- ✅ Depth API for environment
- ✅ DOTS/ECS support

**Supported Patterns**:
- ✅ KeijiroAudioVFX
- ✅ HandTrackingPaint (with Quest Hand Tracking API)
- ✅ ParticleSystem (DOTS) - Quest 3 can hit 90fps with 1M particles
- ✅ UniversalBuildPipeline

**Limitations**:
- ❌ **NO face tracking** (no facial cameras)
- ⚠️ Body tracking is 70 joints, NOT 91 like ARKit (different joint structure)
- ⚠️ Human depth different API than ARKit (environment depth, not human segmentation)

**Workarounds**:
- Face tracking: Upgrade to Quest Pro
- 91-joint equivalent: Quest has 70 joints (18 core + 52 hand) vs ARKit's 91 (19 core + 48 hand + 24 head)
- Human depth: Use Scene API depth data instead of human-specific segmentation

**Implementation**:
- Repo: [oculus-samples/Unity-Movement](https://github.com/oculus-samples/Unity-Movement)
- SDK: Movement SDK v78+, Meta XR Core SDK

---

### Meta Quest Pro

**Strengths**:
- ✅ Everything Quest 3 has, PLUS:
- ✅ **Face tracking** via OVRFaceExpressions
- ✅ Eye tracking
- ✅ Natural facial expressions

**Supported Patterns**:
- ✅ All Quest 3 patterns
- ⚠️ FaceTrackingVFX (different API - OVRFaceExpressions instead of ARKit)

**Limitations**:
- Still only 70 body joints (not 91 like ARKit)
- Face tracking API different from ARKit (not direct mesh, uses blend shapes via OVRFaceExpressions)

**Implementation**:
- Repo: [jemmec/metaface-utilities](https://github.com/jemmec/metaface-utilities)
- Requires user permission for face tracking

---

### WebGL

**Strengths**:
- ✅ Cross-platform web deployment
- ✅ No app store approval needed
- ✅ Instant loading (no install)
- ✅ Works on desktop browsers

**Supported Patterns**:
- ❌ KeijiroAudioVFX (AudioListener.GetSpectrumData() NOT supported in WebGL browsers)
- ✅ WebGLOptimizer (essential for performance)
- ✅ WebGLAdvanced (JS bridge, memory management)
- ⚠️ ParticleSystem DOTS (works but single-threaded, 10x slower, NOT recommended)

**Limitations**:
- ❌ **NO ARKit/ARCore** - Browser APIs don't provide AR features
- ❌ No face tracking, body tracking, depth
- ❌ Single-threaded (no Unity Job System threading)
- ❌ 512MB memory limit typical
- ❌ No native plugins (only JavaScript interop)

**Workarounds for AR features**:
- **Face tracking**: Use MediaPipe Face Mesh (JavaScript) → Unity
- **Body tracking**: Use MediaPipe Pose (JavaScript) → Unity
- **Hand tracking**: Use MediaPipe Hands (JavaScript) → Unity
- **Depth**: Use ML depth estimation models (MiDaS) in JavaScript → Unity

**WebGL Publishing Approaches** (see [WEBGL_PUBLISHING_PLAN.md](./WEBGL_PUBLISHING_PLAN.md)):
1. **Unity WebGL** - Direct Unity build (512MB limit)
2. **Needle Engine** - Optimized Unity → Web pipeline (smaller builds)
3. **PolySpatial** - iOS/visionOS only (not WebGL)

**Multiplayer in WebGL** (see [MULTIPLAYER_WEBGL_PLAN.md](./MULTIPLAYER_WEBGL_PLAN.md)):
1. **Normcore** - Works in WebGL ($0.25/user/month)
2. **Needle Multiplayer** - WebRTC-based, free tier
3. **Pure WebRTC** - Custom implementation, no server costs

---

### Vision Pro (visionOS)

**Strengths**:
- ✅ ARKit APIs available in Full Space mode
- ✅ Skeletal hand tracking
- ✅ World tracking, plane estimation
- ✅ Scene reconstruction

**Supported Patterns**:
- ✅ KeijiroAudioVFX
- ✅ UniversalBuildPipeline
- ✅ ParticleSystem (DOTS)
- ⚠️ FaceTrackingVFX (limited ARKit support)
- ⚠️ BodyTracking91Joints (limited ARKit support)

**Limitations**:
- ⚠️ **ARKit limited to Full Space mode only**
- ❌ Apple disabled facial recognition for security
- ⚠️ Unity PolySpatial has limited ARFoundation features
- ⚠️ No traditional face tracking like iOS

**Workarounds**:
- Use hand tracking instead of face tracking
- Use plane detection + scene reconstruction for spatial understanding
- Request Full Space for ARKit features

**Implementation**:
- Use Unity PolySpatial for visionOS
- AR Foundation features limited compared to iOS

---

## 🔀 Cross-Platform Pattern Recommendations

### Priority 1: Platform-Agnostic Patterns (Works Everywhere)

Use these patterns for maximum compatibility:

1. **UniversalBuildPipeline** - Automated builds ✅
   - Platform configs for all targets
   - CI/CD integration
   - TRUE cross-platform support

2. **KeijiroAudioVFX** - Audio reactive VFX ⚠️
   - Works on iOS, Android, Quest, Vision Pro
   - **NOT WebGL** - AudioListener.GetSpectrumData() unsupported in browsers
   - Use Web Audio API directly for WebGL audio analysis

3. **ParticleSystem (DOTS)** - High-performance particles ⚠️
   - Excellent on iOS, Android, Quest, Vision Pro
   - **Avoid on WebGL** - Single-threaded, 10x slower, minutes-long loads
   - Use traditional ParticleSystem for WebGL instead

### Priority 2: iOS-First, Quest Alternative

If building for iOS first with Quest fallback:

| Feature | iOS Implementation | Quest Alternative |
|---------|-------------------|-------------------|
| **Face Tracking** | FaceTrackingVFX (ARKit) | Quest Pro: OVRFaceExpressions<br>Quest 3: Not available |
| **Body Tracking** | BodyTracking91Joints (91 joints) | Movement SDK (70 joints: 18 core + 52 hand)<br>Comparable to ARKit |
| **Human Depth** | HumanDepthVFX (ARKit segmentation) | Scene API depth (environment only, not human-specific) |
| **Hand Tracking** | ARKit hand tracking (24 joints per hand) | Quest Hand Tracking API (26 joints per hand) |

### Priority 3: WebGL-Compatible Approach

If WebGL deployment is critical:

| Feature | WebGL Solution | Limitations |
|---------|---------------|-------------|
| **Face Tracking** | MediaPipe Face Mesh (JS → Unity) | Lower accuracy than ARKit |
| **Body Tracking** | MediaPipe Pose (JS → Unity) | 33 joints, 2D only |
| **Hand Tracking** | MediaPipe Hands (JS → Unity) | 21 joints per hand |
| **Depth** | ML depth estimation (MiDaS) | Estimated, not real depth |
| **Audio Reactive** | Web Audio API (JS → Unity) | KeijiroAudioVFX NOT compatible - requires custom implementation |
| **Multiplayer** | Normcore or Needle | See [MULTIPLAYER_WEBGL_PLAN.md](./MULTIPLAYER_WEBGL_PLAN.md) |

---

## 🚀 Platform Selection Decision Tree

```
START: What platforms are you targeting?

├─ iOS only?
│  └─ Use ALL patterns (full ARKit support)
│
├─ iOS + Quest?
│  ├─ Face tracking needed?
│  │  ├─ YES → Require Quest Pro (OVRFaceExpressions)
│  │  └─ NO → Quest 3 works (body + hands)
│  └─ Implement both ARKit and Movement SDK versions
│
├─ iOS + WebGL?
│  ├─ AR features needed in WebGL?
│  │  ├─ YES → Use MediaPipe (JS → Unity bridge)
│  │  └─ NO → Disable AR features in WebGL build
│  └─ Use #if UNITY_WEBGL guards for AR code
│
├─ WebGL only?
│  └─ Limited to:
│     ├─ Web Audio API (custom implementation, NOT KeijiroAudioVFX)
│     ├─ Traditional particles (AVOID DOTS - 10x slower)
│     ├─ MediaPipe ML tracking (JS → Unity)
│     └─ WebGL-specific optimizations (WebGLAdvanced)
│
└─ All platforms?
   └─ Use truly platform-agnostic patterns:
      ├─ UniversalBuildPipeline (builds - TRUE cross-platform)
      ├─ KeijiroAudioVFX (audio - works on native, NOT WebGL)
      └─ Traditional ParticleSystem (avoid DOTS for WebGL)
```

---

## 📝 Implementation Guidelines

### Platform Detection Pattern

```csharp
public class PlatformFeatures : MonoBehaviour
{
    void Start()
    {
        #if UNITY_IOS
            EnableARKitFeatures(); // Face, body, depth
        #elif UNITY_ANDROID
            EnableARCoreFeatures(); // Limited AR
        #elif UNITY_WEBGL
            EnableWebGLFeatures(); // Audio, particles, MediaPipe bridge
        #elif PLATFORM_ANDROID && OCULUS // Quest
            EnableQuestFeatures(); // Movement SDK, hand tracking
        #elif UNITY_VISIONOS
            EnableVisionProFeatures(); // Limited ARKit in Full Space
        #endif

        // Always available (platform-agnostic)
        EnableAudioReactive(); // KeijiroAudioVFX
        EnableDOTSParticles(); // ParticleSystem
    }
}
```

### Graceful Degradation Pattern

```csharp
public class BodyTrackingManager : MonoBehaviour
{
    void Start()
    {
        #if UNITY_IOS
            // Best: 91-joint ARKit tracking
            EnableARKitBodyTracking();
        #elif PLATFORM_ANDROID && OCULUS
            // Good: 17-joint Movement SDK + hands
            EnableQuestBodyTracking();
            EnableQuestHandTracking();
        #elif UNITY_WEBGL
            // Fallback: MediaPipe 33-joint pose
            EnableMediaPipePose();
        #else
            // Minimal: Basic transform tracking
            EnableBasicTracking();
        #endif
    }
}
```

---

## 🔗 Related Documents

- [WEBGL_PUBLISHING_PLAN.md](./WEBGL_PUBLISHING_PLAN.md) - WebGL asset publishing strategies
- [MULTIPLAYER_WEBGL_PLAN.md](./MULTIPLAYER_WEBGL_PLAN.md) - Multiplayer options (Normcore/Needle/WebRTC)
- [PLATFORM_WORKAROUNDS.md](./PLATFORM_WORKAROUNDS.md) - Detailed workarounds for each platform
- [_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md](./KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md) - 520+ GitHub repos

---

**Last Verified**: 2025-11-02
**Unity Version**: 6000.1.2f1
**AR Foundation**: 6.1.0
**XR Interaction Toolkit**: 3.1.2
