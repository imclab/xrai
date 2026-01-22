// Editor helper to disable iOS-only components when entering Play mode in Editor
// This prevents crashes from HoloKit native code that only works on device
// Re-enables components when Play mode stops (persists across domain reload)

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace XRRAI.Editor
{
    [InitializeOnLoad]
    public static class EditorPlayModeHelper
    {
        // Components that require iOS native code and crash in Editor
        private static readonly string[] iOSOnlyComponentTypes = new string[]
        {
            "HoloKit.iOS.HandTrackingManager",
            "HoloKit.iOS.HandGestureRecognitionManager",
            "HoloKit.GazeRaycastInteractor",
            "HoloKit.iOS.GazeGestureInteractor",
            "HoloKit.iOS.HoloKitCameraManager",
            "HoloKit.iOS.AppleVisionHandPoseDetector"
        };

        // EditorPrefs key for persisting disabled object paths across domain reload
        private const string DISABLED_OBJECTS_KEY = "H3M_DisabledIOSObjects";
        private const string DISABLED_COMPONENTS_KEY = "H3M_DisabledIOSComponents";

        static EditorPlayModeHelper()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    // About to enter Play mode - disable iOS-only components
                    DisableIOSOnlyComponents();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    // Exited Play mode - re-enable components
                    ReenableComponents();
                    break;
            }
        }

        static void DisableIOSOnlyComponents()
        {
            var disabledPaths = new List<string>();
            var disabledComponentInfo = new List<string>(); // "path|componentType"

            foreach (string typeName in iOSOnlyComponentTypes)
            {
                System.Type type = FindType(typeName);
                if (type == null) continue;

                var components = Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var component in components)
                {
                    if (component is Behaviour behaviour)
                    {
                        string path = GetGameObjectPath(behaviour.gameObject);

                        // Disable the component (not the whole GameObject)
                        if (behaviour.enabled)
                        {
                            behaviour.enabled = false;
                            disabledComponentInfo.Add($"{path}|{typeName}");
                            Debug.Log($"[EditorPlayMode] Disabled component {typeName} on '{behaviour.gameObject.name}' - iOS native code not available in Editor");
                        }
                    }
                    else if (component is Component comp && comp.gameObject.activeSelf)
                    {
                        // Fallback: disable GameObject for non-Behaviour components
                        string path = GetGameObjectPath(comp.gameObject);
                        comp.gameObject.SetActive(false);
                        disabledPaths.Add(path);
                        Debug.Log($"[EditorPlayMode] Disabled GameObject '{comp.gameObject.name}' ({typeName}) - iOS native code not available in Editor");
                    }
                }
            }

            // Persist to EditorPrefs (survives domain reload)
            EditorPrefs.SetString(DISABLED_OBJECTS_KEY, string.Join(";", disabledPaths));
            EditorPrefs.SetString(DISABLED_COMPONENTS_KEY, string.Join(";", disabledComponentInfo));

            int total = disabledPaths.Count + disabledComponentInfo.Count;
            if (total > 0)
            {
                Debug.Log($"[EditorPlayMode] Disabled {total} iOS-only components for Editor Play mode");
            }
        }

        static void ReenableComponents()
        {
            int reenabledCount = 0;

            // Re-enable components
            string componentData = EditorPrefs.GetString(DISABLED_COMPONENTS_KEY, "");
            if (!string.IsNullOrEmpty(componentData))
            {
                string[] entries = componentData.Split(';');
                foreach (string entry in entries)
                {
                    if (string.IsNullOrEmpty(entry)) continue;

                    string[] parts = entry.Split('|');
                    if (parts.Length != 2) continue;

                    string path = parts[0];
                    string typeName = parts[1];

                    GameObject go = FindGameObjectByPath(path);
                    if (go != null)
                    {
                        System.Type type = FindType(typeName);
                        if (type != null)
                        {
                            var comp = go.GetComponent(type) as Behaviour;
                            if (comp != null && !comp.enabled)
                            {
                                comp.enabled = true;
                                reenabledCount++;
                                Debug.Log($"[EditorPlayMode] Re-enabled {typeName} on '{go.name}'");
                            }
                        }
                    }
                }
            }

            // Re-enable GameObjects
            string objectData = EditorPrefs.GetString(DISABLED_OBJECTS_KEY, "");
            if (!string.IsNullOrEmpty(objectData))
            {
                string[] paths = objectData.Split(';');
                foreach (string path in paths)
                {
                    if (string.IsNullOrEmpty(path)) continue;

                    GameObject go = FindGameObjectByPath(path);
                    if (go != null && !go.activeSelf)
                    {
                        go.SetActive(true);
                        reenabledCount++;
                        Debug.Log($"[EditorPlayMode] Re-enabled GameObject '{go.name}'");
                    }
                }
            }

            // Clear persisted data
            EditorPrefs.DeleteKey(DISABLED_OBJECTS_KEY);
            EditorPrefs.DeleteKey(DISABLED_COMPONENTS_KEY);

            if (reenabledCount > 0)
            {
                Debug.Log($"[EditorPlayMode] Re-enabled {reenabledCount} iOS-only components after Play mode");

                // Mark scene dirty so user can save the re-enabled state
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        static string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        static GameObject FindGameObjectByPath(string path)
        {
            // Try direct find first
            var go = GameObject.Find(path);
            if (go != null) return go;

            // Try finding by traversing hierarchy
            string[] parts = path.Split('/');
            if (parts.Length == 0) return null;

            // Find root objects
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                if (root.name == parts[0])
                {
                    if (parts.Length == 1) return root;

                    // Traverse down
                    Transform current = root.transform;
                    for (int i = 1; i < parts.Length; i++)
                    {
                        current = current.Find(parts[i]);
                        if (current == null) break;
                    }
                    if (current != null) return current.gameObject;
                }
            }

            return null;
        }

        static System.Type FindType(string fullTypeName)
        {
            // Search in all loaded assemblies
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullTypeName);
                if (type != null) return type;
            }
            return null;
        }

        // Manual re-enable menu item (in case automatic fails)
        [MenuItem("H3M/Debug/Re-enable iOS Components", priority = 500)]
        public static void ManualReenableComponents()
        {
            ReenableComponents();

            // Also try direct search and enable
            foreach (string typeName in iOSOnlyComponentTypes)
            {
                System.Type type = FindType(typeName);
                if (type == null) continue;

                var components = Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var component in components)
                {
                    if (component is Behaviour behaviour && !behaviour.enabled)
                    {
                        behaviour.enabled = true;
                        Debug.Log($"[EditorPlayMode] Force re-enabled {typeName} on '{behaviour.gameObject.name}'");
                        EditorUtility.SetDirty(behaviour);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[EditorPlayMode] Manual re-enable complete");
        }
    }
}
