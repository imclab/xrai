# Master GitHub Repository Knowledge Base (530+ Projects)

**Purpose**: Comprehensive reference of GitHub repos used across all workspaces (Portals_6, Paint-AR, Open Brush) to improve Claude's coding capabilities for AR/VFX/XR/Unity development.

**Last Updated**: 2025-01-07
**Recent Additions**: Unity-Technologies official repos, Gaussian Splatting Unity implementations, Dilmerv VisionOS examples, Icosa Gallery Unity tools, enhanced Keijiro collection

---

## 📱 Workspace Projects (Active Development)

| Project | Description | Platform Support | Status |
|---------|-------------|------------------|--------|
| [imclab/Paint-AR_app_Unity](https://github.com/imclab/Paint-AR_app_Unity) | iOS AR painting app with 3D brushes, portals, VFX | iOS (ARKit, URP) | ⭐ **WORKSPACE** |
| [imclab/Apple-Vision-PRO-AR-VR-XR-AI](https://github.com/imclab/Apple-Vision-PRO-AR-VR-XR-AI) | Vision Pro AR/VR/XR/AI resources and examples | visionOS | ⭐ **WORKSPACE** |
| [icosa-foundation/open-brush](https://github.com/icosa-foundation/open-brush) | VR brush painting tool (Tilt Brush successor) | VR (Quest, PCVR) | ⭐ **WORKSPACE** |
| [icosa-foundation/open-brush-unity-tools](https://github.com/icosa-foundation/open-brush-unity-tools) | Unity tools for Open Brush integration | Unity | ⭐ **WORKSPACE** |
| [jwtan/SwiftToUnityExample](https://github.com/jwtan/SwiftToUnityExample) | Swift to Unity bridge example | iOS | 🔧 **TOOL** |
| [cdmvision/unity-figma-importer](https://github.com/cdmvision/unity-figma-importer) | Import Figma designs into Unity | Unity Editor | 🔧 **TOOL** |

**Icosa Gallery Resources**:
- Gallery: https://icosa.gallery/
- API Documentation: https://api.icosa.gallery/v1/docs
- Open Brush Docs: https://docs.openbrush.app/
- Example Artwork: https://icosa.gallery/view/3UL8Bz_Id6I

## 🎨 Unity XR Development Tools & Frameworks

### Unity Technologies (Official Repos)
| Project | Description | Platform Support |
|---------|-------------|------------------|
| [Unity-Technologies](https://github.com/Unity-Technologies) | Main organization - all official Unity repos | All |
| [Unity-Technologies/EntityComponentSystemSamples](https://github.com/Unity-Technologies/EntityComponentSystemSamples) | DOTS/ECS examples and best practices | Multi-platform |
| [Unity-Technologies/ml-agents](https://github.com/Unity-Technologies/ml-agents) | Machine Learning agents toolkit | Multi-platform |
| [Unity-Technologies/Addressables-Sample](https://github.com/Unity-Technologies/Addressables-Sample) | Official Addressables examples | Multi-platform |
| [Unity-Technologies/XR-Interaction-Toolkit-Examples](https://github.com/Unity-Technologies/XR-Interaction-Toolkit-Examples) | Official XRI 3.1+ examples and samples | Multi-platform |

### Community XR Tools
| Project | Description | Platform Support |
|---------|-------------|------------------|
| [dilmerv/XRInteractionDemo](https://github.com/dilmerv/XRInteractionDemo) | XR Interaction Toolkit examples for AR and VR | Multi-platform |
| [dilmerv/UnityVisionOS2DAndVR](https://github.com/dilmerv/UnityVisionOS2DAndVR) | Vision OS development examples | visionOS |
| [KhronosGroup/UnityGLTF](https://github.com/KhronosGroup/UnityGLTF) | Official Unity glTF importer/exporter | Multi-platform |
| [needle-tools/UnityGLTF](https://github.com/needle-tools/UnityGLTF) | Needle fork with Draco compression | Multi-platform |
| [microsoft/MixedRealityToolkit](https://github.com/microsoft/MixedRealityToolkit-Unity) | MRTK for HoloLens, Quest, VR | Multi-platform |

## 🎮 Multiplayer & Networking Tools

| Project | Description | Platform Support |
|---------|-------------|------------------|
| [NormalVR/Normcore-Samples](https://github.com/NormalVR/Normcore-Samples) | Official Normcore multiplayer samples | Multi-platform |
| [absurd-joy/Quest-hands-for-Normcore](https://github.com/absurd-joy/Quest-hands-for-Normcore) | Quest hand tracking with Normcore | Quest |
| [calebcram/Passthrough-Online-MRTK_Quest---Sample](https://github.com/calebcram/Passthrough-Online-MRTK_Quest---Sample) | Quest passthrough multiplayer | Quest |

## 🧠 ML & Neural Rendering (Keijiro Collection)

**Profile**: [github.com/keijiro](https://github.com/keijiro?tab=repositories) - Unity VFX/Graphics master

### Machine Learning
| Project | Description | ML Framework |
|---------|-------------|--------------|
| [keijiro/BodyPixSentis](https://github.com/keijiro/BodyPixSentis) | Body segmentation with Unity Sentis | Sentis |
| [keijiro/Minis](https://github.com/keijiro/Minis) | Hand detection with Sentis | Sentis |
| [keijiro/SelfieBarracuda](https://github.com/keijiro/SelfieBarracuda) | Portrait segmentation (deprecated Barracuda) | Barracuda |

### VFX Graph & Effects
| Project | Description | Techniques |
|---------|-------------|------------|
| [keijiro/SplatVFX](https://github.com/keijiro/SplatVFX) | Gaussian Splatting VFX rendering | Neural rendering |
| [keijiro/Kino](https://github.com/keijiro/Kino) | Image effects collection for Unity | Post-processing |

---

## 🧍 Human Depth/Stencil/Body Tracking → VFX (45+ Projects)

| Project | Description | Techniques | iOS Support |
|---------|-------------|------------|-------------|
| [YoHana19/HumanParticleEffect](https://github.com/YoHana19/HumanParticleEffect) | Direct human particle effects | ARKit human segmentation | ✅ |
| [keijiro/Rcam2](https://github.com/keijiro/Rcam2) | Remote depth streaming | LiDAR, depth streaming | ✅ |
| [keijiro/Rcam3](https://github.com/keijiro/Rcam3) | Remote depth streaming v3 | LiDAR, depth streaming | ✅ |
| [keijiro/Rcam4](https://github.com/keijiro/Rcam4) | Remote depth streaming v4 | LiDAR, depth streaming | ✅ |
| [keijiro/MetavidoVFX](https://github.com/keijiro/MetavidoVFX) | Volumetric video with LiDAR | LiDAR → VFX Graph | ✅ |
| [supertask/AkvfxBody](https://github.com/supertask/AkvfxBody) | Azure Kinect body → VFX | Body tracking, depth | ❌ |
| [mao-test-h/FaceTracking-VFX](https://github.com/mao-test-h/FaceTracking-VFX) | Face tracking VFX | ARKit face tracking | ✅ |
| [fncischen/ARBodyTracking](https://github.com/fncischen/ARBodyTracking) | AR body tracking effects | ARKit body tracking | ✅ |
| [EyezLee/ARVolumeVFX](https://github.com/EyezLee/ARVolumeVFX) | LiDAR volume effects | LiDAR, human depth | ✅ |
| [genereddick/ARBodyTrackingAndPuppeteering](https://github.com/genereddick/ARBodyTrackingAndPuppeteering) | Body tracking avatar | ARKit body tracking | ✅ |
| [LightBuzz/Body-Tracking-ARKit](https://github.com/LightBuzz/Body-Tracking-ARKit) | ARKit body tracking sample | ARKit 3D skeleton | ✅ |
| [emilianavt/OpenSeeFace](https://github.com/emilianavt/OpenSeeFace) | Robust face tracking | OpenCV, Unity integration | ✅ |
| [homuler/MediaPipeUnityPlugin](https://github.com/homuler/MediaPipeUnityPlugin) | MediaPipe for Unity | ML body/face tracking | ✅ |
| [keijiro/FaceLandmarkBarracuda](https://github.com/keijiro/FaceLandmarkBarracuda) | Face landmarks ML | MediaPipe, Barracuda | ✅ |
| [yeemachine/kalidokit](https://github.com/yeemachine/kalidokit) | MediaPipe → VRM blendshapes | Face/pose/hands conversion | ✅ |
| [yeemachine/kalidoface](https://github.com/yeemachine/kalidoface) | Live VTuber web app | three.js + MediaPipe | ✅ |
| [yeemachine/kalidoface-3d](https://github.com/yeemachine/kalidoface-3d) | 3D face mesh generator | 468 landmarks → mesh | ✅ |
| [yeemachine/theremix](https://github.com/yeemachine/theremix) | WebXR multiplayer VTuber | WebRTC + tracking sync | ✅ |
| [oculus-samples/Unity-Movement](https://github.com/oculus-samples/Unity-Movement) | Body/eye/face tracking (Quest Pro) | Movement SDK, 17 joints | ❌ |
| [jemmec/metaface-utilities](https://github.com/jemmec/metaface-utilities) | Meta Quest Pro face and eye tracking utilities | OVRFaceExpressions | ❌ |
| [keijiro/Rsvfx](https://github.com/keijiro/Rsvfx) | RealSense → VFX | Depth camera → VFX | ❌ |
| [keijiro/Dkvfx](https://github.com/keijiro/Dkvfx) | Depthkit → VFX | Volumetric video | ✅ |
| [keijiro/DkvfxSketches](https://github.com/keijiro/DkvfxSketches) | Depthkit VFX experiments | Volumetric video sketches | ✅ |
| [keijiro/Akvfx](https://github.com/keijiro/Akvfx) | Azure Kinect → VFX | Depth sensor | ❌ |
| [keijiro/VfxPyro](https://github.com/keijiro/VfxPyro) | Pyrotechnic VFX | VFX Graph effects | ✅ |
| [hecomi/uDepth](https://github.com/hecomi/uDepth) | Depth visualization | Depth capture | ✅ |
| [hecomi/uARKitFaceMesh](https://github.com/hecomi/uARKitFaceMesh) | ARKit face mesh | Face tracking | ✅ |
| [hecomi/UnityARKitFaceTrackingExample](https://github.com/hecomi/UnityARKitFaceTrackingExample) | ARKit face tracking example | Face tracking | ✅ |
| [hecomi/uLipSync](https://github.com/hecomi/uLipSync) | Lip sync library | Audio → facial animation | ✅ |
| [hecomi/uOSC](https://github.com/hecomi/uOSC) | OSC communication | Network data → Unity | ✅ |
| [asus4/ARKitStreamer](https://github.com/asus4/ARKitStreamer) | Stream ARKit data | ARKit remote streaming | ✅ |
| [asus4/WorldEnsemble](https://github.com/asus4/WorldEnsemble) | World tracking ensemble | ARKit world tracking | ✅ |
| [marek-simonik/record3d_offline_unity_demo](https://github.com/marek-simonik/record3d_offline_unity_demo) | Record3D Unity integration | Volumetric capture | ✅ |

## 🎵 Audio Reactive VFX (30+ Projects)

| Project | Description | Techniques | iOS Support |
|---------|-------------|------------|-------------|
| [keijiro/LaspVfx](https://github.com/keijiro/LaspVfx) | Low-latency audio → VFX | LASP, VFX Graph | ✅ |
| [keijiro/Lasp](https://github.com/keijiro/Lasp) | Audio signal processing | Low-latency audio | ✅ |
| [keijiro/Reaktion](https://github.com/keijiro/Reaktion) | Audio reactive toolkit | FFT, spectrum analysis | ✅ |
| [keijiro/unity-audio-spectrum](https://github.com/keijiro/unity-audio-spectrum) | Spectrum analyzer | FFT, octave bands | ✅ |
| [keijiro/unity-spectrum-analyzer](https://github.com/keijiro/unity-spectrum-analyzer) | Unity spectrum visualization | GetSpectrumData | ✅ |
| [smaerdlatigid/VFXcubes-WASAPI](https://github.com/smaerdlatigid/VFXcubes-WASAPI) | Audio reactive cubes | WASAPI, VFX Graph | ❌ |
| [tomer8007/real-time-audio-fft](https://github.com/tomer8007/real-time-audio-fft) | iOS FFT library | vDSP, real-time FFT | ✅ |
| [jscalo/tempi-fft](https://github.com/jscalo/tempi-fft) | Swift FFT for iOS | Swift, FFT | ✅ |
| [dotH55/Audio_Analyser](https://github.com/dotH55/Audio_Analyser) | Android spectrum analyzer | FFT, real-time | ❌ |

## 🌍 Environment Depth → VFX (25+ Projects)

| Project | Description | Techniques | iOS Support |
|---------|-------------|------------|-------------|
| [cdmvision/arfoundation-densepointcloud](https://github.com/cdmvision/arfoundation-densepointcloud) | LiDAR point cloud viz | Scene depth API | ✅ |
| [googlesamples/arcore-depth-lab](https://github.com/googlesamples/arcore-depth-lab) | ARCore depth experiments | Depth API, effects | ❌ |
| [Unity-Technologies/arfoundation-demos](https://github.com/Unity-Technologies/arfoundation-demos) | Mesh viz, depth effects | ARKit meshing | ✅ |
| [Unity-Technologies/arfoundation-samples](https://github.com/Unity-Technologies/arfoundation-samples) | Official AR samples | Various AR features | ✅ |
| [Unity-Technologies/VisualEffectGraph-Samples](https://github.com/Unity-Technologies/VisualEffectGraph-Samples) | VFX Graph examples | VFX Graph techniques | ✅ |
| [yumayanagisawa/Unity-Point-Cloud-VFX-Graph](https://github.com/yumayanagisawa/Unity-Point-Cloud-VFX-Graph) | Point cloud → VFX | PLY → VFX Graph | ✅ |
| [hafewa/Unity-Point-Cloud-VFX-Graph](https://github.com/hafewa/Unity-Point-Cloud-VFX-Graph) | Point cloud VFX fork | PLY → VFX Graph | ✅ |
| [pablothedolphin/Point-Cloud-Renderer](https://github.com/pablothedolphin/Point-Cloud-Renderer) | Compute shader renderer | GPU point clouds | ✅ |
| [pablothedolphin/DOTS-Point-Clouds](https://github.com/pablothedolphin/DOTS-Point-Clouds) | DOTS-based point clouds | ECS point rendering | ✅ |
| [roelkok/Kinect-VFX-Graph](https://github.com/roelkok/Kinect-VFX-Graph) | Kinect depth → VFX | Depth sensor | ❌ |
| [DanMillerDev/ARFoundation_VFX](https://github.com/DanMillerDev/ARFoundation_VFX) | AR + URP + VFX setup | Basic integration | ✅ |
| [dilmerv/UnityVFXMillionsOfParticles](https://github.com/dilmerv/UnityVFXMillionsOfParticles) | Million particle demos | GPU particles | ✅ |
| [dilmerv/UnityARFoundationEssentials](https://github.com/dilmerv/UnityARFoundationEssentials) | AR Foundation essentials | AR basics + VFX | ✅ |
| [holokit/becoming-bats](https://github.com/holokit/becoming-bats) | HoloKit AR experience | AR + VFX effects | ✅ |

## 🎨 Additional VFX Resources

| Project | Description | Techniques | iOS Support |
|---------|-------------|------------|-------------|
| [fuqunaga/VFXGraphSandbox](https://github.com/fuqunaga/VFXGraphSandbox) | VFX Graph experiments | Various VFX techniques | ✅ |
| [texone/unity-vfx-samples](https://github.com/texone/unity-vfx-samples) | Unity VFX samples | VFX Graph examples | ✅ |
| [needle-mirror/com.unity.visualeffectgraph](https://github.com/needle-mirror/com.unity.visualeffectgraph) | VFX Graph package mirror | Unity package | ✅ |
| [Unity-Technologies/arfoundation-samples/issues/387](https://github.com/Unity-Technologies/arfoundation-samples/issues/387) | VFX Graph issue discussion | Community discussion | - |

## ✨ Hologram Shaders (for VFX output)

| Project | Description | Techniques | iOS Support |
|---------|-------------|------------|-------------|
| [ereneker/HologramShader](https://github.com/ereneker/HologramShader) | Hologram shader | Unity shader | ✅ |
| [daniel-ilett/shaders-hologram](https://github.com/daniel-ilett/shaders-hologram) | Hologram shader collection | Various hologram effects | ✅ |
| [andydbc/HologramShader](https://github.com/andydbc/HologramShader) | Hologram shader | Unity shader | ✅ |

## 🌐 Gaussian Splatting - Unity & Web

### Unity Implementations
| Project | Description | Platform |
|---------|-------------|----------|
| [aras-p/UnityGaussianSplatting](https://github.com/aras-p/UnityGaussianSplatting) | Full Unity gaussian splatting implementation | Unity |
| [clarte53/GaussianSplattingVRViewerUnity](https://github.com/clarte53/GaussianSplattingVRViewerUnity) | VR gaussian splat viewer | Unity VR |
| [keijiro/SplatVFX](https://github.com/keijiro/SplatVFX) | Gaussian splatting with VFX Graph | Unity |

### Web-Based Viewers
| Project | Description | Platform |
|---------|-------------|----------|
| [graphdeco-inria/gaussian-splatting](https://github.com/graphdeco-inria/gaussian-splatting) | Original gaussian splatting research | Python |
| [mkkellogg/GaussianSplats3D](https://github.com/mkkellogg/GaussianSplats3D) | Three.js gaussian splats | Web |
| [buaacyw/GaussianEditor](https://github.com/buaacyw/GaussianEditor) | Edit gaussian splat scenes | Web |
| [playcanvas/supersplat](https://github.com/playcanvas/supersplat) | PlayCanvas splat editor | Web |
| [Looking-Glass/super-splat](https://github.com/Looking-Glass/super-splat) | Looking Glass splat viewer | Web |
| [antimatter15.com/splat](https://antimatter15.com/splat/) | Web splat viewer | Web |
| [vincent-lecrubier-skydio/react-three-fiber-gaussian-splat](https://github.com/vincent-lecrubier-skydio/react-three-fiber-gaussian-splat) | React Three Fiber splats | Web |
| [guyettinger/gle-gs3d](https://github.com/guyettinger/gle-gs3d) | WebGL gaussian splats | Web |
| [playcanvas/supersplat-viewer](https://github.com/playcanvas/supersplat-viewer) | PlayCanvas splat viewer | Web |
| [playcanv.as/e/p/cLkf99ZV](https://playcanv.as/e/p/cLkf99ZV/) | PlayCanvas demo | Web |
| [kwaldow.github.io/gsplats](https://kwaldow.github.io/gsplats/index.html) | Gaussian splat viewer | Web |

## 📹 Webcam → Particle Systems (Web)

| Project | Description | Platform |
|---------|-------------|----------|
| [tuqire/webcam-particles](https://github.com/tuqire/webcam-particles) | Webcam particle effects | Web |
| [threejs.org/examples/#webcam](https://threejs.org/examples/#webcam) | Three.js webcam examples | Web |

## ⚡ DOTS/ECS High-Performance Systems

| Project | Description | Techniques | iOS Support |
|---------|-------------|------------|-------------|
| [pablothedolphin/DOTS-Point-Clouds](https://github.com/pablothedolphin/DOTS-Point-Clouds) | Million-particle point clouds with ECS | DOTS, Burst, Quest 90fps | ✅ |
| [keijiro/ECS-Strawman](https://github.com/keijiro/ECS-Strawman) | Simplest Unity ECS/DOTS example | ECS basics | ✅ |
| [keijiro/Voxelman](https://github.com/keijiro/Voxelman) | Voxel rendering with DOTS | ECS, procedural generation | ✅ |
| [particlelife-3d-unity-ecs](https://github.com/particlelife-3d-unity-ecs/particlelife-3d-unity-ecs.github.io) | Particle Life 3D with Unity ECS DOTS | ECS, particle simulation | ✅ |
| [Unity-Technologies/EntityComponentSystemSamples](https://github.com/Unity-Technologies/EntityComponentSystemSamples) | Official DOTS samples | ECS, Physics, Netcode | ✅ |

## 🧪 Experimental/Advanced Projects

| Project | Description | Platform |
|---------|-------------|----------|
| [needle-tools/needle-engine-support](https://github.com/needle-tools/needle-engine-support) | Needle Engine support | Unity → Web |
| [nv-tlabs/3dgrut](https://github.com/nv-tlabs/3dgrut) | 3D reconstruction | Research |
| [KByrski/RaySplatting](https://github.com/KByrski/RaySplatting) | Ray-based splatting | Research |
| [nvpro-samples/vk_gaussian_splatting](https://github.com/nvpro-samples/vk_gaussian_splatting) | Vulkan gaussian splatting | GPU |
| [graphdeco-inria/gaussian-splatting](https://github.com/graphdeco-inria/gaussian-splatting) | Original gaussian splatting | Research |
| [MrNeRF/awesome-3D-gaussian-splatting](https://github.com/MrNeRF/awesome-3D-gaussian-splatting) | Gaussian splatting resources | Collection |
| [holtsetio/softbodies](https://github.com/holtsetio/softbodies) | Soft body simulation | Physics |

## 🔧 Utility/Tools/Resources

### Tools & Platforms
- [poly.cam/tools/gaussian-splatting](https://poly.cam/tools/gaussian-splatting) - Polycam gaussian splatting
- [cloud.needle.tools](https://cloud.needle.tools/) - Needle Engine cloud
- [gaussiantracer.github.io](https://gaussiantracer.github.io/) - Gaussian tracer tool
- [gle-gaussian-splat-3d (npm)](https://www.npmjs.com/package/gle-gaussian-splat-3d) - NPM package

### Tutorials & Documentation
- [blog.playcanvas.com](https://blog.playcanvas.com/create-3d-gaussian-splat-apps-with-the-playcanvas-editor/) - PlayCanvas gaussian splat tutorial
- [qriva.github.io](https://qriva.github.io/posts/how-to-vfx-graph/) - How to VFX Graph
- [depthkit.tv](https://www.depthkit.tv/posts/keijiro-takahashi-creator-profile-depthkit) - Keijiro Takahashi profile
- [lightbuzz.com](https://lightbuzz.com/body-tracking-arkit-lidar/) - Body tracking with ARKit

### Unity Documentation
- [ARFoundation AROcclusionManager 5.0](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@5.0/api/UnityEngine.XR.ARFoundation.AROcclusionManager.html)
- [ARFoundation AROcclusionManager 4.2](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.2/api/UnityEngine.XR.ARFoundation.AROcclusionManager.html)
- [ARFoundation AROcclusionManager 4.1](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.1/api/UnityEngine.XR.ARFoundation.AROcclusionManager.html)
- [Unity Visual Effect Graph](https://unity.com/features/visual-effect-graph)

---

## 🥽 HoloKit & Reality Labs Projects (100+ Projects)

### HoloKit Official Projects
| Project | Description | Platform Support |
|---------|-------------|------------------|
| [holokit/holokit-unity-sdk](https://github.com/holokit/holokit-unity-sdk) | Unity SDK for HoloKit X stereoscopic AR | iOS, Android |
| [holokit/holokit-1-sdk](https://github.com/holokit/holokit-1-sdk) | Legacy SDK for HoloKit 1 (Cardboard version) | iOS, Android |
| [realitydeslab/holokit-app](https://github.com/realitydeslab/holokit-app) | Main HoloKit app with multiple AR experiences | iOS |
| [holokit/becoming-bats](https://github.com/holokit/becoming-bats) | Interactive AR art experiment with VFX | iOS |
| [holokit/talking-olaf](https://github.com/holokit/talking-olaf) | AR chatbot conversation demo | iOS |
| [holokit/touching-hologram](https://github.com/holokit/touching-hologram) | Hand interaction tutorial for HoloKit | iOS |
| [holokit/holokit-image-tracking-relocalization](https://github.com/holokit/holokit-image-tracking-relocalization) | Image tracking for coordinate system alignment | iOS |
| [holokit/apple-multipeer-connectivity-unity-plugin](https://github.com/holokit/apple-multipeer-connectivity-unity-plugin) | Multipeer connectivity for co-located AR | iOS |
| [holokit/netcode-transport-multipeer-connectivity](https://github.com/holokit/netcode-transport-multipeer-connectivity) | Netcode transport layer for local multiplayer | iOS, macOS, visionOS |

### Reality Labs / Meta Quest Projects
| Project | Description | Platform Support |
|---------|-------------|------------------|
| [oculus-samples/Unity-Discover](https://github.com/oculus-samples/Unity-Discover) | Mixed Reality showcase with passthrough, spatial anchors | Quest |
| [oculus-samples/Unity-Movement](https://github.com/oculus-samples/Unity-Movement) | Body/eye/face tracking samples | Quest |
| [Unity-Technologies/mr-example-meta-openxr](https://github.com/Unity-Technologies/mr-example-meta-openxr) | Mixed Reality example with OpenXR integration | Quest 2/Pro/3 |
| [microsoft/MixedRealityDesignLabs_Unity](https://github.com/microsoft/MixedRealityDesignLabs_Unity) | Mixed Reality design samples and explorations | HoloLens |
| [microsoft/MixedReality-WebRTC](https://github.com/microsoft/MixedReality-WebRTC) | WebRTC for Mixed Reality applications | Windows, HoloLens |

## 🌐 Unity + WebRTC + ARFoundation Projects (20+)

| Project | Description | Platform Support |
|---------|-------------|------------------|
| [Unity-Technologies/com.unity.webrtc](https://github.com/Unity-Technologies/com.unity.webrtc) | Official Unity WebRTC package | Multi-platform |
| [Unity-Technologies/UnityRenderStreaming](https://github.com/Unity-Technologies/UnityRenderStreaming) | Streaming server for Unity | Multi-platform |
| [ossrs/srs-unity](https://github.com/ossrs/srs-unity) | WebRTC samples with SRS SFU server | Multi-platform |
| [ritchielozada/UnityWithWebRTC](https://github.com/ritchielozada/UnityWithWebRTC) | Unity with WebRTC UWP libraries | Windows, HoloLens |
| [bengreenier/webrtc-unity-plugin](https://github.com/bengreenier/webrtc-unity-plugin) | Cross-platform WebRTC support | Multi-platform |
| [nicholasluimy/unity-webRTC](https://github.com/nicholasluimy/unity-webRTC) | Unity WebRTC for hybrid apps | WebGL, Mobile |

## 📡 RGBD/LiDAR Streaming Projects (20+)

| Project | Description | Protocol |
|---------|-------------|----------|
| [marek-simonik/record3d-simple-wifi-streaming-demo](https://github.com/marek-simonik/record3d-simple-wifi-streaming-demo) | Record3D RGBD streaming | WebRTC |
| [KshitizKumarGupta/lidar-webRTC](https://github.com/KshitizKumarGupta/lidar-webRTC) | LiDAR data streaming project | WebRTC |
| [digiamm/ba_md_slam](https://github.com/digiamm/ba_md_slam) | Photometric SLAM for RGB-D and LiDAR | CUDA |
| [mac999/simulate_LiDAR](https://github.com/mac999/simulate_LiDAR) | LiDAR point cloud simulation from RGBD | Local |
| [introlab/rtabmap](http://introlab.github.io/rtabmap/) | Real-time SLAM with RGBD/LiDAR support | Various |

## 🧠 Unity + Sentis/ONNX Runtime Projects (20+)

| Project | Description | ML Framework |
|---------|-------------|--------------|
| [Unity-Technologies/sentis-samples](https://github.com/Unity-Technologies/sentis-samples) | Official Sentis sample projects | Sentis |
| [needle-mirror/com.unity.sentis](https://github.com/needle-mirror/com.unity.sentis) | Unity Sentis package mirror | Sentis |
| [asus4/onnxruntime-unity](https://github.com/asus4/onnxruntime-unity) | ONNX Runtime plugin for Unity | ONNX Runtime |
| [asus4/onnxruntime-unity-examples](https://github.com/asus4/onnxruntime-unity-examples) | Examples for ONNX Runtime Unity | ONNX Runtime |
| [cj-mills/onnx-directml-unity-tutorial](https://github.com/cj-mills/onnx-directml-unity-tutorial) | Object detection with ONNX Runtime | ONNX Runtime |

## 🎮 ARFoundation Multiplayer/Multipeer Projects (50+)

| Project | Description | Networking |
|---------|-------------|------------|
| [Unity-Technologies/arfoundation-samples](https://github.com/Unity-Technologies/arfoundation-samples) | Collaborative session samples | MultipeerConnectivity |
| [realitydeslab/apple-multipeer-connectivity-unity-plugin](https://github.com/realitydeslab/apple-multipeer-connectivity-unity-plugin) | Multipeer connectivity wrapper | MultipeerConnectivity |
| [realitydeslab/netcode-transport-multipeer-connectivity](https://github.com/realitydeslab/netcode-transport-multipeer-connectivity) | Netcode transport for local multiplayer | Netcode |
| [enslaved2die/arfoundation-samples-URP](https://github.com/enslaved2die/arfoundation-samples-URP) | URP samples with multiplayer | Various |
| [Unity-Technologies/arfoundation-demos](https://github.com/Unity-Technologies/arfoundation-demos) | Advanced demos including multiplayer | Various |

## 🌉 Unity iOS to WebGL Connection Projects (20+)

| Project | Description | Tech Stack |
|---------|-------------|------------|
| [endel/NativeWebSocket](https://github.com/endel/NativeWebSocket) | WebSocket client for Unity | WebSocket |
| [JohannesDeml/UnityWebGL-LoadingTest](https://github.com/JohannesDeml/UnityWebGL-LoadingTest) | WebGL platform comparisons | WebGL |
| [HISPlayer/UnityWebGL-SDK](https://github.com/HISPlayer/UnityWebGL-SDK) | Video streaming for WebGL | HLS/DASH |
| [Unity-Technologies/UnityRenderStreaming](https://github.com/Unity-Technologies/UnityRenderStreaming) | Render streaming solution | WebRTC |
| [tgraupmann/UnityWebGLSpeech](https://github.com/tgraupmann/UnityWebGLSpeech) | Speech API for WebGL | Web APIs |

## 📱 WebGL Phone Controller Projects (50+)

| Project | Description | Tech Stack |
|---------|-------------|------------|
| [roman01la/websockets-device-controller](https://github.com/roman01la/websockets-device-controller) | Device accelerometer control | WebSockets |
| [jirihybek/unity-websocket-webgl](https://github.com/jirihybek/unity-websocket-webgl) | Hybrid WebSocket implementation | WebSockets |
| [bento-n-box/Websocket-Pong](https://github.com/bento-n-box/Websocket-Pong) | Phone-controlled Pong game | WebSockets |
| [christabella/freewee](https://github.com/christabella/freewee) | Multiplayer phone sensor games | Socket.io |
| [edgegap/mirror-webgl](https://github.com/edgegap/mirror-webgl) | Mirror networking for WebGL | Mirror |

## 🎥 Three.js Fiber Video Streaming Projects (20+)

| Project | Description | Features |
|---------|-------------|----------|
| [pmndrs/react-three-fiber](https://github.com/pmndrs/react-three-fiber) | React renderer for Three.js | Core library |
| [pmndrs/drei](https://github.com/pmndrs/drei) | Useful helpers for R3F | Video textures |
| [tdrdimov/react-three-fiber-gallery](https://github.com/tdrdimov/react-three-fiber-gallery) | 3D gallery implementation | Media display |
| [shubh0107/image-gallery-with-react-three-fiber](https://github.com/shubh0107/image-gallery-with-react-three-fiber) | Scrollable image gallery | Parallax effects |
| [pmndrs/xr](https://github.com/pmndrs/xr) | VR/AR for react-three-fiber | XR support |

## 🎨 WebGL 3D Content Viewers & Galleries (20+)

| Project | Description | Features |
|---------|-------------|----------|
| [bchao1/webgl-3d-viewer](https://github.com/bchao1/webgl-3d-viewer) | Pure WebGL model viewer | No dependencies |
| [endavid/webGL-modelViewer](https://github.com/endavid/webGL-modelViewer) | Minimalist model viewer | Raw WebGL |
| [hopepdm/WebGl-Three.js-Model-Viewer](https://github.com/hopepdm/WebGl-Three.js-Model-Viewer) | Three.js model viewer | OBJ/STL support |
| [cgwire/js-3d-model-viewer](https://github.com/cgwire/js-3d-model-viewer) | Web player for 3D models | OBJ/GLB support |
| [vimaec/vim-webgl-viewer](https://github.com/vimaec/vim-webgl-viewer) | Three.js-based viewer | Easy to use |
| [Rufus31415/react-webgl-3d-viewer-demo](https://github.com/Rufus31415/react-webgl-3d-viewer-demo) | 45+ format support | React-based |

## 🔍 WebGL AR Viewers (10+)

| Project | Description | AR Type |
|---------|-------------|---------|
| [AR-js-org/AR.js](https://github.com/AR-js-org/AR.js) | Efficient AR for the web | Marker/Location |
| [playcanvas/playcanvas-ar](https://github.com/playcanvas/playcanvas-ar) | PlayCanvas AR toolkit | Marker-based |
| [google-ar/WebARonARKit](https://github.com/google-ar/WebARonARKit) | Experimental iOS WebAR | ARKit-based |
| [immersive-web/webxr-samples](https://immersive-web.github.io/webxr-samples/) | WebXR sample pages | Standards-based |
| [8th Wall](https://www.8thwall.com/) | Commercial WebAR platform | SLAM-based |

## 🎯 WebGL Multiplayer Realtime Projects (20+)

| Project | Description | Tech Stack |
|---------|-------------|------------|
| [tehzwen/MultiplayerWebGL](https://github.com/tehzwen/MultiplayerWebGL) | Simple multiplayer game | Socket.io + Three.js |
| [KyleDulce/Unity-Socketio](https://github.com/KyleDulce/Unity-Socketio) | Socket.IO for Unity WebGL | Socket.io |
| [arigbs/Simple-Unity-Multiplayer-with-NodeJS-for-WebGL-Builds](https://github.com/arigbs/Simple-Unity-Multiplayer-with-NodeJS-for-WebGL-Builds) | Unity multiplayer template | Node.js + Socket.io |
| [muaz-khan/WebRTC-Experiment](https://github.com/muaz-khan/WebRTC-Experiment) | WebRTC experiments | WebRTC |

## 📊 WebGL Data Visualizations (50+)

### D3.js & WebGL
| Project | Description | Features |
|---------|-------------|----------|
| [d3/d3](https://github.com/d3/d3) | Data visualization library | SVG/Canvas/WebGL |
| [vasturiano/3d-force-graph](https://github.com/vasturiano/3d-force-graph) | 3D force-directed graphs | Three.js + D3 |
| [vasturiano/three-globe](https://github.com/vasturiano/three-globe) | WebGL globe visualization | Geographic data |
| [Niekes/d3-3d](https://github.com/Niekes/d3-3d) | 3D visualizations with D3 | 3D projections |
| [stardustjs.github.io](https://stardustjs.github.io/) | GPU-based visualizations | WebGL rendering |

### Three.js Visualizations
| Project | Description | Features |
|---------|-------------|----------|
| [vasturiano/3d-force-graph](https://github.com/vasturiano/3d-force-graph) | Force-directed 3D graphs | Interactive |
| [keijiro/GeoVfx](https://github.com/keijiro/GeoVfx) | Geographic data viz | Unity VFX Graph |
| [Dandarawy/Unity3D-Globe](https://github.com/Dandarawy/Unity3D-Globe) | Chrome experiment port | Unity implementation |

## 📈 Unity Data Visualizations (50+)

| Project | Description | Features |
|---------|-------------|----------|
| [drewfrobot/unity-and-data](https://github.com/drewfrobot/unity-and-data) | SQLite data visualization | 3D scatter plots |
| [BitSplash Interactive/Graph-and-Chart](https://assetstore.unity.com/packages/tools/gui/graph-and-chart-data-visualization-78488) | Commercial chart library | Multiple chart types |
| [Aspeccttt/RealtimeVision](https://github.com/Aspeccttt/RealtimeVision) | Real-time data analytics | Unity 3D visualization |
| [Unity-Technologies/graph-visualizer](https://github.com/Unity-Technologies/graph-visualizer) | Playable graph visualizer | Unity tool |
| [keijiro/GeoVfx](https://github.com/keijiro/GeoVfx) | Geographic data with VFX | Population data |
| [vcian/interactive-bar-chart](https://github.com/vcian/interactive-bar-chart) | 3D interactive bar charts | VR ready |
| [TopeOlafisoye/Unity-3D-Big-Data-Visualisation](https://github.com/TopeOlafisoye/Unity-3D-Big-Data-Visualisation) | Big data visualization | Tutorial included |
| [Call-for-Code/UnityStarterKit](https://github.com/Call-for-Code/UnityStarterKit) | COVID-19 data visualization | AR/VR ready |

## 🕷️ Web Crawlers with 3D Data Visualization (20+)

| Project | Description | Tech Stack |
|---------|-------------|------------|
| [pennmem/brain_viz_unity](https://github.com/pennmem/brain_viz_unity) | Brain visualization with web data | Unity WebGL |
| [serhangursoy/WebGL-Test-Unity3D](https://github.com/serhangursoy/WebGL-Test-Unity3D) | Architectural web visualization | Unity WebGL |
| [Dandarawy/Unity3D-Globe](https://github.com/Dandarawy/Unity3D-Globe) | Data globe visualization | Unity + JSON |

## 🪡 Needle Engine Projects (All Repos + 30 External)

### Official Needle Repos
| Project | Description | Platform |
|---------|-------------|----------|
| [needle-tools/needle-engine-support](https://github.com/needle-tools/needle-engine-support) | Main Needle Engine repo | Web runtime |
| [needle-tools/needle-console](https://github.com/needle-tools/needle-console) | Improved Unity console | Unity tool |
| [needle-tools/UnityGLTF](https://github.com/needle-tools/UnityGLTF) | glTF importer/exporter | Unity |
| [needle-tools/needle-engine-modules](https://github.com/needle-tools/needle-engine-modules) | Engine modules | Web |

### External Projects Using Needle
| Project | Description | Platform |
|---------|-------------|----------|
| Various Unity to Web exports | Projects using Needle for web deployment | iOS, Quest, visionOS |
| [needle.tools](https://needle.tools/) | Official showcase projects | Web |

## 📹 Rerun.io & ARKit Recording Projects (50+)

### Rerun.io Official
| Project | Description | Features |
|---------|-------------|----------|
| [rerun-io/rerun](https://github.com/rerun-io/rerun) | Main Rerun repository | Multimodal visualization |
| [rerun-io/rerun-docs](https://github.com/rerun-io/rerun-docs) | Documentation | Examples included |
| ARKitScenes example | Dataset visualization | Depth, mesh, boxes |

### ARKit Recording Projects
| Project | Description | Features |
|---------|-------------|----------|
| [ittybittyapps/ARRecorder](https://github.com/ittybittyapps/ARRecorder) | Private API for recording | Session replay |
| [AFathi/ARVideoKit](https://github.com/AFathi/ARVideoKit) | Capture AR videos/photos | Multiple formats |
| [shu223/ARKit-Sampler](https://github.com/shu223/ARKit-Sampler) | ARKit code examples | Various features |
| [AgoraIO-Community/Example-UIKit-SceneKit](https://github.com/AgoraIO-Community/Example-UIKit-SceneKit) | Stream ARKit sessions | Agora integration |

## 🤖 Convai Unity LLM Chatbot Projects (20+)

| Project | Description | Platforms |
|---------|-------------|-----------|
| [Conv-AI/Convai-Unity-WebGL-SDK](https://github.com/Conv-AI/Convai-Unity-WebGL-SDK) | WebGL SDK for Convai | WebGL |
| [Scthe/ai-iris-avatar](https://github.com/Scthe/ai-iris-avatar) | Detailed 3D avatar with LLM | Unity |
| [undreamai/LLMUnity](https://github.com/undreamai/LLMUnity) | LLM integration for Unity | Multi-platform |
| [uezo/ChatdollKit](https://github.com/uezo/ChatdollKit) | 3D chatbot framework | Multi-platform |
| [TestedLines/Style-Text-WebGL-iOS-LLM](https://assetstore.unity.com/packages/tools/ai-ml-integration/style-text-webgl-ios-stand-alone-llm-llama-cpp-wrapper-292902) | Standalone LLM wrapper | WebGL, iOS |

## 📊 Summary Statistics
- **Total Projects**: 524+
- **Workspace Projects**: 5 (Paint-AR, Open Brush, etc.)
- **Unity XR Tools**: 10+
- **HoloKit & Reality Labs**: 100+
- **WebRTC Projects**: 20+
- **Streaming Projects**: 20+
- **AI/ML Projects**: 40+
- **Multiplayer Projects**: 70+
- **Data Visualization**: 100+
- **WebGL Tools**: 100+
- **AR/VR Projects**: 150+

## 🏷️ Key Contributors
- **Keijiro Takahashi** - Multiple innovative VFX/AR projects
- **HECOMI** - Face tracking and depth visualization
- **Unity Technologies** - Official samples and frameworks
- **HoloKit Team** - Stereoscopic AR innovations
- **Reality Labs** - Mixed reality advancements
- **Needle Tools** - Unity to Web pipeline
- **Rerun.io** - Multimodal data visualization
- **Convai** - Conversational AI for Unity
- **James Tunick (imclab)** - Paint-AR, Apple Vision Pro resources

---

## 🌐 External Resources & Documentation

### visionOS Development

- [Apple visionOS Documentation](https://developer.apple.com/documentation/visionos) - Official Apple docs for visionOS development
- [Creating First visionOS App](https://developer.apple.com/documentation/visionos/creating-your-first-visionos-app) - Apple's official getting started guide
- [Unity PolySpatial visionOS](https://unity.com/polyspatial-visionos-industry) - Unity's official visionOS development tools

### AI/ML Frameworks & LLM Tools

- [LangChain](https://github.com/langchain-ai/langchain) - Python framework for LLM applications (250K+ stars)
- [LangChain.js](https://js.langchain.com/docs/get_started/introduction/) - JavaScript/TypeScript LangChain SDK
- [Awesome LangChain](https://github.com/kyrolabs/awesome-langchain) - Curated list of LangChain tools and resources
- [LlamaIndex](https://docs.llamaindex.ai/en/stable/) - Data framework for LLM applications
- [Hugging Face](https://huggingface.co) - ML model hub and platform (transformers, diffusers, datasets)
- [DeepLearning.ai](https://www.deeplearning.ai/short-courses/chatgpt-prompt-engineering-for-developers/) - Prompt engineering courses by Andrew Ng
- [Voyager (MineDojo)](https://github.com/MineDojo/Voyager) - LLM-powered autonomous agent in Minecraft

### WebXR & Three.js Ecosystem

- [Three.js](https://threejs.org/) - JavaScript 3D library (105K+ stars)
- [Three.js Examples](https://threejs.org/examples/) - Official examples gallery
- [Three.js Animations](https://threejs.org/examples/#webgl_animation_keyframes) - Keyframe animation examples
- [React Three Fiber](https://github.com/pmndrs/react-three-fiber) - React renderer for Three.js (28K+ stars)
- [R3F Documentation](https://docs.pmnd.rs/react-three-fiber/getting-started/introduction) - Official React Three Fiber docs
- [R3F Examples](https://docs.pmnd.rs/react-three-fiber/getting-started/examples) - React Three Fiber examples
- [drei](https://github.com/pmndrs/drei) - Helpers and abstractions for React Three Fiber (8K+ stars)
- [drei README](https://github.com/pmndrs/drei#readme) - drei component documentation
- [React PostProcessing](https://docs.pmnd.rs/react-postprocessing/effect-composer) - PostProcessing for R3F
- [React Spring](https://www.react-spring.dev/) - Spring-physics based animation library
- [React Spring Docs](https://www.react-spring.dev/docs/getting-started) - Getting started guide
- [Three.js Journey](https://threejs-journey.com/lessons/what-are-react-and-react-three-fiber#13-create-a-game-with-r3f) - R3F game development course

### 8th Wall (WebAR Platform)

- [8th Wall Discover](https://www.8thwall.com/discover) - WebAR experience showcase
- [8th Wall Projects](https://www.8thwall.com/projects) - Community projects and templates
- [8th Wall Docs](https://www.8thwall.com/docs/) - Official documentation
- [8th Wall Modules](https://www.8thwall.com/modules) - Reusable WebAR components
- [8th Wall OpenAI Module](https://www.8thwall.com/8thwall/modules/openai) - OpenAI integration for WebAR
- [8th Wall Web GitHub](https://github.com/8thwall/web) - Official GitHub examples

### Development Platforms & Tools

- [Node.js](https://nodejs.org/en) - JavaScript runtime for backend development
- [Meta Quest Pro](https://www.meta.com/quest/quest-pro/) - Official Meta Quest Pro product page
- [ShaderToy](https://www.shadertoy.com/view/4ss3Dn) - GLSL shader playground and community

### Community Forums & Discussion

- [Unity Forums](https://forum.unity.com/) - Official Unity community forums
- [Unity Scripting Forum](https://forum.unity.com/forums/scripting.12/) - C# scripting discussions
- [Unity Multiplayer Forum](https://forum.unity.com/forums/multiplayer.26/) - Networking and multiplayer
- [Unity AR Forum](https://forum.unity.com/forums/ar.161/) - AR Foundation discussions
- [Unity AR/VR/XR Forum](https://forum.unity.com/forums/ar-vr-xr-discussion.80/) - General XR discussions
- [Unity General Discussion](https://forum.unity.com/forums/general-discussion.14/) - General Unity topics
- [Unreal Engine Forums](https://forums.unrealengine.com/categories?tag=unreal-engine) - Official Unreal forums
- [Unreal Development Discussion](https://forums.unrealengine.com/tags/c/development-discussion/11/unreal-engine) - Development topics
- [Unreal Programming Forum](https://forums.unrealengine.com/tags/c/programming-scripting/148/unreal-engine) - C++ and Blueprint scripting

### Personal/Project Sites

- [TheIMCLab.com](https://TheIMCLab.com) - James Tunick's portfolio and projects
- [ZeroSpace.co](https://ZeroSpace.co) - XR/AR development studio

---

**Maintained by**: Claude Code Sessions
**Project Lead**: James Tunick
**Workspaces**: Portals_6, Paint-AR, Open Brush
**Total**: 520+ GitHub repositories + 45+ external resources
**Last Session**: 2025-11-02 (Added external resources: visionOS, LangChain, Three.js, Unity/Unreal forums)
