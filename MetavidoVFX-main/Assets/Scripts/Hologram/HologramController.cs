using UnityEngine;
using UnityEngine.Video;
using UnityEngine.VFX;
using Metavido.Common;
using Metavido.Decoder;

/// <summary>
/// Unified hologram controller that works with both:
/// 1. Live AR (from ARDepthSource)
/// 2. Metavido recorded video playback
///
/// Uses the same VFXARBinder for VFX data binding in both modes.
/// </summary>
public class HologramController : MonoBehaviour
{
    public enum SourceMode
    {
        LiveAR,         // Use ARDepthSource (real-time AR camera)
        MetavidoVideo   // Use Metavido video file playback
    }

    [Header("Mode")]
    [SerializeField] SourceMode _mode = SourceMode.LiveAR;

    [Header("Hologram Transform")]
    [Tooltip("Transform used as anchor point for hologram placement")]
    [SerializeField] Transform _anchor;
    [Tooltip("Scale factor (1.0 = life-size, 0.15 = mini-me)")]
    [Range(0.01f, 2f)]
    [SerializeField] float _scale = 0.15f;

    [Header("VFX")]
    [Tooltip("The hologram VFX to control")]
    [SerializeField] VisualEffect _vfx;
    [Tooltip("VFXARBinder component (auto-found if null)")]
    [SerializeField] VFXARBinder _binder;

    [Header("Metavido Playback (only for MetavidoVideo mode)")]
    [Tooltip("Video player for .metavido files")]
    [SerializeField] VideoPlayer _videoPlayer;
    [Tooltip("Metavido texture demuxer")]
    [SerializeField] TextureDemuxer _demuxer;
    [Tooltip("Metavido metadata decoder")]
    [SerializeField] MetadataDecoder _metadataDecoder;

    [Header("Debug")]
    [SerializeField] bool _showDebugGUI = false;

    // Public accessors
    public SourceMode Mode { get => _mode; set => SetMode(value); }
    public Transform Anchor { get => _anchor; set => _anchor = value; }
    public float Scale { get => _scale; set => _scale = Mathf.Clamp(value, 0.01f, 2f); }
    public VisualEffect VFX => _vfx;
    public bool IsPlaying => _mode == SourceMode.LiveAR ?
        (ARDepthSource.Instance?.IsReady ?? false) :
        (_videoPlayer != null && _videoPlayer.isPlaying);

    void Awake()
    {
        // Auto-find components
        if (_vfx == null) _vfx = GetComponentInChildren<VisualEffect>();
        if (_binder == null && _vfx != null) _binder = _vfx.GetComponent<VFXARBinder>();
        if (_anchor == null) _anchor = transform;
    }

    void Start()
    {
        SetMode(_mode);
    }

    void LateUpdate()
    {
        // Update hologram transform
        if (_vfx != null && _anchor != null)
        {
            _vfx.transform.position = _anchor.position;
            _vfx.transform.rotation = _anchor.rotation;
            _vfx.transform.localScale = Vector3.one * _scale;
        }

        // Handle Metavido playback
        if (_mode == SourceMode.MetavidoVideo)
        {
            UpdateMetavidoPlayback();
        }
    }

    /// <summary>
    /// Switch between Live AR and Metavido playback modes
    /// </summary>
    public void SetMode(SourceMode mode)
    {
        _mode = mode;

        if (_binder != null)
        {
            if (mode == SourceMode.LiveAR)
            {
                // Use ARDepthSource singleton
                _binder.enabled = true;
                // Disable transform mode since we handle it here
                _binder.UseTransformMode = false;
                _binder.BindAnchorPos = false;
                _binder.BindHologramScale = false;
            }
            else
            {
                // Metavido mode - we'll bind textures directly
                _binder.enabled = false;
            }
        }

        Debug.Log($"[HologramController] Mode set to {mode}");
    }

    /// <summary>
    /// Play a Metavido video file
    /// </summary>
    public void PlayVideo(string videoPath)
    {
        if (_videoPlayer == null)
        {
            Debug.LogError("[HologramController] No VideoPlayer assigned for Metavido playback");
            return;
        }

        _mode = SourceMode.MetavidoVideo;
        SetMode(_mode);

        _videoPlayer.url = videoPath;
        _videoPlayer.Play();
    }

    /// <summary>
    /// Stop video playback and optionally switch to Live AR
    /// </summary>
    public void StopVideo(bool switchToLiveAR = true)
    {
        if (_videoPlayer != null)
            _videoPlayer.Stop();

        if (switchToLiveAR)
            SetMode(SourceMode.LiveAR);
    }

    void UpdateMetavidoPlayback()
    {
        if (_videoPlayer == null || _demuxer == null || _vfx == null) return;
        if (!_videoPlayer.isPlaying) return;

        var tex = _videoPlayer.texture;
        if (tex == null) return;

        // Decode metadata
        if (_metadataDecoder != null)
        {
            _metadataDecoder.RequestDecodeAsync(tex);
            var meta = _metadataDecoder.Metadata;

            if (!meta.IsValid) return;

            // Demux color and depth
            _demuxer.Demux(tex, meta);

            // Bind textures to VFX
            if (_demuxer.ColorTexture != null && _vfx.HasTexture("ColorMap"))
                _vfx.SetTexture("ColorMap", _demuxer.ColorTexture);

            if (_demuxer.DepthTexture != null && _vfx.HasTexture("DepthMap"))
                _vfx.SetTexture("DepthMap", _demuxer.DepthTexture);

            // Compute InverseView from camera position/rotation
            if (_vfx.HasMatrix4x4("InverseView"))
            {
                var viewMatrix = Matrix4x4.TRS(
                    meta.CameraPosition,
                    meta.CameraRotation,
                    Vector3.one
                );
                _vfx.SetMatrix4x4("InverseView", viewMatrix);
            }

            // Compute RayParams from FOV
            // RayParams = (0, 0, tan(fov/2)*aspect, tan(fov/2))
            if (_vfx.HasVector4("RayParams"))
            {
                float tanHalfFov = Mathf.Tan(meta.FieldOfView / 2);
                float aspect = (float)tex.width / tex.height / 2; // /2 because metavido is side-by-side
                var rayParams = new Vector4(0, 0, tanHalfFov * aspect, tanHalfFov);
                _vfx.SetVector4("RayParams", rayParams);
            }

            // Bind depth range
            if (_vfx.HasVector2("DepthRange"))
                _vfx.SetVector2("DepthRange", meta.DepthRange);
        }
    }

    void OnGUI()
    {
        if (!_showDebugGUI) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label($"<b>Hologram Controller</b>");
        GUILayout.Label($"Mode: {_mode}");
        GUILayout.Label($"Scale: {_scale:F2}");
        GUILayout.Label($"Anchor: {(_anchor != null ? _anchor.name : "null")}");
        GUILayout.Label($"VFX: {(_vfx != null ? _vfx.name : "null")}");
        GUILayout.Label($"IsPlaying: {IsPlaying}");

        GUILayout.Space(10);

        if (GUILayout.Button(_mode == SourceMode.LiveAR ? "Switch to Metavido" : "Switch to Live AR"))
        {
            SetMode(_mode == SourceMode.LiveAR ? SourceMode.MetavidoVideo : SourceMode.LiveAR);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    #if UNITY_EDITOR
    [ContextMenu("Setup for Live AR")]
    void SetupLiveAR()
    {
        SetMode(SourceMode.LiveAR);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Setup for Metavido")]
    void SetupMetavido()
    {
        // Add required components
        if (_videoPlayer == null)
            _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        if (_demuxer == null)
            _demuxer = gameObject.AddComponent<TextureDemuxer>();
        if (_metadataDecoder == null)
            _metadataDecoder = gameObject.AddComponent<MetadataDecoder>();

        SetMode(SourceMode.MetavidoVideo);
        UnityEditor.EditorUtility.SetDirty(this);
    }
    #endif
}
