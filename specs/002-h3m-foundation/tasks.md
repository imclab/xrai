# Tasks: H3M Hologram Foundation (MVP)

**Input**: Design documents from `/specs/002-h3m-foundation/`
**Prerequisites**: plan.md, spec.md

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify the Unity project is correctly configured for iOS, AR Foundation, and VFX Graph.

- [ ] T001 Verify Unity Project Settings for iOS (Bundle ID, Signing Team).
- [ ] T002 Ensure `com.unity.visualeffectgraph`, `com.unity.xr.arfoundation`, `com.unity.xr.arkit`, and `com.unity.render-pipelines.universal` are installed (Packages/manifest.json).
- [ ] T003 Create `Assets/H3M/Core`, `Assets/H3M/VFX`, `Assets/H3M/Pipelines` directory structure.
- [ ] T004 Create `HologramBuildProcessor.cs` in `Assets/H3M/Editor` to ensure automating complex Xcode build settings (e.g., enable bitcode, frameworks).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure for data acquisition (Compute+AR).

- [ ] T005 Create `PointCloud.compute` shader in `Assets/H3M/Pipelines/`. Define kernels for `DepthToWorld` and `DepthToStencil`.
- [ ] T006 Create `HologramSource.cs` in `Assets/H3M/Core`. Implement explicit `TryAcquireLatestCpuImage` logic for RGB.
- [ ] T007 Extend `HologramSource.cs` to bind `AROcclusionManager.humanStencilTexture` and `environmentDepthTexture`.
- [ ] T008 [P] Create `HologramAnchor.cs` to handle AR Raycast placement on planes.

**Checkpoint**: At this point, we have data flow scripts but no visual output.

---

## Phase 3: User Story 1 - The "Man in the Mirror" (Priority: P1) 🎯 MVP

**Goal**: Render a segmented point cloud of the user on a tabletop.

**Independent Test**: Build to iOS, tap table, see "Mini Me" hologram.

### Implementation for User Story 1

- [ ] T009 [P] [US1] Create `Hologram.vfx` graph in `Assets/H3M/VFX`. Setup "Initialize from Buffer" blocks.
- [ ] T010 [US1] Create `HologramRenderer.cs` in `Assets/H3M/Core`. Implement the `Update()` loop responsible for dispatching the Compute Shader and setting VFX Graph parameters.
- [ ] T011 [US1] Implement "Stencil Filtering" logic in `PointCloud.compute`. (If stencil < 0.5, set position to infinity/discard).
- [ ] T012 [US1] Implement "Transform to Local Anchor" logic in `PointCloud.compute` scale/offset calculation.
- [ ] T013 [US1] Assemble `H3M_Mirror_MVP.unity` scene with `ARSession`, `XROrigin`, and the `HologramRenderer` prefab.
- [ ] T014 [US1] Add "Debug Canvas" to scene: Show Raw Depth Texture and Stencil Texture on UI Image for diagnostic.

**Checkpoint**: "Man in the Mirror" functionality complete.

---

## Phase 4: User Story 2 - Peer-to-Peer Prep (Priority: P2)

**Goal**: WebRTC infrastructure ready for streaming.

**Independent Test**: "Signaling Connected" log message.

### Implementation for User Story 2

- [ ] T015 [P] [US2] Install `com.unity.webrtc` package via Manifest.
- [ ] T016 [P] [US2] Create `Assets/H3M/Network/WebRTCReceiver.cs` stub.
- [ ] T017 [US2] Create `ReceiverScene` with a simple "Connect" button that initializes `WebRTC.Initialize()`.

---

## Phase 5: Optimization & Polish

**Purpose**: Ensure 30 FPS target.

- [ ] T018 Tune Particle Count (start at 50k, test up to 200k).
- [ ] T019 Optimize Compute Shader group size (8x8 vs 32x32).
- [ ] T020 Final code cleanup.
