# Auto-Fix Patterns

**Version**: 1.0 (2026-01-21)
**Purpose**: Automatically applicable fixes for common errors.
**Usage**: AI tools apply these fixes without user confirmation.

---

## Quick Reference

| Error Code | Issue | Fix |
|------------|-------|-----|
| CS0246 | Type not found | Add using statement |
| CS0103 | Name doesn't exist | Check scope/typo |
| CS1061 | No member found | Check API version |
| NullRef | Null reference | Add null guard |
| VFX Property | Property not updating | Use ExposedProperty |

---

## Unity C# Errors

### CS0246 - Type or namespace not found

**Detection**: `error CS0246.*type or namespace.*could not be found`

**Common Fixes**:
| Missing Type | Add Using |
|--------------|-----------|
| `VisualEffect` | `using UnityEngine.VFX;` |
| `ARSession` | `using UnityEngine.XR.ARFoundation;` |
| `XROrigin` | `using Unity.XR.CoreUtils;` |
| `ARCameraManager` | `using UnityEngine.XR.ARFoundation;` |
| `ARPlaneManager` | `using UnityEngine.XR.ARFoundation;` |
| `AROcclusionManager` | `using UnityEngine.XR.ARFoundation;` |
| `HandJoint` | `using UnityEngine.XR.Hands;` |
| `NativeArray` | `using Unity.Collections;` |
| `JobHandle` | `using Unity.Jobs;` |
| `float3` | `using Unity.Mathematics;` |
| `IJobParallelFor` | `using Unity.Jobs;` |
| `PhotonNetwork` | `using Photon.Pun;` |

**Auto-Apply**: Yes (add using statement at top of file)

### CS0103 - Name does not exist in current context

**Detection**: `error CS0103.*name.*does not exist`

**Common Causes**:
1. Variable not declared
2. Typo in variable name
3. Wrong scope (private when should be public)
4. Missing `this.` for class members

**Auto-Apply**: Partial (typo correction, scope suggestions)

### CS1061 - Type does not contain definition

**Detection**: `error CS1061.*does not contain a definition`

**Common Fixes**:
| Old API | New API |
|---------|---------|
| `ARSessionOrigin` | `XROrigin` (AR Foundation 5+) |
| `ARRaycastManager.Raycast` | Check `TrackableType` enum |
| `Application.isPlaying` | Use `#if UNITY_EDITOR` |

**Auto-Apply**: Partial (API migration suggestions)

---

## AR Foundation Fixes

### NullReferenceException in AR Texture Access

**Detection**: `NullReferenceException` + `Texture` + AR stack trace

**Pattern**:
```csharp
// BEFORE (crashes)
var texture = cameraManager.GetTextureDescriptor().nativeTexture;

// AFTER (safe)
if (cameraManager.TryGetIntrinsics(out var intrinsics))
{
    // Safe to proceed
}
```

**Auto-Apply**: Yes (wrap with TryGet pattern)

### AR Session Not Ready

**Detection**: AR operations fail at startup

**Fix**:
```csharp
IEnumerator WaitForARSession()
{
    while (ARSession.state < ARSessionState.SessionTracking)
        yield return null;
    // Now safe to use AR
}
```

**Auto-Apply**: Yes (add coroutine wrapper)

---

## VFX Graph Fixes

### VFX Property Not Updating

**Detection**: VFX property set but visual unchanged

**Cause**: Using string instead of ExposedProperty

**Pattern**:
```csharp
// BEFORE (may fail at runtime)
vfx.SetVector3("Position", pos);

// AFTER (correct)
static readonly ExposedProperty PositionProp = "Position";
vfx.SetVector3(PositionProp, pos);
```

**Auto-Apply**: Yes (convert to ExposedProperty)

### VFX Buffer Size Mismatch

**Detection**: VFX particles disappear or flicker

**Fix**: Ensure buffer count matches particle capacity
```csharp
graphicsBuffer = new GraphicsBuffer(
    GraphicsBuffer.Target.Structured,
    vfx.GetInt("Capacity"),  // Match VFX capacity
    stride
);
```

**Auto-Apply**: Partial (suggest capacity check)

---

## Thread/Dispatch Fixes

### Main Thread Dispatch

**Detection**: `UnityException.*can only be called from the main thread`

**Fix**:
```csharp
// Queue to main thread
UnityMainThreadDispatcher.Instance().Enqueue(() => {
    // Unity API calls here
});
```

**Auto-Apply**: Yes (wrap with dispatcher)

### Compute Shader Thread Mismatch

**Detection**: Compute results incorrect or partial

**Fix**: Query actual thread group size
```csharp
shader.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out uint z);
int groupsX = Mathf.CeilToInt(width / (float)x);
int groupsY = Mathf.CeilToInt(height / (float)y);
shader.Dispatch(kernel, groupsX, groupsY, 1);
```

**Auto-Apply**: Yes (add dynamic thread group calculation)

---

## Memory/Performance Fixes

### RenderTexture Leak

**Detection**: Memory grows over time, RenderTexture warnings

**Fix**:
```csharp
void OnDestroy()
{
    if (renderTexture != null)
    {
        renderTexture.Release();
        renderTexture = null;
    }
}
```

**Auto-Apply**: Yes (add OnDestroy cleanup)

### GraphicsBuffer Leak

**Detection**: Memory grows, buffer warnings

**Fix**:
```csharp
void OnDestroy()
{
    graphicsBuffer?.Dispose();
    graphicsBuffer = null;
}
```

**Auto-Apply**: Yes (add disposal)

---

## MCP/Integration Fixes

### MCP Server Not Responding

**Detection**: MCP tool timeout or connection refused

**Fix**:
```bash
# Kill duplicate servers
mcp-kill-dupes

# Verify server running
lsof -i :6400  # Unity MCP
lsof -i :63342 # JetBrains
```

**Auto-Apply**: Yes (run mcp-kill-dupes)

### Unity MCP Multiple Instances

**Detection**: "Multiple Unity instances connected"

**Fix**: Set active instance before operations
```
set_active_instance(name="ProjectName@hash")
```

**Auto-Apply**: Yes (auto-select most recent)

---

## Tool Usage Fixes

### Grep Instead of Glob for File Search

**Detection**: Using Grep to find files by name

**Fix**: Use Glob for filename patterns
```
# BEFORE (inefficient)
Grep pattern: "FileName"

# AFTER (correct)
Glob pattern: "**/FileName*.cs"
```

**Auto-Apply**: Suggestion only

### Read Full File When Only Part Needed

**Detection**: Read 2000+ lines, used <100

**Fix**: Add offset and limit
```
Read(file, offset=0, limit=100)
```

**Auto-Apply**: Suggestion for next time

---

## Self-Healing Triggers

| Trigger | Auto-Fix Action |
|---------|-----------------|
| Same error 3+ times | Add to this file |
| Pattern not found | Create pattern entry |
| Fix outdated | Update fix instructions |
| New Unity version | Review API changes |

---

## Adding New Patterns

When adding new auto-fix patterns:

```markdown
### [Error Name]

**Detection**: [regex or description]
**Cause**: [why this happens]
**Fix**: [code or steps]
**Auto-Apply**: Yes/Partial/No + reason
```

---

**Last Updated**: 2026-01-21
**Patterns**: 15 active
**Auto-Apply Rate**: 80%
