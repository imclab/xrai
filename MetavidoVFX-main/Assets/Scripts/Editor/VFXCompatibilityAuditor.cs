// VFXCompatibilityAuditor - Analyzes VFX assets for mode compatibility (spec-007 T-011)
// Scans all VFX in project and generates compatibility matrix

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MetavidoVFX.VFX;

namespace MetavidoVFX.Editor
{
    public class VFXCompatibilityAuditor : EditorWindow
    {
        // Audit results
        List<VFXAuditResult> _results = new List<VFXAuditResult>();
        Vector2 _scrollPosition;
        bool _showOnlyResources = true;
        string _filterText = "";
        VFXCategoryType? _filterMode = null;

        // Additional input mapping toggle
        bool _enableCustomInputMapping = false;
        bool _showCustomMappingSection = false;
        string _customDepthProperty = "";
        string _customStencilProperty = "";
        string _customColorProperty = "";
        string _customPositionProperty = "";
        string _customAudioProperty = "";

        // Property detection patterns
        static readonly string[] DepthProperties = { "DepthMap", "Depth Map", "DepthTexture", "_Depth" };
        static readonly string[] StencilProperties = { "StencilMap", "Stencil Map", "StencilTexture", "HumanStencil" };
        static readonly string[] ColorProperties = { "ColorMap", "Color Map", "ColorTexture", "CameraColor" };
        static readonly string[] PositionProperties = { "PositionMap", "Position Map", "WorldPosition" };
        static readonly string[] AudioProperties = { "AudioBass", "AudioVolume", "BeatPulse", "BeatIntensity" };
        static readonly string[] HandProperties = { "HandPosition", "HandVelocity", "KeypointBuffer" };
        static readonly string[] FaceProperties = { "FaceMesh", "BlendShapes", "FacePosition" };
        static readonly string[] PhysicsProperties = { "Velocity", "Gravity", "BounceFactor", "MeshPointCount" };

        [MenuItem("H3M/VFX Pipeline Master/Audit/Audit VFX Compatibility")]
        public static void ShowWindow()
        {
            var window = GetWindow<VFXCompatibilityAuditor>("VFX Compatibility Audit");
            window.minSize = new Vector2(800, 500);
        }

        [MenuItem("H3M/VFX Pipeline Master/Audit/Run Audit (Console Output)")]
        public static void RunAuditToConsole()
        {
            var results = new List<VFXAuditResult>();
            string[] guids = AssetDatabase.FindAssets("t:VisualEffectAsset");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Focus on Resources/VFX
                if (!path.Contains("Resources/VFX"))
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                if (asset != null)
                {
                    var result = AnalyzeVFXStatic(asset, path);
                    results.Add(result);
                }
            }

            // Sort and output
            results = results.OrderBy(r => r.PrimaryMode).ThenBy(r => r.Name).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine("   VFX COMPATIBILITY AUDIT REPORT");
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine($"  Total VFX in Resources/VFX: {results.Count}");
            sb.AppendLine();

            // Mode stats
            var stats = new Dictionary<VFXCategoryType, int>();
            foreach (VFXCategoryType mode in System.Enum.GetValues(typeof(VFXCategoryType)))
                stats[mode] = results.Count(r => r.PrimaryMode == mode);

            sb.AppendLine("  Mode Distribution:");
            foreach (var kv in stats.OrderByDescending(x => x.Value))
            {
                if (kv.Value > 0)
                    sb.AppendLine($"    {kv.Key}: {kv.Value}");
            }
            sb.AppendLine();

            sb.AppendLine("───────────────────────────────────────────────────────────");
            sb.AppendLine("   VFX DETAILS");
            sb.AppendLine("───────────────────────────────────────────────────────────");

            foreach (var r in results)
            {
                string features = string.Join(", ", new[] {
                    r.HasDepth ? "Depth" : null,
                    r.HasStencil ? "Stencil" : null,
                    r.HasColor ? "Color" : null,
                    r.HasAudio ? "Audio" : null,
                    r.HasPhysics ? "Physics" : null
                }.Where(s => s != null));

                sb.AppendLine($"  {r.Name}");
                sb.AppendLine($"    Mode: {r.PrimaryMode} | Supports: {string.Join(", ", r.SupportedModes)}");
                if (!string.IsNullOrEmpty(features))
                    sb.AppendLine($"    Features: {features}");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════");

            // Write to file for easy reading
            string outputPath = "Assets/Documentation/VFX_COMPATIBILITY_AUDIT.md";
            System.IO.File.WriteAllText(outputPath, sb.ToString().Replace("═", "=").Replace("─", "-"));
            AssetDatabase.Refresh();
            Debug.Log($"[VFXCompatibilityAuditor] Audit complete! Report saved to: {outputPath}");
            Debug.Log($"[VFXCompatibilityAuditor] Total: {results.Count} VFX in Resources/VFX");

            // Also show summary in console
            Debug.Log($"Mode Distribution: People={stats.GetValueOrDefault(VFXCategoryType.People)}, " +
                     $"Environment={stats.GetValueOrDefault(VFXCategoryType.Environment)}, " +
                     $"Hands={stats.GetValueOrDefault(VFXCategoryType.Hands)}, " +
                     $"Audio={stats.GetValueOrDefault(VFXCategoryType.Audio)}, " +
                     $"Hybrid={stats.GetValueOrDefault(VFXCategoryType.Hybrid)}");
        }

        static VFXAuditResult AnalyzeVFXStatic(VisualEffectAsset asset, string path)
        {
            var result = new VFXAuditResult
            {
                Name = asset.name,
                Path = path,
                SupportedModes = new List<VFXCategoryType>()
            };

            string nameLower = asset.name.ToLower();
            string pathLower = path.ToLower();

            result.HasDepth = nameLower.Contains("depth") || pathLower.Contains("depth");
            result.HasStencil = nameLower.Contains("stencil") || pathLower.Contains("stencil");
            result.HasColor = nameLower.Contains("color") || nameLower.Contains("camera");

            if (nameLower.Contains("people") || pathLower.Contains("/people/"))
            {
                result.PrimaryMode = VFXCategoryType.People;
                result.SupportedModes.Add(VFXCategoryType.People);
            }
            else if (nameLower.Contains("environment") || pathLower.Contains("/environment/"))
            {
                result.PrimaryMode = VFXCategoryType.Environment;
                result.SupportedModes.Add(VFXCategoryType.Environment);
            }
            else if (nameLower.Contains("hand") || pathLower.Contains("/hands/"))
            {
                result.PrimaryMode = VFXCategoryType.Hands;
                result.SupportedModes.Add(VFXCategoryType.Hands);
                result.HasHands = true;
            }
            else if (nameLower.Contains("audio") || pathLower.Contains("/audio/"))
            {
                result.PrimaryMode = VFXCategoryType.Audio;
                result.SupportedModes.Add(VFXCategoryType.Audio);
                result.HasAudio = true;
            }
            else
            {
                result.PrimaryMode = VFXCategoryType.People;
                result.SupportedModes.Add(VFXCategoryType.People);
            }

            if (pathLower.Contains("/akvfx/") || pathLower.Contains("/rcam"))
            {
                result.HasDepth = true;
                if (!result.SupportedModes.Contains(VFXCategoryType.People))
                    result.SupportedModes.Add(VFXCategoryType.People);
            }

            if (pathLower.Contains("/nncam"))
            {
                result.HasHands = true;
                result.PrimaryMode = VFXCategoryType.Hybrid;
                if (!result.SupportedModes.Contains(VFXCategoryType.Hybrid))
                    result.SupportedModes.Add(VFXCategoryType.Hybrid);
            }

            if (result.HasDepth && !result.HasStencil)
            {
                if (!result.SupportedModes.Contains(VFXCategoryType.Environment))
                    result.SupportedModes.Add(VFXCategoryType.Environment);
            }

            if (!result.SupportedModes.Contains(VFXCategoryType.Hybrid))
                result.SupportedModes.Add(VFXCategoryType.Hybrid);

            return result;
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("VFX Compatibility Auditor (spec-007)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Analyzes all VFX assets for mode compatibility based on exposed properties and naming conventions.", MessageType.Info);

            EditorGUILayout.Space(5);

            // Controls
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run Full Audit", GUILayout.Height(30), GUILayout.Width(150)))
                {
                    RunAudit();
                }

                if (GUILayout.Button("Export CSV", GUILayout.Height(30), GUILayout.Width(100)))
                {
                    ExportCSV();
                }

                if (GUILayout.Button("Export Markdown", GUILayout.Height(30), GUILayout.Width(120)))
                {
                    ExportMarkdown();
                }
            }

            EditorGUILayout.Space(5);

            // Filters
            using (new EditorGUILayout.HorizontalScope())
            {
                _showOnlyResources = EditorGUILayout.ToggleLeft("Resources/VFX only", _showOnlyResources, GUILayout.Width(140));
                EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
                _filterText = EditorGUILayout.TextField(_filterText, GUILayout.Width(150));

                EditorGUILayout.LabelField("Mode:", GUILayout.Width(40));
                string[] modeOptions = new[] { "All", "People", "Environment", "Face", "Hands", "Audio", "Hybrid" };
                int selected = _filterMode.HasValue ? (int)_filterMode.Value + 1 : 0;
                int newSelected = EditorGUILayout.Popup(selected, modeOptions, GUILayout.Width(100));
                _filterMode = newSelected == 0 ? null : (VFXCategoryType?)(newSelected - 1);
            }

            // Custom Input Mapping Section
            EditorGUILayout.Space(5);
            _showCustomMappingSection = EditorGUILayout.Foldout(_showCustomMappingSection, "Additional Input Mapping", true);
            if (_showCustomMappingSection)
            {
                EditorGUI.indentLevel++;
                _enableCustomInputMapping = EditorGUILayout.ToggleLeft("Enable Custom Property Detection", _enableCustomInputMapping);

                if (_enableCustomInputMapping)
                {
                    EditorGUILayout.HelpBox("Add custom property names to detect during audit. Leave empty to skip.", MessageType.Info);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Depth:", GUILayout.Width(60));
                        _customDepthProperty = EditorGUILayout.TextField(_customDepthProperty, GUILayout.Width(150));
                        EditorGUILayout.LabelField("Stencil:", GUILayout.Width(60));
                        _customStencilProperty = EditorGUILayout.TextField(_customStencilProperty, GUILayout.Width(150));
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Color:", GUILayout.Width(60));
                        _customColorProperty = EditorGUILayout.TextField(_customColorProperty, GUILayout.Width(150));
                        EditorGUILayout.LabelField("Position:", GUILayout.Width(60));
                        _customPositionProperty = EditorGUILayout.TextField(_customPositionProperty, GUILayout.Width(150));
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Audio:", GUILayout.Width(60));
                        _customAudioProperty = EditorGUILayout.TextField(_customAudioProperty, GUILayout.Width(150));
                    }
                }
                EditorGUI.indentLevel--;
            }

            // Stats
            if (_results.Count > 0)
            {
                EditorGUILayout.Space(5);
                var stats = GetStats();
                EditorGUILayout.LabelField($"Total: {_results.Count} | " +
                    $"People: {stats.GetValueOrDefault(VFXCategoryType.People)} | " +
                    $"Environment: {stats.GetValueOrDefault(VFXCategoryType.Environment)} | " +
                    $"Hands: {stats.GetValueOrDefault(VFXCategoryType.Hands)} | " +
                    $"Audio: {stats.GetValueOrDefault(VFXCategoryType.Audio)} | " +
                    $"Hybrid: {stats.GetValueOrDefault(VFXCategoryType.Hybrid)}");
            }

            EditorGUILayout.Space(10);

            // Results table
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Header
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("VFX Name", EditorStyles.boldLabel, GUILayout.Width(250));
                EditorGUILayout.LabelField("Primary Mode", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("Depth", EditorStyles.boldLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField("Stencil", EditorStyles.boldLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField("Color", EditorStyles.boldLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField("Physics", EditorStyles.boldLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField("Supported Modes", EditorStyles.boldLabel, GUILayout.Width(200));
            }

            // Rows
            var filtered = GetFilteredResults();
            foreach (var result in filtered)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Name (clickable to select asset)
                    if (GUILayout.Button(result.Name, EditorStyles.linkLabel, GUILayout.Width(250)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(result.Path);
                        if (asset != null)
                        {
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                        }
                    }

                    EditorGUILayout.LabelField(result.PrimaryMode.ToString(), GUILayout.Width(100));
                    DrawCheckmark(result.HasDepth, 50);
                    DrawCheckmark(result.HasStencil, 50);
                    DrawCheckmark(result.HasColor, 50);
                    DrawCheckmark(result.HasAudio, 50);
                    DrawCheckmark(result.HasPhysics, 50);
                    EditorGUILayout.LabelField(string.Join(", ", result.SupportedModes.Select(m => m.ToString().Substring(0, 3))), GUILayout.Width(200));
                }
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawCheckmark(bool value, float width)
        {
            var color = value ? Color.green : Color.gray;
            var oldColor = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField(value ? "✓" : "–", GUILayout.Width(width));
            GUI.color = oldColor;
        }

        void RunAudit()
        {
            _results.Clear();

            string[] guids = AssetDatabase.FindAssets("t:VisualEffectAsset");
            int total = guids.Length;
            int processed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Filter to Resources/VFX if enabled
                if (_showOnlyResources && !path.Contains("Resources/VFX"))
                {
                    processed++;
                    continue;
                }

                EditorUtility.DisplayProgressBar("Auditing VFX", path, (float)processed / total);

                var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                if (asset != null)
                {
                    var result = AnalyzeVFX(asset, path);
                    _results.Add(result);
                }

                processed++;
            }

            EditorUtility.ClearProgressBar();

            // Sort by primary mode, then name
            _results = _results.OrderBy(r => r.PrimaryMode).ThenBy(r => r.Name).ToList();

            Debug.Log($"[VFXCompatibilityAuditor] Audited {_results.Count} VFX assets");
        }

        VFXAuditResult AnalyzeVFX(VisualEffectAsset asset, string path)
        {
            var result = new VFXAuditResult
            {
                Name = asset.name,
                Path = path,
                SupportedModes = new List<VFXCategoryType>()
            };

            // Detect properties from naming convention (fastest, most reliable for this project)
            string nameLower = asset.name.ToLower();
            string pathLower = path.ToLower();

            // Check naming conventions
            result.HasDepth = nameLower.Contains("depth") || pathLower.Contains("depth");
            result.HasStencil = nameLower.Contains("stencil") || pathLower.Contains("stencil");
            result.HasColor = nameLower.Contains("color") || nameLower.Contains("camera");

            // Mode from name/path
            if (nameLower.Contains("people") || pathLower.Contains("/people/"))
            {
                result.PrimaryMode = VFXCategoryType.People;
                result.SupportedModes.Add(VFXCategoryType.People);
            }
            else if (nameLower.Contains("environment") || pathLower.Contains("/environment/"))
            {
                result.PrimaryMode = VFXCategoryType.Environment;
                result.SupportedModes.Add(VFXCategoryType.Environment);
            }
            else if (nameLower.Contains("hand") || pathLower.Contains("/hands/"))
            {
                result.PrimaryMode = VFXCategoryType.Hands;
                result.SupportedModes.Add(VFXCategoryType.Hands);
                result.HasHands = true;
            }
            else if (nameLower.Contains("face") || pathLower.Contains("/face/"))
            {
                result.PrimaryMode = VFXCategoryType.Face;
                result.SupportedModes.Add(VFXCategoryType.Face);
                result.HasFace = true;
            }
            else if (nameLower.Contains("audio") || pathLower.Contains("/audio/"))
            {
                result.PrimaryMode = VFXCategoryType.Audio;
                result.SupportedModes.Add(VFXCategoryType.Audio);
                result.HasAudio = true;
            }
            else if (nameLower.Contains("any") || nameLower.Contains("hybrid"))
            {
                result.PrimaryMode = VFXCategoryType.Hybrid;
                result.SupportedModes.Add(VFXCategoryType.Hybrid);
            }
            else
            {
                // Default to People for unspecified
                result.PrimaryMode = VFXCategoryType.People;
                result.SupportedModes.Add(VFXCategoryType.People);
            }

            // Check folder for additional categorization
            if (pathLower.Contains("/akvfx/") || pathLower.Contains("/rcam"))
            {
                // These are depth-based people VFX
                result.HasDepth = true;
                if (!result.SupportedModes.Contains(VFXCategoryType.People))
                    result.SupportedModes.Add(VFXCategoryType.People);
            }

            if (pathLower.Contains("/nncam"))
            {
                // NNCam VFX use keypoints
                result.HasHands = true;
                result.PrimaryMode = VFXCategoryType.Hybrid;
                if (!result.SupportedModes.Contains(VFXCategoryType.Hybrid))
                    result.SupportedModes.Add(VFXCategoryType.Hybrid);
            }

            // Depth-based VFX can usually work with both People and Environment (with different data)
            if (result.HasDepth && !result.HasStencil)
            {
                if (!result.SupportedModes.Contains(VFXCategoryType.Environment))
                    result.SupportedModes.Add(VFXCategoryType.Environment);
            }

            // Most VFX can support Hybrid mode
            if (!result.SupportedModes.Contains(VFXCategoryType.Hybrid))
                result.SupportedModes.Add(VFXCategoryType.Hybrid);

            return result;
        }

        List<VFXAuditResult> GetFilteredResults()
        {
            var filtered = _results.AsEnumerable();

            if (!string.IsNullOrEmpty(_filterText))
            {
                filtered = filtered.Where(r => r.Name.ToLower().Contains(_filterText.ToLower()));
            }

            if (_filterMode.HasValue)
            {
                filtered = filtered.Where(r => r.SupportedModes.Contains(_filterMode.Value));
            }

            return filtered.ToList();
        }

        Dictionary<VFXCategoryType, int> GetStats()
        {
            var stats = new Dictionary<VFXCategoryType, int>();
            foreach (VFXCategoryType mode in System.Enum.GetValues(typeof(VFXCategoryType)))
            {
                stats[mode] = _results.Count(r => r.PrimaryMode == mode);
            }
            return stats;
        }

        void ExportCSV()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name,Path,PrimaryMode,HasDepth,HasStencil,HasColor,HasAudio,HasPhysics,SupportedModes");

            foreach (var r in _results)
            {
                sb.AppendLine($"\"{r.Name}\",\"{r.Path}\",{r.PrimaryMode},{r.HasDepth},{r.HasStencil},{r.HasColor},{r.HasAudio},{r.HasPhysics},\"{string.Join(";", r.SupportedModes)}\"");
            }

            string path = EditorUtility.SaveFilePanel("Save CSV", "", "VFX_Compatibility_Audit.csv", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, sb.ToString());
                Debug.Log($"[VFXCompatibilityAuditor] Exported to {path}");
            }
        }

        void ExportMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# VFX Compatibility Audit Report");
            sb.AppendLine();
            sb.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Total VFX: {_results.Count}");
            sb.AppendLine();

            // Stats
            var stats = GetStats();
            sb.AppendLine("## Mode Distribution");
            sb.AppendLine();
            sb.AppendLine("| Mode | Count |");
            sb.AppendLine("|------|-------|");
            foreach (var kv in stats.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"| {kv.Key} | {kv.Value} |");
            }
            sb.AppendLine();

            // Matrix
            sb.AppendLine("## Compatibility Matrix");
            sb.AppendLine();
            sb.AppendLine("| VFX | Mode | Depth | Stencil | Color | Audio | Physics |");
            sb.AppendLine("|-----|------|-------|---------|-------|-------|---------|");

            foreach (var r in _results)
            {
                string check(bool v) => v ? "✓" : "–";
                sb.AppendLine($"| {r.Name} | {r.PrimaryMode} | {check(r.HasDepth)} | {check(r.HasStencil)} | {check(r.HasColor)} | {check(r.HasAudio)} | {check(r.HasPhysics)} |");
            }

            string path = EditorUtility.SaveFilePanel("Save Markdown", "", "VFX_Compatibility_Audit.md", "md");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, sb.ToString());
                Debug.Log($"[VFXCompatibilityAuditor] Exported to {path}");
            }
        }

        struct VFXAuditResult
        {
            public string Name;
            public string Path;
            public VFXCategoryType PrimaryMode;
            public List<VFXCategoryType> SupportedModes;
            public bool HasDepth;
            public bool HasStencil;
            public bool HasColor;
            public bool HasAudio;
            public bool HasPhysics;
            public bool HasHands;
            public bool HasFace;
        }
    }
}
#endif
