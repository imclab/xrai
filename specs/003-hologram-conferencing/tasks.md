# Tasks: Hologram Recording, Playback & Multiplayer

**Spec**: 003-hologram-conferencing
**Created**: 2026-01-15

---

## Phase 1: Recording & Playback (MVP)

### Recording System
- [ ] Create `HologramRecorder` scene
- [ ] Implement `RecordingController.cs` (start/stop/save)
- [ ] Integrate `FrameEncoder` from jp.keijiro.metavido
- [ ] Add UI: Record button, timer display, preview
- [ ] Save to Camera Roll via iOS Photos API
- [ ] Test: Record 10s video, verify file size ~17MB

### Playback System
- [ ] Create `HologramPlayback` scene
- [ ] Implement `PlaybackController.cs`
- [ ] Integrate `MetadataDecoder` + `TextureDemuxer`
- [ ] Create `VFXMetavidoBinder` for playback
- [ ] Add AR plane detection for placement
- [ ] Implement tap-to-place hologram
- [ ] Test: Load recorded video, place on desk

### Memory Optimization
- [ ] Profile recording memory usage (target <100MB)
- [ ] Profile playback memory usage (target <120MB)
- [ ] Implement texture pooling if needed
- [ ] Add memory warnings in console

---

## Phase 2: Multiplayer Foundation

### Signaling Server
- [ ] Create Node.js signaling server project
- [ ] Implement WebSocket room management
- [ ] Add join-room / leave-room events
- [ ] Add offer/answer/ice-candidate relay
- [ ] Deploy to test server (local or cloud)
- [ ] Document API in infrastructure.md

### Unity WebRTC Integration
- [ ] Add com.unity.webrtc package ✅ (done in manifest.json)
- [ ] Create `HologramConferenceManager.cs`
- [ ] Implement WebSocket signaling client
- [ ] Implement RTCPeerConnection setup
- [ ] Create video track from FrameEncoder output
- [ ] Test: 2 devices on same WiFi

### Remote Hologram Rendering
- [ ] Create `RemoteHologram.cs` component
- [ ] Decode incoming WebRTC video frames
- [ ] Bind decoded textures to VFX Graph
- [ ] Position remote holograms in AR space
- [ ] Test: User A sees User B as hologram

---

## Phase 3: Optimization & Scale

### TURN Server Setup
- [ ] Evaluate: coturn vs Twilio vs LiveKit
- [ ] Deploy TURN server
- [ ] Add TURN configuration to Unity
- [ ] Test NAT traversal scenarios

### Adaptive Quality
- [ ] Implement network quality monitoring
- [ ] Add bitrate adaptation (480p/720p/1080p)
- [ ] Implement simulcast if using SFU
- [ ] Test on various network conditions

### Multi-User Support (4-6 users)
- [ ] Evaluate SFU options (LiveKit recommended)
- [ ] Implement SFU integration
- [ ] Update UI for multiple remote holograms
- [ ] Test 4-6 concurrent users
- [ ] Profile CPU/memory/battery

### Polish
- [ ] Add connection status UI
- [ ] Add latency indicator
- [ ] Add reconnection logic
- [ ] Add room management UI (create/join/leave)
- [ ] Error handling and user feedback

---

## Definition of Done

Each task is complete when:
1. Code compiles without errors
2. Feature works on device (not just Editor)
3. Memory usage within targets
4. Manual test passes
5. Code reviewed (if applicable)

---

## Dependencies

| Task | Blocked By |
|------|------------|
| Recording | AR Foundation 6.x ✅ |
| Playback | jp.keijiro.metavido ✅ |
| Multiplayer | com.unity.webrtc ✅ |
| SFU | Phase 2 completion |

---

*Last Updated: 2026-01-15*
