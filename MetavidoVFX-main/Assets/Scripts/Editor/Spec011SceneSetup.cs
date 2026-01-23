// Spec011SceneSetup.cs - Editor utility to setup Spec 011 Open Brush demo scene
// Part of Spec 011: Open Brush Integration

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using XRRAI.BrushPainting;

namespace XRRAI.Editor
{
    /// <summary>
    /// Editor utility to setup the Spec 011 Open Brush Integration demo scene.
    /// </summary>
    public static class Spec011SceneSetup
    {
        private const string ScenePath = "Assets/Scenes/SpecDemos/Spec011_OpenBrush_Integration.unity";

        [MenuItem("H3M/Spec Demos/Setup Spec 011 Open Brush Demo", priority = 111)]
        public static void SetupSpec011Scene()
        {
            // Open the scene
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"[Spec011Setup] Scene not found: {ScenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Find or create BrushManager
            var brushManager = Object.FindAnyObjectByType<BrushManager>();
            if (brushManager == null)
            {
                var brushGO = new GameObject("BrushManager");
                SceneManager.MoveGameObjectToScene(brushGO, scene);
                brushManager = brushGO.AddComponent<BrushManager>();
                brushGO.AddComponent<BrushMirror>();
                Debug.Log("[Spec011Setup] Created BrushManager");
            }

            // 2. Add BrushMirror if missing
            if (brushManager.GetComponent<BrushMirror>() == null)
            {
                brushManager.gameObject.AddComponent<BrushMirror>();
                Debug.Log("[Spec011Setup] Added BrushMirror");
            }

            // 2.5. Add BrushInput if missing
            var brushInput = brushManager.GetComponent<BrushInput>();
            if (brushInput == null)
            {
                brushInput = brushManager.gameObject.AddComponent<BrushInput>();
                Debug.Log("[Spec011Setup] Added BrushInput");
            }
            // Configure BrushInput for Editor testing (Mouse mode)
            var inputModeField = typeof(BrushInput).GetField("_inputMode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (inputModeField != null)
            {
                inputModeField.SetValue(brushInput, BrushInput.InputMode.Mouse);
            }
            var requirePlaneField = typeof(BrushInput).GetField("_requirePlaneHit",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (requirePlaneField != null)
            {
                requirePlaneField.SetValue(brushInput, false);
            }
            var drawDistField = typeof(BrushInput).GetField("_drawDistance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (drawDistField != null)
            {
                drawDistField.SetValue(brushInput, 2f);
            }
            EditorUtility.SetDirty(brushInput);

            // 3. Find or create Demo Controller
            var demoController = Object.FindAnyObjectByType<Spec011DemoSetup>();
            if (demoController == null)
            {
                var demoGO = new GameObject("Spec011DemoController");
                SceneManager.MoveGameObjectToScene(demoGO, scene);
                demoController = demoGO.AddComponent<Spec011DemoSetup>();
                Debug.Log("[Spec011Setup] Created Spec011DemoSetup");
            }

            // 4. Wire references
            var brushManagerField = typeof(Spec011DemoSetup).GetField("_brushManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (brushManagerField != null)
            {
                brushManagerField.SetValue(demoController, brushManager);
            }
            EditorUtility.SetDirty(demoController);

            // 5. Create stroke prefab if needed
            CreateStrokePrefab();

            // 6. Assign stroke prefab to BrushManager
            var strokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Brush/BrushStroke.prefab");
            if (strokePrefab != null)
            {
                var prefabField = typeof(BrushManager).GetField("_strokePrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prefabField != null)
                {
                    prefabField.SetValue(brushManager, strokePrefab);
                    EditorUtility.SetDirty(brushManager);
                }
            }

            // 7. Add Camera if missing
            if (Camera.main == null)
            {
                var camGO = new GameObject("Main Camera");
                SceneManager.MoveGameObjectToScene(camGO, scene);
                var cam = camGO.AddComponent<Camera>();
                cam.tag = "MainCamera";
                camGO.transform.position = new Vector3(0, 1.5f, -2f);
                Debug.Log("[Spec011Setup] Created Main Camera");
            }

            // 8. Add directional light if missing
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            bool hasDirectionalLight = false;
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    hasDirectionalLight = true;
                    break;
                }
            }
            if (!hasDirectionalLight)
            {
                var lightGO = new GameObject("Directional Light");
                SceneManager.MoveGameObjectToScene(lightGO, scene);
                var light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.95f, 0.9f);
                light.intensity = 1f;
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                Debug.Log("[Spec011Setup] Created Directional Light");
            }

            // Save scene
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Spec011Setup] Scene setup complete!");
        }

        [MenuItem("H3M/Brush/Create Stroke Prefab", priority = 200)]
        public static void CreateStrokePrefab()
        {
            // Ensure directory exists
            string prefabDir = "Assets/Prefabs/Brush";
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                AssetDatabase.CreateFolder("Assets/Prefabs", "Brush");
            }

            string prefabPath = $"{prefabDir}/BrushStroke.prefab";

            // Check if prefab already exists
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log("[Spec011Setup] BrushStroke prefab already exists");
                return;
            }

            // Create the prefab
            var go = new GameObject("BrushStroke");
            go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();

            // Assign default material
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Brush/BrushStroke.mat");
            if (material == null)
            {
                // Create material if it doesn't exist
                CreateBrushMaterial();
                material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Brush/BrushStroke.mat");
            }

            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            go.AddComponent<BrushStroke>();

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[Spec011Setup] Created BrushStroke prefab at {prefabPath}");
        }

        [MenuItem("H3M/Brush/Create Brush Materials", priority = 201)]
        public static void CreateBrushMaterial()
        {
            // Ensure directory exists
            string matDir = "Assets/Materials/Brush";
            if (!AssetDatabase.IsValidFolder(matDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }
                AssetDatabase.CreateFolder("Assets/Materials", "Brush");
            }

            // Create default brush material
            var shader = Shader.Find("XRRAI/BrushStroke");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader != null)
            {
                var material = new Material(shader);
                material.name = "BrushStroke";
                AssetDatabase.CreateAsset(material, $"{matDir}/BrushStroke.mat");

                // Create glow material
                var glowShader = Shader.Find("XRRAI/BrushStrokeGlow");
                if (glowShader != null)
                {
                    var glowMat = new Material(glowShader);
                    glowMat.name = "BrushStrokeGlow";
                    glowMat.SetFloat("_EmissionStrength", 1.5f);
                    AssetDatabase.CreateAsset(glowMat, $"{matDir}/BrushStrokeGlow.mat");
                }

                // Create tube material
                var tubeShader = Shader.Find("XRRAI/BrushStrokeTube");
                if (tubeShader != null)
                {
                    var tubeMat = new Material(tubeShader);
                    tubeMat.name = "BrushStrokeTube";
                    AssetDatabase.CreateAsset(tubeMat, $"{matDir}/BrushStrokeTube.mat");
                }

                // Create hull material
                var hullShader = Shader.Find("XRRAI/BrushStrokeHull");
                if (hullShader != null)
                {
                    var hullMat = new Material(hullShader);
                    hullMat.name = "BrushStrokeHull";
                    hullMat.SetFloat("_Smoothness", 0.7f);
                    AssetDatabase.CreateAsset(hullMat, $"{matDir}/BrushStrokeHull.mat");
                }

                // Create particle material
                var particleShader = Shader.Find("XRRAI/BrushStrokeParticle");
                if (particleShader != null)
                {
                    var particleMat = new Material(particleShader);
                    particleMat.name = "BrushStrokeParticle";
                    AssetDatabase.CreateAsset(particleMat, $"{matDir}/BrushStrokeParticle.mat");
                }

                AssetDatabase.SaveAssets();
                Debug.Log("[Spec011Setup] Created brush materials");
            }
            else
            {
                Debug.LogWarning("[Spec011Setup] Could not find brush shaders");
            }
        }

        [MenuItem("H3M/Brush/Verify Brush System", priority = 210)]
        public static void VerifyBrushSystem()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Open Brush System Verification ===\n");

            // Check shaders
            string[] shaderNames = {
                "XRRAI/BrushStroke",
                "XRRAI/BrushStrokeGlow",
                "XRRAI/BrushStrokeTube",
                "XRRAI/BrushStrokeHull",
                "XRRAI/BrushStrokeParticle"
            };

            report.AppendLine("Shaders:");
            foreach (var shaderName in shaderNames)
            {
                var shader = Shader.Find(shaderName);
                report.AppendLine($"  {shaderName}: {(shader != null ? "✓" : "✗ MISSING")}");
            }

            // Check materials
            report.AppendLine("\nMaterials:");
            string[] matPaths = {
                "Assets/Materials/Brush/BrushStroke.mat",
                "Assets/Materials/Brush/BrushStrokeGlow.mat",
                "Assets/Materials/Brush/BrushStrokeTube.mat",
                "Assets/Materials/Brush/BrushStrokeHull.mat",
                "Assets/Materials/Brush/BrushStrokeParticle.mat"
            };

            foreach (var path in matPaths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                report.AppendLine($"  {name}: {(mat != null ? "✓" : "✗ MISSING")}");
            }

            // Check prefab
            report.AppendLine("\nPrefabs:");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Brush/BrushStroke.prefab");
            report.AppendLine($"  BrushStroke: {(prefab != null ? "✓" : "✗ MISSING")}");

            // Check scene components
            report.AppendLine("\nScene Components:");
            var brushManager = Object.FindAnyObjectByType<BrushManager>();
            report.AppendLine($"  BrushManager: {(brushManager != null ? "✓" : "✗ MISSING")}");

            var brushInput = Object.FindAnyObjectByType<BrushInput>();
            report.AppendLine($"  BrushInput: {(brushInput != null ? "✓" : "✗ MISSING")}");
            if (brushInput != null)
            {
                report.AppendLine($"    Mode: {brushInput.CurrentMode}");
            }

            var brushMirror = Object.FindAnyObjectByType<BrushMirror>();
            report.AppendLine($"  BrushMirror: {(brushMirror != null ? "✓" : "✗ MISSING")}");

            var demoSetup = Object.FindAnyObjectByType<Spec011DemoSetup>();
            report.AppendLine($"  Spec011DemoSetup: {(demoSetup != null ? "✓" : "✗ MISSING")}");

            // Test brush catalog
            report.AppendLine("\nBrush Catalog:");
            var essentialCatalog = BrushCatalogFactory.CreateEssentialCatalog();
            report.AppendLine($"  Essential brushes: {essentialCatalog.Count}");

            var fullCatalog = BrushCatalogFactory.CreateFullCatalog();
            report.AppendLine($"  Full catalog: {fullCatalog.Count}");

            // Cleanup test brushes
            foreach (var brush in essentialCatalog) Object.DestroyImmediate(brush);
            foreach (var brush in fullCatalog) Object.DestroyImmediate(brush);

            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("Brush System Verification", report.ToString(), "OK");
        }
    }
}
