# Feature Specification: Hologram Recording, Playback & Multiplayer Conferencing

**Feature Branch**: `003-hologram-conferencing`
**Created**: 2026-01-14
**Updated**: 2026-01-21
**Status**: In Progress (Recording implemented 2026-01-21)
**Implementation**:
- ✅ RecordingController.cs - Avfi-based MP4 recording
- ✅ RecordingUI.cs - UI Toolkit record button/timer
- ✅ RECORDING_ARCHITECTURE.md - Metavido/Avfi separation docs
- ⬜ WebRTC multiplayer (pending)
**Input**: User description: "Optimal low memory way to record & playback holograms. Record person hologram, play it back & put it on desk. Use metavidovfx technique. Also multiplayer via webrtc video conferencing - streaming lidar depth info or metavidovfx encoded video live to drive vfx graph hologram of other connected users."

## Triple Verification (2026-01-20)

| Source | Status | Notes |
|--------|--------|-------|
| KB `_HOLOGRAM_RECORDING_PLAYBACK.md` | ✅ Verified | 40K detailed spec, Metavido format |
| KB `_WEBRTC_MULTIUSER_MULTIPLATFORM_GUIDE.md` | ✅ Verified | Photon/Normcore/coherence comparison |
| Online: [keijiro/Metavido](https://github.com/keijiro/Metavido) | ✅ Verified | Burnt-in-barcode metadata + squeezed depth |
| Online: [Unity WebRTC](https://github.com/Unity-Technologies/com.unity.webrtc) | ⚠️ Conflict | Do NOT add - conflicts with WebRtcVideoChat plugin |
| Online: [VideoSDK Guide](https://www.videosdk.live/developer-hub/webrtc/unity-webrtc-video-streaming-multiplayer-game) | ✅ Verified | Architecture for PC/mobile/VR streaming |

### Key Technical Findings

1. **Metavido Format**: Embeds camera pose in barcode, depth/stencil via "squeezing" into frame
2. **WebRTC**: Microsoft MR-WebRTC deprecated - use `com.unity.webrtc` or LiveKit SDK
3. **SFU Scaling**: For 4+ users, use SRS or LiveKit SFU (not pure P2P)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record Hologram (Priority: P1)

As a user, I want to record myself as a hologram using my iPhone's LiDAR, saving it as a standard video file that embeds all depth and pose data.

**Why this priority**: Recording is the foundation for playback and sharing.

**Independent Test**:

1. Build iOS app on LiDAR-enabled device.
2. Point camera at a person.
3. Press "Record" button.
4. Record 10 seconds, press "Stop".
5. Verify video saved to Camera Roll.
6. Verify file size ~17MB (1.7 MB/sec × 10 sec).

**Acceptance Scenarios**:

1. **Given** LiDAR is available, **When** I press Record, **Then** `FrameEncoder` begins encoding Metavido frames.
2. **Given** recording is active, **When** 10 seconds pass, **Then** ~300 frames are captured.
3. **Given** recording stops, **When** I check Camera Roll, **Then** a playable MP4 exists.

---

### User Story 2 - Playback Hologram on Desk (Priority: P1)

As a user, I want to load a recorded hologram video and place it as a 3D hologram on my desk.

**Why this priority**: Playback proves the full encode-decode cycle.

**Independent Test**:

1. Import recorded .mp4 into Unity project (rename to Test.mp4).
2. Open MetavidoPlayback scene.
3. Detect desk surface (AR Plane).
4. Tap to place hologram.
5. Verify point cloud hologram appears.
6. Verify hologram animates as per recorded motion.

**Acceptance Scenarios**:

1. **Given** video is loaded, **When** `MetadataDecoder` processes frame, **Then** camera pose is valid.
2. **Given** valid metadata, **When** `TextureDemuxer.Demux()` runs, **Then** ColorTexture and DepthTexture are populated.
3. **Given** textures are ready, **When** VFX renders, **Then** hologram matches recorded person.

---

### User Story 3 - Multiplayer Hologram Conferencing (Priority: P2)

As a user, I want to join a virtual room where other users appear as live holograms in my AR space.

**Why this priority**: Extends recording/playback to real-time multi-user.

**Independent Test**:

1. User A and User B both open app.
2. Both join room "test-room".
3. User A sees User B as hologram.
4. User B sees User A as hologram.
5. User A waves; User B sees wave with <200ms latency.

**Acceptance Scenarios**:

1. **Given** signaling server is running, **When** users join same room, **Then** P2P WebRTC connection established.
2. **Given** connection exists, **When** User A's `FrameEncoder` outputs frame, **Then** User B receives via video track.
3. **Given** User B receives frame, **When** `MetadataDecoder` + `TextureDemuxer` process it, **Then** hologram renders.

---

### User Story 4 - Low-Memory Optimization (Priority: P1)

As a developer, I want recording and playback to use minimal memory (<150MB total).

**Why this priority**: Mobile devices have limited RAM.

**Independent Test**:

1. Start recording.
2. Open Xcode Instruments (Memory).
3. Record for 60 seconds.
4. Verify memory stays <150MB.

**Acceptance Scenarios**:

1. **Given** recording is active, **When** I check memory, **Then** <100MB used for encoding.
2. **Given** playback is active, **When** I check memory, **Then** <120MB used for decoding + VFX.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Recording MUST use **Metavido** format (color + depth + pose in single video).
- **FR-002**: Recording MUST encode at **1920×1080 @ 30fps** using H.264/HEVC.
- **FR-003**: Playback MUST use **VFX Graph** with `VFXARBinder` (via Hologram prefab).
- **FR-004**: Playback MUST support AR plane detection for hologram placement.
- **FR-005**: Multiplayer MUST use **WebRTC** for P2P video streaming.
- **FR-006**: Multiplayer MUST support **2-6 simultaneous users**.
- **FR-007**: System MUST work without persistent internet (P2P over local network).

### Non-Functional Requirements

- **NFR-001**: Recording memory usage <100MB.
- **NFR-002**: Playback memory usage <120MB.
- **NFR-003**: Multiplayer latency <200ms on WiFi.
- **NFR-004**: Multiplayer bandwidth <2 Mbps per user at 720p.
- **NFR-005**: Battery drain <20%/hour during active streaming.

### Key Entities

**Implemented** (Hologram prefab - `Assets/Prefabs/Hologram/Hologram.prefab`):
```
Hologram (root GameObject)
├── HologramPlacer           // AR placement + gestures
├── HologramController       // Mode switching (Live AR / Metavido)
├── VideoPlayer              // Metavido video playback
├── MetadataDecoder          // Camera matrix extraction
├── TextureDemuxer           // RGB/Depth separation
│
└── HologramVFX (child, scale 0.15)
    ├── VisualEffect         // VFX Graph renderer
    ├── VFXARBinder          // Binds textures to VFX
    └── VFXCategory          // Category tagging
```

**For Multiplayer** (Phase 2):
```
HologramConference {
  roomId: string
  localEncoder: FrameEncoder
  remoteHolograms: Dictionary<string, RemoteHologram>
  signalingConnection: WebSocket  // Use existing WebRtcVideoChat
}

RemoteHologram {
  userId: string
  videoTrack: Texture
  decoder: MetadataDecoder
  demuxer: TextureDemuxer
  vfx: VisualEffect + VFXARBinder
}
```

---

## Technical Architecture

### Recording Pipeline

```
ARKit LiDAR → XRDataProvider → FrameEncoder → iOS Recording API → Camera Roll
     ↓              ↓               ↓
[256×192 Depth] [1920×1080 Color] [Metavido Frame 1920×1080]
```

### Playback Pipeline (Implemented in Hologram Prefab)

```
VideoPlayer → MetadataDecoder → TextureDemuxer → HologramController → VFXARBinder → VFX Graph
     ↓              ↓                ↓                  ↓                  ↓
[MP4 Frame] [Camera Pose] [Color + Depth RT] [Mode Switch] [Hologram Particles]
```

**HologramController** handles mode switching:
- `SourceMode.LiveAR` - Uses ARDepthSource textures
- `SourceMode.MetavidoVideo` - Uses VideoPlayer → TextureDemuxer textures

### Multiplayer Pipeline

```
Local:  FrameEncoder → WebRTC Video Track → Network
Remote: Network → WebRTC Video Track → MetadataDecoder → TextureDemuxer → VFX
```

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Recording works on iPhone 12 Pro+ with LiDAR.
- **SC-002**: Recorded video plays back correctly with depth reconstruction.
- **SC-003**: Playback maintains >30 FPS on iPhone 12+.
- **SC-004**: Multiplayer connects 2 users successfully.
- **SC-005**: Multiplayer latency <200ms on same WiFi network.
- **SC-006**: Memory usage <150MB during recording or playback.
- **SC-007**: Recorded file size ~1.7 MB/sec (50MB/min).

---

## Implementation Phases

### Phase 1: Recording & Playback (MVP)

- Implement Metavido recording scene
- Implement playback scene with desk placement
- Test encode/decode cycle
- **Deliverable**: Record hologram, play on desk

### Phase 2: Multiplayer Foundation

- Add Unity WebRTC package
- Implement signaling server (Node.js)
- Implement `HologramConferenceManager`
- **Deliverable**: 2-user hologram conference

### Phase 3: Optimization & Scale

- Adaptive bitrate streaming
- Resolution scaling based on network
- Support 4-6 users
- **Deliverable**: Production-ready conferencing

---

## Dependencies

### Packages Required

- `jp.keijiro.metavido` v5.1.1 (already installed)
- ~~`com.unity.webrtc`~~ ⚠️ **DO NOT ADD** - conflicts with WebRtcVideoChat plugin (duplicate frameworks on iOS)
- `jp.keijiro.vfxgraphassets` v3.10.1 (already installed)
- **WebRtcVideoChat** (already in `Assets/3rdparty/WebRtcVideoChat/`) - use for Phase 2

### Infrastructure Required

- Signaling server (WebSocket)
- TURN server (for NAT traversal)
- Optional: SFU for 4+ users

---

## Risk Assessment


| Risk                             | Probability | Impact | Mitigation                     |
| -------------------------------- | ----------- | ------ | ------------------------------ |
| WebRTC Unity 6 compatibility     | Medium      | High   | Use LiveKit SDK as fallback    |
| High bandwidth on mobile         | Medium      | Medium | Adaptive bitrate, 720p default |
| Thermal throttling during record | Low         | Medium | Cap at 30fps, 10min max        |
| NAT traversal failures           | Medium      | High   | Deploy TURN server             |

---

## References

- KnowledgeBase: `_HOLOGRAM_RECORDING_PLAYBACK.md`
- Metavido Package: `jp.keijiro.metavido`
- Original Bibcam: https://github.com/keijiro/Bibcam
- Unity WebRTC: https://github.com/Unity-Technologies/com.unity.webrtc
- LiveKit Unity SDK: https://docs.livekit.io/client-sdk-unity/

---

*Created: 2026-01-14*
*Author: Claude Code + User*
