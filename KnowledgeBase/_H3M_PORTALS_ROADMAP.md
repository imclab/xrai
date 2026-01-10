# H3M Portals - Cross-Platform Hologram Roadmap

**Goal**: A simple, efficient mechanism for turning sparse video depth and audio from mobile devices, VR headsets, WebCams, and wearable devices into volumetric VFX graph or shader-based holograms.

**Core Philosophy**:
- Infinitely Scalable (MMO style)
- Fidelity (Gaussian Splat-like)
- Reactivity (Audio, Physics)
- Efficiency (Fewest dependencies possible)

---

## Phase 1: Local Foundation (Current)

| Attribute | Value |
|-----------|-------|
| **Target** | Single iOS Device (Local) |
| **Input** | Local Camera + LiDAR Depth |
| **Output** | On-screen Volumetric VFX (Rcam4 style) |
| **Status** | Debugging "Empty Scene" on local build |

**Key Repos**:
- `keijiro/Rcam4` - LiDAR depth to VFX Graph pipeline
- `keijiro/DepthAITestbed` - Depth estimation experiments

---

## Phase 2: Peer-to-Peer Mobile

| Attribute | Value |
|-----------|-------|
| **Target** | Phone to Phone |
| **Transport** | WebRTC (Simplest possible implementation) |
| **Data** | RGBD Video + Audio |
| **Goal** | One-way or Two-way holographic streaming |

**Key Repos**:
- `Unity-Technologies/com.unity.webrtc` - Official Unity WebRTC
- `because-why-not/awrtc_unity` - Alternative WebRTC wrapper

---

## Phase 3: Web Integration

| Attribute | Value |
|-----------|-------|
| **Target** | Phone to WebGL |
| **Goal** | View mobile holograms in a desktop/mobile browser |

**Key Repos**:
- `needle-tools/needle-engine-support` - Unity to Web export
- `AltspaceVR/aframe-volumetric` - A-Frame volumetric video

---

## Phase 4: Extended Inputs

| Attribute | Value |
|-----------|-------|
| **Target** | WebCam to Phone |
| **Goal** | Desktop webcam depth estimation (BodyPix/Neural) streaming to mobile AR |

**Key Repos**:
- `tensorflow/tfjs-models` - BodyPix, depth estimation
- `keijiro/BodyPix-Unity` - Unity TFLite body segmentation

---

## Phase 5: Full Web Interop

| Attribute | Value |
|-----------|-------|
| **Target** | Mobile Web Browser <-> iOS App |
| **Goal** | Bi-directional streaming between native app and web client |

---

## Phase 6: Conferencing

| Attribute | Value |
|-----------|-------|
| **Target** | >2 Users (Mesh Topology or SFU) |
| **Goal** | Multi-user holographic chat |

**Architecture Options**:
- **Mesh**: Simple, but O(n²) connections
- **SFU**: Scales better, requires server (e.g., Janus, mediasoup)

---

## Phase 7: VR/MR Integration

| Attribute | Value |
|-----------|-------|
| **Target** | Meta Quest (Passthrough) |
| **Goal** | View holograms in mixed reality |
| **Tech** | Needle Engine or native Unity build |

**Key Repos**:
- `oculus-samples/Unity-DepthAPI` - Quest depth access
- `needle-tools/needle-engine-support` - Web-based XR

---

## Phase 8: Scale & Fidelity

| Attribute | Value |
|-----------|-------|
| **Target** | MMO Scale |
| **Tech** | Gaussian Splats, Advanced Physics, Audio Reactivity |

**Key Repos**:
- `aras-p/UnityGaussianSplatting` - Gaussian splat rendering
- `graphdeco-inria/gaussian-splatting` - Original 3DGS implementation

---

## Technical Notes

### Depth Formats
- **iOS LiDAR**: 256x192 @ 60fps, confidence map available
- **Quest Depth API**: Variable resolution, passthrough-aligned
- **Neural Depth**: MiDaS, DPT - relative depth only

### Compression Considerations
- RGBD can use H.264/H.265 for RGB + separate depth channel
- Consider temporal compression for depth (low frequency changes)
- Audio: Opus codec via WebRTC

---

**Last Updated**: 2025-01-10
**Maintainer**: James Tunick
