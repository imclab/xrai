# Assembly Instructions: H3M Mirror MVP

**Scene**: `Assets/Scenes/H3M_Mirror_MVP.unity`

## 1. Configure Segmentation
1. Select the **AR Camera** (child of `XR Origin`).
2. Locate the **AR Occlusion Manager** component.
3. Set **Human Segmentation Stencil Mode** to **Best** (or `Fastest`).
4. Set **Human Segmentation Depth Mode** to **Best**.
5. Ensure **Environment Depth Mode** is **Best**.

## 2. Setup Hologram Logic
1. Select the **Rcam4 VFX** GameObject (Rename it to `Hologram`).
2. **Remove** the `H3MLiDARCapture` script/component (if attached here or on Camera).
3. **Add Component**: `HologramSource` (Script).
   - `Occlusion Manager`: Drag AR Camera here.
   - `Color Provider`: Drag AR Camera here (ensure `ARCameraTextureProvider` is on it).
   - `Compute Shader`: Assign `Assets/H3M/Pipelines/PointCloud.compute`.
4. **Add Component**: `HologramRenderer` (Script).
   - `Source`: Drag this GameObject (Self).
5. **Add Component**: `HologramAnchor` (Script).
   - `Target`: Drag this GameObject (Self).
   - `Raycast Manager`: You must add `ARRaycastManager` to the `XR Origin`.

## 3. Verify VFX Graph
1. Open `Assets/H3M/VFX/Hologram.vfx`.
2. Ensure it has "Get Graphics Buffer" nodes for:
   - `PositionBuffer`
   - `ColorBuffer`
3. Ensure "Initialize Particle" block uses "Set Position from Buffer".

## 4. Build
1. Open **Build Settings**.
2. Add `H3M_Mirror_MVP` scene.
3. Build and Run on iOS Device.
