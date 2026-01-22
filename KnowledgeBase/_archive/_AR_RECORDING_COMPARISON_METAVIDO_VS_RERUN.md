# AR Recording Comparison: Metavido vs Rerun.io

**Created**: 2026-01-21
**Purpose**: Compare volumetric AR recording/playback approaches

---

## Architecture Comparison

| Aspect | Metavido | Rerun.io |
|--------|----------|----------|
| **Primary Purpose** | Volumetric hologram recording | Multimodal data logging/visualization |
| **Data Format** | MP4 video with embedded metadata | .rrd (Apache Arrow serialization) |
| **Storage** | Standard video container | Columnar database format |
| **Metadata** | Barcode in video frame (camera pose) | Separate data channels |
| **Codec** | H.264/HEVC (hardware-accelerated) | N/A (data, not video) |

## Data Flow

### Metavido Pipeline
```
AR Foundation → XRDataProvider → FrameEncoder → Avfi → MP4
                                       ↓
                         Single 1920x1080 frame contains:
                         ├── Color (960x1080 left half)
                         ├── Depth (960x540 right top)
                         └── Metadata barcode (bottom strip)
```

### Rerun Pipeline
```
Sensors → SDK (Python/Rust/C++) → .rrd file or gRPC stream
                                        ↓
                         Separate data channels:
                         ├── Images (any resolution)
                         ├── Point clouds
                         ├── Tensors
                         ├── Transforms (camera poses)
                         └── Timeline metadata
```

## WebGL Playback

| Feature | Metavido | Rerun.io |
|---------|----------|----------|
| **Web Viewer** | Requires custom player | Built-in web viewer |
| **Technology** | VideoPlayer + Custom decode | WebGPU/WebGL (wgpu) |
| **Streaming** | Standard video streaming | gRPC or file drag-drop |
| **Embed** | Custom implementation | iframe or npm package |
| **Format Support** | MP4 only | .rrd (version-locked) |

### Metavido WebGL Challenges
- No native WebGL player - requires porting MetadataDecoder + TextureDemuxer
- Video decoding via HTML5 `<video>` element works
- Metadata barcode decoding needs GLSL shader or JS implementation
- Depth texture extraction needs custom demuxing

### Rerun WebGL Support
- First-class web viewer: `@rerun-io/web-viewer` npm package
- WebGPU by default, WebGL fallback
- Embed via iframe: `https://app.rerun.io/version/{VERSION}/index.html?url={RRD_URL}`
- Version compatibility: .rrd files tied to SDK version (±1 minor version)

## Pros & Cons

### Metavido

**Pros**:
- **Hardware codec acceleration** - iOS AVFoundation for efficient encoding
- **Standard format** - MP4 plays anywhere (with custom depth unpacking)
- **Compact** - Single video file contains all data
- **Low latency** - ~16ms frame time
- **Mobile-optimized** - Designed for iOS/Android
- **Hologram-native** - Built specifically for AR volumetric capture

**Cons**:
- **No web player** - Requires custom WebGL implementation
- **Fixed resolution** - 1920x1080 frame layout is hardcoded
- **Metadata in frame** - Barcode strip reduces usable area
- **Single camera** - No multi-camera or sensor fusion support
- **Unity-only** - No SDK for other engines/platforms

### Rerun.io

**Pros**:
- **Cross-platform SDK** - Python, Rust, C++
- **Web-native** - Built-in web viewer with WebGPU
- **Flexible data** - Any sensor, any rate, any resolution
- **Queryable** - Arrow format enables SQL-like queries
- **Multi-camera** - Designed for robotics with many sensors
- **Timeline scrubbing** - Full temporal navigation

**Cons**:
- **Not video** - Can't use standard video tools/CDNs
- **Version lock** - .rrd files tied to SDK version
- **Large files** - Uncompressed sensor data
- **No codec acceleration** - CPU-bound encoding/decoding
- **Robotics focus** - Not optimized for AR/hologram use cases

## WebGL Playback Feasibility

### Can Metavido videos play in WebGL?

**Partially yes**, with significant work:

1. **Video decoding**: HTML5 `<video>` handles MP4 playback
2. **Metadata extraction**: Need JS/GLSL barcode decoder
3. **Texture demuxing**: Need shader to split Y/CbCr/Depth
4. **VFX rendering**: WebGL VFX is limited vs Unity VFX Graph

**Effort estimate**: ~2-4 weeks for basic playback, no VFX

### Can Rerun .rrd play in WebGL?

**Yes**, natively supported:

1. Embed viewer: `<iframe src="https://app.rerun.io/...?url=file.rrd">`
2. npm package: `@rerun-io/web-viewer` or `@rerun-io/web-viewer-react`
3. Version constraint: Must match SDK version

## Recommendation Matrix

| Use Case | Recommended |
|----------|-------------|
| Mobile AR hologram recording | Metavido |
| Robotics data logging | Rerun |
| WebGL playback needed | Rerun |
| Minimal file size | Metavido |
| Multi-sensor fusion | Rerun |
| Unity VFX integration | Metavido |
| Cross-engine support | Rerun |
| Offline/embedded use | Metavido |

## Hybrid Approach (Future)

For WebGL playback of AR holograms, consider:

1. **Record with Metavido** (compact, hardware-accelerated)
2. **Convert to Rerun** (for web playback)
3. **Or**: Build custom WebGL Metavido player

Conversion would extract:
- Video frames → Rerun Image archetype
- Depth data → Rerun DepthImage archetype
- Camera pose → Rerun Transform3D archetype

---

## References

- [keijiro/Metavido](https://github.com/keijiro/Metavido)
- [keijiro/Avfi](https://github.com/keijiro/Avfi)
- [Rerun.io Docs](https://rerun.io/docs)
- [Rerun Web Viewer](https://rerun.io/docs/howto/integrations/embed-web)
- [@rerun-io/web-viewer npm](https://www.npmjs.com/package/@rerun-io/web-viewer)

---

*Added: 2026-01-21*
