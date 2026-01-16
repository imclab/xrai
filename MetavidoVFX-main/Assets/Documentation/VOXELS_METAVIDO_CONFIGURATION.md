# Voxels_metavido Configuration Summary

## Overview
**Voxels_metavido** prefab is now configured to use **ARDepthSource + VFXARBinder** for clean parameter mapping to the `voxels_depth_people_metavido.vfx` graph.

## Architecture

### Data Flow
```
ARDepthSource (main compute dispatcher)
    ↓
    Provides: DepthMap, PositionMap, ColorMap, StencilMap, RayParams, InverseView
    ↓
VFXARBinder (lightweight binder)
    ↓
    Reads from ARDepthSource.Instance
    ↓
VisualEffect (Voxels_metavido)
```

## Component Configuration

### Enabled Components
✅ **VFXPropertyBinder** - Required for VFXARBinder to bind properties
✅ **VFXARBinder** - Active binder that reads from ARDepthSource
✅ **VisualEffect** - Main voxels_depth_people_metavido graph

### Disabled Components
❌ **VFXARDataBinder** (MetavidoVFX.VFX.Binders)
  - Reason: Conflicting with VFXARBinder; kept for reference but disabled
  
❌ **Fluo.ARVfxBridge** (Fluo.ARVfxBridge)
  - Reason: Alternative AR binding approach; not used with VFXARBinder

## Parameter Mappings

VFXARBinder automatically binds the following if VFX has them defined:

### Textures
| Parameter | Source | Purpose |
|-----------|--------|---------|
| `DepthMap` | ARDepthSource.DepthMap | Raw depth from AR Foundation |
| `PositionMap` | ARDepthSource.PositionMap | Computed world positions from depth |
| `ColorMap` | ARDepthSource.ColorMap | Captured camera color |
| `StencilMap` | ARDepthSource.StencilMap | Human segmentation mask |
| `VelocityMap` | ARDepthSource.VelocityMap | Frame-to-frame motion (optional) |

### Vectors/Matrices
| Parameter | Source | Purpose |
|-----------|--------|---------|
| `RayParams` | ARDepthSource.RayParams | Camera ray projection parameters |
| `InverseView` | ARDepthSource.InverseView | Camera-to-world transform |

## Execution Order

**DefaultExecutionOrder = -100** (ARDepthSource)
- Runs FIRST before other systems
- Computes PositionMap and VelocityMap every frame
- Captures ColorMap via RenderPipelineManager

**LateUpdate** (VFXARBinder)
- Reads from ARDepthSource.Instance
- Binds all available textures to VFX
- Runs AFTER ARDepthSource completes

## Performance Characteristics

- **Single Compute Dispatch**: ARDepthSource does ONE compute shader dispatch for ALL VFX
- **No Redundant Blits**: ColorMap uses ARCameraTextureProvider for efficient capture
- **Lightweight Binding**: VFXARBinder only does SetTexture/SetVector calls (no compute)

## Verification Checklist

To verify the configuration is working:

1. **In Editor (AR Foundation Remote)**:
   - Ensure device is connected and streaming
   - Check Unity logs for: `ARDepthSource` compute times
   - Verify VFXARBinder.IsBound is true

2. **On Real Device**:
   - Run with AR session enabled
   - Voxels should render with depth-based effects
   - Performance should be optimal (single dispatch for all VFX)

3. **Debug Script** (Run in Inspector):
   - Right-click any GameObject → **Debug Source** (ARDepthSource)
   - Right-click Voxels_metavido → **Debug Binder** (VFXARBinder)

## Troubleshooting

### Issue: depth=False in logs
- **Cause**: AR Foundation not providing depth textures
- **Solution**: 
  - Check ARSession has environment depth enabled
  - On device: Ensure app has camera permissions
  - In editor: Verify AR Foundation Remote device connection

### Issue: VFXARDataBinder still running
- **Cause**: Component cache not refreshed
- **Solution**: 
  - Close/reopen scene
  - Or: Manually disable in Inspector → apply to prefab

### Issue: Missing textures in VFX
- **Cause**: VFX graph parameter names don't match
- **Solution**:
  - VFXARBinder checks VFX.HasTexture() before binding
  - Ensure VFX parameter names match: DepthMap, PositionMap, ColorMap, etc.

## References

- **ARDepthSource**: `Assets/Scripts/Bridges/ARDepthSource.cs`
- **VFXARBinder**: `Assets/Scripts/Bridges/VFXARBinder.cs`
- **Configuration Script**: `Assets/Scripts/Editor/ConfigureVoxelsMetavido.cs`
- **VFX Graph**: `Assets/VFX/Metavido/voxels_depth_people_metavido.vfx`
