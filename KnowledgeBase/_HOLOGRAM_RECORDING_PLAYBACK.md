# Hologram Recording & Playback Guide

## Overview

This document explains optimal low-memory approaches for recording and playing back holographic content using the MetavidoVFX technique.

---

## Metavido: The Optimal Low-Memory Approach

Metavido (formerly Bibcam) is a **video subformat** that embeds AR metadata directly into video frames using:
1. **Burnt-in barcode** - Camera pose encoded in visible pattern at frame edges
2. **Squeezed planes** - Color + Depth + Stencil multiplexed into single video frame

### Key Advantages

| Feature | Metavido | Point Cloud | Mesh Recording |
|---------|----------|-------------|----------------|
| **Storage** | ~50MB/min (H.264) | ~500MB/min | ~200MB/min |
| **CPU Load** | Low (video decode) | Medium | High |
| **GPU Load** | Medium (demux) | High (sorting) | High |
| **Quality** | 1080p color + depth | Resolution-limited | Good |
| **Editable** | Yes (standard video) | Specialized tools | Specialized |
| **Sync** | Perfect (embedded) | External tracking | External |

---

## Recording Architecture

### 1. Metavido Frame Encoding

```
┌────────────────────────────────────────────────────────────────────────┐
│                         RECORDING PIPELINE                              │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐       │
│  │  ARKit Color    │   │  ARKit Depth    │   │ Human Stencil   │       │
│  │  (YCbCr 4:2:0)  │   │ (RFloat 16-bit) │   │  (R8 mask)      │       │
│  └────────┬────────┘   └────────┬────────┘   └────────┬────────┘       │
│           │                     │                     │                 │
│           ▼                     ▼                     ▼                 │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    XRDataProvider                                 │  │
│  │  • TextureSet (y, cbcr, depth, stencil)                          │  │
│  │  • CameraTransform (position, rotation)                          │  │
│  │  • ProjectionMatrix                                               │  │
│  └─────────────────────────────┬────────────────────────────────────┘  │
│                                │                                        │
│                                ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    FrameEncoder                                   │  │
│  │                                                                    │  │
│  │   Shader Pass: Multiplex all planes + burn metadata barcode      │  │
│  │                                                                    │  │
│  │   ┌──────────────────────────────────────────────────────────┐   │  │
│  │   │                  Encoded Frame (1920×1080)                │   │  │
│  │   │  ┌───────────────────────┬───────────────────────┐       │   │  │
│  │   │  │                       │                       │       │   │  │
│  │   │  │     Color (RGB)       │     Depth (RG)        │       │   │  │
│  │   │  │     960 × 1080        │     960 × 540         │       │   │  │
│  │   │  │                       ├───────────────────────┤       │   │  │
│  │   │  │                       │     Stencil (R)       │       │   │  │
│  │   │  │                       │     960 × 540         │       │   │  │
│  │   │  ├──[BARCODE]────────────┴───────────────────────┤       │   │  │
│  │   │  │  px,py,pz | rx,ry,rz | fov,shift | depth_range│       │   │  │
│  │   │  └───────────────────────────────────────────────┘       │   │  │
│  │   └──────────────────────────────────────────────────────────┘   │  │
│  └─────────────────────────────┬────────────────────────────────────┘  │
│                                │                                        │
│                                ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    iOS Video Recording                            │  │
│  │  • H.264/HEVC compression                                        │  │
│  │  • Saved to Camera Roll                                          │  │
│  │  • Standard MP4 format (editable in any video editor)            │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  Output: ~1.7 MB/second @ 30fps (vs ~17 MB/sec uncompressed)           │
└────────────────────────────────────────────────────────────────────────┘
```

### 2. Metadata Encoding

The 12-float metadata structure burnt into each frame:

```csharp
struct Metadata {
    float px, py, pz;     // Camera position (world space)
    float rx, ry, rz;     // Camera rotation (quaternion xyz, w derived)
    float sx, sy;         // Projection center shift
    float fov;            // Vertical field of view
    float near, far;      // Depth range
    float hash;           // Frame uniqueness hash
}
```

---

## Playback Architecture

### 1. Metavido Decoding

```
┌────────────────────────────────────────────────────────────────────────┐
│                         PLAYBACK PIPELINE                               │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    Video Source                                   │  │
│  │  • VideoPlayer component (streaming or file)                     │  │
│  │  • AVPro Video (optional, better iOS performance)                │  │
│  └─────────────────────────────┬────────────────────────────────────┘  │
│                                │                                        │
│                                ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    MetadataDecoder                                │  │
│  │                                                                    │  │
│  │   Compute Shader: Read barcode → Extract 12 floats               │  │
│  │   Output: Metadata struct with camera pose + projection          │  │
│  └─────────────────────────────┬────────────────────────────────────┘  │
│                                │                                        │
│                                ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    TextureDemuxer                                 │  │
│  │                                                                    │  │
│  │   Shader Pass 0: Extract Color (left half of frame)              │  │
│  │   Shader Pass 1: Extract Depth (right half, top/bottom)          │  │
│  │                                                                    │  │
│  │   Outputs:                                                        │  │
│  │   • ColorTexture (RGBA, 960×1080)                                │  │
│  │   • DepthTexture (RHalf, 960×540)                                │  │
│  └─────────────────────────────┬────────────────────────────────────┘  │
│                                │                                        │
│                                ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    VFXMetavidoBinder                              │  │
│  │                                                                    │  │
│  │   Properties pushed to VFX Graph:                                │  │
│  │   • ColorMap ← demux.ColorTexture                                │  │
│  │   • DepthMap ← demux.DepthTexture                                │  │
│  │   • InverseView ← RenderUtils.InverseView(metadata)              │  │
│  │   • RayParams ← RenderUtils.RayParams(metadata)                  │  │
│  │   • DepthRange ← metadata.DepthRange                             │  │
│  └─────────────────────────────┬────────────────────────────────────┘  │
│                                │                                        │
│                                ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    VFX Graph Rendering                            │  │
│  │                                                                    │  │
│  │   • Sample DepthMap → Reconstruct 3D position                    │  │
│  │   • Sample ColorMap → Apply color to particles                   │  │
│  │   • Transform by InverseView → World space positioning           │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  Performance: ~2ms GPU decode + ~3ms VFX render @ 30fps                │
└────────────────────────────────────────────────────────────────────────┘
```

---

## Use Case: Record Person → Play on Desk

### Recording (Capture Session)

```csharp
// Scene Setup for Recording
[Scene: MetavidoEncoder]
├── AR Session Origin
│   └── AR Camera
│       ├── AR Occlusion Manager (human stencil + depth)
│       ├── XRDataProvider (captures all AR textures)
│       └── FrameEncoder (multiplexes into single frame)
├── UI
│   └── Record Button
│       └── [Calls iOS native recording to Camera Roll]
```

### Playback (Hologram on Desk)

```csharp
// Scene Setup for Playback
[Scene: MetavidoPlayback]
├── AR Session Origin
│   └── AR Camera
│       ├── AR Plane Manager (detects desk surface)
│       └── AR Raycast Manager (placement interaction)
├── Hologram Container
│   └── [Positioned on detected desk plane]
│       ├── Video Player (plays recorded .mp4)
│       ├── MetadataDecoder (extracts camera pose)
│       ├── TextureDemuxer (extracts color/depth)
│       ├── HologramVFX [VisualEffect]
│       │   └── VFXMetavidoBinder (binds decoded textures)
│       └── HologramScaler (adjusts size for desk)
```

### Placement Script

```csharp
public class HologramPlacer : MonoBehaviour
{
    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] GameObject hologramPrefab;
    [SerializeField] VideoPlayer videoPlayer;
    [SerializeField] VideoClip recordedClip;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject placedHologram;

    void Update()
    {
        // Tap to place hologram on detected plane
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 touchPos = Input.GetTouch(0).position;

            if (raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                PlaceHologram(hitPose);
            }
        }
    }

    void PlaceHologram(Pose pose)
    {
        if (placedHologram == null)
        {
            placedHologram = Instantiate(hologramPrefab, pose.position, pose.rotation);
        }
        else
        {
            placedHologram.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        // Start playback
        videoPlayer.clip = recordedClip;
        videoPlayer.Play();
    }
}
```

---

## Memory Optimization Strategies

### 1. Streaming vs Preload

```csharp
// Streaming (low memory, slight latency)
videoPlayer.source = VideoSource.Url;
videoPlayer.url = "path/to/hologram.mp4";

// Preload (higher memory, instant playback)
videoPlayer.source = VideoSource.VideoClip;
videoPlayer.clip = preloadedClip;
```

### 2. Resolution Scaling

| Resolution | File Size | GPU Cost | Quality |
|------------|-----------|----------|---------|
| 1920×1080 | ~50MB/min | 3ms | Full |
| 1280×720 | ~25MB/min | 1.5ms | Good |
| 960×540 | ~15MB/min | 0.8ms | Adequate |

### 3. Particle Count Limits

```csharp
// VFX Graph settings for mobile
[Context: Initialize Particle]
├── Capacity: 65,536 (max practical for mobile)
├── Spawn Rate: Sample from depth texture density
└── Lifetime: 0.1s (minimize live particles)
```

### 4. Depth Texture Pooling

```csharp
// Reuse RenderTextures across frames
RenderTexture depthPool = new RenderTexture(960, 540, 0, RenderTextureFormat.RHalf);
depthPool.enableRandomWrite = true;
depthPool.Create();
// Reuse for all hologram playback, release when scene ends
```

---

## Complete Scene Setup

### Step 1: Import Metavido Package

```json
// Packages/manifest.json
{
  "dependencies": {
    "jp.keijiro.metavido": "5.1.1"
  },
  "scopedRegistries": [
    {
      "name": "Keijiro",
      "url": "https://registry.npmjs.com",
      "scopes": ["jp.keijiro"]
    }
  ]
}
```

### Step 2: Recording Scene

1. Create new scene "MetavidoRecorder"
2. Add AR Session + AR Session Origin
3. Add XRDataProvider to AR Camera
4. Add FrameEncoder to AR Camera
5. Add RecordingUI with start/stop buttons
6. Use iOS native recording API to save to Camera Roll

### Step 3: Playback Scene

1. Create new scene or add to existing AR scene
2. Create empty "HologramAnchor" GameObject
3. Add VideoPlayer component
4. Add MetadataDecoder + TextureDemuxer
5. Add VisualEffect with VFXMetavidoBinder
6. Add HologramPlacer script for desk placement

---

## Alternative: Live Streaming (No Recording)

For real-time hologram transmission without recording:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    LIVE HOLOGRAM STREAMING                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  SENDER (Recording Device)                                              │
│  ┌────────────────────────┐    ┌───────────────────────────────────┐   │
│  │  FrameEncoder          │───▶│  WebRTC / NDI / Metawire          │   │
│  │  (same as recording)   │    │  (network transmission)           │   │
│  └────────────────────────┘    └───────────────────────────────────┘   │
│                                                                          │
│  RECEIVER (Viewing Device)                                              │
│  ┌────────────────────────┐    ┌───────────────────────────────────┐   │
│  │  Network Receiver      │───▶│  MetadataDecoder + TextureDemuxer │   │
│  │  (video stream)        │    │  + VFX Graph rendering            │   │
│  └────────────────────────┘    └───────────────────────────────────┘   │
│                                                                          │
│  Latency: ~100-300ms (depending on network)                             │
│  Use: jp.keijiro.metawire for Metavido-optimized transmission          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Multiplayer Holographic Video Conferencing

### Architecture Overview

Stream Metavido-encoded video between users via WebRTC to render each participant as a VFX hologram.

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                    MULTIPLAYER HOLOGRAM STREAMING ARCHITECTURE                   │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────────┐│
│  │                           SIGNALING SERVER                                  ││
│  │                                                                              ││
│  │   • WebSocket (ws://signaling.example.com)                                  ││
│  │   • Room management (create/join/leave)                                     ││
│  │   • ICE candidate exchange                                                  ││
│  │   • SDP offer/answer relay                                                  ││
│  │                                                                              ││
│  └─────────────────────────────┬───────────────────────────────────────────────┘│
│                                │                                                 │
│         ┌──────────────────────┼──────────────────────┐                         │
│         │                      │                      │                         │
│         ▼                      ▼                      ▼                         │
│  ┌─────────────┐        ┌─────────────┐        ┌─────────────┐                  │
│  │   USER A    │        │   USER B    │        │   USER C    │                  │
│  │  (iPhone)   │        │  (iPhone)   │        │  (Quest 3)  │                  │
│  │             │◄──────►│             │◄──────►│             │                  │
│  │  P2P WebRTC │        │  P2P WebRTC │        │  P2P WebRTC │                  │
│  └─────────────┘        └─────────────┘        └─────────────┘                  │
│         │                      │                      │                         │
│         └──────────────────────┼──────────────────────┘                         │
│                                │                                                 │
│                                ▼                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────────┐│
│  │                    PER-DEVICE HOLOGRAM RENDERING                            ││
│  │                                                                              ││
│  │   Each device renders remote users as VFX holograms:                        ││
│  │                                                                              ││
│  │   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐                       ││
│  │   │ User A VFX  │   │ User B VFX  │   │ User C VFX  │                       ││
│  │   │ (self=hide) │   │ (hologram)  │   │ (hologram)  │                       ││
│  │   └─────────────┘   └─────────────┘   └─────────────┘                       ││
│  │                                                                              ││
│  └─────────────────────────────────────────────────────────────────────────────┘│
│                                                                                  │
│  Bandwidth: ~2 Mbps per user (Metavido @ 720p 30fps)                            │
│  Latency: ~100-150ms (WebRTC optimized)                                          │
│  Max Users: 4-6 (practical for mobile devices)                                   │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Per-User Data Flow

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         PER-USER STREAMING PIPELINE                              │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  LOCAL CAPTURE (This Device)                                                     │
│  ┌──────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                           │   │
│  │  ARKit LiDAR ──► XRDataProvider ──► FrameEncoder ──► Metavido Frame      │   │
│  │       │              │                    │               │               │   │
│  │       │              │                    │               ▼               │   │
│  │  [Depth 256x192] [Color 1920x1080]   [Multiplexed]   WebRTC Video Track  │   │
│  │                                                                           │   │
│  └───────────────────────────────────────────────────┬──────────────────────┘   │
│                                                       │                          │
│                                              ┌────────▼────────┐                 │
│                                              │  WebRTC Sender  │                 │
│                                              │  (H.264 Encode) │                 │
│                                              └────────┬────────┘                 │
│                                                       │                          │
│                                    ─ ─ ─ ─ ─ ─ ─ ─ ─ ┼ ─ ─ ─ ─ ─ ─ ─ ─ ─        │
│                                   │    NETWORK        │                  │       │
│                                    ─ ─ ─ ─ ─ ─ ─ ─ ─ ┼ ─ ─ ─ ─ ─ ─ ─ ─ ─        │
│                                                       │                          │
│  REMOTE RECEIVE (Other User's Stream)                 │                          │
│  ┌────────────────────────────────────────────────────▼─────────────────────┐   │
│  │                                                                           │   │
│  │  WebRTC Video Track ──► MetadataDecoder ──► TextureDemuxer              │   │
│  │        │                     │                    │                       │   │
│  │        ▼                     ▼                    ▼                       │   │
│  │  [H.264 Decode]      [Camera Pose]         [Color + Depth]               │   │
│  │                            │                      │                       │   │
│  │                            └──────────┬───────────┘                       │   │
│  │                                       │                                   │   │
│  │                                       ▼                                   │   │
│  │                            ┌──────────────────────┐                       │   │
│  │                            │  RemoteUserHologram  │                       │   │
│  │                            │                      │                       │   │
│  │                            │  VisualEffect        │                       │   │
│  │                            │  + VFXMetavidoBinder │                       │   │
│  │                            │  + WorldAnchor       │                       │   │
│  │                            └──────────────────────┘                       │   │
│  │                                                                           │   │
│  └───────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### WebRTC Integration Options

| Solution | Latency | Bandwidth | Complexity | Mobile Support |
|----------|---------|-----------|------------|----------------|
| **Unity WebRTC** | 100-200ms | 2-4 Mbps | Medium | iOS/Android |
| **LiveKit** | 80-150ms | 1-3 Mbps | Low (SDK) | Excellent |
| **Agora** | 100-200ms | 1-4 Mbps | Low (SDK) | Excellent |
| **Custom TURN** | 150-300ms | 2-4 Mbps | High | Variable |
| **NDI (LAN only)** | 20-50ms | 50+ Mbps | Low | Desktop only |

### Implementation: HologramConferenceManager

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using Metavido.Encoder;
using Metavido.Decoder;

namespace MetavidoVFX.Multiplayer
{
    /// <summary>
    /// Manages multiplayer holographic video conferencing.
    /// Each remote user is rendered as a VFX hologram.
    /// </summary>
    public class HologramConferenceManager : MonoBehaviour
    {
        [Header("Local Capture")]
        [SerializeField] FrameEncoder localEncoder;
        [SerializeField] Camera arCamera;

        [Header("Remote Hologram Template")]
        [SerializeField] GameObject remoteHologramPrefab;
        [SerializeField] Transform hologramParent;

        [Header("WebRTC")]
        [SerializeField] string signalingUrl = "wss://signaling.example.com";
        [SerializeField] string roomId = "hologram-room";

        // Active remote users
        private Dictionary<string, RemoteHologram> remoteUsers = new();

        // WebRTC connection (stub - replace with actual implementation)
        // private RTCPeerConnection peerConnection;

        public void JoinRoom(string roomId)
        {
            this.roomId = roomId;
            // Connect to signaling server
            // Exchange SDP offers/answers
            // Begin streaming local Metavido frames
            Debug.Log($"[HoloConference] Joining room: {roomId}");
        }

        public void LeaveRoom()
        {
            foreach (var user in remoteUsers.Values)
            {
                user.Cleanup();
            }
            remoteUsers.Clear();
            Debug.Log("[HoloConference] Left room");
        }

        /// <summary>
        /// Called when a new remote user joins
        /// </summary>
        public void OnRemoteUserJoined(string oderId, Texture videoTexture)
        {
            if (remoteUsers.ContainsKey(userId)) return;

            // Instantiate hologram for this user
            var hologramGO = Instantiate(remoteHologramPrefab, hologramParent);
            hologramGO.name = $"Hologram_{userId}";

            var hologram = hologramGO.GetComponent<RemoteHologram>();
            hologram.Initialize(userId, videoTexture);

            remoteUsers[userId] = hologram;
            Debug.Log($"[HoloConference] Remote user joined: {userId}");
        }

        /// <summary>
        /// Called when a remote user leaves
        /// </summary>
        public void OnRemoteUserLeft(string userId)
        {
            if (remoteUsers.TryGetValue(userId, out var hologram))
            {
                hologram.Cleanup();
                Destroy(hologram.gameObject);
                remoteUsers.Remove(userId);
            }
            Debug.Log($"[HoloConference] Remote user left: {userId}");
        }

        void Update()
        {
            // Stream local encoded frame to all peers
            if (localEncoder != null && localEncoder.EncodedTexture != null)
            {
                // Send localEncoder.EncodedTexture via WebRTC video track
                // (Implementation depends on WebRTC SDK)
            }
        }
    }

    /// <summary>
    /// Represents a single remote user's hologram
    /// </summary>
    public class RemoteHologram : MonoBehaviour
    {
        [SerializeField] VisualEffect vfx;
        [SerializeField] MetadataDecoder metadataDecoder;
        [SerializeField] TextureDemuxer textureDemuxer;

        private string userId;
        private Texture videoSource;

        public void Initialize(string userId, Texture videoTexture)
        {
            this.userId = userId;
            this.videoSource = videoTexture;
        }

        void Update()
        {
            if (videoSource == null || vfx == null) return;

            // Decode metadata from video frame
            var metadata = metadataDecoder.Metadata;
            if (!metadata.IsValid) return;

            // Demux color and depth
            textureDemuxer.Demux(videoSource, metadata);

            // Push to VFX
            if (vfx.HasTexture("ColorMap"))
                vfx.SetTexture("ColorMap", textureDemuxer.ColorTexture);

            if (vfx.HasTexture("DepthMap"))
                vfx.SetTexture("DepthMap", textureDemuxer.DepthTexture);

            if (vfx.HasMatrix4x4("InverseView"))
            {
                // Apply world transform to position hologram relative to local AR space
                var invView = RenderUtils.InverseView(metadata);
                vfx.SetMatrix4x4("InverseView", transform.localToWorldMatrix * invView);
            }

            if (vfx.HasVector4("RayParams"))
                vfx.SetVector4("RayParams", RenderUtils.RayParams(metadata));

            if (vfx.HasVector2("DepthRange"))
                vfx.SetVector2("DepthRange", metadata.DepthRange);
        }

        public void Cleanup()
        {
            videoSource = null;
        }
    }
}
```

### Signaling Server (Node.js Example)

```javascript
// signaling-server.js
const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 8080 });

const rooms = new Map();

wss.on('connection', (ws) => {
    ws.on('message', (data) => {
        const msg = JSON.parse(data);

        switch (msg.type) {
            case 'join':
                joinRoom(ws, msg.roomId, msg.userId);
                break;
            case 'offer':
            case 'answer':
            case 'ice-candidate':
                relayToRoom(ws, msg);
                break;
            case 'leave':
                leaveRoom(ws, msg.roomId, msg.userId);
                break;
        }
    });
});

function joinRoom(ws, roomId, userId) {
    if (!rooms.has(roomId)) {
        rooms.set(roomId, new Map());
    }
    const room = rooms.get(roomId);
    room.set(userId, ws);

    // Notify others
    room.forEach((client, id) => {
        if (id !== userId) {
            client.send(JSON.stringify({ type: 'user-joined', userId }));
        }
    });
}

function relayToRoom(ws, msg) {
    const room = rooms.get(msg.roomId);
    if (!room) return;

    room.forEach((client, id) => {
        if (id === msg.targetId) {
            client.send(JSON.stringify(msg));
        }
    });
}
```

### Bandwidth Optimization

| Strategy | Bandwidth Savings | Quality Impact | Complexity |
|----------|-------------------|----------------|------------|
| **Resolution Scaling** | 50-75% | Moderate | Low |
| **Frame Rate Reduction** | 30-50% | Noticeable | Low |
| **Adaptive Bitrate** | Variable | Minimal | Medium |
| **Depth Quantization** | 20-30% | Low | Medium |
| **ROI Encoding** | 40-60% | Minimal | High |

### Recommended Settings by Network

| Network | Resolution | FPS | Bitrate | Max Users |
|---------|------------|-----|---------|-----------|
| **5G/WiFi 6** | 1920×1080 | 30 | 4 Mbps | 6 |
| **4G LTE** | 1280×720 | 24 | 2 Mbps | 4 |
| **3G/Poor WiFi** | 960×540 | 15 | 1 Mbps | 2 |

### Cross-Platform Considerations

| Platform | WebRTC | Metavido Encode | Metavido Decode | Notes |
|----------|--------|-----------------|-----------------|-------|
| **iOS (LiDAR)** | Yes | Yes | Yes | Full support |
| **iOS (Non-LiDAR)** | Yes | Partial | Yes | No depth capture |
| **Android** | Yes | Limited | Yes | Depth varies by device |
| **Quest 3** | Yes | No (use Quest depth) | Yes | Needs custom encoder |
| **WebGL** | Limited | No | Yes | Receive-only feasible |

---

## Performance Benchmarks

### Recording (iPhone 14 Pro)

| Metric | Value |
|--------|-------|
| Encoding Time | ~3ms/frame |
| Battery Drain | ~15%/hour |
| Thermal Limit | ~30 min continuous |
| Storage Rate | ~1.7 MB/sec |

### Playback (iPhone 14 Pro)

| Metric | Value |
|--------|-------|
| Decode Time | ~2ms/frame |
| VFX Render | ~3ms/frame |
| Total Frame | ~5ms @ 30fps |
| Memory | ~120MB (video buffer + textures) |

---

## References

- Metavido Package: `jp.keijiro.metavido` v5.1.1
- Metawire (streaming): `jp.keijiro.metawire` v2.1.1
- Original Bibcam: https://github.com/keijiro/Bibcam
- VFX Graph Assets: `jp.keijiro.vfxgraphassets` v3.10.1

---

*Last Updated: 2026-01-14*
*Part of MetavidoVFX Unity-XR-AI Knowledge Base*
