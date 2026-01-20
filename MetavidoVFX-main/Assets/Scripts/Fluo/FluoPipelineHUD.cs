// FluoPipelineHUD - Monitor HUD for viewing interchangeable VFX pipeline parts
// Shows pipeline flow: Sources → Processors → VFX → Output

using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;
using MetavidoVFX.VFX;

namespace MetavidoVFX.Fluo
{
    public class FluoPipelineHUD : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private bool showHUD = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private float hudScale = 1f;

        [Header("Texture Previews")]
        [SerializeField] private bool showTexturePreviews = true;
        [SerializeField] private int previewSize = 128;

        private ARDepthSource _depthSource;
        private AudioBridge _audioBridge;
        private FluoCanvas _fluoCanvas;
        private List<VisualEffect> _activeVFX = new();

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private bool _stylesInit;

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                showHUD = !showHUD;

            CacheReferences();
        }

        void CacheReferences()
        {
            if (_depthSource == null) _depthSource = ARDepthSource.Instance;
            if (_audioBridge == null) _audioBridge = FindFirstObjectByType<AudioBridge>();
            if (_fluoCanvas == null) _fluoCanvas = FindFirstObjectByType<FluoCanvas>();
        }

        void OnGUI()
        {
            if (!showHUD) return;

            InitStyles();

            float x = 10;
            float y = 10;
            float w = 220 * hudScale;
            float h = 24 * hudScale;

            // Title
            GUI.Box(new Rect(x, y, w, h), "FLUO PIPELINE MONITOR", _headerStyle);
            y += h + 4;

            // Sources Section
            DrawSection(ref x, ref y, w, "SOURCES", () =>
            {
                DrawSourceStatus("ARDepthSource", _depthSource != null, _depthSource?.IsReady ?? false, ref y, 220 * hudScale);
                DrawSourceStatus("AudioBridge", _audioBridge != null, _audioBridge != null && _audioBridge.enabled, ref y, 220 * hudScale);
                DrawSourceStatus("FluoCanvas", _fluoCanvas != null, _fluoCanvas != null && _fluoCanvas.enabled, ref y, 220 * hudScale);
            });

            // Binding Mode Section
            DrawSection(ref x, ref y, w, "BINDING MODES", () =>
            {
                int arCount = 0, audioCount = 0, keypointCount = 0, standaloneCount = 0;
                foreach (var vfx in _activeVFX)
                {
                    var cat = vfx.GetComponent<VFXCategory>();
                    if (cat != null)
                    {
                        switch (cat.BindingMode)
                        {
                            case VFXBindingMode.AR: arCount++; break;
                            case VFXBindingMode.Audio: audioCount++; break;
                            case VFXBindingMode.Keypoint: keypointCount++; break;
                            case VFXBindingMode.Standalone: standaloneCount++; break;
                        }
                    }
                }
                GUI.Label(new Rect(x + 8, y, w - 16, 18), $"AR: {arCount}  Audio: {audioCount}  Keypoint: {keypointCount}  Standalone: {standaloneCount}", _labelStyle);
                y += 20;
            });

            // Active VFX Section
            DrawSection(ref x, ref y, w, $"ACTIVE VFX ({_activeVFX.Count})", () =>
            {
                RefreshActiveVFX();
                int shown = 0;
                foreach (var vfx in _activeVFX)
                {
                    if (shown >= 8) { GUI.Label(new Rect(x + 8, y, w - 16, 18), $"... +{_activeVFX.Count - 8} more", _labelStyle); y += 20; break; }
                    string status = vfx.aliveParticleCount > 0 ? $"({vfx.aliveParticleCount})" : "(0)";
                    var cat = vfx.GetComponent<VFXCategory>();
                    string mode = cat != null ? cat.BindingMode.ToString()[0].ToString() : "?";
                    GUI.Label(new Rect(x + 8, y, w - 16, 18), $"[{mode}] {vfx.name} {status}", _labelStyle);
                    y += 20;
                    shown++;
                }
            });

            // Texture Previews
            if (showTexturePreviews)
            {
                DrawTexturePreviewSection(ref x, ref y, w);
            }

            // Performance
            DrawSection(ref x, ref y, w, "PERFORMANCE", () =>
            {
                float fps = 1f / Time.smoothDeltaTime;
                GUI.Label(new Rect(x + 8, y, w - 16, 18), $"FPS: {fps:F0}", _labelStyle);
                y += 20;
                long mem = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
                GUI.Label(new Rect(x + 8, y, w - 16, 18), $"Memory: {mem} MB", _labelStyle);
                y += 20;
            });
        }

        void DrawSection(ref float x, ref float y, float w, string title, System.Action content)
        {
            GUI.Box(new Rect(x, y, w, 22 * hudScale), title, _boxStyle);
            y += 24 * hudScale;
            content();
            y += 4;
        }

        void DrawSourceStatus(string name, bool exists, bool active, ref float y, float w)
        {
            Color c = exists ? (active ? Color.green : Color.yellow) : Color.red;
            string status = exists ? (active ? "●" : "○") : "✗";
            GUI.color = c;
            GUI.Label(new Rect(18, y, w - 16, 18), $"{status} {name}", _labelStyle);
            GUI.color = Color.white;
            y += 20;
        }

        void DrawTexturePreviewSection(ref float x, ref float y, float w)
        {
            GUI.Box(new Rect(x, y, w, 22 * hudScale), "TEXTURE PREVIEW", _boxStyle);
            y += 24 * hudScale;

            float px = x + 4;
            float py = y;
            float ps = previewSize * hudScale * 0.5f;

            // DepthMap
            if (_depthSource?.DepthMap != null)
            {
                GUI.DrawTexture(new Rect(px, py, ps, ps), _depthSource.DepthMap);
                GUI.Label(new Rect(px, py + ps, ps, 16), "Depth", _labelStyle);
                px += ps + 4;
            }

            // ColorMap
            if (_depthSource?.ColorMap != null)
            {
                GUI.DrawTexture(new Rect(px, py, ps, ps), _depthSource.ColorMap);
                GUI.Label(new Rect(px, py + ps, ps, 16), "Color", _labelStyle);
                px += ps + 4;
            }

            // FluoCanvas
            if (_fluoCanvas?.ColorCanvas != null)
            {
                GUI.DrawTexture(new Rect(px, py, ps, ps), _fluoCanvas.ColorCanvas);
                GUI.Label(new Rect(px, py + ps, ps, 16), "Canvas", _labelStyle);
            }

            y += ps + 20;
        }

        void RefreshActiveVFX()
        {
            _activeVFX.Clear();
            var allVFX = FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            foreach (var vfx in allVFX)
            {
                if (vfx.enabled && vfx.gameObject.activeInHierarchy)
                    _activeVFX.Add(vfx);
            }
        }

        void InitStyles()
        {
            if (_stylesInit) return;

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(12 * hudScale),
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 4, 2, 2)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(11 * hudScale),
                alignment = TextAnchor.MiddleLeft
            };

            _headerStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(14 * hudScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _headerStyle.normal.textColor = Color.cyan;

            _stylesInit = true;
        }
    }
}
