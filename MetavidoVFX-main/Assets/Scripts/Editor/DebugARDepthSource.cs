using UnityEngine;

public class DebugARDepthSource : MonoBehaviour
{
    public static void DebugSource()
    {
        var source = ARDepthSource.Instance;
        if (source == null)
        {
            Debug.LogError("[DebugARDepthSource] ARDepthSource.Instance is NULL!");
            return;
        }

        Debug.Log("=== ARDepthSource Status ===");
        Debug.Log($"DepthMap: {source.DepthMap}");
        Debug.Log($"  Width x Height: {source.DepthMap?.width}x{source.DepthMap?.height}");
        Debug.Log($"PositionMap: {source.PositionMap}");
        Debug.Log($"  Width x Height: {source.PositionMap?.width}x{source.PositionMap?.height}");
        Debug.Log($"StencilMap: {source.StencilMap}");
        Debug.Log($"  Width x Height: {source.StencilMap?.width}x{source.StencilMap?.height}");
        Debug.Log($"ColorMap: {source.ColorMap}");
        Debug.Log($"  Width x Height: {source.ColorMap?.width}x{source.ColorMap?.height}");
        Debug.Log($"VelocityMap: {source.VelocityMap}");
        Debug.Log($"  Width x Height: {source.VelocityMap?.width}x{source.VelocityMap?.height}");
        Debug.Log($"RayParams: {source.RayParams}");
        Debug.Log($"InverseView: {source.InverseView}");
        Debug.Log($"IsReady: {source.IsReady}");
        Debug.Log($"LastComputeTimeMs: {source.LastComputeTimeMs}");

        // Try to manually debug source
        source.SendMessage("DebugSource", SendMessageOptions.DontRequireReceiver);
    }
}
