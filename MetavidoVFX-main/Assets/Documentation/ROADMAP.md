# H3M Portals - Cross-Platform Hologram Roadmap

**Goal**: A simple, efficient mechanism for turning sparse video depth and audio from mobile devices, VR headsets, WebCams, and wearable devices into volumetric VFX graph or shader-based holograms.

**Core Philosophy**:
- **Infinitely Scalable** (MMO style)
- **Fidelity** (Gaussian Splat-like)
- **Reactivity** (Audio, Physics)
- **Efficiency** (Fewest dependencies possible)

---

## Phase 1: Local Foundation (COMPLETE)
*Target: Single iOS Device (Local)*
- **Input**: Local Camera + LiDAR Depth.
- **Output**: On-screen Volumetric VFX (Hybrid Bridge).
- **Status**: Verified 353 FPS with 10 VFX on iPhone 15 Pro.

## Phase 2: Peer-to-Peer Mobile (IN PROGRESS)
*Target: Phone to Phone*
- **Transport**: WebRTC (Spec 003).
- **Goal**: One-way or Two-way holographic streaming.

## Phase 3: Web Integration
*Target: Phone to WebGL*
- **Goal**: View mobile holograms in desktop/mobile browser using Needle Engine or A-Frame.

## Phase 4: Extended Inputs
*Target: WebCam to Phone*
- **Goal**: Desktop webcam depth estimation (BodyPix/Neural) streaming to mobile AR.

## Phase 5: Full Web Interop
- Bi-directional streaming between native apps and web clients.

## Phase 6: Conferencing
- Multi-user holographic chat (SFU Architecture).

## Phase 7: VR/MR Integration
- Meta Quest (Passthrough) support via Quest Depth API.

## Phase 8: Scale & Fidelity
- **MMO Scale**: Gaussian Splats + advanced physics.

---

## Technical Notes

### Depth Formats
- **iOS LiDAR**: 256x192 @ 60fps.
- **Quest Depth API**: Passthrough-aligned.
- **Neural Depth**: MiDaS/DPT (relative depth only).

### Compression
- RGBD multiplexing via H.264/H.265.
- Temporal depth compression.
- Audio: Opus codec via WebRTC.
