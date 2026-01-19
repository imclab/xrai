#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public class iOSBuild : MonoBehaviour
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        string projectPath = PBXProject.GetPBXProjectPath(path);
        PBXProject project = new PBXProject();
        project.ReadFromString(File.ReadAllText(projectPath));
#if UNITY_2019_3_OR_NEWER
        string targetGuid = project.GetUnityFrameworkTargetGuid();
#else
        string targetGuid = project.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif
        project.AddFrameworkToProject(targetGuid, "libz.tbd", false);
        project.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");
        File.WriteAllText(projectPath, project.WriteToString());
    }
}
#endif