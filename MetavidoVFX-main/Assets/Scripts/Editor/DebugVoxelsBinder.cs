using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Debug script to verify Voxels_metavido bindings
/// </summary>
public class DebugVoxelsBinder : MonoBehaviour
{
    public static void DebugVoxelsBinding()
    {
        var voxels = GameObject.Find("Voxels_metavido");
        if (voxels == null)
        {
            Debug.LogError("[DebugVoxelsBinder] Voxels_metavido not found!");
            return;
        }

        var vfx = voxels.GetComponent<VisualEffect>();
        if (vfx == null)
        {
            Debug.LogError("[DebugVoxelsBinder] VisualEffect not found on Voxels_metavido!");
            return;
        }

        var arBinder = voxels.GetComponent<VFXARBinder>();
        var arDepthSource = ARDepthSource.Instance;

        Debug.Log("=== Voxels_metavido Binding Status ===");
        Debug.Log($"VFXARBinder enabled: {arBinder?.enabled ?? false}");
        Debug.Log($"VFXARBinder is bound: {arBinder?.IsBound ?? false}");
        Debug.Log($"ARDepthSource available: {arDepthSource != null}");

        if (arDepthSource != null)
        {
            Debug.Log($"  DepthMap: {arDepthSource.DepthMap} ({arDepthSource.DepthMap?.width}x{arDepthSource.DepthMap?.height})");
            Debug.Log($"  PositionMap: {arDepthSource.PositionMap} ({arDepthSource.PositionMap?.width}x{arDepthSource.PositionMap?.height})");
            Debug.Log($"  StencilMap: {arDepthSource.StencilMap}");
            Debug.Log($"  ColorMap: {arDepthSource.ColorMap} ({arDepthSource.ColorMap?.width}x{arDepthSource.ColorMap?.height})");
            Debug.Log($"  VelocityMap: {arDepthSource.VelocityMap}");
            Debug.Log($"  RayParams: {arDepthSource.RayParams}");
            Debug.Log($"  IsReady: {arDepthSource.IsReady}");
        }

        Debug.Log("=== VFX Properties ===");
        Debug.Log($"Has DepthMap: {vfx.HasTexture(Shader.PropertyToID("DepthMap"))}");
        Debug.Log($"Has PositionMap: {vfx.HasTexture(Shader.PropertyToID("PositionMap"))}");
        Debug.Log($"Has ColorMap: {vfx.HasTexture(Shader.PropertyToID("ColorMap"))}");
        Debug.Log($"Has StencilMap: {vfx.HasTexture(Shader.PropertyToID("StencilMap"))}");
        Debug.Log($"Has VelocityMap: {vfx.HasTexture(Shader.PropertyToID("VelocityMap"))}");
        Debug.Log($"Has RayParams: {vfx.HasVector4(Shader.PropertyToID("RayParams"))}");
        Debug.Log($"Has InverseView: {vfx.HasMatrix4x4(Shader.PropertyToID("InverseView"))}");

        Debug.Log("=== Binder Components ===");
        Debug.Log($"VFXARBinder: {arBinder != null}");
        Debug.Log($"VFXPropertyBinder: {voxels.GetComponent<UnityEngine.VFX.Utility.VFXPropertyBinder>()?.enabled}");
        var vfxARDataBinder = voxels.GetComponent<MetavidoVFX.VFX.Binders.VFXARDataBinder>();
        Debug.Log($"VFXARDataBinder enabled: {vfxARDataBinder?.enabled}");
        var fluoBridge = voxels.GetComponent<Fluo.ARVfxBridge>();
        Debug.Log($"Fluo.ARVfxBridge enabled: {fluoBridge?.enabled}");
    }
}
