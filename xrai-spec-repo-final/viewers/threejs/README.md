# XRAI Three.js Viewer

This folder contains a minimal Three.js-based viewer for rendering `.xrai.json` scenes.

## Features

- Loads geometry (e.g. mesh, splats) from XRAI JSON
- Placeholder for agentic AI logic
- Support for procedural audio (WIP)

## To Run

```bash
npm install
npm run dev
```

## TODO
- Parse `geometry.ref` and dynamically load GLB/PLY/etc.
- Connect `aiAgent.prompt` to local or cloud AI service
- Trigger spatial audio via WebAudio API