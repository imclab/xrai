# Testing & Debugging Checklist

> Quick reference for triple-verified testing workflow.

---

## Triple-Verified Workflow

| Step | Tool | Check |
|------|------|-------|
| 1. **Console** | `mcp__UnityMCP__read_console` | No compilation errors |
| 2. **Play Mode** | `mcp__UnityMCP__manage_editor(action="play")` | VFX renders correctly |
| 3. **Device** | `./build_and_deploy.sh` | Works on iOS device |

---

## Quick Debug Commands

```csharp
// ARDepthSource - check in inspector or add:
[ContextMenu("Debug Source")]
void DebugSource() {
    Debug.Log($"DepthMap: {DepthMap} ({DepthMap?.width}x{DepthMap?.height})");
    Debug.Log($"PositionMap: {PositionMap?.width}x{PositionMap?.height}");
    Debug.Log($"RayParams: {RayParams}");
}

// VFXARBinder - verify binding:
[ContextMenu("Debug Binder")]
void DebugBinder() {
    var vfx = GetComponent<VisualEffect>();
    Debug.Log($"Has DepthMap: {vfx.HasTexture(Shader.PropertyToID("DepthMap"))}");
    Debug.Log($"Has PositionMap: {vfx.HasTexture(Shader.PropertyToID("PositionMap"))}");
    Debug.Log($"Source: {ARDepthSource.Instance != null}");
}
```

---

## Verbose Logging Pattern

```csharp
[Header("Debug")]
[Tooltip("Enable verbose logging")]
public bool verboseLogging = false;

void Update() {
    if (verboseLogging && Time.frameCount % 60 == 0) // Log every second
        Debug.Log($"[{name}] Status: {GetStatus()}");
}
```

---

## Common Issues

| Issue | Check | Fix |
|-------|-------|-----|
| VFX not rendering | `ARDepthSource.Instance == null` | Add ARDepthSource to scene |
| Particles at origin | `PositionMap == null` | Verify compute shader dispatch |
| Stretched particles | `RayParams.z != RayParams.w * aspect` | Check aspect ratio calculation |
| No stencil masking | `StencilMap == null` | AROcclusionManager not configured |

---

## MCP Testing Tools

```
# Check console for errors
mcp__UnityMCP__read_console(types=["error"])

# Enter play mode
mcp__UnityMCP__manage_editor(action="play")

# Find VFX in scene
mcp__UnityMCP__find_gameobjects(search_term="VisualEffect", search_method="by_component")

# Check component properties
mcp__UnityMCP__manage_components(action="get", target="VFXName", component_type="VisualEffect")
```

---

## Performance Targets

| Metric | Target | Warning | Critical |
|--------|--------|---------|----------|
| FPS | 60 | 45 | 30 |
| Compute/frame | <2ms | <4ms | >6ms |
| Binding/VFX | <0.1ms | <0.2ms | >0.5ms |
| Memory | <2GB | <3GB | >4GB |

---

## Pre-Commit Checklist

- [ ] `read_console` shows no errors
- [ ] Play mode tested (30+ seconds)
- [ ] VFX renders at correct positions
- [ ] RayParams logged and verified
- [ ] No memory leaks (check RenderTexture disposal)
- [ ] `verboseLogging = false` before commit
