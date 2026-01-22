using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Prevents VFX Graph context errors during domain reload by closing
/// VFX windows before assembly unload.
/// Fixes: ArgumentException: Context does not exist
/// </summary>
[InitializeOnLoad]
public static class VFXDomainReloadFix
{
    static VFXDomainReloadFix()
    {
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
    }

    static void OnBeforeAssemblyReload()
    {
        // Find VFXViewWindow type via reflection (it's internal)
        var vfxAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Unity.VisualEffectGraph.Editor");

        if (vfxAssembly == null) return;

        var vfxWindowType = vfxAssembly.GetType("UnityEditor.VFX.UI.VFXViewWindow");
        if (vfxWindowType == null) return;

        // Close all VFX Graph editor windows before domain reload
        var vfxWindows = Resources.FindObjectsOfTypeAll(vfxWindowType);
        foreach (var window in vfxWindows)
        {
            try
            {
                ((EditorWindow)window).Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VFXDomainReloadFix] Could not close VFX window: {e.Message}");
            }
        }
    }
}
