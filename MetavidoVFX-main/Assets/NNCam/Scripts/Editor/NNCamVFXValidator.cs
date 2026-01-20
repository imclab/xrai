// NNCam VFX Validator
// Editor tool to validate and diagnose NNCam VFX subgraph wiring issues
// Created to improve VFX Graph debugging workflow

using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace MetavidoVFX.NNCam.Editor
{
    public class NNCamVFXValidator : EditorWindow
    {
        // Known subgraph GUIDs
        static readonly Dictionary<string, string> KnownSubgraphs = new Dictionary<string, string>
        {
            { "69374374b83d4400db2bb56f0970b48d", "Get Keypoint World (has PositionMap input)" },
            { "d165f6ed4dc68443e9de23907e00d7bb", "Get Keypoint (screen-space, no PositionMap)" },
        };

        Vector2 scrollPos;
        List<VFXReport> reports = new List<VFXReport>();

        class VFXReport
        {
            public string assetPath;
            public string assetName;
            public List<SubgraphInfo> subgraphs = new List<SubgraphInfo>();
            public List<string> issues = new List<string>();
            public bool hasPositionMapProperty;
            public bool hasKeypointBufferProperty;
        }

        class SubgraphInfo
        {
            public string guid;
            public string name;
            public int inputSlotCount;
            public bool hasPositionMapInput;
        }

        [MenuItem("H3M/NNCam/Validate NNCam VFX Assets")]
        static void ShowWindow()
        {
            var window = GetWindow<NNCamVFXValidator>("NNCam VFX Validator");
            window.minSize = new Vector2(500, 400);
            window.Scan();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan NNCam VFX", GUILayout.Height(30)))
            {
                Scan();
            }
            if (GUILayout.Button("Open VFX Folder", GUILayout.Height(30)))
            {
                var path = "Assets/VFX/NNCam2";
                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var report in reports)
            {
                DrawReport(report);
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawReport(VFXReport report)
        {
            bool hasIssues = report.issues.Count > 0;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header with color indicator
            var headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.normal.textColor = hasIssues ? new Color(1f, 0.5f, 0.3f) : new Color(0.3f, 0.8f, 0.3f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(hasIssues ? "⚠" : "✓", GUILayout.Width(20));
            if (GUILayout.Button(report.assetName, headerStyle))
            {
                var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(report.assetPath);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Properties
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"PositionMap property: {(report.hasPositionMapProperty ? "Yes" : "No")}");
            EditorGUILayout.LabelField($"KeypointBuffer property: {(report.hasKeypointBufferProperty ? "Yes" : "No")}");

            // Subgraphs
            if (report.subgraphs.Count > 0)
            {
                EditorGUILayout.LabelField("Subgraphs:");
                EditorGUI.indentLevel++;
                foreach (var sg in report.subgraphs)
                {
                    string info = sg.name;
                    if (sg.hasPositionMapInput)
                        info += " [PositionMap: Yes]";
                    EditorGUILayout.LabelField($"• {info}");
                }
                EditorGUI.indentLevel--;
            }

            // Issues
            if (hasIssues)
            {
                EditorGUILayout.Space(5);
                var issueStyle = new GUIStyle(EditorStyles.label);
                issueStyle.normal.textColor = new Color(1f, 0.4f, 0.3f);
                issueStyle.wordWrap = true;

                foreach (var issue in report.issues)
                {
                    EditorGUILayout.LabelField($"⚠ {issue}", issueStyle);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        void Scan()
        {
            reports.Clear();

            string[] vfxPaths = new[]
            {
                "Assets/VFX/NNCam2",
                "Assets/Resources/VFX/NNCam2"
            };

            foreach (var basePath in vfxPaths)
            {
                if (!Directory.Exists(basePath)) continue;

                var files = Directory.GetFiles(basePath, "*.vfx", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var report = AnalyzeVFX(file);
                    if (report != null)
                        reports.Add(report);
                }
            }

            Debug.Log($"[NNCamVFXValidator] Scanned {reports.Count} VFX assets");
            Repaint();
        }

        VFXReport AnalyzeVFX(string path)
        {
            var report = new VFXReport
            {
                assetPath = path.Replace("\\", "/"),
                assetName = Path.GetFileNameWithoutExtension(path)
            };

            try
            {
                string content = File.ReadAllText(path);

                // Check for exposed properties
                report.hasPositionMapProperty = content.Contains("m_ExposedName: PositionMap");
                report.hasKeypointBufferProperty = content.Contains("m_ExposedName: KeypointBuffer");

                // Find all subgraph references
                var subgraphMatches = Regex.Matches(content, @"m_Subgraph:.*guid: ([a-f0-9]+)");
                var seenGuids = new HashSet<string>();

                foreach (Match match in subgraphMatches)
                {
                    string guid = match.Groups[1].Value;
                    if (seenGuids.Contains(guid)) continue;
                    seenGuids.Add(guid);

                    var sgInfo = new SubgraphInfo { guid = guid };

                    if (KnownSubgraphs.TryGetValue(guid, out string name))
                    {
                        sgInfo.name = name;
                        sgInfo.hasPositionMapInput = guid == "69374374b83d4400db2bb56f0970b48d";
                    }
                    else
                    {
                        // Try to find the subgraph asset
                        string sgPath = AssetDatabase.GUIDToAssetPath(guid);
                        sgInfo.name = string.IsNullOrEmpty(sgPath)
                            ? $"Unknown ({guid.Substring(0, 8)}...)"
                            : Path.GetFileNameWithoutExtension(sgPath);
                    }

                    report.subgraphs.Add(sgInfo);
                }

                // Validation checks
                ValidateReport(report, content);
            }
            catch (System.Exception e)
            {
                report.issues.Add($"Error reading file: {e.Message}");
            }

            return report;
        }

        void ValidateReport(VFXReport report, string content)
        {
            // Check: Has PositionMap property but no world-space subgraph
            bool hasWorldSpaceSubgraph = report.subgraphs.Exists(s => s.hasPositionMapInput);

            if (report.hasPositionMapProperty && !hasWorldSpaceSubgraph)
            {
                report.issues.Add("Has PositionMap property but no 'Get Keypoint World' subgraph to use it");
            }

            // Check: Uses world-space subgraph but no PositionMap property
            if (hasWorldSpaceSubgraph && !report.hasPositionMapProperty)
            {
                report.issues.Add("Uses 'Get Keypoint World' subgraph but missing PositionMap blackboard property");
            }

            // Check: PositionMap connection might be broken (slot reference doesn't exist)
            if (report.hasPositionMapProperty && hasWorldSpaceSubgraph)
            {
                // Look for linkedSlots connections to PositionMap
                var posMapMatch = Regex.Match(content, @"m_ExposedName: PositionMap[\s\S]*?linkedSlots:[\s\S]*?inputSlot: \{fileID: (\d+)\}");
                if (posMapMatch.Success)
                {
                    string slotId = posMapMatch.Groups[1].Value;
                    // Check if this slot ID has a definition
                    if (!content.Contains($"&{slotId}"))
                    {
                        report.issues.Add($"PositionMap connection references missing slot (ID: {slotId}) - subgraph may need rewiring");
                    }
                }
            }

            // Check: No KeypointBuffer property
            if (!report.hasKeypointBufferProperty)
            {
                report.issues.Add("Missing KeypointBuffer property - NNCam effects need this for pose data");
            }
        }

        [MenuItem("H3M/NNCam/Open eyes_any_nncam2 in VFX Graph")]
        static void OpenEyesVFX()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/NNCam2/eyes_any_nncam2.vfx");
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
                Debug.Log("[NNCamVFXValidator] Opened eyes_any_nncam2.vfx - Look for 'Get Keypoint World' subgraph and verify PositionMap is wired");
            }
            else
            {
                Debug.LogError("[NNCamVFXValidator] Could not find eyes_any_nncam2.vfx");
            }
        }
    }
}
