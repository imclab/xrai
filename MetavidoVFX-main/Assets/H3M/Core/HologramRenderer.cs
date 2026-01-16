using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;
using Metavido;
using MetavidoVFX.VFX;

namespace H3M.Core
{
    [RequireComponent(typeof(VisualEffect))]
    public class HologramRenderer : MonoBehaviour
    {
        void LogWarning(string msg) { if (!VFXBinderManager.SuppressHologramLogs) Debug.LogWarning(msg); }

        [Header("H3M References")]
        [SerializeField] HologramSource _source;
        [SerializeField] Transform _anchor;

        [Header("Settings")]
        [SerializeField] Vector2 _depthRange = new Vector2(0.1f, 10.0f);
        [SerializeField] float _hologramScale = 0.1f; // "Mini Me" scale

        VisualEffect _vfx;
        int _frameCount = 0;

        void Awake()
        {
            _vfx = GetComponent<VisualEffect>();
        }

        void Start()
        {
            if (_source == null) _source = FindFirstObjectByType<HologramSource>();
            if (_anchor == null)
            {
                var anchorObj = FindFirstObjectByType<HologramAnchor>();
                if (anchorObj != null) _anchor = anchorObj.transform;
            }
        }

        void Update()
        {
            _frameCount++;

            if (_vfx == null || _source == null)
            {
                if (_frameCount % 300 == 0)
                    LogWarning("[HologramRenderer] Missing Core Refs (VFX or Source)");
                return;
            }

            // 1. Bind Textures from Source
            // NOTE: Only bind PositionMap, NOT DepthMap. VFX expecting DepthMap need raw depth
            // (bound by VFXBinderManager), not computed positions. Previous fallback was incorrect.
            if (_source.PositionMap != null)
            {
                if (_vfx.HasTexture("PositionMap")) _vfx.SetTexture("PositionMap", _source.PositionMap);
            }

            if (_source.ColorTexture != null && _vfx.HasTexture("ColorMap"))
                _vfx.SetTexture("ColorMap", _source.ColorTexture);

            if (_source.StencilTexture != null && _vfx.HasTexture("StencilMap"))
                _vfx.SetTexture("StencilMap", _source.StencilTexture);

            // 2. Bind Transformation State
            if (_vfx.HasMatrix4x4("InverseView"))
                _vfx.SetMatrix4x4("InverseView", _source.GetInverseViewMatrix());

            if (_vfx.HasMatrix4x4("ProjectionMatrix"))
                _vfx.SetMatrix4x4("ProjectionMatrix", _source.arCamera.projectionMatrix);

            if (_vfx.HasVector4("RayParams"))
                _vfx.SetVector4("RayParams", _source.GetRayParams());

            if (_vfx.HasVector2("DepthRange"))
                _vfx.SetVector2("DepthRange", _source.DepthRange);

            // 3. Bind Anchor & Scale for "Man in the Mirror"
            if (_vfx.HasVector3("AnchorPos"))
            {
                if (_anchor != null)
                    _vfx.SetVector3("AnchorPos", _anchor.position);
                else
                    _vfx.SetVector3("AnchorPos", Vector3.zero);
            }

            if (_vfx.HasFloat("HologramScale"))
                _vfx.SetFloat("HologramScale", _hologramScale);

            // 4. Control Flags
            if (_vfx.HasBool("Spawn"))
                _vfx.SetBool("Spawn", _source.PositionMap != null);
        }

        #region Public Debug API (for HologramDebugUI)

        /// <summary>Get debug info for UI display.</summary>
        public HologramDebugInfo GetDebugInfo()
        {
            return new HologramDebugInfo
            {
                HasVFX = _vfx != null,
                AliveParticles = _vfx != null ? _vfx.aliveParticleCount : 0,
                HasPositionMap = _vfx != null && _vfx.HasTexture("PositionMap"),
                HasColorMap = _vfx != null && _vfx.HasTexture("ColorMap"),
                HasSpawnProperty = _vfx != null && _vfx.HasBool("Spawn"),
                PositionMapSize = _source?.PositionMap != null
                    ? $"{_source.PositionMap.width}x{_source.PositionMap.height}"
                    : "NULL",
                StencilSize = _source?.StencilTexture != null
                    ? $"{_source.StencilTexture.width}x{_source.StencilTexture.height}"
                    : "NULL",
                AnchorPosition = _anchor != null ? _anchor.position : Vector3.zero,
                HasAnchor = _anchor != null,
                HologramScale = _hologramScale,
                FrameCount = _frameCount
            };
        }

        #endregion
    }

    /// <summary>Debug info struct for hologram renderer state.</summary>
    public struct HologramDebugInfo
    {
        public bool HasVFX;
        public int AliveParticles;
        public bool HasPositionMap;
        public bool HasColorMap;
        public bool HasSpawnProperty;
        public string PositionMapSize;
        public string StencilSize;
        public Vector3 AnchorPosition;
        public bool HasAnchor;
        public float HologramScale;
        public int FrameCount;
    }
}
