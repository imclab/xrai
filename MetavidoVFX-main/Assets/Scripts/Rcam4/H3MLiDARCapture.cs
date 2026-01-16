using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;
using Metavido;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Rcam4
{
    [RequireComponent(typeof(Camera))]
    public class H3MLiDARCapture : MonoBehaviour
    {
        [Header("Data Sources")]
        [SerializeField] AROcclusionManager _occlusionManager;
        [SerializeField] ARCameraTextureProvider _colorProvider;

        [Header("Target")]
        [SerializeField] VisualEffect _vfx;

        [Header("Settings")]
        [SerializeField] float _depthRangeMin = 0.1f;
        [SerializeField] float _depthRangeMax = 5.0f;

        Camera _camera;
        string _logPath;
        static Queue<string> _logQueue = new Queue<string>();
        const int MAX_LOG_LINES = 20;

        // Native iOS logging
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void _NSLog(string message);
        static void NSLog(string msg) => _NSLog(msg);
#else
        static void NSLog(string msg) => Debug.Log("[NSLog] " + msg);
#endif

#pragma warning disable 0618 // Disable obsolete warnings for environmentDepthTexture

        // Multi-channel logging: Unity Console + NSLog + File + On-screen
        void Log(string message)
        {
            string timestamped = $"[{Time.frameCount}] {message}";

            // 1. Unity Console (Silenced to reduce spam)
            // Debug.Log(timestamped);

            // 2. Native iOS log (Silenced to reduce spam)
            // NSLog(timestamped);

            // 3. File log
            try
            {
                if (!string.IsNullOrEmpty(_logPath))
                    File.AppendAllText(_logPath, timestamped + "\n");
            }
            catch { }

            // 4. On-screen queue
            _logQueue.Enqueue(timestamped);
            while (_logQueue.Count > MAX_LOG_LINES)
                _logQueue.Dequeue();
        }

        void Update()
        {
            if (_vfx == null) return;

            // 1. Update Color Map
            if (_colorProvider != null && _colorProvider.Texture != null)
            {
                _vfx.SetTexture("ColorMap", _colorProvider.Texture);
            }

            // 2. Update Depth Map
            try
            {
                if (_occlusionManager != null && _occlusionManager.environmentDepthTexture != null)
                {
                    _vfx.SetTexture("DepthMap", _occlusionManager.environmentDepthTexture);
                    _vfx.SetBool("Spawn", true);  // Enable particle spawning when depth is available
                }
            }
            catch { }

            // 3. Update Camera Matrices & Params
            if (_camera != null)
            {
                // Inverse View Matrix (World -> View -> Inverse = View -> World)
                // We actually want the matrix that transforms from View Space (where depth is) to World Space.
                // Camera.cameraToWorldMatrix is exactly that.
                _vfx.SetMatrix4x4("InverseView", _camera.cameraToWorldMatrix);

                // Ray Parameters for reconstructing position from depth
                // FIXED: Metavido HLSL uses p.xy = (p.xy + RayParams.xy) * RayParams.zw
                // So format is (offsetX, offsetY, widthScale, heightScale) per RenderUtils.cs
                var vfov = 2.0f * Mathf.Atan(1.0f / _camera.projectionMatrix[1, 1]);
                var aspect = _camera.projectionMatrix[1, 1] / _camera.projectionMatrix[0, 0];
                var h = Mathf.Tan(vfov * 0.5f);
                var rayParams = new Vector4(0, 0, h * aspect, h);  // (offset_xy=0, scale_zw=tan*aspect,tan)
                _vfx.SetVector4("RayParams", rayParams);
            }

            // 4. Update Settings
            _vfx.SetVector2("DepthRange", new Vector2(_depthRangeMin, _depthRangeMax));
        }

    void OnGUI()
        {
            GUI.skin.label.fontSize = 32;
            GUILayout.BeginArea(new Rect(30, 50, Screen.width - 60, Screen.height - 100));

            // Header with key status
            int particles = _vfx != null ? _vfx.aliveParticleCount : -1;
            bool hasDepth = _occlusionManager?.environmentDepthTexture != null;
            GUILayout.Label($"=== H3M [{(hasDepth ? "DEPTH OK" : "NO DEPTH")}] Particles: {particles} ===");

            // Compact status line
            string depthSize = hasDepth ? $"{_occlusionManager.environmentDepthTexture.width}x{_occlusionManager.environmentDepthTexture.height}" : "NULL";
            string colorSize = _colorProvider?.Texture != null ? $"{_colorProvider.Texture.width}x{_colorProvider.Texture.height}" : "NULL";
            GUILayout.Label($"Depth: {depthSize} | Color: {colorSize}");

            // VFX parameters
            if (_vfx != null)
            {
                bool spawn = _vfx.HasBool("Spawn") && _vfx.GetBool("Spawn");
                GUILayout.Label($"Spawn: {spawn} | HasDepthMap: {_vfx.HasTexture("DepthMap")} | HasColorMap: {_vfx.HasTexture("ColorMap")}");
            }

            GUILayout.Space(10);
            GUILayout.Label("--- Log ---");

            // Show log queue
            foreach (var line in _logQueue)
                GUILayout.Label(line);

            GUILayout.EndArea();
        }

        System.Collections.IEnumerator Start()
        {
            _camera = GetComponent<Camera>();

            // Initialize file logging
            _logPath = Path.Combine(Application.persistentDataPath, "h3m_debug.log");
            try { File.WriteAllText(_logPath, $"=== H3M LiDAR Debug Log Started {System.DateTime.Now} ===\n"); }
            catch { }

            Log($"H3M Start - LogPath: {_logPath}");

            if (_occlusionManager == null)
                _occlusionManager = FindObjectOfType<AROcclusionManager>();

            if (_colorProvider == null)
                _colorProvider = GetComponent<ARCameraTextureProvider>();

            Log($"Init - Occlusion: {_occlusionManager != null}, Color: {_colorProvider != null}, VFX: {_vfx != null}");

            while (true)
            {
                yield return new WaitForSeconds(2f);
                LogStatus();
            }
        }

        void LogStatus()
        {
            string colorInfo = _colorProvider == null ? "NULL" :
                (_colorProvider.Texture != null ? $"{_colorProvider.Texture.width}x{_colorProvider.Texture.height}" : "NoTex");

            string depthInfo = "NULL";
            if (_occlusionManager != null)
            {
                var envDepth = _occlusionManager.environmentDepthTexture;
                var humanDepth = _occlusionManager.humanDepthTexture;
                depthInfo = envDepth != null ? $"Env:{envDepth.width}x{envDepth.height}" :
                           (humanDepth != null ? $"Human:{humanDepth.width}x{humanDepth.height}" : "NoDepth");
            }

            int particles = _vfx != null ? _vfx.aliveParticleCount : -1;
            bool spawn = _vfx != null && _vfx.HasBool("Spawn") ? _vfx.GetBool("Spawn") : false;

            Log($"STATUS: Color={colorInfo} Depth={depthInfo} Particles={particles} Spawn={spawn}");
        }
#pragma warning restore 0618
    }
}
