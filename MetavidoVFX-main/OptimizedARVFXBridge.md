# OptimizedARVFXBridge usage

Lightweight AR → VFX bridge (no scene changes required by default). Attach to the AR camera that already has `ARCameraManager` + `AROcclusionManager`.

## Setup
1) Add `OptimizedARVFXBridge` to your AR camera GameObject.
2) Assign your VFX Graph asset to the `vfx` field.
3) Optional: assign a compute shader with a `DepthToWorld` kernel that accepts `_Depth`, `_Stencil`, `_PositionRT`, `_InvVP` and writes world positions to `_PositionRT`. A sample is provided at `Assets/Shaders/DepthToWorld.compute`. If not assigned, the bridge still forwards raw depth/stencil textures and camera matrices.
4) Ensure your VFX Graph has texture properties named (or rename in the inspector):
   - `DepthMap` (Texture2D)
   - `StencilMap` (Texture2D)
   - `PositionMap` (Texture2D, optional if using compute)
   - `InverseView` (Matrix4x4)
   - `InverseProj` (Matrix4x4)
   - `DepthRange` (Vector2)

## Performance knobs
- `adaptiveResolution` (on by default): RT auto scales down if FPS drops below ~80% of target.
- `baseResolution`: default 512x512. Lower for Editor/remote testing (256–384), raise for device if budget allows.
- `targetFPS`: default 60; used for the adaptive threshold and sets `Application.targetFrameRate` + disables vsync.

## Notes
- The component is null-safe: if no compute is provided, it simply forwards depth/stencil to VFX.
- Uses environment depth when available; falls back to human depth; stencil is human-only.
- Depth range default is (0.1, 10); adjust in the inspector if your effect expects another range.
