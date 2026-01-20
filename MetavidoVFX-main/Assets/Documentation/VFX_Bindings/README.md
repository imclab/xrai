# VFX Binding Documentation

This folder contains auto-generated binding documentation for all VFX assets.

## Generation

Run in Unity Editor:
- `H3M > VFX Pipeline Master > Binding Docs > Generate All Binding Docs`
- Or: `H3M > VFX Pipeline Master > Binding Docs > Quick Generate (Console)`

## Files Generated

### Per-VFX Documentation
- `{vfxname}-source-bindings.md` - Maps VFX properties to pipeline sources
- `{vfxname}-custom-bindings.md` - Lists properties needing custom implementation

### Master Documents
- `_MASTER_VFX_BINDINGS.md` - Aggregated bindings for all 223 VFX
- `_VFX_ORIGINAL_NAMES_REGISTRY.md` - Tracks original names before any renames

## Property Mappings

| VFX Property | Source | Notes |
|--------------|--------|-------|
| DepthMap | ARDepthSource | Raw AR depth |
| StencilMap | ARDepthSource | Human segmentation |
| PositionMap | ARDepthSource | GPU-computed world XYZ |
| ColorMap | ARDepthSource | Camera RGB |
| KeypointBuffer | NNCamKeypointBinder | 17 pose landmarks |
| AudioVolume | AudioBridge | 0-1 volume |
| AudioBands | AudioBridge | Vector4 frequency bands |

## Integration

The VFXBindingDocGenerator integrates with:
1. **VFXLibraryManager** - VFX lifecycle management
2. **VFXCompatibilityAuditor** - Mode compatibility analysis
3. **VFXARBinder** - Automatic property binding
