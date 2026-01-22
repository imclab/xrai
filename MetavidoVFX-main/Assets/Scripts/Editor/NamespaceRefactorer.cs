// NamespaceRefactorer.cs - Consolidate namespaces for easy migration
// Reduces 20+ namespaces to 6 migration-ready feature namespaces
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace MetavidoVFX.Editor
{
    public static class NamespaceRefactorer
    {
        // Mapping: old namespace patterns → new namespace (XRRAI = XR Real-time AI)
        private static readonly Dictionary<string, string> NamespaceMappings = new()
        {
            // HandTracking consolidation → XRRAI.HandTracking
            { "MetavidoVFX.HandTracking.Providers", "XRRAI.HandTracking" },
            { "MetavidoVFX.HandTracking.Gestures", "XRRAI.HandTracking" },
            { "MetavidoVFX.HandTracking.Mappers", "XRRAI.HandTracking" },
            { "MetavidoVFX.HandTracking", "XRRAI.HandTracking" },

            // VoiceToObject (Icosa) consolidation → XRRAI.VoiceToObject
            { "MetavidoVFX.Icosa", "XRRAI.VoiceToObject" },

            // BrushPainting consolidation → XRRAI.BrushPainting
            { "MetavidoVFX.Painting", "XRRAI.BrushPainting" },
            { "H3M.Painting", "XRRAI.BrushPainting" },

            // VFXBinders consolidation → XRRAI.VFXBinders
            { "MetavidoVFX.VFX.Binders", "XRRAI.VFXBinders" },
            { "MetavidoVFX.VFX", "XRRAI.VFXBinders" },

            // Hologram consolidation → XRRAI.Hologram
            { "MetavidoVFX.H3M.Network", "XRRAI.Hologram" },
            { "MetavidoVFX.H3M.VFX", "XRRAI.Hologram" },
            { "H3M.Network", "XRRAI.Hologram" },
            { "H3M.Core", "XRRAI.Hologram" },

            // ARTracking consolidation → XRRAI.ARTracking
            { "MetavidoVFX.Tracking.Providers", "XRRAI.ARTracking" },
            { "MetavidoVFX.Tracking", "XRRAI.ARTracking" },

            // Additional namespaces → XRRAI.*
            { "MetavidoVFX.Audio", "XRRAI.Audio" },
            { "MetavidoVFX.Performance", "XRRAI.Performance" },
            { "MetavidoVFX.Debugging", "XRRAI.Debug" },
            { "MetavidoVFX.Recording", "XRRAI.Recording" },
            { "MetavidoVFX.Testing", "XRRAI.Testing" },
            { "MetavidoVFX.Editor", "XRRAI.Editor" },
            { "MetavidoVFX", "XRRAI" },
            { "Metavido.Diagnostics", "XRRAI.Debug" },
        };

        [MenuItem("H3M/Refactor/Preview Namespace Changes")]
        public static void PreviewChanges()
        {
            Debug.Log("=== XRRAI Namespace Refactoring Preview ===");
            Debug.Log("Changes will consolidate 20+ namespaces into migration-ready XRRAI.* namespaces:");
            Debug.Log("");
            Debug.Log("Target namespaces (XRRAI = XR Real-time AI):");
            Debug.Log("  1. XRRAI.HandTracking  - Hand tracking providers, gestures, mappers");
            Debug.Log("  2. XRRAI.VoiceToObject - Voice commands, Icosa/Sketchfab search");
            Debug.Log("  3. XRRAI.BrushPainting - Brush strokes, painting system");
            Debug.Log("  4. XRRAI.VFXBinders    - AR→VFX data binding");
            Debug.Log("  5. XRRAI.Hologram      - Hologram core and network");
            Debug.Log("  6. XRRAI.ARTracking    - Tracking providers and data");
            Debug.Log("  7. XRRAI.Audio         - Audio processing");
            Debug.Log("  8. XRRAI.Performance   - FPS/LOD optimization");
            Debug.Log("  9. XRRAI.Debug         - Debugging utilities");
            Debug.Log(" 10. XRRAI.Testing       - Test harnesses");
            Debug.Log(" 11. XRRAI               - Core types");
            Debug.Log("");

            var scriptsPath = "Assets/Scripts";
            var h3mPath = "Assets/H3M";
            int fileCount = 0;

            foreach (var mapping in NamespaceMappings)
            {
                var files = FindFilesWithNamespace(mapping.Key, scriptsPath, h3mPath);
                if (files.Count > 0)
                {
                    Debug.Log($"{mapping.Key} → {mapping.Value} ({files.Count} files)");
                    fileCount += files.Count;
                }
            }

            Debug.Log($"\nTotal files to refactor: {fileCount}");
            Debug.Log("\nRun 'H3M > Refactor > Execute Namespace Consolidation' to apply changes");
        }

        [MenuItem("H3M/Refactor/Execute Namespace Consolidation")]
        public static void ExecuteRefactor()
        {
            if (!EditorUtility.DisplayDialog(
                "XRRAI Namespace Refactoring",
                "This will consolidate 20+ namespaces into XRRAI.* migration-ready namespaces.\n\n" +
                "Brand: H3M → XRRAI (XR Real-time AI)\n\n" +
                "This is a significant change. Make sure you have committed your current changes.\n\n" +
                "Continue?",
                "Yes, Refactor to XRRAI",
                "Cancel"))
            {
                return;
            }

            var scriptsPath = Path.Combine(Application.dataPath, "Scripts");
            var h3mPath = Path.Combine(Application.dataPath, "H3M");
            int filesChanged = 0;
            int errorsCount = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                // Process each mapping (order matters - more specific first)
                var orderedMappings = new List<KeyValuePair<string, string>>(NamespaceMappings);
                orderedMappings.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length)); // Longest first

                foreach (var mapping in orderedMappings)
                {
                    var files = FindFilesWithNamespaceFullPath(mapping.Key, scriptsPath, h3mPath);
                    foreach (var file in files)
                    {
                        try
                        {
                            if (RefactorFile(file, mapping.Key, mapping.Value))
                            {
                                filesChanged++;
                                Debug.Log($"[Refactor] {Path.GetFileName(file)}: {mapping.Key} → {mapping.Value}");
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"[Refactor] Error processing {file}: {e.Message}");
                            errorsCount++;
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"=== Namespace Refactoring Complete ===");
            Debug.Log($"Files changed: {filesChanged}");
            Debug.Log($"Errors: {errorsCount}");

            if (errorsCount == 0)
            {
                Debug.Log("Next: Check for compilation errors and fix any missing 'using' statements");
            }
        }

        private static List<string> FindFilesWithNamespace(string ns, params string[] searchPaths)
        {
            var results = new List<string>();
            foreach (var basePath in searchPaths)
            {
                var fullPath = Path.Combine(Application.dataPath, basePath.Replace("Assets/", ""));
                if (!Directory.Exists(fullPath)) continue;

                foreach (var file in Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories))
                {
                    var content = File.ReadAllText(file);
                    if (Regex.IsMatch(content, $@"^namespace\s+{Regex.Escape(ns)}\s*$", RegexOptions.Multiline))
                    {
                        results.Add(file.Replace(Application.dataPath, "Assets"));
                    }
                }
            }
            return results;
        }

        private static List<string> FindFilesWithNamespaceFullPath(string ns, params string[] searchPaths)
        {
            var results = new List<string>();
            foreach (var basePath in searchPaths)
            {
                if (!Directory.Exists(basePath)) continue;

                foreach (var file in Directory.GetFiles(basePath, "*.cs", SearchOption.AllDirectories))
                {
                    var content = File.ReadAllText(file);
                    if (Regex.IsMatch(content, $@"^namespace\s+{Regex.Escape(ns)}\s*$", RegexOptions.Multiline))
                    {
                        results.Add(file);
                    }
                }
            }
            return results;
        }

        private static bool RefactorFile(string filePath, string oldNamespace, string newNamespace)
        {
            var content = File.ReadAllText(filePath);
            var originalContent = content;

            // Replace namespace declaration
            content = Regex.Replace(
                content,
                $@"^namespace\s+{Regex.Escape(oldNamespace)}\s*$",
                $"namespace {newNamespace}",
                RegexOptions.Multiline
            );

            // Replace using statements
            content = Regex.Replace(
                content,
                $@"^using\s+{Regex.Escape(oldNamespace)}\s*;",
                $"using {newNamespace};",
                RegexOptions.Multiline
            );

            if (content != originalContent)
            {
                File.WriteAllText(filePath, content);
                return true;
            }
            return false;
        }

        [MenuItem("H3M/Refactor/Fix Missing Usings After Refactor")]
        public static void FixMissingUsings()
        {
            Debug.Log("=== Fixing Missing Using Statements ===");

            var scriptsPath = Path.Combine(Application.dataPath, "Scripts");
            var h3mPath = Path.Combine(Application.dataPath, "H3M");
            int filesFixed = 0;

            var allCsFiles = new List<string>();
            if (Directory.Exists(scriptsPath))
                allCsFiles.AddRange(Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories));
            if (Directory.Exists(h3mPath))
                allCsFiles.AddRange(Directory.GetFiles(h3mPath, "*.cs", SearchOption.AllDirectories));

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var file in allCsFiles)
                {
                    var content = File.ReadAllText(file);
                    var originalContent = content;
                    bool needsSave = false;

                    // Add using statements for new namespaces if types from them are used
                    foreach (var mapping in NamespaceMappings)
                    {
                        var newNs = mapping.Value;
                        // Skip if already has the using
                        if (content.Contains($"using {newNs};")) continue;

                        // Check if namespace is the file's own namespace (don't add using for own namespace)
                        if (Regex.IsMatch(content, $@"^namespace\s+{Regex.Escape(newNs)}\s*$", RegexOptions.Multiline))
                            continue;

                        // If old namespace was used, might need new one
                        if (content.Contains($"using {mapping.Key};"))
                        {
                            // Replace old using with new
                            content = content.Replace($"using {mapping.Key};", $"using {newNs};");
                            needsSave = true;
                        }
                    }

                    if (needsSave && content != originalContent)
                    {
                        File.WriteAllText(file, content);
                        filesFixed++;
                        Debug.Log($"[FixUsings] Updated: {Path.GetFileName(file)}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"Fixed using statements in {filesFixed} files");
        }
    }
}
#endif
