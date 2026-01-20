# XR + AI Industry Roadmap (2025-2027)

**Last Updated**: 2026-01-20
**Scope**: Tracking, Voice, No-Code, Standards, VFX
**Sources**: Unity, Meta, Google, OpenAI, xAI, Khronos, W3C, USPTO

---

## Unity AI

| Timeline | Change |
|----------|--------|
| 2025 Q3 | Sentis → **Inference Engine** (same ONNX, new name) |
| 2025 Q3 | Muse → **Unity AI** (Generators + Assistant) |
| 2026+ | ML-Agents + Inference Engine integration |

**Key**: Local inference free; cloud uses Unity Points. ONNX compatibility preserved.

---

## Meta XR

| Timeline | Hardware | Notes |
|----------|----------|-------|
| 2025 | Quest 3S, Hands 2.4 | 40% hand tracking latency reduction |
| 2026 | Smart glasses focus | No Quest 4, Ray-Ban Display |
| 2027 | Phoenix (Quest Air) | Ultralight, hand-optimized |

**Key**: Pivot from VR headsets to smart glasses. Llama AI for NPCs.

---

## Google / Android XR

| Timeline | Product |
|----------|---------|
| 2025 Q4 | Android XR OS + Samsung Moohan headset |
| 2026 | Project Aura glasses (with XREAL) |
| 2026+ | Smart glasses from Warby Parker, Gentle Monster |

**Key**: Gemini-native spatial AI. Supports Unity, OpenXR. Cross-iOS glasses planned.

---

## OpenAI GPT

| Model | Spatial Capability |
|-------|-------------------|
| GPT-5 (Aug 2025) | Multimodal visual/spatial reasoning |
| GPT-5.2 (Nov 2025) | 50% error reduction on charts/diagrams |
| GPT-6 (Q1 2026) | Expected significant gains |

**Key**: Enhanced spatial understanding, no dedicated XR product.

---

## xAI Grok

| Timeline | Feature |
|----------|---------|
| 2025 | Grok 4 multimodal, Tesla integration |
| Q1 2026 | Grok 5 (6T params), AI-powered VR game |

**Key**: Gaming/VR focus, cross-platform VR potential.

---

## Patent Trends: Wrist-Based Tracking

| Company | Focus | Date |
|---------|-------|------|
| Apple | Watch ↔ Vision Pro hand tracking | 2024-2025 |
| Apple | Smart ring force sensors | 2024 |
| Google | Wrist-worn IR gesture camera | 2024/01 |
| Meta | Wristband AR input device | 2024 |

**Key**: Industry converging on wrist+headset fusion for hand tracking.

---

## Architecture Implications

1. **Unity Migration**: Sentis → Inference Engine API path exists
2. **Android XR Provider**: Plan for Gemini-integrated tracking SDK
3. **Wrist Device Input**: Design interface for supplementary tracking
4. **Smart Glasses Mode**: Lighter tracking (hands only) for AR glasses
5. **ONNX Portability**: Keep models in ONNX for maximum swappability

---

## Voice AI for XR

| Technology | Latency | Notes |
|------------|---------|-------|
| Whisper v3-turbo | ~200ms | 99+ languages |
| gpt-4o-transcribe | ~300ms | Improved WER |
| Pipecat (OSS) | varies | Multimodal framework |
| ElevenLabs | <200ms | Low-latency TTS |

**Trend**: Hybrid architecture (on-device perception + cloud reasoning) for <200ms turn latency.

---

## No-Code XR Builders

| Tool | Status |
|------|--------|
| Unity Visual Scripting | AR Foundation nodes available |
| Unity Muse/AI | Natural language → scenes |
| Google XR Blocks | WebXR + ML visual prototyping |
| ShapesXR | Voice-driven Quest creation |
| Adobe Aero | Shutting down end 2025 |

---

## Cross-Platform XR Standards

### Khronos (2025)
- **OpenXR 1.1**: Core spec, all major vendors
- **OpenXR Spatial Entities**: Planes, anchors, persistence (public review)
- **glTF**: Gaussian splats extension (with Niantic, Cesium)
- **glTF + USD**: AOUSD liaison for material interop

### W3C WebXR
- **WebXR Device API**: Candidate Rec (Oct 2025)
- **Depth/Occlusion API**: Expected Q4 2026
- **Interop 2026**: Cross-browser compatibility focus

---

## VFX Technology

| Engine | GPU System | Scale |
|--------|-----------|-------|
| Unity | VFX Graph | Millions (GPU) |
| Unreal | Niagara | Millions (GPU) |

**AI + VFX**: Procedural generation, adaptive LOD, pose-driven particles.

**Market**: $464B (2026) → $600B (2034).

---

## References

- [Unity 2025 Roadmap](https://unity.com/blog/unity-engine-2025-roadmap)
- [Unity 6.2 AI](https://www.cgchannel.com/2025/08/unity-rolls-out-unity-ai-in-unity-6-2/)
- [Meta Llama Roadmap](https://pub.towardsai.net/whats-next-for-meta-s-llama-a-roadmap-to-2026)
- [Android XR Overview](https://framesixty.com/android-xr-the-next-wave-of-spatial-computing/)
- [GPT-5.2](https://openai.com/index/introducing-gpt-5-2/)
- [Grok 5 Preview](https://www.sentisight.ai/what-to-expect-from-grok-in-2026/)
- [OpenXR Spatial Entities](https://www.khronos.org/blog/openxr-spatial-entities-extensions-released-for-developer-feedback)
- [WebXR Device API](https://www.w3.org/TR/webxr/)
- [Google XR Blocks](https://research.google/blog/xr-blocks-accelerating-ai-xr-innovation/)
- [Pipecat Framework](https://github.com/pipecat-ai/pipecat)
