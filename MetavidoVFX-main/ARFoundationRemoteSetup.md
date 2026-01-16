# AR Foundation Remote 2 – Editor Testing Workflow

This project uses AR Foundation 6.x (Unity 6). Use the AR Foundation Remote 2 companion app to stream camera/depth into the Editor for fast iteration.

## 1) Install AR Foundation Remote 2 (manual – Asset Store)
1. Open Unity Package Manager → My Assets → search **AR Foundation Remote 2**.
2. Download + Import the plugin.
3. In Unity, open **Window → AR Foundation Remote Installer** and install:
   - Core
   - ARKit plugin (for iOS) or ARCore plugin (Android) as needed.

_Note_: AR Foundation Remote is a paid Asset Store package and cannot be added via Git. Keep it out of version control.

## 2) Build the companion app (device)
1. Switch Build Target to the platform you will test (iOS recommended here).
2. Open **AR Foundation Remote** window → build/install the “ARFoundationRemote” companion app to your device.
3. Launch the app on the device; grant camera/mic permissions.

## 3) Scene wiring for Metavido
- Ensure your scene has: `ARSession`, `XROrigin`, `ARCameraManager`, `AROcclusionManager`, and your VFX rig (`ARKitMetavidoBinder`/Controller).
- You can keep your existing camera rig; AR Foundation Remote streams textures into the existing managers.
- Disable real device XR loaders in Editor play (XR Management) to avoid conflicts.

## 4) Play in Editor
1. In the Editor, open **AR Foundation Remote** window and click **Connect** once the companion app is running on-device (same Wi‑Fi).
2. Press Play in Editor. The device streams camera/depth into the Editor; your VFX binders/bridges will see `environmentDepthTexture`/`humanDepthTexture` and camera feed.

## 5) Recommended settings for smooth Editor streaming
- Keep VFX RT resolutions modest (256–512) when testing.
- If using the optimized bridge (GPU depth/velocity), start with `Fastest` depth modes + temporal smoothing.
- Turn off Metal validation and heavy GPU debugging during Editor tests.

## 6) Troubleshooting
- “No connection”: make sure the AR Foundation Remote window is open and the device is on the same network; then hit **Connect**.
- Pink camera feed: ensure AR Foundation Remote Core is installed and ARCameraBackground is active.
- Stale textures: stop Play, reconnect, and restart Play.

## 7) What stays out of source control
- The AR Foundation Remote package and companion app binaries (Asset Store-licensed). Do not commit them; keep them installed locally.
