# Cosmos & Data Visualization Resources

> **Last Updated**: 2025-01-13

---

## Illustris Project

> **Source**: https://www.illustris-project.org/data/

### Overview

Large-scale cosmological simulation project providing public data for exploring the universe's structure. Described in [Nelson+ (2015)](http://arxiv.org/abs/1504.00362).

### Data Access Methods

1. **Raw Data Download**: Download data files and example scripts for local analysis
2. **Web API**: Query specific objects without downloading full datasets
3. **Combined**: Search locally, then extract specific data via API

### Data Types

| Type | Description |
|------|-------------|
| **Snapshots** | Particle data at specific simulation times |
| **Group Catalogs** | FoF (Friends-of-Friends) and Subfind halo catalogs |
| **Merger Trees** | Galaxy merger history (SubLink, LHaloTree) |
| **Supplementary** | Additional data catalogs for specific runs |

### Available Simulations

| Simulation | Box Size | Dark Matter Particles | DM Mass (M☉) | Gas Mass (M☉) | Snapshots |
|------------|----------|----------------------|--------------|---------------|-----------|
| Illustris-1 | 106.5 Mpc | 1820³ | 6.3×10⁶ | 1.3×10⁶ | 134 |
| Illustris-1-Dark | 106.5 Mpc | 1820³ | 7.5×10⁶ | 0 | 136 |
| Illustris-2 | 106.5 Mpc | 910³ | 5.0×10⁷ | 1.0×10⁷ | 136 |
| Illustris-3 | 106.5 Mpc | 455³ | 4.0×10⁸ | 8.1×10⁷ | 136 |

**Total Data**: 250 TB, 814 snapshots, 2.7 trillion particles

### Web Tools

| Tool | Purpose |
|------|---------|
| [Group Catalog Search](https://www.illustris-project.org/data/search/) | Query subhalo catalog online |
| [Browsable API](https://www.illustris-project.org/api/) | Human-navigable API |
| [The Explorer](https://www.illustris-project.org/explorer/) | Deep zoom map of Illustris-1 at z=0 |
| [Galaxy Observatory](https://www.illustris-project.org/galaxy_obs/) | Browse stellar images |

### Documentation

- [Background & Details](https://www.illustris-project.org/data/docs/background/) - Physics models, caveats
- [Data Specifications](https://www.illustris-project.org/data/docs/specifications/) - Field descriptions
- [Example Scripts](https://www.illustris-project.org/data/docs/scripts/) - Python, IDL, Matlab I/O
- [API Reference](https://www.illustris-project.org/data/docs/api/) - Web query interface

---

## CosmosVR

> **Source**: https://github.com/RealityVirtually2019/CosmosVR

### Overview

VR experience for exploring large-scale universe simulations. Created at Reality Virtually 2019 hackathon.

### Features

- Explore simulation of the Universe at largest scales
- Visualize cosmic web (filaments, clusters of dark matter)
- Switch between different telescope views
- Learn how galaxies exist within dark matter structure

### Use Case

Educational VR tool for understanding cosmological structure and how different astronomical instruments reveal different aspects of the universe.

---

## 3D-TSNE

> **Source**: https://github.com/awjuliani/3D-TSNE

### Overview

Unity3D project for visualizing t-SNE dimensionality reduction data in 3D. Enables **animated visualization** of how neural networks learn to represent datasets over training.

### Purpose

While Python can create static 3D t-SNE visualizations, this project enables:
- Dynamic visualization of learning process
- Animated transitions between training stages
- Interactive 3D exploration in Unity

### Workflow

1. Train neural network and save representations at various stages
2. Generate t-SNE embeddings as CSV files at each stage
3. Load CSV series into Unity project
4. Visualize animated learning trajectory

### Resources

- [Tutorial Post](https://medium.com/@awjuliani/visualizing-deep-learning-with-t-sne-tutorial-and-video-e7c59ee4080c) - How to generate CSV files
- [Update Post](https://medium.com/@awjuliani/make-your-own-3d-t-sne-visualizations-download-e0cdfe80d6e3) - Creating your own visualizations

### Technical Notes

- **Platform**: Unity3D
- **Input**: Series of CSV files with t-SNE coordinates
- **Output**: Animated 3D visualization of embedding space evolution

---

## Potential Applications

### For Paint-AR / XR Projects

| Resource | Application |
|----------|-------------|
| Illustris Data | AR visualization of cosmic structures |
| CosmosVR | Reference for VR cosmos visualization |
| 3D-TSNE | Visualize AI model embeddings in AR/VR |

### Combined Concept

Create AR experience that:
1. Uses Illustris data for accurate cosmic structure
2. Applies CosmosVR-style lens switching
3. Visualizes neural network learning with 3D-TSNE techniques

---

*Created for Unity-XR-AI Knowledge Base*
