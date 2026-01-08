# H3M Portals - Cross-Platform Hologram Roadmap

**Goal**: A simple, efficient mechanism for turning sparse video depth and audio from mobile devices, VR headsets, WebCams, and wearable devices into volumetric VFX graph or shader-based holograms.

**Core Philosophy**:
*   Infinitely Scalable (MMO style)
*   Fidelity (Gaussian Splat-like)
*   Reactivity (Audio, Physics)
*   Efficiency (Fewest dependencies possible)

## Phase 1: Local Foundation (Current)
*   **Target**: Single iOS Device (Local)
*   **Input**: Local Camera + LiDAR Depth
*   **Output**: On-screen Volumetric VFX (Rcam4 style)
*   **Status**: Debugging "Empty Scene" on local build.

## Phase 2: Peer-to-Peer Mobile
*   **Target**: Phone to Phone
*   **Transport**: WebRTC (Simplest possible implementation)
*   **Data**: RGBD Video + Audio
*   **Goal**: One-way or Two-way holographic streaming.

## Phase 3: Web Integration
*   **Target**: Phone to WebGL
*   **Goal**: View mobile holograms in a desktop/mobile browser.

## Phase 4: Extended Inputs
*   **Target**: WebCam to Phone
*   **Goal**: Desktop webcam depth estimation (BodyPix/Neural) streaming to mobile AR.

## Phase 5: Full Web Interop
*   **Target**: Mobile Web Browser <-> iOS App
*   **Goal**: Bi-directional streaming between native app and web client.

## Phase 6: Conferencing
*   **Target**: >2 Users (Mesh Topology or SFU)
*   **Goal**: Multi-user holographic chat.

## Phase 7: VR/MR Integration
*   **Target**: Meta Quest (Passthrough)
*   **Goal**: View holograms in mixed reality.
*   **Tech**: Needle Engine or native Unity build.

## Phase 8: Scale & Fidelity
*   **Target**: MMO Scale
*   **Tech**: Gaussian Splats, Advanced Physics, Audio Reactivity.
