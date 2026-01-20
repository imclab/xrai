# Feature Specification: AR Foundation Texture Safety Pattern

**Feature Branch**: `005-ar-texture-safety`
**Created**: 2026-01-20
**Status**: Implemented ✅
**Input**: Fix NullReferenceException crash when accessing AR Foundation textures before subsystem ready

## Triple Verification (2026-01-20)

| Source | Status | Notes |
|--------|--------|-------|
| KB `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` | ✅ Verified | Pattern added to KB |
| KB `LEARNING_LOG.md` | ✅ Verified | Entry logged |
| Online: [AR Foundation 6.2 Docs](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.2/manual/features/occlusion/occlusion-manager.html) | ✅ Verified | Texture access requires subsystem ready |
| Device Testing (iPhone 15 Pro) | ✅ Verified | No crash on startup |

## Overview

AR Foundation texture property getters throw NullReferenceException internally when AR subsystem isn't ready. The `?.` operator doesn't protect because the exception occurs inside the getter. This spec documents the TryGetTexture pattern solution.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - App Startup Without Crash (Priority: P1)

As a user, I want the app to start without crashing, even if AR takes time to initialize.

**Why this priority**: Crash on startup = unusable app

**Independent Test**:
1. Launch app on iOS device
2. App should display without crash
3. AR features enable once subsystem ready

**Acceptance Scenarios**:
1. **Given** AR subsystem not ready, **When** app starts, **Then** no crash occurs
2. **Given** AR initializing, **When** texture accessed, **Then** returns null safely
3. **Given** AR ready, **When** texture accessed, **Then** returns valid texture

---

### User Story 2 - Graceful AR Initialization (Priority: P2)

As a developer, I want safe texture access that doesn't require timing hacks.

**Why this priority**: Clean code > workarounds

**Independent Test**:
1. Access `humanDepthTexture` before AR ready
2. Should return null, not throw
3. Access again after AR ready - returns texture

**Acceptance Scenarios**:
1. **Given** TryGetTexture pattern, **When** getter throws internally, **Then** catch returns null

---

### Edge Cases

- AR never initializes (device doesn't support) → returns null forever
- AR disabled mid-session → returns null until re-enabled
- Multiple rapid calls → all safe, no race conditions

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST NOT crash when AR subsystem unavailable
- **FR-002**: TryGetTexture MUST wrap all AR texture property access
- **FR-003**: Pattern MUST work with `humanDepthTexture`, `humanStencilTexture`, `environmentDepthTexture`
- **FR-004**: Callers MUST handle null return gracefully

### Key Pattern

```csharp
Texture TryGetTexture(System.Func<Texture> getter)
{
    try { return getter?.Invoke(); }
    catch { return null; }
}

// Usage:
var depth = TryGetTexture(() => occlusionManager.humanDepthTexture);
```

### Files Requiring Pattern

| File | Texture Access |
|------|----------------|
| ARDepthSource.cs | humanDepthTexture, environmentDepthTexture |
| SimpleHumanHologram.cs | humanDepthTexture |
| DiagnosticOverlay.cs | humanDepthTexture, humanStencilTexture |
| DirectDepthBinder.cs | humanDepthTexture |
| HumanParticleVFX.cs | humanDepthTexture |
| DepthImageProcessor.cs | humanStencilTexture |

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: App launches without crash on 100% of supported devices
- **SC-002**: Zero NullReferenceException in AROcclusionManager stack traces
- **SC-003**: AR features work correctly once subsystem initializes
- **SC-004**: No performance impact (try-catch is negligible)

## Implementation Notes

**Root Cause**: AR Foundation 6.x texture getters call internal methods that throw when subsystem handle is null. The exception happens inside the property getter, not at access point.

**Stack Trace**:
```
NullReferenceException: Object reference not set
UnityEngine.Texture2D.UpdateExternalTexture()
UnityEngine.XR.ARFoundation.AROcclusionManager.get_humanDepthTexture()
```

**Commits**: `ed280f8d2` (code), `945cfb8fa` (docs), `dbf20899d` (KB)
