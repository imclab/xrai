using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Shape that does not depend on further subshapes.
/// This shape in particular links the transform (position & rotation)
/// of the shape to the Raymarch setup.
/// Elementary shapes include primitives and fractals.
/// </summary>
public abstract class FERM_Shape : FERM_CharacterizingComponent {

    public UnityTransformMode unityTransformMode;

    public enum UnityTransformMode
    {
        ApplyBeforeModifiers, ApplyAfterModifiers, Ignore
    }

    public override string GetParametrizedDF() {
        string toReturn = GetElementaryDF();
        if(unityTransformMode == UnityTransformMode.ApplyBeforeModifiers) {
            toReturn = ApplyTransform(GetElementaryDF());
            toReturn = FillInParameters(toReturn, true);
        }
        return FillInParameters(toReturn, true);
    }

    public override string PostApply(string inputFunction) {
        string toReturn = inputFunction;
        if(unityTransformMode == UnityTransformMode.ApplyAfterModifiers) {
            toReturn = ApplyTransform(inputFunction);
            toReturn = FillInParameters(toReturn, true);
        }
        return toReturn;
    }

    protected override float InnerDF(Vector3 pos) {
        bool transform = unityTransformMode == UnityTransformMode.ApplyBeforeModifiers;

        if(transform) {
            int nbPars = parameters.Length;
            Vector3 shift = (Vector3)parameters[nbPars - 3].GetValue();
            Quaternion rot = (Quaternion)parameters[nbPars - 2].GetValue();
            float scale = (float)parameters[nbPars - 1].GetValue();

            pos = FERM_Modifier.PreTransform(pos, shift, rot, scale);
            float value = ElementaryDF(pos);
            return FERM_Modifier.PostTransform(value, shift, rot, scale);
        }

        return ElementaryDF(pos);
    }

    protected override Vector3 Pre(Vector3 pos) {
        bool transform = unityTransformMode == UnityTransformMode.ApplyAfterModifiers;

        if(transform) {
            int nbPars = parameters.Length;
            Vector3 shift = (Vector3)parameters[nbPars - 3].GetValue();
            Quaternion rot = (Quaternion)parameters[nbPars - 2].GetValue();
            float scale = (float)parameters[nbPars - 1].GetValue();

            return FERM_Modifier.PreTransform(pos, shift, rot, scale);
        }

        return pos;
    }

    protected override float Post(float value) {
        bool transform = unityTransformMode == UnityTransformMode.ApplyAfterModifiers;

        if(transform) {
            int nbPars = parameters.Length;
            Vector3 shift = (Vector3)parameters[nbPars - 3].GetValue();
            Quaternion rot = (Quaternion)parameters[nbPars - 2].GetValue();
            float scale = (float)parameters[nbPars - 1].GetValue();

            return FERM_Modifier.PostTransform(value, shift, rot, scale);
        }

        return value;
    }

    private string ApplyTransform(string inputFunction) {
        int nbPars = parameters.Length;

        string pos = "@par" + (nbPars - 3);
        string rot = "@par" + (nbPars - 2);
        string scl = "@par" + (nbPars - 1);

        return FERM_Modifier.ApplyTransform(inputFunction, pos, rot, scl);
    }

    protected abstract string GetElementaryDF();
    protected abstract float ElementaryDF(Vector3 pos);

    protected override FERM_Parameter[] GetParameters() {
        FERM_Parameter[] el = GetElementaryParameters();
        int nbPars = el.Length + 3;
        FERM_Parameter[] toReturn = new FERM_Parameter[nbPars];

        for(int i = 0; i < nbPars - 3; i++)
            toReturn[i] = el[i];

        toReturn[nbPars - 3] = new FERM_Parameter("t_pos", Vector3.zero);
        toReturn[nbPars - 2] = new FERM_Parameter("t_rot", Quaternion.identity);
        toReturn[nbPars - 1] = new FERM_Parameter("t_scl", 1f);

        return toReturn;
    }

    /// <summary>
    /// Return (static reference) of internal parameter list.
    /// Given parameters do not contain Unity Transform parameters.
    /// </summary>
    protected abstract FERM_Parameter[] GetElementaryParameters();

    protected override void UpdateParameters() {
        base.UpdateParameters();

        Matrix4x4 projection = rend.transform.worldToLocalMatrix;

        Vector3 pos = projection.MultiplyPoint(transform.position);

        Quaternion rot = Quaternion.Inverse(rend.transform.rotation) * transform.rotation;

        Vector3 scale = FERM_Util.EqualizeScale(transform.localScale);
        transform.localScale = scale;
        float scl = scale.x;
        
        int nbPars = parameters.Length;
        parameters[nbPars - 3].SetValue(pos);
        parameters[nbPars - 2].SetValue(rot);
        parameters[nbPars - 1].SetValue(scl);
    }

}
