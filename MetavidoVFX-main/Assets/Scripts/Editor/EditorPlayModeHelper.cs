// Editor helper to disable iOS-only components when entering Play mode in Editor
// This prevents crashes from HoloKit native code that only works on device

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MetavidoVFX.Editor
{
    [InitializeOnLoad]
    public static class EditorPlayModeHelper
    {
        // Components that require iOS native code and crash in Editor
        private static readonly string[] iOSOnlyComponentTypes = new string[]
        {
            "HoloKit.iOS.HandTrackingManager",
            "HoloKit.iOS.HandGestureRecognitionManager",
            "HoloKit.GazeRaycastInteractor"
        };

        // Store disabled objects to re-enable after Play mode
        private static List<GameObject> disabledObjects = new List<GameObject>();

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
            disabledObjects.Clear();

            foreach (string typeName in iOSOnlyComponentTypes)
            {
                System.Type type = FindType(typeName);
                if (type == null) continue;

                var components = Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var component in components)
                {
                    if (component is Component comp && comp.gameObject.activeSelf)
                    {
                        // Store reference and disable
                        disabledObjects.Add(comp.gameObject);
                        comp.gameObject.SetActive(false);
                        Debug.Log($"[EditorPlayMode] Disabled {comp.gameObject.name} ({typeName}) - iOS native code not available in Editor");
                    }
                }
            }

            if (disabledObjects.Count > 0)
            {
                Debug.Log($"[EditorPlayMode] Disabled {disabledObjects.Count} iOS-only GameObjects for Editor Play mode");
            }
        }

        static void ReenableComponents()
        {
            foreach (var go in disabledObjects)
            {
                if (go != null)
                {
                    go.SetActive(true);
                }
            }

            if (disabledObjects.Count > 0)
            {
                Debug.Log($"[EditorPlayMode] Re-enabled {disabledObjects.Count} iOS-only GameObjects");
            }

            disabledObjects.Clear();
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
    }
}
