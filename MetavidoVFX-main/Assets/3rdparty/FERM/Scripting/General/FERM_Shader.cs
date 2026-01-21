using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class FERM_Shader {

    public Material material;

    [HideInInspector]
    [SerializeField]
    private int signature;

    private string shaderPath { get {
            if(!IsValidShaderPath(_shaderPath))
                _shaderPath = GetShaderPath(material);
            return _shaderPath;
    } }
    private string _shaderPath;

    private const string NAME = "name";
    private const string PARAMETERS = "parameters";
    private const string HELPERS = "helpers";
    private const string FUNCTION = "function";
    private const string OPTIONS = "options";

    public FERM_Shader(string name, string shaderPath, string template) {
        this._shaderPath = shaderPath;

        //file in name (permanently)
        template = FERM_Util.Insert(template, NAME, name);

        //make slots
        template = FERM_Util.MakeSlot(template, PARAMETERS);
        template = FERM_Util.MakeSlot(template, HELPERS);
        template = FERM_Util.MakeSlot(template, FUNCTION);
        template = FERM_Util.MakeSlot(template, OPTIONS);

        //fill in default implementation in the new slots: simple box
        template = FERM_Util.InsertSlot(template, FUNCTION, "return Box(pos, float3(1, 1, 1));");
        template = FERM_Util.InsertSlot(template, OPTIONS, 
            GetOptions(FERM_Renderer.Mode.Normal, 
            FERM_Renderer.SuperSampling.None));

        Compile(shaderPath, template);
    }

    public void SetMaterial(Material material) {
        this.material = material;
        _shaderPath = GetShaderPath(material);
    }

    private static string GetShaderPath(Material material) {
        if(material == null)
            return null;
#if UNITY_EDITOR
        string relativePath = UnityEditor.AssetDatabase.GetAssetPath(material.shader);
        string dataPath = Application.dataPath;
        dataPath = dataPath.Substring(0, dataPath.Length - 6);
        return dataPath + relativePath;
#else
        return null;
#endif
    }

    /// <summary>
    /// Fill in the shader template using the RaymarchComponent structure
    /// under the given parent.
    /// </summary>
    public void Generate(Transform parent, FERM_Renderer.Mode mode, FERM_Renderer.SuperSampling sampling) {
        CheckForStructureChanges(parent);

        string parameters = CollectParameters(parent);
        string function = GetDistanceFunction(parent);
        string helpers = CollectHelpers(parent);
        string options = GetOptions(mode, sampling);

        string content = GetTemplate();
        content = FERM_Util.InsertSlot(content, PARAMETERS, parameters);
        content = FERM_Util.InsertSlot(content, HELPERS, helpers);
        content = FERM_Util.InsertSlot(content, FUNCTION, function);
        content = FERM_Util.InsertSlot(content, OPTIONS, options);
        Compile(shaderPath, content);
    }

    /// <summary>
    /// Returns true if the raymarch structure has changed
    /// and the shader needs to be regenerated.
    /// </summary>
    public bool CheckForStructureChanges(Transform parent) {
        int newSignature = GetStructureHash(parent);
        bool toReturn = newSignature != signature;
        signature = newSignature;
        return toReturn;
    }

    private int GetStructureHash(Transform parent) {
        int signature = 0;
        int p = (1 << 30) - 101;
        FERM_Component[] components = parent.GetComponentsInChildren<FERM_Component>(true);
        foreach(FERM_Component rc in components) {
            if(!rc.active)
                continue;
            int x = rc.GetHashCode();
            signature = (signature + x) * p;
        }
        return signature;
    }

    /// <summary>
    /// Retrieve distance function from the primary characterizing shape.
    /// </summary>
    private string GetDistanceFunction(Transform parent) {
        List<string> dfs = new List<string>();
        for(int i = 0; i < parent.childCount; i++) {
            FERM_CharacterizingComponent cc = parent.GetChild(i).
                GetComponent<FERM_CharacterizingComponent>();
            if(cc != null && (cc.enabled & cc.gameObject.activeInHierarchy))
                dfs.Add(cc.GetFullDF());
        }
        string toReturn = FERM_Mixer.Union(dfs);
        if(toReturn.Contains("@pos"))
            toReturn = FERM_Util.Insert(toReturn, "pos", "pos");
        return "return " + toReturn + ";";
    }

    public List<FERM_CharacterizingComponent> GetBaseComponents(Transform parent) {
        List<FERM_CharacterizingComponent> toReturn = new List<FERM_CharacterizingComponent>();
        for(int i = 0; i < parent.childCount; i++) {
            FERM_CharacterizingComponent cc = parent.GetChild(i).
                GetComponent<FERM_CharacterizingComponent>();
            if(cc != null && cc.active)
                toReturn.Add(cc);
        }
        return toReturn;
    }

    /// <summary>
    /// Uniquate and collect parameters for all RaymarchComponents under parent.
    /// Returns shader code that can be used to declare all those parameters
    /// in the shader.
    /// </summary>
    private string CollectParameters(Transform parent) {
        FERM_Component[] components = parent.GetComponentsInChildren<FERM_Component>(false);
        List<FERM_Parameter> parameters = new List<FERM_Parameter>();
        foreach(FERM_Component rcp in components) {
            if(rcp.active)
                parameters.AddRange(rcp.UniquateParameters());
        }
        
        int nbParams = parameters.Count;

        string toReturn = "";
        for(int i = 0; i < nbParams; i++) {
            parameters[i].SetGenericIdentifier(i, nbParams);
            toReturn += parameters[i].GetShaderCodeDeclaration();
        }
        
        return toReturn;
    }

    private string CollectHelpers(Transform parent) {
        FERM_RecurseModifier[] recs = parent.GetComponentsInChildren
            <FERM_RecurseModifier>();
        int nbHelpers = recs.Length;

        string toReturn = "";
        for(int i = 0; i < nbHelpers; i++) {
            if(!recs[i].active)
                continue;

            recs[i].name = "hlp" + FERM_Util.NumString(i, nbHelpers);
            toReturn += recs[i].GetHelperFunction();
        }

        return toReturn;
    }

    private string GetOptions(FERM_Renderer.Mode mode, FERM_Renderer.SuperSampling sampling) {
        string toReturn = GetRenderingModeDefine(mode);
        toReturn += GetSamplingDefine(sampling);
        return toReturn;
    }

    private string GetRenderingModeDefine(FERM_Renderer.Mode mode) {
        switch(mode) {
        case FERM_Renderer.Mode.Normal:
        return "#define USE_RAYMARCHING_DEPTH\r\n";
        case FERM_Renderer.Mode.Depthless:
        return "";
        case FERM_Renderer.Mode.Skybox:
        return "#define SKYBOX_MODE\r\n";
        case FERM_Renderer.Mode.Equirect360:
        return "#define EQUIRECT_360_MODE\r\n";
        default:
        Debug.LogError("Unkown rendering mode: " + mode);
        return "";
        }
    }

    private string GetSamplingDefine(FERM_Renderer.SuperSampling sampling) {
        int samplingIndex = (int)sampling;
        int samples = new int[] { 1, 2, 4, 9, 16 }[samplingIndex];
        return "#define SUPERSAMPLING_" + samples + "X\r\n";
    }

    /// <summary>
    /// Check if the path towards a compiled shader is valid.
    /// Can only be done in the Unity editor!
    /// </summary>
    private static bool IsValidShaderPath(string path) {
        if(string.IsNullOrEmpty(path))
            return false;

#if UNITY_EDITOR
        try {
            string content = File.ReadAllText(path);
            bool toReturn = true;
            toReturn &= FERM_Util.HasSlot(content, PARAMETERS);
            toReturn &= FERM_Util.HasSlot(content, HELPERS);
            toReturn &= FERM_Util.HasSlot(content, FUNCTION);
            toReturn &= FERM_Util.HasSlot(content, OPTIONS);
            return toReturn;
        }
        catch(Exception) { return false; } 
#else
        return true;
#endif
    }

    /// <summary>
    /// Read the last compiled shader content and return it
    /// so we can overwrite and recompile it. This can only 
    /// be done in the Unity editor!
    /// </summary>
    private string GetTemplate() {
        if(!IsValidShaderPath(shaderPath)) {
            throw new Exception("Shader path does not point towards a valid FERM shader. Please check that the material of the FERM renderer has a valid FERM shader set as its source shader.");
        }
#if UNITY_EDITOR
        return File.ReadAllText(shaderPath);
#else
        return "";
#endif
    }

    /// <summary>
    /// Export the given content to an actual shader file 
    /// so it may be rendered. This can only be done in the 
    /// Unity editor!
    /// </summary>
    private void Compile(string path, string content) {
#if UNITY_EDITOR
        File.WriteAllText(path, content);
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    /// <summary>
    /// Delete shader and material references by this shader.
    /// This can only be done in the Unity editor!
    /// </summary>
    public void Delete() {
#if UNITY_EDITOR
        string materialPath = UnityEditor.AssetDatabase.GetAssetPath(material);
        string shaderPath = UnityEditor.AssetDatabase.GetAssetPath(material.shader);
        UnityEditor.AssetDatabase.DeleteAsset(materialPath);
        UnityEditor.AssetDatabase.DeleteAsset(shaderPath);
#endif
    }
}
