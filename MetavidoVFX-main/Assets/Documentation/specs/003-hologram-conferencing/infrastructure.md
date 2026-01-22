# Infrastructure Specification: Hologram Conferencing

**Spec**: 003-hologram-conferencing
**Created**: 2026-01-15
**Status**: Draft

## Overview

This annex specifies the backend infrastructure required for multiplayer hologram conferencing (Phase 2-3 of the main spec).

---

## 1. Signaling Server

### Purpose
WebRTC requires a signaling server to exchange SDP offers/answers and ICE candidates between peers before direct P2P connection is established.

### Technology Stack
- **Runtime**: Node.js 18+
- **Framework**: Express.js or Fastify
- **Protocol**: WebSocket (ws library) for real-time signaling
- **Alternative**: Socket.io for auto-reconnection support

### API Design

```typescript
// WebSocket Events (Server → Client)
interface SignalingEvents {
  'user-joined': { roomId: string; userId: string; };
  'user-left': { roomId: string; userId: string; };
  'offer': { from: string; sdp: RTCSessionDescription; };
  'answer': { from: string; sdp: RTCSessionDescription; };
  'ice-candidate': { from: string; candidate: RTCIceCandidate; };
  'room-full': { roomId: string; maxUsers: number; };
}

// WebSocket Events (Client → Server)
interface ClientEvents {
  'join-room': { roomId: string; userId: string; };
  'leave-room': { roomId: string; };
  'offer': { to: string; sdp: RTCSessionDescription; };
  'answer': { to: string; sdp: RTCSessionDescription; };
  'ice-candidate': { to: string; candidate: RTCIceCandidate; };
}
```

### REST Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/health` | GET | Health check for load balancer |
| `/rooms` | GET | List active rooms (admin only) |
| `/rooms/:id/users` | GET | List users in room |
| `/stats` | GET | Connection statistics |

### Deployment Options

1. **Local Development**: `localhost:3000`
2. **Self-Hosted**: DigitalOcean Droplet / AWS EC2
3. **Serverless**: Cloudflare Workers with Durable Objects
4. **Managed**: LiveKit Cloud (recommended for production)

### Sample Implementation

```javascript
// signaling-server.js
const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 3000 });

const rooms = new Map(); // roomId -> Set<{ws, userId}>

wss.on('connection', (ws) => {
  ws.on('message', (data) => {
    const msg = JSON.parse(data);
    switch (msg.type) {
      case 'join-room':
        joinRoom(ws, msg.roomId, msg.userId);
        break;
      case 'offer':
      case 'answer':
      case 'ice-candidate':
        relay(msg.to, { ...msg, from: ws.userId });
        break;
    }
  });
});
```

---

## 2. TURN Server

### Purpose
NAT traversal for peers behind symmetric NATs or firewalls. Required for ~15% of connections.

### Options

| Option | Cost | Latency | Recommendation |
|--------|------|---------|----------------|
| **coturn (self-hosted)** | ~$20/mo VPS | Variable | Good for testing |
| **Twilio TURN** | Pay-per-use | Low | Good for production |
| **Cloudflare TURN** | Included with Workers | Very low | Best for scale |
| **LiveKit Cloud** | Included | Low | Best overall |

### Configuration

```javascript
// Unity WebRTC ICE Configuration
const iceServers = [
  // STUN (free, for ~85% of connections)
  { urls: 'stun:stun.l.google.com:19302' },

  // TURN (paid, fallback for NAT traversal)
  {
    urls: 'turn:turn.example.com:3478',
    username: 'user',
    credential: 'secret'
  }
];
```

### Self-Hosted coturn Setup

```bash
# Install coturn
apt-get install coturn

# /etc/turnserver.conf
listening-port=3478
realm=hologram.example.com
server-name=hologram.example.com
lt-cred-mech
user=hologram:secretpassword
```

---

## 3. Selective Forwarding Unit (SFU)

### Purpose
For 4+ user conferences, P2P mesh becomes expensive (N*(N-1)/2 connections). An SFU centralizes streams.

### Architecture Comparison

| Topology | Users | Bandwidth (per user) | Latency |
|----------|-------|---------------------|---------|
| P2P Mesh | 2-3 | High (N-1 uploads) | Lowest |
| SFU | 4-20 | Low (1 upload) | Low |
| MCU | 20+ | Lowest | Higher |

### Recommended: LiveKit

LiveKit provides:
- Open-source SFU
- Unity SDK available
- Built-in TURN
- Adaptive simulcast
- E2E encryption support

```csharp
// Unity Integration
using LiveKit;

async void JoinRoom()
{
    var room = new Room();
    await room.Connect("wss://livekit.example.com", token);

    // Publish local video track (Metavido encoded)
    var track = await LocalVideoTrack.CreateScreenShareTrack();
    await room.LocalParticipant.PublishVideoTrack(track);
}
```

---

## 4. Network Requirements

### Bandwidth Per User

| Quality | Resolution | Bitrate | Bandwidth |
|---------|------------|---------|-----------|
| Low | 480p | 500 kbps | 0.6 MB/s |
| Medium | 720p | 1.5 Mbps | 1.8 MB/s |
| High | 1080p | 3 Mbps | 3.6 MB/s |

### Latency Targets

| Metric | Target | Acceptable |
|--------|--------|------------|
| One-way | <100ms | <200ms |
| Round-trip | <200ms | <400ms |
| Jitter | <30ms | <50ms |

### Adaptive Bitrate Strategy

```csharp
// Adjust quality based on network conditions
void OnNetworkQualityChanged(NetworkQuality quality)
{
    switch (quality)
    {
        case NetworkQuality.Poor:
            SetEncodingQuality(480, 500_000); // 480p @ 500kbps
            break;
        case NetworkQuality.Good:
            SetEncodingQuality(720, 1_500_000); // 720p @ 1.5Mbps
            break;
        case NetworkQuality.Excellent:
            SetEncodingQuality(1080, 3_000_000); // 1080p @ 3Mbps
            break;
    }
}
```

---

## 5. Security Considerations

### Authentication
- JWT tokens for room access
- Token expiry: 24 hours
- Room-specific tokens (can't reuse across rooms)

### Encryption
- DTLS for media encryption (WebRTC default)
- WSS for signaling (TLS)
- Optional: E2E encryption with SFrame

### Rate Limiting
- Max 6 users per room
- Max 10 rooms per IP (prevent abuse)
- Connection cooldown: 5 seconds

---

## 6. Deployment Checklist

### Phase 2 (2-user MVP)
- [ ] Deploy signaling server (Node.js)
- [ ] Configure STUN servers
- [ ] Test P2P connection on same WiFi
- [ ] Test P2P connection across networks

### Phase 3 (4+ users)
- [ ] Deploy LiveKit SFU or alternative
- [ ] Configure TURN servers
- [ ] Implement adaptive bitrate
- [ ] Load test with 6 concurrent users

### Production
- [ ] SSL certificates for WSS
- [ ] Health monitoring (Grafana/DataDog)
- [ ] Auto-scaling for SFU
- [ ] Geographic distribution (if global)

---

## References

- [WebRTC Samples](https://webrtc.github.io/samples/)
- [LiveKit Documentation](https://docs.livekit.io/)
- [coturn Documentation](https://github.com/coturn/coturn)
- [Unity WebRTC](https://github.com/Unity-Technologies/com.unity.webrtc)

---

*Created: 2026-01-15*
*Author: Claude Code*
