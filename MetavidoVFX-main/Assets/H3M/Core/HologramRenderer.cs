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

        // Debug overlay for on-device troubleshooting
        void OnGUI()
        {
            if (_source == null) return;

            GUI.skin.label.fontSize = 32;
            GUI.skin.button.fontSize = 28;
            GUILayout.BeginArea(new Rect(30, 100, Screen.width - 60, Screen.height - 200));

            GUILayout.Label("=== H3M Hologram Debug ===");

            // VFX Status
            if (_vfx == null)
                GUILayout.Label("VFX: NULL");
            else
            {
                GUILayout.Label($"VFX Alive Particles: {_vfx.aliveParticleCount}");
                GUILayout.Label($"PositionMap: {(_vfx.HasTexture("PositionMap") ? "YES" : "NO")}");
                GUILayout.Label($"ColorMap: {(_vfx.HasTexture("ColorMap") ? "YES" : "NO")}");
                GUILayout.Label($"Spawn Property: {(_vfx.HasBool("Spawn") ? "YES" : "NO")}");
            }

            // Source Status
            if (_source != null)
            {
                var posMap = _source.PositionMap;
                var stencilTex = _source.StencilTexture;
                GUILayout.Label($"Source PosMap: {(posMap != null ? posMap.width + "x" + posMap.height : "NULL")}");
                GUILayout.Label($"Source Stencil: {(stencilTex != null ? stencilTex.width + "x" + stencilTex.height : "NULL")}");
            }

            // Anchor Status
            if (_anchor != null)
                GUILayout.Label($"Anchor: {_anchor.position.ToString("F2")} | Scale: {_hologramScale:F2}");
            else
                GUILayout.Label("Anchor: NULL (Not Placed)");

            GUILayout.Label($"Frame: {_frameCount}");

            GUILayout.EndArea();
        }
    }
}
