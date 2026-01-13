# Unity URP Post-Processing Guide

> **Source**: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/integration-with-post-processing.html  
> **Last Updated**: 2025-01-13

---

## Overview

URP includes integrated post-processing effects. **No extra package needed** - URP is NOT compatible with Post Processing Stack v2.

URP uses the **Volume framework** for post-processing effects.

---

## Quick Setup

### 1. Enable on Camera
Select Camera → Enable **Post Processing** checkbox

### 2. Add Global Volume
GameObject → Volume → Global Volume

### 3. Create Profile
In Volume component → Click **New** next to Profile

### 4. Add Effects
Add **Volume Overrides** to the Volume component

---

## Important Notes

- Post-processing only applies if camera's **Volume Mask** contains the volume's layer
- **Not supported** on OpenGL ES 2.0

---

## Mobile-Friendly Effects

These effects are optimized for mobile by default:

| Effect | Notes |
|--------|-------|
| **Bloom** | Disable "High Quality Filtering" |
| **Chromatic Aberration** | Mobile-friendly |
| **Color Grading** | Mobile-friendly |
| **Lens Distortion** | Mobile-friendly |
| **Vignette** | Mobile-friendly |

### Depth of Field
- **Mobile/Lower-end**: Use **Gaussian Depth of Field**
- **Console/Desktop**: Use **Bokeh Depth of Field**

### Anti-Aliasing
- **Mobile**: Use **FXAA** (recommended)

---

## VR Considerations

To reduce motion sickness in VR:

| Effect | Recommendation |
|--------|----------------|
| **Vignette** | ✅ Use for fast-paced apps |
| **Lens Distortion** | ❌ Avoid |
| **Chromatic Aberration** | ❌ Avoid |
| **Motion Blur** | ❌ Avoid |

---

## Volume Types

### Global Volume
- Affects entire scene
- No boundaries

### Local Volume
- Location-based effects
- Uses collider for boundaries
- Blend between volumes based on camera position

---

## Common Effects

| Effect | Purpose |
|--------|---------|
| Bloom | Glowing highlights |
| Color Grading | Color correction/LUTs |
| Depth of Field | Focus blur |
| Vignette | Darkened edges |
| Film Grain | Noise texture |
| Chromatic Aberration | Color fringing |
| Lens Distortion | Barrel/pincushion |
| Motion Blur | Movement blur |
| Panini Projection | Wide-angle correction |
| Tonemapping | HDR to LDR |

---

*Created for Unity-XR-AI Knowledge Base*
