// Editor utility to quickly add VFX Gallery UI to scenes
// Menu: Metavido > Add VFX Gallery UI
// Menu: Metavido > Setup Complete VFX Gallery System

using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;

namespace MetavidoVFX.UI.Editor
{
    public static class VFXGallerySetup
    {
        [MenuItem("Metavido/Add VFX Gallery UI")]
        public static void AddVFXGalleryToScene()
        {
            // Check if gallery already exists
            var existing = Object.FindFirstObjectByType<VFXGalleryUI>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[VFXGallery] Gallery already exists in scene - selected it");
                return;
            }

            // Create gallery object
            GameObject galleryObj = new GameObject("VFXGalleryUI");

            // Add component
            VFXGalleryUI gallery = galleryObj.AddComponent<VFXGalleryUI>();

            // Try to find PeopleOcclusionVFXManager
            var peopleVFX = Object.FindFirstObjectByType<PeopleOcclusionVFXManager>();
            if (peopleVFX != null)
            {
                // Use SerializedObject to set private field
                SerializedObject so = new SerializedObject(gallery);
                so.FindProperty("peopleVFXManager").objectReferenceValue = peopleVFX;
                so.ApplyModifiedProperties();
            }

            // Position in front of camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                galleryObj.transform.position = mainCam.transform.position + mainCam.transform.forward * 0.6f;
            }

            // Select the new object
            Selection.activeGameObject = galleryObj;
            Undo.RegisterCreatedObjectUndo(galleryObj, "Create VFX Gallery UI");

            Debug.Log("[VFXGallery] Created VFX Gallery UI - configure VFX assets in Inspector");
        }

        [MenuItem("Metavido/Setup Complete VFX Gallery System")]
        public static void SetupCompleteVFXSystem()
        {
            Debug.Log("[VFXGallery] Setting up complete VFX Gallery system...");

            // 1. Find or create VFX Gallery
            var gallery = Object.FindFirstObjectByType<VFXGalleryUI>();
            if (gallery == null)
            {
                AddVFXGalleryToScene();
                gallery = Object.FindFirstObjectByType<VFXGalleryUI>();
            }

            if (gallery == null)
            {
                Debug.LogError("[VFXGallery] Failed to create gallery");
                return;
            }

            // 2. Load all VFX from Resources/VFX
            var vfxAssets = Resources.LoadAll<VisualEffectAsset>("VFX");
            if (vfxAssets.Length == 0)
            {
                Debug.LogWarning("[VFXGallery] No VFX assets found in Resources/VFX folder. Copy VFX files there first.");
                return;
            }

            Debug.Log($"[VFXGallery] Found {vfxAssets.Length} VFX assets in Resources/VFX");

            // 3. Clean up ALL existing VFX containers (search entire scene)
            var allContainers = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allContainers)
            {
                if (t != null && t.name == "SpawnControlVFX_Container")
                {
                    Debug.Log($"[VFXGallery] Removing existing container: {t.name}");
                    Undo.DestroyObjectImmediate(t.gameObject);
                }
            }

            // Create single container for VFX instances
            GameObject container = new GameObject("SpawnControlVFX_Container");

            // 4. Find PeopleOcclusionVFXManager for data binding
            var peopleVFXManager = Object.FindFirstObjectByType<PeopleOcclusionVFXManager>();

            // 5. Create VFX instances
            var vfxList = new VisualEffect[vfxAssets.Length];

            for (int i = 0; i < vfxAssets.Length; i++)
            {
                GameObject vfxObj = new GameObject($"VFX_{vfxAssets[i].name}");
                vfxObj.transform.SetParent(container.transform);

                var vfx = vfxObj.AddComponent<VisualEffect>();
                vfx.visualEffectAsset = vfxAssets[i];

                // DISABLE all VFX except first to prevent scene freeze
                bool isFirst = (i == 0);
                vfx.enabled = isFirst;

                if (vfx.HasBool("Spawn"))
                {
                    vfx.SetBool("Spawn", isFirst);
                }

                vfxList[i] = vfx;

                Debug.Log($"[VFXGallery] Created VFX instance: {vfxAssets[i].name} (enabled={isFirst})");
            }

            // 6. Configure gallery via SerializedObject
            SerializedObject so = new SerializedObject(gallery);

            // Set VFX assets array
            var vfxAssetsProp = so.FindProperty("vfxAssets");
            vfxAssetsProp.arraySize = vfxAssets.Length;
            for (int i = 0; i < vfxAssets.Length; i++)
            {
                vfxAssetsProp.GetArrayElementAtIndex(i).objectReferenceValue = vfxAssets[i];
            }

            // Set spawn control list
            var spawnListProp = so.FindProperty("spawnControlVFXList");
            spawnListProp.arraySize = vfxList.Length;
            for (int i = 0; i < vfxList.Length; i++)
            {
                spawnListProp.GetArrayElementAtIndex(i).objectReferenceValue = vfxList[i];
            }

            // Enable spawn control mode
            so.FindProperty("useSpawnControlMode").boolValue = true;
            so.FindProperty("autoPopulateFromResources").boolValue = true;

            // Set PeopleOcclusionVFXManager if found
            if (peopleVFXManager != null)
            {
                so.FindProperty("peopleVFXManager").objectReferenceValue = peopleVFXManager;
            }

            so.ApplyModifiedProperties();

            // 7. Register for undo
            Undo.RegisterCreatedObjectUndo(container, "Setup Complete VFX System");

            // 8. Select the gallery
            Selection.activeGameObject = gallery.gameObject;

            Debug.Log($"[VFXGallery] Complete! Created {vfxList.Length} VFX instances with spawn control.");
            Debug.Log("[VFXGallery] Gallery will show floating cards - gaze + dwell or HoloKit pinch to select.");
        }

        [MenuItem("Metavido/Setup VFX for Spawn Control")]
        public static void SetupVFXSpawnControl()
        {
            var gallery = Object.FindFirstObjectByType<VFXGalleryUI>();
            if (gallery == null)
            {
                Debug.LogWarning("[VFXGallery] No VFXGalleryUI found. Add one first via Metavido > Add VFX Gallery UI");
                return;
            }

            // Find all VFX in Resources/VFX folder
            var vfxAssets = Resources.LoadAll<VisualEffectAsset>("VFX");
            if (vfxAssets.Length == 0)
            {
                Debug.LogWarning("[VFXGallery] No VFX assets found in Resources/VFX folder");
                return;
            }

            Debug.Log($"[VFXGallery] Found {vfxAssets.Length} VFX assets for spawn control");

            // Create container for VFX instances
            GameObject container = new GameObject("SpawnControlVFX_Container");
            container.transform.SetParent(gallery.transform.parent);

            var vfxList = new VisualEffect[vfxAssets.Length];

            for (int i = 0; i < vfxAssets.Length; i++)
            {
                GameObject vfxObj = new GameObject($"VFX_{vfxAssets[i].name}");
                vfxObj.transform.SetParent(container.transform);

                var vfx = vfxObj.AddComponent<VisualEffect>();
                vfx.visualEffectAsset = vfxAssets[i];

                vfxList[i] = vfx;
            }

            // Assign to gallery via SerializedObject
            SerializedObject so = new SerializedObject(gallery);
            var spawnListProp = so.FindProperty("spawnControlVFXList");
            spawnListProp.arraySize = vfxList.Length;
            for (int i = 0; i < vfxList.Length; i++)
            {
                spawnListProp.GetArrayElementAtIndex(i).objectReferenceValue = vfxList[i];
            }
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(container, "Setup VFX Spawn Control");

            Debug.Log($"[VFXGallery] Created {vfxList.Length} VFX instances for spawn control");
        }

        [MenuItem("Metavido/Copy More VFX to Resources")]
        public static void CopyMoreVFXToResources()
        {
            // Show a dialog to copy VFX assets to Resources/VFX
            string sourcePath = EditorUtility.OpenFolderPanel("Select VFX Source Folder", "Assets/VFX", "");
            if (string.IsNullOrEmpty(sourcePath)) return;

            string destPath = "Assets/Resources/VFX";

            // Ensure destination exists
            if (!AssetDatabase.IsValidFolder(destPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "VFX");
            }

            // Find all .vfx files
            string[] vfxFiles = System.IO.Directory.GetFiles(sourcePath, "*.vfx", System.IO.SearchOption.AllDirectories);

            int copied = 0;
            foreach (string file in vfxFiles)
            {
                string fileName = System.IO.Path.GetFileName(file);
                string relativePath = file.Replace(Application.dataPath, "Assets");
                string destFile = $"{destPath}/{fileName}";

                if (!System.IO.File.Exists(destFile.Replace("Assets", Application.dataPath)))
                {
                    AssetDatabase.CopyAsset(relativePath, destFile);
                    copied++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[VFXGallery] Copied {copied} VFX files to Resources/VFX");
        }

        [MenuItem("Metavido/Cleanup Duplicate VFX Containers")]
        public static void CleanupDuplicateContainers()
        {
            int removed = 0;

            // Clean up SpawnControlVFX_Container - keep the ACTIVE one
            removed += CleanupDuplicatesByName("SpawnControlVFX_Container");

            // Clean up duplicate VFXGalleryUI - keep the ACTIVE one
            var galleries = Object.FindObjectsByType<VFXGalleryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (galleries.Length > 1)
            {
                // Find the active one first
                VFXGalleryUI toKeep = null;
                foreach (var g in galleries)
                {
                    if (g.gameObject.activeInHierarchy)
                    {
                        toKeep = g;
                        break;
                    }
                }
                // If none active, keep the first one
                if (toKeep == null) toKeep = galleries[0];

                foreach (var g in galleries)
                {
                    if (g != toKeep)
                    {
                        Debug.Log($"[VFXGallery] Removing duplicate VFXGalleryUI (active={g.gameObject.activeInHierarchy})");
                        Undo.DestroyObjectImmediate(g.gameObject);
                        removed++;
                    }
                }
                Debug.Log($"[VFXGallery] Kept VFXGalleryUI (active={toKeep.gameObject.activeInHierarchy})");
            }

            Debug.Log($"[VFXGallery] Cleanup complete - removed {removed} duplicate objects");
        }

        /// <summary>
        /// Clean up duplicates by name, keeping the ACTIVE one.
        /// </summary>
        static int CleanupDuplicatesByName(string objectName)
        {
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var matches = new System.Collections.Generic.List<Transform>();

            foreach (var t in allTransforms)
            {
                if (t != null && t.name == objectName)
                {
                    matches.Add(t);
                }
            }

            if (matches.Count <= 1) return 0;

            // Find the active one to keep
            Transform toKeep = null;
            foreach (var t in matches)
            {
                if (t.gameObject.activeInHierarchy)
                {
                    toKeep = t;
                    break;
                }
            }
            // If none active, keep the first one
            if (toKeep == null) toKeep = matches[0];

            int removed = 0;
            foreach (var t in matches)
            {
                if (t != toKeep)
                {
                    Debug.Log($"[VFXGallery] Removing duplicate {objectName} (active={t.gameObject.activeInHierarchy})");
                    Undo.DestroyObjectImmediate(t.gameObject);
                    removed++;
                }
            }
            Debug.Log($"[VFXGallery] Kept {objectName} (active={toKeep.gameObject.activeInHierarchy})");
            return removed;
        }

        [MenuItem("Metavido/Cleanup All Scene Duplicates")]
        public static void CleanupAllDuplicates()
        {
            Debug.Log("[Cleanup] Starting comprehensive duplicate cleanup...");
            int total = 0;

            // VFX containers
            total += CleanupDuplicatesByName("SpawnControlVFX_Container");

            // Hand Tracking Manager
            total += CleanupDuplicatesByName("Hand Tracking Manager");

            // Gaze Interaction
            total += CleanupDuplicatesByName("Gaze Interaction");

            // XR Origins - keep the NAMED one without "(1)"
            var xrOrigins = Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (xrOrigins.Length > 1)
            {
                Unity.XR.CoreUtils.XROrigin toKeep = null;
                foreach (var xr in xrOrigins)
                {
                    // Prefer non-numbered, active ones
                    if (!xr.gameObject.name.Contains("(") && xr.gameObject.activeInHierarchy)
                    {
                        toKeep = xr;
                        break;
                    }
                }
                // Fallback: keep first non-numbered
                if (toKeep == null)
                {
                    foreach (var xr in xrOrigins)
                    {
                        if (!xr.gameObject.name.Contains("("))
                        {
                            toKeep = xr;
                            break;
                        }
                    }
                }
                // Fallback: just keep first
                if (toKeep == null) toKeep = xrOrigins[0];

                foreach (var xr in xrOrigins)
                {
                    if (xr != toKeep)
                    {
                        Debug.Log($"[Cleanup] Removing duplicate XR Origin: {xr.gameObject.name}");
                        Undo.DestroyObjectImmediate(xr.gameObject);
                        total++;
                    }
                }
                Debug.Log($"[Cleanup] Kept XR Origin: {toKeep.gameObject.name}");
            }

            // VFXGalleryUI
            var galleries = Object.FindObjectsByType<VFXGalleryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (galleries.Length > 1)
            {
                VFXGalleryUI toKeep = null;
                foreach (var g in galleries)
                {
                    if (g.gameObject.activeInHierarchy)
                    {
                        toKeep = g;
                        break;
                    }
                }
                if (toKeep == null) toKeep = galleries[0];

                foreach (var g in galleries)
                {
                    if (g != toKeep)
                    {
                        Debug.Log($"[Cleanup] Removing duplicate VFXGalleryUI");
                        Undo.DestroyObjectImmediate(g.gameObject);
                        total++;
                    }
                }
            }

            Debug.Log($"[Cleanup] ✓ Complete - removed {total} duplicate objects");
            Debug.Log("[Cleanup] Remember to save the scene!");
        }
    }
}
