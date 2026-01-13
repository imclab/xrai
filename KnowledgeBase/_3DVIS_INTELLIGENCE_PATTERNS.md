# 3D Visualization Intelligence Patterns

**Activation**: Say **"Using 3DVis Intelligence patterns"**
**Covers**: Sorting, clustering, pattern recognition, anomaly detection, graphing, semantic parsing, spatial algorithms

---

## 1. Sorting Algorithms for Rendering

### Z-Sorting (Painter's Algorithm)
```javascript
// Sort objects back-to-front for transparency
function zSort(objects, camera) {
  const cameraPosition = camera.position;

  return objects.sort((a, b) => {
    const distA = a.position.distanceTo(cameraPosition);
    const distB = b.position.distanceTo(cameraPosition);
    return distB - distA; // Far to near
  });
}
```

### Radix Sort for Particles (GPU-friendly)
```javascript
// O(n) sorting for 32-bit depth values
function radixSort(particles, bits = 32) {
  const buckets = new Array(256).fill(null).map(() => []);

  for (let shift = 0; shift < bits; shift += 8) {
    // Scatter phase
    for (const p of particles) {
      const bucket = (p.depth >> shift) & 0xFF;
      buckets[bucket].push(p);
    }

    // Gather phase
    particles.length = 0;
    for (const bucket of buckets) {
      particles.push(...bucket);
      bucket.length = 0;
    }
  }
  return particles;
}
```

### Parallel Bitonic Sort (Compute Shader)
```glsl
// GLSL compute shader for GPU sorting
layout(local_size_x = 256) in;

shared float sharedDepth[256];
shared uint sharedIndex[256];

void bitonicCompare(uint i, uint j, bool ascending) {
  if ((sharedDepth[i] > sharedDepth[j]) == ascending) {
    // Swap
    float tempDepth = sharedDepth[i];
    uint tempIndex = sharedIndex[i];
    sharedDepth[i] = sharedDepth[j];
    sharedIndex[i] = sharedIndex[j];
    sharedDepth[j] = tempDepth;
    sharedIndex[j] = tempIndex;
  }
}

void main() {
  uint id = gl_LocalInvocationID.x;

  // Load to shared memory
  sharedDepth[id] = depths[gl_GlobalInvocationID.x];
  sharedIndex[id] = gl_GlobalInvocationID.x;
  barrier();

  // Bitonic sort
  for (uint k = 2; k <= 256; k *= 2) {
    for (uint j = k / 2; j > 0; j /= 2) {
      uint ixj = id ^ j;
      if (ixj > id) {
        bool ascending = ((id & k) == 0);
        bitonicCompare(id, ixj, ascending);
      }
      barrier();
    }
  }

  // Write back
  sortedIndices[gl_GlobalInvocationID.x] = sharedIndex[id];
}
```

---

## 2. Clustering Algorithms

### K-Means Clustering
```javascript
function kMeans(points, k, maxIterations = 100) {
  // Initialize centroids randomly
  let centroids = points.slice(0, k).map(p => ({ ...p }));

  for (let iter = 0; iter < maxIterations; iter++) {
    // Assignment step
    const clusters = Array.from({ length: k }, () => []);

    for (const point of points) {
      let minDist = Infinity;
      let cluster = 0;

      for (let i = 0; i < k; i++) {
        const dist = distance(point, centroids[i]);
        if (dist < minDist) {
          minDist = dist;
          cluster = i;
        }
      }
      clusters[cluster].push(point);
    }

    // Update step
    let converged = true;
    for (let i = 0; i < k; i++) {
      if (clusters[i].length === 0) continue;

      const newCentroid = computeCentroid(clusters[i]);
      if (distance(newCentroid, centroids[i]) > 0.001) {
        converged = false;
      }
      centroids[i] = newCentroid;
    }

    if (converged) break;
  }

  return { centroids, clusters: assignClusters(points, centroids) };
}

function computeCentroid(points) {
  const sum = { x: 0, y: 0, z: 0 };
  for (const p of points) {
    sum.x += p.x;
    sum.y += p.y;
    sum.z += p.z;
  }
  return {
    x: sum.x / points.length,
    y: sum.y / points.length,
    z: sum.z / points.length
  };
}
```

### DBSCAN (Density-Based)
```javascript
function dbscan(points, epsilon, minPoints) {
  const clusters = [];
  const visited = new Set();
  const noise = [];

  for (const point of points) {
    if (visited.has(point)) continue;
    visited.add(point);

    const neighbors = getNeighbors(points, point, epsilon);

    if (neighbors.length < minPoints) {
      noise.push(point);
    } else {
      const cluster = [];
      expandCluster(point, neighbors, cluster, visited, points, epsilon, minPoints);
      clusters.push(cluster);
    }
  }

  return { clusters, noise };
}

function expandCluster(point, neighbors, cluster, visited, points, epsilon, minPoints) {
  cluster.push(point);

  for (let i = 0; i < neighbors.length; i++) {
    const neighbor = neighbors[i];

    if (!visited.has(neighbor)) {
      visited.add(neighbor);
      const neighborNeighbors = getNeighbors(points, neighbor, epsilon);

      if (neighborNeighbors.length >= minPoints) {
        neighbors.push(...neighborNeighbors.filter(n => !neighbors.includes(n)));
      }
    }

    if (!cluster.includes(neighbor)) {
      cluster.push(neighbor);
    }
  }
}

function getNeighbors(points, point, epsilon) {
  return points.filter(p => distance(p, point) <= epsilon);
}
```

### Hierarchical Clustering
```javascript
function hierarchicalClustering(points, threshold) {
  // Start with each point as its own cluster
  let clusters = points.map(p => [p]);

  while (clusters.length > 1) {
    // Find closest pair
    let minDist = Infinity;
    let mergeI = 0, mergeJ = 1;

    for (let i = 0; i < clusters.length; i++) {
      for (let j = i + 1; j < clusters.length; j++) {
        const dist = clusterDistance(clusters[i], clusters[j]);
        if (dist < minDist) {
          minDist = dist;
          mergeI = i;
          mergeJ = j;
        }
      }
    }

    // Stop if distance exceeds threshold
    if (minDist > threshold) break;

    // Merge closest clusters
    clusters[mergeI] = [...clusters[mergeI], ...clusters[mergeJ]];
    clusters.splice(mergeJ, 1);
  }

  return clusters;
}

// Ward's method for cluster distance
function clusterDistance(c1, c2) {
  const centroid1 = computeCentroid(c1);
  const centroid2 = computeCentroid(c2);
  return distance(centroid1, centroid2);
}
```

---

## 3. Pattern Recognition

### Template Matching (3D Point Clouds)
```javascript
function templateMatch(source, template, threshold = 0.9) {
  // Normalize point clouds
  const normalizedSource = normalizePointCloud(source);
  const normalizedTemplate = normalizePointCloud(template);

  // Compute shape descriptors
  const sourceDescriptor = computeShapeDescriptor(normalizedSource);
  const templateDescriptor = computeShapeDescriptor(normalizedTemplate);

  // Compare descriptors
  const similarity = cosineSimilarity(sourceDescriptor, templateDescriptor);

  return similarity > threshold;
}

function normalizePointCloud(points) {
  const centroid = computeCentroid(points);
  const centered = points.map(p => ({
    x: p.x - centroid.x,
    y: p.y - centroid.y,
    z: p.z - centroid.z
  }));

  // Scale to unit sphere
  const maxDist = Math.max(...centered.map(p =>
    Math.sqrt(p.x * p.x + p.y * p.y + p.z * p.z)
  ));

  return centered.map(p => ({
    x: p.x / maxDist,
    y: p.y / maxDist,
    z: p.z / maxDist
  }));
}
```

### Shape Context Descriptor
```javascript
function computeShapeDescriptor(points, bins = { radial: 5, azimuthal: 12, polar: 6 }) {
  const histogram = new Float32Array(bins.radial * bins.azimuthal * bins.polar);

  for (const point of points) {
    const r = Math.sqrt(point.x * point.x + point.y * point.y + point.z * point.z);
    const theta = Math.atan2(point.y, point.x); // azimuthal
    const phi = Math.acos(point.z / (r + 1e-10)); // polar

    const rBin = Math.min(Math.floor(r * bins.radial), bins.radial - 1);
    const thetaBin = Math.floor((theta + Math.PI) / (2 * Math.PI) * bins.azimuthal);
    const phiBin = Math.floor(phi / Math.PI * bins.polar);

    const idx = rBin * bins.azimuthal * bins.polar + thetaBin * bins.polar + phiBin;
    histogram[idx]++;
  }

  // Normalize
  const sum = histogram.reduce((a, b) => a + b, 0);
  return histogram.map(v => v / sum);
}
```

---

## 4. Anomaly Detection

### Isolation Forest
```javascript
class IsolationTree {
  constructor(points, maxDepth) {
    this.root = this.buildTree(points, 0, maxDepth);
  }

  buildTree(points, depth, maxDepth) {
    if (depth >= maxDepth || points.length <= 1) {
      return { size: points.length };
    }

    // Random split
    const axis = ['x', 'y', 'z'][Math.floor(Math.random() * 3)];
    const values = points.map(p => p[axis]);
    const min = Math.min(...values);
    const max = Math.max(...values);
    const splitValue = min + Math.random() * (max - min);

    const left = points.filter(p => p[axis] < splitValue);
    const right = points.filter(p => p[axis] >= splitValue);

    return {
      axis,
      splitValue,
      left: this.buildTree(left, depth + 1, maxDepth),
      right: this.buildTree(right, depth + 1, maxDepth)
    };
  }

  pathLength(point, node = this.root, depth = 0) {
    if (node.size !== undefined) {
      return depth + this.c(node.size);
    }

    if (point[node.axis] < node.splitValue) {
      return this.pathLength(point, node.left, depth + 1);
    } else {
      return this.pathLength(point, node.right, depth + 1);
    }
  }

  c(n) {
    // Average path length of unsuccessful search in BST
    if (n <= 1) return 0;
    return 2 * (Math.log(n - 1) + 0.5772156649) - 2 * (n - 1) / n;
  }
}

function isolationForest(points, numTrees = 100, sampleSize = 256) {
  const trees = [];
  const maxDepth = Math.ceil(Math.log2(sampleSize));

  for (let i = 0; i < numTrees; i++) {
    const sample = points.sort(() => Math.random() - 0.5).slice(0, sampleSize);
    trees.push(new IsolationTree(sample, maxDepth));
  }

  // Score each point
  const avgPathLength = trees[0].c(sampleSize);
  return points.map(point => {
    const avgPath = trees.reduce((sum, tree) => sum + tree.pathLength(point), 0) / numTrees;
    return Math.pow(2, -avgPath / avgPathLength); // Anomaly score
  });
}
```

### Statistical Outlier Detection
```javascript
function statisticalOutlierRemoval(points, k = 10, stdRatio = 1.0) {
  const distances = points.map(point => {
    const neighbors = getKNearestNeighbors(points, point, k);
    return neighbors.reduce((sum, n) => sum + distance(point, n), 0) / k;
  });

  const mean = distances.reduce((a, b) => a + b, 0) / distances.length;
  const std = Math.sqrt(
    distances.reduce((sum, d) => sum + Math.pow(d - mean, 2), 0) / distances.length
  );

  const threshold = mean + stdRatio * std;

  return {
    inliers: points.filter((_, i) => distances[i] <= threshold),
    outliers: points.filter((_, i) => distances[i] > threshold)
  };
}
```

---

## 5. Force-Directed Graph Layout

### Barnes-Hut Force Layout (O(n log n))
```javascript
class QuadTree {
  constructor(bounds, capacity = 4) {
    this.bounds = bounds;
    this.capacity = capacity;
    this.points = [];
    this.divided = false;
    this.mass = 0;
    this.centerOfMass = { x: 0, y: 0 };
  }

  insert(point) {
    if (!this.contains(point)) return false;

    this.mass++;
    this.centerOfMass.x += (point.x - this.centerOfMass.x) / this.mass;
    this.centerOfMass.y += (point.y - this.centerOfMass.y) / this.mass;

    if (this.points.length < this.capacity && !this.divided) {
      this.points.push(point);
      return true;
    }

    if (!this.divided) this.subdivide();

    return this.nw.insert(point) || this.ne.insert(point) ||
           this.sw.insert(point) || this.se.insert(point);
  }

  calculateForce(node, theta = 0.5) {
    if (this.mass === 0 || this === node) return { fx: 0, fy: 0 };

    const dx = this.centerOfMass.x - node.x;
    const dy = this.centerOfMass.y - node.y;
    const d = Math.sqrt(dx * dx + dy * dy) + 0.01;
    const s = this.bounds.width;

    if (!this.divided || s / d < theta) {
      // Treat as single body
      const force = -this.mass / (d * d);
      return { fx: force * dx / d, fy: force * dy / d };
    }

    // Recurse
    const forces = [this.nw, this.ne, this.sw, this.se].map(child =>
      child.calculateForce(node, theta)
    );

    return {
      fx: forces.reduce((sum, f) => sum + f.fx, 0),
      fy: forces.reduce((sum, f) => sum + f.fy, 0)
    };
  }
}

function forceDirectedLayout(nodes, edges, iterations = 100) {
  const k = Math.sqrt((1000 * 1000) / nodes.length); // Optimal distance

  for (let iter = 0; iter < iterations; iter++) {
    const tree = new QuadTree({ x: 0, y: 0, width: 2000, height: 2000 });
    nodes.forEach(n => tree.insert(n));

    // Repulsive forces (Barnes-Hut)
    nodes.forEach(node => {
      const force = tree.calculateForce(node);
      node.vx = (node.vx || 0) + force.fx * k;
      node.vy = (node.vy || 0) + force.fy * k;
    });

    // Attractive forces (springs)
    edges.forEach(({ source, target }) => {
      const dx = target.x - source.x;
      const dy = target.y - source.y;
      const d = Math.sqrt(dx * dx + dy * dy) + 0.01;
      const force = d / k;

      source.vx += dx / d * force;
      source.vy += dy / d * force;
      target.vx -= dx / d * force;
      target.vy -= dy / d * force;
    });

    // Update positions with damping
    const damping = 0.9;
    nodes.forEach(node => {
      node.x += node.vx * damping;
      node.y += node.vy * damping;
      node.vx *= damping;
      node.vy *= damping;
    });
  }

  return nodes;
}
```

---

## 6. Spatial Data Structures

### Octree
```javascript
class Octree {
  constructor(bounds, maxDepth = 8, maxObjects = 8) {
    this.bounds = bounds;
    this.maxDepth = maxDepth;
    this.maxObjects = maxObjects;
    this.objects = [];
    this.children = null;
    this.depth = 0;
  }

  insert(object) {
    if (!this.containsPoint(object.position)) return false;

    if (this.children === null) {
      if (this.objects.length < this.maxObjects || this.depth >= this.maxDepth) {
        this.objects.push(object);
        return true;
      }
      this.subdivide();
    }

    for (const child of this.children) {
      if (child.insert(object)) return true;
    }

    this.objects.push(object);
    return true;
  }

  query(range, found = []) {
    if (!this.intersects(range)) return found;

    for (const obj of this.objects) {
      if (this.containsPoint(obj.position, range)) {
        found.push(obj);
      }
    }

    if (this.children) {
      for (const child of this.children) {
        child.query(range, found);
      }
    }

    return found;
  }

  subdivide() {
    const { x, y, z, size } = this.bounds;
    const half = size / 2;
    const quarter = size / 4;

    this.children = [
      new Octree({ x: x - quarter, y: y - quarter, z: z - quarter, size: half }),
      new Octree({ x: x + quarter, y: y - quarter, z: z - quarter, size: half }),
      new Octree({ x: x - quarter, y: y + quarter, z: z - quarter, size: half }),
      new Octree({ x: x + quarter, y: y + quarter, z: z - quarter, size: half }),
      new Octree({ x: x - quarter, y: y - quarter, z: z + quarter, size: half }),
      new Octree({ x: x + quarter, y: y - quarter, z: z + quarter, size: half }),
      new Octree({ x: x - quarter, y: y + quarter, z: z + quarter, size: half }),
      new Octree({ x: x + quarter, y: y + quarter, z: z + quarter, size: half }),
    ];

    this.children.forEach(c => c.depth = this.depth + 1);
  }
}
```

### KD-Tree
```javascript
class KDTree {
  constructor(points, depth = 0) {
    if (points.length === 0) {
      this.point = null;
      return;
    }

    const axis = depth % 3; // x=0, y=1, z=2
    const axisName = ['x', 'y', 'z'][axis];

    points.sort((a, b) => a[axisName] - b[axisName]);
    const median = Math.floor(points.length / 2);

    this.point = points[median];
    this.axis = axis;
    this.left = new KDTree(points.slice(0, median), depth + 1);
    this.right = new KDTree(points.slice(median + 1), depth + 1);
  }

  nearestNeighbor(target, best = null, bestDist = Infinity) {
    if (this.point === null) return { point: best, distance: bestDist };

    const dist = distance(this.point, target);
    if (dist < bestDist) {
      best = this.point;
      bestDist = dist;
    }

    const axisName = ['x', 'y', 'z'][this.axis];
    const diff = target[axisName] - this.point[axisName];
    const first = diff < 0 ? this.left : this.right;
    const second = diff < 0 ? this.right : this.left;

    // Search closer side first
    const result = first.nearestNeighbor(target, best, bestDist);
    best = result.point;
    bestDist = result.distance;

    // Check if we need to search other side
    if (Math.abs(diff) < bestDist) {
      const result2 = second.nearestNeighbor(target, best, bestDist);
      best = result2.point;
      bestDist = result2.distance;
    }

    return { point: best, distance: bestDist };
  }
}
```

---

## 7. Semantic Parsing for Data Vis

### Query Parser
```javascript
function parseVisualizationQuery(query) {
  const tokens = tokenize(query.toLowerCase());

  return {
    chartType: extractChartType(tokens),
    dimensions: extractDimensions(tokens),
    metrics: extractMetrics(tokens),
    filters: extractFilters(tokens),
    groupBy: extractGroupBy(tokens),
    sortBy: extractSortBy(tokens)
  };
}

const CHART_TYPES = {
  scatter: ['scatter', 'plot', 'points', 'dots'],
  bar: ['bar', 'column', 'histogram'],
  line: ['line', 'trend', 'time series'],
  pie: ['pie', 'donut', 'proportion'],
  surface: ['surface', '3d', 'terrain', 'heightmap'],
  network: ['network', 'graph', 'connection', 'relationship']
};

function extractChartType(tokens) {
  for (const [type, keywords] of Object.entries(CHART_TYPES)) {
    if (keywords.some(k => tokens.includes(k))) return type;
  }
  return 'scatter'; // default
}

function extractDimensions(tokens) {
  const dimensionKeywords = ['by', 'per', 'across', 'over'];
  const dims = [];

  for (let i = 0; i < tokens.length; i++) {
    if (dimensionKeywords.includes(tokens[i]) && tokens[i + 1]) {
      dims.push(tokens[i + 1]);
    }
  }

  return dims;
}
```

---

## Quick Reference

| Algorithm | Complexity | Use Case |
|-----------|------------|----------|
| Z-Sort | O(n log n) | Transparency ordering |
| Radix Sort | O(n) | GPU particle depth |
| K-Means | O(nki) | Data grouping |
| DBSCAN | O(n log n) | Noise-tolerant clustering |
| Isolation Forest | O(n log n) | Anomaly detection |
| Barnes-Hut | O(n log n) | Force layout |
| Octree Query | O(log n) | Spatial search |
| KD-Tree NN | O(log n) | Nearest neighbor |

---

## Related KB Files

### Visualization Resources
- `_3D_VISUALIZATION_KNOWLEDGEBASE.md` - 3D visualization patterns
- `_VISUALIZATION_RESOURCES_INDEX.md` - Visualization index
- `_ECHARTS_VISUALIZATION_PATTERNS.md` - ECharts charts
- `_COSMOS_VISUALIZATION_RESOURCES.md` - Cosmos visualizer

### Web Implementation
- `_WEBGL_INTELLIGENCE_PATTERNS.md` - WebGL/Three.js patterns
- `_WEBGPU_THREEJS_2025.md` - WebGPU patterns
- `_GAUSSIAN_SPLATTING_AND_VIZ_TOOLS.md` - Gaussian splatting

### Unity Implementation
- `_UNITY_INTELLIGENCE_PATTERNS.md` - Unity VFX patterns
- `_UNITY_PATTERNS_BY_INTEREST.md` - Particle systems

### Graph/Network
- `_REPO_GRAPH_SCHEMA.md` - Graph schema
- `_XRAI_HYPERGRAPH_WORLD_GENERATION.md` - Hypergraph generation

### Reference
- `_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` - GitHub repos
- `_KB_CROSS_LINKS.md` - Full KB navigation

---

*Updated: 2026-01-13*
