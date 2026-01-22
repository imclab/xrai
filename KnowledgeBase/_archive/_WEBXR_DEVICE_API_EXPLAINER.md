# WebXR Device API Explainer

**Source**: [Immersive Web WebXR Explainer](https://immersive-web.github.io/webxr/explainer.html)
**Spec**: [WebXR Device API](https://immersive-web.github.io/webxr/)
**Last Updated**: 2026-01-17

---

## What is WebXR?

The WebXR Device API provides access to input and output capabilities commonly associated with Virtual Reality (VR) and Augmented Reality (AR) devices. It enables web-based immersive experiences.

### Goals
- Detect if XR capabilities are available
- Query the XR device capabilities
- Poll the XR device and associated input device state
- Display imagery on the XR device at the appropriate frame rate

### Non-Goals
- Define how a VR/AR browser would work
- Expose every feature of every piece of VR/AR hardware
- Build "The Metaverse"

### What's the "X" in XR?
The "X" is an algebraic variable meaning "Your Reality Here" - covering Virtual Reality, Augmented Reality, Mixed Reality, Extended Reality, Cross Reality, etc.

---

## Target Hardware

| Device Type | Examples |
|-------------|----------|
| Mobile AR | ARCore/ARKit-compatible devices |
| Mobile VR | Google Daydream, Samsung Gear VR |
| PC VR | HTC Vive, Oculus Rift, Windows MR |
| Standalone | Meta Quest, Magic Leap One |
| AR Glasses | Microsoft HoloLens |

---

## Use Cases

### Video
360° and 3D video playback with head-tracking. Users can view content on a theater-sized virtual screen or as immersive 360° experiences.

### Object/Data Visualization
- 3D model viewing (e.g., SketchFab)
- Architectural previsualizations
- Medical imaging
- Mapping and data visualization
- Home shopping (e.g., Matterport)

Scale from photo carousel → interactive 3D → full VR walkthrough based on device capabilities.

### Artistic Experiences
VR provides experimental artists a **frictionless distribution method** via web:
- No app store friction
- Shorter, abstract, experimental experiences
- Easily attract viewers with a link
- Single codebase for all platforms

> "The web's transient nature makes these types of applications more appealing, since they provide a frictionless way of viewing the experience."

---

## Session Modes

| Mode | Enum | Description |
|------|------|-------------|
| **Inline** | `'inline'` | "Magic Window" - renders to page, accesses device tracking |
| **Immersive VR** | `'immersive-vr'` | Presents directly to headset |

---

## Lifetime of a WebXR App

### 1. Query Support
```javascript
navigator.xr.isSessionSupported('immersive-vr').then((supported) => {
  if (supported) {
    // Add "Enter VR" button to page
  }
});
```

### 2. Request Session (requires user gesture)
```javascript
function beginXRSession() {
  navigator.xr.requestSession('immersive-vr')
    .then(onSessionStarted)
    .catch(err => {
      // Render without XR
      window.requestAnimationFrame(onDrawFrame);
    });
}
```

### 3. Setup Session
```javascript
let xrSession = null;
let xrReferenceSpace = null;

function onSessionStarted(session) {
  xrSession = session;

  xrSession.requestReferenceSpace('local')
    .then((referenceSpace) => {
      xrReferenceSpace = referenceSpace;
    })
    .then(setupWebGLLayer)
    .then(() => {
      xrSession.requestAnimationFrame(onDrawFrame);
    });
}
```

### 4. Create XRWebGLLayer
```javascript
function setupWebGLLayer() {
  return gl.makeXRCompatible().then(() => {
    xrSession.updateRenderState({
      baseLayer: new XRWebGLLayer(xrSession, gl)
    });
  });
}
```

### 5. Render Loop
```javascript
function onDrawFrame(timestamp, xrFrame) {
  if (xrSession) {
    let glLayer = xrSession.renderState.baseLayer;
    let pose = xrFrame.getViewerPose(xrReferenceSpace);

    if (pose) {
      gl.bindFramebuffer(gl.FRAMEBUFFER, glLayer.framebuffer);

      for (let view of pose.views) {
        let viewport = glLayer.getViewport(view);
        gl.viewport(viewport.x, viewport.y, viewport.width, viewport.height);
        drawScene(view);
      }
    }

    xrSession.requestAnimationFrame(onDrawFrame);
  }
}
```

### 6. End Session
```javascript
xrSession.end();
```

---

## Key Concepts

### XRFrame
- Snapshot of XR device state at a point in time
- Only valid during the callback it's passed to
- Contains `XRViewerPose` with tracking data

### XRViewerPose
- Retrieved via `xrFrame.getViewerPose(referenceSpace)`
- Contains array of `XRView` objects for stereo rendering

### XRView
- `projectionMatrix` - 4x4 projection matrix
- `transform` - `XRRigidTransform` with position and orientation
- Used with `glLayer.getViewport(view)` for viewport setup

### XRRigidTransform
```javascript
// Position (Vector3)
view.transform.position.x
view.transform.position.y
view.transform.position.z

// Orientation (Quaternion)
view.transform.orientation.x
view.transform.orientation.y
view.transform.orientation.z
view.transform.orientation.w

// Full 4x4 matrix
view.transform.matrix
```

---

## WebGL Context Compatibility

### XR Enhanced Apps (Progressive Enhancement)
```javascript
// Make existing context XR-compatible (may cause context loss)
gl.makeXRCompatible().then(() => {
  // Context is now compatible
});
```

### XR Centric Apps (Primary Use Case)
```javascript
// Create XR-compatible context from the start
let gl = canvas.getContext("webgl", { xrCompatible: true });
```

### Handle Context Loss
```javascript
canvas.addEventListener("webglcontextlost", (event) => {
  event.preventDefault();
});

canvas.addEventListener("webglcontextrestored", () => {
  loadSceneGraphics(gl);
});
```

---

## Advanced Features

### Feature Dependencies
```javascript
navigator.xr.requestSession('immersive-vr', {
  requiredFeatures: ['local-floor'],  // Session fails if unavailable
  optionalFeatures: ['hand-tracking'] // Session succeeds regardless
});
```

### Framebuffer Scaling
```javascript
// Adjust rendering resolution (1.0 = default)
new XRWebGLLayer(session, gl, {
  framebufferScaleFactor: 0.5  // Half resolution for performance
});

// Query native resolution
XRWebGLLayer.getNativeFramebufferScaleFactor(session);
```

### Depth Control
```javascript
xrSession.updateRenderState({
  depthNear: 0.1,
  depthFar: 1000.0
});
```

### Session Visibility States
| State | Description |
|-------|-------------|
| `visible` | Active rendering |
| `visible-blurred` | Reduced framerate, UA may reproject |
| `hidden` | No device state access |

```javascript
xrSession.addEventListener('visibilitychange', (event) => {
  if (xrSession.visibilityState === 'visible-blurred') {
    // Reduce rendering complexity
  }
});
```

---

## Spatial Audio Integration

Synchronize `AudioContext.listener` with viewer pose:

```javascript
const m = pose.transform.matrix;

// Forward direction (-Z)
listener.forwardX.value = -m[8];
listener.forwardY.value = -m[9];
listener.forwardZ.value = -m[10];

// Up direction (+Y)
listener.upX.value = m[4];
listener.upY.value = m[5];
listener.upZ.value = m[6];

// Position
listener.positionX.value = m[12];
listener.positionY.value = m[13];
listener.positionZ.value = m[14];
```

---

## Why Not Existing APIs?

| API | Problem |
|-----|---------|
| **DeviceOrientation Events** | Non-standard, lack positional data, skip frames |
| **WebSockets** | High latency incompatible with 20ms motion-to-photons |
| **Gamepad API** | Normalized axes unsuitable for spatial data |
| **WebVR (legacy)** | Made assumptions limiting AR support |

WebXR supersedes WebVR with improved architecture supporting diverse device types.

---

## OpenXR Relationship

WebXR and Khronos' OpenXR are **distinct APIs** developed by different standards bodies. Unlike WebGL/OpenGL, WebXR is NOT a 1:1 mapping of OpenXR.

However, OpenXR is a reasonable native backend for implementing WebXR features.

---

## Reference Spaces

See [Spatial Tracking Explainer](https://immersive-web.github.io/webxr/spatial-tracking-explainer.html) for details.

| Space | Description |
|-------|-------------|
| `viewer` | Head-locked, always available |
| `local` | Seated/standing, origin at initial position |
| `local-floor` | Standing, floor-level origin |
| `bounded-floor` | Room-scale with boundaries |
| `unbounded` | Large-scale AR/VR |

---

## Device Change Events

```javascript
navigator.xr.addEventListener('devicechange', () => {
  // XR device connected/disconnected
  // Re-check session support
  checkForXRSupport();
});
```

---

## Unity WebXR Integration

### De-Panther/unity-webxr-export (Recommended)

**Repo**: [github.com/De-Panther/unity-webxr-export](https://github.com/De-Panther/unity-webxr-export)
**Demo**: [Live VR Demo](https://de-panther.github.io/unity-webxr-export/Build) | [XR Interaction Toolkit Demo](https://de-panther.github.io/unity-webxr-export/XRInteractionToolkitDemo)

Integrates the WebXR JavaScript API into Unity WebGL, enabling VR/AR development in Unity with C#.

#### Installation (OpenUPM)
```bash
# WebXR Export
openupm add com.de-panther.webxr

# WebXR Interactions
openupm add com.de-panther.webxr-interactions
```

#### Unity Version Compatibility
| Version | Support |
|---------|---------|
| 2020.3.11f1+ | ✅ |
| 2021.1.4f1+ | ✅ |
| 2022.1+ | ✅ |
| 2023.1+ | ✅ |
| 6000.0.23f1+ | ✅ |

#### Browser/Device Compatibility
| Platform | Browser | VR | AR |
|----------|---------|:--:|:--:|
| Windows | Chrome, Edge | ✅ | - |
| Meta Quest (1/2/Pro/3) | Quest Browser, Wolvic | ✅ | ✅ |
| HoloLens 2 | Edge | ✅ | ✅ |
| Android | Chrome, Samsung Internet | ✅ | ✅ |
| iOS | Mozilla WebXR Viewer | ✅ | ✅ |
| VIVE Focus Plus | Firefox Reality | ✅ | - |
| VIVE Focus 3 | Wolvic | ✅ | - |
| Magic Leap 2 | Helio | ✅ | ✅ |
| PICO 4/4E | PICO Browser, Wolvic | ✅ | - |
| Apple Vision Pro | Safari (with flags) | ⚠️ | ⚠️ |

⚠️ Vision Pro: Requires enabling flags in system settings. No `selected` event on hand pinch.

#### WebXR APIs Supported
| API | Status |
|-----|--------|
| WebXR Device API | ✅ Unity Display/Input XR Subsystems |
| WebXR Gamepads Module | ✅ Unity New Input System |
| WebXR AR Module | ✅ |
| WebXR Hit Test | ✅ (viewer space only, not AR Foundation) |
| WebXR Hand Input | ✅ Unity XR Hands package |
| WebXR Input Profiles | ✅ XR Interaction Toolkit |
| Haptic Feedback | ✅ on supported devices |

#### Additional Features
- **Spectator Camera** - Third-person view for streaming
- **Mixed Reality Capture** - Record MR content
- **WebXR Polyfill** - Fallback for unsupported browsers
- **Passthrough/Seethrough** - Not using AR Foundation

#### Roadmap
- Version 0.20.0+: Unity XR SDK support
- Next: AR Foundation support
- Note: Built-in Render Pipeline dropped in 0.20.0 (URP/HDRP only)

### Other Options

| Package | Description |
|---------|-------------|
| [needle-engine](https://needle.tools/) | Unity to WebXR runtime (three.js based) |
| [SimpleWebXR](https://github.com/Rufus31415/Simple-WebXR-Unity) | Lightweight WebXR for Unity |
| [three.js](https://threejs.org/) + WebXR | Direct JavaScript implementation |

### Useful Resources
- [WebXR Input Profile Viewer](https://immersive-web.github.io/webxr-input-profiles/packages/viewer/dist/index.html)
- [WebXR Samples](https://immersive-web.github.io/webxr-samples/)
- [WebXR Polyfill](https://github.com/immersive-web/webxr-polyfill)
- [WebXR Discord](https://discord.gg/Jt5tfaM)

---

## Related KB Files

- `_WEBRTC_MULTIUSER_MULTIPLATFORM_GUIDE.md` - Multiplayer networking
- `_WEBGL_THREEJS_COMPREHENSIVE_GUIDE.md` - WebGL/Three.js reference
- `_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` - Related repos

---

## Sources

- [WebXR Device API Explainer](https://immersive-web.github.io/webxr/explainer.html)
- [WebXR Device API Spec](https://immersive-web.github.io/webxr/)
- [Spatial Tracking Explainer](https://immersive-web.github.io/webxr/spatial-tracking-explainer.html)
- [Immersive Web Community Group](https://www.w3.org/community/immersive-web/)
