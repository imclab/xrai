using UnityEditor;
using UnityEngine;
using IcosaClientInternal;

namespace H3M.Editor
{
    /// <summary>
    /// Creates the required PtSettings asset for Icosa API Client.
    /// </summary>
    public static class IcosaSettingsCreator
    {
        private const string RESOURCES_PATH = "Assets/Resources";
        private const string SETTINGS_PATH = "Assets/Resources/PtSettings.asset";

        [MenuItem("H3M/Icosa/Create PtSettings Asset", priority = 100)]
        public static void CreatePtSettings()
        {
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder(RESOURCES_PATH))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<PtSettings>(SETTINGS_PATH);
            if (existing != null)
            {
                Debug.Log("[IcosaSettingsCreator] PtSettings already exists at: " + SETTINGS_PATH);
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            // Create new PtSettings
            var settings = ScriptableObject.CreateInstance<PtSettings>();
            AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[IcosaSettingsCreator] Created PtSettings at: " + SETTINGS_PATH);
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem("H3M/Icosa/Select PtSettings", priority = 101)]
        public static void SelectPtSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PtSettings>(SETTINGS_PATH);
            if (settings != null)
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
            else
            {
                Debug.LogWarning("[IcosaSettingsCreator] PtSettings not found. Use H3M > Icosa > Create PtSettings Asset first.");
            }
        }

        [InitializeOnLoadMethod]
        private static void AutoCreate()
        {
            // Auto-create on editor load if missing
            EditorApplication.delayCall += () =>
            {
                var existing = Resources.Load<PtSettings>("PtSettings");
                if (existing == null)
                {
                    Debug.Log("[IcosaSettingsCreator] Auto-creating PtSettings asset...");
                    CreatePtSettings();
                }
            };
        }
    }
}
