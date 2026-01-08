# Feature Specification: H3M Hologram Foundation (MVP)

**Feature Branch**: `002-h3m-foundation`
**Created**: 2025-12-06
**Status**: Draft
**Input**: User description: "Focus on holograms. MVP is 'Man in the Mirror' - see myself as a mini hologram on the table. Use RGBD video, segmentation (body/face/hands), and VFX Graph. Look at MetavidoVFX/Rcam/Bibcam/Kamm examples. Decouple LLM/Voice stuff (deferred)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The "Man in the Mirror" (Mini Hologram Tabletop) (Priority: P1)
As a user, I want to point my phone at myself (or a friend), see the live video feed segmented (body cutout), and simultaneously see a **miniature 3D hologram** of that person standing on a virtual or AR surface (tabletop) in real-time.

**Why this priority**: Corrects the core MVP. This proves the full pipeline: RGBD Capture -> Segmentation -> Projection -> VFX Rendering.

**Independent Test**:
1. Build iOS app.
2. Point camera at a person (or yourself in mirror).
3. Tap on a surface (AR Plane) to place the hologram.
4. Verify a "Mini Me" point cloud appears on the table.
5. Wave hand; verify the mini hologram waves back instantly.
6. Verify background (walls/furniture) is **not** part of the hologram (segmentation works).

**Acceptance Scenarios**:
1. **Given** valid AR plane detection, **When** I tap to place, **Then** a `HologramAnchor` is created.
2. **Given** the camera feed, **When** a person is in frame, **Then** the `AROcclusionManager` (or similar) provides a stencil mask.
3. **Given** the stencil mask, **When** the VFX Graph renders, **Then** only particles corresponding to the person are visible (clean cutout).

---

### User Story 2 - RGBD Data Pipeline (Priority: P1)
As a developer, I want a robust pipeline that extracts Color (RGB), Depth (D), and Stencil (S) textures from AR Foundation and feeds them into a Compute Shader/VFX Graph efficiently.

**Why this priority**: This is the "Engine" that powers the hologram. It must be efficient (60 FPS on mobile).

**References**: `MetavidoVFX`, `Rcam4`, `Bibcam`.

**Independent Test**:
1. Open Unity Editor (or check Debug UI on device).
2. Inspect the `RenderTexture` outputs for Depth and Stencil.
3. Verify they are updating every frame.
4. Verify the Compute Shader is dispatching correctly (no errors in Console).

**Acceptance Scenarios**:
1. **Given** the app is running, **When** I inspect the `Texture2D` inputs to the VFX Graph, **Then** I see valid data (not black/null).

---

### User Story 3 - Peer-to-Peer Prep (Priority: P2)
As a developer, I want the WebRTC infrastructure (Unity WebRTC) installed and ready, so I can pivot to streaming this data in the next phase.

**Why this priority**: "Phone to Phone" is the next immediate goal after local loop.

**Independent Test**:
1. Verify `com.unity.webrtc` package installation.
2. Confirm `WebRTCSender` and `WebRTCReceiver` scripts exist (stubs or basic implementation).

**Acceptance Scenarios**:
1. **Given** the project is open, **When** I check Packages, **Then** `com.unity.webrtc` is present.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Rendering MUST use **VFX Graph** with **Compute Shader** preprocessing for point cloud generation.
- **FR-002**: Pipeline MUST support **Human Segmentation** (Stencil) to isolate people from the background.
    - Reference: AR Foundation `AROcclusionManager` or `ARHumanBodyManager`.
- **FR-003**: System MUST provide a "Tabletop Mode" (Miniature Scale) for the hologram.
    - Scale factor (e.g., 0.1x) must be adjustable.
- **FR-004**: LLM/Voice features are **DEFERRED** (Microservices). DO NOT include in this build.
- **FR-005**: Project MUST compile to iOS (Xcode) with `MetavidoVFX` dependencies resolved.

### Key Entities

- **HologramSource**: `{ rgbTexture, depthTexture, stencilTexture, worldMatrix }`
- **HologramInstance**: `{ scale, position, rotation, vfxGraph }`
- **SegmentationMask**: `{ humanStencil, depthConfidence }`

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: "Mini Me" hologram renders at >30 FPS on iPhone 12 Pro+.
- **SC-002**: Segmentation cleanly separates person from background (no "wall noise" in hologram).
- **SC-003**: Latency < 100ms.
- **SC-004**: Tabletop placement is stable (AR Anchor holds position).
- **SC-005**: **ZERO** LLM/Voice code in the main update loop.

