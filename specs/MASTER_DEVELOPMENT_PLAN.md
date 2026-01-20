# Master Development Plan: Unity-XR-AI

**Last Updated**: 2026-01-20
**Status**: Active (Sprint 0 in progress)
**Methodology**: Incremental, Test-First, Debug-Verbose
**VFX Count**: 235 assets (73 in Resources/VFX)
**Migration Target**: `/Users/jamestunick/Documents/GitHub/portals_main` (modular features)

---

## Development Principles

### 1. Incremental Development
- Each task produces testable output
- No task takes longer than 1 day
- Every PR includes tests

### 2. Test-First with Mock Data
- Editor testing via MCP + webcam mock
- Device testing via ARFoundationRemote
- CI via recorded sessions + MockProvider

### 3. Verbose Debugging (Selective)
- `[Conditional("DEBUG_TRACKING")]` for tracking logs
- `[Conditional("DEBUG_VOICE")]` for voice logs
- `[Conditional("DEBUG_VFX")]` for VFX logs
- Production builds: all debug code stripped

### 4. MCP-Integrated Editor Testing
- Unity MCP for scene manipulation
- ARFoundationRemote for device camera streaming
- Webcam fallback when no device available

### 5. Modular & Migration-Ready
- Features designed as self-contained modules (no hard dependencies between specs)
- Interfaces over implementations (IHandTrackingProvider, IVoiceProvider, etc.)
- ScriptableObject configs for runtime parameters
- Assembly Definitions for clean dependency boundaries
- **Migration target**: `portals_main` Unity project

---

## Spec Dependency Graph

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           FOUNDATION LAYER                                   │
│  006-vfx-library-pipeline [COMPLETE]                                        │
│  - ARDepthSource, VFXARBinder, VFXLibraryManager                           │
│  - 73 VFX organized, categories, dashboard                                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
     ┌──────────────────────────────┼──────────────────────────────┐
     ▼                              ▼                              ▼
┌─────────────────────┐ ┌─────────────────────────┐ ┌─────────────────────────┐
│ ENHANCEMENT LAYER   │ │   3D CONTENT LAYER      │ │   MULTIMODAL LAYER      │
│ 007-vfx-multi-mode  │ │ 009-icosa-sketchfab     │ │ 008-multimodal-ml       │
│ [READY] 19 tasks    │ │ [DRAFT] 14 tasks        │ │ [ARCH APPROVED] 67 tasks│
│ - VFX modes         │ │ - Voice-to-object       │ │ - ITrackingProvider     │
│ - Audio: beat, FFT  │ │ - Icosa + Sketchfab     │ │ - IVoiceProvider        │
│ - Physics: collision│ │ - glTF runtime loading  │ │ - LLM context           │
└─────────────────────┘ │ - LRU model cache       │ └───────────┬─────────────┘
                        │ - Attribution tracking  │             │
                        └───────────┬─────────────┘             │
                                    │                           │
     ┌──────────────────────────────┴───────────────────────────┴─────────┐
     ▼                                                                     ▼
┌─────────────────────────────────────────┐ ┌─────────────────────────────────┐
│           PAINTING LAYER                 │ │       MULTIPLAYER LAYER          │
│  011-openbrush-integration [DRAFT]       │ │  010-normcore-multiuser [DRAFT]  │
│  25 tasks, ~62 hours                     │ │  15 tasks, ~22 hours             │
│  - 90+ URP brushes (20 Tier 1)           │ │  - AR-only multiuser drawing     │
│  - Audio reactive (5 Waveform variants)  │ │  - Normcore real-time sync       │
│  - Mirror painting (14 point, 17 wall)   │ │  - Voice chat integration        │
│  - Save/load (JSON format)               │ │  - AR Foundation touch input     │
│  - API painting (HTTP endpoints)         │ │  - BrushStroke RealtimeModel     │
│  - Reaktion audio system                 │ └────────────────┬────────────────┘
└────────────────┬────────────────────────┘                   │
                 │                                            │
    ┌────────────┴────────────────────────────────────────────┘
    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        HAND TRACKING LAYER                                   │
│  012-hand-tracking [DRAFT - P0]                                             │
│  24 tasks (8 phases), ~32 hours                                             │
│  - IHandTrackingProvider unified interface                                   │
│  - HoloKit SDK (21 joints) + XR Hands (26 joints) providers                 │
│  - BodyPix/Touch fallbacks                                                   │
│  - Brush painting: pinch→draw, velocity→particles, rotation→angle           │
│  - GraphicsBuffer stroke persistence                                         │
└─────────────────────────────────────────────────────────────────────────────┘
                                                              │
                                                              │
                               ┌──────────────────────────────┘
                               ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           HOLOGRAM LAYER                                     │
│  002-h3m-foundation [✅ COMPLETE - Legacy components superseded]            │
│  003-hologram-conferencing [DRAFT - depends on 008+010 multiuser]          │
│                                                                              │
│  RECOMMENDED: Use Hologram prefab (HologramPlacer + HologramController)     │
│  LEGACY: H3M_HologramRig (HologramSource + HologramRenderer + HologramAnchor)│
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Consolidated Implementation Order

### Sprint 0: Debug Infrastructure (3 days)

**Goal**: Establish debugging and testing foundation before any new features.

| ID | Task | LOC | Output |
|----|------|-----|--------|
| D-001 | Create `DebugFlags.cs` with conditional attributes | ~50 | Compile-time debug control |
| D-002 | Create `DebugLogger.cs` with category filtering | ~100 | Runtime debug filtering |
| D-003 | Create `DebugOverlay.cs` IMGUI panel | ~150 | Visual debug dashboard |
| D-004 | Create `WebcamMockSource.cs` for Editor | ~120 | Webcam → AR texture simulation |
| D-005 | Integrate ARFoundationRemote fallback | ~80 | Device camera in Editor |
| D-006 | Create `EditorTestScene.unity` | - | MCP-ready test environment |

**Files Created**:
```
Assets/Scripts/Debug/
├── DebugFlags.cs           # Conditional compilation flags
├── DebugLogger.cs          # Category-filtered logging
├── DebugOverlay.cs         # IMGUI debug panel
├── DebugConfig.asset       # ScriptableObject settings
└── WebcamMockSource.cs     # Webcam → AR mock

Assets/Scenes/
└── EditorTestScene.unity   # MCP testing scene
```

### Sprint 1: Spec 007 - VFX Multi-Mode (5 days)

**Dependency**: Spec 006 [COMPLETE]

| Day | Tasks | Test |
|-----|-------|------|
| 1 | T-001: VFXCategory integration | Unit: mode binding |
| 1 | T-002: VFXModeController | Editor: mode switch |
| 2 | T-004: BeatDetector | Unit: onset detection |
| 2 | T-005: AudioBridge beat | Editor: audio → VFX |
| 3 | T-008: AR mesh collision | Editor: particle bounce |
| 3 | T-010: Hand velocity | Editor: velocity binding |
| 4 | T-011-T-013: VFX audit | Matrix: 73 VFX × modes |
| 5 | T-017-T-019: Device testing | Device: performance |

### Sprint 2: Spec 008 Core - Tracking Interfaces (5 days)

**Dependency**: Sprint 0 (debug infrastructure)

| Day | Tasks | Test |
|-----|-------|------|
| 1 | T-005: ITrackingProvider | Unit: interface contract |
| 1 | T-006: TrackingProviderFactory | Unit: platform detection |
| 2 | D-007: MockTrackingProvider | Unit: all capabilities |
| 2 | D-008: TrackingRecorder | Editor: record session |
| 3 | D-009: TrackingSimulator | Unit: playback accuracy |
| 3 | T-007: SentisTrackingProvider | Editor: BodyPix wrap |
| 4 | T-020: VFXARBinder + providers | Editor: VFX binding |
| 5 | Integration testing | Device: full pipeline |

### Sprint 3: Spec 008 - ARKit Providers (5 days)

**Dependency**: Sprint 2

| Day | Tasks | Test |
|-----|-------|------|
| 1 | T-008b: ARKitBodyProvider (91j) | Device: skeleton |
| 2 | T-008c: ARKitHandProvider (21j) | Device: hand joints |
| 2 | T-008d: ARKitPoseProvider (17j) | Device: 2D pose |
| 3 | T-008e: ARKitFaceProvider | Device: blendshapes |
| 4 | T-008f: CompositeProvider | Device: aggregation |
| 5 | T-013b-c: 91-joint VFX | Device: skeleton VFX |

### Sprint 4: Spec 008 - Voice Architecture (5 days)

**Dependency**: Sprint 2 (uses same patterns)

| Day | Tasks | Test |
|-----|-------|------|
| 1 | T-030: IVoiceProvider | Unit: interface |
| 1 | T-031: IVoiceConsumer | Unit: interface |
| 2 | T-032: VoiceService | Unit: orchestration |
| 2 | T-037: MockVoiceProvider | Unit: simulation |
| 3 | T-033: WhisperProvider | Editor: transcription |
| 4 | T-034: WebSpeechProvider | WebGL: browser API |
| 5 | T-052: VoiceCommandConsumer | Editor: commands |

### Sprint 5: Spec 008 - Testing & LLM (5 days)

**Dependency**: Sprints 2-4

| Day | Tasks | Test |
|-----|-------|------|
| 1 | T-038-T-041: Test infrastructure | CI: automated |
| 2 | T-042-T-043: Unit test suites | CI: coverage |
| 3 | T-044-T-047: Context providers | Unit: LLM context |
| 4 | T-048-T-049: Self-validation | Unit: refinement |
| 5 | T-050-T-051: XRAssistant | Editor: integration |

### Sprint 6: Spec 008 - Platform Providers (5 days)

**Dependency**: Sprints 2-3

| Day | Tasks | Test |
|-----|-------|------|
| 1-2 | T-012: MetaTrackingProvider | Quest: tracking |
| 2-3 | T-014-T-016: ONNX Web | WebGL: inference |
| 4 | T-018: visionOSProvider | visionOS: hands |
| 5 | Cross-platform testing | Matrix: all platforms |

### Sprint 7: Spec 008 - Multiuser (5 days)

**Dependency**: Sprint 5 (testing), Sprint 6 (platforms)

| Day | Tasks | Test |
|-----|-------|------|
| 1 | T-023: TrackingDataSerializer | Unit: encoding |
| 2 | T-024: WebRTC integration | Editor: P2P |
| 3 | T-025: RemoteAvatarReconstructor | Editor: avatar |
| 4-5 | T-026: Multiuser testing | Device: 2-4 users |

### Sprint 8: Spec 009 - Icosa/Sketchfab Foundation (5 days)

**Dependency**: Sprint 4 (Voice Architecture - for voice commands)

| Day | Tasks | Test |
|-----|-------|------|
| 1-2 | Task 0.1: SketchfabClient.cs | Unit: search, rate limiting |
| 2-3 | Task 0.2: ModelCache.cs | Unit: LRU eviction |
| 4 | Task 0.3: Editor settings | Editor: API key config |
| 5 | Integration: existing WhisperIcosaController | Editor: voice → search |

**Key Deliverables**:
- `Assets/H3M/Icosa/SketchfabClient.cs` - Sketchfab Download API
- `Assets/H3M/Icosa/ModelCache.cs` - LRU disk caching
- Extended PtSettings for Sketchfab API key

### Sprint 9: Spec 009 - Unified Search & Caching (5 days)

**Dependency**: Sprint 8

| Day | Tasks | Test |
|-----|-------|------|
| 1-2 | Task 1.1: UnifiedModelSearch.cs | Unit: parallel search |
| 2-3 | Task 1.2: Extend IcosaAssetLoader | Unit: download + cache |
| 4 | Task 1.3: Extend WhisperIcosaController | Editor: voice → both sources |
| 5 | Integration testing | Device: voice-to-object |

**Key Deliverables**:
- `Assets/H3M/Icosa/UnifiedModelSearch.cs` - Aggregated search
- Extended `IcosaAssetLoader` for both sources
- Full voice-to-object pipeline

### Sprint 10: Spec 009 - UI & Polish (5 days)

**Dependency**: Sprint 9

| Day | Tasks | Test |
|-----|-------|------|
| 1-2 | Task 2.1: ModelSearchUI panel | Editor: search + grid |
| 2-3 | Task 2.2-2.3: Preview + Attribution | Editor: 3D preview |
| 3-4 | Task 3.1-3.2: Error handling, performance | Unit: resilience |
| 5 | Task 3.3: Device testing | Device: iPhone 15 Pro |

**Key Deliverables**:
- UI Toolkit search panel with thumbnails
- 3D model preview before placement
- Attribution panel for CC compliance
- Device-validated performance

### Sprint 11: Spec 010 - Normcore AR Multiuser (5 days)

**Dependency**: Sprint 0 (debug infrastructure)

| Day | Tasks | Test |
|-----|-------|------|
| 1 | Task 0.1-0.3: Setup | Import AR Spectator, configure Normcore |
| 2 | Task 1.1-1.2: Connection tests | Multi-device room join |
| 3 | Task 2.1-2.2: AR input | ARBrushInput, adapt Brush.cs |
| 4 | Task 2.3-2.4: Sync tests | Stroke sync, late joiner |
| 5 | Task 3.1-3.5: Voice & polish | Voice chat, performance |

**Key Deliverables**:
- `Assets/Scripts/Normcore/ARBrushInput.cs` - AR touch to brush position
- `Assets/Scripts/Normcore/ARBrush.cs` - Adapted from VR Brush.cs
- Voice chat working between devices
- Full multiplayer drawing validated

### Sprint 12: Spec 011 - Open Brush Integration (14 days / 7 internal sprints)

**Dependency**: Sprint 1 (VFX Multi-Mode audio), Sprint 0 (debug infrastructure)

| Days | Internal Sprint | Tasks | Test |
|------|-----------------|-------|------|
| 1-2 | Sprint 0: Setup | 0.1-0.4: Namespace, BrushData, BrushStroke, BrushManager | Unit: mesh generation |
| 3-4 | Sprint 1: Tier 1 | 1.1-1.4: Import materials, port shaders, flat/tube geometry | Editor: 10 brushes render |
| 5-6 | Sprint 2: Audio | 2.1-2.4: Reaktion port, modulation, Waveform shader | Editor: audio reactive |
| 7-8 | Sprint 3: Mirror | 3.1-3.5: Point symmetry, wallpaper groups, UI | Editor: 12-fold symmetry |
| 9-10 | Sprint 4: Save | 4.1-4.4: JSON format, serializer, load, UI | Unit: round-trip |
| 11-12 | Sprint 5: API | 5.1-5.4: HTTP server, endpoints, stroke API | Integration: remote drawing |
| 13-14 | Sprint 6: Polish | 6.1-6.4: Tier 2 brushes, AR input, perf, testing | Device: full validation |

**Key Deliverables**:
- `Assets/Scripts/Brush/` - Complete brush namespace (~15 scripts)
- 20+ URP brush materials with BrushData assets
- 5 audio reactive brushes with Reaktion integration
- Point symmetry (14 families) + Wallpaper groups (17 patterns)
- JSON save/load for scenes
- HTTP API for programmatic painting
- AR touch input with plane raycasting

**Performance Targets**:
| Metric | Target | Device |
|--------|--------|--------|
| 100 strokes FPS | 30+ | iPhone 12 |
| 500 strokes FPS | 30+ | iPhone 15 Pro |
| Audio latency | <20ms | Waveform brush |
| Save 500 strokes | <2s | JSON serialization |

### Sprint 13: Spec 012 - Hand Tracking + Brush Painting (5 days)

**Dependency**: Sprint 1 (VFX Multi-Mode), existing HandVFXController/ARKitHandTracking code

| Day | Tasks | Test |
|-----|-------|------|
| 1 | T1.1-T1.4: IHandTrackingProvider interface | Unit: interface contract |
| 2 | T2.1-T2.4: Provider implementations | Unit: HoloKit, XRHands, BodyPix, Touch |
| 3 | T3.1-T3.4: VFX integration | Editor: VFXHandBinder, property bindings |
| 3-4 | T4.1-T4.3: Gesture system | Editor: pinch/grab detection |
| 4-5 | T5.1-T5.4: Testing & verification | Device: iPhone + HoloKit |

**Key Deliverables**:
- `Assets/Scripts/HandTracking/Interfaces/IHandTrackingProvider.cs`
- `Assets/Scripts/HandTracking/Providers/*` (4 providers)
- `Assets/Scripts/VFX/Binders/VFXHandBinder.cs`
- Updated `HandVFXController.cs` using unified provider

**Note**: Leverages existing `HandVFXController.cs` (420 LOC) and `ARKitHandTracking.cs` (340 LOC) - refactors to unified interface, does NOT rewrite.

### Sprint 14: Spec 012 - Brush Painting System (5 days)

**Dependency**: Sprint 13 (hand tracking providers)

| Day | Tasks | Test |
|-----|-------|------|
| 1 | T5.1-T5.4: BrushController, GestureInterpreter | Unit: gesture→action |
| 2 | T6.1-T6.3: BrushPalette, ColorPicker | Editor: selection UI |
| 3 | T7.1-T7.3: StrokeManager, persistence | Unit: GraphicsBuffer |
| 4-5 | T8.1-T8.4: VFX brush integration | Device: 8 brush types |

**Key Deliverables**:
- `Assets/Scripts/Painting/` (6 scripts)
- `Assets/Scripts/VFX/Binders/VFXBrushBinder.cs`
- 8 brush VFX assets configured

### Sprint 15: Spec 003 - Hologram Conferencing (5 days)

**Dependency**: Sprint 7 (multiuser foundation from 008), Sprint 11 (Normcore validation)

*Tasks from 003-hologram-conferencing/tasks.md*

---

## Debug Infrastructure Details

### DebugFlags.cs

```csharp
// Conditional compilation symbols defined in:
// Project Settings > Player > Scripting Define Symbols

public static class DebugFlags
{
    // Add to Player settings for debug builds:
    // DEBUG_TRACKING;DEBUG_VOICE;DEBUG_VFX;DEBUG_NETWORK

    [Conditional("DEBUG_TRACKING")]
    public static void LogTracking(string message, UnityEngine.Object context = null)
        => Debug.Log($"[TRACKING] {message}", context);

    [Conditional("DEBUG_VOICE")]
    public static void LogVoice(string message, UnityEngine.Object context = null)
        => Debug.Log($"[VOICE] {message}", context);

    [Conditional("DEBUG_VFX")]
    public static void LogVFX(string message, UnityEngine.Object context = null)
        => Debug.Log($"[VFX] {message}", context);

    [Conditional("DEBUG_NETWORK")]
    public static void LogNetwork(string message, UnityEngine.Object context = null)
        => Debug.Log($"[NETWORK] {message}", context);

    // Runtime toggle (for builds with symbols defined)
    public static bool TrackingEnabled = true;
    public static bool VoiceEnabled = true;
    public static bool VFXEnabled = true;
    public static bool NetworkEnabled = true;
}
```

### DebugLogger.cs

```csharp
public enum LogCategory { Tracking, Voice, VFX, Network, System }
public enum LogLevel { Verbose, Info, Warning, Error }

[CreateAssetMenu(fileName = "DebugConfig", menuName = "Debug/Config")]
public class DebugConfig : ScriptableObject
{
    public LogLevel MinLevel = LogLevel.Info;
    public LogCategory[] EnabledCategories = { LogCategory.System };
    public bool ShowTimestamps = true;
    public bool ShowStackTrace = false;
    public int MaxLogHistory = 1000;
}

public static class DebugLogger
{
    private static DebugConfig _config;
    private static List<LogEntry> _history = new();

    public static void Log(LogCategory category, LogLevel level, string message)
    {
        if (_config == null) LoadConfig();
        if (level < _config.MinLevel) return;
        if (!_config.EnabledCategories.Contains(category)) return;

        var entry = new LogEntry(category, level, message, Time.time);
        _history.Add(entry);
        if (_history.Count > _config.MaxLogHistory)
            _history.RemoveAt(0);

        var prefix = _config.ShowTimestamps ? $"[{Time.time:F2}] " : "";
        Debug.Log($"{prefix}[{category}] {message}");
    }

    public static IReadOnlyList<LogEntry> GetHistory() => _history;
    public static void Clear() => _history.Clear();
}
```

### WebcamMockSource.cs

```csharp
#if UNITY_EDITOR
[ExecuteAlways]
public class WebcamMockSource : MonoBehaviour
{
    [SerializeField] private bool _useWebcam = true;
    [SerializeField] private int _webcamIndex = 0;
    [SerializeField] private Vector2Int _resolution = new(640, 480);

    private WebCamTexture _webcamTex;
    private RenderTexture _depthMock;
    private RenderTexture _colorMock;

    public RenderTexture DepthTexture => _depthMock;
    public RenderTexture ColorTexture => _colorMock;

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (!_useWebcam) return;

        var devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogWarning("[WebcamMock] No webcam found, using procedural mock");
            return;
        }

        _webcamTex = new WebCamTexture(devices[_webcamIndex].name, _resolution.x, _resolution.y);
        _webcamTex.Play();

        _colorMock = new RenderTexture(_resolution.x, _resolution.y, 0, RenderTextureFormat.ARGB32);
        _depthMock = new RenderTexture(_resolution.x, _resolution.y, 0, RenderTextureFormat.RFloat);

        // Register with ARDepthSource as mock input
        ARDepthSource.Instance?.SetMockTextures(_colorMock, _depthMock);
    }

    void Update()
    {
        if (_webcamTex == null || !_webcamTex.isPlaying) return;

        // Copy webcam to color
        Graphics.Blit(_webcamTex, _colorMock);

        // Generate mock depth (gradient or ML inference)
        GenerateMockDepth();
    }

    private void GenerateMockDepth()
    {
        // Simple gradient depth for basic testing
        // Can be replaced with ML depth estimation
        var mat = new Material(Shader.Find("Hidden/DepthGradient"));
        Graphics.Blit(null, _depthMock, mat);
    }

    void OnDisable()
    {
        if (_webcamTex != null)
        {
            _webcamTex.Stop();
            Destroy(_webcamTex);
        }
        if (_colorMock != null) _colorMock.Release();
        if (_depthMock != null) _depthMock.Release();
    }
}
#endif
```

### MCP Test Scene Setup

```
EditorTestScene.unity
├── Main Camera (with WebcamMockSource)
├── AR Session Origin (disabled in Editor)
├── Debug
│   ├── DebugOverlay
│   ├── DebugConfig (ScriptableObject)
│   └── TrackingDebugVisualizer
├── Tracking
│   ├── TrackingService
│   └── MockTrackingProvider
├── Voice
│   ├── VoiceService
│   └── MockVoiceProvider
├── VFX
│   ├── ARDepthSource
│   ├── VFXLibraryManager
│   └── [VFX Instances]
└── UI
    ├── VFXToggleUI
    └── DebugPanel
```

---

## CI/CD Pipeline

### Editor Tests (No Device)

```yaml
# .github/workflows/editor-tests.yml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: game-ci/unity-test-runner@v4
        with:
          testMode: editmode
          projectPath: MetavidoVFX-main
          customParameters: -enableCodeCoverage
```

### Recorded Session Tests

```yaml
# Runs against recorded tracking sessions
jobs:
  recorded-tests:
    runs-on: ubuntu-latest
    steps:
      - name: Run tracking simulation tests
        run: |
          unity-editor -batchmode -runTests \
            -testFilter "TrackingSimulationTests" \
            -testResults results.xml
```

### Device Build Matrix

| Platform | Build | Test Method |
|----------|-------|-------------|
| iOS | Xcode archive | TestFlight |
| Android | APK/AAB | Firebase Test Lab |
| Quest | APK | Meta Quest Developer Hub |
| WebGL | Static build | BrowserStack |

---

## Verification Gates

### Per-Task Verification

1. **Code compiles** - No errors
2. **Unit tests pass** - Coverage >80%
3. **Editor test pass** - MCP verification
4. **Debug logs clean** - No unexpected errors
5. **Performance target met** - FPS/latency within spec

### Per-Sprint Verification

1. **All tasks complete** - Checklist verified
2. **Integration tests pass** - Cross-component
3. **Device build succeeds** - At least iOS
4. **Performance regression** - No >10% degradation
5. **Documentation updated** - API docs current

### Release Gate

1. **All sprints complete** - Through Sprint 7
2. **Cross-platform matrix** - All platforms green
3. **Performance benchmarks** - Meeting targets
4. **Security review** - No vulnerabilities
5. **User acceptance** - Stakeholder sign-off

---

## Quick Reference: Test Commands

```bash
# MCP: Check Unity console
mcp__UnityMCP__read_console

# MCP: Run EditMode tests
mcp__UnityMCP__run_tests --mode EditMode

# MCP: Take screenshot
mcp__UnityMCP__manage_scene --action screenshot

# MCP: Get scene hierarchy
mcp__UnityMCP__manage_scene --action get_hierarchy

# Unity CLI: Run tests
unity-editor -runTests -testResults results.xml

# Unity CLI: Build iOS
unity-editor -executeMethod BuildScript.BuildIOS
```

---

## Related Documents

| Document | Location |
|----------|----------|
| Spec Index | [specs/README.md](./README.md) |
| Spec 006 | [006-vfx-library-pipeline/](./006-vfx-library-pipeline/) |
| Spec 007 | [007-vfx-multi-mode/](./007-vfx-multi-mode/) |
| Spec 008 | [008-crossplatform-multimodal-ml-foundations/](./008-crossplatform-multimodal-ml-foundations/) |
| Spec 009 | [009-icosa-sketchfab-integration/](./009-icosa-sketchfab-integration/) |
| Spec 010 | [010-normcore-multiuser/](./010-normcore-multiuser/) |
| Spec 011 | [011-openbrush-integration/](./011-openbrush-integration/) |
| Spec 012 | [012-hand-tracking/](./012-hand-tracking/) |
| Architecture | [008.../FINAL_RECOMMENDATIONS.md](./008-crossplatform-multimodal-ml-foundations/FINAL_RECOMMENDATIONS.md) |
| KB: LLMR | [KnowledgeBase/_LLMR_XR_AI_ARCHITECTURE_PATTERNS.md](../KnowledgeBase/_LLMR_XR_AI_ARCHITECTURE_PATTERNS.md) |
| KB: Multiuser | [KnowledgeBase/_WEBRTC_MULTIUSER_MULTIPLATFORM_GUIDE.md](../KnowledgeBase/_WEBRTC_MULTIUSER_MULTIPLATFORM_GUIDE.md) |
| Icosa Integration | [MetavidoVFX-main/Assets/Documentation/ICOSA_INTEGRATION.md](../MetavidoVFX-main/Assets/Documentation/ICOSA_INTEGRATION.md) |
| Open Brush Ref | [_ref/open-brush-feature-pure-openxr/](../_ref/open-brush-feature-pure-openxr/) |

---

## Total Effort Summary

| Spec | Tasks | Hours | Sprints | Status |
|------|-------|-------|---------|--------|
| 006 - VFX Library Pipeline | 17 | ~35h | - | ✅ Complete |
| 007 - VFX Multi-Mode | 19 | ~40h | 1 | Ready |
| 008 - Multimodal ML | 67 | ~140h | 7 | Arch Approved |
| 009 - Icosa/Sketchfab | 14 | ~42h | 3 | Draft |
| 010 - Normcore Multiuser | 15 | ~22h | 1 | Draft |
| 011 - Open Brush | 25 | ~62h | 2 (14 days) | Draft |
| **012 - Hand Tracking** | **24** | **~32h** | **2** | **Draft (P0)** |
| 003 - Hologram Conferencing | TBD | TBD | 1 | Draft |
| **Total** | **181+** | **~370h+** | **17** |

---

## Legacy & Deprecated Components

### ⚠️ Deprecated (Do Not Use)

| Component | Location | Replaced By | Notes |
|-----------|----------|-------------|-------|
| `VFXBinderManager` | `_Legacy/` | `VFXARBinder` | Old centralized binding (spec 006 removed) |
| `VFXARDataBinder` | `_Legacy/` | `VFXARBinder` | Old per-VFX binding (spec 006 removed) |
| `EnhancedAudioProcessor` | `Scripts/Audio/` | `AudioBridge` | Simpler 6-band FFT (spec 006) |
| `PeopleOcclusionVFXManager` | Removed | `ARDepthSource` | Consolidated into single source |
| `H3M_HologramRig.prefab` | `H3M/Prefabs/` | `Hologram.prefab` | Legacy hologram (spec 002 deprecated) |
| `HologramSource` | `H3M/Core/` | `HologramPlacer` | Legacy hologram recording |
| `HologramRenderer` | `H3M/Core/` | `HologramController` | Legacy hologram playback |
| `HologramAnchor` | `H3M/Core/` | Built into `HologramController` | Legacy AR anchor |

### ✅ Current (Use These)

| Component | Location | Purpose | Spec |
|-----------|----------|---------|------|
| `ARDepthSource` | `Scripts/Bridges/` | Single compute dispatch for depth→world | 006 |
| `VFXARBinder` | `Scripts/Bridges/` | Per-VFX texture binding (lightweight) | 006 |
| `VFXLibraryManager` | `Scripts/VFX/` | 73 VFX organized by category | 006 |
| `AudioBridge` | `Scripts/Bridges/` | 6-band FFT → global shader props | 006 |
| `Hologram.prefab` | `Prefabs/` | Recommended hologram (HologramPlacer + Controller) | 002 |
| `HandVFXController` | `Scripts/HandTracking/` | HoloKit hand tracking + VFX | 012 |
| `ARKitHandTracking` | `Scripts/HandTracking/` | XR Hands fallback | 012 |
| `NNCamKeypointBinder` | `NNCam/Scripts/` | BodyPix keypoints → VFX | 006 |
| `BodyPartSegmenter` | `Scripts/Segmentation/` | 24-part body segmentation | 004 |

### 🔒 Frozen (Reserved for Spec 003)

| Component | Status | Reserved For |
|-----------|--------|--------------|
| `HologramSource`, `HologramRenderer`, `HologramAnchor` | Frozen | Spec 003 |
| `HologramPlacer`, `HologramController` | Frozen | Spec 003 |
| `MetadataDecoder`, `TextureDemuxer`, `FrameEncoder` | Reserved | Spec 003 |
| WebRTC hologram streaming | Reserved | Spec 003 |

**Policy**: Future specs (012+) MUST NOT modify hologram-related components.

---

*Created: 2026-01-20*
*Last Updated: 2026-01-20*
