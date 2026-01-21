// VFXCatalogEditor - Editor tools for VFXCatalog
// Populates catalog from project VFX assets

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using MetavidoVFX.VFX;

namespace MetavidoVFX.Editor
{
    [CustomEditor(typeof(VFXCatalog))]
    public class VFXCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var catalog = (VFXCatalog)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("VFX Catalog Tools", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Populate from Project", GUILayout.Height(30)))
                PopulateCatalog(catalog);
            if (GUILayout.Button("Cleanup Nulls", GUILayout.Height(30)))
            {
                int removed = catalog.CleanupNullEntries();
                EditorUtility.SetDirty(catalog);
                Debug.Log($"[VFXCatalog] Removed {removed} null entries");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Total VFX: {catalog.Count}", EditorStyles.miniLabel);

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }

        void PopulateCatalog(VFXCatalog catalog)
        {
            var searchPaths = new[] { "Assets/VFX", "Assets/Resources/VFX" };
            var guids = AssetDatabase.FindAssets("t:VisualEffectAsset", searchPaths);

            int added = 0, updated = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                if (asset == null) continue;

                var category = InferCategory(path, asset.name);
                var existing = catalog.GetEntry(asset.name);

                if (existing == null)
                {
                    catalog.AddOrUpdateEntry(asset, category, InferTags(path));
                    added++;
                }
                else
                {
                    updated++;
                }
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[VFXCatalog] Added {added}, updated {updated} VFX entries");
        }

        VFXCategoryType InferCategory(string path, string name)
        {
            var lower = path.ToLowerInvariant();
            var nameLower = name.ToLowerInvariant();

            if (lower.Contains("/people/") || nameLower.Contains("people") || nameLower.Contains("human") || nameLower.Contains("body"))
                return VFXCategoryType.People;
            if (lower.Contains("/environment/") || nameLower.Contains("environment") || nameLower.Contains("world"))
                return VFXCategoryType.Environment;
            if (lower.Contains("/nncam") || nameLower.Contains("nncam") || nameLower.Contains("keypoint"))
                return VFXCategoryType.People; // NNCam uses body keypoints
            if (lower.Contains("/akvfx") || nameLower.Contains("akvfx") || nameLower.Contains("kinect"))
                return VFXCategoryType.Hybrid; // Depth-based
            if (lower.Contains("/rcam") || nameLower.Contains("rcam"))
                return VFXCategoryType.Hybrid; // Depth-based
            if (lower.Contains("/sdf") || nameLower.Contains("sdf"))
                return VFXCategoryType.Environment;
            if (lower.Contains("/myaku") || nameLower.Contains("myaku"))
                return VFXCategoryType.People;
            if (nameLower.Contains("hand") || nameLower.Contains("finger"))
                return VFXCategoryType.Hands;
            if (nameLower.Contains("audio") || nameLower.Contains("beat") || nameLower.Contains("wave"))
                return VFXCategoryType.Audio;
            if (nameLower.Contains("face"))
                return VFXCategoryType.Face;

            return VFXCategoryType.Hybrid; // Default to hybrid
        }

        string[] InferTags(string path)
        {
            var tags = new List<string>();
            var folder = Path.GetDirectoryName(path)?.Replace("\\", "/") ?? "";

            if (folder.Contains("Rcam2")) tags.Add("rcam2");
            if (folder.Contains("Rcam3")) tags.Add("rcam3");
            if (folder.Contains("Rcam4")) tags.Add("rcam4");
            if (folder.Contains("Akvfx")) tags.Add("akvfx");
            if (folder.Contains("NNCam")) tags.Add("nncam");
            if (folder.Contains("SdfVfx")) tags.Add("sdf");
            if (folder.Contains("Myaku")) tags.Add("myaku");

            return tags.ToArray();
        }

        [MenuItem("H3M/VFX Catalog/Create VFX Catalog")]
        static void CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<VFXCatalog>();
            var path = "Assets/Resources/VFXCatalog.asset";

            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            AssetDatabase.CreateAsset(catalog, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = catalog;
            Debug.Log($"[VFXCatalog] Created at {path}");
        }

        [MenuItem("H3M/VFX Catalog/Populate Existing Catalog")]
        static void PopulateExistingCatalog()
        {
            var guids = AssetDatabase.FindAssets("t:VFXCatalog");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[VFXCatalog] No catalog found. Create one first.");
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var catalog = AssetDatabase.LoadAssetAtPath<VFXCatalog>(path);
            if (catalog != null)
            {
                var editor = CreateEditor(catalog) as VFXCatalogEditor;
                editor?.PopulateCatalog(catalog);
            }
        }

        [MenuItem("H3M/VFX Catalog/Move VFX Out of Resources")]
        static void MoveVFXOutOfResources()
        {
            // Find all VFX in Resources/VFX
            var resourcesPath = "Assets/Resources/VFX";
            var targetPath = "Assets/VFX";

            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                Debug.Log("[VFXCatalog] No Assets/Resources/VFX folder found");
                return;
            }

            // Create target folder
            if (!AssetDatabase.IsValidFolder(targetPath))
                AssetDatabase.CreateFolder("Assets", "VFX");

            // Get subfolders
            var subfolders = AssetDatabase.GetSubFolders(resourcesPath);
            int moved = 0;

            foreach (var subfolder in subfolders)
            {
                var folderName = Path.GetFileName(subfolder);
                var targetSubfolder = $"{targetPath}/{folderName}";

                if (!AssetDatabase.IsValidFolder(targetSubfolder))
                    AssetDatabase.CreateFolder(targetPath, folderName);

                // Move all assets
                var guids = AssetDatabase.FindAssets("", new[] { subfolder });
                foreach (var guid in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.IsValidFolder(assetPath)) continue;

                    var fileName = Path.GetFileName(assetPath);
                    var error = AssetDatabase.MoveAsset(assetPath, $"{targetSubfolder}/{fileName}");
                    if (string.IsNullOrEmpty(error))
                        moved++;
                    else
                        Debug.LogWarning($"[VFXCatalog] Failed to move {assetPath}: {error}");
                }
            }

            // Move root VFX
            var rootGuids = AssetDatabase.FindAssets("t:VisualEffectAsset", new[] { resourcesPath });
            foreach (var guid in rootGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileName(assetPath);
                var error = AssetDatabase.MoveAsset(assetPath, $"{targetPath}/{fileName}");
                if (string.IsNullOrEmpty(error))
                    moved++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[VFXCatalog] Moved {moved} assets from Resources/VFX to Assets/VFX");

            // Repopulate catalog
            PopulateExistingCatalog();
        }
    }
}
