# Vision Pro Spatial Personas: Multi-User AR Telepresence Patterns

**Source**: Apple WWDC25 Session 318, visionOS 26 Documentation
**Category**: AR/VR, Multi-User, Telepresence
**Last Updated**: 2026-01-21

---

## Overview

Apple Vision Pro Spatial Personas represent the gold standard for multi-user AR telepresence. This document captures the key patterns and how to implement them in Unity for hologram conferencing.

---

## Core Concepts

### 1. Seat Poses vs Participant Poses

| Concept | Description | Use Case |
|---------|-------------|----------|
| **Seat Pose** | Fixed position relative to app/content | Pre-defined meeting positions, theater seats |
| **Participant Pose** | Dynamic position where user actually is | AR passthrough, freeform movement |

```csharp
// Unity implementation
public struct SeatPose
{
    public Vector3 Position;
    public Quaternion Rotation;
    public int SeatIndex;
    public bool IsOccupied;
    public string OccupantId;
}
```

### 2. Context-Aware Layouts

The position of remote users changes based on the shared activity:

| Context | Layout | Rationale |
|---------|--------|-----------|
| **Movie/Video** | Side-by-side | Like sitting next to each other in theater |
| **Game/Board** | Facing | Like sitting across a table |
| **Meeting** | Semi-circle | Equal visibility to shared content |
| **Presentation** | Rows | Audience facing presenter |

```csharp
public enum ConferenceLayoutMode
{
    Theater,    // Side-by-side for shared viewing
    Table,      // Semi-circle for collaboration
    Freeform,   // AR passthrough - stay where joined
    Grid        // Stress testing / large groups
}
```

### 3. Optimal Placement Algorithm

When a new user joins, the system:
1. Identifies gaps around shared content
2. Places new persona in optimal position for both local and remote participants
3. Smoothly animates into position

```csharp
public SeatPose RegisterHologram(string peerId, Transform hologramTransform)
{
    // 1. Find next available seat
    int seatIndex = FindNextAvailableSeat();

    // 2. If no seats, expand layout
    if (seatIndex < 0) {
        ExpandSeats(1);
        seatIndex = FindNextAvailableSeat();
    }

    // 3. Animate to position
    StartCoroutine(AnimateToSeat(hologramTransform, _seats[seatIndex]));
}
```

### 4. Directional Presence

Spatial Personas preserve directional cues:
- **Pointing**: If User A points left, User B on left sees pointing towards them
- **Gaze**: Eye contact follows spatial positioning
- **Gestures**: Wave direction preserved relative to positions

```csharp
// Hologram always faces center or camera depending on mode
Quaternion targetRotation = _layoutMode == ConferenceLayoutMode.Table
    ? Quaternion.LookRotation(_centerPoint.position - seat.Position)
    : Quaternion.LookRotation(Camera.main.transform.position - seat.Position);
```

### 5. Spatial Audio

Voice comes from where the persona is positioned:

| Feature | Implementation |
|---------|---------------|
| **Point Source** | AudioSource at hologram head position |
| **Distance Attenuation** | Logarithmic rolloff, 0.5-10m range |
| **Active Speaker Boost** | 1.2x volume for current speaker |
| **Inactive Ducking** | 0.7x volume for non-speaking users |
| **HRTF** | Use Unity's Spatializer plugin |

```csharp
private void ConfigureAudioSource(AudioSource source)
{
    source.spatialBlend = 1f;  // Full 3D
    source.minDistance = 0.5f;
    source.maxDistance = 10f;
    source.dopplerLevel = 0f;  // Disable for voice
    source.rolloffMode = AudioRolloffMode.Logarithmic;
}
```

---

## Implementation Components

### ConferenceLayoutManager

Manages auto-positioning of remote holograms.

```
ConferenceLayoutManager
├── GenerateTableSeats()       // Semi-circle around center
├── GenerateTheaterSeats()     // Side-by-side rows
├── GenerateGridSeats()        // N×M grid for stress testing
├── RegisterHologram(id, transform)
├── UnregisterHologram(id)
├── SetCenterPoint(position)   // When shared content moves
└── RefreshLayout()            // Force re-layout
```

### SpatialAudioController

Manages spatial audio for each remote hologram.

```
SpatialAudioController
├── FeedAudio(peerId, samples, channels, sampleRate)
├── GetAudioSource(peerId)     // For WebRTC binding
├── SetPeerVolume(id, volume)
├── SetPeerMuted(id, muted)
├── GetVoiceLevel(peerId)      // 0-1 RMS level
└── Events: OnActiveSpeakerChanged
```

### EditorConferenceSimulator

Testing infrastructure without network connectivity.

```
EditorConferenceSimulator
├── StartSimulation(userCount)
├── StopSimulation()
├── AddMockUser()
├── RemoveMockUser(id)
├── StartStressTest()          // 20 users
├── SetLayoutMode(mode)
└── OnGUI debug panel
```

---

## Scalability Guidelines

| Users | Architecture | Bandwidth | Notes |
|-------|-------------|-----------|-------|
| 2-4 | P2P Full Mesh | 2-8 Mbps | Each user sends to all others |
| 5-8 | SFU | 4-12 Mbps | Server relays, reduces upload |
| 9-20 | SFU + Adaptive | 6-20 Mbps | Resolution scaling per user |
| 20+ | Selective Forwarding | 8-30 Mbps | Only active speakers high quality |

### Quality Adaptation Tiers

```csharp
public enum QualityTier
{
    High,       // 720p, 30fps - active speaker
    Medium,     // 480p, 24fps - recent speakers
    Low,        // 360p, 15fps - inactive users
    Audio       // Audio only - very distant users
}
```

---

## visionOS 26 Persona Enhancements

The latest visionOS 26 update (announced WWDC25) brings:

1. **Volumetric Rendering**: Full 3D personas with depth
2. **Side Profile Support**: Accurate appearance from any angle
3. **Enhanced Hair/Lashes**: ML-powered detail preservation
4. **Realistic Skin**: Better complexion rendering

For Unity implementation, this suggests:
- Use high particle count for detailed point clouds (100K+)
- Sample actual RGB color from video texture
- Consider Gaussian Splatting for ultra-realistic rendering

---

## References

- [WWDC25 Session 318: Share visionOS experiences](https://developer.apple.com/videos/play/wwdc2025/318/)
- [Apple Developer: Adding spatial Persona support](https://developer.apple.com/documentation/groupactivities/adding-spatial-persona-support-to-an-activity)
- [Apple Support: Use spatial Persona](https://support.apple.com/guide/apple-vision-pro/use-spatial-persona-tana1ea03f18/visionos)
- [UploadVR: Spatial Personas Hands-On](https://www.uploadvr.com/apple-vision-pro-spatial-personas-hands-on/)
- [Spec 003: Hologram Conferencing](../MetavidoVFX-main/Assets/Documentation/specs/003-hologram-conferencing/spec.md)

---

## Related KB Files

- `_WEBRTC_MULTIUSER_MULTIPLATFORM_GUIDE.md` - WebRTC architecture comparison
- `_HOLOGRAM_RECORDING_PLAYBACK.md` - Metavido format, recording/playback
- `_COMPREHENSIVE_HOLOGRAM_PIPELINE_ARCHITECTURE.md` - Hologram rendering pipeline

---

*Created: 2026-01-21*
*Category: AR/VR, Multi-User*
