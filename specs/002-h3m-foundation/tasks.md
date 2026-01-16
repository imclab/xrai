# Tasks: H3M Hologram Foundation (MVP)

**Input**: Design documents from `/specs/002-h3m-foundation/`
**Prerequisites**: plan.md, spec.md
**Last Updated**: 2026-01-16

## Phase 1: Setup (Shared Infrastructure) ✅ COMPLETE

**Purpose**: Verify the Unity project is correctly configured for iOS, AR Foundation, and VFX Graph.

- [x] T001 Verify Unity Project Settings for iOS (Bundle ID, Signing Team).
- [x] T002 Ensure `com.unity.visualeffectgraph`, `com.unity.xr.arfoundation`, `com.unity.xr.arkit`, and `com.unity.render-pipelines.universal` are installed (Packages/manifest.json).
- [x] T003 Create `Assets/H3M/Core`, `Assets/H3M/VFX`, `Assets/H3M/Pipelines`, `Assets/H3M/Editor`, `Assets/H3M/Network` directory structure.
- [x] T004 Create `iOSBuildPostProcessor.cs` in `Assets/Scripts/Editor/` for Xcode build settings.

---

## Phase 2: Foundational (Blocking Prerequisites) ✅ COMPLETE

**Purpose**: Core infrastructure for data acquisition (Compute+AR).

- [x] T005 Create `DepthToWorld.compute` shader in `Assets/Resources/`. Define kernels for `DepthToWorld` and `CalculateVelocity`.
- [x] T006 Create `HologramSource.cs` in `Assets/H3M/Core`. Implements AR depth/stencil acquisition and compute dispatch.
- [x] T007 Extend `HologramSource.cs` to bind `AROcclusionManager.humanStencilTexture` and `environmentDepthTexture`.
- [x] T008 [P] Create `HologramAnchor.cs` to handle AR Raycast placement on planes.

**Checkpoint**: ✅ Data flow scripts complete with visual output ready.

---

## Phase 3: User Story 1 - The "Man in the Mirror" (Priority: P1) 🎯 MVP ✅ COMPLETE

**Goal**: Render a segmented point cloud of the user on a tabletop.

**Independent Test**: Build to iOS, tap table, see "Mini Me" hologram.

### Implementation for User Story 1

- [x] T009 [P] [US1] Create `hologram_depth_people_metavido.vfx` graph in `Assets/H3M/VFX`. Setup texture-based sampling with DepthMap/ColorMap/RayParams.
- [x] T010 [US1] Create `HologramRenderer.cs` in `Assets/H3M/Core`. Implements VFX binding with HologramSource.
- [x] T011 [US1] Implement "Stencil Filtering" logic via `MaskDepthWithStencil.compute`.
- [x] T012 [US1] Implement "Transform to Local Anchor" logic in `HologramAnchor.cs` with scale/offset.
- [x] T013 [US1] Assemble `HOLOGRAM_Mirror_MVP.unity` scene with `ARSession`, `XROrigin`, and H3M components.
- [x] T014 [US1] Add Debug UI: `HologramDebugUI.cs` with UI Toolkit panel showing texture/particle status.

### Additional Phase 3 Tasks (2026-01-16)
- [x] T013b Create `H3M_HologramRig.prefab` with Source/Renderer/Anchor/DebugUI hierarchy.
- [x] T014b Create `H3MHologramSetup.cs` editor utilities for one-click rig setup.
- [x] T014c Add `H3M > Hologram` menu items for setup, verification, and re-wiring.

**Checkpoint**: ✅ "Man in the Mirror" functionality complete.

---

## Phase 4: User Story 2 - WebRTC Network (Priority: P2) ✅ COMPLETE

**Goal**: WebRTC infrastructure ready for streaming holograms.

**Independent Test**: "Signaling Connected" log message.

### Implementation for User Story 2

- [x] T015 [P] [US2] Add `com.unity.webrtc` to package manifest (guarded by UNITY_WEBRTC_AVAILABLE define).
- [x] T016 [P] [US2] Create `Assets/H3M/Network/H3MWebRTCReceiver.cs` with video stream handling.
- [x] T017 [US2] Create `H3MSignalingClient.cs` for WebSocket peer discovery.
- [x] T018 [US2] Create `H3MWebRTCVFXBinder.cs` to bind remote streams to VFX.
- [x] T019 [US2] Create `H3MStreamMetadata.cs` for camera position/projection metadata.
- [x] T020 [US2] Create `H3MNetworkSetup.cs` editor utilities with `H3M > Network` menu.

**Checkpoint**: ✅ WebRTC infrastructure complete.

---

## Phase 5: VFX Binding System ✅ COMPLETE (2026-01-16)

**Purpose**: Unified AR data binding to all VFX.

- [x] T021 Create `VFXBinderManager.cs` for global AR data binding to all VFX.
- [x] T022 Create `VFXARDataBinder.cs` for per-VFX runtime binding.
- [x] T023 Create `VFXAudioDataBinder.cs` for audio frequency bands.
- [x] T024 Create `VFXHandDataBinder.cs` for hand tracking.
- [x] T025 Create `VFXPhysicsBinder.cs` for velocity/gravity.
- [x] T026 Create `VFXBinderUtility.cs` with preset-based setup.
- [x] T027 Create `VFXAutoBinderSetup.cs` editor window.
- [x] T028 Add `H3M > VFX > Auto-Setup ALL VFX (One-Click)` menu.

**Checkpoint**: ✅ All 115 VFX can receive AR data.

---

## Phase 6: Optimization & Polish

**Purpose**: Ensure 30 FPS target.

- [ ] T029 Tune Particle Count (start at 50k, test up to 200k).
- [ ] T030 Optimize Compute Shader group size (32x32 confirmed optimal).
- [ ] T031 Final code cleanup.
- [ ] T032 On-device testing and profiling.
