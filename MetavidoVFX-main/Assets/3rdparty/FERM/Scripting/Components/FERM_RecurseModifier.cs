using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes all modifier attached to this 
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(FERM_CharacterizingComponent))]
[RequireComponent(typeof(FERM_Modifier))]
public class FERM_RecurseModifier : FERM_Component {

    //connection cache
    private FERM_Component[] sisterComponents;
    private bool foundSelf;
    private List<FERM_Modifier> preMods, recurseMods, postMods;

    new public string name;
    private string inputFunction;

    public Type type;

    public enum Type {
        TargetAllModifiers, TargetModifiersBefore, TargetModifiersAfter

    }

    private const string template = @"
inline float @name(float3 pos)
{
    for(int i = 0; i < @par0; i++) {
        pos = @in;
    }
    float toReturn = @function;
    for(i = 0; i < @par0; i++) {
        toReturn = @out;
    }
    return toReturn;
}";


    public string GetHelperFunction() {
        List<FERM_Modifier> mods = GetTargetMods();

        string inputMod = GetInputModifiers(mods);
        string outputMod = GetOutputModifiers(mods);
        if(inputFunction == null)
            inputFunction = "Box(@pos, float3(1.0, 1.0, 1.0)";
        if(inputFunction.Contains("@pos"))
            inputFunction = FERM_Util.Insert(inputFunction, "pos", "pos");

        string helper = template;
        helper = FERM_Util.Insert(helper, "name", this.name);
        helper = FERM_Util.Insert(helper, "in", inputMod);
        helper = FERM_Util.Insert(helper, "out", outputMod);
        helper = FERM_Util.Insert(helper, "function", inputFunction);

        return FillInParameters(helper, false);
    }

    private string GetInputModifiers(List<FERM_Modifier> mods) {
        string toReturn = "@pos";
        foreach(FERM_Modifier mod in mods)
            toReturn = mod.ApplyInput(toReturn);
        return FERM_Util.Insert(toReturn, "pos", "pos");
    }

    private string GetOutputModifiers(List<FERM_Modifier> mods) {
        string toReturn = "toReturn";
        foreach(FERM_Modifier mod in mods)
            toReturn = mod.ApplyOutput(toReturn);
        return toReturn;
    }

    public string Apply(string inputFunction) {
        this.inputFunction = ApplyNonTargetMods(inputFunction, false);
        return ApplyNonTargetMods(name + "(@pos)", true);
    }

    protected override FERM_Parameter[] GetParameters() {
        return new FERM_Parameter[] { new FERM_Parameter("Iterations", 1) };
    }

    public override int GetHashCode() {
        return 731 + (int)type;
    }

    public override Geometry GetGeometry() {
        return FERM_Component.Geometry.Complex;
    }

    private List<FERM_Modifier> GetTargetMods() {
        recurseMods = new List<FERM_Modifier>();
        sisterComponents = GetComponents<FERM_Component>();
        foundSelf = false;
        foreach(FERM_Component cp in sisterComponents) {
            if(ModifierValid(cp, true, false))
                recurseMods.Add((FERM_Modifier)cp);
        }
        return recurseMods;
    }

    private string ApplyNonTargetMods(string function, bool after) {
        List<FERM_Modifier> list = new List<FERM_Modifier>();
        sisterComponents = GetComponents<FERM_Component>();
        foundSelf = false;
        foreach(FERM_Component cp in sisterComponents) {
            if(ModifierValid(cp, false, after)) {
                list.Add((FERM_Modifier)cp);
                function = ((FERM_Modifier)cp).Apply(function);
            }
        }
        if(after)
            postMods = list;
        else
            preMods = list;
        return function;
    }

    public Vector3 Pre(Vector3 pos) {
        for(int i = postMods.Count - 1; i >= 0 ; i--) 
            pos = postMods[i].Pre(pos);

        for(int i = 0; i < (int)parameters[0].GetValue(); i++) {
            for(int j = recurseMods.Count - 1; j >= 0; j--)
                pos = recurseMods[j].Pre(pos);
        }

        for(int i = preMods.Count - 1; i >= 0; i--)
            pos = preMods[i].Pre(pos);

        return pos;
    }

    public float Post(float value) {
        for(int i = 0; i < preMods.Count; i++)
            value = preMods[i].Post(value);

        for(int i = 0; i < (int)parameters[0].GetValue(); i++) {
            for(int j = 0; j < recurseMods.Count; j++)
                value = recurseMods[j].Post(value);
        }

        for(int i = 0; i < postMods.Count; i++)
            value = postMods[i].Post(value);

        return value;
    }

    private bool ModifierValid(FERM_Component component, bool recursing, bool after) {
        if(component is FERM_RecurseModifier) {
            foundSelf = true;
            return false;
        }
        if(component is FERM_Modifier && component.active) {

            switch(type) {

            case Type.TargetModifiersAfter:
            if(recursing)
                return foundSelf;
            else
                return !foundSelf && !after;

            case Type.TargetModifiersBefore:
            if(recursing)
                return !foundSelf;
            else
                return foundSelf && after;
            }

            return recursing;
        }
        return false;
    }
}
