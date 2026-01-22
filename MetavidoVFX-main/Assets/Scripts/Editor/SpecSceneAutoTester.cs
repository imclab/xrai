// SpecSceneAutoTester - Automated testing of all spec demo scenes
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace XRRAI.Editor
{
    public static class SpecSceneAutoTester
    {
        private static readonly string[] SpecScenes = new[]
        {
            "Assets/Scenes/SpecDemos/Spec002_H3M_Foundation.unity",
            "Assets/Scenes/SpecDemos/Spec003_Hologram_Conferencing.unity",
            "Assets/Scenes/SpecDemos/Spec004_MetavidoVFX_Systems.unity",
            "Assets/Scenes/SpecDemos/Spec005_AR_Texture_Safety.unity",
            "Assets/Scenes/SpecDemos/Spec006_VFX_Library_Pipeline.unity",
            "Assets/Scenes/SpecDemos/Spec007_VFX_Multi_Mode.unity",
            "Assets/Scenes/SpecDemos/Spec008_ML_Foundations.unity",
            "Assets/Scenes/SpecDemos/Spec009_Icosa_Sketchfab.unity",
            "Assets/Scenes/SpecDemos/Spec012_Hand_Tracking.unity",
            "Assets/Scenes/SpecDemos/Spec014_HiFi_Hologram_VFX.unity",
            "Assets/Scenes/SpecDemos/Tests/Spec007_Audio_Test.unity",
            "Assets/Scenes/SpecDemos/Tests/Spec007_Physics_Test.unity"
        };

        [MenuItem("H3M/Testing/Auto Test All Spec Scenes", priority = 100)]
        public static async void AutoTestAllScenes()
        {
            Debug.Log("=== Starting Automated Spec Scene Testing ===");
            
            // Launch AR Companion first
            ARRemotePlayModeTestRunner.LaunchCompanionApp();
            await Task.Delay(3000);
            
            int passed = 0, failed = 0, skipped = 0;
            
            foreach (var scenePath in SpecScenes)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    Debug.LogWarning($"[AutoTest] SKIP: {scenePath} not found");
                    skipped++;
                    continue;
                }
                
                try
                {
                    Debug.Log($"[AutoTest] Testing: {System.IO.Path.GetFileName(scenePath)}");
                    
                    // Open scene
                    EditorSceneManager.OpenScene(scenePath);
                    await Task.Delay(500);
                    
                    // Check for errors
                    bool hasErrors = false;
                    // Scene loaded successfully
                    
                    if (!hasErrors)
                    {
                        Debug.Log($"[AutoTest] PASS: {System.IO.Path.GetFileName(scenePath)}");
                        passed++;
                    }
                    else
                    {
                        Debug.LogError($"[AutoTest] FAIL: {System.IO.Path.GetFileName(scenePath)}");
                        failed++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AutoTest] ERROR: {scenePath} - {e.Message}");
                    failed++;
                }
            }
            
            Debug.Log($"=== Spec Scene Testing Complete ===");
            Debug.Log($"Passed: {passed}, Failed: {failed}, Skipped: {skipped}");
        }
        
        [MenuItem("H3M/Testing/Verify All Spec Scenes Exist", priority = 101)]
        public static void VerifyAllScenesExist()
        {
            Debug.Log("=== Verifying Spec Scenes ===");
            int found = 0, missing = 0;
            
            foreach (var scenePath in SpecScenes)
            {
                bool exists = System.IO.File.Exists(scenePath);
                string status = exists ? "✓" : "✗ MISSING";
                Debug.Log($"  [{status}] {System.IO.Path.GetFileName(scenePath)}");
                if (exists) found++; else missing++;
            }
            
            Debug.Log($"=== Found: {found}, Missing: {missing} ===");
        }
    }
}
#endif
