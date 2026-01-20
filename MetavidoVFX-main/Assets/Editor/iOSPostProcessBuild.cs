using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class iOSPostProcessBuild
{
    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);

        // Get all target GUIDs
        string frameworkGuid = project.GetUnityFrameworkTargetGuid();
        string mainGuid = project.GetUnityMainTargetGuid();

        // Fix ARM64 branch out of range error for large IL2CPP builds
        // The new linker in Xcode 15+ doesn't generate branch islands automatically
        // These flags tell the compiler/linker to use indirect addressing
        string[] allTargets = new[] { frameworkGuid, mainGuid };
        foreach (var guid in allTargets)
        {
            // Use classic linker which auto-generates branch islands
            project.AddBuildProperty(guid, "OTHER_LDFLAGS", "-Wl,-ld_classic");
            // Force indirect access for external symbols (avoids direct branch limitations)
            project.AddBuildProperty(guid, "OTHER_CFLAGS", "-fno-direct-access-external-data");
        }

        // Also need to handle the GameAssembly target if it exists
        string gameAssemblyGuid = project.TargetGuidByName("GameAssembly");
        if (!string.IsNullOrEmpty(gameAssemblyGuid))
        {
            project.AddBuildProperty(gameAssemblyGuid, "OTHER_LDFLAGS", "-Wl,-ld_classic");
            project.AddBuildProperty(gameAssemblyGuid, "OTHER_CFLAGS", "-fno-direct-access-external-data");
        }

        project.WriteToFile(projectPath);
        UnityEngine.Debug.Log("[iOSPostProcessBuild] Added ARM64 large binary linker flags to all targets");
    }
}
