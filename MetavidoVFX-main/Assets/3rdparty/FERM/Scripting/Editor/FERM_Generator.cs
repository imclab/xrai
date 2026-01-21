using UnityEngine;
using System.IO;
using System;
using UnityEditor;
using System.Text.RegularExpressions;

public class FERM_Generator{

    private const string MATERIAL_FOLDER = "_Materials";
    private const string MATERIAL_EXT = "mat";
    private const string SHADER_FOLDER = "_Shaders";
    private const string SHADER_EXT = "shader";

    private const string DEFAULT_TEMPLATE = "StandardTemplate.txt";
    private const string NAME_PREFIX = "FERM_";

    [MenuItem("GameObject/FERM/Create Standard")]
    public static void CreateStandardRenderer() {
        Generate("StandardTemplate.txt");
    }

    [MenuItem("GameObject/FERM/Create Unlit")]
    public static void CreateUnlitRenderer() {
        Generate("UnlitTemplate.txt");
    }

    private static string GetTemplatePath(string template) {
        string toReturn = FERM_EditorUtil.FindAssetPath(template);
        if(toReturn == null)
            Debug.LogError("FERM plugin error, cannot find template file: " + template);
        return toReturn;
    }

    private static void Generate(string template) {
        //generate shader & material
        int index = GetNextMaterialIndex();
        string extension = index.ToString().PadLeft(4, '0');
        string name = NAME_PREFIX + extension;

        FERM_Shader shader = CreateShader(name, template);
        Material mat = CreateMaterial(name, shader);

        //create gameObject
        GameObject g = new GameObject(name);
        FERM_Renderer r = g.AddComponent<FERM_Renderer>();
        shader.SetMaterial(mat);
        r.SetShader(shader);
    }

    private static int GetNextMaterialIndex() {
        int toReturn = -1;

        FileInfo[] files = GetMaterialDirectory().GetFiles();

        foreach(FileInfo file in files) {
            if(Path.GetExtension(file.Name).Trim('.') == MATERIAL_EXT) {
                Match m = Regex.Match(file.Name, @"(\d+).mat$");
                if(m.Success && m.Groups.Count > 1) {
                    int nextIndex = -1;
                    int.TryParse(m.Groups[1].Value, out nextIndex);
                    toReturn = Math.Max(toReturn, nextIndex);
                }
            }
        }

        return toReturn + 1;
    }

    private static DirectoryInfo GetMaterialDirectory() {
        DirectoryInfo baseDir = GetPluginBaseDirectory();
        DirectoryInfo[] toReturn0 = baseDir.GetDirectories(MATERIAL_FOLDER);
        if(toReturn0.Length <= 0)
            return baseDir.CreateSubdirectory(MATERIAL_FOLDER);
        return baseDir.GetDirectories(MATERIAL_FOLDER)[0];
    }

    public static DirectoryInfo GetPluginBaseDirectory() {
        string templatePath = GetTemplatePath(DEFAULT_TEMPLATE);
        return Directory.GetParent(templatePath).Parent;
    }

    private static FERM_Shader CreateShader(string name, string templateName) {
        DirectoryInfo baseDir = GetPluginBaseDirectory();
        string shaderPath = baseDir.GetDirectories(SHADER_FOLDER)[0].FullName;
        shaderPath += "/" + name + '.' + SHADER_EXT;
        shaderPath = FERM_EditorUtil.GetRelativeAssetPath(shaderPath);
        string templatePath = GetTemplatePath(templateName);
        string template = File.ReadAllText(templatePath);
        FERM_Shader shader = new FERM_Shader(name, shaderPath, template);

        return shader;
    }

    private static Material CreateMaterial(string name, FERM_Shader shader) {
        DirectoryInfo baseDir = GetPluginBaseDirectory();
        string materialPath = baseDir.GetDirectories(MATERIAL_FOLDER)[0].FullName;
        materialPath += "/" + name + '.' + MATERIAL_EXT;
        materialPath = FERM_EditorUtil.GetRelativeAssetPath(materialPath);
        Shader shaderRef = Shader.Find("FERM/" + name);
        if(shaderRef == null) {
            Debug.LogError("Could not find shader + FERM/" + name + "! If it was generated please create a material for it manually.");
            return null;
        }
        Material material = new Material(Shader.Find("FERM/" + name));
        AssetDatabase.CreateAsset(material, materialPath);

        return material;
    }    
}

