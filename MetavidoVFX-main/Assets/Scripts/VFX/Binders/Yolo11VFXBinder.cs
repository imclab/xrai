using UnityEngine;
using UnityEngine.VFX;
using MyakuMyakuAR;

namespace XRRAI
{
    /// <summary>
    /// Binds YOLO11 segmentation output to VFX Graph.
    /// Alternative to MyakuMyakuBinder for object detection instead of human body.
    /// </summary>
    [RequireComponent(typeof(VisualEffect))]
    public class Yolo11VFXBinder : MonoBehaviour
    {
        [Header("YOLO11 Source")]
        [SerializeField] Yolo11SegARController yoloController;

        [Header("Spawn Settings")]
        [SerializeField] Vector4 spawnUvMinMax = new(0.2f, 0.2f, 0.8f, 0.8f);
        [SerializeField] float spawnRate = 0.5f;

        VisualEffect vfx;

        // Property IDs matching MyakuMyaku VFX
        static readonly int _SegmentationTex = Shader.PropertyToID("_SegmentationTex");
        static readonly int _ARRgbDTex = Shader.PropertyToID("_ARRgbDTex");
        static readonly int _SpawnUvMinMax = Shader.PropertyToID("_SpawnUvMinMax");
        static readonly int _SpawnRate = Shader.PropertyToID("_SpawnRate");

        void Awake()
        {
            vfx = GetComponent<VisualEffect>();
        }

        void Start()
        {
            if (yoloController == null)
                yoloController = FindAnyObjectByType<Yolo11SegARController>();

            if (yoloController != null)
                yoloController.OnDetect += OnYoloDetect;
        }

        void OnDestroy()
        {
            if (yoloController != null)
                yoloController.OnDetect -= OnYoloDetect;
        }

        void OnYoloDetect(Yolo11SegARController controller)
        {
            if (vfx == null) return;

            // Bind YOLO segmentation output
            var segTex = controller.SegmentationTexture;
            var colorTex = controller.ARCameraTexture;

            if (segTex != null)
                vfx.SetTexture(_SegmentationTex, segTex);

            if (colorTex != null)
                vfx.SetTexture(_ARRgbDTex, colorTex);

            // Update spawn rate based on detection count
            var detections = controller.Detections;
            spawnRate = detections.Length > 0 ? 0.5f : 0f;

            vfx.SetVector4(_SpawnUvMinMax, spawnUvMinMax);
            vfx.SetFloat(_SpawnRate, spawnRate);
        }

        public void SetSpawnBounds(Rect bounds)
        {
            spawnUvMinMax = new Vector4(bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax);
        }

        public void SetSpawnRate(float rate) => spawnRate = Mathf.Clamp01(rate);
    }
}
