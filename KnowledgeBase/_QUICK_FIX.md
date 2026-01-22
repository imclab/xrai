# Quick Fix Table

**Usage**: Error → Fix. No explanation. Instant lookup.

---

## C# Compiler Errors

| Error | Fix |
|-------|-----|
| CS0246 type not found | Add `using UnityEngine.VFX;` or relevant namespace |
| CS0103 name not found | Check spelling, add `using`, check scope |
| CS1061 no member | Check Unity version, API changed |
| CS0029 cannot convert | Add explicit cast `(Type)value` |
| CS0120 non-static | Create instance or make method static |

## Unity Runtime

| Error | Fix |
|-------|-----|
| NullReferenceException | Add null check, use `?.` or `??` |
| MissingReferenceException | Check if destroyed, use `if (obj != null)` |
| IndexOutOfRange | Check array bounds before access |
| InvalidOperationException | Check state before operation |

## AR Foundation

| Error | Fix |
|-------|-----|
| AR texture null | Use `TryGetTexture()` pattern |
| AR session not ready | Wait for `ARSession.state == SessionTracking` |
| Subsystem not found | Check XR Plugin Management settings |
| Camera intrinsics null | Use `TryGetIntrinsics()` |
| Depth rotated wrong | Rotate UV: `float2(1-uv.y, uv.x)` for portrait |
| Depth distance wrong | Apply 0.625 scale factor for ARKit |
| Depth → world mismatch | Use RayParams calculation from projection matrix |

## VFX Graph

| Error | Fix |
|-------|-----|
| VFX property not updating | Use `ExposedProperty` not string |
| VFX buffer mismatch | Match buffer count to VFX Capacity |
| VFX not playing | Call `vfx.Play()`, check `enabled` |
| VFX wrong color space | Check project color space settings |
| VFX global texture null | Use `vfx.SetTexture()` per-VFX, not Shader.SetGlobal |
| VFX event creating GC | Cache `VFXEventAttribute` in Start() |
| Sample() not found in HLSL | Use `SampleLevel(tex, uv, 0)` instead |
| VFX attribute error | Wrap with `#if VFX_HAS_ATTR_*` guards |
| Custom HLSL not working | Use `void Func(inout VFXAttributes attrs)` signature |

## Compute Shaders

| Error | Fix |
|-------|-----|
| Dispatch size wrong | Use `CeilToInt(size / threadGroupSize)` |
| Buffer not set | Call `SetBuffer` before Dispatch |
| Thread group query | Use `GetKernelThreadGroupSizes()` |
| Thread group exceeds max | Use `[numthreads(32,32,1)]` (1024 = Metal max) |
| Integer division truncation | Use `CeilToInt()` not integer division |

## Audio VFX

| Error | Fix |
|-------|-----|
| SetPixels GC every frame | Use `NativeArray` + `LoadRawTextureData()` |
| Audio texture wasteful | Use `TextureFormat.RFloat` (4x smaller) |
| Audio data flicker | Set `filterMode = FilterMode.Point` |

## Hand Tracking

| Error | Fix |
|-------|-----|
| XR Hands joint null | Use `TryGetPose(out Pose pose)` |
| Gesture flickering | Add hysteresis (different start/end thresholds) |
| Velocity-driven VFX | `SpawnRate = Lerp(0, 1000, speed / maxSpeed)` |
| Pinch edge detection | Track `wasPinching` state for rising edge |

## WebXR

| Error | Fix |
|-------|-----|
| WebXR session fails | Create context with `{ xrCompatible: true }` |
| Context loss on XR | Handle `webglcontextlost` + `restored` events |
| AR camera → WebRTC | Use `endCameraRendering` + `AsyncGPUReadback` |

## MCP / Tools

| Error | Fix |
|-------|-----|
| MCP timeout | Run `mcp-kill-dupes` |
| Unity MCP not responding | Window > MCP for Unity > Start Server |
| JetBrains MCP slow | Check Rider is open and indexed |
| Multiple Unity instances | Use `set_active_instance()` |
| Huge response payload | Set `generate_preview=false`, `include_properties=false` |
| Slow batch operations | Use `batch_execute` with `parallel=true` |
| Test polling timeout | Use `wait_timeout=60` in `get_test_job` |
| VFX action fails | Check component exists: `particle_*`→ParticleSystem, `vfx_*`→VisualEffect |
| Script edit fails | Use `script_apply_edits` with structured ops |
| Custom tool not found | Place in `Editor/` folder, reconnect MCP |
| Undo not working | Add `Undo.RecordObject()` before modify |
| Async Unity API fails | Wrap in `MainThread.Instance.Run()` |
| Roslyn validation fails | Add `USE_ROSLYN` to Scripting Define Symbols |

## Build / Deploy

| Error | Fix |
|-------|-----|
| iOS code signing | Check Xcode team, provisioning |
| Android SDK not found | Set ANDROID_HOME, check SDK Manager |
| IL2CPP error | Check scripting backend settings |
| WebGL memory | Increase memory size in Player Settings |

## Memory

| Error | Fix |
|-------|-----|
| RenderTexture leak | Add `Release()` in `OnDestroy()` |
| GraphicsBuffer leak | Add `Dispose()` in `OnDestroy()` |
| NativeArray leak | Add `Dispose()` or use `Allocator.Temp` |

## Threading

| Error | Fix |
|-------|-----|
| Main thread only | Use `UnityMainThreadDispatcher` or coroutine |
| Async deadlock | Use `ConfigureAwait(false)` |

---

## Usage Tracking

| Fix | Count | Last Used |
|-----|-------|-----------|
| ExposedProperty | 3 | 2026-01-21 |
| TryGetTexture | 2 | 2026-01-20 |
| mcp-kill-dupes | 5 | 2026-01-21 |

---

## Rapid Debug Loop (MCP)

**30-60% faster debugging** via Unity MCP batch operations.

```
1. read_console(types=["error"], count=5)     → See errors
2. find_in_file() OR get_file_text_by_path()  → Locate source
3. Edit(file, old, new)                       → Apply fix
4. refresh_unity(mode="if_dirty")             → Recompile
5. read_console(types=["error"], count=5)     → Verify fix
```

**Multiple Fixes**: Use `batch_execute` (10-100x faster than individual calls)

```javascript
batch_execute([
  {tool: "manage_script", params: {action: "edit", ...}},
  {tool: "manage_script", params: {action: "edit", ...}},
  {tool: "refresh_unity", params: {mode: "if_dirty"}}
])
```

---

## When to Use AI vs Type Directly

| Task Type | AI Impact | Action |
|-----------|-----------|--------|
| Familiar patterns (VFX, AR) | -19% | Type directly |
| New APIs/frameworks | +35% | Use AI |
| Boilerplate | +55% | Always AI |
| Debugging with MCP | +30-60% | Use rapid loop |

**Source**: METR RCT (arXiv:2507.09089)

---

**Details**: See `_AUTO_FIX_PATTERNS.md` for full explanations.
**Research**: See `_AI_CODING_BEST_PRACTICES.md` for evidence.
