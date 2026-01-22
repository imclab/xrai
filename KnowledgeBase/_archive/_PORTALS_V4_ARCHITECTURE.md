# Portals v4 - Complete Architecture Reference

**Project**: Unity AR + React Native hybrid mobile app
**Branch**: `react-unity` (active development)
**Last Updated**: 2026-01-13

---

## Executive Summary

Portals v4 is a dual-AR hybrid app combining:
- **React Native (Expo SDK 54)** - Business logic, navigation, state, UI
- **Unity 6 as a Library (UAAL)** - High-fidelity AR rendering, VFX
- **ViroReact** - Legacy AR features (360° scenes, portals)

**Core Principle**: "If it is Logic, write in RN. If it is 10,000 Particles, write in Unity VFX Graph."

---

## 1. Navigation Architecture

### Screen Count: 37 screens across 5 navigation stacks

```
RootNavigator (Stack.Navigator)
├── Auth Stack
│   ├── LoginScreen (Magic link auth via Supabase)
│   ├── SignUpScreen
│   └── VerificationScreen
│
├── Main Stack (after auth)
│   ├── MainTabNavigator (Tab.Navigator) ─────────────────────┐
│   │   ├── ExploreTab → ExploreStack                         │
│   │   │   ├── PortalExploreScreen (portal discovery)        │
│   │   │   ├── PortalDetailScreen                            │
│   │   │   └── ChannelScreen                                 │
│   │   │                                                     │
│   │   ├── CreateTab → CreateStack                           │
│   │   │   ├── CreateScreen (portal creation hub)            │
│   │   │   ├── SceneEditorScreen                             │
│   │   │   └── PublishScreen                                 │
│   │   │                                                     │
│   │   ├── LibraryTab → LibraryStack                         │
│   │   │   ├── AssetLibraryScreen (user's assets)            │
│   │   │   └── CollectionScreen                              │
│   │   │                                                     │
│   │   └── ProfileTab → ProfileStack                         │
│       │   ├── ProfileScreen                                 │
│       │   └── SettingsScreen                                │
│       │                                                     │
│   └── AR Experience Screens (modal/fullscreen)              │
│       ├── UnityArScreen → UnityArView.tsx (UAAL)            │
│       ├── FigmentARScreen → ViroReact AR                    │
│       └── Portal360Screen → ViroReact 360°                  │
```

### Key Navigation Patterns

| Pattern | Implementation | Why |
|---------|---------------|-----|
| Tab Navigation | `@react-navigation/bottom-tabs` | Standard mobile UX |
| Stack Navigation | `@react-navigation/native-stack` | Performance (native) |
| AR Screens | Modal presentation | Full-screen immersion |
| Deep Linking | Expo Router compatible | Share/QR code support |

---

## 2. State Management Architecture

### Primary: Zustand (Global State)

```
src/stores/
├── authStore.ts          # User session, tokens
├── portalStore.ts        # Portal data, CRUD
├── assetStore.ts         # Asset library state
├── sceneStore.ts         # Active scene state
└── uiStore.ts            # UI preferences, modals
```

**Why Zustand over Redux?**
- Simpler API, less boilerplate
- Better TypeScript inference
- Smaller bundle size
- No provider wrapping needed

### Legacy: Redux (FigmentAR only)

FigmentAR retains isolated Redux store for:
- AR session state
- Recording state
- Effect parameters

**Pattern**: FigmentAR Redux is self-contained, doesn't leak to app.

---

## 3. Unity-React Native Bridge

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     React Native Layer                          │
│  ┌─────────────────┐    ┌─────────────────┐                     │
│  │ UnityArView.tsx │───▶│ Native Module   │                     │
│  │ (Fabric)        │◀───│ (Objective-C++) │                     │
│  └─────────────────┘    └─────────────────┘                     │
│         │ sendMessage()        ▲ onUnityMessage                 │
│         ▼                      │                                │
├─────────────────────────────────────────────────────────────────┤
│                     Native Bridge Layer                         │
│  ┌─────────────────┐    ┌─────────────────┐                     │
│  │ RNUnityView.mm  │───▶│NativeCallProxy.m│                     │
│  │ (Fabric Host)   │◀───│ (C Interop)     │                     │
│  └─────────────────┘    └─────────────────┘                     │
│         │ UnityFramework        ▲ sendMessageToMobileApp        │
│         ▼                      │                                │
├─────────────────────────────────────────────────────────────────┤
│                     Unity Layer                                 │
│  ┌─────────────────┐    ┌─────────────────┐                     │
│  │ ARTestScene     │───▶│ BridgeTarget.cs │                     │
│  │ (AR Foundation) │    │ (Message Router)│                     │
│  └─────────────────┘    └─────────────────┘                     │
└─────────────────────────────────────────────────────────────────┘
```

### Message Protocol

**RN → Unity** (JSON):
```json
{
  "type": "ping|spawn_vfx|load_scene|set_property",
  "payload": { "key": "value" }
}
```

**Unity → RN** (JSON):
```json
{
  "type": "unity_ready|pong|vfx_spawned|error",
  "payload": { "status": "ok", "data": {} }
}
```

### Initialization Sequence

```
1. UnityArScreen mounts
2. UnityArView renders (Fabric component)
3. Unity framework initializes
4. ARTestScene loads
5. BridgeTarget.Awake() runs
6. Unity sends "unity_ready" message
7. RN receives via onUnityMessage
8. UI enables interaction buttons
```

### Critical Patches (6 total)

| Patch | File | Purpose |
|-------|------|---------|
| Main patch | `@artmajeur+react-native-unity+0.0.6.patch` | Core fixes (~600KB) |
| ViroKit | `patch-virokit.sh` | ViroKit pod compatibility |
| Codegen | `patch-react-native-unity.js` | Fabric discovery |
| Message Queue | `patch-rn-unity-message-queue.sh` | Buffer early messages |
| Fabric Registry | `patch-fabric-registry.sh` | Register RNUnityView |
| VSync | BridgeTarget.cs | Fix 15 FPS issue |

### Known Issues & Solutions

| Issue | Symptom | Solution |
|-------|---------|----------|
| Fabric registration | Unity never initializes | Run `patch-fabric-registry.sh` |
| Early messages lost | Buttons stay disabled | Message queue patch |
| 15 FPS | Choppy rendering | vSyncCount = 0 |
| Debug build crash | `_mh_dylib_header` | Use Release config only |

---

## 4. Recording Infrastructure

### Native Module: ArViewRecorderModule.swift

```swift
// Core Components
- AVAssetWriter (video encoding)
- CADisplayLink (30 FPS frame capture)
- drawHierarchy() (UIView → image)

// Key Methods
- startRecording(viewTag, fileName) → Promise
- pauseRecording() → void
- resumeRecording() → Promise
- stopRecording() → Promise<videoPath>
```

### FigmentAR Recording Pattern

```javascript
// app.js lines 2585-2601
<TouchableWithoutFeedback
  onPressIn={() => {
    const viewTag = findNodeHandle(this._arNavigator);
    ArViewRecorder.startRecording(viewTag, fileName);
  }}
  onPressOut={() => {
    ArViewRecorder.pauseRecording();
  }}
>
  {/* SVG record button */}
</TouchableWithoutFeedback>
```

### Unity View Recording (Future)

The same pattern works for Unity because:
1. `drawHierarchy()` works on ANY UIView
2. Unity renders to a UIView via UnityFramework
3. Just need to get Unity view's tag via `findNodeHandle(unityRef)`

**Implementation needed**:
- Modify `findARView()` to detect Unity view class
- Or pass viewTag explicitly from React Native

---

## 5. Build System

### Build Scripts

| Script | Purpose | Time |
|--------|---------|------|
| `build_minimal.sh` | Incremental build | ~5 min |
| `build_and_run_ios.sh` | Full build + deploy | ~7 min |
| `capture_device_logs.sh` | Safe log capture | 10-60 sec |

### Build Flow

```
1. Unity Build (BuildScript.cs)
   └─▶ Export to /tmp/unity-ios-export/
   └─▶ Build UnityFramework.framework
   └─▶ Copy to unity/builds/ios/

2. React Native Build
   └─▶ pod install (links UnityFramework)
   └─▶ xcodebuild (Release config)
   └─▶ Install to device

3. Verification
   └─▶ Check console for errors
   └─▶ Verify Unity initializes
   └─▶ Test bridge communication
```

### Build Modes

| Mode | Flag | When to Use |
|------|------|-------------|
| Incremental | (default) | Day-to-day development |
| Clean | `UNITY_CLEAN_BUILD=1` | After major changes |
| Skip Unity | `--skip-unity-export` | RN-only changes |

### Xcode Configuration

| Setting | Value | Why |
|---------|-------|-----|
| Configuration | Release | Debug has Unity bug |
| Linker flag | `-Wl,-ld_classic` | Xcode 15+ compatibility |
| Team ID | `Z8622973EB` | Code signing |

---

## 6. Service Architecture

### Backend Services

```
src/services/
├── supabase/
│   ├── client.ts         # Supabase client init
│   ├── auth.ts           # Magic link auth
│   └── storage.ts        # Asset uploads
│
├── cloudflare/
│   └── r2Client.ts       # Heavy data (scene JSON)
│
├── sceneAI/
│   ├── llmClient.ts      # Gemini 2.0 integration
│   ├── intentSchema.ts   # Voice command parsing
│   └── vibeEngine.ts     # Scene generation
│
└── bridge/
    └── unityBridge.ts    # Unity message dispatcher
```

### Data Flow

```
User Action → Zustand Store → Service Layer → Backend
                   ↓
            React Component → Native Module → Unity
```

---

## 7. AR Architecture (Dual System)

### ViroReact (Legacy)

- **Use Cases**: 360° portals, image tracking, simple AR
- **Screens**: FigmentARScreen, Portal360Screen
- **Status**: Maintained but not extended

### Unity AR Foundation (Primary)

- **Use Cases**: High-fidelity VFX, complex AR, plane tracking
- **Screens**: UnityArScreen
- **Status**: Active development

### Selection Criteria

| Feature | Use ViroReact | Use Unity |
|---------|--------------|-----------|
| 360° content | ✅ | ❌ |
| Particle VFX | ❌ | ✅ |
| Plane detection | Basic | Advanced |
| Performance | Medium | High |
| Bundle size | +50MB | +300MB |

---

## 8. File Structure Reference

```
portals_v4/
├── src/
│   ├── components/        # Reusable React components
│   ├── screens/           # Screen components (37 total)
│   ├── navigation/        # Navigation configuration
│   ├── stores/            # Zustand stores
│   ├── services/          # API clients, bridges
│   └── hooks/             # Custom React hooks
│
├── unity/
│   ├── Assets/
│   │   ├── Scenes/        # ARTestScene, UnityTestScene
│   │   ├── Scripts/       # BridgeTarget, ARDebugOverlay
│   │   ├── Prefabs/       # AR Origin prefab
│   │   ├── Plugins/iOS/   # NativeCallProxy
│   │   └── Editor/        # BuildScript.cs
│   └── builds/ios/        # UnityFramework.framework
│
├── ios/
│   ├── Portals.xcworkspace
│   └── Podfile
│
├── modules/
│   └── ar-view-recorder/  # Native recording module
│
├── scripts/
│   ├── build_minimal.sh
│   ├── patch-*.sh         # Various patches
│   └── common.sh
│
└── .specify/              # Spec-kit methodology
    ├── memory/            # Project constitution
    ├── templates/         # Spec/plan templates
    └── specs/             # Numbered specifications
```

---

## 9. Performance Targets

| Platform | Target FPS | Current | Status |
|----------|------------|---------|--------|
| iPhone 12+ | 60 FPS | 60 FPS | ✅ |
| iPad Pro | 60 FPS | 60 FPS | ✅ |
| Quest 2/3 | 90 FPS | TBD | Planned |

### Optimization Applied

- VSync disabled in Unity (prevents 15 FPS issue)
- Release build only (Debug has framework bug)
- ccache for C++ compilation
- Unity Append mode for incremental IL2CPP

---

## 10. Testing Strategy

### Automated
- Build verification (100%)
- Console error checking (Unity MCP)

### Manual (FINAL_VERIFICATION.md)
1. Login flow
2. Tab navigation
3. Unity bridge (Ping, VFX)
4. AR initialization

### Device Testing
- Primary: iPhone (iPad Pro when available)
- Logs: `./scripts/capture_device_logs.sh`

---

## Quick Reference

### Common Commands

```bash
# Build & Deploy
./scripts/build_minimal.sh

# Clean Build
UNITY_CLEAN_BUILD=1 ./scripts/build_minimal.sh

# Device Logs
./scripts/capture_device_logs.sh 20 "Unity|Bridge"

# Unity Console (via MCP)
read_console(action="get", types=["error"])
```

### Key Files

| Purpose | File |
|---------|------|
| Unity message handler | `unity/Assets/Scripts/BridgeTarget.cs` |
| RN Unity wrapper | `src/components/UnityArView.tsx` |
| Build script | `scripts/build_minimal.sh` |
| Navigation | `src/navigation/RootNavigator.tsx` |
| Auth store | `src/stores/authStore.ts` |

---

## Version History

| Date | Change |
|------|--------|
| 2026-01-13 | Initial comprehensive documentation |
| 2026-01-13 | Unity-RN bridge verified working |
| 2026-01-08 | Spec-kit methodology implemented |
