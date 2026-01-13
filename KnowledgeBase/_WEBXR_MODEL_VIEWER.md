# WebXR & Model-Viewer AR Reference

> **Source**: https://modelviewer.dev/examples/augmentedreality/  
> **Last Updated**: 2025-01-13

---

## Overview

`<model-viewer>` is a web component for displaying 3D models with AR support. Supports WebXR, Scene Viewer (Android), and Quick Look (iOS).

---

## Basic Usage

```html
<model-viewer
  src="model.glb"
  ar
  ar-modes="webxr scene-viewer quick-look"
  camera-controls
  alt="3D Model">
</model-viewer>
```

---

## AR Modes

| Mode | Platform | Notes |
|------|----------|-------|
| `webxr` | Chrome 83+ Android | Default, stays in browser |
| `scene-viewer` | Android | Native app, requires re-download |
| `quick-look` | iOS | Native AR Quick Look |

### Priority
Specify order in `ar-modes` attribute:
```html
ar-modes="webxr scene-viewer quick-look"
```

---

## Key Attributes

| Attribute | Purpose |
|-----------|---------|
| `ar` | Enable AR functionality |
| `ar-modes` | AR mode priority |
| `ar-scale` | `"auto"` or `"fixed"` (prevent user scaling) |
| `ar-placement` | `"floor"` (default) or `"wall"` |
| `ios-src` | Separate USDZ file for iOS (optional) |
| `xr-environment` | Use estimated lighting in WebXR |

---

## Auto-Generated USDZ

If no `ios-src` specified, model-viewer auto-generates USDZ for iOS Quick Look.

**Limitations**:
- No animation support in auto-generated USDZ
- Use `ios-src` for animations or complex materials

---

## WebXR Requirements

1. **HTTPS** required
2. If in iframe, must allow `xr-spatial-tracking` policy:
```html
<iframe allow="xr-spatial-tracking">
```

---

## Custom AR Button

Use slots to replace default AR button:

```html
<model-viewer ar>
  <button slot="ar-button" class="custom-ar-btn">
    View in AR
  </button>
</model-viewer>
```

---

## AR Status Styling

Style based on `ar-status` attribute:

```css
/* Show only when AR not tracking */
model-viewer[ar-status="not-presenting"] .ar-prompt {
  display: none;
}

model-viewer[ar-status="session-started"] .ar-prompt {
  display: block;
}
```

---

## Wall Placement

```html
<model-viewer
  ar
  ar-placement="wall"
  src="painting.glb">
</model-viewer>
```

---

## Transparent Background

Model-viewer supports transparent objects showing page background in AR.

---

## Example: Full AR Setup

```html
<model-viewer
  src="astronaut.glb"
  ios-src="astronaut.usdz"
  ar
  ar-modes="webxr scene-viewer quick-look"
  ar-scale="fixed"
  ar-placement="floor"
  xr-environment
  camera-controls
  auto-rotate
  alt="Astronaut model">
  
  <button slot="ar-button" id="ar-btn">
    👋 View in AR
  </button>
  
  <div class="ar-prompt">
    Move your phone to find a surface
  </div>
</model-viewer>

<style>
  model-viewer {
    width: 100%;
    height: 400px;
  }
  
  #ar-btn {
    position: absolute;
    top: 16px;
    right: 16px;
    background: #fff;
    border-radius: 8px;
    padding: 8px 16px;
  }
  
  .ar-prompt {
    display: none;
  }
  
  model-viewer[ar-status="session-started"] .ar-prompt {
    display: block;
    position: absolute;
    bottom: 32px;
    left: 50%;
    transform: translateX(-50%);
  }
</style>
```

---

## Resources

- [model-viewer Documentation](https://modelviewer.dev/)
- [WebXR Device API](https://developer.mozilla.org/en-US/docs/Web/API/WebXR_Device_API)
- [AR Quick Look](https://developer.apple.com/augmented-reality/quick-look/)
- [Scene Viewer](https://developers.google.com/ar/develop/scene-viewer)

---

*Created for Unity-XR-AI Knowledge Base*
