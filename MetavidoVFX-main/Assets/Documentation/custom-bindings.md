# Custom VFX Bindings

User-configured binding overrides. Edit this file to customize VFX behavior.

## Override Format

```yaml
VFXName:
  mode: AR|Audio|Keypoint|Standalone
  bindings:
    PropertyName: true|false
  custom:
    PropertyName: value
```

## User Overrides

```yaml
# Example: Force Bubbles to use AR depth instead of audio
# Fluo_Bubbles:
#   mode: AR
#   bindings:
#     DepthMap: true
#     ColorMap: true
#     StencilMap: false
#   custom:
#     Throttle: 0.8

# Add your custom bindings below:

```

## Notes

- Overrides take precedence over source-bindings.md defaults
- Use `mode: Standalone` to disable all AR bindings
- Set `bindings.PropertyName: false` to skip specific bindings
- Set `custom.PropertyName: value` for static values

---

*Edit this file to customize. Audit & Fix will respect these settings.*
