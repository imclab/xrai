using UnityEditor;

/// <summary>
/// Prevents WebRTC dispose errors during domain reload by ensuring
/// WebRTC is properly initialized before assembly unload.
/// Fixes: EntryPointNotFoundException: RegisterDebugLog
/// </summary>
[InitializeOnLoad]
public static class WebRTCDomainReloadFix
{
    static WebRTCDomainReloadFix()
    {
        // Only run if WebRTC package is installed
        #if UNITY_WEBRTC_AVAILABLE
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        #endif
    }

    #if UNITY_WEBRTC_AVAILABLE
    static void OnBeforeAssemblyReload()
    {
        try
        {
            // Force WebRTC disposal BEFORE Unity tries to do it
            // This prevents the EntryPointNotFoundException
            if (Unity.WebRTC.WebRTC.IsInitialized)
            {
                Unity.WebRTC.WebRTC.Dispose();
            }
        }
        catch (System.Exception)
        {
            // Swallow - native lib not loaded is OK
        }
    }
    #endif
}
