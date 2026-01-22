// DomainReloadFixes.cs - Prevents errors during assembly reload
// Consolidates: VFXDomainReloadFix, WebRTCDomainReloadFix
// Closes VFX windows and disposes WebRTC before domain reload

#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MetavidoVFX.Editor
{
    /// <summary>
    /// Prevents common errors during Unity domain reload by properly
    /// cleaning up resources before assembly unload.
    /// </summary>
    [InitializeOnLoad]
    public static class DomainReloadFixes
    {
        static DomainReloadFixes()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnBeforeAssemblyReload()
        {
            CloseVFXWindows();
            DisposeWebRTC();
        }

        #region VFX Graph Fix

        /// <summary>
        /// Closes all VFX Graph editor windows before domain reload.
        /// Fixes: ArgumentException: Context does not exist
        /// </summary>
        private static void CloseVFXWindows()
        {
            try
            {
                // Find VFXViewWindow type via reflection (it's internal)
                var vfxAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Unity.VisualEffectGraph.Editor");

                if (vfxAssembly == null) return;

                var vfxWindowType = vfxAssembly.GetType("UnityEditor.VFX.UI.VFXViewWindow");
                if (vfxWindowType == null) return;

                // Close all VFX Graph editor windows
                var vfxWindows = Resources.FindObjectsOfTypeAll(vfxWindowType);
                foreach (var window in vfxWindows)
                {
                    try
                    {
                        ((EditorWindow)window).Close();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[DomainReloadFixes] Could not close VFX window: {e.Message}");
                    }
                }
            }
            catch (Exception)
            {
                // Silently ignore - VFX Graph assembly might not be loaded
            }
        }

        #endregion

        #region WebRTC Fix

        /// <summary>
        /// Disposes WebRTC before domain reload.
        /// Fixes: EntryPointNotFoundException: RegisterDebugLog
        /// Only runs if Unity WebRTC package is installed.
        /// </summary>
        private static void DisposeWebRTC()
        {
#if UNITY_WEBRTC_AVAILABLE
            try
            {
                if (Unity.WebRTC.WebRTC.IsInitialized)
                {
                    Unity.WebRTC.WebRTC.Dispose();
                }
            }
            catch (Exception)
            {
                // Swallow - native lib not loaded is OK
            }
#endif
        }

        #endregion
    }
}
#endif
