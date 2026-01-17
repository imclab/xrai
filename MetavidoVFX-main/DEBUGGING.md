# Debugging & Workflow Guide

## 1. Editor Testing (AR Foundation Remote 2)
Fastest way to iterate. connect your device running the *AR Companion App* to Unity Editor.

### Setup
1.  **Install App**: Build & Run `Assets/Plugins/ARFoundationRemoteInstaller/Installer` scene to device.
2.  **Connect**: Open `Window > AR Foundation Remote > Connection`.
3.  **Play**: Press Play in Editor. Input from phone (Camera, LiDAR, Gyro) drives the Unity Scene.

### Common Issues
*   **Black Screen**: Ensure "Enable AR Remote" is checked in Project Settings.
*   **Lag**: Reduce "Video Resolution" in AR Remote settings on device.

---

## 2. Pipeline Debug Flow (ARDepthSource -> VFXARBinder)

Use this checklist before deeper investigation.

### Step 1: Verify ARDepthSource
1. Ensure `ARDepthSource` exists and is enabled in the scene.
2. Right-click ARDepthSource and run `Debug Source`.
3. Confirm `DepthMap`, `PositionMap`, and `RayParams` are non-null and update.
4. If in Editor with no device, enable `Use Mock Data In Editor`.
5. For portrait orientation issues, toggle `Rotate Depth Texture`.

### Step 2: Verify VFXARBinder
1. Ensure each `VisualEffect` has a `VFXARBinder` component.
2. Run `Auto-Detect Bindings` to match exposed properties.
3. Check `IsBound` and `BoundCount` in the inspector.
4. Verify common property names: `DepthMap`, `StencilMap`, `PositionMap`, `RayParams`, `InverseView`, `DepthRange`.

### Step 3: Verify VFX Graph Inputs
1. Confirm the VFX graph exposes the properties you expect (names must match exactly).
2. If using `PositionMap`, ensure the graph samples it rather than raw depth.
3. If VFX uses audio, add `AudioBridge` and enable `_bindAudio` on VFXARBinder.

### Quick Checks
- Use `H3M > VFX Pipeline Master > Testing > Validate All Bindings` for a scene-wide sanity check.
- If `ColorMap` is black, ensure `ARCameraTextureProvider` is present and the AR camera is assigned.

---

## 3. Device Debugging (iOS)

### Streaming Logs
Use the automated script to see what's happening on the phone:
```bash
./debug.sh
```
*   Filters for `Unity` and `MetavidoVFX` tags automatically.
*   Works with `xcrun devicectl` (iOS 17+) or `idevicesyslog`.

### On-Screen Console
**Highly Recommended**: Install [IngameDebugConsole](https://github.com/yasirkula/UnityIngameDebugConsole) to view logs on-device.

**Installation**:
1.  Open `Packages/manifest.json`.
2.  Add to dependencies:
    ```json
    "com.yasirkula.ingamedebugconsole": "https://github.com/yasirkula/UnityIngameDebugConsole.git",
    ```
3.  In Unity: `GameObject > Ingame Debug Console`.

*   **Usage**: Tap 3 fingers (or click the bubble) to open logs inside the running app.
*   **Critical**: Use this to catch "Script Missing" or "NullReference" errors that don't crash the app but break logic.

## 4. Crash Diagnosis
If app closes instantly:
1.  Run `./debug.sh --dump` immediately.
2.  Check `Logs/Device/` for crash reports.
3.  Look for `EXC_BAD_ACCESS` (Memory) or `Abort` (Unity C# Exception).
