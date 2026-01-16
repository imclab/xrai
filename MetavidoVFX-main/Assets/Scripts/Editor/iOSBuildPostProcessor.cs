using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class iOSBuildPostProcessor
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            string projPath = PBXProject.GetPBXProjectPath(path);
            PBXProject proj = new PBXProject();
            proj.ReadFromFile(projPath);

            string targetGuid = proj.GetUnityMainTargetGuid();
            string frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();

            // 1. Set Development Team
            proj.SetTeamId(targetGuid, "Z8622973EB");
            proj.SetTeamId(frameworkTargetGuid, "Z8622973EB");

            // 2. Enable Automatic Signing
            proj.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Automatic");
            proj.SetBuildProperty(frameworkTargetGuid, "CODE_SIGN_STYLE", "Automatic");

            // 3. Fix "HumanDepthMode" error if it persists in generated code (unlikely for Unity generated code, but good to know)

            // 4. Add required frameworks if missing (ARKit is usually added automatically)

            // 5. Disable Bitcode (often causes issues)
            proj.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");
            proj.SetBuildProperty(frameworkTargetGuid, "ENABLE_BITCODE", "NO");

            File.WriteAllText(projPath, proj.WriteToString());
        }
    }
}
