using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A characterizing component is unique per gameObject in the 
/// Raymarch hierarchy. It outputs a distance function that
/// may be based on components on its children and 
/// modifyers on the same object. Characterizing shapes 
/// are generally split into shapes and mixers.
/// </summary>
[DisallowMultipleComponent]
public abstract class FERM_CharacterizingComponent : FERM_Component {

    //connection cache
    private FERM_RecurseModifier rec;
    private FERM_Modifier[] mods;

    /// <summary>
    /// Returns distance function with default paremeter names ("@par0") 
    /// and no modifyers applied
    /// </summary>
    public abstract string GetParametrizedDF();

    /// <summary>
    /// Can be overriden in case a shape needs to apply something unusual 
    /// to the distance function in post.
    /// </summary>
    public virtual string PostApply(string inputFunction) {
        return inputFunction;
    }

    /// <summary>
    /// Returns distance function with all parameters and modifyers applied.
    /// The given function is ready to be inserted in a shader code line, 
    /// apart from the input which is given as "@pos".
    /// </summary>
    public string GetFullDF() {
        return PostApply(ApplyAllModifiers(GetParametrizedDF()));
    }

    /// <summary>
    /// Filled in DF (like FullDF) but without any modifiers applied.
    /// </summary>
    public string GetUnmodifiedDF() {
        return GetParametrizedDF();
    }

    /// <summary>
    /// Apply all the modifiers parallell to this RaymarchComponent to the 
    /// given distance function
    /// </summary>
    protected string ApplyAllModifiers(string function) {
        rec = GetComponent<FERM_RecurseModifier>();
        if(rec != null && rec.active)
            return rec.Apply(function);
        else {
            mods = GetComponents<FERM_Modifier>();
            foreach(FERM_Modifier mod in mods) {
                if(mod.active)
                    function = mod.Apply(function);
            }
        }

        return function;
    }

    public float DF(Vector3 pos) {
        pos = Pre(pos);
        float value;
        if(rec != null && rec.active) {
            pos = rec.Pre(pos);
            float result = InnerDF(pos);
            value = rec.Post(result);
        }
        else {
            for(int i = mods.Length - 1; i >= 0; i--) {
                if(mods[i].active)
                    pos = mods[i].Pre(pos);
            }
            value = InnerDF(pos);
            for(int i = 0; i < mods.Length; i++) {
                if(mods[i].active)
                    value = mods[i].Post(value);
            }
        }
        return Post(value);
    }

    protected abstract Vector3 Pre(Vector3 pos);
    protected abstract float InnerDF(Vector3 pos);
    protected abstract float Post(float value);

    protected override void UpdateParameters() {
        foreach(FERM_ParamExposer exp in GetComponents<FERM_ParamExposer>())
            exp.ApplyValue();
    }
}
