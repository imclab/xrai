using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;

public class FERM_EditorUtil {

	public static class Style {
        public static GUIStyle h1 { get { return Make(22); } }
        public static GUIStyle p { get { return Make(16); } }

        public static GUIStyle Make(int fontSize) {
            GUIStyle g = new GUIStyle();
            g.fontSize = fontSize;
            return g;
        }

        public static GUIStyle Make(Color color) {
            GUIStyle g = new GUIStyle();
            g.normal.textColor = color;
            g.wordWrap = true;
            return g;
        }
    }



    public static T FindAsset<T>(string name, string extension) 
        where T : UnityEngine.Object{

        string[] results = AssetDatabase.FindAssets(name);
        string matchName = name + "." + extension;

        foreach(string result in results) {
            string resultPath = AssetDatabase.GUIDToAssetPath(result);
            string resultName = Path.GetFileName(resultPath);

            if(string.Equals(resultName, matchName, StringComparison.OrdinalIgnoreCase)) {
                T candidate = AssetDatabase.LoadAssetAtPath<T>(resultPath);
                if(candidate != null)
                    return candidate;
            }
        }
        return null;
    }

    public static string FindAssetPath(string name) {
        string searchName = Path.GetFileNameWithoutExtension(name);
        string[] results = AssetDatabase.FindAssets(searchName);

        foreach(string result in results) {
            string resultPath = AssetDatabase.GUIDToAssetPath(result);
            string resultName = Path.GetFileName(resultPath);

            if(string.Equals(resultName, name, StringComparison.OrdinalIgnoreCase))
                return resultPath;
        }
        return null;
    }

    /// <summary>
    /// Converts given absolute path to a relative path that starts 
    /// in the Assets folder
    /// </summary>
    public static string GetRelativeAssetPath(string path) {
        return "Assets" + path.Substring(Application.dataPath.Length);
    }


    public static System.Enum CreateEnumFromList(List<string> list) {

        System.AppDomain currentDomain = System.AppDomain.CurrentDomain;
        AssemblyName aName = new AssemblyName("Enum");
        AssemblyBuilder ab = currentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
        ModuleBuilder mb = ab.DefineDynamicModule(aName.Name);
        EnumBuilder enumerator = mb.DefineEnum("Enum", TypeAttributes.Public, typeof(int));

        int i = 0;
        enumerator.DefineLiteral("None", i);

        foreach(string names in list) {
            i++;
            enumerator.DefineLiteral(names, i);
        }

        System.Type finished = enumerator.CreateType();

        return (System.Enum)System.Enum.ToObject(finished, 0);
    }

}
