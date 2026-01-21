using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Raymarch components define the distance function and parameters
/// of a Raymarch setup. They are generally split into characterizing 
/// components and modifiers.
/// </summary>
[ExecuteInEditMode]
public abstract class FERM_Component : MonoBehaviour {

    public FERM_Parameter[] parameters;
    public FERM_Renderer rend;

    private void OnEnable() {
        rend = transform.GetComponentInParent<FERM_Renderer>();
    }

    /// <summary>
    /// Makes unique instance copies of the static parameter set and 
    /// sets them on this component. The parameters are also returned
    /// so that they can be assigned uniqe id's.
    /// </summary>
    public FERM_Parameter[] UniquateParameters() {
        FERM_Parameter[] temp = GetParameters();
        FERM_Parameter[] old = parameters;

        parameters = new FERM_Parameter[temp.Length];
        for(int i = 0; i < temp.Length; i++)
            parameters[i] = FERM_Parameter.Clone(temp[i]);

        CopyParamsIfMatch(parameters, old);

        return parameters;
    }

    /// <summary>
    /// Copy values of oldParams to newParams if their types
    /// match exactly.
    /// </summary>
    private void CopyParamsIfMatch(FERM_Parameter[] newParams, FERM_Parameter[] oldParams) {
        int nb = newParams.Length;
        if(oldParams == null || oldParams.Length != nb)
            return;
        for(int i = 0; i < nb; i++) {
            if(newParams[i].type != oldParams[i].type)
                return;
        }
        for(int i = 0; i < nb; i++)
            newParams[i].SetValue(oldParams[i].GetValue());
    }

    /// <summary>
    /// Returns a static dummy list of parameters that this
    /// component wants to use to connect to the Raymarch shader.
    /// The actual parameters to be used should be clones of these.
    /// </summary>
    protected abstract FERM_Parameter[] GetParameters();

    public void Update() {
        if(rend == null)
            rend = transform.GetComponentInParent<FERM_Renderer>();
        if(rend == null || rend.material == null || parameters == null)
            return;

        UpdateParameters();

        foreach(FERM_Parameter par in parameters)
            par.ApplyToMaterial(rend.material);
    }

    /// <summary>
    /// Fetch updates values for parameters that can be changed on the fly.
    /// Includes parameters controlled by object transforms.
    /// </summary>
    protected virtual void UpdateParameters() {}

    /// <summary>
    /// Fill in parameter flags ("@par0") of the given function
    /// with the uniquated paremeter id's, currently set in this 
    /// component.
    /// </summary>
    protected string FillInParameters(string template, bool ignoreMissing) {
        int nbParams = parameters.Length;
        for(int i = 0; i < nbParams; i++) {
            string match = "par" + FERM_Util.NumString(i, nbParams);
            if(ignoreMissing && !template.Contains("@" + match)) 
                continue;
            template = FERM_Util.Insert(template, match, parameters[i].GetIdentifier());
        }
        return template;
    }


    /// <summary>
    /// Return Geometry type of the given component:
    /// Exact for exact euclidian distance functions that are both safe and fast,
    /// Bounded for functions that are safe, but slower,
    /// Distorted for functions that are unsafe.
    /// A classification may still apply when it's only valid under certain 
    /// conditions for the input parameters (e.g. inflating by -1)
    /// </summary>
    public abstract Geometry GetGeometry();

    public enum Geometry {
        Exact, ExactConditonal, Complex, Bounded, BoundedConditonal,
        Distorted, Recurse
    }

    public string GetTypeName() {
        FieldInfo typeField = this.GetType().GetField("type");
        object typeValue = typeField.GetValue(this);
        return typeValue.ToString();
    }

    public bool IsDisplayable() {
        if(transform.parent == null)
            return false;
        if(transform.parent.GetComponent<FERM_Renderer>() != null)
            return true;
        if(transform.parent.GetComponent<FERM_Mixer>() != null)
            return true;
        return false;
    }

    public void SetParam(string name, object value) {
        if(parameters == null) {
            Debug.LogError("Cannot set param values on uninitialized FERM component");
            return;
        }

        bool match = false;
        for(int i = 0; i < parameters.Length; i++) {
            match = string.Equals(parameters[i].name, name, System.StringComparison.OrdinalIgnoreCase);
            if(match) {
                if(!parameters[i].Match(value))
                    Debug.LogError("Parameter type " + parameters[i].type +
                        " cannot be assigned value of " + value);
                else
                    parameters[i].SetValue(value);
                return;
            }
        }
        
        Debug.LogError("Parameter name " + name + " could not be found");
    }

    public bool active { get { return enabled & gameObject.activeInHierarchy; } }
}
