# Recording Architecture: Metavido + Avfi

**Key Insight**: Metavido and Avfi are separate packages with different responsibilities.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        METAVIDO PACKAGE                              │
│                     (jp.keijiro.metavido)                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐   │
│  │  XRDataProvider │   │  FrameEncoder   │   │    Decoder      │   │
│  │                 │──▶│                 │   │    Components   │   │
│  │  - Y/CbCr       │   │  - Multiplex    │   │                 │   │
│  │  - Depth        │   │  - Metadata     │   │  - MetadataDecoder
│  │  - Stencil      │   │  - 1920x1080 RT │   │  - TextureDemuxer
│  │  - Pose         │   │                 │   │  - VideoFeeder  │   │
│  └─────────────────┘   └────────┬────────┘   └─────────────────┘   │
│                                 │                                    │
│          ENCODING               │                   DECODING         │
│          (Frame Creation)       │              (Frame Playback)      │
└─────────────────────────────────┼────────────────────────────────────┘
                                  │
                                  │ RenderTexture (1920x1080)
                                  │ with embedded metadata barcode
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                          AVFI PACKAGE                                │
│                      (jp.keijiro.avfi)                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    ScreenRecorder                            │   │
│  │                                                               │   │
│  │  Native iOS AVFoundation integration:                        │   │
│  │  - StartRecording(path, width, height)                       │   │
│  │  - AppendFrame(pixelPtr, pixelLength, time)                  │   │
│  │  - EndRecording(saveToGallery)                               │   │
│  │                                                               │   │
│  │  Output: H.264/HEVC MP4 to Camera Roll or temp directory     │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                      │
│                          RECORDING                                   │
│                   (Video File Creation)                              │
└─────────────────────────────────────────────────────────────────────┘
```

## Package Responsibilities

| Package | Purpose | Output |
|---------|---------|--------|
| **jp.keijiro.metavido** | Encode AR data (color+depth+pose) into single frame | RenderTexture 1920x1080 |
| **jp.keijiro.avfi** | Write RenderTexture frames to video file | MP4 file |

## Key Components

### Metavido (Encoding)

- **XRDataProvider**: Collects Y/CbCr, depth, stencil, camera pose from AR Foundation
- **FrameEncoder**: Multiplexes all data into single 1920x1080 frame with metadata barcode

### Avfi (Recording)

- **ScreenRecorder**: Native iOS plugin using AVFoundation for hardware-accelerated H.264/HEVC encoding

## Recording Flow

```csharp
// 1. Setup
XRDataProvider → collects AR textures
FrameEncoder → creates encoded RenderTexture

// 2. Recording
Avfi.ScreenRecorder.StartRecording(path, 1920, 1080);

// 3. Each Frame
Graphics.Blit(frameEncoder.EncodedTexture, buffer);
AsyncGPUReadback.Request(buffer, OnReadback);
// In OnReadback:
Avfi.ScreenRecorder.AppendFrame(pixelPtr, length, time);

// 4. Finalize
Avfi.ScreenRecorder.EndRecording(saveToGallery: true);
```

## Playback Flow

```csharp
// 1. Load Video
VideoPlayer.clip = recordedVideo;

// 2. Decode Metadata
MetadataDecoder → extracts camera pose from barcode

// 3. Demux Textures
TextureDemuxer → separates Color (960x1080) and Depth (960x540)

// 4. Render VFX
VFXARBinder/VFXMetavidoBinder → binds textures to VFX Graph
```

## Installation

```json
// Packages/manifest.json
{
  "scopedRegistries": [{
    "name": "Keijiro",
    "url": "https://registry.npmjs.com",
    "scopes": ["jp.keijiro"]
  }],
  "dependencies": {
    "jp.keijiro.metavido": "5.1.1",
    "jp.keijiro.avfi": "1.0.3"
  }
}
```

## References

- [keijiro/Metavido](https://github.com/keijiro/Metavido)
- [keijiro/Avfi](https://github.com/keijiro/Avfi)
- [keijiro/MetavidoVFX](https://github.com/keijiro/MetavidoVFX)

---

*Created: 2026-01-21*
*Updated: 2026-01-21*
