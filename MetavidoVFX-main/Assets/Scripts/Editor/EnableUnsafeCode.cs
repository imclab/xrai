using UnityEditor;
using UnityEngine;

namespace H3M.Editor
{
    /// <summary>
    /// Enables unsafe code compilation for the project.
    /// Required by com.icosa.icosa-api-client-unity package.
    /// </summary>
    public static class EnableUnsafeCode
    {
        [MenuItem("H3M/Setup/Enable Unsafe Code (Required for Icosa)", priority = 0)]
        public static void Enable()
        {
            PlayerSettings.allowUnsafeCode = true;
            Debug.Log("[EnableUnsafeCode] Unsafe code enabled. Recompiling...");
            AssetDatabase.Refresh();
        }

        [MenuItem("H3M/Setup/Check Unsafe Code Status")]
        public static void CheckStatus()
        {
            bool enabled = PlayerSettings.allowUnsafeCode;
            Debug.Log($"[EnableUnsafeCode] Allow Unsafe Code: {(enabled ? "ENABLED" : "DISABLED")}");

            if (!enabled)
            {
                EditorUtility.DisplayDialog(
                    "Unsafe Code Status",
                    "Unsafe code is DISABLED.\n\n" +
                    "Click 'H3M > Setup > Enable Unsafe Code' to enable it.\n\n" +
                    "This is required for the Icosa API Client package.",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Unsafe Code Status",
                    "Unsafe code is ENABLED.\n\n" +
                    "All packages requiring unsafe code should compile correctly.",
                    "OK"
                );
            }
        }

        // Auto-enable on Editor load if Icosa package is present
        [InitializeOnLoadMethod]
        private static void AutoEnable()
        {
            // Check if we need to enable unsafe code
            if (!PlayerSettings.allowUnsafeCode)
            {
                // Check if Icosa package exists
                var packagePath = "Packages/com.icosa.icosa-api-client-unity/package.json";
                if (System.IO.File.Exists(packagePath))
                {
                    Debug.Log("[EnableUnsafeCode] Icosa API Client detected. Enabling unsafe code...");
                    PlayerSettings.allowUnsafeCode = true;
                }
            }
        }
    }
}
