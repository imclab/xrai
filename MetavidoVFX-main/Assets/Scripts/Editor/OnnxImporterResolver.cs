using UnityEditor;

namespace XRRAI.Editor
{
    /// <summary>
    /// Resolves ONNX importer conflict between Unity Sentis and ONNX Runtime.
    /// Both packages want to import .onnx files - we give priority to Sentis.
    /// ONNX Runtime models should use .bytes extension instead.
    /// DISABLED (2026-01-22): Only printed a log message on every reload - unnecessary.
    /// </summary>
    // [InitializeOnLoad] - DISABLED: Only printed log, no actual functionality
    public static class OnnxImporterResolver
    {
        static OnnxImporterResolver()
        {
            // Log guidance for the conflict
            UnityEngine.Debug.Log(
                "[OnnxImporterResolver] ONNX file handling:\n" +
                "- Unity Sentis (.onnx) - BodyPix, Sentis models\n" +
                "- ONNX Runtime (.bytes) - YOLO11, external models\n" +
                "For ONNX Runtime: rename .onnx to .bytes or download at runtime"
            );
        }
    }
}
