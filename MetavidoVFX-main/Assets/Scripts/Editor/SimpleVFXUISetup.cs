// Editor setup for SimpleVFXUI
using UnityEngine;
using UnityEditor;

namespace MetavidoVFX.Editor
{
    public static class SimpleVFXUISetup
    {
        [MenuItem("H3M/VFX UI/Add Simple VFX UI (Metavido Style)")]
        public static void AddSimpleVFXUI()
        {
            // Check if already exists
            var existing = Object.FindFirstObjectByType<UI.SimpleVFXUI>();
            if (existing != null)
            {
                Debug.Log("[SimpleVFXUI] Already exists in scene");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Create GameObject
            var go = new GameObject("SimpleVFXUI");
            var ui = go.AddComponent<UI.SimpleVFXUI>();

            // Configure to use existing SpawnControlVFX
            var serializedObject = new SerializedObject(ui);
            serializedObject.FindProperty("autoPopulateFromResources").boolValue = false;
            serializedObject.ApplyModifiedProperties();

            // Mark dirty
            EditorUtility.SetDirty(go);

            Debug.Log("[SimpleVFXUI] Created SimpleVFXUI - will use SpawnControlVFX_Container");
            Selection.activeGameObject = go;
        }

        [MenuItem("H3M/VFX UI/Switch to Simple UI (Disable Gallery)")]
        public static void SwitchToSimpleUI()
        {
            // Disable VFXGalleryUI
            var galleryUI = Object.FindFirstObjectByType<UI.VFXGalleryUI>();
            if (galleryUI != null)
            {
                galleryUI.gameObject.SetActive(false);
                Debug.Log("[VFX UI] Disabled VFXGalleryUI");
            }

            // Enable/Create SimpleVFXUI
            var simpleUI = Object.FindFirstObjectByType<UI.SimpleVFXUI>(FindObjectsInactive.Include);
            if (simpleUI != null)
            {
                simpleUI.gameObject.SetActive(true);
                Debug.Log("[VFX UI] Enabled SimpleVFXUI");
            }
            else
            {
                AddSimpleVFXUI();
            }
        }

        [MenuItem("H3M/VFX UI/Switch to Gallery UI (Disable Simple)")]
        public static void SwitchToGalleryUI()
        {
            // Disable SimpleVFXUI
            var simpleUI = Object.FindFirstObjectByType<UI.SimpleVFXUI>();
            if (simpleUI != null)
            {
                simpleUI.gameObject.SetActive(false);
                Debug.Log("[VFX UI] Disabled SimpleVFXUI");
            }

            // Enable VFXGalleryUI
            var galleryUI = Object.FindFirstObjectByType<UI.VFXGalleryUI>(FindObjectsInactive.Include);
            if (galleryUI != null)
            {
                galleryUI.gameObject.SetActive(true);
                Debug.Log("[VFX UI] Enabled VFXGalleryUI");
            }
        }
    }
}
