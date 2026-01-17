# WebRTC Multiuser Multiplatform Scalability Guide

**Platforms**: iOS, Android, WebGL, React-Unity, Quest, visionOS
**Last Updated**: 2026-01-17
**Research Sources**: Unity docs, Photon, Normcore, coherence, community forums

---

## Platform Compatibility Matrix

| Solution | iOS | Android | WebGL | Quest | visionOS | React-Unity |
|----------|-----|---------|-------|-------|----------|-------------|
| **Unity WebRTC** | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ |
| **Photon Fusion** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Normcore** | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ |
| **coherence** | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ |
| **Netcode for GO** | ✅ | ✅ | ⚠️ | ✅ | ⚠️ | ✅ |
| **Mirror** | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ |

Legend: ✅ Full support | ⚠️ Limited/experimental | ❌ Not supported

---

## Solution Comparison

### 1. Photon Fusion 2 (Recommended for XR)

**Best for**: Large-scale, cross-platform XR (Quest + visionOS), competitive games

**Pros**:
- Official Quest + visionOS cross-platform samples
- Battle-tested scalability (millions of CCU)
- Rich XR tooling (XRShared addon, grabbing, teleport)
- Tick-based simulation with rollback netcode

**Cons**:
- CCU-based pricing can get expensive
- Steeper learning curve than alternatives
- Requires Photon Cloud or self-hosted servers

**Pricing**: Free up to 20 CCU, then tiered CCU pricing

**Key Repos**:
- [Photon Fusion VR Shared Sample](https://doc.photonengine.com/fusion/current/technical-samples/fusion-vr-shared)
- [Cross-platform MR (Quest + visionOS)](https://doc.photonengine.com/arvr/current/apple-vision-pro/crossplatform-mixed-reality)

---

### 2. Normcore (Recommended for Simplicity)

**Best for**: XR multiplayer, voice chat, networked physics, indie projects

**Pros**:
- Simplest setup ("add component and go")
- Built-in voice chat
- Excellent networked physics (local simulation switching)
- Room-hour pricing (better for short sessions)
- Auto-scaling to millions of users

**Cons**:
- Less control than alternatives
- visionOS support unclear
- Hosted-only (no self-hosting)

**Pricing**: Free tier, then room-hour based

**Key Repos**:
- [Normcore Samples](https://github.com/NormalVR/Normcore-Samples)
- [Quest Hands for Normcore](https://github.com/absurd-joy/Quest-hands-for-Normcore)

**Reference**: [Unity Multiplayer VR with Normcore](https://jeffrafter.com/unity-multiplayer-vr-with-normcore/)

---

### 3. coherence (Recommended for WebGL + Mobile)

**Best for**: WebGL games, flexible architecture, credit-based pricing

**Pros**:
- Seamless WebGL builds (WebRTC + WebSocket fallback)
- Credit-based pricing (pay for what you use)
- Supports cloud, P2P, self-hosted, or hybrid
- Built-in LOD, area of interest, delta compression
- Free during development

**Cons**:
- Newer platform (less community resources)
- visionOS not explicitly documented

**Pricing**:
- Develop: 10,000 credits/month FREE
- Launch: 25,000 credits + $0.16/100 extra
- Scale: 500,000 credits + $0.13/100 extra

**Key Features**:
- UDP transport with WebRTC fallback for WebGL
- TCP fallback for mobile (reliability)
- Automatic bandwidth optimization

**Reference**: [coherence.io](https://coherence.io/) | [Unity Asset Store](https://assetstore.unity.com/packages/tools/network/coherence-multiplayer-networking-301977)

---

### 4. Unity Netcode for GameObjects (NGO)

**Best for**: Small Unity-native games (2-10 players), rapid prototyping

**Pros**:
- Official Unity solution
- Integrates with Unity Relay & Lobby
- No external dependencies
- Free

**Cons**:
- Limited WebGL support (requires custom transport)
- Not designed for large scale without customization
- Requires networking knowledge for complex mechanics

**Key Repos**:
- [Netcode Transport Multipeer Connectivity](https://github.com/holokit/netcode-transport-multipeer-connectivity) - iOS/macOS/visionOS local multiplayer

---

### 5. Mirror (Open Source)

**Best for**: Self-hosted, no CCU limits, full control

**Pros**:
- Free and open source
- No CCU limits
- WebGL support via [mirror-webgl](https://github.com/edgegap/mirror-webgl)
- Large community

**Cons**:
- Self-hosted only (need your own servers)
- More setup required
- Less XR-specific tooling

---

## WebGL-Specific Considerations

### Browser Limitations
- No raw UDP sockets in browsers
- Must use **WebRTC** or **WebSockets**
- Chrome requires HTTPS for WebRTC
- Signaling server must use WSS (secure WebSockets)

### Recommended Stack for WebGL
```
Unity → coherence/Photon → WebRTC transport → Browser
                         ↓
              WebSocket fallback (if WebRTC fails)
```

### Key WebGL Repos
| Repo | Description |
|------|-------------|
| [Unity-Technologies/com.unity.webrtc](https://github.com/Unity-Technologies/com.unity.webrtc) | Official Unity WebRTC package |
| [Unity-Technologies/UnityRenderStreaming](https://github.com/Unity-Technologies/UnityRenderStreaming) | Streaming server for Unity |
| [ossrs/srs-unity](https://github.com/ossrs/srs-unity) | WebRTC samples with SRS SFU server |
| [edgegap/mirror-webgl](https://github.com/edgegap/mirror-webgl) | Mirror networking for WebGL |
| [danbuckland/aframe-socket-io](https://github.com/danbuckland/aframe-socket-io) | A-Frame + Socket.IO + WebRTC multiplayer |
| [networked-aframe/networked-aframe](https://github.com/networked-aframe/networked-aframe) | Multi-user A-Frame (alternative) |

---

## React-Unity Integration

### Architecture Options

**Option A: WebRTC in React Native layer**
```
React Native → react-native-webrtc → Signaling Server
     ↓
Unity (embedded) ← Message Bridge → React Native
```

**Option B: WebRTC in Unity layer**
```
React Native → @azesmway/react-native-unity → Unity
                                                ↓
                                        com.unity.webrtc → Server
```

### Key Packages
| Package | Purpose |
|---------|---------|
| [@azesmway/react-native-unity](https://github.com/azesmway/react-native-unity) | Embed Unity in React Native |
| [react-native-webrtc](https://github.com/react-native-webrtc/react-native-webrtc) | WebRTC for React Native |
| [com.unity.webrtc](https://github.com/Unity-Technologies/com.unity.webrtc) | WebRTC for Unity |

### Expo Considerations
- react-native-webrtc requires `expo-dev-client`
- Use [config-plugins/react-native-webrtc](https://github.com/AzzappApp/config-plugins-react-native-webrtc) for Expo

---

## visionOS Support

### Current State (2026-01)
- **Photon Fusion**: ✅ Official cross-platform samples (Quest + visionOS)
- **Normcore**: ⚠️ Not explicitly documented
- **coherence**: ⚠️ Not explicitly documented
- **Netcode + Multipeer**: ✅ Via [netcode-transport-multipeer-connectivity](https://github.com/holokit/netcode-transport-multipeer-connectivity)

### Recommended Approach for visionOS
1. Use **Photon Fusion** with XRShared addon
2. Use **PolySpatial** helpers for bounded/unbounded state sync
3. Handle indirect pinch compatibility for grabbing

---

## AR Foundation + WebRTC Integration

### Capturing AR Camera for WebRTC Streaming

**Problem**: AR Foundation's camera background uses external textures (YCbCr/NV12) that can't be directly sent via WebRTC.

**Solution**: Use `Graphics.Blit` with AR background material at `endCameraRendering` callback.

```csharp
public class ARCameraWebRTCCapture : MonoBehaviour
{
    [SerializeField] private ARCameraBackground m_ARCameraBackground;
    [SerializeField] private RenderTexture targetRenderTexture;

    private Texture2D m_LastCameraTexture;

#if !UNITY_EDITOR
    void OnEnable()
    {
        // URP: Use RenderPipelineManager callback (not OnPostRender)
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        CaptureARFrameForWebRTC();
    }
#endif

    private void CaptureARFrameForWebRTC()
    {
        // Blit AR background (converts YCbCr → RGBA)
        Graphics.Blit(null, targetRenderTexture, m_ARCameraBackground.material);

        var activeRT = RenderTexture.active;
        RenderTexture.active = targetRenderTexture;

        // WebRTC requires RGBA32 format
        if (m_LastCameraTexture == null)
            m_LastCameraTexture = new Texture2D(
                targetRenderTexture.width,
                targetRenderTexture.height,
                TextureFormat.RGBA32,
                mipChain: false);

        m_LastCameraTexture.ReadPixels(
            new Rect(0, 0, targetRenderTexture.width, targetRenderTexture.height), 0, 0);
        m_LastCameraTexture.Apply();
        RenderTexture.active = activeRT;

        // Send to WebRTC
        byte[] rawData = m_LastCameraTexture.GetRawTextureData();
        // videoInput.UpdateFrame(deviceName, rawData, width, height, ImageFormat.kABGR);
    }

    void OnDestroy()
    {
        if (m_LastCameraTexture != null)
            Destroy(m_LastCameraTexture);
    }
}
```

### Key Considerations

| Issue | Solution |
|-------|----------|
| Memory leak (CPU image API) | Use GPU path via `Graphics.Blit` |
| URP timing | Use `RenderPipelineManager.endCameraRendering` not `OnPostRender` |
| Format mismatch | Convert to RGBA32 for WebRTC |
| Editor testing | Wrap device code in `#if !UNITY_EDITOR` |
| Performance | Consider async GPU readback for high FPS |

### Alternative: AsyncGPUReadback (Recommended for Performance)

```csharp
AsyncGPUReadback.Request(targetRenderTexture, 0, TextureFormat.RGBA32, OnGPUReadback);

void OnGPUReadback(AsyncGPUReadbackRequest request)
{
    if (!request.hasError)
    {
        NativeArray<byte> data = request.GetData<byte>();
        // Send to WebRTC without blocking main thread
    }
}
```

**Reference**: [AR Foundation Issue #973](https://github.com/Unity-Technologies/arfoundation-samples/issues/973)

---

## Scalability Recommendations

### By Player Count

| Scale | Recommendation |
|-------|----------------|
| 2-10 players | Normcore, NGO, Mirror |
| 10-100 players | Photon Fusion, coherence |
| 100-1000 players | Photon Fusion (room sharding) |
| 1000+ players | Netcode for Entities (DOTS), custom solution |

### By Use Case

| Use Case | Recommendation |
|----------|----------------|
| XR Social (Quest/visionOS) | **Photon Fusion** or **Normcore** |
| WebGL Browser Game | **coherence** (best WebRTC handling) |
| Mobile AR Multiplayer | **Photon Fusion** or **coherence** |
| React-Unity Hybrid | **react-native-webrtc** + Unity bridge |
| Indie/Budget | **Mirror** (free, self-hosted) |
| Voice Chat Required | **Normcore** (built-in) or Photon Voice |

---

## Quick Start Recommendations

### For Your Stack (iOS/Android/WebGL/Quest/visionOS + React-Unity)

**Primary: Photon Fusion 2**
- Best cross-platform XR support
- Official visionOS + Quest samples
- WebGL via WebRTC transport
- React-Unity: Use Unity-side networking

**Alternative: coherence**
- Better WebGL handling (automatic fallbacks)
- Credit-based pricing (more predictable)
- Simpler setup than Photon

**For Voice Chat: Add Normcore or Photon Voice**

---

## Related KB Files

- `_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` - 520+ repos including networking
- `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` - AR Foundation patterns
- `_PLATFORM_COMPATIBILITY_MATRIX.md` - Platform support details

---

## Sources

- [Unity WebRTC Package](https://github.com/Unity-Technologies/com.unity.webrtc)
- [Photon Fusion Documentation](https://doc.photonengine.com/fusion)
- [Normcore Documentation](https://normcore.io/documentation)
- [coherence Documentation](https://docs.coherence.io/)
- [Unity Netcode Documentation](https://docs-multiplayer.unity3d.com/)
- [Unity WebGL Networking Manual](https://docs.unity3d.com/Manual/webgl-networking.html)
- [Multiplayer in Unity 2025 Guide](https://appwill.co/multiplayer-in-unity-best-networking-solutions-2025/)
- [Unity Blog: Choosing Netcode](https://blog.unity.com/technology/choosing-the-right-netcode-for-your-game)
