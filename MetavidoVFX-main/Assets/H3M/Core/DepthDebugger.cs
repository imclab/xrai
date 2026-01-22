using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;
using XRRAI.VFXBinders;

namespace XRRAI.Hologram
{
    /// <summary>
    /// Debug component that logs depth texture availability and VFX status every second.
    /// Auto-creates itself on app start via RuntimeInitializeOnLoadMethod.
    /// </summary>
    public class DepthDebugger : MonoBehaviour
    {
        static void Log(string msg) { Debug.Log(msg); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            var go = new GameObject("DepthDebugger");
            go.AddComponent<DepthDebugger>();
            DontDestroyOnLoad(go);
            Log("[DepthDebug] Auto-created DepthDebugger");
        }

        [SerializeField] AROcclusionManager occlusionManager;
        [SerializeField] VisualEffect vfx;

        float lastLogTime = 0;

        void Start()
        {
            Debug.Log("[DepthDebug] DepthDebugger started");

            // Auto-find if not set
            if (occlusionManager == null)
                occlusionManager = FindObjectOfType<AROcclusionManager>();

            if (vfx == null)
                vfx = FindObjectOfType<VisualEffect>();

            Log($"[DepthDebug] OccMgr={occlusionManager != null} VFX={vfx != null}");
        }

        void Update()
        {
            if (Time.time - lastLogTime < 1f) return;
            lastLogTime = Time.time;

            string status = "[DepthDebug] ";

            if (occlusionManager != null)
            {
                try {
                    var envDepth = occlusionManager.environmentDepthTexture;
                    var humanDepth = occlusionManager.humanDepthTexture;
                    var humanStencil = occlusionManager.humanStencilTexture;

                    status += $"Env={FormatTex(envDepth)} ";
                    status += $"Human={FormatTex(humanDepth)} ";
                    status += $"Stencil={FormatTex(humanStencil)} ";
                } catch (System.Exception e) {
                    status += $"OccError={e.GetType().Name} ";
                }
            }
            else
            {
                status += "NO_OCCMGR ";
            }

            if (vfx != null)
            {
                status += $"Particles={vfx.aliveParticleCount} ";
                status += $"HasDepth={vfx.HasTexture("DepthMap")} ";
                status += $"HasSpawn={vfx.HasBool("Spawn")}";
            }
            else
            {
                status += "NO_VFX";
            }

//            Debug.Log(status);
        }

        string FormatTex(Texture tex)
        {
            if (tex == null) return "NULL";
            return $"{tex.width}x{tex.height}";
        }
    }
}
