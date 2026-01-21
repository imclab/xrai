# AR Foundation 6.x Complete Tracking Capabilities Reference

**Date**: 2026-01-20
**Version**: AR Foundation 6.2.1, XR Hands 1.7.1
**Status**: Production Reference

---

## Executive Summary

AR Foundation 6.x provides **16 native tracking subsystems**. iOS ARKit has superior support (body 91j, face 468pt, LiDAR meshing). This document catalogs ALL tracking capabilities with implementation status in MetavidoVFX.

---

## 1. Human Body Tracking (ARHumanBodyManager)

**Status**: iOS ARKit 3+ only | **MetavidoVFX**: Integrated

### Capabilities
- **91-joint skeleton** (not 17!) - Complete articulation
  - 48 finger joints (24 per hand)
  - 7-segment spine
  - 18 head/facial joints
- **2D joint tracking** - Screen-space positions
- **3D joint tracking** - World-space with confidence
- **Pose estimation** - Real-time body pose
- **Event callbacks** - `humanBodiesChanged`

### Data Access
```csharp
ARHumanBody body = humanBodyManager.trackables.First();
NativeArray<XRHumanBodyJoint> joints = body.joints; // 91 joints
Vector3 position = joints[i].localPose.position;
Quaternion rotation = joints[i].localPose.rotation;
float confidence = joints[i].trackingState;
```

### Joint Index Reference (Key)
| Index | Joint | Index | Joint |
|-------|-------|-------|-------|
| 0 | Hips | 12 | LeftShoulder |
| 1 | LeftUpLeg | 13 | RightShoulder |
| 2 | RightUpLeg | 14 | Neck |
| 3 | LeftLeg | 15 | LeftHand (start) |
| 4 | RightLeg | 63 | RightHand (start) |
| 5 | LeftFoot | 87 | Head |
| 6 | RightFoot | 88-90 | Eyes, Jaw |

### Performance
- 353 FPS @ 10 active VFX (verified Jan 16, 2026)
- ~2-3ms inference time

---

## 2. Human Segmentation / Occlusion (AROcclusionManager)

**Status**: iOS ARKit 15+ (full) / Android ARCore (limited) | **MetavidoVFX**: PRIMARY

### Capabilities
| Texture | Resolution | Purpose |
|---------|------------|---------|
| `humanDepthTexture` | 256x192 | Distance per pixel |
| `humanStencilTexture` | 256x192 | Binary human mask |
| `environmentDepthTexture` | LiDAR native | Full environment depth |
| `environmentDepthConfidenceTexture` | LiDAR native | Confidence per pixel |

### Occlusion Modes
```csharp
public enum OcclusionPreferenceMode
{
    NoOcclusion,                    // Disable occlusion
    PreferEnvironmentOcclusion,     // Environment depth only
    PreferHumanOcclusion,           // Human stencil only
    PreferEnvironmentAndHumanOcclusion // Both blended
}
```

### Implementation (ARDepthSource.cs)
```csharp
// Singleton compute pipeline - ONE dispatch for ALL VFX
private void UpdateTextures()
{
    _depthTexture = TryGetTexture(() => _occlusionManager.environmentDepthTexture);
    _stencilTexture = TryGetTexture(() => _occlusionManager.humanStencilTexture);

    // GPU compute: depth → world positions
    DispatchDepthToWorld();
}

// Critical: TryGetTexture pattern prevents crashes
Texture TryGetTexture(Func<Texture> getter)
{
    try { return getter?.Invoke(); }
    catch { return null; }
}
```

### VFX Properties Exposed
| Property | Type | Description |
|----------|------|-------------|
| `DepthMap` | Texture2D | Raw AR depth |
| `StencilMap` | Texture2D | Human mask (0=bg, 1=human) |
| `PositionMap` | Texture2D | GPU-computed world positions |
| `VelocityMap` | Texture2D | Camera velocity |
| `ColorMap` | Texture2D | Camera RGB |
| `RayParams` | Vector4 | (0, 0, tan(fov/2)*aspect, tan(fov/2)) |
| `InverseView` | Matrix4x4 | Inverse view matrix |
| `DepthRange` | Vector2 | Near/far (default 0.1-10m) |

---

## 3. Face Tracking (ARFaceManager)

**Status**: iOS ARKit 3+ (TrueDepth camera) | **MetavidoVFX**: Research only

### Capabilities
- **468-point face mesh** - Full topology
- **52 blend shapes** - Facial expressions
- **Face anchor** - Position/rotation tracking
- **Eye tracking** - Left/right eye gaze
- **Head pose** - Roll, pitch, yaw

### Blend Shape Reference (Key)
| Blend Shape | Range | Blend Shape | Range |
|-------------|-------|-------------|-------|
| eyeBlinkLeft | 0-1 | mouthSmileLeft | 0-1 |
| eyeBlinkRight | 0-1 | mouthSmileRight | 0-1 |
| jawOpen | 0-1 | browDownLeft | 0-1 |
| mouthFunnel | 0-1 | browDownRight | 0-1 |

### Data Access
```csharp
ARFace face = faceManager.trackables.First();
float[] blendShapes = face.blendShapes;
Mesh faceMesh = face.mesh; // 468 vertices
Vector3 leftEyePos = face.leftEye.position;
```

---

## 4. AR Mesh Detection (ARMeshManager)

**Status**: iOS ARKit 14+ (LiDAR) | **MetavidoVFX**: Integrated (EchoVision)

### Capabilities
- **Real-time mesh generation** - Dynamic environmental mesh
- **Mesh classification** - Surface type labels
- **Mesh updates** - Added, updated, removed events
- **Full mesh data** - Vertices, normals, triangles

### Mesh Classifications (ARKit 6+)
| Classification | Description |
|----------------|-------------|
| Wall | Vertical surfaces |
| Floor | Horizontal ground |
| Ceiling | Horizontal above |
| Table | Flat surfaces |
| Seat | Chairs, sofas |
| Door | Door frames |
| Window | Window frames |

### Implementation (MeshVFX.cs)
```csharp
// Sorts meshes by distance, fills GraphicsBuffer
void UpdateMeshBuffer()
{
    var meshes = meshManager.meshes.OrderBy(m =>
        Vector3.Distance(m.transform.position, head.position));

    foreach (MeshFilter mesh in meshes.Take(maxMeshes))
    {
        Vector3[] vertices = mesh.sharedMesh.vertices;
        // Fill buffer...
    }

    vfx.SetGraphicsBuffer("MeshPointCache", bufferVertex);
}
```

---

## 5. Hand Tracking (XR Hands - Separate Package)

**Status**: All platforms via com.unity.xr.hands 1.7.1 | **MetavidoVFX**: Integrated

### Capabilities
- **26-joint hand skeleton** - Detailed finger articulation
- **Both hands** - Simultaneous dual-hand tracking
- **Hand pose** - Real-time position/orientation
- **Gesture detection** - Pinch, grab, etc.

### Joint Enum (XRHandJointID)
| Joint | Index | Joint | Index |
|-------|-------|-------|-------|
| Wrist | 0 | ThumbTip | 4 |
| Palm | 1 | IndexTip | 9 |
| ThumbProximal | 2 | MiddleTip | 14 |
| ThumbIntermediate | 3 | RingTip | 19 |
| ... | ... | LittleTip | 24 |

### Implementation (HandVFXController.cs)
```csharp
// Velocity-driven VFX with pinch detection
void Update()
{
    if (handSubsystem.TryGetJoint(XRHandJointID.Wrist, out joint))
    {
        Vector3 velocity = (joint.position - lastPosition) / Time.deltaTime;
        vfx.SetVector3("HandVelocity", velocity);
        vfx.SetFloat("HandSpeed", velocity.magnitude);
    }

    // Pinch detection
    float pinchDistance = Vector3.Distance(
        GetJoint(XRHandJointID.ThumbTip),
        GetJoint(XRHandJointID.IndexTip));
    vfx.SetBool("IsPinching", pinchDistance < pinchThreshold);
}
```

---

## 6. Image Tracking (ARTrackedImageManager)

**Status**: iOS + Android | **MetavidoVFX**: Available

### Capabilities
- **2D image detection** - Detect reference images
- **Concurrent tracking** - Multiple images
- **Image size tracking** - Detected dimensions
- **Tracking state** - Quality indicators

### Data Access
```csharp
void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
{
    foreach (var image in args.added.Concat(args.updated))
    {
        Vector2 size = image.size;
        Pose pose = image.transform.GetPose();
        TrackingState state = image.trackingState;
        XRReferenceImage refImage = image.referenceImage;
    }
}
```

---

## 7. Object Tracking (ARTrackedObjectManager)

**Status**: iOS ARKit 4+ / Android ARCore 4+ | **MetavidoVFX**: Available

### Capabilities
- **3D object detection** - Pre-scanned reference objects
- **Spatial tracking** - Position/rotation
- **Reference library** - `XRReferenceObjectLibrary`

### Setup
```csharp
var manager = gameObject.AddComponent<ARTrackedObjectManager>();
manager.referenceLibrary = referenceLibrary; // Pre-scanned objects
manager.trackedObjectPrefab = objectPrefab;
```

---

## 8. Plane Detection (ARPlaneManager)

**Status**: iOS + Android | **MetavidoVFX**: Via raycast

### Capabilities
- **Horizontal planes** - Floor, tables
- **Vertical planes** - Walls
- **Plane classification** - Surface types
- **Plane boundaries** - Polygon edges

### Plane Classifications
| Classification | Description |
|----------------|-------------|
| None | Unknown |
| Wall | Vertical |
| Floor | Horizontal ground |
| Ceiling | Horizontal above |
| Table | Flat surface |
| Seat | Chairs |
| Door | Door frame |
| Window | Window frame |

---

## 9. Raycasting (ARRaycastManager)

**Status**: iOS + Android | **MetavidoVFX**: PRIMARY for placement

### Capabilities
- **Hit detection** - Planes, meshes, images
- **Multiple hits** - All along ray
- **Hit filtering** - TrackableType flags

### Implementation (HologramAnchor.cs)
```csharp
List<ARRaycastHit> hits = new List<ARRaycastHit>();
if (raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon))
{
    Pose hitPose = hits[0].pose;
    float distance = hits[0].distance;
    // Place hologram at hitPose
}
```

---

## 10. Point Cloud (ARPointCloudManager)

**Status**: iOS + Android | **MetavidoVFX**: Research only

### Capabilities
- **Sparse feature points** - 10K-50K points
- **Point confidence** - Per-point reliability
- **Real-time updates** - Dynamic point tracking

### Note
Less dense than LiDAR mesh. Use ARMeshManager for dense point clouds.

---

## 11. Environment Probes (AREnvironmentProbeManager)

**Status**: iOS ARKit 14+ | **MetavidoVFX**: Not used

### Capabilities
- **Cubemap probes** - Environment reflections
- **HDR textures** - Lighting info
- **Automatic updates** - Real-time generation

---

## 12. Light Estimation (ARLightEstimationManager)

**Status**: iOS + Android | **MetavidoVFX**: Minimal

### Capabilities
- **Brightness** - Average scene brightness
- **Color temperature** - Lighting warmth
- **Main light direction** - Primary light vector
- **Ambient spherical harmonics** - Full environment

---

## 13. Camera (ARCameraManager)

**Status**: iOS + Android | **MetavidoVFX**: Integrated

### Capabilities
- **Camera texture** - RGB/YCbCr
- **Camera intrinsics** - Focal length, principal point
- **Camera pose** - Position/orientation
- **Frame time** - Synchronization

### Data Access
```csharp
Matrix4x4 projection = cameraManager.GetProjectionMatrix();
Matrix4x4 view = camera.worldToCameraMatrix;
Texture cameraTexture = cameraManager.GetLatestImageTexture();
```

---

## 14. Spatial Anchors (ARAnchorManager)

**Status**: iOS + Android | **MetavidoVFX**: Integrated

### Capabilities
- **Local anchors** - Persist across frames
- **Cloud anchors** - Share between devices
- **Anchor lifecycle** - Create, update, remove
- **Serialization** - Save/restore state

---

## 15. Scene Understanding (iOS 15+ Only)

**Status**: iOS ARKit 6+ | **MetavidoVFX**: Research

### Capabilities
- **Room type** - Living room, bedroom, etc.
- **Furniture** - Table, chair, sofa, bed
- **Appliances** - TV, refrigerator
- **Structural** - Wall, floor, ceiling, door, window

### NOT Native AR Foundation
Requires ARKit API access via native plugin or ARKitObjectDetection.

---

## 16. Semantic Segmentation

**Status**: NOT NATIVE | **MetavidoVFX**: Via BodyPix/YOLO

### Native Options
- **AROcclusionManager** - Binary human stencil only
- **ARMeshManager** - Surface classification (wall/floor/etc.)

### Third-Party Required For
- **24-part body** - BodyPixSentis
- **Object classes** - YOLO11
- **Scene semantics** - Custom ML models

---

## Summary Table

| Capability | Manager | iOS | Android | MetavidoVFX |
|------------|---------|-----|---------|-------------|
| Body 91 joints | ARHumanBodyManager | ✅ | ❌ | ✅ |
| Human depth/stencil | AROcclusionManager | ✅ | ✅ | ✅ PRIMARY |
| Environment depth | AROcclusionManager | ✅ LiDAR | ❌ | ✅ |
| Face 468pt | ARFaceManager | ✅ | ❌ | Research |
| AR mesh | ARMeshManager | ✅ LiDAR | ❌ | ✅ EchoVision |
| Hands 26j | XRHands | ✅ | ✅ | ✅ |
| Image tracking | ARTrackedImageManager | ✅ | ✅ | Available |
| Object tracking | ARTrackedObjectManager | ✅ | ✅ | Available |
| Planes | ARPlaneManager | ✅ | ✅ | Via raycast |
| Raycasting | ARRaycastManager | ✅ | ✅ | ✅ |
| Point cloud | ARPointCloudManager | ✅ | ✅ | Research |
| Env probes | AREnvironmentProbeManager | ✅ | ❌ | Not used |
| Light estimation | ARLightEstimationManager | ✅ | ✅ | Minimal |
| Camera | ARCameraManager | ✅ | ✅ | ✅ |
| Anchors | ARAnchorManager | ✅ | ✅ | ✅ |
| Scene semantics | Native plugin | ✅ iOS 15+ | ❌ | Research |
| Body 24-part | BodyPixSentis | All | All | ✅ |
| Objects | YOLO11 | All | All | Experimental |

---

## Key Files in MetavidoVFX

| File | LOC | Purpose |
|------|-----|---------|
| `ARDepthSource.cs` | 256 | Primary depth compute pipeline |
| `VFXARBinder.cs` | 160 | Per-VFX lightweight binding |
| `HumanParticleVFX.cs` | 200 | Body-reactive VFX |
| `MeshVFX.cs` | 300 | AR mesh → VFX buffer |
| `HandVFXController.cs` | 350 | Hand tracking + pinch |
| `HologramAnchor.cs` | 400 | AR placement |
| `BodyPartSegmenter.cs` | 400 | BodyPix 24-part |
| `NNCamKeypointBinder.cs` | 200 | Pose keypoints → VFX |

---

## References

- [AR Foundation Manual](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.2/manual/index.html)
- [XR Hands Manual](https://docs.unity3d.com/Packages/com.unity.xr.hands@1.7/manual/index.html)
- [ARKit Feature Availability](https://developer.apple.com/documentation/arkit/arkit_in_ios)
- [MetavidoVFX CLAUDE.md](../MetavidoVFX-main/CLAUDE.md)

---

*Last Updated: 2026-01-20*
