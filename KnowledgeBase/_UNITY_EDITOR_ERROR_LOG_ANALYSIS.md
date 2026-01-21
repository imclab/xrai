# Unity Editor Error Log Analysis

**Date**: 2026-01-21
**Project**: MetavidoVFX-main
**Unity Version**: 6000.2.14f1
**Status**: Active Issue Tracking

---

## Executive Summary

Analysis of Unity Editor crash logs revealed **4 categories** of errors requiring attention:
1. HoloKit native plugin entry point failures
2. NullReferenceExceptions in VFX/AR operations
3. Package manifest warnings
4. LINQ empty sequence errors

---

## 1. HoloKit Native Plugin Errors

### Error Messages
```
EntryPointNotFoundException: HoloKit_AppleVisionHandPoseDetector_initWithARSession
EntryPointNotFoundException: RegisterDebugLog
```

### Root Cause
HoloKit SDK native iOS plugins are not available in the Unity Editor environment. These functions are implemented in native iOS code (`.framework` or `.a` files) that only execute on device.

### Impact
- **Severity**: Medium (Editor-only)
- **Runtime**: No impact on iOS builds
- **Development**: Blocks Editor testing of hand tracking features

### Recommended Fix
Wrap HoloKit calls in platform-conditional compilation:

```csharp
#if UNITY_IOS && !UNITY_EDITOR
    HoloKit_AppleVisionHandPoseDetector_initWithARSession(session);
#else
    Debug.Log("[HoloKit] Native hand tracking not available in Editor");
#endif
```

Or use `Application.isEditor`:
```csharp
if (!Application.isEditor)
{
    // Native plugin calls here
}
```

### Files to Check
- Any script referencing `HoloKit_AppleVision*` methods
- HoloKit SDK initialization code
- Hand pose detection components

---

## 2. NullReferenceExceptions

### Common Patterns Found

#### Pattern A: AR Texture Access
```
NullReferenceException: Object reference not set to an instance of an object
  at UnityEngine.VFX.VisualEffect.SetTexture()
```

**Cause**: Attempting to set AR textures (depth, stencil, color) before they're available.

**Fix**: Already implemented in ARDepthSource.cs with TryGetTexture pattern:
```csharp
Texture TryGetTexture(Func<Texture> getter)
{
    try { return getter?.Invoke(); }
    catch { return null; }
}
```

#### Pattern B: VFX Component Missing
```
NullReferenceException at VFXLibraryManager
```

**Cause**: VFX component destroyed or not yet initialized.

**Fix**: Null-check before operations:
```csharp
if (entry?.VFX != null && entry.VFX.enabled)
{
    entry.VFX.SetTexture(...);
}
```

#### Pattern C: First() on Empty Collection
```
InvalidOperationException: Sequence contains no elements
```

**Cause**: LINQ `First()` called on empty collection.

**Fix**: Use `FirstOrDefault()` with null check:
```csharp
// Bad
var item = collection.First();

// Good
var item = collection.FirstOrDefault();
if (item != null)
{
    // Use item
}
```

### Files Requiring Validation
| File | Pattern | Status |
|------|---------|--------|
| ARDepthSource.cs | A | FIXED (TryGetTexture) |
| VFXARBinder.cs | A | FIXED |
| VFXLibraryManager.cs | B, C | NEEDS REVIEW |
| HumanParticleVFX.cs | A | FIXED |
| MeshVFX.cs | A | FIXED |
| HandVFXController.cs | A | FIXED |

---

## 3. Package Manifest Warnings

### Warning Messages
```
[WARN] Package folder [.../Packages/Coplay] missing package manifest.
[WARN] Package folder [.../Packages/Microsoft.CodeAnalysis.Analyzers.3.11.0] missing package manifest.
```

### Root Cause
Package folders exist but lack required `package.json` files.

### Impact
- **Severity**: Low
- **Runtime**: No impact
- **UPM**: Package Manager can't recognize these as valid packages

### Recommended Fixes

#### Option A: Create package.json files

For `Packages/Coplay/package.json`:
```json
{
  "name": "com.local.coplay",
  "version": "1.0.0",
  "displayName": "Coplay",
  "description": "Local Coplay package",
  "unity": "6000.2"
}
```

For `Packages/Microsoft.CodeAnalysis.Analyzers.3.11.0/package.json`:
```json
{
  "name": "com.microsoft.codeanalysis.analyzers",
  "version": "3.11.0",
  "displayName": "Microsoft CodeAnalysis Analyzers",
  "description": "Roslyn analyzers for code quality"
}
```

#### Option B: Move to Assets/Plugins
If these aren't true UPM packages, move them to `Assets/Plugins/` instead.

#### Option C: Remove if unused
If packages are no longer needed, delete the folders.

---

## 4. Native Crash Information

### Crash Log Excerpt
```
=================================================================
Native Crash Reporting
=================================================================
Got a SIGABRT while executing native code.
```

### Potential Causes
1. **Memory corruption** - Buffer overflows in native plugins
2. **Thread safety** - Accessing Unity objects from background threads
3. **AR session teardown** - Improper cleanup of ARKit/ARCore sessions

### Debugging Steps
1. Enable `Development Build` and `Script Debugging`
2. Check Xcode console for more details
3. Review crash reports in `~/Library/Logs/DiagnosticReports/`
4. Look for pattern: crash after AR session starts/stops

### Mitigation
```csharp
// Safe AR session cleanup
void OnDestroy()
{
    if (arSession != null && arSession.enabled)
    {
        arSession.enabled = false;
        // Wait a frame before destroying
        StartCoroutine(SafeDestroy());
    }
}

IEnumerator SafeDestroy()
{
    yield return null; // Wait one frame
    Destroy(arSession.gameObject);
}
```

---

## 5. Error Prevention Checklist

### Before Play Mode
- [ ] Verify AR Foundation managers are in scene
- [ ] Check HoloKit components have Editor guards
- [ ] Ensure VFXLibrary has rebuilt from children

### During Development
- [ ] Use `?.` null-conditional operators
- [ ] Use `FirstOrDefault()` instead of `First()`
- [ ] Wrap native plugin calls in `#if !UNITY_EDITOR`
- [ ] Validate textures before passing to VFX

### Before Build
- [ ] Check Console for warnings (0 errors required)
- [ ] Run `Tools > VFX > Validate VFX Bindings`
- [ ] Clear UPM cache if package warnings persist

---

## 6. Quick Reference: Error → Solution

| Error Type | Quick Fix |
|------------|-----------|
| `EntryPointNotFoundException` | `#if !UNITY_EDITOR` guard |
| `NullReferenceException: SetTexture` | Use TryGetTexture pattern |
| `InvalidOperationException: Sequence` | Use `FirstOrDefault()` |
| `Package missing manifest` | Add package.json or move to Plugins |
| `SIGABRT native crash` | Check AR session lifecycle |

---

## 7. Log File Locations

| Log | Path | Purpose |
|-----|------|---------|
| Editor.log | `~/Library/Logs/Unity/Editor.log` | Current session |
| Editor-prev.log | `~/Library/Logs/Unity/Editor-prev.log` | Previous session |
| upm.log | `~/Library/Logs/Unity/upm.log` | Package manager |
| Crash reports | `~/Library/Logs/DiagnosticReports/` | Native crashes |
| Player.log | `~/Library/Logs/Unity/Player.log` | Standalone builds |

---

## References

- [Unity Debug Log](https://docs.unity3d.com/Manual/LogFiles.html)
- [AR Foundation Troubleshooting](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.2/manual/troubleshooting.html)
- [HoloKit SDK Documentation](https://holokit.io/docs)
- [VFX Graph Best Practices](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.0/manual/BestPractices.html)

---

*Last Updated: 2026-01-21*
