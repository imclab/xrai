#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using XRRAI.Debugging;

namespace XRRAI.Editor
{
    /// <summary>
    /// Editor utilities for VFX Graph debugging and development.
    /// </summary>
    public static class VFXDebugSetup
    {
        [MenuItem("H3M/VFX Debug/Add Property Inspector")]
        public static void AddPropertyInspector()
        {
            var existing = Object.FindFirstObjectByType<VFXPropertyInspector>();
            if (existing != null)
            {
                Debug.Log("[VFX Debug] Property Inspector already exists");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            var go = new GameObject("VFX_PropertyInspector");
            go.AddComponent<VFXPropertyInspector>();
            Selection.activeGameObject = go;
            Debug.Log("[VFX Debug] Added Property Inspector. Press F1 to toggle, F2 to cycle VFX, 1-9 to select.");
        }

        [MenuItem("H3M/VFX Debug/Add Mock Texture Provider")]
        public static void AddMockTextureProvider()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                // Create standalone
                var go = new GameObject("VFX_MockTextureProvider");
                go.AddComponent<VFXMockTextureProvider>();
                Selection.activeGameObject = go;
                Debug.Log("[VFX Debug] Added Mock Texture Provider. Assign a target VFX in inspector.");
                return;
            }

            var vfx = selected.GetComponent<VisualEffect>();
            if (vfx == null)
            {
                Debug.LogWarning("[VFX Debug] Selected object has no VisualEffect. Add a VFX first.");
                return;
            }

            var provider = selected.GetComponent<VFXMockTextureProvider>();
            if (provider == null)
            {
                provider = selected.AddComponent<VFXMockTextureProvider>();
                Debug.Log($"[VFX Debug] Added Mock Texture Provider to {selected.name}");
            }
            else
            {
                Debug.Log("[VFX Debug] Mock Texture Provider already exists on this object");
            }
        }

        [MenuItem("H3M/VFX Debug/Add All Debug Tools")]
        public static void AddAllDebugTools()
        {
            AddPropertyInspector();

            // Add dashboard if not present
            var dashboard = Object.FindFirstObjectByType<VFXPipelineDashboard>();
            if (dashboard == null)
            {
                var go = new GameObject("VFX_PipelineDashboard");
                go.AddComponent<VFXPipelineDashboard>();
                Debug.Log("[VFX Debug] Added Pipeline Dashboard. Press Tab to toggle.");
            }

            Debug.Log("[VFX Debug] Debug tools ready:");
            Debug.Log("  - F1: Property Inspector");
            Debug.Log("  - Tab: Pipeline Dashboard");
            Debug.Log("  - 1-9: Select VFX");
        }

        [MenuItem("H3M/VFX Debug/List All VFX Properties")]
        public static void ListAllVFXProperties()
        {
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            if (allVFX.Length == 0)
            {
                Debug.Log("[VFX Debug] No VFX found in scene");
                return;
            }

            foreach (var vfx in allVFX)
            {
                if (vfx.visualEffectAsset == null) continue;

                Debug.Log($"\n[VFX Debug] === {vfx.name} ({vfx.visualEffectAsset.name}) ===");
                Debug.Log($"  Alive Particles: {vfx.aliveParticleCount:N0}");

                // Check standard properties
                LogProperty(vfx, "ColorMap", "Texture");
                LogProperty(vfx, "DepthMap", "Texture");
                LogProperty(vfx, "StencilMap", "Texture");
                LogProperty(vfx, "PositionMap", "Texture");
                LogProperty(vfx, "VelocityMap", "Texture");
                LogProperty(vfx, "RayParams", "Vector4");
                LogProperty(vfx, "InverseView", "Matrix");
                LogProperty(vfx, "DepthRange", "Vector2");
                LogProperty(vfx, "ParticleCount", "UInt");
                LogProperty(vfx, "ParticleSize", "Float");
                LogProperty(vfx, "Throttle", "Float");
            }
        }

        private static void LogProperty(VisualEffect vfx, string name, string type)
        {
            bool has = false;
            string value = "";

            switch (type)
            {
                case "Texture":
                    has = vfx.HasTexture(name);
                    if (has)
                    {
                        var tex = vfx.GetTexture(name);
                        value = tex != null ? $"{tex.width}x{tex.height}" : "null";
                    }
                    break;
                case "Vector4":
                    has = vfx.HasVector4(name);
                    if (has)
                    {
                        var v = vfx.GetVector4(name);
                        value = $"({v.x:F2}, {v.y:F2}, {v.z:F2}, {v.w:F2})";
                    }
                    break;
                case "Vector2":
                    has = vfx.HasVector2(name);
                    if (has)
                    {
                        var v = vfx.GetVector2(name);
                        value = $"({v.x:F2}, {v.y:F2})";
                    }
                    break;
                case "Matrix":
                    has = vfx.HasMatrix4x4(name);
                    value = has ? "(matrix)" : "";
                    break;
                case "Float":
                    has = vfx.HasFloat(name);
                    if (has) value = vfx.GetFloat(name).ToString("F3");
                    break;
                case "UInt":
                    has = vfx.HasUInt(name);
                    if (has) value = vfx.GetUInt(name).ToString("N0");
                    break;
            }

            if (has)
            {
                Debug.Log($"  [\u2713] {name} ({type}): {value}");
            }
        }

        [MenuItem("H3M/VFX Debug/Validate All VFX Bindings")]
        public static void ValidateAllBindings()
        {
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            var binders = Object.FindObjectsByType<VFXARBinder>(FindObjectsSortMode.None);

            Debug.Log($"[VFX Debug] === Binding Validation ===");
            Debug.Log($"  VFX Count: {allVFX.Length}");
            Debug.Log($"  Binders: {binders.Length}");

            int bound = 0;
            int unbound = 0;
            int errors = 0;

            foreach (var vfx in allVFX)
            {
                if (vfx.visualEffectAsset == null)
                {
                    Debug.LogWarning($"  [\u2717] {vfx.name}: No VFX asset assigned");
                    errors++;
                    continue;
                }

                var binder = vfx.GetComponent<VFXARBinder>();
                if (binder != null)
                {
                    // Check if textures are bound
                    bool hasColorMap = vfx.HasTexture("ColorMap") && vfx.GetTexture("ColorMap") != null;
                    bool hasDepthMap = vfx.HasTexture("DepthMap") && vfx.GetTexture("DepthMap") != null;

                    if (hasColorMap || hasDepthMap)
                    {
                        Debug.Log($"  [\u2713] {vfx.name}: Bound (Color:{hasColorMap}, Depth:{hasDepthMap})");
                        bound++;
                    }
                    else
                    {
                        Debug.LogWarning($"  [\u26A0] {vfx.name}: Has binder but no textures bound");
                        unbound++;
                    }
                }
                else
                {
                    Debug.Log($"  [ ] {vfx.name}: No binder (standalone VFX)");
                    unbound++;
                }
            }

            Debug.Log($"\n[VFX Debug] Summary: {bound} bound, {unbound} unbound, {errors} errors");
        }

        [MenuItem("H3M/VFX Debug/Open VFX Graph for Selected")]
        public static void OpenVFXGraphForSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[VFX Debug] No object selected");
                return;
            }

            var vfx = selected.GetComponent<VisualEffect>();
            if (vfx == null || vfx.visualEffectAsset == null)
            {
                Debug.LogWarning("[VFX Debug] Selected object has no VFX or VFX asset");
                return;
            }

            // Open VFX Graph window with this asset
            AssetDatabase.OpenAsset(vfx.visualEffectAsset);
            Debug.Log($"[VFX Debug] Opened VFX Graph: {vfx.visualEffectAsset.name}");
        }

        [MenuItem("H3M/VFX Debug/Reinitialize All VFX")]
        public static void ReinitializeAllVFX()
        {
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            foreach (var vfx in allVFX)
            {
                vfx.Reinit();
            }
            Debug.Log($"[VFX Debug] Reinitialized {allVFX.Length} VFX");
        }

        [MenuItem("H3M/VFX Debug/Stop All VFX")]
        public static void StopAllVFX()
        {
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            foreach (var vfx in allVFX)
            {
                vfx.Stop();
            }
            Debug.Log($"[VFX Debug] Stopped {allVFX.Length} VFX");
        }

        [MenuItem("H3M/VFX Debug/Play All VFX")]
        public static void PlayAllVFX()
        {
            var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            foreach (var vfx in allVFX)
            {
                vfx.Play();
            }
            Debug.Log($"[VFX Debug] Started {allVFX.Length} VFX");
        }
    }
}
#endif
