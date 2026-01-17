# H3M Signaling Server (Optional Self-Hosting)

This is an optional self-hosted WebSocket signaling server for H3M hologram conferencing.

## When to Use This

**You probably don't need this!**

The default `HologramConferenceManager` uses the WebRtcVideoChat library which includes built-in signaling at `wss://s.y-not.app/conferenceapp`. This works out of the box with no server setup required.

**Use this server only if:**
- You need a fully self-hosted solution
- You want custom signaling logic
- You're in a restricted network that can't reach `s.y-not.app`
- You need to scale beyond the free tier limits

## Quick Start

```bash
cd SignalingServer
npm install
npm start
```

Server runs at `ws://localhost:3003`

## Protocol

```json
// Join a room
{ "type": "join", "room": "room-id", "peerId": "peer-id" }

// Leave a room
{ "type": "leave", "room": "room-id", "peerId": "peer-id" }

// WebRTC signaling
{ "type": "offer", "room": "room-id", "targetPeerId": "...", "sourcePeerId": "...", "sdp": "..." }
{ "type": "answer", "room": "room-id", "targetPeerId": "...", "sourcePeerId": "...", "sdp": "..." }
{ "type": "candidate", "room": "room-id", "targetPeerId": "...", "sourcePeerId": "...", "candidate": "...", "sdpMid": "...", "sdpMLineIndex": 0 }
```

## Using with Unity

To use this server instead of the built-in signaling, you would need to:

1. Modify `HologramConferenceManager.cs` to use custom `NetworkConfig.SignalingUrl`
2. Implement `H3MSignalingClient.cs` (currently a stub)

**Recommended approach:** Just use the built-in WebRtcVideoChat signaling unless you have specific requirements.

## Deployment

For production, deploy to any Node.js host (Heroku, Railway, Fly.io, etc.) and use WSS (WebSocket Secure):

```bash
npm run start
```

Set `PORT` environment variable if needed.
