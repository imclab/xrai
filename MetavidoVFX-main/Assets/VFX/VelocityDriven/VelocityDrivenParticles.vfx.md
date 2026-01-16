# Velocity-Driven VFX Graph Setup

This document describes how to configure a VFX Graph to use velocity-driven particles from AR depth data.

## Required Exposed Properties

Add these properties to the VFX Graph Blackboard:

| Property Name | Type | Description |
|---------------|------|-------------|
| `Position Map` | Texture2D | World positions from depth (ARGBFloat) |
| `Velocity Map` | Texture2D | Per-pixel velocity vectors (ARGBFloat, xyz=velocity, w=speed) |
| `Color Map` | Texture2D | Camera RGB texture |
| `Stencil Map` | Texture2D | Human body mask |

## VFX Graph Structure

### Initialize Particle
1. **Set Position from Map**
   - Sample `Position Map` at random UV
   - Use `Sample Texture2D` → Position attribute

2. **Set Velocity from Map**
   - Sample `Velocity Map` at same UV
   - Use `Sample Texture2D` → Velocity attribute
   - Multiply by velocity scale (0.5-2.0 recommended)

3. **Set Color from Map**
   - Sample `Color Map` at same UV
   - Apply to Color attribute

### Update Particle
1. **Add Velocity**
   - Apply velocity to position each frame

2. **Optional: Turbulence**
   - Add noise to velocity for organic motion

3. **Optional: Gravity**
   - Add downward force for grounding effect

### Output
- Use Output Particle Quad or Point
- Enable alpha blending
- Size based on velocity (faster = smaller trail)

## Integration with PeopleOcclusionVFXManager

The manager automatically binds:
```csharp
m_VfxInstance.SetTexture("Position Map", m_PositionTexture);
m_VfxInstance.SetTexture("Velocity Map", m_VelocityTexture);
m_VfxInstance.SetTexture("Color Map", m_CaptureTexture);
m_VfxInstance.SetTexture("Stencil Map", stencilTexture);
```

## Performance Notes

- Velocity texture is computed per-frame via compute shader
- Frame-to-frame position delta / deltaTime
- Clamped to ±10 m/s to prevent explosion artifacts
- Invalid pixels (no depth) have zero velocity
