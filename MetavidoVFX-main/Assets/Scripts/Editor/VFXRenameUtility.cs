// VFX Rename Utility - Batch rename VFX assets with proper naming convention
// Naming: {effect}_{target}_{category}_{source}
// Targets: stencil, mesh, depth (omitted if universal)
// Categories: people, face, hands, environment, any
// Sources: rcam2, rcam3, rcam4, nncam2, metavido, akvfx, sdfvfx, h3m (ALWAYS included)

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XRRAI.Editor
{
    public class VFXRenameUtility : EditorWindow
    {
        private Vector2 scrollPos;
        private bool showPreview = true;
        private List<RenameEntry> renameEntries = new List<RenameEntry>();
        private bool initialized = false;

        private class RenameEntry
        {
            public string oldPath;
            public string oldName;
            public string newName;
            public string category;
            public string target;
            public string source;
            public bool selected = true;
            public bool isDuplicate = false;
        }

        [MenuItem("H3M/VFX/Rename VFX Assets (Preview)", false, 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<VFXRenameUtility>("VFX Rename Utility");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        void OnEnable()
        {
            BuildRenameList();
        }

        void BuildRenameList()
        {
            renameEntries.Clear();

            // Find all VFX assets (exclude Samples folder)
            var vfxGuids = AssetDatabase.FindAssets("t:VisualEffectAsset", new[] { "Assets" });
            var allVfx = new List<(string path, string name)>();

            foreach (var guid in vfxGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Skip Unity samples
                if (path.Contains("/Samples/Visual Effect Graph/"))
                    continue;

                string name = Path.GetFileNameWithoutExtension(path);
                allVfx.Add((path, name));
            }

            // Find duplicates by base name
            var nameCounts = allVfx.GroupBy(v => v.name.ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            // Process each VFX
            foreach (var (path, name) in allVfx)
            {
                var entry = new RenameEntry
                {
                    oldPath = path,
                    oldName = name,
                    isDuplicate = nameCounts.Contains(name.ToLower())
                };

                // Determine category, target, and source from path
                ClassifyVFX(entry, path, name);

                // Build new name
                entry.newName = BuildNewName(entry);

                renameEntries.Add(entry);
            }

            // Sort by category then name
            renameEntries = renameEntries
                .OrderBy(e => e.category)
                .ThenBy(e => e.newName)
                .ToList();

            initialized = true;
        }

        void ClassifyVFX(RenameEntry entry, string path, string name)
        {
            string pathLower = path.ToLower();
            string nameLower = name.ToLower();

            // Analyze VFX properties for better classification
            var vfxSignature = AnalyzeVFXProperties(path);

            // Determine source project (ALWAYS required)
            // First check path-based sources
            if (pathLower.Contains("/rcam2/")) entry.source = "rcam2";
            else if (pathLower.Contains("/rcam3/")) entry.source = "rcam3";
            else if (pathLower.Contains("/rcam4/")) entry.source = "rcam4";
            else if (pathLower.Contains("/nncam2/")) entry.source = "nncam2";
            else if (pathLower.Contains("/metavido/")) entry.source = "metavido";
            else if (pathLower.Contains("/akvfx/")) entry.source = "akvfx";
            else if (pathLower.Contains("/sdfvfx/")) entry.source = "sdfvfx";
            else if (pathLower.Contains("/echovision/")) entry.source = "echovision";
            // For ambiguous paths, use VFX property analysis
            else if (pathLower.Contains("/h3m/"))
            {
                // H3M VFX with Metavido signature = derived from metavido
                entry.source = vfxSignature.isMetavidoStyle ? "metavido" : "h3m";
            }
            else if (pathLower.Contains("/environment/"))
            {
                // Environment VFX - check if Rcam-style (has Spawn) or custom
                entry.source = vfxSignature.hasSpawnOnly ? "rcam3" : "h3m";
            }
            else if (pathLower.Contains("/peopleocclusion/"))
            {
                // PeopleOcclusion uses Position Map = h3m custom pipeline
                entry.source = "h3m";
            }
            else if (pathLower.Contains("/resources/vfx/"))
            {
                // Resources VFX with Metavido signature = metavido copy
                entry.source = vfxSignature.isMetavidoStyle ? "metavido" : "h3m";
            }
            else if (pathLower.Contains("/cameraproxy/"))
            {
                // CameraProxy with Metavido signature = metavido debug tool
                entry.source = vfxSignature.isMetavidoStyle ? "metavido" : "h3m";
            }
            else
            {
                // Unknown path - use property analysis
                entry.source = vfxSignature.isMetavidoStyle ? "metavido" : "h3m";
            }

            // Determine category from path structure and VFX properties
            if (pathLower.Contains("/bodyfx/") || pathLower.Contains("/body/"))
                entry.category = "people";
            else if (pathLower.Contains("/envfx/") || pathLower.Contains("/environment/"))
                entry.category = "environment";
            else if (pathLower.Contains("/metavido/"))
                entry.category = "people"; // Metavido is body-focused
            else if (pathLower.Contains("/akvfx/"))
                entry.category = "people"; // Azure Kinect is body-focused
            else if (pathLower.Contains("/h3m/"))
                entry.category = "people"; // Hologram is body-focused
            else if (pathLower.Contains("/sdfvfx/"))
                entry.category = "environment";
            else if (pathLower.Contains("/cameraproxy/"))
                entry.category = "any"; // Debug/visualization tool
            else if (pathLower.Contains("/peopleocclusion/"))
                entry.category = "people";
            else if (pathLower.Contains("/resources/vfx/"))
                entry.category = vfxSignature.usesHumanData ? "people" : "any";
            else
                entry.category = vfxSignature.usesHumanData ? "people" : "any";

            // Determine target from VFX properties (most accurate)
            if (vfxSignature.hasPositionMap && vfxSignature.hasStencilMap)
                entry.target = "stencil"; // Uses computed positions with stencil
            else if (vfxSignature.hasPositionMap)
                entry.target = "stencil"; // Position Map implies stencil pipeline
            else if (vfxSignature.hasStencilMap)
                entry.target = "stencil";
            else if (vfxSignature.hasDepthMap && vfxSignature.hasRayParams)
                entry.target = "depth"; // Raw depth with ray marching
            else if (nameLower.Contains("mesh") || pathLower.Contains("mesh"))
                entry.target = "mesh";
            else if (entry.category == "environment")
                entry.target = ""; // Environment usually doesn't need target
            else if (entry.category == "people")
                entry.target = vfxSignature.hasDepthMap ? "depth" : "stencil";
            else
                entry.target = "";
        }

        struct VFXSignature
        {
            public bool hasDepthMap;
            public bool hasColorMap;
            public bool hasRayParams;
            public bool hasInverseView;
            public bool hasPositionMap;
            public bool hasStencilMap;
            public bool hasSpawnOnly;
            public bool isMetavidoStyle; // DepthMap + RayParams + InverseView (raw depth pipeline)
            public bool usesHumanData;   // Any stencil/position/depth for people
        }

        VFXSignature AnalyzeVFXProperties(string assetPath)
        {
            var sig = new VFXSignature();

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.VFX.VisualEffectAsset>(assetPath);
            if (asset == null) return sig;

            // Check exposed properties by reading the asset file
            string fileContent = "";
            try { fileContent = System.IO.File.ReadAllText(assetPath); }
            catch { return sig; }

            string contentLower = fileContent.ToLower();

            // Check for key properties
            sig.hasDepthMap = contentLower.Contains("depthmap") || contentLower.Contains("depth map");
            sig.hasColorMap = contentLower.Contains("colormap") || contentLower.Contains("color map");
            sig.hasRayParams = contentLower.Contains("rayparams") || contentLower.Contains("ray params");
            sig.hasInverseView = contentLower.Contains("inverseview") || contentLower.Contains("inverse view");
            sig.hasPositionMap = contentLower.Contains("position map") || contentLower.Contains("positionmap");
            sig.hasStencilMap = contentLower.Contains("stencil map") || contentLower.Contains("stencilmap");

            // Check for minimal env VFX (only Spawn exposed)
            bool hasSpawn = contentLower.Contains("\"spawn\"") || contentLower.Contains("name: spawn");
            sig.hasSpawnOnly = hasSpawn && !sig.hasDepthMap && !sig.hasPositionMap && !sig.hasStencilMap;

            // Metavido signature: DepthMap + RayParams + InverseView (raw depth reconstruction)
            sig.isMetavidoStyle = sig.hasDepthMap && sig.hasRayParams && sig.hasInverseView;

            // Uses human data if has stencil, position map, or depth with ray params
            sig.usesHumanData = sig.hasStencilMap || sig.hasPositionMap || sig.isMetavidoStyle;

            return sig;
        }

        string BuildNewName(RenameEntry entry)
        {
            string effectName = entry.oldName.ToLower()
                .Replace(" ", "_")
                .Replace("-", "_");

            // Build name parts: {effect}_{target}_{category}_{source}
            var parts = new List<string>();

            // 1. Effect name (first)
            parts.Add(effectName);

            // 2. Target (if applicable)
            if (!string.IsNullOrEmpty(entry.target))
                parts.Add(entry.target);

            // 3. Category
            parts.Add(entry.category);

            // 4. Source (ALWAYS included)
            if (!string.IsNullOrEmpty(entry.source))
                parts.Add(entry.source);

            return string.Join("_", parts);
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("VFX Rename Utility", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Naming Convention: {effect}_{target}_{category}_{source}\n" +
                "Targets: stencil, mesh, depth (omitted if universal)\n" +
                "Categories: people, face, hands, environment, any\n" +
                "Sources: rcam2, rcam3, rcam4, nncam2, metavido, akvfx, sdfvfx, h3m (ALWAYS included)",
                MessageType.Info);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh List", GUILayout.Width(120)))
                BuildRenameList();
            if (GUILayout.Button("Select All", GUILayout.Width(100)))
                renameEntries.ForEach(e => e.selected = true);
            if (GUILayout.Button("Select None", GUILayout.Width(100)))
                renameEntries.ForEach(e => e.selected = false);
            GUILayout.FlexibleSpace();

            int selectedCount = renameEntries.Count(e => e.selected);
            EditorGUILayout.LabelField($"Selected: {selectedCount}/{renameEntries.Count}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("", GUILayout.Width(20));
            EditorGUILayout.LabelField("Category", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Old Name", EditorStyles.boldLabel, GUILayout.Width(200));
            EditorGUILayout.LabelField("→", GUILayout.Width(20));
            EditorGUILayout.LabelField("New Name", EditorStyles.boldLabel, GUILayout.Width(300));
            EditorGUILayout.LabelField("Dup", GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();

            // Scroll view for entries
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            string lastCategory = "";
            foreach (var entry in renameEntries)
            {
                // Category separator
                if (entry.category != lastCategory)
                {
                    lastCategory = entry.category;
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"── {entry.category.ToUpper()} ──", EditorStyles.centeredGreyMiniLabel);
                }

                EditorGUILayout.BeginHorizontal();

                entry.selected = EditorGUILayout.Toggle(entry.selected, GUILayout.Width(20));

                // Category
                EditorGUILayout.LabelField(entry.category, GUILayout.Width(80));

                // Old name
                EditorGUILayout.LabelField(entry.oldName, GUILayout.Width(200));

                EditorGUILayout.LabelField("→", GUILayout.Width(20));

                // New name (editable)
                entry.newName = EditorGUILayout.TextField(entry.newName, GUILayout.Width(300));

                // Duplicate indicator
                if (entry.isDuplicate)
                    EditorGUILayout.LabelField("⚠", GUILayout.Width(30));
                else
                    EditorGUILayout.LabelField("", GUILayout.Width(30));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Generate Rename Script", GUILayout.Width(180), GUILayout.Height(30)))
            {
                GenerateRenameScript();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"Apply Rename ({selectedCount} files)", GUILayout.Width(180), GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Rename VFX Assets",
                    $"This will rename {selectedCount} VFX assets.\n\n" +
                    "Asset GUIDs will be preserved, so references will remain intact.\n\n" +
                    "Continue?", "Rename", "Cancel"))
                {
                    ApplyRenames();
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        void GenerateRenameScript()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# VFX Rename Plan");
            sb.AppendLine($"# Generated: {System.DateTime.Now}");
            sb.AppendLine($"# Total: {renameEntries.Count} assets");
            sb.AppendLine();

            string lastCategory = "";
            foreach (var entry in renameEntries.Where(e => e.selected))
            {
                if (entry.category != lastCategory)
                {
                    lastCategory = entry.category;
                    sb.AppendLine($"\n## {entry.category.ToUpper()}");
                }

                if (entry.oldName != entry.newName)
                {
                    sb.AppendLine($"- {entry.oldName} → **{entry.newName}**");
                    sb.AppendLine($"  Path: {entry.oldPath}");
                }
            }

            // Save to file
            string outputPath = "Assets/Documentation/VFX_RENAME_PLAN.md";
            File.WriteAllText(outputPath, sb.ToString());
            AssetDatabase.Refresh();

            Debug.Log($"[VFXRenameUtility] Rename plan saved to {outputPath}");
            EditorUtility.DisplayDialog("Rename Plan Generated",
                $"Rename plan saved to:\n{outputPath}", "OK");
        }

        void ApplyRenames()
        {
            int renamed = 0;
            int failed = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var entry in renameEntries.Where(e => e.selected && e.oldName != e.newName))
                {
                    string directory = Path.GetDirectoryName(entry.oldPath);
                    string extension = Path.GetExtension(entry.oldPath);
                    string newPath = Path.Combine(directory, entry.newName + extension);

                    // Use RenameAsset to preserve GUID
                    string error = AssetDatabase.RenameAsset(entry.oldPath, entry.newName);

                    if (string.IsNullOrEmpty(error))
                    {
                        renamed++;
                        Debug.Log($"[VFXRenameUtility] Renamed: {entry.oldName} → {entry.newName}");
                    }
                    else
                    {
                        failed++;
                        Debug.LogError($"[VFXRenameUtility] Failed to rename {entry.oldName}: {error}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[VFXRenameUtility] Rename complete: {renamed} succeeded, {failed} failed");
            EditorUtility.DisplayDialog("Rename Complete",
                $"Renamed {renamed} assets.\n{failed} failures.", "OK");

            // Refresh the list
            BuildRenameList();
        }
    }
}
