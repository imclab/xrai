# HoloKit App - Official Multi-Reality AR Platform

**Source**: [holoi/holokit-app](https://github.com/holoi/holokit-app)
**App Store**: [HoloKit on Apple App Store](https://apps.apple.com/us/app/holokit/id6444073276)
**Unity Version**: 2021.3 LTS
**License**: Apache 2.0

---

## Overview

Official HoloKit app featuring multiple AR "realities" (experiences) with stereo rendering, hand tracking, and colocated multiplayer via Multipeer Connectivity. Uses modular package architecture where each reality is a separate Unity package.

---

## Architecture

```
HoloKitApp/
├── Assets/
│   ├── Constants/               # AvailableRealityList.asset
│   ├── Scenes/                  # App scenes
│   └── ...
│
├── Packages/
│   ├── com.holoi.xr.holokit/                    # Core HoloKit SDK
│   ├── com.holoi.library.holokit-app-lib/       # Base app library
│   ├── com.holoi.library.arux/                  # AR UX helpers
│   ├── com.holoi.library.asset-foundation/      # Shared assets
│   ├── com.holoi.library.shadergraph-assets/    # Shader library
│   │
│   ├── # REALITY PACKAGES
│   ├── com.holoi.reality.ball-and-chain/
│   ├── com.holoi.reality.dragon/
│   ├── com.holoi.reality.light-man/
│   ├── com.holoi.reality.typed/
│   ├── com.holoi.reality.inbetween/
│   ├── com.holoi.reality.blank-pieces-of-paper/
│   │
│   ├── # MOFA (Multiplayer) REALITIES
│   ├── org.realitydeslab.mofa.library.base/
│   ├── org.realitydeslab.mofa.the-ducks/
│   ├── org.realitydeslab.mofa.the-duel/
│   ├── org.realitydeslab.mofa.the-ghost/
│   ├── org.realitydeslab.mofa.the-hunting/
│   ├── org.realitydeslab.mofa.the-training/
│   ├── org.realitydeslab.mofa.object.spells/
│   │
│   ├── # APPLE PLUGINS
│   ├── com.apple.unityplugin.core/
│   ├── com.apple.unityplugin.corehaptics/
│   ├── com.apple.unityplugin.phase/
│   │
│   └── org.realitydeslab.library.permissions/
│
├── WatchApp/                    # Apple Watch companion
└── Documentation/
```

---

## Realities (Experiences)

| Reality Package | Description |
|-----------------|-------------|
| `ball-and-chain` | Physics-based ball on chain |
| `dragon` | AR dragon creature |
| `light-man` | Light-based character |
| `typed` | Typography-based experience |
| `inbetween` | Between-worlds experience |
| `blank-pieces-of-paper` | Paper-based AR |
| `quantum-realm` | Quantum physics visualization |

### MOFA Multiplayer Realities
| Reality | Description |
|---------|-------------|
| `the-ducks` | Multiplayer duck game |
| `the-duel` | 1v1 duel experience |
| `the-ghost` | Ghost hunting multiplayer |
| `the-hunting` | Hunting game |
| `the-training` | Training mode |

---

## Native Plugins

| Plugin | Location |
|--------|----------|
| HoloKitAppIOSNative | `holokit-app-lib/Plugins/` |
| WatchConnectivity | `holokit-app-lib/Plugins/` |
| Permissions | `org.realitydeslab.library.permissions` |
| MPC Transport | `com.holoi.netcode.transport.mpc` |

### Apple Unity Plugins
- CoreHaptics - iOS haptic feedback
- PHASE - Spatial audio
- Core - Base Apple utilities

---

## Key Technologies

- **HoloKit SDK** - Stereo rendering, phone calibration
- **Netcode for GameObjects** - Network multiplayer
- **Multipeer Connectivity** - iOS local networking
- **Apple Watch** - Companion app integration
- **CoreHaptics** - Haptic feedback
- **PHASE** - Spatial audio

---

## Creating New Realities

1. Duplicate `com.holoi.reality.reality-template`
2. Rename to `com.yourname.reality.your-reality`
3. Update `package.json`
4. Create `RealityManager` inheriting from base
5. Configure `Reality` scriptable object
6. Add to `AvailableRealityList.asset`
7. Add scene to build settings

See README.md for detailed instructions.

---

## Build Notes

### Git LFS Required
```bash
git lfs pull
```

### Xcode Build Order Fix
Move "Unity Process symbols" phase before "Embed Watch Content"

### Apple Plugins Build
```bash
python3 build.py -m iOS macOS -b
```

---

## Related KB Files

- `KnowledgeBase/_HAND_SENSING_CAPABILITIES.md` - Hand tracking
- `KnowledgeBase/_WEBRTC_MULTIUSER_MULTIPLATFORM_GUIDE.md` - Networking
- `TouchingHologram/CLAUDE.md` - HoloKit tutorial project
