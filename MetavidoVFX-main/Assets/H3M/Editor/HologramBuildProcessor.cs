using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace H3M.Editor
{
    public class HologramBuildProcessor
    {
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
        {
            if (buildTarget != BuildTarget.iOS) return;

            Debug.Log($"[H3M] Processing iOS Build at: {path}");

            string projPath = PBXProject.GetPBXProjectPath(path);
            PBXProject proj = new PBXProject();
            proj.ReadFromFile(projPath);

            string target = proj.GetUnityFrameworkTargetGuid();

            // 1. Disable Bitcode (Standard for AR/VFX)
            proj.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

            // 2. Ensure Metal (Should be default, but enforcing)
            // proj.SetBuildProperty(target, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES"); // Example

            // 3. Write back
            File.WriteAllText(projPath, proj.WriteToString());

            Debug.Log("[H3M] iOS Build Settings Applied: Bitcode Disabled.");
        }
    }
}
