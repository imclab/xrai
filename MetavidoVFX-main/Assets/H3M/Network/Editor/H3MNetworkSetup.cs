// H3MNetworkSetup - Editor utilities for H3M WebRTC video conferencing
// Created: 2026-01-16

using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using XRRAI.Hologram;

namespace XRRAI.Hologram.Editor
{
    public static class H3MNetworkSetup
    {
        private const string WEBRTC_DEFINE = "UNITY_WEBRTC_AVAILABLE";

        #region Menu Items

        [MenuItem("H3M/Network/Setup WebRTC Receiver", priority = 300)]
        public static void SetupWebRTCReceiver()
        {
            // Check for WebRTC package
            if (!HasWebRTCPackage())
            {
                bool install = EditorUtility.DisplayDialog(
                    "WebRTC Package Required",
                    "The com.unity.webrtc package is required for video conferencing.\n\n" +
                    "Would you like to add the scripting define symbol anyway?\n" +
                    "(You'll need to install the package separately)",
                    "Add Define Symbol",
                    "Cancel");

                if (!install) return;
            }

            // Add scripting define
            AddScriptingDefine(WEBRTC_DEFINE);

            // Create receiver GameObject
            var existing = Object.FindFirstObjectByType<H3MWebRTCReceiver>();
            if (existing != null)
            {
                Debug.Log($"[H3MNetworkSetup] H3MWebRTCReceiver already exists on '{existing.gameObject.name}'");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            var go = new GameObject("H3M WebRTC Receiver");
            go.AddComponent<H3MSignalingClient>();
            go.AddComponent<H3MWebRTCReceiver>();

            Undo.RegisterCreatedObjectUndo(go, "Create H3M WebRTC Receiver");
            Selection.activeGameObject = go;

            Debug.Log("[H3MNetworkSetup] Created H3M WebRTC Receiver");
            Debug.Log("[H3MNetworkSetup] Configure the signaling server URL and room name in Inspector");
        }

        [MenuItem("H3M/Network/Add WebRTC Binder to Selected VFX", priority = 301)]
        public static void AddWebRTCBinderToSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[H3MNetworkSetup] No GameObject selected");
                return;
            }

            var vfx = selected.GetComponent<VisualEffect>();
            if (vfx == null)
            {
                Debug.LogWarning("[H3MNetworkSetup] Selected GameObject doesn't have a VisualEffect component");
                return;
            }

            // Check if binder already exists
            var existingBinder = selected.GetComponent<H3MWebRTCVFXBinder>();
            if (existingBinder != null)
            {
                Debug.Log("[H3MNetworkSetup] H3MWebRTCVFXBinder already exists on this VFX");
                return;
            }

            // Add binder
            var binder = selected.AddComponent<H3MWebRTCVFXBinder>();

            // Try to find receiver
            var receiver = Object.FindFirstObjectByType<H3MWebRTCReceiver>();
            if (receiver != null)
            {
                binder.Target = receiver;
                Debug.Log($"[H3MNetworkSetup] Auto-linked to receiver on '{receiver.gameObject.name}'");
            }

            Undo.RegisterCreatedObjectUndo(binder, "Add H3M WebRTC Binder");
            EditorUtility.SetDirty(selected);

            Debug.Log($"[H3MNetworkSetup] Added H3MWebRTCVFXBinder to '{selected.name}'");
        }

        [MenuItem("H3M/Network/Setup WebRTC Defines", priority = 310)]
        public static void SetupWebRTCDefines()
        {
            if (HasScriptingDefine(WEBRTC_DEFINE))
            {
                Debug.Log($"[H3MNetworkSetup] {WEBRTC_DEFINE} already defined");
                return;
            }

            AddScriptingDefine(WEBRTC_DEFINE);
            Debug.Log($"[H3MNetworkSetup] Added {WEBRTC_DEFINE} scripting define");
        }

        [MenuItem("H3M/Network/Verify Network Setup", priority = 320)]
        public static void VerifyNetworkSetup()
        {
            Debug.Log("=== H3M Network Setup Verification ===\n");

            // Check scripting define
            bool hasDefine = HasScriptingDefine(WEBRTC_DEFINE);
            Debug.Log($"{(hasDefine ? "✓" : "✗")} {WEBRTC_DEFINE} scripting define");

            // Check WebRTC package
            bool hasPackage = HasWebRTCPackage();
            Debug.Log($"{(hasPackage ? "✓" : "✗")} com.unity.webrtc package installed");

            // Check components in scene
            var signaling = Object.FindFirstObjectByType<H3MSignalingClient>();
            var receiver = Object.FindFirstObjectByType<H3MWebRTCReceiver>();
            var binders = Object.FindObjectsByType<H3MWebRTCVFXBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Debug.Log($"{(signaling != null ? "✓" : "○")} H3MSignalingClient in scene");
            Debug.Log($"{(receiver != null ? "✓" : "○")} H3MWebRTCReceiver in scene");
            Debug.Log($"  VFX with H3MWebRTCVFXBinder: {binders.Length}");

            if (receiver != null)
            {
                var receiverSo = new SerializedObject(receiver);
                var roomProp = receiverSo.FindProperty("_roomName");
                if (roomProp != null)
                {
                    Debug.Log($"  Room name: {roomProp.stringValue}");
                }
            }

            Debug.Log("\n=== Verification Complete ===");
        }

        [MenuItem("H3M/Network/Documentation", priority = 350)]
        public static void OpenDocumentation()
        {
            Debug.Log("=== H3M Network (WebRTC) Documentation ===\n");
            Debug.Log("Components:");
            Debug.Log("  - H3MSignalingClient: WebSocket connection for peer discovery");
            Debug.Log("  - H3MWebRTCReceiver: Receives video stream from remote peer");
            Debug.Log("  - H3MWebRTCVFXBinder: Binds remote stream to VFX properties\n");

            Debug.Log("Setup:");
            Debug.Log("  1. Install com.unity.webrtc package");
            Debug.Log("  2. H3M > Network > Setup WebRTC Receiver");
            Debug.Log("  3. Configure signaling server URL in Inspector");
            Debug.Log("  4. Add H3MWebRTCVFXBinder to VFX that need remote data\n");

            Debug.Log("VFX Properties bound:");
            Debug.Log("  - ColorMap: Remote camera color");
            Debug.Log("  - DepthMap: Remote depth");
            Debug.Log("  - RayParams: Inverse projection for ray casting");
            Debug.Log("  - InverseView: Remote camera transform");
            Debug.Log("  - DepthRange: Near/far clipping\n");

            Debug.Log("Signaling Server:");
            Debug.Log("  The signaling server handles WebRTC session negotiation.");
            Debug.Log("  See Server/ folder for reference implementation.");
            Debug.Log("  Default: ws://localhost:3003");
        }

        #endregion

        #region Helpers

        private static bool HasWebRTCPackage()
        {
            // Check if Unity.WebRTC assembly exists
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.GetName().Name == "Unity.WebRTC")
                    return true;
            }
            return false;
        }

        private static bool HasScriptingDefine(string define)
        {
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
            return defines.Contains(define);
        }

        private static void AddScriptingDefine(string define)
        {
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

            if (!defines.Contains(define))
            {
                defines = string.IsNullOrEmpty(defines) ? define : defines + ";" + define;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, defines);
                Debug.Log($"[H3MNetworkSetup] Added scripting define: {define}");
            }
        }

        #endregion
    }
}
