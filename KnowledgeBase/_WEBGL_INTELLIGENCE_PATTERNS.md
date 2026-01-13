# WebGL Intelligence Patterns

**Activation**: Say **"Using WebGL Intelligence patterns"**
**Covers**: WebGL, WebGPU, Three.js, React Three Fiber, GLSL/TSL

---

## 1. WebGPU Million Particles (Operation Swarm Pattern)

From `shadowofaroman/Operation-Swarm` - 400K+ particles at 60FPS.

### Architecture: GPU-First Compute
```javascript
// Traditional: CPU updates every particle
// WebGPU: GPU maintains simulation loop internally

import { StorageInstancedBufferAttribute } from 'three/webgpu';

// Storage attributes are READ/WRITE on GPU
const positionAttribute = new StorageInstancedBufferAttribute(
  new Float32Array(PARTICLE_COUNT * 3), 3
);
const velocityAttribute = new StorageInstancedBufferAttribute(
  new Float32Array(PARTICLE_COUNT * 3), 3
);
```

### TSL (Three Shading Language) Physics
```javascript
import { tslFn, storage, uniform, instanceIndex } from 'three/tsl';

const computeParticles = tslFn(() => {
  const position = storage(positionAttribute, 'vec3', instanceIndex);
  const velocity = storage(velocityAttribute, 'vec3', instanceIndex);

  // Physics
  const gravity = uniform(new Vector3(0, -9.81, 0));
  const drag = uniform(0.98);

  // Update velocity
  velocity.addAssign(gravity.mul(deltaTime));
  velocity.mulAssign(drag);

  // Update position
  position.addAssign(velocity.mul(deltaTime));
});

// Execute compute shader each frame
renderer.computeAsync(computeParticles);
```

### Velocity-Based Color Mapping
```javascript
const colorByVelocity = tslFn(() => {
  const speed = velocity.length();
  const t = clamp(speed.div(maxSpeed), 0, 1);

  // Blue (slow) -> Red (fast)
  return mix(vec3(0, 0, 1), vec3(1, 0, 0), t);
});
```

---

## 2. Three.js Core Patterns

### Scene Setup (Modern)
```javascript
import * as THREE from 'three';

const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });

renderer.setSize(window.innerWidth, window.innerHeight);
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2)); // Cap at 2x for performance
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
```

### Instanced Mesh (10K+ objects)
```javascript
const geometry = new THREE.BoxGeometry(0.1, 0.1, 0.1);
const material = new THREE.MeshStandardMaterial();
const mesh = new THREE.InstancedMesh(geometry, material, 10000);

const dummy = new THREE.Object3D();
const matrix = new THREE.Matrix4();

for (let i = 0; i < 10000; i++) {
  dummy.position.set(
    Math.random() * 10 - 5,
    Math.random() * 10 - 5,
    Math.random() * 10 - 5
  );
  dummy.rotation.set(Math.random() * Math.PI, Math.random() * Math.PI, 0);
  dummy.scale.setScalar(0.5 + Math.random() * 0.5);
  dummy.updateMatrix();
  mesh.setMatrixAt(i, dummy.matrix);
}
mesh.instanceMatrix.needsUpdate = true;
```

### BufferGeometry Particles
```javascript
const particleCount = 100000;
const positions = new Float32Array(particleCount * 3);
const colors = new Float32Array(particleCount * 3);

for (let i = 0; i < particleCount; i++) {
  positions[i * 3] = (Math.random() - 0.5) * 100;
  positions[i * 3 + 1] = (Math.random() - 0.5) * 100;
  positions[i * 3 + 2] = (Math.random() - 0.5) * 100;

  colors[i * 3] = Math.random();
  colors[i * 3 + 1] = Math.random();
  colors[i * 3 + 2] = Math.random();
}

const geometry = new THREE.BufferGeometry();
geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));

const material = new THREE.PointsMaterial({
  size: 0.1,
  vertexColors: true,
  transparent: true,
  opacity: 0.8,
  sizeAttenuation: true
});

const particles = new THREE.Points(geometry, material);
```

---

## 3. React Three Fiber (R3F) Patterns

### Basic R3F Scene
```jsx
import { Canvas, useFrame } from '@react-three/fiber';
import { OrbitControls, Environment } from '@react-three/drei';

function App() {
  return (
    <Canvas camera={{ position: [0, 0, 5], fov: 75 }}>
      <ambientLight intensity={0.5} />
      <pointLight position={[10, 10, 10]} />
      <Box />
      <OrbitControls />
      <Environment preset="sunset" />
    </Canvas>
  );
}

function Box() {
  const meshRef = useRef();

  useFrame((state, delta) => {
    meshRef.current.rotation.x += delta;
    meshRef.current.rotation.y += delta * 0.5;
  });

  return (
    <mesh ref={meshRef}>
      <boxGeometry args={[1, 1, 1]} />
      <meshStandardMaterial color="orange" />
    </mesh>
  );
}
```

### R3F Instanced Mesh
```jsx
import { useRef, useMemo } from 'react';
import { useFrame } from '@react-three/fiber';
import * as THREE from 'three';

function Instances({ count = 1000 }) {
  const meshRef = useRef();
  const dummy = useMemo(() => new THREE.Object3D(), []);

  const particles = useMemo(() => {
    return Array.from({ length: count }, () => ({
      position: [
        (Math.random() - 0.5) * 10,
        (Math.random() - 0.5) * 10,
        (Math.random() - 0.5) * 10
      ],
      scale: 0.2 + Math.random() * 0.3
    }));
  }, [count]);

  useFrame(({ clock }) => {
    particles.forEach((particle, i) => {
      dummy.position.set(...particle.position);
      dummy.position.y += Math.sin(clock.elapsedTime + i) * 0.01;
      dummy.scale.setScalar(particle.scale);
      dummy.updateMatrix();
      meshRef.current.setMatrixAt(i, dummy.matrix);
    });
    meshRef.current.instanceMatrix.needsUpdate = true;
  });

  return (
    <instancedMesh ref={meshRef} args={[null, null, count]}>
      <sphereGeometry args={[0.1, 16, 16]} />
      <meshStandardMaterial color="#ff6600" />
    </instancedMesh>
  );
}
```

### R3F with Drei Helpers
```jsx
import {
  Text,
  Float,
  MeshDistortMaterial,
  GradientTexture,
  Stars,
  Sparkles
} from '@react-three/drei';

function Scene() {
  return (
    <>
      <Stars radius={100} depth={50} count={5000} factor={4} />
      <Sparkles count={100} scale={10} size={2} speed={0.4} />

      <Float speed={2} rotationIntensity={1} floatIntensity={2}>
        <mesh>
          <sphereGeometry args={[1, 64, 64]} />
          <MeshDistortMaterial distort={0.5} speed={2}>
            <GradientTexture stops={[0, 1]} colors={['#ff0000', '#0000ff']} />
          </MeshDistortMaterial>
        </mesh>
      </Float>

      <Text
        position={[0, 2, 0]}
        fontSize={0.5}
        color="white"
        anchorX="center"
        anchorY="middle"
      >
        Hello R3F!
      </Text>
    </>
  );
}
```

---

## 4. GLSL Shader Patterns

### Vertex Shader (Particle Animation)
```glsl
uniform float uTime;
uniform float uSize;

attribute float aScale;
attribute vec3 aRandomness;

varying vec3 vColor;

void main() {
  vec4 modelPosition = modelMatrix * vec4(position, 1.0);

  // Spin animation
  float angle = atan(modelPosition.x, modelPosition.z);
  float distanceToCenter = length(modelPosition.xz);
  float angleOffset = (1.0 / distanceToCenter) * uTime * 0.2;
  angle += angleOffset;
  modelPosition.x = cos(angle) * distanceToCenter;
  modelPosition.z = sin(angle) * distanceToCenter;

  // Add randomness
  modelPosition.xyz += aRandomness;

  vec4 viewPosition = viewMatrix * modelPosition;
  vec4 projectedPosition = projectionMatrix * viewPosition;

  gl_Position = projectedPosition;
  gl_PointSize = uSize * aScale;
  gl_PointSize *= (1.0 / -viewPosition.z); // Size attenuation

  vColor = color;
}
```

### Fragment Shader (Soft Particle)
```glsl
varying vec3 vColor;

void main() {
  // Circle shape
  float distanceToCenter = distance(gl_PointCoord, vec2(0.5));
  if (distanceToCenter > 0.5) discard;

  // Soft edge
  float strength = 1.0 - (distanceToCenter * 2.0);
  strength = pow(strength, 2.0);

  gl_FragColor = vec4(vColor, strength);
}
```

### Audio Reactive Shader
```glsl
uniform float uBass;
uniform float uMid;
uniform float uHigh;
uniform float uTime;

varying vec2 vUv;

void main() {
  // Distortion based on audio
  vec2 uv = vUv;
  uv.x += sin(uv.y * 10.0 + uTime) * uBass * 0.1;
  uv.y += cos(uv.x * 10.0 + uTime) * uMid * 0.1;

  // Color based on frequency bands
  vec3 color = vec3(uBass, uMid, uHigh);
  color = mix(vec3(0.1, 0.2, 0.3), color, 0.5);

  gl_FragColor = vec4(color, 1.0);
}
```

---

## 5. Performance Optimization

### GPU Particle System Pattern
```javascript
class GPUParticleSystem {
  constructor(count, renderer) {
    this.count = count;
    this.renderer = renderer;

    // Double-buffer for compute shaders
    this.positionBufferA = this.createBuffer(count * 4);
    this.positionBufferB = this.createBuffer(count * 4);

    // Ping-pong between buffers
    this.currentBuffer = 0;
  }

  createBuffer(size) {
    return new THREE.DataTexture(
      new Float32Array(size),
      Math.sqrt(size / 4),
      Math.sqrt(size / 4),
      THREE.RGBAFormat,
      THREE.FloatType
    );
  }

  update() {
    const readBuffer = this.currentBuffer === 0 ? this.positionBufferA : this.positionBufferB;
    const writeBuffer = this.currentBuffer === 0 ? this.positionBufferB : this.positionBufferA;

    // Compute pass
    this.computeShader.uniforms.tPositions.value = readBuffer;
    this.renderer.setRenderTarget(writeBuffer);
    this.renderer.render(this.computeScene, this.computeCamera);

    // Swap buffers
    this.currentBuffer = 1 - this.currentBuffer;
  }
}
```

### LOD (Level of Detail)
```javascript
const lod = new THREE.LOD();

// High detail (close)
const highDetail = new THREE.Mesh(
  new THREE.SphereGeometry(1, 64, 64),
  material
);
lod.addLevel(highDetail, 0);

// Medium detail
const medDetail = new THREE.Mesh(
  new THREE.SphereGeometry(1, 32, 32),
  material
);
lod.addLevel(medDetail, 50);

// Low detail (far)
const lowDetail = new THREE.Mesh(
  new THREE.SphereGeometry(1, 8, 8),
  material
);
lod.addLevel(lowDetail, 200);

scene.add(lod);
```

### Memory Management
```javascript
// Dispose unused resources
function dispose(object) {
  if (object.geometry) object.geometry.dispose();
  if (object.material) {
    if (Array.isArray(object.material)) {
      object.material.forEach(m => m.dispose());
    } else {
      object.material.dispose();
    }
  }
  if (object.texture) object.texture.dispose();
}

// Object pooling
class ObjectPool {
  constructor(factory, initialSize = 100) {
    this.factory = factory;
    this.pool = Array.from({ length: initialSize }, () => factory());
    this.active = [];
  }

  acquire() {
    return this.pool.pop() || this.factory();
  }

  release(object) {
    object.visible = false;
    this.pool.push(object);
  }
}
```

---

## 6. WebXR Patterns

### Three.js WebXR Setup
```javascript
renderer.xr.enabled = true;
document.body.appendChild(VRButton.createButton(renderer));

// XR animation loop
renderer.setAnimationLoop((time, frame) => {
  if (frame) {
    const referenceSpace = renderer.xr.getReferenceSpace();
    const session = renderer.xr.getSession();

    // Handle XR input
    session.inputSources.forEach(source => {
      if (source.gamepad) {
        const { buttons, axes } = source.gamepad;
        // Process input
      }
    });
  }
  renderer.render(scene, camera);
});
```

### R3F XR
```jsx
import { VRButton, XR, Controllers, Hands } from '@react-three/xr';

function App() {
  return (
    <>
      <VRButton />
      <Canvas>
        <XR>
          <Controllers />
          <Hands />
          <ambientLight />
          <Scene />
        </XR>
      </Canvas>
    </>
  );
}
```

---

## 7. Gaussian Splatting (Web)

### Luma AI WebGL Splats
```javascript
import { LumaSplatsThree } from '@lumaai/luma-web';

const splats = new LumaSplatsThree({
  source: 'https://lumalabs.ai/capture/...',
  loadingAnimationEnabled: true
});

scene.add(splats);
```

### SuperSplat Integration
```javascript
// Load .splat file
const loader = new SplatLoader();
loader.load('scene.splat', (splat) => {
  scene.add(splat);
});
```

---

## Quick Reference

| Task | Pattern |
|------|---------|
| 100K+ particles | InstancedMesh or BufferGeometry Points |
| 1M+ particles | WebGPU Compute Shaders |
| React integration | React Three Fiber |
| VR/AR | WebXR API |
| Gaussian splats | LumaSplatsThree |
| Performance | LOD, Object Pooling, Dispose |

---

## GitHub Repos Referenced

- `shadowofaroman/Operation-Swarm` - WebGPU 400K+ particles
- `onion2k/r3f-by-example` - R3F examples collection
- `mlt131220/Astral3D` - Vue3 + Three.js engine
- `ULuIQ12/webgputest-particlesSDF` - WebGPU SDF particles
- `pmndrs/react-three-fiber` - React renderer for Three.js
- `pmndrs/drei` - R3F helpers library

---

## Browser Support

| Feature | Chrome | Firefox | Safari | Edge |
|---------|--------|---------|--------|------|
| WebGL 2.0 | 56+ | 51+ | 15+ | 79+ |
| WebGPU | 113+ | Nightly | Preview | 113+ |
| WebXR | 79+ | 98+ | No | 79+ |

---

*Updated: 2026-01-13*
