#ifndef _AR_GLOBALS_H_
#define _AR_GLOBALS_H_

// VECTORS work globally (set by ARDepthSource.cs)
float4 _ARRayParams;      // (0, 0, tan(fov/2)*aspect, tan(fov/2))
float4x4 _ARInverseView;
float4 _AudioBands;       // (bass, lowmid, highmid, treble) from AudioBridge

// NOTE: Textures do NOT work globally for VFX Graph!
// Use explicit vfx.SetTexture() via VFXARBinder instead.

#endif
