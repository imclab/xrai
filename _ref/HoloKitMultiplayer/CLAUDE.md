# HoloKit Colocated Multiplayer Boilerplate

**Source**: [holoi/holokit-colocated-multiplayer-boilerplate](https://github.com/holoi/holokit-colocated-multiplayer-boilerplate)
**Unity Version**: 2022.3 LTS
**License**: MIT

---

## Overview

Boilerplate project for building colocated multiplayer AR experiences using HoloKit SDK. Uses Multipeer Connectivity for iOS-to-iOS networking with image marker alignment for shared coordinate spaces.

---

## Key Features

- **Colocated Multiplayer** - Same physical space AR
- **Image Marker Alignment** - Shared world coordinate
- **Multipeer Connectivity** - Local iOS networking
- **Spectator View** - Third-person camera for recording
- **HoloKit Stereo** - MR headset support

---

## Project Structure

```
HoloKitMultiplayer/
├── Assets/
│   ├── Scenes/
│   ├── Scripts/
│   ├── Prefabs/
│   └── ...
├── Packages/
│   └── manifest.json
└── ProjectSettings/
```

---

## Networking Architecture

```
┌─────────────────┐     Multipeer      ┌─────────────────┐
│   Host iPhone   │◄──────────────────►│  Client iPhone  │
│                 │   Connectivity     │                 │
│ ┌─────────────┐ │                    │ ┌─────────────┐ │
│ │ AR Session  │ │                    │ │ AR Session  │ │
│ └─────────────┘ │                    │ └─────────────┘ │
│        │        │                    │        │        │
│   Image Marker  │◄───────────────────│   Image Marker  │
│   Alignment     │   Shared Origin    │   Alignment     │
└─────────────────┘                    └─────────────────┘
```

---

## Colocated AR Workflow

1. **Host** scans image marker → establishes world origin
2. **Client** joins via Multipeer Connectivity
3. **Client** scans same marker → aligns to shared origin
4. **Netcode** syncs game objects across devices
5. All players see objects in same physical locations

---

## Dependencies

- HoloKit Unity SDK
- Netcode for GameObjects
- Multipeer Connectivity Transport
- AR Foundation
- URP

---

## Quick Start

1. Open main scene
2. Build to two iOS devices
3. Host starts session
4. Client joins
5. Both scan shared image marker
6. AR objects appear at same physical locations

---

## Integration Patterns

### Shared World Origin
```csharp
// After scanning image marker
worldOrigin = detectedImage.transform;
NetworkManager.Singleton.transform.SetParent(worldOrigin);
```

### Spawn Networked Object
```csharp
[ServerRpc]
void SpawnObjectServerRpc(Vector3 position) {
    var obj = Instantiate(prefab, position, Quaternion.identity);
    obj.GetComponent<NetworkObject>().Spawn();
}
```

---

## Related Projects

- `HoloKitApp/` - Full HoloKit app with realities
- `TouchingHologram/` - Hand tracking tutorial
- `KnowledgeBase/_WEBRTC_MULTIUSER_MULTIPLATFORM_GUIDE.md`
