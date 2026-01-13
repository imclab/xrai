# HOLOVIS - Unity Project Visualizer

A dynamic 3D visualization tool for Unity codebases that provides holistic views of your project structure, dependencies, and relationships.

## Features

- **Real-time Analysis**: Crawls Unity project directories and analyzes file types, scripts, systems, packages, and dependencies
- **Multiple Visualization Modes**:
  - **Tree Graph**: Nested cube visualization showing project hierarchy
  - **Node Graph**: Connected packages and systems fanning out from center
  - **Flow Chart**: Timeline view of scenes and their associated scripts

- **Unity-Specific Analysis**:
  - Detects Unity version and project settings
  - Identifies MonoBehaviours and ScriptableObjects
  - Tracks Unity and third-party packages
  - Maps asset relationships (scenes, prefabs, materials, shaders)

## Quick Start

1. Install dependencies:
   ```bash
   cd HOLOVIS
   npm install
   ```

2. Start the server:
   ```bash
   npm run serve
   ```

3. Open http://localhost:3000 in your browser

4. Enter the path to your Unity project and click "Analyze Project"

## Usage

### Analyzing Local Projects
Enter the full path to your Unity project folder (the one containing the Assets folder).

### Navigation
- **Mouse**: Rotate view by dragging
- **Scroll**: Zoom in/out
- **Click**: Select objects to view details

### Visualization Modes
- **Tree Graph**: Best for understanding project structure and asset organization
- **Node Graph**: Shows package dependencies and relationships
- **Flow Chart**: Visualizes scene flow and script associations

## Example Projects
The tool has been tested with:
- NNCam2 - Neural network camera system
- SplatVFX - Gaussian splat visual effects

## Architecture

- **Analyzer**: Scans Unity projects and extracts metadata
- **Visualizer**: Generates 3D representations using Three.js
- **Server**: Express/WebSocket backend for real-time updates
- **Frontend**: Interactive 3D canvas with controls

## Future Enhancements
- GitHub/Plastic SCM integration
- Asset bundle visualization
- Network service mapping
- Performance profiling overlays
- VR/AR viewing modes