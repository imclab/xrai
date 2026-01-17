/**
 * H3M Signaling Server
 * WebSocket-based signaling for WebRTC hologram conferencing
 *
 * Usage:
 *   npm install
 *   npm start
 *
 * Protocol:
 *   { type: 'join', room: 'room-id', peerId: 'peer-id' }
 *   { type: 'leave', room: 'room-id', peerId: 'peer-id' }
 *   { type: 'offer', room: 'room-id', targetPeerId: 'peer-id', sourcePeerId: 'peer-id', sdp: '...' }
 *   { type: 'answer', room: 'room-id', targetPeerId: 'peer-id', sourcePeerId: 'peer-id', sdp: '...' }
 *   { type: 'candidate', room: 'room-id', targetPeerId: 'peer-id', sourcePeerId: 'peer-id', candidate: '...', sdpMid: '...', sdpMLineIndex: 0 }
 */

const WebSocket = require('ws');

const PORT = process.env.PORT || 3003;
const wss = new WebSocket.Server({ port: PORT });

// Map<roomId, Map<peerId, WebSocket>>
const rooms = new Map();

// Map<WebSocket, { room: string, peerId: string }>
const connections = new Map();

console.log(`H3M Signaling Server running on ws://localhost:${PORT}`);

wss.on('connection', (ws) => {
    console.log('New connection');

    ws.on('message', (data) => {
        try {
            const msg = JSON.parse(data.toString());
            handleMessage(ws, msg);
        } catch (e) {
            console.error('Failed to parse message:', e);
        }
    });

    ws.on('close', () => {
        handleDisconnect(ws);
    });

    ws.on('error', (err) => {
        console.error('WebSocket error:', err);
        handleDisconnect(ws);
    });
});

function handleMessage(ws, msg) {
    const { type, room, peerId, targetPeerId, sourcePeerId, sdp, candidate, sdpMid, sdpMLineIndex } = msg;

    switch (type) {
        case 'join':
            handleJoin(ws, room, peerId);
            break;

        case 'leave':
            handleLeave(ws, room, peerId);
            break;

        case 'offer':
        case 'answer':
        case 'candidate':
            relayToPeer(room, targetPeerId, msg);
            break;

        default:
            console.log('Unknown message type:', type);
    }
}

function handleJoin(ws, roomId, peerId) {
    if (!roomId || !peerId) {
        console.error('Missing room or peerId');
        return;
    }

    // Get or create room
    if (!rooms.has(roomId)) {
        rooms.set(roomId, new Map());
    }
    const room = rooms.get(roomId);

    // Get existing peers before adding new one
    const existingPeers = Array.from(room.keys());

    // Add peer to room
    room.set(peerId, ws);
    connections.set(ws, { room: roomId, peerId });

    console.log(`Peer ${peerId} joined room ${roomId} (${room.size} peers)`);

    // Send list of existing peers to new peer
    ws.send(JSON.stringify({
        type: 'room-joined',
        room: roomId,
        peerId: peerId,
        peers: existingPeers
    }));

    // Notify existing peers about new peer
    existingPeers.forEach(existingPeerId => {
        const existingWs = room.get(existingPeerId);
        if (existingWs && existingWs.readyState === WebSocket.OPEN) {
            existingWs.send(JSON.stringify({
                type: 'peer-joined',
                room: roomId,
                sourcePeerId: peerId
            }));
        }
    });
}

function handleLeave(ws, roomId, peerId) {
    const room = rooms.get(roomId);
    if (!room) return;

    room.delete(peerId);
    connections.delete(ws);

    console.log(`Peer ${peerId} left room ${roomId} (${room.size} peers remaining)`);

    // Notify other peers
    room.forEach((client, id) => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(JSON.stringify({
                type: 'peer-left',
                room: roomId,
                sourcePeerId: peerId
            }));
        }
    });

    // Clean up empty rooms
    if (room.size === 0) {
        rooms.delete(roomId);
        console.log(`Room ${roomId} deleted (empty)`);
    }
}

function handleDisconnect(ws) {
    const info = connections.get(ws);
    if (info) {
        handleLeave(ws, info.room, info.peerId);
    }
    connections.delete(ws);
}

function relayToPeer(roomId, targetPeerId, msg) {
    const room = rooms.get(roomId);
    if (!room) {
        console.log(`Room ${roomId} not found`);
        return;
    }

    const targetWs = room.get(targetPeerId);
    if (targetWs && targetWs.readyState === WebSocket.OPEN) {
        targetWs.send(JSON.stringify(msg));
        console.log(`Relayed ${msg.type} from ${msg.sourcePeerId} to ${targetPeerId}`);
    } else {
        console.log(`Target peer ${targetPeerId} not found or not connected`);
    }
}

// Heartbeat to detect disconnected clients
setInterval(() => {
    wss.clients.forEach((ws) => {
        if (ws.isAlive === false) {
            handleDisconnect(ws);
            return ws.terminate();
        }
        ws.isAlive = false;
        ws.ping();
    });
}, 30000);

wss.on('connection', (ws) => {
    ws.isAlive = true;
    ws.on('pong', () => { ws.isAlive = true; });
});

// Graceful shutdown
process.on('SIGINT', () => {
    console.log('\nShutting down...');
    wss.close(() => {
        console.log('Server closed');
        process.exit(0);
    });
});
