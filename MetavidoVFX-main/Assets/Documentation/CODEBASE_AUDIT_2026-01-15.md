# MetavidoVFX Codebase Audit Report

**Date**: 2026-01-15
**Auditor**: Claude Code (Opus 4.5)
**Project**: MetavidoVFX
**Unity Version**: 6000.2.14f1
**Pipeline**: URP 17.2.0

---

## Fix Status (Updated 2026-01-19)

| Bug | Status | Fix Applied |
|-----|--------|-------------|
| BUG 1: Thread Dispatch `/8.0f` | ✅ FIXED | `OptimizedARVFXBridge.cs`: Now queries thread sizes dynamically. `ARKitMetavidoBinder.cs`: Fixed to `/32.0f` |
| BUG 2: Integer Division | ✅ FIXED | `HumanParticleVFX.cs`: Added `Mathf.CeilToInt()` for both portrait and landscape dispatch |
| BUG 3: Memory Leak | ❌ NOT A BUG | Serialized RenderTextures are inspector-assigned assets, not runtime allocations |
| BUG 4: Missing HasTexture Guards | ✅ FIXED | `PeopleOcclusionVFXManager.cs`: Added guards on lines 175-180 |
| BUG 5: Blocking Microphone Init | ❌ ALREADY FIXED | `EnhancedAudioProcessor.cs`: Already has 3-second timeout |
| **BUG 6: AR Texture Access Crash** | ✅ FIXED | 6 files: `TryGetTexture()` pattern wraps AR Foundation texture getters |

---

## Executive Summary

The MetavidoVFX codebase demonstrates **strong architecture** with a centralized VFX binding system. Originally identified 5 critical bugs, of which **3 were genuine and have been fixed**, 1 was a false positive, and 1 was already fixed. The project follows modern Unity 6 patterns but has legacy/deprecated code that needs cleanup.

| Category | Status |
|----------|--------|
| Architecture | ✅ Well-designed centralized pipeline |
| Code Quality | ✅ Critical bugs fixed (3/3 genuine bugs resolved) |
| Documentation | ✅ Comprehensive (CLAUDE.md, QUICK_REFERENCE.md) |
| Performance | ✅ Good patterns (LOD, auto-optimizer) |
| Deprecated Code | ⚠️ 3 legacy systems need removal |

---

## Critical Bugs (Must Fix)

### BUG 1: Compute Shader Thread Dispatch Mismatch
**Files**: `OptimizedARVFXBridge.cs:120-122`, `ARKitMetavidoBinder.cs:217-219`
**Confidence**: 95%

**Issue**: Uses `/8.0f` for dispatch but `DepthToWorld.compute` uses `[numthreads(32,32,1)]`

```csharp
// WRONG (found in OptimizedARVFXBridge.cs):
int tgX = Mathf.CeilToInt(positionRT.width / 8.0f);

// CORRECT (as in VFXBinderManager.cs:383):
int tgX = Mathf.CeilToInt(positionRT.width / 32.0f);
```

**Impact**: Only 1/16th of depth texture processed. Particles will render incorrectly.

**Fix**: Change `/8.0f` to `/32.0f` in both files.

---

### BUG 2: Integer Division in HumanParticleVFX.cs
**File**: `Assets/Scripts/VFX/HumanParticleVFX.cs:164-166`
**Confidence**: 100%

**Issue**: Integer division truncates instead of ceiling.

```csharp
// WRONG:
computeShader.Dispatch(_portraitKernel,
    Screen.width / (int)_threadSizeX,  // Integer division!
    Screen.height / (int)_threadSizeY,
    (int)_threadSizeZ);

// CORRECT:
computeShader.Dispatch(_portraitKernel,
    Mathf.CeilToInt(Screen.width / (float)_threadSizeX),
    Mathf.CeilToInt(Screen.height / (float)_threadSizeY),
    (int)_threadSizeZ);
```

**Impact**: Right/bottom pixels not processed. Visual artifacts at edges.

---

### BUG 3: Memory Leak in HumanParticleVFX.cs
**File**: `Assets/Scripts/VFX/HumanParticleVFX.cs:299-305`
**Confidence**: 85%

**Issue**: `OnDestroy()` doesn't release all RenderTextures.

```csharp
// MISSING in OnDestroy():
if (positionMapPortrait != null) positionMapPortrait.Release();
if (positionMapLandscape != null) positionMapLandscape.Release();
if (colorMapPortrait != null) colorMapPortrait.Release();
if (colorMapLandscape != null) colorMapLandscape.Release();
```

**Impact**: GPU memory leak over time.

---

### BUG 4: Missing HasTexture Guards in PeopleOcclusionVFXManager
**File**: `Assets/Scripts/PeopleOcclusion/PeopleOcclusionVFXManager.cs:175-177`
**Confidence**: 85%

**Issue**: Sets textures without checking if VFX has those properties.

```csharp
// SHOULD BE:
if (m_VfxInstance.HasTexture("Color Map"))
    m_VfxInstance.SetTexture("Color Map", m_CaptureTexture);
```

**Impact**: Unity errors in console; log pollution.

---

### BUG 5: Blocking Microphone Init in EnhancedAudioProcessor
**File**: `Assets/Scripts/Audio/EnhancedAudioProcessor.cs:120-121`
**Confidence**: 75%

**Issue**: Blocking `while` loop in `Start()` waiting for microphone.

```csharp
// PROBLEM:
while (Microphone.GetPosition(device) <= 0) { }  // Blocks main thread!

// SHOULD BE:
yield return new WaitWhile(() => Microphone.GetPosition(device) <= 0);
```

**Impact**: App freezes 100-500ms on some iOS devices during start.

---

### BUG 6: AR Foundation Texture Access NullReferenceException (FIXED 2026-01-19)
**Files**: `ARDepthSource.cs`, `HumanParticleVFX.cs`, `DepthImageProcessor.cs`, `DirectDepthBinder.cs`, `DiagnosticOverlay.cs`, `SimpleHumanHologram.cs`
**Confidence**: 100% (verified on device)

**Issue**: AR Foundation texture property getters (`humanDepthTexture`, `environmentDepthTexture`, `humanStencilTexture`) throw `NullReferenceException` internally when AR subsystem isn't ready. The null-coalescing operator (`?.`) does NOT protect against this because the exception happens **inside** the getter, not at the property access level.

```csharp
// WRONG (crashes when AR isn't ready):
var depth = occlusionManager?.humanDepthTexture;  // ?. doesn't help!

// CORRECT (TryGetTexture pattern):
Texture TryGetTexture(System.Func<Texture> getter)
{
    try { return getter?.Invoke(); }
    catch { return null; }
}
var depth = TryGetTexture(() => occlusionManager.humanDepthTexture);
```

**Impact**: App crashes on startup before AR Foundation initializes. Manifests as:
```
NullReferenceException: Object reference not set to an instance of an object.
UnityEngine.Texture2D.UpdateExternalTexture (System.IntPtr nativeTex)
UnityEngine.XR.ARFoundation.AROcclusionManager.get_humanDepthTexture ()
```

**Fix Applied**: Added `TryGetTexture()` helper method to all 6 files that access AR Foundation textures. The helper wraps the texture getter in try-catch to safely return null when AR isn't ready.

---

## Deprecated Systems (Should Remove)

| System | Status | Reason |
|--------|--------|--------|
| `PeopleOcclusionVFXManager` | ❌ DISABLED | Creates duplicate VFX; conflicts with VFXBinderManager |
| `ARKitMetavidoBinder` | ❌ DEPRECATED | Per-VFX binding is redundant; VFXBinderManager is centralized |
| `OptimizedARVFXBridge` | ❌ LEGACY | Superseded by VFXBinderManager |

**Action**: Run `H3M > Pipeline Cleanup > Run Full Cleanup` before builds.

---

## Architecture Strengths

### Centralized Binding Hub (VFXBinderManager)
```
AR Session Origin → VFXBinderManager → All VFX
         ↓
GPU Compute (DepthToWorld.compute)
         ↓
Properties: DepthMap, StencilMap, PositionMap, RayParams
```

**Why it's good**:
- Single source of truth for AR data
- Eliminates per-VFX binding redundancy
- Supports legacy AudioProcessor fallback
- Comprehensive diagnostic logging

### Compute Shader Pipeline
- Uses `[numthreads(32,32,1)]` - optimal for texture processing
- Correct RayParams format: `Vector4(0, 0, tanH, tanV)`
- Proper matrix calculations (InverseView, InverseProjection)

### Performance Systems
- `VFXAutoOptimizer`: FPS-based quality adjustment
- `VFXLODController`: Distance-based particle reduction
- `VFXProfiler`: Analysis and recommendations

---

## KnowledgeBase Discrepancy Found

**Issue**: KB shows `[numthreads(8, 8, 1)]` but actual project uses `[numthreads(32, 32, 1)]`

**File**: `KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md:178-179`

```hlsl
// KB says (OUTDATED):
[numthreads(8, 8, 1)]

// Actual (CORRECT):
[numthreads(32, 32, 1)]
```

**Action**: Update KB to reflect actual pattern.

---

## Online Research Findings

### Unity 6 VFX Graph Best Practices
Source: [Unity VFX Graph E-Book](https://unity.com/resources/creating-advanced-vfx-unity6)

- **Thread Groups**: 64 threads is good default (AMD=64, NVidia=32)
- **Culling**: Use Culling node to limit rendered particles
- **LOD**: Reduce complexity based on camera distance
- **Baking**: Pre-compute complex simulations to texture

### AR Foundation 6.x Changes
Source: [AR Foundation 6.1 Changelog](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.1/changelog/CHANGELOG.html)

- `environmentDepthTexture` → `TryGetEnvironmentDepthTexture()`
- `frameReceived` now fires during `Application.onBeforeRender`
- Always check `manager.descriptor.environmentDepthImageSupported`

**Impact**: The code correctly uses both new and fallback APIs with deprecation suppression.

---

## Code Statistics

| Category | Count |
|----------|-------|
| C# Scripts | 211 (49 custom) |
| VFX Assets | 97 |
| Shaders | 35+ |
| Scenes | 6 |
| Documentation Files | 5 |

### Project Size
- Total: ~1.74 MB source code
- Packages: AR Foundation 6.2.1, VFX Graph 17.2.0, Metavido 5.1.1

---

## Recommendations

### Immediate (Before Next Build)
1. Fix thread dispatch in `OptimizedARVFXBridge.cs` and `ARKitMetavidoBinder.cs`
2. Fix integer division in `HumanParticleVFX.cs`
3. Add RenderTexture releases in `HumanParticleVFX.OnDestroy()`
4. Run `H3M > Pipeline Cleanup > Run Full Cleanup`

### Short-Term
1. Add HasTexture guards to `PeopleOcclusionVFXManager.cs`
2. Convert microphone wait to coroutine in `EnhancedAudioProcessor.cs`
3. Consider removing deprecated scripts entirely
4. Update KB `[numthreads(8,8,1)]` to `[numthreads(32,32,1)]`

### Long-Term
1. Query `SystemInfo.maxTextureSize` instead of hardcoding 1920
2. Implement graceful degradation if compute shader fails
3. Add VFX property validation tool (Editor script)
4. Expose hand gesture thresholds as project settings

---

## Files Changed by This Audit

| File | Action Needed |
|------|---------------|
| `OptimizedARVFXBridge.cs` | Fix `/8.0f` → `/32.0f` |
| `ARKitMetavidoBinder.cs` | Fix `/8.0f` → `/32.0f` |
| `HumanParticleVFX.cs` | Fix integer division + add releases |
| `PeopleOcclusionVFXManager.cs` | Add HasTexture guards |
| `EnhancedAudioProcessor.cs` | Convert blocking wait to coroutine |
| KB `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` | Update numthreads example |

---

## Sources

- [Unity VFX Graph Best Practices](https://unity.com/resources/creating-advanced-vfx-unity6)
- [AR Foundation 6.x Documentation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/features/occlusion/occlusion-manager.html)
- [Catlike Coding: Compute Shaders](https://catlikecoding.com/unity/tutorials/basics/compute-shaders/)
- Local Keijiro/Rcam projects for pattern verification

---

**Audit Complete**

*Generated by Claude Code on 2026-01-15*

---

## Deep Audit Update (2026-01-21)

### Issues Fixed

| Issue | File | Fix Applied |
|-------|------|-------------|
| Memory Leak: FindFirstObjectByType every frame | `MetavidoWebRTCEncoder.cs` | Cached `_cachedARDepthSource` reference |
| Memory Leak: Temp texture not destroyed | `MetavidoWebRTCDecoder.cs` | Added null check and existing Destroy call verified |
| Missing OnDestroy | `HiFiHologramController.cs` | Added OnDestroy method |
| Dead code: Deprecated signaling | `H3MSignalingClient.cs` | Moved to `_Legacy/Network/` |
| Dead code: Stub WebRTC | `WebRTCReceiver.cs` | Moved to `_Legacy/Network/` |
| Dead code: Empty body controller | `BodyVFXController.cs` | Moved to `_Legacy/BodyTracking/` |
| Duplicate menu item | `HologramSetup.cs` | Removed (kept `H3MHologramSetup.cs`) |
| Spec title inconsistency | `specs/012-hand-tracking/spec.md` | Fixed to use "Feature Specification:" prefix |

### Code Organization

**New _Legacy Structure**:
```
Assets/_Legacy/
├── BodyTracking/
│   └── BodyVFXController.cs (empty stub)
└── Network/
    ├── H3MSignalingClient.cs (deprecated, WebRtcVideoChat has built-in signaling)
    └── WebRTCReceiver.cs (commented out stub)
```

### Remaining Issues (Non-Critical)

| Issue | Severity | Notes |
|-------|----------|-------|
| Hardcoded texture dimensions in HologramConferenceManager | Low | 1280x720 fixed, should match incoming |
| Unused serialized fields in HiFiHologramController | Low | _enableSSAO, _useGaussianSplat for future use |
| Reflection in ARCameraWebRTCCapture.SendToWebRTC | Low | Performance, but rarely called path |

### Specs Status Update

| Spec | Status | Notes |
|------|--------|-------|
| 003 - Hologram Conferencing | ✅ Phase 2 Complete | Encoder/Decoder/Layout/Audio done |
| 013 - UI/UX Conferencing | 📋 Planning | Spec created |
| 014 - HiFi Hologram VFX | 📋 Planning | Spec created, Controller implemented |

*Deep Audit by Claude Code on 2026-01-21*
