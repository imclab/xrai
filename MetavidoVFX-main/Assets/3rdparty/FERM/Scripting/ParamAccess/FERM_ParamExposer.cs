using UnityEngine;

[ExecuteInEditMode]
public class FERM_ParamExposer : FERM_ParamAccess {

    public float value_float;
    public int value_int;
    public Vector3 value_vec;
    public Vector2 value_vec2;
    public Quaternion value_quat;

    private void LateUpdate() {
        ApplyValue();
    }

    public void ApplyValue() {
        if(target == null || target.parameter == null)
            return;

        switch(target.parameter.type) {
        case FERM_Parameter.Type.Floating:
            target.parameter.SetValue(value_float);
            break;
        case FERM_Parameter.Type.Integer:
        case FERM_Parameter.Type.Axis:
            target.parameter.SetValue(value_int);
            break;
        case FERM_Parameter.Type.Vector:
            target.parameter.SetValue(value_vec);
            break;
        case FERM_Parameter.Type.Quaternion:
            target.parameter.SetValue(value_quat);
            break;
        default:
            Debug.LogError("Unkown target type: " + target.parameter.type);
            break;
        }
    }

    public void AcquireValue() {
        if(target == null)
            return;

        switch(target.parameter.type) {
        case FERM_Parameter.Type.Floating:
            value_float = (float)target.parameter.GetValue();
            break;
        case FERM_Parameter.Type.Integer:
            value_int = (int)target.parameter.GetValue();
            break;
        case FERM_Parameter.Type.Vector:
            value_vec = (Vector3)target.parameter.GetValue();
            break;
        case FERM_Parameter.Type.Quaternion:
            value_quat = (Quaternion)target.parameter.GetValue();
            break;
        case FERM_Parameter.Type.Axis:
            value_int = (int)(FERM_Parameter.Axis)target.parameter.GetValue();
            break;
        default:
            Debug.LogError("Unkown target type: " + target.parameter.type);
            break;
        }
    }
}
