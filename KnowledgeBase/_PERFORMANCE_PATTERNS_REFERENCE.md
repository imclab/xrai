# Performance Patterns Reference

**Version**: 1.0
**Last Updated**: 2025-01-07
**Purpose**: Quick reference for Unity and WebGL/Three.js performance optimization patterns

---

## Unity Performance Patterns

### Object Pooling for Mobile/XR
```csharp
/// <summary>
/// High-performance object pooling for mobile and XR platforms
/// Preallocates objects to avoid runtime instantiation costs
/// </summary>
public class FastPool<T> where T : Component
{
    private Stack<T> pool = new Stack<T>(1000);
    private T prefab;

    public FastPool(T prefab, int initialSize = 100)
    {
        this.prefab = prefab;
        for (int i = 0; i < initialSize; i++)
        {
            var obj = Object.Instantiate(prefab);
            obj.gameObject.SetActive(false);
            pool.Push(obj);
        }
    }

    public T Get()
    {
        if (pool.Count > 0)
        {
            var obj = pool.Pop();
            obj.gameObject.SetActive(true);
            return obj;
        }
        return Object.Instantiate(prefab);
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Push(obj);
    }
}

// Usage example
FastPool<ParticleSystem> particlePool = new FastPool<ParticleSystem>(particlePrefab, 50);
var particle = particlePool.Get();
// ... use particle ...
particlePool.Return(particle);
```

### Addressables for Dynamic Loading
```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

/// <summary>
/// Optimized async asset loading with Addressables
/// Reduces memory footprint by loading assets on-demand
/// </summary>
public class AssetLoader
{
    public async Task<GameObject> LoadOptimized(string key)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(key);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            return handle.Result;
        }

        Debug.LogError($"Failed to load asset: {key}");
        return null;
    }

    public void ReleaseAsset(GameObject asset)
    {
        Addressables.Release(asset);
    }
}

// Usage example
AssetLoader loader = new AssetLoader();
GameObject prefab = await loader.LoadOptimized("MyPrefab");
// ... use prefab ...
loader.ReleaseAsset(prefab);
```

### VFX Graph Performance Tips
```csharp
/// <summary>
/// VFX Graph optimization checklist:
/// 1. Use GPU Events instead of CPU Events
/// 2. Minimize particle count (target: <100k for Quest 2)
/// 3. Use texture atlases instead of multiple textures
/// 4. Enable static properties when possible
/// 5. Batch similar systems together
/// 6. Use LOD (Level of Detail) for distant effects
/// </summary>
public class VFXOptimizer : MonoBehaviour
{
    [SerializeField] private VisualEffect vfx;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float maxDistance = 50f;

    void Update()
    {
        // Distance-based LOD
        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);

        if (distance > maxDistance)
        {
            vfx.pause = true;
        }
        else if (distance > maxDistance * 0.7f)
        {
            // Reduce particle count at medium distance
            vfx.SetFloat("ParticleMultiplier", 0.5f);
        }
        else
        {
            vfx.pause = false;
            vfx.SetFloat("ParticleMultiplier", 1.0f);
        }
    }
}
```

### Burst-Compiled Jobs for Heavy Computation
```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Burst-compiled job for parallel particle position updates
/// Achieves 10-100x speedup over traditional C# loops
/// </summary>
[BurstCompile]
public struct ParticleUpdateJob : IJobParallelFor
{
    public NativeArray<float3> positions;
    public NativeArray<float3> velocities;
    public float deltaTime;

    public void Execute(int index)
    {
        // Burst compiles this to highly optimized SIMD code
        positions[index] += velocities[index] * deltaTime;

        // Apply simple gravity
        velocities[index] += new float3(0, -9.8f, 0) * deltaTime;
    }
}

// Usage
NativeArray<float3> positions = new NativeArray<float3>(10000, Allocator.TempJob);
NativeArray<float3> velocities = new NativeArray<float3>(10000, Allocator.TempJob);

var job = new ParticleUpdateJob
{
    positions = positions,
    velocities = velocities,
    deltaTime = Time.deltaTime
};

JobHandle handle = job.Schedule(positions.Length, 64);
handle.Complete();

// Remember to dispose NativeArrays
positions.Dispose();
velocities.Dispose();
```

---

## WebGL/Three.js Performance Patterns

### Instanced Rendering (1M Objects @ 144fps)
```javascript
/**
 * InstancedMesh for massive object counts
 * Achieves 144fps with 1 million instances on high-end GPUs
 * Quest 2 can handle ~100k instances at 90fps
 */
import * as THREE from 'three';

class InstancedRenderer {
    constructor(geometry, material, count) {
        this.mesh = new THREE.InstancedMesh(geometry, material, count);
        this.dummy = new THREE.Object3D();
        this.count = count;
    }

    updateInstances(positions, colors) {
        for (let i = 0; i < this.count; i++) {
            // Set position
            this.dummy.position.copy(positions[i]);
            this.dummy.updateMatrix();
            this.mesh.setMatrixAt(i, this.dummy.matrix);

            // Set color
            this.mesh.setColorAt(i, colors[i]);
        }

        // Critical: mark for GPU update
        this.mesh.instanceMatrix.needsUpdate = true;
        this.mesh.instanceColor.needsUpdate = true;
    }
}

// Usage
const geometry = new THREE.SphereGeometry(0.1);
const material = new THREE.MeshBasicMaterial();
const renderer = new InstancedRenderer(geometry, material, 1000000);

// Prepare data
const positions = Array(1000000).fill().map(() => new THREE.Vector3(
    Math.random() * 100 - 50,
    Math.random() * 100 - 50,
    Math.random() * 100 - 50
));
const colors = Array(1000000).fill().map(() => new THREE.Color(Math.random(), Math.random(), Math.random()));

renderer.updateInstances(positions, colors);
scene.add(renderer.mesh);
```

### Texture Atlas for Reduced Draw Calls
```javascript
/**
 * Texture atlas reduces draw calls by batching multiple textures
 * Single atlas can replace 100+ individual texture loads
 */
class TextureAtlas {
    constructor(images, atlasSize = 2048) {
        const canvas = document.createElement('canvas');
        canvas.width = atlasSize;
        canvas.height = atlasSize;
        const ctx = canvas.getContext('2d');

        // Calculate grid layout
        const cols = Math.ceil(Math.sqrt(images.length));
        const cellSize = atlasSize / cols;

        // Draw images to atlas
        images.forEach((img, i) => {
            const x = (i % cols) * cellSize;
            const y = Math.floor(i / cols) * cellSize;
            ctx.drawImage(img, x, y, cellSize, cellSize);
        });

        this.texture = new THREE.CanvasTexture(canvas);
        this.cellSize = cellSize;
        this.atlasSize = atlasSize;
    }

    getUVOffset(index) {
        const cols = this.atlasSize / this.cellSize;
        return {
            x: (index % cols) / cols,
            y: Math.floor(index / cols) / cols,
            scale: 1 / cols
        };
    }
}
```

### GPU-Based Particle System (GPGPU)
```javascript
/**
 * GPU particle simulation using compute shaders (WebGPU)
 * or ping-pong framebuffer rendering (WebGL)
 * Handles millions of particles in real-time
 */
import { GPUComputationRenderer } from 'three/examples/jsm/misc/GPUComputationRenderer.js';

class GPUParticles {
    constructor(renderer, count = 1000000) {
        const width = Math.sqrt(count);
        this.gpuCompute = new GPUComputationRenderer(width, width, renderer);

        // Position texture
        const dtPosition = this.gpuCompute.createTexture();
        this.fillPositionTexture(dtPosition);

        // Velocity texture
        const dtVelocity = this.gpuCompute.createTexture();
        this.fillVelocityTexture(dtVelocity);

        // Shaders
        const positionVariable = this.gpuCompute.addVariable(
            'texturePosition',
            positionShader,
            dtPosition
        );
        const velocityVariable = this.gpuCompute.addVariable(
            'textureVelocity',
            velocityShader,
            dtVelocity
        );

        this.gpuCompute.setVariableDependencies(positionVariable, [positionVariable, velocityVariable]);
        this.gpuCompute.setVariableDependencies(velocityVariable, [positionVariable, velocityVariable]);

        this.gpuCompute.init();

        this.positionVariable = positionVariable;
        this.velocityVariable = velocityVariable;
    }

    update() {
        this.gpuCompute.compute();

        // Get current position texture for rendering
        return this.gpuCompute.getCurrentRenderTarget(this.positionVariable).texture;
    }
}
```

### Web Worker for Heavy Computation
```javascript
/**
 * Offload physics/AI to Web Workers
 * Prevents main thread blocking, maintains 60fps
 */

// worker.js
self.addEventListener('message', (e) => {
    const { positions, velocities, deltaTime } = e.data;

    // Heavy computation in worker thread
    for (let i = 0; i < positions.length; i++) {
        positions[i].x += velocities[i].x * deltaTime;
        positions[i].y += velocities[i].y * deltaTime;
        positions[i].z += velocities[i].z * deltaTime;

        // Simple physics
        velocities[i].y -= 9.8 * deltaTime;
    }

    self.postMessage({ positions, velocities });
});

// Main thread
class WorkerManager {
    constructor() {
        this.worker = new Worker('worker.js');
        this.worker.addEventListener('message', (e) => {
            this.onUpdate(e.data);
        });
    }

    update(positions, velocities, deltaTime) {
        this.worker.postMessage({ positions, velocities, deltaTime });
    }

    onUpdate(data) {
        // Update rendering with computed data
        this.positions = data.positions;
        this.velocities = data.velocities;
    }
}
```

---

## Cross-Platform Optimization Checklist

### Unity Mobile/XR Targets
```yaml
Performance Targets:
  Quest 2: 90 FPS (11.1ms frame budget)
  Quest 3: 120 FPS (8.3ms frame budget)
  iPhone 12+: 60 FPS (16.6ms frame budget)

Optimization Priorities:
  1. Reduce draw calls (< 100 for mobile)
  2. Minimize overdraw (use occlusion culling)
  3. Optimize texture size (max 1024x1024 for mobile)
  4. Use texture compression (ASTC for mobile)
  5. Limit particle systems (< 10k particles total)
  6. Profile with Unity Profiler (CPU/GPU/Memory)
  7. Test on worst target device first

Quest-Specific:
  - Use single-pass stereo rendering
  - Enable fixed foveated rendering
  - Limit shadow distance (5-10m max)
  - Use baked lighting where possible
  - Avoid dynamic GI
```

### WebGL Performance Targets
```yaml
Performance Targets:
  Desktop (High-end): 144 FPS
  Desktop (Mid-range): 60 FPS
  Mobile (Flagship): 60 FPS
  Mobile (Mid-range): 30 FPS

Optimization Priorities:
  1. Use instancing for repeated geometry
  2. Implement frustum culling
  3. Use texture atlases
  4. Minimize shader complexity
  5. Implement LOD (Level of Detail)
  6. Use Web Workers for physics/AI
  7. Enable hardware acceleration
  8. Test on "potato device" first

WebXR-Specific:
  - Target 90fps minimum
  - Use multiview rendering
  - Limit vertex count per frame
  - Optimize for mobile GPUs (Mali, Adreno)
  - Test on Quest Browser
```

---

## Profiling Tools

### Unity Profiler
```csharp
using UnityEngine.Profiling;

// Custom profiling markers
void Update()
{
    Profiler.BeginSample("MyExpensiveOperation");
    PerformExpensiveOperation();
    Profiler.EndSample();
}

// Memory profiling
void CheckMemory()
{
    long totalMemory = Profiler.GetTotalAllocatedMemoryLong();
    long usedMemory = Profiler.GetMonoUsedSizeLong();
    Debug.Log($"Total: {totalMemory / 1024 / 1024}MB, Used: {usedMemory / 1024 / 1024}MB");
}
```

### Chrome DevTools for WebGL
```javascript
// Performance monitoring
class PerformanceMonitor {
    constructor() {
        this.lastTime = performance.now();
        this.frames = 0;
        this.fps = 0;
    }

    update() {
        this.frames++;
        const now = performance.now();

        if (now >= this.lastTime + 1000) {
            this.fps = Math.round((this.frames * 1000) / (now - this.lastTime));
            this.frames = 0;
            this.lastTime = now;
        }

        return this.fps;
    }
}

// Memory monitoring
function checkMemory() {
    if (performance.memory) {
        const used = performance.memory.usedJSHeapSize / 1024 / 1024;
        const total = performance.memory.totalJSHeapSize / 1024 / 1024;
        console.log(`Memory: ${used.toFixed(2)}MB / ${total.toFixed(2)}MB`);
    }
}
```

---

## Quick Optimization Decision Tree

```
Is performance < 60fps?
├─ YES: Profile to find bottleneck
│   ├─ CPU bound?
│   │   ├─ Too many draw calls? → Batch meshes, use instancing
│   │   ├─ Heavy physics? → Use Burst jobs (Unity) or Web Workers (Web)
│   │   └─ Complex scripts? → Optimize loops, cache references
│   └─ GPU bound?
│       ├─ Too many vertices? → Use LOD, reduce poly count
│       ├─ Overdraw? → Enable occlusion culling, optimize transparency
│       └─ Shader complexity? → Simplify shaders, reduce texture samples
└─ NO: Optimize further for lower-end devices
    └─ Test on "potato device" (5-year-old phone/Quest 2)
```

---

**Remember**: Always profile before optimizing. Premature optimization wastes time. Measure, optimize, verify.

**Target Philosophy**: If it runs at 60fps on a 5-year-old device, it will fly on modern hardware.
