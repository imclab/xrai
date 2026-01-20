// VFXBindingDocGenerator - Generates per-VFX binding documentation
// Creates: vfxname-source-bindings.md and vfxname-custom-bindings.md
// Maintains: _VFX_ORIGINAL_NAMES_REGISTRY.md for pre-rename tracking

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MetavidoVFX.VFX;

namespace MetavidoVFX.Editor
{
    public class VFXBindingDocGenerator : EditorWindow
    {
        static readonly string DocsFolder = "Assets/Documentation/VFX_Bindings";
        static readonly string RegistryPath = "Assets/Documentation/VFX_Bindings/_VFX_ORIGINAL_NAMES_REGISTRY.md";
        static readonly string MasterBindingsPath = "Assets/Documentation/VFX_Bindings/_MASTER_VFX_BINDINGS.md";

        Vector2 _scrollPosition;
        List<VFXBindingInfo> _vfxInfos = new();
        bool _includeCustomBindings = true;
        bool _generateMasterDoc = true;

        // Known pipeline property mappings
        static readonly Dictionary<string, string> SourceBindings = new()
        {
            { "DepthMap", "ARDepthSource.DepthMap" },
            { "StencilMap", "ARDepthSource.StencilMap" },
            { "PositionMap", "ARDepthSource.PositionMap" },
            { "VelocityMap", "ARDepthSource.VelocityMap" },
            { "ColorMap", "ARDepthSource.ColorMap" },
            { "RayParams", "ARDepthSource.RayParams" },
            { "InverseView", "ARDepthSource.InverseView" },
            { "InverseProjection", "ARDepthSource.InverseProjection" },
            { "DepthRange", "ARDepthSource.DepthRange" },
            { "KeypointBuffer", "NNCamKeypointBinder.KeypointBuffer" },
            { "_SegmentationTex", "MyakuMyakuBinder.StencilMap" },
            { "_ARRgbDTex", "MyakuMyakuBinder.ColorMap" },
            { "AudioVolume", "AudioBridge._AudioVolume" },
            { "AudioBands", "AudioBridge._AudioBands" },
            { "HandPosition", "HandVFXController.Position" },
            { "HandVelocity", "HandVFXController.Velocity" },
        };

        [MenuItem("H3M/VFX Pipeline Master/Binding Docs/Generate All Binding Docs")]
        public static void ShowWindow()
        {
            var window = GetWindow<VFXBindingDocGenerator>("VFX Binding Docs");
            window.minSize = new Vector2(600, 400);
        }

        [MenuItem("H3M/VFX Pipeline Master/Binding Docs/Quick Generate (Console)")]
        public static void QuickGenerate()
        {
            EnsureDocsFolder();
            var infos = ScanAllVFX();
            GenerateMasterBindingsDoc(infos);
            GenerateOriginalNamesRegistry(infos);
            Debug.Log($"[VFXBindingDocGenerator] Generated docs for {infos.Count} VFX");
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("VFX Binding Documentation Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Generates per-VFX binding docs and maintains original names registry.", MessageType.Info);

            EditorGUILayout.Space(5);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan All VFX", GUILayout.Height(30)))
                    _vfxInfos = ScanAllVFX();

                if (GUILayout.Button("Generate All Docs", GUILayout.Height(30)))
                    GenerateAllDocs();
            }

            _includeCustomBindings = EditorGUILayout.Toggle("Include Custom Bindings", _includeCustomBindings);
            _generateMasterDoc = EditorGUILayout.Toggle("Generate Master Doc", _generateMasterDoc);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Found: {_vfxInfos.Count} VFX", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (var info in _vfxInfos)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(info.Name, EditorStyles.linkLabel, GUILayout.Width(200)))
                        Selection.activeObject = info.Asset;

                    EditorGUILayout.LabelField(info.Category, GUILayout.Width(100));
                    EditorGUILayout.LabelField($"{info.ExposedProperties.Count} props", GUILayout.Width(60));
                    EditorGUILayout.LabelField(info.SourcePath, GUILayout.Width(200));
                }
            }
            EditorGUILayout.EndScrollView();
        }

        static void EnsureDocsFolder()
        {
            if (!Directory.Exists(DocsFolder))
            {
                Directory.CreateDirectory(DocsFolder);
                AssetDatabase.Refresh();
            }
        }

        static List<VFXBindingInfo> ScanAllVFX()
        {
            var infos = new List<VFXBindingInfo>();
            string[] guids = AssetDatabase.FindAssets("t:VisualEffectAsset", new[] { "Assets/Resources/VFX" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                if (asset == null) continue;

                var info = new VFXBindingInfo
                {
                    Name = asset.name,
                    Asset = asset,
                    Path = path,
                    Category = ExtractCategory(path),
                    SourcePath = ExtractSourcePath(path),
                    ExposedProperties = GetExposedProperties(asset)
                };

                infos.Add(info);
            }

            return infos.OrderBy(i => i.Category).ThenBy(i => i.Name).ToList();
        }

        static string ExtractCategory(string path)
        {
            // Extract folder name after Resources/VFX/
            var parts = path.Split('/');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i] == "VFX" && i + 1 < parts.Length - 1)
                    return parts[i + 1];
            }
            return "Uncategorized";
        }

        static string ExtractSourcePath(string path)
        {
            // Map known folder patterns to source repos
            if (path.Contains("/Rcam2/")) return "keijiro/Rcam2";
            if (path.Contains("/Rcam3/")) return "keijiro/Rcam3";
            if (path.Contains("/Rcam4/")) return "keijiro/Rcam4";
            if (path.Contains("/Akvfx/")) return "keijiro/Akvfx";
            if (path.Contains("/NNCam2/")) return "jp.keijiro.nncam2";
            if (path.Contains("/Buddha/")) return "holoi/touching-hologram";
            if (path.Contains("/Fluo/")) return "keijiro/Fluo";
            if (path.Contains("/Khoreo/")) return "keijiro/Khoreo";
            if (path.Contains("/SdfVfx/")) return "keijiro/SdfVfx";
            if (path.Contains("/Smrvfx/")) return "keijiro/Smrvfx";
            if (path.Contains("/Splat/")) return "keijiro/SplatVFX";
            if (path.Contains("/Keijiro/")) return "keijiro/VfxGraphTestbed";
            if (path.Contains("/UnitySamples/")) return "Unity VFX Samples";
            if (path.Contains("/Portals6/")) return "Unity Portals Demo";
            if (path.Contains("/FaceTracking/")) return "mao-test-h/FaceTrackingVFX";
            if (path.Contains("/Compute/")) return "cinight/MinimalCompute";
            if (path.Contains("/Myaku/")) return "plantblobs/MyakuMyakuAR";
            if (path.Contains("/Tamagotchu/")) return "EyezLee/TamagotchU";
            if (path.Contains("/WebRTC/")) return "URP-WebRTC-Convai";
            if (path.Contains("/Essentials/")) return "VFX-Essentials";
            if (path.Contains("/Dcam/")) return "keijiro/Dcam2";
            if (path.Contains("/People/") || path.Contains("/Environment/")) return "MetavidoVFX (Original)";
            return "Unknown";
        }

        static List<VFXPropertyInfo> GetExposedProperties(VisualEffectAsset asset)
        {
            var props = new List<VFXPropertyInfo>();

            // Use VFX Graph API to get exposed parameters
            // Note: This requires creating a temp VisualEffect to inspect properties
            var tempGO = new GameObject("TempVFXInspector");
            var vfx = tempGO.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = asset;

            // Get all exposed properties via reflection on the asset
            foreach (var name in GetPropertyNames(vfx))
            {
                var prop = new VFXPropertyInfo
                {
                    Name = name,
                    Type = GetPropertyType(vfx, name),
                    SourceBinding = SourceBindings.GetValueOrDefault(name, "Custom")
                };
                props.Add(prop);
            }

            Object.DestroyImmediate(tempGO);
            return props;
        }

        static List<string> GetPropertyNames(VisualEffect vfx)
        {
            var names = new List<string>();

            // Common property names to check
            string[] knownProps = {
                "DepthMap", "StencilMap", "PositionMap", "VelocityMap", "ColorMap",
                "RayParams", "InverseView", "InverseProjection", "DepthRange",
                "KeypointBuffer", "AudioVolume", "AudioBands", "AudioBass", "AudioMid", "AudioTreble",
                "HandPosition", "HandVelocity", "BrushWidth",
                "_SegmentationTex", "_ARRgbDTex", "_SpawnUvMinMax", "_SpawnRate",
                "Velocity", "Gravity", "Throttle", "Intensity", "Scale",
                "NormalMap", "MeshBuffer", "MeshPointCount"
            };

            foreach (var prop in knownProps)
            {
                if (vfx.HasTexture(prop) || vfx.HasFloat(prop) || vfx.HasVector3(prop) ||
                    vfx.HasVector4(prop) || vfx.HasMatrix4x4(prop) || vfx.HasGraphicsBuffer(prop))
                {
                    names.Add(prop);
                }
            }

            return names;
        }

        static string GetPropertyType(VisualEffect vfx, string name)
        {
            if (vfx.HasTexture(name)) return "Texture2D";
            if (vfx.HasFloat(name)) return "float";
            if (vfx.HasVector3(name)) return "Vector3";
            if (vfx.HasVector4(name)) return "Vector4";
            if (vfx.HasMatrix4x4(name)) return "Matrix4x4";
            if (vfx.HasGraphicsBuffer(name)) return "GraphicsBuffer";
            return "Unknown";
        }

        void GenerateAllDocs()
        {
            if (_vfxInfos.Count == 0)
                _vfxInfos = ScanAllVFX();

            EnsureDocsFolder();

            // Generate per-VFX docs
            foreach (var info in _vfxInfos)
            {
                GenerateSourceBindingsDoc(info);
                if (_includeCustomBindings)
                    GenerateCustomBindingsDoc(info);
            }

            // Generate master docs
            if (_generateMasterDoc)
            {
                GenerateMasterBindingsDoc(_vfxInfos);
                GenerateOriginalNamesRegistry(_vfxInfos);
            }

            AssetDatabase.Refresh();
            Debug.Log($"[VFXBindingDocGenerator] Generated docs for {_vfxInfos.Count} VFX in {DocsFolder}");
        }

        static void GenerateSourceBindingsDoc(VFXBindingInfo info)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {info.Name} - Source Bindings");
            sb.AppendLine();
            sb.AppendLine($"**Category**: {info.Category}");
            sb.AppendLine($"**Source**: {info.SourcePath}");
            sb.AppendLine($"**Path**: {info.Path}");
            sb.AppendLine();
            sb.AppendLine("## Exposed Properties");
            sb.AppendLine();
            sb.AppendLine("| Property | Type | Source Binding |");
            sb.AppendLine("|----------|------|----------------|");

            foreach (var prop in info.ExposedProperties)
            {
                sb.AppendLine($"| {prop.Name} | {prop.Type} | {prop.SourceBinding} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Binder Configuration");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine($"// Auto-generated binding for {info.Name}");
            sb.AppendLine("// Add VFXARBinder component with these settings:");

            var arProps = info.ExposedProperties.Where(p => p.SourceBinding.StartsWith("ARDepthSource")).ToList();
            if (arProps.Count > 0)
            {
                sb.AppendLine("// VFXARBinder binds: " + string.Join(", ", arProps.Select(p => p.Name)));
            }

            var customProps = info.ExposedProperties.Where(p => p.SourceBinding == "Custom").ToList();
            if (customProps.Count > 0)
            {
                sb.AppendLine("// Custom bindings needed for: " + string.Join(", ", customProps.Select(p => p.Name)));
            }

            sb.AppendLine("```");

            string filename = $"{SanitizeFilename(info.Name)}-source-bindings.md";
            File.WriteAllText(Path.Combine(DocsFolder, filename), sb.ToString());
        }

        static void GenerateCustomBindingsDoc(VFXBindingInfo info)
        {
            var customProps = info.ExposedProperties.Where(p => p.SourceBinding == "Custom").ToList();
            if (customProps.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine($"# {info.Name} - Custom Bindings");
            sb.AppendLine();
            sb.AppendLine("## Properties Requiring Custom Binding");
            sb.AppendLine();

            foreach (var prop in customProps)
            {
                sb.AppendLine($"### {prop.Name} ({prop.Type})");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine($"// TODO: Implement binding for {prop.Name}");
                sb.AppendLine($"vfx.Set{prop.Type.Replace("Texture2D", "Texture")}(\"{prop.Name}\", value);");
                sb.AppendLine("```");
                sb.AppendLine();
            }

            string filename = $"{SanitizeFilename(info.Name)}-custom-bindings.md";
            File.WriteAllText(Path.Combine(DocsFolder, filename), sb.ToString());
        }

        static void GenerateMasterBindingsDoc(List<VFXBindingInfo> infos)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Master VFX Bindings Reference");
            sb.AppendLine();
            sb.AppendLine($"**Total VFX**: {infos.Count}");
            sb.AppendLine($"**Generated**: {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine();

            // Group by category
            var byCategory = infos.GroupBy(i => i.Category).OrderBy(g => g.Key);

            sb.AppendLine("## Summary by Category");
            sb.AppendLine();
            sb.AppendLine("| Category | Count | AR Props | Audio Props | Custom Props |");
            sb.AppendLine("|----------|-------|----------|-------------|--------------|");

            foreach (var group in byCategory)
            {
                int arCount = group.Sum(i => i.ExposedProperties.Count(p => p.SourceBinding.StartsWith("ARDepthSource")));
                int audioCount = group.Sum(i => i.ExposedProperties.Count(p => p.SourceBinding.StartsWith("AudioBridge")));
                int customCount = group.Sum(i => i.ExposedProperties.Count(p => p.SourceBinding == "Custom"));
                sb.AppendLine($"| {group.Key} | {group.Count()} | {arCount} | {audioCount} | {customCount} |");
            }

            sb.AppendLine();
            sb.AppendLine("## VFX by Category");

            foreach (var group in byCategory)
            {
                sb.AppendLine();
                sb.AppendLine($"### {group.Key}");
                sb.AppendLine();
                sb.AppendLine("| VFX | Source | Properties |");
                sb.AppendLine("|-----|--------|------------|");

                foreach (var info in group)
                {
                    var props = string.Join(", ", info.ExposedProperties.Take(5).Select(p => p.Name));
                    if (info.ExposedProperties.Count > 5) props += "...";
                    sb.AppendLine($"| {info.Name} | {info.SourcePath} | {props} |");
                }
            }

            File.WriteAllText(MasterBindingsPath, sb.ToString());
        }

        static void GenerateOriginalNamesRegistry(List<VFXBindingInfo> infos)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# VFX Original Names Registry");
            sb.AppendLine();
            sb.AppendLine("Tracks original VFX names prior to any renaming.");
            sb.AppendLine($"**Generated**: {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
            sb.AppendLine("| Current Name | Original Name | Source Repo | Rename Date |");
            sb.AppendLine("|--------------|---------------|-------------|-------------|");

            foreach (var info in infos)
            {
                // For now, current = original (no renames yet)
                sb.AppendLine($"| {info.Name} | {info.Name} | {info.SourcePath} | - |");
            }

            sb.AppendLine();
            sb.AppendLine("## Naming Convention");
            sb.AppendLine();
            sb.AppendLine("Format: `name_type_source.vfx`");
            sb.AppendLine();
            sb.AppendLine("- `name`: Descriptive effect name");
            sb.AppendLine("- `type`: people, env, audio, hand, face, hybrid");
            sb.AppendLine("- `source`: Origin project (rcam, nncam, buddha, etc.)");

            File.WriteAllText(RegistryPath, sb.ToString());
        }

        static string SanitizeFilename(string name)
        {
            return name.Replace(" ", "_").Replace("/", "_").ToLower();
        }

        class VFXBindingInfo
        {
            public string Name;
            public VisualEffectAsset Asset;
            public string Path;
            public string Category;
            public string SourcePath;
            public List<VFXPropertyInfo> ExposedProperties = new();
        }

        class VFXPropertyInfo
        {
            public string Name;
            public string Type;
            public string SourceBinding;
        }
    }
}
#endif
